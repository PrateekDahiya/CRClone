using System;
using UnityEngine;
using UnityEngine.UI;
using CRClone.Core;
using CRClone.Network;

namespace CRClone.UI.Screens
{
    public class LobbyScreen : MonoBehaviour
    {
        [Header("Battle Mode Buttons")]
        [SerializeField] private Button _battle1v1Button;
        [SerializeField] private Button _battle2v2Button;
        [SerializeField] private Button _tournamentButton;
        [SerializeField] private Button _friendlyButton;
        [SerializeField] private Button _practiceButton;

        [Header("Deck Builder Shortcut")]
        [SerializeField] private Button _deckBuilderButton;

        [Header("Chest Slots")]
        [SerializeField] private Transform _chestSlotsContainer;
        [SerializeField] private GameObject _chestSlotPrefab;
        [SerializeField] private int _chestSlotCount = 4;

        [Header("Player Info")]
        [SerializeField] private Text _playerNameText;
        [SerializeField] private Text _trophiesText;
        [SerializeField] private Text _gemsText;
        [SerializeField] private Text _goldText;

        [Header("Season Info")]
        [SerializeField] private Text _seasonNameText;
        [SerializeField] private Text _seasonProgressText;
        [SerializeField] private Image _seasonProgressBar;

        private ChestSlotUI[] _chestSlots;
        private bool _isInitialized;

        public void Initialize()
        {
            if (_isInitialized) return;

            SetupBattleModeButtons();
            SetupDeckBuilderButton();
            SetupChestSlots();
            UpdatePlayerInfo();
            UpdateSeasonInfo();

            _isInitialized = true;
        }

        private void SetupBattleModeButtons()
        {
            _battle1v1Button?.onClick.AddListener(() => StartMatchmaking(BattleType.Ladder));
            _battle2v2Button?.onClick.AddListener(() => StartMatchmaking(BattleType.TwoVTwo));
            _tournamentButton?.onClick.AddListener(() => StartMatchmaking(BattleType.Tournament));
            _friendlyButton?.onClick.AddListener(() => StartMatchmaking(BattleType.Friendly));
            _practiceButton?.onClick.AddListener(() => StartMatchmaking(BattleType.Practice));
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

            UpdateChestSlots();
        }

        private void StartMatchmaking(BattleType type)
        {
            UISoundPlayer.Instance?.PlayButtonClick();
            Services.Get<NetworkClient>().Send(new NetworkClient.MatchmakingRequest { battleType = type });
            Services.Get<GameManager>().ChangeState(GameState.Matchmaking);
        }

        private void UpdatePlayerInfo()
        {
            var playerData = Services.Get<GameManager>()?.LocalPlayer;
            if (playerData != null)
            {
                if (_playerNameText != null) _playerNameText.text = playerData.playerName;
                if (_trophiesText != null) _trophiesText.text = playerData.trophies.ToString("N0");
                if (_gemsText != null) _gemsText.text = playerData.gems.ToString("N0");
                if (_goldText != null) _goldText.text = playerData.gold.ToString("N0");
            }
        }

        private void UpdateSeasonInfo()
        {
        }

        private void UpdateChestSlots()
        {
            var playerData = Services.Get<GameManager>()?.LocalPlayer;
            if (playerData?.chestSlots != null && _chestSlots != null)
            {
                for (int i = 0; i < _chestSlots.Length && i < playerData.chestSlots.Count; i++)
                {
                    _chestSlots[i].SetChestData(playerData.chestSlots[i]);
                }
            }
        }

        private void OnEnable()
        {
            if (_isInitialized)
            {
                UpdatePlayerInfo();
                UpdateChestSlots();
            }
        }
    }
}