using System;
using UnityEngine;
using UnityEngine.UI;
using CRClone.Core;
using CRClone.Battle.Simulation;
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

        private void Awake()
        {
            _simulation = Services.Get<GameManager>().BattleSim;

            _pauseButton?.onClick.AddListener(OnPauseClicked);
            _resumeButton?.onClick.AddListener(OnResumeClicked);
            _settingsButton?.onClick.AddListener(OnSettingsClicked);
            _concedeButton?.onClick.AddListener(OnConcedeClicked);

            if (_pauseMenu != null) _pauseMenu.SetActive(false);
            if (_overtimeLabel != null) _overtimeLabel.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (_simulation == null || _isPaused) return;

            UpdateTimer();
            UpdateCrowns();
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

        private System.Collections.IEnumerator OvertimeFlashCoroutine()
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

        private void UpdateCrowns()
        {
            int p1Crowns = 0, p2Crowns = 0;

            foreach (var tower in _simulation.Towers)
            {
                if (tower.Type != TowerType.King && tower.IsDead)
                {
                    if (tower.OwnerPlayerId == 1) p2Crowns++;
                    else p1Crowns++;
                }
            }

            for (int i = 0; i < 3; i++)
            {
                if (i < _p1Crowns.Length)
                {
                    _p1Crowns[i].sprite = i < p1Crowns ? _crownFilled : _crownEmpty;
                }

                if (i < _p2Crowns.Length)
                {
                    _p2Crowns[i].sprite = i < p2Crowns ? _crownFilled : _crownEmpty;
                }
            }

            bool p1KingDead = false, p2KingDead = false;
            foreach (var tower in _simulation.Towers)
            {
                if (tower.Type == TowerType.King && tower.IsDead)
                {
                    if (tower.OwnerPlayerId == 1) p2KingDead = true;
                    else p1KingDead = true;
                }
            }

            if (p1KingDead)
            {
                for (int i = 0; i < 3; i++)
                {
                    if (i < _p1Crowns.Length) _p1Crowns[i].sprite = _kingCrown;
                }
            }
            if (p2KingDead)
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
            _pauseMenu?.SetActive(true);
            Time.timeScale = 0f;
            UISoundPlayer.Instance?.PlayScreenOpen();
        }

        public void OnResumeClicked()
        {
            _isPaused = false;
            _pauseMenu?.SetActive(false);
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

            var confirmModal = UIManager.Instance?.ShowModal(Resources.Load<GameObject>("UI/ConcedeConfirmModal"));
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
            _pauseButton?.gameObject.SetActive(false);
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