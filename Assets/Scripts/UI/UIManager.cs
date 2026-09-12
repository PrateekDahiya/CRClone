using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using CRClone.Core;
using CRClone.UI.Animation;

namespace CRClone.UI
{
    public class UIManager : MonoBehaviour
    {
        [Header("Screen Prefabs")]
        [SerializeField] private GameObject _mainMenuScreen;
        [SerializeField] private GameObject _lobbyScreen;
        [SerializeField] private GameObject _deckBuilderScreen;
        [SerializeField] private GameObject _shopScreen;
        [SerializeField] private GameObject _clanScreen;
        [SerializeField] private GameObject _profileScreen;
        [SerializeField] private GameObject _settingsScreen;
        [SerializeField] private GameObject _battleResultScreen;
        [SerializeField] private GameObject _chestUnlockScreen;

        [Header("Common UI")]
        [SerializeField] private GameObject _toastPrefab;
        [SerializeField] private Transform _toastContainer;
        [SerializeField] private GameObject _loadingOverlay;

        [Header("Transition Settings")]
        [SerializeField] private TransitionType _screenTransitionType = TransitionType.SlideHorizontal;
        [SerializeField] private TransitionType _modalTransitionType = TransitionType.ScaleFade;

        [Header("System References")]
        [SerializeField] private ResponsiveLayout _responsiveLayout;
        [SerializeField] private AccessibilityManager _accessibilityManager;
        [SerializeField] private LocalizationManager _localizationManager;

        private GameObject _currentScreen;
        private ScreenType _currentScreenType = ScreenType.MainMenu;
        private ScreenTransition _screenTransition;
        private bool _isTransitioning;

        public static UIManager Instance { get; private set; }

        public ResponsiveLayout ResponsiveLayout => _responsiveLayout;
        public AccessibilityManager Accessibility => _accessibilityManager;
        public LocalizationManager Localization => _localizationManager;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeSystems();
        }

        private void InitializeSystems()
        {
            _screenTransition = GetComponent<ScreenTransition>();
            if (_screenTransition == null) _screenTransition = gameObject.AddComponent<ScreenTransition>();

            if (_responsiveLayout == null) _responsiveLayout = FindObjectOfType<ResponsiveLayout>();
            if (_accessibilityManager == null) _accessibilityManager = FindObjectOfType<AccessibilityManager>();
            if (_localizationManager == null) _localizationManager = FindObjectOfType<LocalizationManager>();

            EventBus.OnScreenChanged += ShowScreen;
            EventBus.OnToast += ShowToast;
            EventBus.OnLoadingProgress += UpdateLoadingProgress;
            EventBus.OnGameStateChanged += OnGameStateChanged;

            HideAllScreens();
        }

        private void OnGameStateChanged(GameState newState)
        {
            ScreenType screenType = GameStateToScreenType(newState);
            if (screenType != ScreenType.MainMenu)
            {
                ShowScreen(screenType);
            }
        }

        private ScreenType GameStateToScreenType(GameState state)
        {
            return state switch
            {
                GameState.MainMenu => ScreenType.MainMenu,
                GameState.Lobby => ScreenType.Lobby,
                GameState.DeckBuilder => ScreenType.DeckBuilder,
                GameState.Shop => ScreenType.Shop,
                GameState.Clan => ScreenType.Clan,
                GameState.Profile => ScreenType.Profile,
                GameState.Settings => ScreenType.Settings,
                GameState.BattleResult => ScreenType.BattleResult,
                _ => ScreenType.MainMenu
            };
        }

        public void ShowScreen(ScreenType screenType)
        {
            if (_isTransitioning) return;

            StartCoroutine(ShowScreenRoutine(screenType));
        }

        private IEnumerator ShowScreenRoutine(ScreenType screenType)
        {
            _isTransitioning = true;

            if (_currentScreen != null)
            {
                var oldTransition = _currentScreen.GetComponent<ScreenTransition>();
                if (oldTransition == null) oldTransition = _currentScreen.AddComponent<ScreenTransition>();

                yield return oldTransition.TransitionOut(_screenTransitionType);
                _currentScreen.SetActive(false);
            }

            GameObject screenPrefab = GetScreenPrefab(screenType);
            if (screenPrefab != null)
            {
                _currentScreen = Instantiate(screenPrefab, transform);
                _currentScreenType = screenType;

                var newTransition = _currentScreen.GetComponent<ScreenTransition>();
                if (newTransition == null) newTransition = _currentScreen.AddComponent<ScreenTransition>();

                newTransition.ResetTransform();
                _currentScreen.SetActive(true);

                yield return newTransition.TransitionIn(_screenTransitionType);

                InitializeScreen(screenType, _currentScreen);
            }

            _isTransitioning = false;
        }

        public void ShowModal(GameObject modalPrefab, Action onClosed = null)
        {
            if (modalPrefab == null) return;

            StartCoroutine(ShowModalRoutine(modalPrefab, onClosed));
        }

