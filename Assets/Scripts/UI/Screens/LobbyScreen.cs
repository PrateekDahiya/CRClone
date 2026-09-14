using System;
using UnityEngine;
using UnityEngine.UI;
using CRClone.Core;
using CRClone.Network;
using CRClone.UI.Components;

namespace CRClone.UI.Screens
{
    public class LobbyScreen : MonoBehaviour
    {
        [Header("Top Bar")]
        [SerializeField] private Text _playerNameText;
        [SerializeField] private Text _trophyText;
        [SerializeField] private Text _gemText;
        [SerializeField] private Text _goldText;
        [SerializeField] private Button _settingsButton;
        [SerializeField] private Button _backButton;

        [Header("Battle Mode Buttons")]
        [SerializeField] private Button _battle1v1Button;
        [SerializeField] private Button _battle2v2Button;
        [SerializeField] private Button _tournamentButton;
        [SerializeField] private Button _friendlyButton;
        [SerializeField] private Button _practiceButton;

        [Header("Chest Slots")]
        [SerializeField] private Transform _chestSlotsContainer;
        [SerializeField] private GameObject _chestSlotPrefab;
        [SerializeField] private int _chestSlotCount = 4;

        [Header("Deck Builder Shortcut")]
        [SerializeField] private Button _deckBuilderButton;

        [Header("Season Info")]
        [SerializeField] private Text _seasonNameText;
        [SerializeField] private Text _seasonProgressText;
        [SerializeField] private Image _seasonProgressBar;

        private ChestSlotUI[] _chestSlots;
        private bool _isInitialized;

        private void Awake()
        {
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            if (_isInitialized) return;

            _backButton?.onClick.AddListener(() => Services.Get<GameManager>().ChangeState(GameState.MainMenu));
            _settingsButton?.onClick.AddListener(() => Services.Get<GameManager>().ChangeState(GameState.Settings));

            SetupBattleModeButtons();
            SetupDeckBuilderButton();
            SetupChestSlots();

            _isInitialized = true;
        }

        public void Initialize()
        {
            InitializeComponents();
        }

        private void OnEnable()
        {
            UpdatePlayerInfo();
            UpdateChestSlots();
            UpdateSeasonInfo();
        }

        private void SetupBattleModeButtons()
        {
            _battle1v1Button?.onClick.AddListener(() => StartMatchmaking(CRClone.Network.BattleType.Ladder));
            _battle2v2Button?.onClick.AddListener(() => StartMatchmaking(CRClone.Network.BattleType.TwoVTwo));
            _tournamentButton?.onClick.AddListener(() => StartMatchmaking(CRClone.Network.BattleType.Tournament));
            _friendlyButton?.onClick.AddListener(() => StartMatchmaking(CRClone.Network.BattleType.Friendly));
            _practiceButton?.onClick.AddListener(() => StartMatchmaking(CRClone.Network.BattleType.Practice));
        }

        private void SetupDeckBuilderButton()
        {
            _deckBuilderButton?.onClick.AddListener(() => 
            {
                UISoundPlayer.Instance?.PlayButtonClick();
                Services.Get<GameManager>().ChangeState(GameState.DeckBuilder);
            });
        }

        private void SetupChestSlots()
        {
            if (_chestSlotsContainer == null || _chestSlotPrefab == null) return;

            _chestSlots = new ChestSlotUI[_chestSlotCount];
            for (int i = 0; i < _chestSlotCount; i++)
            {
                var slotGO = Instantiate(_chestSlotPrefab, _chestSlotsContainer);
                _chestSlots[i] = slotGO.GetComponent<ChestSlotUI>();
                if (_chestSlots[i] != null)
                {
                    _chestSlots[i].Initialize(i);
                }
            }
        }

        private void StartMatchmaking(CRClone.Network.BattleType type)
        {
            UISoundPlayer.Instance?.PlayButtonClick();
            Services.Get<NetworkClient>().Send(new MatchmakingRequest { battleType = type });
        }

        private void UpdatePlayerInfo()
        {
            var playerData = Services.Get<GameManager>().LocalPlayer;
            if (playerData != null)
            {
                if (_playerNameText != null) _playerNameText.text = playerData.playerName;
                if (_trophyText != null) _trophyText.text = playerData.trophies.ToString("N0");
                if (_gemText != null) _gemText.text = playerData.gems.ToString("N0");
                if (_goldText != null) _goldText.text = playerData.gold.ToString("N0");
            }
        }

        private void UpdateChestSlots()
        {
            var playerData = Services.Get<GameManager>().LocalPlayer;
            if (playerData?.chestSlots != null && _chestSlots != null)
            {
                for (int i = 0; i < _chestSlots.Length && i < playerData.chestSlots.Count; i++)
                {
                    _chestSlots[i].SetChestData(playerData.chestSlots[i]);
                }
            }
        }

        private void UpdateSeasonInfo()
        {
            if (_seasonNameText != null) _seasonNameText.text = "Season 1: Clash Royale Clone";
            if (_seasonProgressText != null) _seasonProgressText.text = "14 days remaining";
            if (_seasonProgressBar != null) _seasonProgressBar.fillAmount = 0.3f;
        }
    }
}