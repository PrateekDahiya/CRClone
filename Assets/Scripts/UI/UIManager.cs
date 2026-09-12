using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using CRClone.Core;

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

        private GameObject _currentScreen;
        private ScreenType _currentScreenType = ScreenType.MainMenu;

        public void Initialize()
        {
            EventBus.OnScreenChanged += ShowScreen;
            EventBus.OnToast += ShowToast;
            EventBus.OnLoadingProgress += UpdateLoadingProgress;

            // Hide all screens initially
            HideAllScreens();
        }

        public void ShowScreen(ScreenType screenType)
        {
            if (_currentScreen != null)
            {
                _currentScreen.SetActive(false);
            }

            GameObject screenPrefab = GetScreenPrefab(screenType);
            if (screenPrefab != null)
            {
                _currentScreen = Instantiate(screenPrefab, transform);
                _currentScreenType = screenType;

                // Initialize screen-specific logic
                InitializeScreen(screenType, _currentScreen);
            }
        }

        private GameObject GetScreenPrefab(ScreenType type)
        {
            return type switch
            {
                ScreenType.MainMenu => _mainMenuScreen,
                ScreenType.Lobby => _lobbyScreen,
                ScreenType.DeckBuilder => _deckBuilderScreen,
                ScreenType.Battle => null, // Battle scene loaded separately
                ScreenType.BattleResult => _battleResultScreen,
                ScreenType.Shop => _shopScreen,
                ScreenType.Clan => _clanScreen,
                ScreenType.Profile => _profileScreen,
                ScreenType.Settings => _settingsScreen,
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

            var deckBuilderBtn = screen.transform.Find("DeckBuilderButton")?.GetComponent<Button>();
            deckBuilderBtn?.onClick.AddListener(() => Services.Get<GameManager>().ChangeState(GameState.DeckBuilder));
        }

        private void StartMatchmaking(BattleType type)
        {
            Services.Get<NetworkClient>().Send(new NetworkClient.MatchmakingRequest { battleType = type });
        }

        private void InitializeDeckBuilder(GameObject screen)
        {
            var deckBuilder = screen.GetComponent<DeckBuilderUI>();
            if (deckBuilder == null) deckBuilder = screen.AddComponent<DeckBuilderUI>();
            deckBuilder.Initialize();
        }

        private void InitializeShop(GameObject screen) { }
        private void InitializeClan(GameObject screen) { }
        private void InitializeProfile(GameObject screen) { }

        private void InitializeBattleResult(GameObject screen)
        {
            // Filled in by BattleResultUI component
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
            
            var resultUI = _currentScreen.GetComponent<BattleResultUI>();
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
            _loadingOverlay?.SetActive(show);
        }

        public void UpdateLoadingProgress(float progress)
        {
            var loadingUI = _loadingOverlay?.GetComponentInChildren<LoadingUI>();
            loadingUI?.SetProgress(progress);
        }
    }

    public class ToastUI : MonoBehaviour
    {
        [SerializeField] private Text _messageText;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private float _fadeDuration = 0.3f;

        public void Show(string message)
        {
            _messageText.text = message;
            StartCoroutine(FadeIn());
        }

        private System.Collections.IEnumerator FadeIn()
        {
            float elapsed = 0f;
            while (elapsed < _fadeDuration)
            {
                elapsed += Time.deltaTime;
                _canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / _fadeDuration);
                yield return null;
            }
            _canvasGroup.alpha = 1f;
        }
    }

    public class LoadingUI : MonoBehaviour
    {
        [SerializeField] private Image _progressBar;
        [SerializeField] private Text _progressText;

        public void SetProgress(float progress)
        {
            if (_progressBar != null) _progressBar.fillAmount = progress;
            if (_progressText != null) _progressText.text = $"{progress * 100:0}%";
        }
    }
}