using System;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using CRClone.Core;

namespace CRClone.Network
{
    public class NetworkClient : MonoBehaviour
    {
        private System.Net.WebSockets.ClientWebSocket _ws;
        private bool _isConnected = false;
        private string _serverUrl;
        private string _authToken;
        private uint _lastAckedTick = 0;
        private uint _clientTick = 0;
        private readonly Queue<PlayerInput> _pendingInputs = new();
        private readonly Dictionary<uint, PlayerInput> _sentInputs = new();
        private readonly Dictionary<uint, Action<NetworkMessage>> _pendingRequests = new();
        private uint _requestId = 0;
        private float _reconnectTimer = 0f;
        private const float RECONNECT_DELAY = 5f;
        private const float HEARTBEAT_INTERVAL = 10f;
        private float _lastHeartbeat = 0f;

        public bool IsConnected => _isConnected && _ws?.State == System.Net.WebSockets.WebSocketState.Open;

        public void Connect(string serverUrl, string authToken)
        {
            _serverUrl = serverUrl;
            _authToken = authToken;
            _ = ConnectAsync();
        }

        private async System.Threading.Tasks.Task ConnectAsync()
        {
            try
            {
                _ws = new System.Net.WebSockets.ClientWebSocket();
                var uri = new Uri(_serverUrl);
                await _ws.ConnectAsync(uri, System.Threading.CancellationToken.None);
                
                _isConnected = true;
                _clientTick = 0;
                _lastAckedTick = 0;
                _pendingInputs.Clear();
                _sentInputs.Clear();

                Debug.Log("[NetworkClient] Connected to server");
                EventBus.Raise(new EventBus.NetworkConnectedEvent { serverAddress = _serverUrl });

                // Send auth
                Send(new AuthMessage { token = _authToken });

                // Start receive loop
                _ = ReceiveLoop();
            }
            catch (Exception e)
            {
                Debug.LogError($"[NetworkClient] Connection failed: {e.Message}");
                _isConnected = false;
                ScheduleReconnect();
            }
        }

