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
        [SerializeField] private Text _gemText;
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

        [Header("Offer Prefabs")]
        [SerializeField] private GameObject _offerItemPrefab;
        [SerializeField] private GameObject _largeOfferPrefab;

        [Header("Timers")]
        [SerializeField] private Text _dailyRefreshTimer;
        [SerializeField] private Text _specialOfferTimer;

        private ShopTab _currentTab = ShopTab.Daily;
        private float _dailyRefreshTime = 86400f; // 24 hours
        private float _specialOfferTime = 3600f; // 1 hour
        private Coroutine _timerCoroutine;

        public enum ShopTab
        {
            Daily,
            Special,
            Chests,
            Gems,
            WildCards
        }

        private void Awake()
        {
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            _backButton?.onClick.AddListener(() => Services.Get<GameManager>().ChangeState(GameState.MainMenu));

            _dailyTab?.onClick.AddListener(() => SwitchTab(ShopTab.Daily));
            _specialTab?.onClick.AddListener(() => SwitchTab(ShopTab.Special));
            _chestsTab?.onClick.AddListener(() => SwitchTab(ShopTab.Chests));
            _gemsTab?.onClick.AddListener(() => SwitchTab(ShopTab.Gems));
            _wildCardsTab?.onClick.AddListener(() => SwitchTab(ShopTab.WildCards));

            LoadOffers();
            UpdateCurrency();
        }

        private void OnEnable()
        {
            UpdateCurrency();
            StartTimers();
        }

        private void OnDisable()
        {
            StopTimers();
        }

        private void SwitchTab(ShopTab tab)
        {
            _currentTab = tab;
            UpdateTabVisuals();
            ShowTabContent(tab);
        }

        private void UpdateTabVisuals()
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

        private void ShowTabContent(ShopTab tab)
        {
            _dailyContent?.gameObject.SetActive(tab == ShopTab.Daily);
            _specialContent?.gameObject.SetActive(tab == ShopTab.Special);
            _chestsContent?.gameObject.SetActive(tab == ShopTab.Chests);
            _gemsContent?.gameObject.SetActive(tab == ShopTab.Gems);
            _wildCardsContent?.gameObject.SetActive(tab == ShopTab.WildCards);
        }

        private void LoadOffers()
        {
            LoadDailyOffers();
            LoadSpecialOffers();
            LoadChestOffers();
            LoadGemOffers();
            LoadWildCardOffers();
        }

        private void LoadDailyOffers()
        {
            if (_dailyContent == null || _offerItemPrefab == null) return;

            ClearContent(_dailyContent);

            var offers = GenerateDailyOffers();
            foreach (var offer in offers)
            {
                CreateOfferItem(_dailyContent, offer);
            }
        }

        private void LoadSpecialOffers()
        {
            if (_specialContent == null || _largeOfferPrefab == null) return;

            ClearContent(_specialContent);

            var offers = GenerateSpecialOffers();
            foreach (var offer in offers)
            {
                CreateLargeOffer(_specialContent, offer);
            }
        }

        private void LoadChestOffers()
        {
            if (_chestsContent == null || _offerItemPrefab == null) return;

            ClearContent(_chestsContent);

            var offers = GenerateChestOffers();
            foreach (var offer in offers)
            {
                CreateOfferItem(_chestsContent, offer);
            }
        }

        private void LoadGemOffers()
        {
            if (_gemsContent == null || _offerItemPrefab == null) return;

            ClearContent(_gemsContent);

            var offers = GenerateGemOffers();
            foreach (var offer in offers)
            {
                CreateOfferItem(_gemsContent, offer);
            }
        }

        private void LoadWildCardOffers()
        {
            if (_wildCardsContent == null || _offerItemPrefab == null) return;

            ClearContent(_wildCardsContent);

            var offers = GenerateWildCardOffers();
            foreach (var offer in offers)
            {
                CreateOfferItem(_wildCardsContent, offer);
            }
        }

        private void ClearContent(Transform container)
        {
            foreach (Transform child in container)
            {
                Destroy(child.gameObject);
            }
        }

        private void CreateOfferItem(Transform parent, ShopOffer offer)
        {
            var offerGO = Instantiate(_offerItemPrefab, parent);
            var offerUI = offerGO.GetComponent<ShopOfferItemUI>();
            if (offerUI != null)
            {
                offerUI.Initialize(offer, OnPurchaseClicked);
            }
        }

        private void CreateLargeOffer(Transform parent, ShopOffer offer)
        {
            var offerGO = Instantiate(_largeOfferPrefab, parent);
            var offerUI = offerGO.GetComponent<ShopOfferItemUI>();
            if (offerUI != null)
            {
                offerUI.Initialize(offer, OnPurchaseClicked);
            }
        }

        private void OnPurchaseClicked(ShopOffer offer)
        {
            var playerData = Services.Get<GameManager>().LocalPlayer;
            if (playerData == null) return;

            bool canAfford = offer.costType == CurrencyType.Gold ? playerData.gold >= offer.cost : playerData.gems >= offer.cost;

            if (!canAfford)
            {
                EventBus.RaiseToast("Not enough currency!");
                UISoundPlayer.Instance?.PlayError();
                return;
            }

            // Deduct currency
            if (offer.costType == CurrencyType.Gold)
            {
                playerData.gold -= offer.cost;
            }
            else
            {
                playerData.gems -= offer.cost;
            }

            // Grant reward
            GrantReward(offer);

            UpdateCurrency();
            EventBus.RaiseToast($"Purchased {offer.name}!");
            UISoundPlayer.Instance?.PlaySuccess();

            // Send to server
            Services.Get<NetworkClient>().Send(new NetworkClient.ShopPurchaseRequest 
            { 
                offerId = offer.id, 
                currency = offer.costType 
            });
        }

        private void GrantReward(ShopOffer offer)
        {
            var playerData = Services.Get<GameManager>().LocalPlayer;
            if (playerData == null) return;

            switch (offer.rewardType)
            {
                case RewardType.Card:
                    if (!playerData.collection.ContainsKey(offer.rewardCardId))
                        playerData.collection[offer.rewardCardId] = 0;
                    playerData.collection[offer.rewardCardId] += offer.rewardCount;
                    break;
                case RewardType.Gold:
                    playerData.gold += offer.rewardCount;
                    break;
                case RewardType.Gems:
                    playerData.gems += offer.rewardCount;
                    break;
                case RewardType.Chest:
                    // Add chest to chest slots
                    break;
            }
        }

        private void UpdateCurrency()
        {
            var playerData = Services.Get<GameManager>().LocalPlayer;
            if (playerData != null)
            {
                if (_gemText != null) _gemText.text = playerData.gems.ToString("N0");
                if (_goldText != null) _goldText.text = playerData.gold.ToString("N0");
            }
        }

        private void StartTimers()
        {
            if (_timerCoroutine != null) StopCoroutine(_timerCoroutine);
            _timerCoroutine = StartCoroutine(TimerRoutine());
        }

        private void StopTimers()
        {
            if (_timerCoroutine != null)
            {
                StopCoroutine(_timerCoroutine);
                _timerCoroutine = null;
            }
        }

        private IEnumerator TimerRoutine()
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
                TimeSpan ts = TimeSpan.FromSeconds(_dailyRefreshTime);
                _dailyRefreshTimer.text = $"{ts.Hours:00}:{ts.Minutes:00}:{ts.Seconds:00}";
            }

            if (_specialOfferTimer != null)
            {
                TimeSpan ts = TimeSpan.FromSeconds(_specialOfferTime);
                _specialOfferTimer.text = $"{ts.Hours:00}:{ts.Minutes:00}:{ts.Seconds:00}";
            }
        }

        private List<ShopOffer> GenerateDailyOffers()
        {
            var dataManager = Services.Get<DataManager>();
            var playerData = Services.Get<GameManager>().LocalPlayer;
            var offers = new List<ShopOffer>();

            // Card offers
            var cards = dataManager.GetAllCards();
            var availableCards = new List<CardData>();
            foreach (var card in cards)
            {
                if (card.isEnabled && playerData?.collection.ContainsKey(card.cardId) == true)
                {
                    availableCards.Add(card);
                }
            }

            for (int i = 0; i < 4 && i < availableCards.Count; i++)
            {
                var card = availableCards[UnityEngine.Random.Range(0, availableCards.Count)];
                offers.Add(new ShopOffer
                {
                    id = $"daily_card_{i}",
                    name = card.cardName,
                    description = $"{card.cardName} x{UnityEngine.Random.Range(1, 10)}",
                    cost = UnityEngine.Random.Range(50, 500),
                    costType = CurrencyType.Gold,
                    rewardType = RewardType.Card,
                    rewardCardId = card.cardId,
                    rewardCount = UnityEngine.Random.Range(1, 10),
                    iconSprite = Services.Get<AssetManager>().LoadSprite(card.portraitId)
                });
            }

            return offers;
        }

        private List<ShopOffer> GenerateSpecialOffers()
        {
            var offers = new List<ShopOffer>();
            offers.Add(new ShopOffer
            {
                id = "special_bundle_1",
                name = "Starter Bundle",
                description = "10,000 Gold + 100 Gems + 5 Chests",
                cost = 500,
                costType = CurrencyType.Gems,
                rewardType = RewardType.Gold,
                rewardCount = 10000,
                isSpecial = true
            });
            return offers;
        }

        private List<ShopOffer> GenerateChestOffers()
        {
            var offers = new List<ShopOffer>();
            string[] chestNames = { "Wooden Chest", "Silver Chest", "Golden Chest", "Magical Chest" };
            int[] chestCosts = { 50, 200, 500, 1000 };

            for (int i = 0; i < chestNames.Length; i++)
            {
                offers.Add(new ShopOffer
                {
                    id = $"chest_{i}",
                    name = chestNames[i],
                    description = $"Contains cards and gold",
                    cost = chestCosts[i],
                    costType = CurrencyType.Gems,
                    rewardType = RewardType.Chest,
                    rewardChestType = i
                });
            }
            return offers;
        }

        private List<ShopOffer> GenerateGemOffers()
        {
            var offers = new List<ShopOffer>();
            int[] gemAmounts = { 80, 500, 1200, 2500, 6500, 14000 };
            int[] gemCosts = { 1, 5, 10, 20, 50, 100 };

            for (int i = 0; i < gemAmounts.Length; i++)
            {
                offers.Add(new ShopOffer
                {
                    id = $"gems_{i}",
                    name = $"{gemAmounts[i]} Gems",
                    description = "Premium currency",
                    cost = gemCosts[i],
                    costType = CurrencyType.RealMoney,
                    rewardType = RewardType.Gems,
                    rewardCount = gemAmounts[i]
                });
            }
            return offers;
        }

        private List<ShopOffer> GenerateWildCardOffers()
        {
            var offers = new List<ShopOffer>();
            string[] rarities = { "Common", "Rare", "Epic", "Legendary" };
            int[] costs = { 100, 500, 1000, 2000 };

            for (int i = 0; i < rarities.Length; i++)
            {
                offers.Add(new ShopOffer
                {
                    id = $"wildcard_{i}",
                    name = $"{rarities[i]} Wild Card",
                    description = $"Convert to any {rarities[i]} card",
                    cost = costs[i],
                    costType = CurrencyType.Gold,
                    rewardType = RewardType.WildCard,
                    rewardRarity = (CardRarity)(i + 1),
                    rewardCount = 1
                });
            }
            return offers;
        }
    }

    [Serializable]
    public class ShopOffer
    {
        public string id;
        public string name;
        public string description;
        public int cost;
        public CurrencyType costType;
        public RewardType rewardType;
        public int rewardCardId;
        public int rewardCount;
        public int rewardChestType;
        public CardRarity rewardRarity;
        public Sprite iconSprite;
        public bool isSpecial;
    }

    public enum CurrencyType
    {
        Gold,
        Gems,
        RealMoney
    }

    public enum RewardType
    {
        Card,
        Gold,
        Gems,
        Chest,
        WildCard
    }
}