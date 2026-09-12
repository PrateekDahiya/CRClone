using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using CRClone.Core;
using CRClone.Network;
using CRClone.UI.Animation;

namespace CRClone.UI.Screens
{
    public class MainMenuScreen : MonoBehaviour
    {
        [Header("Top Bar")]
        [SerializeField] private Image _avatarImage;
        [SerializeField] private Text _playerNameText;
        [SerializeField] private Text _trophyText;
        [SerializeField] private Text _gemText;
        [SerializeField] private Text _goldText;
        [SerializeField] private Button _settingsButton;

        [Header("Battle Button")]
        [SerializeField] private Button _battleButton;
        [SerializeField] private RectTransform _battleButtonRect;
        [SerializeField] private float _pulseScale = 1.05f;
        [SerializeField] private float _pulseDuration = 1.5f;

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

        [Header("Quest Bar")]
        [SerializeField] private Transform _questBarContainer;
        [SerializeField] private GameObject _questItemPrefab;

        [Header("News Banner")]
        [SerializeField] private GameObject _newsBanner;
        [SerializeField] private Text _newsText;
        [SerializeField] private Button _newsDismissButton;

        [Header("Bottom Navigation")]
        [SerializeField] private Button _eventsNavButton;
        [SerializeField] private Button _clanNavButton;
        [SerializeField] private Button _shopNavButton;
        [SerializeField] private Button _cardsNavButton;
        [SerializeField] private Button _battleNavButton;
        [SerializeField] private Button _profileNavButton;
        [SerializeField] private Image _battleNavHighlight;

        private ChestSlotUI[] _chestSlots;
        private Coroutine _pulseCoroutine;
        private bool _isInitialized;

        private void Awake()
        {
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            if (_isInitialized) return;

            SetupBattleButton();
            SetupBattleModeButtons();
            SetupChestSlots();
            SetupQuestBar();
            SetupNewsBanner();
            SetupBottomNavigation();
            SetupTopBar();

            _isInitialized = true;
        }

        private void OnEnable()
        {
            UpdatePlayerInfo();
            UpdateChestSlots();
            StartBattleButtonPulse();
        }

        private void OnDisable()
        {
            StopBattleButtonPulse();
        }

        private void SetupTopBar()
        {
            _settingsButton?.onClick.AddListener(() => Services.Get<GameManager>().ChangeState(GameState.Settings));
        }

        private void SetupBattleButton()
        {
            _battleButton?.onClick.AddListener(OnBattleButtonClicked);
        }

        private void SetupBattleModeButtons()
        {
            _battle1v1Button?.onClick.AddListener(() => StartMatchmaking(BattleType.Ladder));
            _battle2v2Button?.onClick.AddListener(() => StartMatchmaking(BattleType.TwoVTwo));
            _tournamentButton?.onClick.AddListener(() => StartMatchmaking(BattleType.Tournament));
            _friendlyButton?.onClick.AddListener(() => StartMatchmaking(BattleType.Friendly));
            _practiceButton?.onClick.AddListener(() => StartMatchmaking(BattleType.Practice));
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

        private void SetupQuestBar()
        {
        }

        private void SetupNewsBanner()
        {
            if (_newsBanner != null)
            {
                _newsDismissButton?.onClick.AddListener(() => _newsBanner.SetActive(false));
                _newsBanner.SetActive(false);
            }
        }

        private void SetupBottomNavigation()
        {
            _eventsNavButton?.onClick.AddListener(() => NavigateTo(ScreenType.QuestLog));
            _clanNavButton?.onClick.AddListener(() => Services.Get<GameManager>().ChangeState(GameState.Clan));
            _shopNavButton?.onClick.AddListener(() => Services.Get<GameManager>().ChangeState(GameState.Shop));
            _cardsNavButton?.onClick.AddListener(() => Services.Get<GameManager>().ChangeState(GameState.DeckBuilder));
            _battleNavButton?.onClick.AddListener(() => Services.Get<GameManager>().ChangeState(GameState.Lobby));
            _profileNavButton?.onClick.AddListener(() => Services.Get<GameManager>().ChangeState(GameState.Profile));

            UpdateNavHighlight(ScreenType.Lobby);
        }

        public void UpdateNavHighlight(ScreenType currentScreen)
        {
            bool isLobby = currentScreen == ScreenType.Lobby;
            if (_battleNavHighlight != null) _battleNavHighlight.gameObject.SetActive(isLobby);
        }

        private void OnBattleButtonClicked()
        {
            UISoundPlayer.Instance?.PlayButtonClick();
            Services.Get<GameManager>().ChangeState(GameState.Lobby);
        }

        private void StartMatchmaking(BattleType type)
        {
            UISoundPlayer.Instance?.PlayButtonClick();
            Services.Get<NetworkClient>().Send(new NetworkClient.MatchmakingRequest { battleType = type });
        }

        private void NavigateTo(ScreenType screenType)
        {
            UISoundPlayer.Instance?.PlayButtonClick();
            EventBus.Raise(screenType);
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

        private void StartBattleButtonPulse()
        {
            if (_pulseCoroutine != null) StopCoroutine(_pulseCoroutine);
            if (AccessibilityManager.Instance?.ReduceMotion != true && _battleButtonRect != null)
            {
                _pulseCoroutine = StartCoroutine(PulseAnimation());
            }
        }

        private void StopBattleButtonPulse()
        {
            if (_pulseCoroutine != null)
            {
                StopCoroutine(_pulseCoroutine);
                _pulseCoroutine = null;
            }
            if (_battleButtonRect != null)
            {
                _battleButtonRect.localScale = Vector3.one;
            }
        }

        private IEnumerator PulseAnimation()
        {
            Vector3 originalScale = Vector3.one;
            Vector3 targetScale = originalScale * _pulseScale;

            while (true)
            {
                yield return _battleButtonRect.ScaleTo(targetScale, _pulseDuration / 2f, AnimationCurve.EaseInOut(0, 0, 1, 1), null, true);
                yield return _battleButtonRect.ScaleTo(originalScale, _pulseDuration / 2f, AnimationCurve.EaseInOut(0, 0, 1, 1), null, true);
            }
        }

        public void ShowNews(string message)
        {
            if (_newsBanner != null && _newsText != null)
            {
                _newsText.text = message;
                _newsBanner.SetActive(true);
            }
        }

        public void RefreshChestSlots()
        {
            UpdateChestSlots();
        }
    }
}