        private async System.Threading.Tasks.Task ReceiveLoop()
        {
            var buffer = new byte[8192];
            var memory = new Memory<byte>(buffer);

            while (_isConnected && _ws.State == System.Net.WebSockets.WebSocketState.Open)
            {
                try
                {
                    var result = await _ws.ReceiveAsync(memory, System.Threading.CancellationToken.None);
                    
                    if (result.MessageType == System.Net.WebSockets.WebSocketMessageType.Close)
                    {
                        await _ws.CloseAsync(System.Net.WebSockets.WebSocketCloseStatus.NormalClosure, "", System.Threading.CancellationToken.None);
                        break;
                    }

                    if (result.Count > 0)
                    {
                        var data = new byte[result.Count];
                        Array.Copy(buffer, data, result.Count);
                        ProcessMessage(data);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[NetworkClient] Receive error: {e.Message}");
                    break;
                }
            }

            HandleDisconnect();
        }

        private void ProcessMessage(byte[] data)
        {
            try
            {
                var json = Encoding.UTF8.GetString(data);
                var baseMsg = Newtonsoft.Json.JsonConvert.DeserializeObject<NetworkMessage>(json);

                switch (baseMsg.type)
                {
                    case "auth_response":
                        HandleAuthResponse(json);
                        break;
                    case "battle_found":
                        HandleBattleFound(json);
                        break;
                    case "game_state":
                        HandleGameState(json);
                        break;
                    case "input_ack":
                        HandleInputAck(json);
                        break;
                    case "reconcile":
                        HandleReconcile(json);
                        break;
                    case "battle_end":
                        HandleBattleEnd(json);
                        break;
                    case "error":
                        HandleError(json);
                        break;
                    case "pong":
                        // Heartbeat response
                        break;
                    default:
                        Debug.LogWarning($"[NetworkClient] Unknown message type: {baseMsg.type}");
                        break;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[NetworkClient] Message parse error: {e.Message}");
            }
        }

        private void HandleAuthResponse(string json)
        {
            var msg = Newtonsoft.Json.JsonConvert.DeserializeObject<AuthResponseMessage>(json);
            if (msg.success)
            {
                Debug.Log("[NetworkClient] Authenticated");
            }
            else
            {
                Debug.LogError($"[NetworkClient] Auth failed: {msg.error}");
                Disconnect();
            }
        }

        private void HandleBattleFound(string json)
        {
            var msg = Newtonsoft.Json.JsonConvert.DeserializeObject<BattleFoundMessage>(json);
            var battleData = new GameManager.BattleData
            {
                battleId = msg.battleId,
                type = (GameManager.BattleType)msg.battleType,
                seed = msg.seed,
                player1 = new GameManager.PlayerBattleInfo
                {
                    playerId = msg.player1.playerId,
                    username = msg.player1.username,
                    trophies = msg.player1.trophies,
                    deck = new GameManager.DeckData { cardIds = msg.player1.deck }
                },
                player2 = new GameManager.PlayerBattleInfo
                {
                    playerId = msg.player2.playerId,
                    username = msg.player2.username,
                    trophies = msg.player2.trophies,
                    deck = new GameManager.DeckData { cardIds = msg.player2.deck }
                }
            };

            Services.Get<GameManager>().StartBattle(battleData);
        }

        private void HandleGameState(string json)
        {
            var msg = Newtonsoft.Json.JsonConvert.DeserializeObject<GameStateMessage>(json);
            
            _lastAckedTick = msg.tick;
            
            // Remove acknowledged inputs
            var toRemove = new List<uint>();
            foreach (var kvp in _sentInputs)
            {
                if (kvp.Key <= msg.tick)
                    toRemove.Add(kvp.Key);
            }
            foreach (var tick in toRemove) _sentInputs.Remove(tick);

            // Forward to simulation for reconciliation
            EventBus.Raise(new EventBus.ReconciliationEvent
            {
                serverTick = msg.tick,
                clientTick = _clientTick,
                entityCount = msg.entities?.Length ?? 0
            });

            // TODO: Apply state to BattleSimulation
        }

        private void HandleInputAck(string json)
        {
            var msg = Newtonsoft.Json.JsonConvert.DeserializeObject<InputAckMessage>(json);
            _lastAckedTick = msg.ackTick;
            
            var toRemove = new List<uint>();
            foreach (var kvp in _sentInputs)
            {
                if (kvp.Key <= msg.ackTick)
                    toRemove.Add(kvp.Key);
            }
            foreach (var tick in toRemove) _sentInputs.Remove(tick);
        }

        private void HandleReconcile(string json)
        {
            var msg = Newtonsoft.Json.JsonConvert.DeserializeObject<ReconcileMessage>(json);
            // Full state resync
            EventBus.Raise(new EventBus.ReconciliationEvent
            {
                serverTick = msg.tick,
                clientTick = _clientTick,
                entityCount = msg.entities?.Length ?? 0,
                fullResync = true
            });
        }

        private void HandleBattleEnd(string json)
        {
            var msg = Newtonsoft.Json.JsonConvert.DeserializeObject<BattleEndMessage>(json);
            var result = new GameManager.BattleResult
            {
                battleId = msg.battleId,
                result = (BattleStatus)msg.result,
                player1Crowns = msg.player1Crowns,
                player2Crowns = msg.player2Crowns,
                player1TrophyChange = msg.player1TrophyChange,
                player2TrophyChange = msg.player2TrophyChange,
                duration = msg.duration,
                wentOvertime = msg.wentOvertime,
                replayId = msg.replayId
            };

            Services.Get<GameManager>().EndBattle(result);
        }

        private void HandleError(string json)
        {
            var msg = Newtonsoft.Json.JsonConvert.DeserializeObject<ErrorMessage>(json);
            EventBus.RaiseError(msg.message);
        }

        public void SendInput(PlayerInput input)
        {
            input.clientTick = _clientTick;
            _pendingInputs.Enqueue(input);
        }

        public void Send(NetworkMessage message)
        {
            if (!IsConnected) return;

            var json = Newtonsoft.Json.JsonConvert.SerializeObject(message);
            var data = Encoding.UTF8.GetBytes(json);
            _ = _ws.SendAsync(new ArraySegment<byte>(data), System.Net.WebSockets.WebSocketMessageType.Text, true, System.Threading.CancellationToken.None);
        }

        public void SendRequest<TResponse>(NetworkMessage request, Action<TResponse> callback) where TResponse : NetworkMessage
        {
            uint id = ++_requestId;
            request.requestId = id;
            
            _pendingRequests[id] = (response) => {
                if (response is TResponse typed)
                    callback?.Invoke(typed);
            };

            Send(request);
        }

        private void Update()
        {
            if (!IsConnected) return;

            // Send pending inputs
            while (_pendingInputs.Count > 0)
            {
                var input = _pendingInputs.Dequeue();
                _sentInputs[_clientTick] = input;
                
                var msg = new InputMessage
                {
                    tick = _clientTick,
                    input = input
                };
                Send(msg);
            }

            _clientTick++;

            // Heartbeat
            if (Time.time - _lastHeartbeat > HEARTBEAT_INTERVAL)
            {
                Send(new HeartbeatMessage());
                _lastHeartbeat = Time.time;
            }

            // Check for stale inputs (retransmit)
            CheckRetransmission();
        }

        private void CheckRetransmission()
        {
            const float RETRANSMIT_TIMEOUT = 1f; // 1 second
            // In a real implementation, track send times and retransmit unacknowledged inputs
        }

        private void HandleDisconnect()
        {
            _isConnected = false;
            string reason = "Connection lost";
            Debug.Log($"[NetworkClient] Disconnected: {reason}");
            EventBus.Raise(new EventBus.NetworkDisconnectedEvent { reason = reason, wasClean = false });
            ScheduleReconnect();
        }

        private void ScheduleReconnect()
        {
            _reconnectTimer = RECONNECT_DELAY;
        }

        public void Disconnect()
        {
            if (_ws != null && _ws.State == System.Net.WebSockets.WebSocketState.Open)
            {
                _ = _ws.CloseAsync(System.Net.WebSockets.WebSocketCloseStatus.NormalClosure, "Client disconnect", System.Threading.CancellationToken.None);
            }
            _isConnected = false;
        }

        public void Reconnect()
        {
            if (!string.IsNullOrEmpty(_serverUrl) && !string.IsNullOrEmpty(_authToken))
            {
                Connect(_serverUrl, _authToken);
            }
        }

        // Message classes
        [Serializable]
        public class NetworkMessage
        {
            public string type;
            public uint requestId;
            public uint tick;
        }

        [Serializable]
        public class AuthMessage : NetworkMessage
        {
            public string token;
        }

        [Serializable]
        public class AuthResponseMessage : NetworkMessage
        {
            public bool success;
            public string error;
        }

        [Serializable]
        public class BattleFoundMessage : NetworkMessage
        {
            public long battleId;
            public int battleType;
            public ulong seed;
            public PlayerInfo player1;
            public PlayerInfo player2;

            [Serializable]
            public class PlayerInfo
            {
                public long playerId;
                public string username;
                public int trophies;
                public int[] deck;
            }
        }

        [Serializable]
        public class InputMessage : NetworkMessage
        {
            public PlayerInput input;
        }

        [Serializable]
        public class GameStateMessage : NetworkMessage
        {
            public uint tick;
            public EntityState[] entities;
            public ProjectileState[] projectiles;
            public PlayerState player1;
            public PlayerState player2;
            public BattleStatus status;
        }

        [Serializable]
        public class InputAckMessage : NetworkMessage
        {
            public uint ackTick;
        }

        [Serializable]
        public class ReconcileMessage : NetworkMessage
        {
            public uint tick;
            public EntityState[] entities;
        }

        [Serializable]
        public class BattleEndMessage : NetworkMessage
        {
            public long battleId;
            public int result;
            public int player1Crowns;
            public int player2Crowns;
            public int player1TrophyChange;
            public int player2TrophyChange;
            public float duration;
            public bool wentOvertime;
            public long replayId;
        }

        [Serializable]
        public class ErrorMessage : NetworkMessage
        {
            public string message;
        }

        [Serializable]
        public class HeartbeatMessage : NetworkMessage { }

        [Serializable]
        public class PlayerInput
        {
            public uint clientTick;
            public int cardId;
            public Vector2 position;
            public int spellId;
            public Vector2 targetPosition;
            public InputType type;
        }

        public enum InputType
        {
            PlayCard,
            CastSpell,
            UseChampionAbility,
            Emote
        }

        [Serializable]
        public class EntityState
        {
            public uint id;
            public int type;
            public int owner;
            public Vector2 position;
            public Vector2 velocity;
            public int hp;
            public uint targetId;
        }

        [Serializable]
        public class ProjectileState
        {
            public uint id;
            public int type;
            public int owner;
            public Vector2 position;
            public Vector2 velocity;
            public uint targetId;
        }

        [Serializable]
        public class PlayerState
        {
            public int elixir;
            public int[] hand;
            public int nextCardIndex;
        }
    }
}