using System;
using UnityEngine;
using UnityEngine.UI;
using CRClone.Core;

namespace CRClone.UI.Screens
{
    public class ProfileScreen : MonoBehaviour
    {
        [Header("Profile Info")]
        [SerializeField] private Image _avatarImage;
        [SerializeField] private Text _playerNameText;
        [SerializeField] private Text _playerTagText;
        [SerializeField] private Text _trophiesText;
        [SerializeField] private Text _bestTrophiesText;
        [SerializeField] private Text _levelText;
        [SerializeField] private Image _levelProgressFill;
        [SerializeField] private Text _winRateText;
        [SerializeField] private Text _totalBattlesText;
        [SerializeField] private Text _threeCrownWinsText;
        [SerializeField] private Text _cardsCollectedText;
        [SerializeField] private Text _favoriteCardText;

        [Header("Battle Log")]
        [SerializeField] private Transform _battleLogContainer;
        [SerializeField] private GameObject _battleLogItemPrefab;
        [SerializeField] private int _maxLogItems = 20;

        [Header("Action Buttons")]
        [SerializeField] private Button _changeNameButton;
        [SerializeField] private Button _settingsButton;
        [SerializeField] private Button _backButton;

        private void Awake()
        {
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            _backButton?.onClick.AddListener(() => Services.Get<GameManager>().ChangeState(GameState.MainMenu));
            _settingsButton?.onClick.AddListener(() => Services.Get<GameManager>().ChangeState(GameState.Settings));
            _changeNameButton?.onClick.AddListener(OnChangeName);
        }

        private void OnEnable()
        {
            RefreshProfile();
        }

        private void RefreshProfile()
        {
            var playerData = Services.Get<GameManager>().LocalPlayer;
            if (playerData == null) return;

            _playerNameText.text = playerData.playerName;
            _playerTagText.text = $"#{playerData.playerTag}";
            _trophiesText.text = playerData.trophies.ToString("N0");
            _bestTrophiesText.text = playerData.bestTrophies.ToString("N0");
            _levelText.text = $"Level {playerData.level}";

            if (_levelProgressFill != null)
            {
                float progress = GetLevelProgress(playerData.experience, playerData.level);
                _levelProgressFill.fillAmount = progress;
            }

            int totalBattles = playerData.wins + playerData.losses + playerData.draws;
            float winRate = totalBattles > 0 ? (float)playerData.wins / totalBattles * 100f : 0f;

            _winRateText.text = $"{winRate:F1}%";
            _totalBattlesText.text = totalBattles.ToString("N0");
            _threeCrownWinsText.text = playerData.threeCrownWins.ToString("N0");

            int cardsOwned = 0;
            if (playerData.collection != null)
            {
                foreach (var kvp in playerData.collection)
                {
                    if (kvp.Value > 0) cardsOwned++;
                }
            }
            _cardsCollectedText.text = $"{cardsOwned}/100";

            if (_favoriteCardText != null && playerData.favoriteCardId > 0)
            {
                var cardData = Services.Get<DataManager>().GetCard(playerData.favoriteCardId);
                _favoriteCardText.text = cardData?.cardName ?? "None";
            }

            RefreshBattleLog(playerData);
        }

        private float GetLevelProgress(long experience, int level)
        {
            long currentLevelExp = GetExpForLevel(level);
            long nextLevelExp = GetExpForLevel(level + 1);
            if (nextLevelExp <= currentLevelExp) return 1f;
            return (float)(experience - currentLevelExp) / (nextLevelExp - currentLevelExp);
        }

        private long GetExpForLevel(int level)
        {
            return (long)(level * level * 1000);
        }

        private void RefreshBattleLog(GameManager.PlayerData playerData)
        {
            if (_battleLogContainer == null || _battleLogItemPrefab == null) return;

            foreach (Transform child in _battleLogContainer)
            {
                Destroy(child.gameObject);
            }

            if (playerData.battleLog != null)
            {
                int count = Math.Min(playerData.battleLog.Count, _maxLogItems);
                for (int i = 0; i < count; i++)
                {
                    var logEntry = playerData.battleLog[i];
                    var logGO = Instantiate(_battleLogItemPrefab, _battleLogContainer);
                    var logUI = logGO.GetComponent<BattleLogItemUI>();
                    if (logUI != null)
                    {
                        logUI.Initialize(logEntry);
                    }
                }
            }
        }

        private void OnChangeName()
        {
            var modal = UIManager.Instance?.ShowModal(Resources.Load<GameObject>("UI/ChangeNameModal"));
            var modalUI = modal?.GetComponent<ChangeNameModal>();
            if (modalUI != null)
            {
                modalUI.Initialize(OnNameChanged);
            }
        }

        private void OnNameChanged(string newName)
        {
            var playerData = Services.Get<GameManager>().LocalPlayer;
            if (playerData != null)
            {
                playerData.playerName = newName;
                _playerNameText.text = newName;
                Services.Get<NetworkClient>().Send(new NetworkClient.ChangeNameRequest { newName = newName });
            }
        }
    }

    public class BattleLogItemUI : MonoBehaviour
    {
        [SerializeField] private Text _dateText;
        [SerializeField] private Text _resultText;
        [SerializeField] private Text _crownsText;
        [SerializeField] private Text _trophyChangeText;
        [SerializeField] private Text _modeText;
        [SerializeField] private Button _replayButton;

        public void Initialize(BattleLogEntry entry)
        {
            _dateText.text = entry.timestamp.ToString("MMM dd, HH:mm");
            
            string resultStr = entry.result == BattleStatus.Player1Won ? "VICTORY" : 
                              entry.result == BattleStatus.Player2Won ? "DEFEAT" : "DRAW";
            _resultText.text = resultStr;
            _resultText.color = entry.result == BattleStatus.Player1Won ? Color.green : 
                               entry.result == BattleStatus.Player2Won ? Color.red : Color.yellow;

            _crownsText.text = $"{entry.playerCrowns} - {entry.opponentCrowns}";
            _trophyChangeText.text = entry.trophyChange >= 0 ? $"+{entry.trophyChange}" : entry.trophyChange.ToString();
            _trophyChangeText.color = entry.trophyChange >= 0 ? Color.green : Color.red;
            _modeText.text = entry.battleType.ToString();

            if (_replayButton != null)
            {
                _replayButton.onClick.RemoveAllListeners();
                _replayButton.onClick.AddListener(() => 
                {
                    Services.Get<NetworkClient>().Send(new NetworkClient.ReplayRequest { replayId = entry.replayId });
                });
            }
        }
    }

    [Serializable]
    public class BattleLogEntry
    {
        public DateTime timestamp;
        public BattleStatus result;
        public int playerCrowns;
        public int opponentCrowns;
        public int trophyChange;
        public BattleType battleType;
        public long replayId;
    }
}