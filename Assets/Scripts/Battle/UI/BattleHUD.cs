using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using CRClone.Core;
using CRClone.Battle.Simulation;
using CRClone.UI;
using CRClone.UI.Animation;

namespace CRClone.Battle.UI
{
    public class BattleHUD : MonoBehaviour
    {
        [Header("Timer")]
        [SerializeField] private Text _timerText;
        [SerializeField] private Image _timerBackground;
        [SerializeField] private Text _overtimeLabel;

        [Header("Crowns")]
        [SerializeField] private Image[] _p1Crowns = new Image[3];
        [SerializeField] private Image[] _p2Crowns = new Image[3];
        [SerializeField] private Sprite _crownFilled;
        [SerializeField] private Sprite _crownEmpty;
        [SerializeField] private Sprite _kingCrown;

        [Header("Player Names")]
        [SerializeField] private Text _p1NameText;
        [SerializeField] private Text _p2NameText;

        [Header("Tower Health")]
        [SerializeField] private TowerHealthUI[] _p1Towers;
        [SerializeField] private TowerHealthUI[] _p2Towers;

        [Header("Pause Menu")]
        [SerializeField] private Button _pauseButton;
        [SerializeField] private GameObject _pauseMenu;
        [SerializeField] private Button _resumeButton;
        [SerializeField] private Button _settingsButton;
        [SerializeField] private Button _concedeButton;
        [SerializeField] private Transform _connectedPlayersContainer;
        [SerializeField] private GameObject _playerListItemPrefab;

        [Header("Animation")]
        [SerializeField] private float _overtimeFlashDuration = 0.5f;
        [SerializeField] private Color _overtimeColor = Color.red;
        [SerializeField] private Color _normalColor = Color.white;

        private BattleSimulation _simulation;
        private bool _isPaused;
        private bool _wasOvertime;
        private Coroutine _overtimeFlashCoroutine;
        private int _p1CrownsCount;
        private int _p2CrownsCount;

