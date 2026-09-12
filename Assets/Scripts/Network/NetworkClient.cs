using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using CRClone.Core;
using CRClone.Network;

namespace CRClone.Network
{
    public class NetworkClient : MonoBehaviour
    {
        private ClientWebSocket _ws;
        private bool _isConnected = false;
        private string _serverUrl;
        private string _authToken;
        private uint _lastAckedTick = 0;
        private uint _clientTick = 0;
        private uint _serverTick = 0;
        
        // Input management
        private readonly Queue<PlayerInput> _pendingInputs = new Queue<PlayerInput>();
        private readonly Dictionary<uint, PlayerInput> _sentInputs = new Dictionary<uint, PlayerInput>();
        private readonly Dictionary<uint, float> _inputSentTimes = new Dictionary<uint, float>();
        private const float RETRANSMIT_TIMEOUT = 1f; // 1 second
        private const int MAX_PENDING_INPUTS = 20;

        // Request/Response
        private readonly Dictionary<uint, Action<NetworkMessage>> _pendingRequests = new Dictionary<uint, Action<NetworkMessage>>();
        private uint _requestId = 0;

        // Reconnection
        private ReconnectionManager _reconnectionManager;
        private float _reconnectTimer = 0f;
        private const float RECONNECT_DELAY = 5f;

        // Heartbeat
        private const float HEARTBEAT_INTERVAL = 10f;
        private float _lastHeartbeat = 0f;

        // Message buffer for fragmentation
        private readonly List<byte> _receiveBuffer = new List<byte>();

        public bool IsConnected => _isConnected && _ws?.State == WebSocketState.Open;

        private void Awake()
        {
            _reconnectionManager = GetComponent<ReconnectionManager>();
            if (_reconnectionManager == null)
            {
                _reconnectionManager = gameObject.AddComponent<ReconnectionManager>();
            }
        }

        public void Connect(string serverUrl, string authToken)
        {
            _serverUrl = serverUrl;
            _authToken = authToken;
            _reconnectionManager?.SetCredentials(serverUrl, authToken);
            _ = ConnectAsync();
        }

        private async Task ConnectAsync()
        {
            try
            {
                _ws = new ClientWebSocket();
                var uri = new Uri(_serverUrl);
                
                // Set keep alive
                _ws.Options.KeepAliveInterval = TimeSpan.FromSeconds(30);
                
                await _ws.ConnectAsync(uri, CancellationToken.None);
                
                _isConnected = true;
                _clientTick = 0;
                _lastAckedTick = 0;
                _serverTick = 0;
                _pendingInputs.Clear();
                _sentInputs.Clear();
                _inputSentTimes.Clear();
                _pendingRequests.Clear();

                Debug.Log($"[NetworkClient] Connected to server: {_serverUrl}");
                EventBus.Raise(new NetworkConnectedEvent { serverAddress = _serverUrl });

                // Send auth
                Send(new AuthMessage { token = _authToken });

                // Start receive loop
                _ = ReceiveLoop();
            }
            catch (Exception e)
            {
                Debug.LogError($"[NetworkClient] Connection failed: {e.Message}");
                _isConnected = false;
                _reconnectionManager?.ScheduleReconnect();
            }
        }

