using System;
using UnityEngine;
using UnityEngine.UI;
using CRClone.Core;
using CRClone.UI.Animation;

namespace CRClone.UI.Screens
{
    public class MainMenuScreen : MonoBehaviour
    {
        [Header("Top Bar")]
        [SerializeField] private Image _avatarImage;
        [SerializeField] private Text _playerNameText;
        [SerializeField] private Text _trophiesText;
        [SerializeField] private Text _gemsText;
        [SerializeField] private Text _goldText;
        [SerializeField] private Button _settingsButton;

        [Header("Battle Button")]
        [SerializeField] private Button _battleButton;
        [SerializeField] private Image _battleButtonImage;
        [SerializeField] private float _pulseScale = 1.05f;
        [SerializeField] private float _pulseDuration = 2f;

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
        [SerializeField] private Button _newsActionButton;

        [Header("Bottom Navigation")]
        [SerializeField] private Button _eventsTab;
        [SerializeField] private Button _clanTab;
        [SerializeField] private Button _shopTab;
        [SerializeField] private Button _cardsTab;
        [SerializeField] private Button _battleTab;
        [SerializeField] private Button _profileTab;

        private ChestSlotUI[] _chestSlots;
        private Coroutine _pulseCoroutine;
        private bool _isInitialized;

        public void Initialize()
        {
            if (_isInitialized) return;

            SetupBattleButton();
            SetupChestSlots();
            SetupQuestBar();
            SetupNewsBanner();
            SetupBottomNavigation();
            SetupTopBar();
            UpdatePlayerInfo();

            _isInitialized = true;
        }

        private void SetupBattleButton()
        {
            if (_battleButton != null)
            {
                _battleButton.onClick.AddListener(OnBattleButtonClicked);
                StartPulseAnimation();
            }
        }

        private void StartPulseAnimation()
        {
            if (_pulseCoroutine != null) StopCoroutine(_pulseCoroutine);
            if (AccessibilityManager.Instance?.ReduceMotion != true)
            {
                _pulseCoroutine = StartCoroutine(PulseAnimation());
            }
        }

        private System.Collections.IEnumerator PulseAnimation()
        {
            Vector3 originalScale = _battleButton.transform.localScale;
            Vector3 targetScale = originalScale * _pulseScale;

            while (true)
            {
                yield return _battleButton.transform.ScaleTo(targetScale, _pulseDuration * 0.5f, AnimationCurves.EaseInOutBack);
                yield return _battleButton.transform.ScaleTo(originalScale, _pulseDuration * 0.5f, AnimationCurves.EaseInOutBack);
            }
        }

        private void OnBattleButtonClicked()
        {
            UISoundPlayer.Instance?.PlayButtonClick();
            Services.Get<GameManager>().ChangeState(GameState.Lobby);
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

        private void SetupQuestBar()
        {
        }

        private void SetupNewsBanner()
        {
            if (_newsBanner != null)
            {
                _newsDismissButton?.onClick.AddListener(() => _newsBanner.SetActive(false));
                _newsActionButton?.onClick.AddListener(OnNewsActionClicked);
                _newsBanner.SetActive(false);
            }
        }

        private void OnNewsActionClicked()
        {
        }

        private void SetupBottomNavigation()
        {
            _eventsTab?.onClick.AddListener(() => NavigateTo(ScreenType.QuestLog));
            _clanTab?.onClick.AddListener(() => Services.Get<GameManager>().ChangeState(GameState.Clan));
            _shopTab?.onClick.AddListener(() => Services.Get<GameManager>().ChangeState(GameState.Shop));
            _cardsTab?.onClick.AddListener(() => Services.Get<GameManager>().ChangeState(GameState.DeckBuilder));
            _battleTab?.onClick.AddListener(() => Services.Get<GameManager>().ChangeState(GameState.Lobby));
            _profileTab?.onClick.AddListener(() => Services.Get<GameManager>().ChangeState(GameState.Profile));
        }

        private void SetupTopBar()
        {
            _settingsButton?.onClick.AddListener(() => Services.Get<GameManager>().ChangeState(GameState.Settings));
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

        private void NavigateTo(ScreenType screenType)
        {
            UISoundPlayer.Instance?.PlayButtonClick();
            EventBus.Raise(screenType);
        }

        public void ShowNews(string message, string actionText = null, Action onAction = null)
        {
            if (_newsBanner != null && _newsText != null)
            {
                _newsText.text = message;
                _newsBanner.SetActive(true);

                if (_newsActionButton != null)
                {
                    _newsActionButton.gameObject.SetActive(!string.IsNullOrEmpty(actionText));
                    if (!string.IsNullOrEmpty(actionText))
                    {
                        _newsActionButton.GetComponentInChildren<Text>().text = actionText;
                    }
                }
            }
        }

        public void RefreshChestSlots()
        {
            UpdateChestSlots();
        }

        private void OnEnable()
        {
            if (_isInitialized)
            {
                UpdatePlayerInfo();
                UpdateChestSlots();
                StartPulseAnimation();
            }
        }

        private void OnDisable()
        {
            if (_pulseCoroutine != null)
            {
                StopCoroutine(_pulseCoroutine);
                _pulseCoroutine = null;
            }
        }
    }
}