        private IEnumerator ShowModalRoutine(GameObject modalPrefab, Action onClosed)
        {
            GameObject modal = Instantiate(modalPrefab, transform);
            modal.SetActive(true);

            var transition = modal.GetComponent<ScreenTransition>();
            if (transition == null) transition = modal.AddComponent<ScreenTransition>();

            transition.ResetTransform();
            yield return transition.TransitionIn(_modalTransitionType);

            var modalController = modal.GetComponent<ModalController>();
            if (modalController != null)
            {
                yield return new WaitUntil(() => modalController.IsClosed);
            }

            yield return transition.TransitionOut(_modalTransitionType);
            Destroy(modal);
            onClosed?.Invoke();
        }

        private GameObject GetScreenPrefab(ScreenType type)
        {
            return type switch
            {
                ScreenType.MainMenu => _mainMenuScreen,
                ScreenType.Lobby => _lobbyScreen,
                ScreenType.DeckBuilder => _deckBuilderScreen,
                ScreenType.Battle => null,
                ScreenType.BattleResult => _battleResultScreen,
                ScreenType.Shop => _shopScreen,
                ScreenType.Clan => _clanScreen,
                ScreenType.Profile => _profileScreen,
                ScreenType.Settings => _settingsScreen,
                ScreenType.ChestUnlock => _chestUnlockScreen,
                ScreenType.QuestLog => null,
                ScreenType.Tournament => null,
                _ => null
            };
        }

        private void InitializeScreen(ScreenType type, GameObject screen)
        {
            switch (type)
            {
                case ScreenType.MainMenu:
                    InitializeMainMenu(screen);
                    break;
                case ScreenType.Lobby:
                    InitializeLobby(screen);
                    break;
                case ScreenType.DeckBuilder:
                    InitializeDeckBuilder(screen);
                    break;
                case ScreenType.Shop:
                    InitializeShop(screen);
                    break;
                case ScreenType.Clan:
                    InitializeClan(screen);
                    break;
                case ScreenType.Profile:
                    InitializeProfile(screen);
                    break;
                case ScreenType.Settings:
                    InitializeSettings(screen);
                    break;
                case ScreenType.BattleResult:
                    InitializeBattleResult(screen);
                    break;
            }
        }

        private void InitializeMainMenu(GameObject screen)
        {
            var playButton = screen.transform.Find("PlayButton")?.GetComponent<Button>();
            playButton?.onClick.AddListener(() => Services.Get<GameManager>().ChangeState(GameState.Lobby));

            var deckButton = screen.transform.Find("DeckButton")?.GetComponent<Button>();
            deckButton?.onClick.AddListener(() => Services.Get<GameManager>().ChangeState(GameState.DeckBuilder));

            var shopButton = screen.transform.Find("ShopButton")?.GetComponent<Button>();
            shopButton?.onClick.AddListener(() => Services.Get<GameManager>().ChangeState(GameState.Shop));

            var clanButton = screen.transform.Find("ClanButton")?.GetComponent<Button>();
            clanButton?.onClick.AddListener(() => Services.Get<GameManager>().ChangeState(GameState.Clan));

            var profileButton = screen.transform.Find("ProfileButton")?.GetComponent<Button>();
            profileButton?.onClick.AddListener(() => Services.Get<GameManager>().ChangeState(GameState.Profile));

            var settingsButton = screen.transform.Find("SettingsButton")?.GetComponent<Button>();
            settingsButton?.onClick.AddListener(() => Services.Get<GameManager>().ChangeState(GameState.Settings));

            var mainMenuScreen = screen.GetComponent<MainMenuScreen>();
            mainMenuScreen?.Initialize();
        }

        private void InitializeLobby(GameObject screen)
        {
            var battle1v1 = screen.transform.Find("Battle1v1")?.GetComponent<Button>();
            battle1v1?.onClick.AddListener(() => StartMatchmaking(BattleType.Ladder));

            var battle2v2 = screen.transform.Find("Battle2v2")?.GetComponent<Button>();
            battle2v2?.onClick.AddListener(() => StartMatchmaking(BattleType.TwoVTwo));

            var tournament = screen.transform.Find("Tournament")?.GetComponent<Button>();
            tournament?.onClick.AddListener(() => StartMatchmaking(BattleType.Tournament));

            var friendly = screen.transform.Find("Friendly")?.GetComponent<Button>();
            friendly?.onClick.AddListener(() => StartMatchmaking(BattleType.Friendly));

            var practice = screen.transform.Find("Practice")?.GetComponent<Button>();
            practice?.onClick.AddListener(() => StartMatchmaking(BattleType.Practice));

            var deckBuilderBtn = screen.transform.Find("DeckBuilderButton")?.GetComponent<Button>();
            deckBuilderBtn?.onClick.AddListener(() => Services.Get<GameManager>().ChangeState(GameState.DeckBuilder));

            var lobbyScreen = screen.GetComponent<LobbyScreen>();
            lobbyScreen?.Initialize();
        }

        private void StartMatchmaking(BattleType type)
        {
            Services.Get<NetworkClient>().Send(new NetworkClient.MatchmakingRequest { battleType = type });
            Services.Get<GameManager>().ChangeState(GameState.Matchmaking);
        }

