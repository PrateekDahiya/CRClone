using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using CRClone.Core;
using CRClone.Network;
using CRClone.UI.Animation;

namespace CRClone.UI.Screens
{
    public class ShopScreen : MonoBehaviour
    {
        [Header("Top Bar")]
        [SerializeField] private Text _gemsText;
        [SerializeField] private Text _goldText;
        [SerializeField] private Button _backButton;

        [Header("Tabs")]
        [SerializeField] private Button _dailyTab;
        [SerializeField] private Button _specialTab;
        [SerializeField] private Button _chestsTab;
        [SerializeField] private Button _gemsTab;
        [SerializeField] private Button _wildCardsTab;

        [Header("Tab Content")]
        [SerializeField] private Transform _dailyContent;
        [SerializeField] private Transform _specialContent;
        [SerializeField] private Transform _chestsContent;
        [SerializeField] private Transform _gemsContent;
        [SerializeField] private Transform _wildCardsContent;

        [Header("Offer Template")]
        [SerializeField] private GameObject _offerItemPrefab;

        [Header("Timers")]
        [SerializeField] private Text _dailyRefreshTimer;
        [SerializeField] private Text _specialOfferTimer;

        private ShopTab _currentTab = ShopTab.Daily;
        private Coroutine _timerCoroutine;
        private Dictionary<ShopTab, List<ShopOffer>> _cachedOffers = new Dictionary<ShopTab, List<ShopOffer>>();

        public enum ShopTab
        {
            Daily,
            Special,
            Chests,
            Gems,
            WildCards
        }

        public void Initialize()
        {
            _backButton?.onClick.AddListener(() => Services.Get<GameManager>().ChangeState(GameState.MainMenu));

            SetupTabs();
            UpdateCurrency();
            ShowTab(ShopTab.Daily);
            StartTimer();
        }

        private void SetupTabs()
        {
            _dailyTab?.onClick.AddListener(() => ShowTab(ShopTab.Daily));
            _specialTab?.onClick.AddListener(() => ShowTab(ShopTab.Special));
            _chestsTab?.onClick.AddListener(() => ShowTab(ShopTab.Chests));
            _gemsTab?.onClick.AddListener(() => ShowTab(ShopTab.Gems));
            _wildCardsTab?.onClick.AddListener(() => ShowTab(ShopTab.WildCards));
        }

        private void ShowTab(ShopTab tab)
        {
            _currentTab = tab;

            _dailyContent?.gameObject.SetActive(tab == ShopTab.Daily);
            _specialContent?.gameObject.SetActive(tab == ShopTab.Special);
            _chestsContent?.gameObject.SetActive(tab == ShopTab.Chests);
            _gemsContent?.gameObject.SetActive(tab == ShopTab.Gems);
            _wildCardsContent?.gameObject.SetActive(tab == ShopTab.WildCards);

            UpdateTabButtons();
            LoadOffersForTab(tab);
        }

        private void UpdateTabButtons()
        {
            SetTabSelected(_dailyTab, _currentTab == ShopTab.Daily);
            SetTabSelected(_specialTab, _currentTab == ShopTab.Special);
            SetTabSelected(_chestsTab, _currentTab == ShopTab.Chests);
            SetTabSelected(_gemsTab, _currentTab == ShopTab.Gems);
            SetTabSelected(_wildCardsTab, _currentTab == ShopTab.WildCards);
        }

        private void SetTabSelected(Button button, bool selected)
        {
            if (button == null) return;
            var colors = button.colors;
            colors.normalColor = selected ? Color.white : new Color(0.7f, 0.7f, 0.7f);
            button.colors = colors;
        }

        private void LoadOffersForTab(ShopTab tab)
        {
            Transform content = GetContentForTab(tab);
            if (content == null) return;

            foreach (Transform child in content)
            {
                Destroy(child.gameObject);
            }

            if (_cachedOffers.TryGetValue(tab, out var offers))
            {
                foreach (var offer in offers)
                {
                    CreateOfferItem(content, offer);
                }
            }
            else
            {
                RequestOffersFromServer(tab);
            }
        }

        private Transform GetContentForTab(ShopTab tab)
        {
            return tab switch
            {
                ShopTab.Daily => _dailyContent,
                ShopTab.Special => _specialContent,
                ShopTab.Chests => _chestsContent,
                ShopTab.Gems => _gemsContent,
                ShopTab.WildCards => _wildCardsContent,
                _ => null
            };
        }

        private void RequestOffersFromServer(ShopTab tab)
        {
        }

