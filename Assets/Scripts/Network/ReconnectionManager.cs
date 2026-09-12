using System;
using System.Collections.Generic;
using UnityEngine;
using CRClone.Core;
using CRClone.Network;

namespace CRClone.Network
{
    public class ReconnectionManager : MonoBehaviour
    {
        private NetworkClient _networkClient;
        private string _serverUrl;
        private string _authToken;
        private int _reconnectAttempts = 0;
        private const int MAX_RECONNECT_ATTEMPTS = 10;
        private const float BASE_DELAY = 5f;
        private const float MAX_DELAY = 60f;
        private const float RECONNECT_JITTER = 0.5f;

        private uint _lastKnownServerTick = 0;
        private GameManager.BattleData _currentBattleData;
        private bool _wasInBattle = false;

        public event Action OnReconnectionStarted;
        public event Action OnReconnectionSuccess;
        public event Action<string> OnReconnectionFailed;
        public event Action<uint> OnStateResyncRequested;

        private void Awake()
        {
            _networkClient = GetComponent<NetworkClient>();
            if (_networkClient == null)
            {
                _networkClient = gameObject.AddComponent<NetworkClient>();
            }

            // Subscribe to network events
            EventBus.On<NetworkDisconnectedEvent>(HandleDisconnected);
            EventBus.On<NetworkConnectedEvent>(HandleConnected);
            EventBus.On<ReconciliationEvent>(HandleReconciliation);
        }

        private void OnDestroy()
        {
            EventBus.Off<NetworkDisconnectedEvent>(HandleDisconnected);
            EventBus.Off<NetworkConnectedEvent>(HandleConnected);
            EventBus.Off<ReconciliationEvent>(HandleReconciliation);
        }

        public void SetCredentials(string serverUrl, string authToken)
        {
            _serverUrl = serverUrl;
            _authToken = authToken;
        }

        public void SetBattleContext(GameManager.BattleData battleData)
        {
            _currentBattleData = battleData;
            _wasInBattle = true;
        }

        public void ClearBattleContext()
        {
            _currentBattleData = null;
            _wasInBattle = false;
            _lastKnownServerTick = 0;
        }

        public void UpdateLastKnownTick(uint serverTick)
        {
            _lastKnownServerTick = serverTick;
        }

        private void HandleDisconnected(NetworkDisconnectedEvent evt)
        {
            if (!_wasInBattle && !_networkClient.IsConnected)
            {
                // Not in battle, just schedule reconnect
                ScheduleReconnect();
                return;
            }

            // In battle - start reconnection with state recovery
            Debug.Log($"[ReconnectionManager] Disconnected during battle. Reason: {evt.reason}. Starting reconnection...");
            OnReconnectionStarted?.Invoke();
            ScheduleReconnect();
        }

        private void HandleConnected(NetworkConnectedEvent evt)
        {
            if (_reconnectAttempts > 0)
            {
                _reconnectAttempts = 0;
                Debug.Log("[ReconnectionManager] Reconnected successfully");
                
                // Re-authenticate
                if (!string.IsNullOrEmpty(_authToken))
                {
                    _networkClient.Send(new AuthMessage { token = _authToken });
                }

                OnReconnectionSuccess?.Invoke();
            }
        }

        private void HandleReconciliation(ReconciliationEvent evt)
        {
            _lastKnownServerTick = evt.serverTick;
        }

        private void ScheduleReconnect()
        {
            if (_reconnectAttempts >= MAX_RECONNECT_ATTEMPTS)
            {
                Debug.LogError("[ReconnectionManager] Max reconnect attempts reached. Giving up.");
                OnReconnectionFailed?.Invoke("Max reconnection attempts reached");
                ReturnToMainMenu();
                return;
            }

            float delay = Mathf.Min(BASE_DELAY * Mathf.Pow(2, _reconnectAttempts), MAX_DELAY);
            // Add jitter to prevent thundering herd
            delay += UnityEngine.Random.Range(-RECONNECT_JITTER, RECONNECT_JITTER) * delay;
            
            Debug.Log($"[ReconnectionManager] Scheduling reconnect attempt {_reconnectAttempts + 1}/{MAX_RECONNECT_ATTEMPTS} in {delay:F1}s");
            _reconnectAttempts++;

            StartCoroutine(ReconnectAfterDelay(delay));
        }

        private System.Collections.IEnumerator ReconnectAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);

            if (_networkClient.IsConnected) yield break;

            if (!string.IsNullOrEmpty(_serverUrl) && !string.IsNullOrEmpty(_authToken))
            {
                _networkClient.Connect(_serverUrl, _authToken);
            }
            else
            {
                Debug.LogError("[ReconnectionManager] Missing credentials for reconnection");
                OnReconnectionFailed?.Invoke("Missing credentials");
                ReturnToMainMenu();
            }
        }

        private void ReturnToMainMenu()
        {
            // Notify UI to show disconnect screen
            EventBus.Raise(new NetworkDisconnectedEvent 
            { 
                reason = "Connection lost - returning to menu", 
                wasClean = false 
            });

            // Load main menu scene
            Services.Get<GameManager>()?.LoadScene(GameManager.GameScene.MainMenu, null);
        }

        // Public method to force reconnection (e.g., from UI button)
        public void ForceReconnect()
        {
            _reconnectAttempts = 0;
            if (!string.IsNullOrEmpty(_serverUrl) && !string.IsNullOrEmpty(_authToken))
            {
                _networkClient.Connect(_serverUrl, _authToken);
            }
        }

        // Request full state resync from server
        public void RequestStateResync()
        {
            if (_lastKnownServerTick > 0)
            {
                Debug.Log($"[ReconnectionManager] Requesting state resync from tick {_lastKnownServerTick}");
                OnStateResyncRequested?.Invoke(_lastKnownServerTick);
                
                // Send reconcile request to server
                var reconcileMsg = new ReconcileMessage
                {
                    tick = _lastKnownServerTick,
                    entities = new EntityState[0] // Empty - we want full state
                };
                _networkClient.Send(reconcileMsg);
            }
        }

        // Handle battle end - clear context
        public void OnBattleEnd()
        {
            ClearBattleContext();
            _reconnectAttempts = 0;
        }
    }
}