        private async Task ReceiveLoop()
        {
            var buffer = new byte[8192];
            var memory = new Memory<byte>(buffer);

            while (_isConnected && _ws.State == WebSocketState.Open)
            {
                try
                {
                    var result = await _ws.ReceiveAsync(memory, CancellationToken.None);
                    
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                        break;
                    }

                    if (result.Count > 0)
                    {
                        // Handle binary protobuf messages
                        var data = new byte[result.Count];
                        Array.Copy(buffer, data, result.Count);
                        ProcessBinaryMessage(data);
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

        private void ProcessBinaryMessage(byte[] data)
        {
            try
            {
                var message = Serialization.DeserializeByType(data);
                if (message != null)
                {
                    HandleMessage(message);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[NetworkClient] Message parse error: {e.Message}");
            }
        }

        private void HandleMessage(NetworkMessage message)
        {
            switch (message.type)
            {
                case MessageTypes.AuthResponse:
                    HandleAuthResponse(message as AuthResponseMessage);
                    break;
                case MessageTypes.BattleFound:
                    HandleBattleFound(message as BattleFoundMessage);
                    break;
                case MessageTypes.BattleStart:
                    HandleBattleStart(message as BattleStartMessage);
                    break;
                case MessageTypes.GameState:
                    HandleGameState(message as GameStateMessage);
                    break;
                case MessageTypes.InputAck:
                    HandleInputAck(message as InputAckMessage);
                    break;
                case MessageTypes.Reconcile:
                    HandleReconcile(message as ReconcileMessage);
                    break;
                case MessageTypes.BattleEnd:
                    HandleBattleEnd(message as BattleEndMessage);
                    break;
                case MessageTypes.Error:
                    HandleError(message as ErrorMessage);
                    break;
                case MessageTypes.Pong:
                    // Heartbeat response
                    break;
                case "clan_response":
                case "shop_response":
                case "quest_response":
                case "season_response":
                case "tournament_response":
                case "replay_response":
                case "player_response":
                    HandleAsyncResponse(message);
                    break;
                default:
                    Debug.LogWarning($"[NetworkClient] Unknown message type: {message.type}");
                    break;
            }
        }

        private void HandleAuthResponse(AuthResponseMessage msg)
        {
            if (msg.success)
            {
                Debug.Log("[NetworkClient] Authenticated successfully");
                _reconnectionManager?.SetCredentials(_serverUrl, _authToken);
            }
            else
            {
                Debug.LogError($"[NetworkClient] Auth failed: {msg.error}");
                Disconnect();
            }
        }

        private void HandleBattleFound(BattleFoundMessage msg)
        {
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

            _reconnectionManager?.SetBattleContext(battleData);
            Services.Get<GameManager>().StartBattle(battleData);
        }

        private void HandleBattleStart(BattleStartMessage msg)
        {
            _serverTick = msg.tick;
            _lastAckedTick = msg.tick;
            
            // Update local simulation with initial state
            var reconcilEvent = new ReconciliationEvent
            {
                serverTick = msg.tick,
                clientTick = _clientTick,
                entityCount = 0,
                fullResync = true
            };
            EventBus.Raise(reconcilEvent);
        }

        private void HandleGameState(GameStateMessage msg)
        {
            _serverTick = msg.tick;
            _lastAckedTick = msg.tick;

            // Remove acknowledged inputs
            var toRemove = new List<uint>();
            foreach (var kvp in _sentInputs)
            {
                if (kvp.Key <= msg.tick)
                    toRemove.Add(kvp.Key);
            }
            foreach (var tick in toRemove)
            {
                _sentInputs.Remove(tick);
                _inputSentTimes.Remove(tick);
            }

            // Forward to simulation for reconciliation
            var reconcilEvent = new ReconciliationEvent
            {
                serverTick = msg.tick,
                clientTick = _clientTick,
                entityCount = msg.entities?.Length ?? 0,
                fullResync = false
            };
            EventBus.Raise(reconcilEvent);

            // TODO: Apply state to BattleSimulation
        }

        private void HandleInputAck(InputAckMessage msg)
        {
            _lastAckedTick = msg.ackTick;

            var toRemove = new List<uint>();
            foreach (var kvp in _sentInputs)
            {
                if (kvp.Key <= msg.ackTick)
                    toRemove.Add(kvp.Key);
            }
            foreach (var tick in toRemove)
            {
                _sentInputs.Remove(tick);
                _inputSentTimes.Remove(tick);
            }
        }

        private void HandleReconcile(ReconcileMessage msg)
        {
            // Full state resync requested
            var reconcilEvent = new ReconciliationEvent
            {
                serverTick = msg.tick,
                clientTick = _clientTick,
                entityCount = msg.entities?.Length ?? 0,
                fullResync = true
            };
            EventBus.Raise(reconcilEvent);
        }

        private void HandleBattleEnd(BattleEndMessage msg)
        {
            var result = new GameManager.BattleResult
            {
                battleId = msg.battleId,
                result = (BattleStatus)Enum.Parse(typeof(BattleStatus), msg.result.winner, true),
                player1Crowns = msg.result.player1Crowns,
                player2Crowns = msg.result.player2Crowns,
                player1TrophyChange = msg.result.player1TrophyChange,
                player2TrophyChange = msg.result.player2TrophyChange,
                duration = msg.result.duration,
                wentOvertime = msg.result.wentOvertime,
                replayId = msg.result.replayId
            };

            _reconnectionManager?.OnBattleEnd();
            Services.Get<GameManager>().EndBattle(result);
        }

        private void HandleError(ErrorMessage msg)
        {
            Debug.LogError($"[NetworkClient] Server error: {msg.code} - {msg.message}");
            EventBus.RaiseError(msg.message);
        }

        private void HandleAsyncResponse(NetworkMessage msg)
        {
            if (msg.requestId > 0 && _pendingRequests.TryGetValue(msg.requestId, out var callback))
            {
                _pendingRequests.Remove(msg.requestId);
                callback?.Invoke(msg);
            }
        }

        public void SendInput(PlayerInput input)
        {
            if (!IsConnected) return;

            input.clientTick = _clientTick;
            _pendingInputs.Enqueue(input);
        }

        public void Send<T>(T message) where T : NetworkMessage
        {
            if (!IsConnected) return;

            var data = Serialization.Serialize(message);
            _ = SendBinaryAsync(data);
        }

        public void SendRequest<TResponse>(NetworkMessage request, Action<TResponse> callback) where TResponse : NetworkMessage
        {
            if (!IsConnected)
            {
                callback?.Invoke(null);
                return;
            }

            uint id = ++_requestId;
            request.requestId = id;
            
            _pendingRequests[id] = (response) => {
                if (response is TResponse typed)
                    callback?.Invoke(typed);
            };

            Send(request);
        }

        private async Task SendBinaryAsync(byte[] data)
        {
            try
            {
                await _ws.SendAsync(new ArraySegment<byte>(data), WebSocketMessageType.Binary, true, CancellationToken.None);
            }
            catch (Exception e)
            {
                Debug.LogError($"[NetworkClient] Send error: {e.Message}");
                HandleDisconnect();
            }
        }

        private void Update()
        {
            if (!IsConnected) return;

            // Send pending inputs
            while (_pendingInputs.Count > 0 && _sentInputs.Count < MAX_PENDING_INPUTS)
            {
                var input = _pendingInputs.Dequeue();
                _sentInputs[_clientTick] = input;
                _inputSentTimes[_clientTick] = Time.time;
                
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
            var now = Time.time;
            var toRetransmit = new List<uint>();

            foreach (var kvp in _inputSentTimes)
            {
                if (now - kvp.Value > RETRANSMIT_TIMEOUT && kvp.Key > _lastAckedTick)
                {
                    toRetransmit.Add(kvp.Key);
                }
            }

            foreach (var tick in toRetransmit)
            {
                if (_sentInputs.TryGetValue(tick, out var input))
                {
                    _inputSentTimes[tick] = now;
                    var msg = new InputMessage
                    {
                        tick = tick,
                        input = input
                    };
                    Send(msg);
                    Debug.Log($"[NetworkClient] Retransmitted input for tick {tick}");
                }
            }
        }

        private void HandleDisconnect()
        {
            _isConnected = false;
            string reason = "Connection lost";
            Debug.Log($"[NetworkClient] Disconnected: {reason}");
            EventBus.Raise(new NetworkDisconnectedEvent { reason = reason, wasClean = false });
            _reconnectionManager?.ScheduleReconnect();
        }

        public void Disconnect()
        {
            if (_ws != null && _ws.State == WebSocketState.Open)
            {
                _ = _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client disconnect", CancellationToken.None);
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

        // Async message sending for non-gameplay messages
        public async Task<TResponse> SendRequestAsync<TResponse>(NetworkMessage request, float timeout = 10f) where TResponse : NetworkMessage
        {
            var tcs = new TaskCompletionSource<TResponse>();
            
            SendRequest<TResponse>(request, (response) => {
                tcs.SetResult(response);
            });

            var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(timeout)));
            if (completedTask == tcs.Task)
            {
                return tcs.Task.Result;
            }
            else
            {
                throw new TimeoutException($"Request timed out after {timeout}s");
            }
        }
    }
}