        public void SetOffers(ShopTab tab, List<ShopOffer> offers)
        {
            _cachedOffers[tab] = offers;

            if (tab == _currentTab)
            {
                LoadOffersForTab(tab);
            }
        }

        private void CreateOfferItem(Transform parent, ShopOffer offer)
        {
            if (_offerItemPrefab == null) return;

            var offerGO = Instantiate(_offerItemPrefab, parent);
            var offerUI = offerGO.GetComponent<OfferItemUI>();
            if (offerUI != null)
            {
                offerUI.Initialize(offer, OnPurchaseClicked);
            }
        }

        private void OnPurchaseClicked(ShopOffer offer)
        {
            var confirmModal = UIManager.Instance?.ShowModal(Resources.Load<GameObject>("UI/PurchaseConfirmModal"));
            var confirmUI = confirmModal?.GetComponent<PurchaseConfirmModal>();
            if (confirmUI != null)
            {
                confirmUI.Initialize(offer, () => ConfirmPurchase(offer));
            }
        }

        private void ConfirmPurchase(ShopOffer offer)
        {
            Services.Get<NetworkClient>().Send(new NetworkClient.PurchaseRequest
            {
                offerId = offer.offerId,
                currency = offer.currency
            });

            UISoundPlayer.Instance?.PlaySuccess();
        }

        private void UpdateCurrency()
        {
            var playerData = Services.Get<GameManager>().LocalPlayer;
            if (playerData != null)
            {
                if (_gemsText != null) _gemsText.text = playerData.gems.ToString("N0");
                if (_goldText != null) _goldText.text = playerData.gold.ToString("N0");
            }
        }

        private void StartTimer()
        {
            if (_timerCoroutine != null) StopCoroutine(_timerCoroutine);
            _timerCoroutine = StartCoroutine(TimerRoutine());
        }

        private System.Collections.IEnumerator TimerRoutine()
        {
            while (true)
            {
                UpdateTimers();
                yield return new WaitForSeconds(1f);
            }
        }

        private void UpdateTimers()
        {
            if (_dailyRefreshTimer != null)
            {
                TimeSpan dailyRemaining = GetDailyRefreshTime();
                _dailyRefreshTimer.text = FormatTime(dailyRemaining);
            }

            if (_specialOfferTimer != null)
            {
                TimeSpan specialRemaining = GetSpecialOfferTime();
                _specialOfferTimer.text = FormatTime(specialRemaining);
            }
        }

        private TimeSpan GetDailyRefreshTime()
        {
            DateTime now = DateTime.UtcNow;
            DateTime nextRefresh = now.Date.AddDays(1);
            return nextRefresh - now;
        }

        private TimeSpan GetSpecialOfferTime()
        {
            return TimeSpan.FromHours(24);
        }

        private string FormatTime(TimeSpan time)
        {
            if (time.TotalHours >= 1)
                return $"{(int)time.TotalHours}h {time.Minutes}m";
            else
                return $"{time.Minutes}m {time.Seconds}s";
        }

        public void OnPurchaseCompleted(string offerId, List<EventBus.ChestReward> rewards)
        {
            var offer = FindOfferById(offerId);
            if (offer != null)
            {
                offer.purchased = true;
            }

            UpdateCurrency();
            UIManager.Instance?.ShowToast("Purchase successful!");

            var rewardModal = UIManager.Instance?.ShowModal(Resources.Load<GameObject>("UI/RewardModal"));
            var rewardUI = rewardModal?.GetComponent<RewardModal>();
            if (rewardUI != null)
            {
                rewardUI.Initialize(rewards);
            }
        }

        private ShopOffer FindOfferById(string offerId)
        {
            foreach (var offers in _cachedOffers.Values)
            {
                var offer = offers.Find(o => o.offerId == offerId);
                if (offer != null) return offer;
            }
            return null;
        }

        private void OnDestroy()
        {
            if (_timerCoroutine != null) StopCoroutine(_timerCoroutine);
        }
    }

    [Serializable]
    public class ShopOffer
    {
        public string offerId;
        public string title;
        public OfferType type;
        public int cardId;
        public int cardCount;
        public int goldAmount;
        public int gemAmount;
        public int chestTypeId;
        public int cost;
        public CurrencyType currency;
        public bool purchased;
        public TimeSpan timeRemaining;
        public Sprite iconSprite;
    }

    public enum OfferType
    {
        Card,
        Gold,
        Gems,
        Chest,
        WildCard
    }

    public enum CurrencyType
    {
        Gold,
        Gems,
        RealMoney
    }
}