        private void Awake()
        {
            _simulation = Services.Get<GameManager>().BattleSim;

            _pauseButton.OrNull()?.onClick.AddListener(OnPauseClicked);
            _resumeButton.OrNull()?.onClick.AddListener(OnResumeClicked);
            _settingsButton.OrNull()?.onClick.AddListener(OnSettingsClicked);
            _concedeButton.OrNull()?.onClick.AddListener(OnConcedeClicked);

            if (_pauseMenu != null) _pauseMenu.SetActive(false);
            if (_overtimeLabel != null) _overtimeLabel.gameObject.SetActive(false);

            InitializeTowerHealthUI();
            SubscribeToEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        private void SubscribeToEvents()
        {
            EventBus.OnTowerDamaged += OnTowerDamaged;
            EventBus.OnTowerDestroyed += OnTowerDestroyed;
            EventBus.OnBattleEnded += OnBattleEnded;
            EventBus.OnKingTowerActivated += OnKingTowerActivated;
        }

        private void UnsubscribeFromEvents()
        {
            EventBus.OnTowerDamaged -= OnTowerDamaged;
            EventBus.OnTowerDestroyed -= OnTowerDestroyed;
            EventBus.OnBattleEnded -= OnBattleEnded;
            EventBus.OnKingTowerActivated -= OnKingTowerActivated;
        }

        private void InitializeTowerHealthUI()
        {
            if (_simulation == null) return;

            // Initialize P1 towers
            int p1TowerIndex = 0;
            foreach (var tower in _simulation.Towers)
            {
                if (tower.OwnerPlayerId == 1)
                {
                    if (p1TowerIndex < _p1Towers.Length && _p1Towers[p1TowerIndex] != null)
                    {
                        _p1Towers[p1TowerIndex].Initialize(tower);
                    }
                    p1TowerIndex++;
                }
            }

            // Initialize P2 towers
            int p2TowerIndex = 0;
            foreach (var tower in _simulation.Towers)
            {
                if (tower.OwnerPlayerId == 2)
                {
                    if (p2TowerIndex < _p2Towers.Length && _p2Towers[p2TowerIndex] != null)
                    {
                        _p2Towers[p2TowerIndex].Initialize(tower);
                    }
                    p2TowerIndex++;
                }
            }
        }

        private void OnTowerDamaged(EventBus.TowerDamagedEvent evt)
        {
            if (evt.playerId == 1)
            {
                int towerIndex = GetTowerIndex(evt.towerType);
                if (towerIndex >= 0 && towerIndex < _p1Towers.Length && _p1Towers[towerIndex] != null)
                {
                    _p1Towers[towerIndex].PlayDamageEffect(evt.damage);
                }
            }
            else if (evt.playerId == 2)
            {
                int towerIndex = GetTowerIndex(evt.towerType);
                if (towerIndex >= 0 && towerIndex < _p2Towers.Length && _p2Towers[towerIndex] != null)
                {
                    _p2Towers[towerIndex].PlayDamageEffect(evt.damage);
                }
            }
        }

        private void OnTowerDestroyed(EventBus.TowerDestroyedEvent evt)
        {
            if (evt.playerId == 1)
            {
                _p2CrownsCount++;
                UpdateCrownDisplay();
                
                int towerIndex = GetTowerIndex(evt.towerType);
                if (towerIndex >= 0 && towerIndex < _p1Towers.Length && _p1Towers[towerIndex] != null)
                {
                    _p1Towers[towerIndex].PlayDestroyAnimation();
                }

                if (evt.towerType == EventBus.TowerType.King)
                {
                    // King tower destroyed - 3 crown win
                    _p2CrownsCount = 3;
                    UpdateCrownDisplay();
                }
            }
            else if (evt.playerId == 2)
            {
                _p1CrownsCount++;
                UpdateCrownDisplay();

                int towerIndex = GetTowerIndex(evt.towerType);
                if (towerIndex >= 0 && towerIndex < _p2Towers.Length && _p2Towers[towerIndex] != null)
                {
                    _p2Towers[towerIndex].PlayDestroyAnimation();
                }

                if (evt.towerType == EventBus.TowerType.King)
                {
                    _p1CrownsCount = 3;
                    UpdateCrownDisplay();
                }
            }
        }

        private void OnKingTowerActivated(EventBus.KingTowerActivatedEvent evt)
        {
            // Visual feedback for king tower activation
            if (evt.playerId == 1)
            {
                int kingIndex = Array.FindIndex(_p1Towers, t => t != null && t.GetTowerType() == TowerType.King);
                if (kingIndex >= 0 && _p1Towers[kingIndex] != null)
                {
                    _p1Towers[kingIndex].PlayKingActivation();
                }
            }
            else
            {
                int kingIndex = Array.FindIndex(_p2Towers, t => t != null && t.GetTowerType() == TowerType.King);
                if (kingIndex >= 0 && _p2Towers[kingIndex] != null)
                {
                    _p2Towers[kingIndex].PlayKingActivation();
                }
            }
        }

        private void OnBattleEnded(EventBus.BattleEndedEvent evt)
        {
            ShowBattleResult();
        }

        private int GetTowerIndex(EventBus.TowerType towerType)
        {
            return towerType switch
            {
                EventBus.TowerType.King => 0,
                EventBus.TowerType.PrincessLeft => 1,
                EventBus.TowerType.PrincessRight => 2,
                _ => -1
            };
        }

        private void Update()
        {
            if (_simulation == null || _isPaused) return;

            UpdateTimer();
            UpdatePlayerNames();
        }

        private void UpdateTimer()
        {
            float remaining = Mathf.Max(0, _simulation._config.battleDuration - _simulation.CurrentTick * BattleSimulation.FIXED_DT);
            bool overtime = remaining <= 0;

            if (overtime)
            {
                remaining = Mathf.Max(0, _simulation._config.overtimeDuration - (_simulation.CurrentTick * BattleSimulation.FIXED_DT - _simulation._config.battleDuration));
            }

            int minutes = Mathf.FloorToInt(remaining / 60f);
            int seconds = Mathf.FloorToInt(remaining % 60f);

            if (_timerText != null)
            {
                _timerText.text = $"{minutes:00}:{seconds:00}";
            }

            if (overtime != _wasOvertime)
            {
                _wasOvertime = overtime;
                OnOvertimeStateChanged(overtime);
            }

            if (overtime)
            {
                if (_timerText != null) _timerText.color = _overtimeColor;
                if (_timerBackground != null) _timerBackground.color = new Color(1f, 0.3f, 0.3f, 0.8f);
                if (_overtimeLabel != null) _overtimeLabel.gameObject.SetActive(true);
            }
            else
            {
                if (_timerText != null) _timerText.color = _normalColor;
                if (_timerBackground != null) _timerBackground.color = new Color(0f, 0f, 0f, 0.5f);
                if (_overtimeLabel != null) _overtimeLabel.gameObject.SetActive(false);
            }
        }

        private void OnOvertimeStateChanged(bool isOvertime)
        {
            if (isOvertime)
            {
                if (_overtimeFlashCoroutine != null) StopCoroutine(_overtimeFlashCoroutine);
                _overtimeFlashCoroutine = StartCoroutine(OvertimeFlashCoroutine());
                UISoundPlayer.Instance?.PlayElixirTick();
            }
            else
            {
                if (_overtimeFlashCoroutine != null)
                {
                    StopCoroutine(_overtimeFlashCoroutine);
                    _overtimeFlashCoroutine = null;
                }
            }
        }

        private IEnumerator OvertimeFlashCoroutine()
        {
            while (_wasOvertime)
            {
                if (_timerText != null)
                {
                    _timerText.color = _overtimeColor;
                }
                yield return new WaitForSeconds(_overtimeFlashDuration);

                if (_timerText != null)
                {
                    _timerText.color = _normalColor;
                }
                yield return new WaitForSeconds(_overtimeFlashDuration);
            }
        }

        private void UpdateCrownDisplay()
        {
            for (int i = 0; i < 3; i++)
            {
                if (i < _p1Crowns.Length)
                {
                    _p1Crowns[i].sprite = i < _p1CrownsCount ? _crownFilled : _crownEmpty;
                }

                if (i < _p2Crowns.Length)
                {
                    _p2Crowns[i].sprite = i < _p2CrownsCount ? _crownFilled : _crownEmpty;
                }
            }

            if (_p1CrownsCount == 3)
            {
                for (int i = 0; i < 3; i++)
                {
                    if (i < _p1Crowns.Length) _p1Crowns[i].sprite = _kingCrown;
                }
            }
            if (_p2CrownsCount == 3)
            {
                for (int i = 0; i < 3; i++)
                {
                    if (i < _p2Crowns.Length) _p2Crowns[i].sprite = _kingCrown;
                }
            }
        }

        private void UpdatePlayerNames()
        {
            if (_p1NameText != null && _simulation.Player1 != null)
            {
                _p1NameText.text = _simulation.Player1.PlayerName;
            }
            if (_p2NameText != null && _simulation.Player2 != null)
            {
                _p2NameText.text = _simulation.Player2.PlayerName;
            }
        }

        private void OnPauseClicked()
        {
            _isPaused = true;
            _pauseMenu.OrNull()?.SetActive(true);
            Time.timeScale = 0f;
            UISoundPlayer.Instance?.PlayScreenOpen();
        }

        public void OnResumeClicked()
        {
            _isPaused = false;
            _pauseMenu.OrNull()?.SetActive(false);
            Time.timeScale = 1f;
            UISoundPlayer.Instance?.PlayScreenClose();
        }

        private void OnSettingsClicked()
        {
            UISoundPlayer.Instance?.PlayButtonClick();
            UIManager.Instance?.ShowModal(Resources.Load<GameObject>("UI/SettingsModal"));
        }

        private void OnConcedeClicked()
        {
            UISoundPlayer.Instance?.PlayButtonClick();

            UIManager.Instance?.ShowModal(Resources.Load<GameObject>("UI/ConcedeConfirmModal"));
        }

        public void UpdateConnectedPlayers(System.Collections.Generic.List<PlayerInfo> players)
        {
            if (_connectedPlayersContainer == null || _playerListItemPrefab == null) return;

            foreach (Transform child in _connectedPlayersContainer)
            {
                Destroy(child.gameObject);
            }

            foreach (var player in players)
            {
                var itemGO = Instantiate(_playerListItemPrefab, _connectedPlayersContainer);
                var itemUI = itemGO.GetComponent<ConnectedPlayerItemUI>();
                if (itemUI != null)
                {
                    itemUI.Initialize(player);
                }
            }
        }

        public void ShowBattleResult()
        {
            _pauseButton.OrNull()?.gameObject.SetActive(false);
            UIManager.Instance?.ShowBattleResult(new EventBus.BattleEndedEvent
            {
                result = _p1CrownsCount > _p2CrownsCount ? BattleStatus.Player1Won : 
                        _p2CrownsCount > _p1CrownsCount ? BattleStatus.Player2Won : BattleStatus.Draw,
                player1Crowns = _p1CrownsCount,
                player2Crowns = _p2CrownsCount
            });
        }
    }

    public class ConnectedPlayerItemUI : MonoBehaviour
    {
        [SerializeField] private Text _playerNameText;
        [SerializeField] private Image _connectionStatusIcon;
        [SerializeField] private Sprite _connectedSprite;
        [SerializeField] private Sprite _disconnectedSprite;
        [SerializeField] private Sprite _reconnectingSprite;

        public void Initialize(PlayerInfo player)
        {
            if (_playerNameText != null) _playerNameText.text = player.playerName;

            if (_connectionStatusIcon != null)
            {
                _connectionStatusIcon.sprite = player.isConnected ? _connectedSprite : 
                    player.isReconnecting ? _reconnectingSprite : _disconnectedSprite;
            }
        }
    }

    [Serializable]
    public class PlayerInfo
    {
        public string playerName;
        public bool isConnected;
        public bool isReconnecting;
        public int ping;
    }
}