        private void InitializeDeckBuilder(GameObject screen)
        {
            var deckBuilder = screen.GetComponent<DeckBuilderUI>();
            if (deckBuilder == null) deckBuilder = screen.AddComponent<DeckBuilderUI>();
            deckBuilder.Initialize();
        }

        private void InitializeShop(GameObject screen)
        {
            var shopScreen = screen.GetComponent<ShopScreen>();
            shopScreen?.Initialize();
        }

        private void InitializeClan(GameObject screen)
        {
            var clanScreen = screen.GetComponent<ClanScreen>();
            clanScreen?.Initialize();
        }

        private void InitializeProfile(GameObject screen)
        {
            var profileScreen = screen.GetComponent<ProfileScreen>();
            profileScreen?.Initialize();
        }

        private void InitializeSettings(GameObject screen)
        {
            var settingsScreen = screen.GetComponent<SettingsScreen>();
            settingsScreen?.Initialize();
        }

        private void InitializeBattleResult(GameObject screen)
        {
        }

        private void HideAllScreens()
        {
            _mainMenuScreen?.SetActive(false);
            _lobbyScreen?.SetActive(false);
            _deckBuilderScreen?.SetActive(false);
            _shopScreen?.SetActive(false);
            _clanScreen?.SetActive(false);
            _profileScreen?.SetActive(false);
            _settingsScreen?.SetActive(false);
            _battleResultScreen?.SetActive(false);
            _chestUnlockScreen?.SetActive(false);
        }

        public void ShowBattleResult(EventBus.BattleEndedEvent evt)
        {
            ShowScreen(ScreenType.BattleResult);

            var resultUI = _currentScreen.GetComponent<BattleResultScreen>();
            if (resultUI != null)
            {
                resultUI.DisplayResult(evt);
            }
        }

        public void ShowToast(string message, object data = null)
        {
            if (_toastPrefab != null && _toastContainer != null)
            {
                var toast = Instantiate(_toastPrefab, _toastContainer);
                var toastUI = toast.GetComponent<ToastUI>();
                if (toastUI != null)
                {
                    toastUI.Show(message);
                }
                Destroy(toast, 3f);
            }
        }

        public void ShowLoading(bool show)
        {
            if (_loadingOverlay != null)
            {
                _loadingOverlay.SetActive(show);
                if (show)
                {
                    var loadingUI = _loadingOverlay.GetComponentInChildren<LoadingUI>();
                    loadingUI?.SetProgress(0f);
                }
            }
        }

        public void UpdateLoadingProgress(float progress)
        {
            var loadingUI = _loadingOverlay?.GetComponentInChildren<LoadingUI>();
            loadingUI?.SetProgress(progress);
        }

        public void ShowChestUnlock(int chestTypeId, System.Collections.Generic.List<EventBus.ChestReward> rewards)
        {
            ShowScreen(ScreenType.ChestUnlock);

            var chestUI = _currentScreen.GetComponent<ChestUnlockScreen>();
            if (chestUI != null)
            {
                chestUI.DisplayChest(chestTypeId, rewards);
            }
        }
    }

    public class ModalController : MonoBehaviour
    {
        public bool IsClosed { get; private set; }

        public void Close()
        {
            IsClosed = true;
        }
    }

    public class ToastUI : MonoBehaviour
    {
        [SerializeField] private Text _messageText;
        [SerializeField] private TMPro.TextMeshProUGUI _tmpText;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private float _fadeDuration = 0.3f;
        [SerializeField] private float _displayDuration = 3f;

        private void Awake()
        {
            if (_canvasGroup == null) _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null) _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        public void Show(string message)
        {
            if (_messageText != null) _messageText.text = message;
            if (_tmpText != null) _tmpText.text = message;

            StopAllCoroutines();
            StartCoroutine(ShowRoutine());
        }

        private IEnumerator ShowRoutine()
        {
            if (!AccessibilityManager.Instance?.ReduceMotion == true)
            {
                yield return _canvasGroup.FadeTo(1f, _fadeDuration);
            }
            else
            {
                _canvasGroup.alpha = 1f;
            }

            yield return new WaitForSecondsRealtime(_displayDuration);

            if (!AccessibilityManager.Instance?.ReduceMotion == true)
            {
                yield return _canvasGroup.FadeTo(0f, _fadeDuration);
            }
            else
            {
                _canvasGroup.alpha = 0f;
            }

            Destroy(gameObject);
        }
    }

    public class LoadingUI : MonoBehaviour
    {
        [SerializeField] private Image _progressBar;
        [SerializeField] private Text _progressText;
        [SerializeField] private TMPro.TextMeshProUGUI _tmpProgressText;
        [SerializeField] private GameObject _spinner;

        public void SetProgress(float progress)
        {
            if (_progressBar != null) _progressBar.fillAmount = progress;

            string progressText = $"{progress * 100:0}%";
            if (_progressText != null) _progressText.text = progressText;
            if (_tmpProgressText != null) _tmpProgressText.text = progressText;
        }

        private void Update()
        {
            if (_spinner != null && _spinner.activeInHierarchy)
            {
                _spinner.transform.Rotate(0, 0, -360f * Time.deltaTime);
            }
        }
    }
}