using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using CRClone.Core;
using CRClone.UI.Animation;

namespace CRClone.UI.Screens
{
    public class ProfileScreen : MonoBehaviour
    {
        [Header("Player Info")]
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

        [Header("Battle Log")]
        [SerializeField] private Transform _battleLogContainer;
        [SerializeField] private GameObject _battleLogItemPrefab;
        [SerializeField] private int _maxLogItems = 10;

        [Header("Actions")]
        [SerializeField] private Button _changeNameButton;
        [SerializeField] private Button _settingsButton;
        [SerializeField] private Button _backButton;

        private List<BattleLogEntry> _battleLog = new List<BattleLogEntry>();

        public void Initialize()
        {
            _backButton?.onClick.AddListener(() => Services.Get<GameManager>().ChangeState(GameState.MainMenu));
            _settingsButton?.onClick.AddListener(() => Services.Get<GameManager>().ChangeState(GameState.Settings));
            _changeNameButton?.onClick.AddListener(OnChangeName);

            LoadPlayerData();
            LoadBattleLog();
        }

        private void LoadPlayerData()
        {
            var playerData = Services.Get<GameManager>().LocalPlayer;
            if (playerData == null) return;

            if (_playerNameText != null) _playerNameText.text = playerData.playerName;
            if (_playerTagText != null) _playerTagText.text = $"#{playerData.playerTag}";
            if (_trophiesText != null) _trophiesText.text = playerData.trophies.ToString("N0");
            if (_bestTrophiesText != null) _bestTrophiesText.text = playerData.bestTrophies.ToString("N0");
            if (_levelText != null) _levelText.text = $"Level {playerData.level}";

            if (_levelProgressFill != null)
            {
                float progress = GetLevelProgress(playerData.experience, playerData.level);
                _levelProgressFill.fillAmount = progress;
            }

            if (_winRateText != null)
            {
                float winRate = playerData.totalBattles > 0 ? (float)playerData.wins / playerData.totalBattles * 100f : 0f;
                _winRateText.text = $"{winRate:F1}%";
            }

            if (_totalBattlesText != null) _totalBattlesText.text = playerData.totalBattles.ToString("N0");
            if (_threeCrownWinsText != null) _threeCrownWinsText.text = playerData.threeCrownWins.ToString("N0");
        }

        private float GetLevelProgress(long exp, int level)
        {
            long currentLevelExp = GetExpForLevel(level);
            long nextLevelExp = GetExpForLevel(level + 1);
            return nextLevelExp > currentLevelExp ? (float)(exp - currentLevelExp) / (nextLevelExp - currentLevelExp) : 1f;
        }

        private long GetExpForLevel(int level)
        {
            return (long)(Mathf.Pow(level, 2) * 1000);
        }

        private void LoadBattleLog()
        {
            var playerData = Services.Get<GameManager>().LocalPlayer;
            if (playerData?.battleLog != null)
            {
                _battleLog = playerData.battleLog;
            }

            RefreshBattleLog();
        }

        private void RefreshBattleLog()
        {
            if (_battleLogContainer == null || _battleLogItemPrefab == null) return;

            foreach (Transform child in _battleLogContainer)
            {
                Destroy(child.gameObject);
            }

            int count = Math.Min(_battleLog.Count, _maxLogItems);
            for (int i = 0; i < count; i++)
            {
                var logGO = Instantiate(_battleLogItemPrefab, _battleLogContainer);
                var logUI = logGO.GetComponent<BattleLogItemUI>();
                if (logUI != null)
                {
                    logUI.Initialize(_battleLog[i]);
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
                if (_playerNameText != null) _playerNameText.text = newName;
                Services.Get<NetworkClient>().Send(new NetworkClient.ChangeNameRequest { newName = newName });
            }
        }
    }

    [Serializable]
    public class BattleLogEntry
    {
        public DateTime timestamp;
        public BattleResult result;
        public int playerCrowns;
        public int opponentCrowns;
        public int trophyChange;
        public string opponentName;
        public string battleMode;
        public long replayId;
    }

    public class BattleLogItemUI : MonoBehaviour
    {
        [SerializeField] private Text _dateText;
        [SerializeField] private Text _resultText;
        [SerializeField] private Text _crownsText;
        [SerializeField] private Text _trophyChangeText;
        [SerializeField] private Text _opponentText;
        [SerializeField] private Text _modeText;
        [SerializeField] private Button _replayButton;

        public void Initialize(BattleLogEntry entry)
        {
            if (_dateText != null) _dateText.text = entry.timestamp.ToLocalTime().ToString("MMM dd, HH:mm");

            if (_resultText != null)
            {
                _resultText.text = entry.result.ToString();
                _resultText.color = entry.result == BattleResult.Victory ? Color.green : 
                                   entry.result == BattleResult.Defeat ? Color.red : Color.yellow;
            }

            if (_crownsText != null) _crownsText.text = $"{entry.playerCrowns}-{entry.opponentCrowns}";
            if (_trophyChangeText != null)
            {
                _trophyChangeText.text = entry.trophyChange >= 0 ? $"+{entry.trophyChange}" : entry.trophyChange.ToString();
                _trophyChangeText.color = entry.trophyChange >= 0 ? Color.green : Color.red;
            }

            if (_opponentText != null) _opponentText.text = $"vs {entry.opponentName}";
            if (_modeText != null) _modeText.text = entry.battleMode;

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
}