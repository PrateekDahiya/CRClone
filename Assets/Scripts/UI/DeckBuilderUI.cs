using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using CRClone.Core;
using CRClone.Data;

namespace CRClone.UI
{
    public class DeckBuilderUI : MonoBehaviour, IDropHandler
    {
        [Header("Deck Slots")]
        [SerializeField] private Transform _deckSlotsContainer;
        [SerializeField] private GameObject _deckSlotPrefab;
        [SerializeField] private Text _avgElixirText;
        [SerializeField] private Text _championWarningText;

        [Header("Card Collection")]
        [SerializeField] private Transform _collectionContainer;
        [SerializeField] private GameObject _collectionCardPrefab;
        [SerializeField] private Dropdown _rarityFilter;
        [SerializeField] private Dropdown _typeFilter;
        [SerializeField] private InputField _searchInput;

        [Header("Actions")]
        [SerializeField] private Button _saveButton;
        [SerializeField] private Button _cancelButton;
        [SerializeField] private Button _copyLinkButton;

        private DeckSlotUI[] _deckSlots = new DeckSlotUI[8];
        private List<CollectionCardUI> _collectionCards = new();
        private int[] _currentDeck = new int[8];
        private CardRarity _selectedRarity = CardRarity.Common;
        private CardType _selectedType = CardType.Troop;
        private string _searchText = "";

        private void Awake()
        {
            // Create deck slots
            for (int i = 0; i < 8; i++)
            {
                var slotGO = Instantiate(_deckSlotPrefab, _deckSlotsContainer);
                _deckSlots[i] = slotGO.GetComponent<DeckSlotUI>();
                _deckSlots[i].Initialize(i, this);
            }

            _rarityFilter?.onValueChanged.AddListener(OnRarityFilterChanged);
            _typeFilter?.onValueChanged.AddListener(OnTypeFilterChanged);
            _searchInput?.onValueChanged.AddListener(OnSearchChanged);
            _saveButton?.onClick.AddListener(OnSaveClicked);
            _cancelButton?.onClick.AddListener(OnCancelClicked);
            _copyLinkButton?.onClick.AddListener(OnCopyLinkClicked);
        }

        public void Initialize()
        {
            LoadCurrentDeck();
            PopulateCollection();
            UpdateDeckDisplay();
        }

        private void LoadCurrentDeck()
        {
            var playerData = Services.Get<GameManager>().LocalPlayer;
            if (playerData?.activeDeck != null)
            {
                Array.Copy(playerData.activeDeck.cardIds, _currentDeck, 8);
            }
        }

        private void PopulateCollection()
        {
            var dataManager = Services.Get<DataManager>();
            var playerData = Services.Get<GameManager>().LocalPlayer;

            foreach (var card in dataManager.GetAllCards())
            {
                if (!card.isEnabled) continue;
                if (playerData != null && !playerData.collection.ContainsKey(card.cardId)) continue;

                var cardGO = Instantiate(_collectionCardPrefab, _collectionContainer);
                var cardUI = cardGO.GetComponent<CollectionCardUI>();
                cardUI.Initialize(card, playerData?.collection.ContainsKey(card.cardId) == true);
                _collectionCards.Add(cardUI);
            }

            ApplyFilters();
        }

        private void ApplyFilters()
        {
            foreach (var cardUI in _collectionCards)
            {
                bool show = true;

                if (_selectedRarity != CardRarity.Common && cardUI.CardData.rarity != _selectedRarity)
                    show = false;

                if (_selectedType != CardType.Troop && cardUI.CardData.type != _selectedType)
                    show = false;

                if (!string.IsNullOrEmpty(_searchText))
                {
                    if (!cardUI.CardData.cardName.ToLower().Contains(_searchText.ToLower()))
                        show = false;
                }

                cardUI.gameObject.SetActive(show);
            }
        }

        private void OnRarityFilterChanged(int value)
        {
            _selectedRarity = (CardRarity)value;
            ApplyFilters();
        }

        private void OnTypeFilterChanged(int value)
        {
            _selectedType = (CardType)value;
            ApplyFilters();
        }

        private void OnSearchChanged(string text)
        {
            _searchText = text;
            ApplyFilters();
        }

        public void OnCardDragStart(CollectionCardUI cardUI)
        {
            // Visual feedback
        }

        public void OnCardDragEnd(CollectionCardUI cardUI)
        {
            // Cleanup
        }

        public void OnDrop(PointerEventData eventData)
        {
            // Handle drop on deck slot
            var slotUI = eventData.pointerEnter?.GetComponent<DeckSlotUI>();
            if (slotUI != null)
            {
                var cardUI = eventData.pointerDrag?.GetComponent<CollectionCardUI>();
                if (cardUI != null)
                {
                    AddCardToSlot(slotUI.SlotIndex, cardUI.CardData.cardId);
                }
            }
        }

        public bool AddCardToSlot(int slotIndex, int cardId)
        {
            if (slotIndex < 0 || slotIndex >= 8) return false;

            var cardData = Services.Get<DataManager>().GetCard(cardId);
            if (cardData == null) return false;

            // Check champion limit
            if (cardData.rarity == CardRarity.Champion)
            {
                int championCount = 0;
                for (int i = 0; i < 8; i++)
                {
                    if (i != slotIndex && _currentDeck[i] > 0)
                    {
                        var c = Services.Get<DataManager>().GetCard(_currentDeck[i]);
                        if (c?.rarity == CardRarity.Champion) championCount++;
                    }
                }
                if (championCount >= 1) return false;
            }

            _currentDeck[slotIndex] = cardId;
            UpdateDeckDisplay();
            return true;
        }

        public void RemoveCardFromSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= 8) return;
            _currentDeck[slotIndex] = 0;
            UpdateDeckDisplay();
        }

        private void UpdateDeckDisplay()
        {
            for (int i = 0; i < 8; i++)
            {
                int cardId = _currentDeck[i];
                if (cardId > 0)
                {
                    var cardData = Services.Get<DataManager>().GetCard(cardId);
                    _deckSlots[i].SetCard(cardData);
                }
                else
                {
                    _deckSlots[i].ClearCard();
                }
            }

            UpdateStats();
        }

        private void UpdateStats()
        {
            var dataManager = Services.Get<DataManager>();
            float totalElixir = 0;
            int championCount = 0;
            int cardCount = 0;

            for (int i = 0; i < 8; i++)
            {
                if (_currentDeck[i] > 0)
                {
                    var card = dataManager.GetCard(_currentDeck[i]);
                    if (card != null)
                    {
                        totalElixir += card.elixirCost;
                        cardCount++;
                        if (card.rarity == CardRarity.Champion) championCount++;
                    }
                }
            }

            float avgElixir = cardCount > 0 ? totalElixir / cardCount : 0;
            _avgElixirText.text = $"Avg Elixir: {avgElixir:F1}";

            _championWarningText.gameObject.SetActive(championCount > 1);
            _championWarningText.text = championCount > 1 ? "Max 1 Champion!" : "";
        }

        private void OnSaveClicked()
        {
            var dataManager = Services.Get<DataManager>();
            if (dataManager.ValidateDeck(_currentDeck, out string error))
            {
                // Save to server/local
                var playerData = Services.Get<GameManager>().LocalPlayer;
                if (playerData != null)
                {
                    playerData.activeDeck = new GameManager.DeckData
                    {
                        cardIds = (int[])_currentDeck.Clone(),
                        avgElixir = float.Parse(_avgElixirText.text.Split(':')[1]),
                        hasChampion = _championWarningText.gameObject.activeSelf
                    };
                }

                Services.Get<NetworkClient>().Send(new NetworkClient.SaveDeckRequest { cardIds = _currentDeck });
                EventBus.RaiseToast("Deck saved!");
            }
            else
            {
                EventBus.RaiseError(error);
            }
        }

        private void OnCancelClicked()
        {
            LoadCurrentDeck();
            UpdateDeckDisplay();
        }

        private void OnCopyLinkClicked()
        {
            // Generate deck link
            string link = GenerateDeckLink(_currentDeck);
            GUIUtility.systemCopyBuffer = link;
            EventBus.RaiseToast("Deck link copied!");
        }

        private string GenerateDeckLink(int[] deck)
        {
            // Simple encoding
            return $"crclone://deck/{string.Join(",", deck)}";
        }
    }

    public class DeckSlotUI : MonoBehaviour, IDropHandler
    {
        [SerializeField] private Image _cardImage;
        [SerializeField] private Text _elixirCostText;
        [SerializeField] private Button _removeButton;

        public int SlotIndex { get; private set; }
        private DeckBuilderUI _deckBuilder;

        public void Initialize(int index, DeckBuilderUI builder)
        {
            SlotIndex = index;
            _deckBuilder = builder;
            _removeButton?.onClick.AddListener(() => _deckBuilder.RemoveCardFromSlot(SlotIndex));
        }

        public void SetCard(CardData cardData)
        {
            if (_cardImage != null && cardData != null)
            {
                _cardImage.sprite = Services.Get<AssetManager>().LoadSprite(cardData.portraitId);
                _cardImage.enabled = true;
            }

            if (_elixirCostText != null)
            {
                _elixirCostText.text = cardData?.elixirCost.ToString() ?? "";
            }
        }

        public void ClearCard()
        {
            if (_cardImage != null) _cardImage.enabled = false;
            if (_elixirCostText != null) _elixirCostText.text = "";
        }

        public void OnDrop(PointerEventData eventData)
        {
            var cardUI = eventData.pointerDrag?.GetComponent<CollectionCardUI>();
            if (cardUI != null)
            {
                _deckBuilder.AddCardToSlot(SlotIndex, cardUI.CardData.cardId);
            }
        }
    }

    public class CollectionCardUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private Image _cardImage;
        [SerializeField] private Text _elixirCostText;
        [SerializeField] private GameObject _ownedBadge;
        [SerializeField] private CanvasGroup _canvasGroup;

        public CardData CardData { get; private set; }
        private DeckBuilderUI _deckBuilder;
        private RectTransform _rectTransform;
        private Vector3 _originalPosition;

        public void Initialize(CardData cardData, bool owned)
        {
            CardData = cardData;
            _deckBuilder = FindObjectOfType<DeckBuilderUI>();
            _rectTransform = GetComponent<RectTransform>();

            if (_cardImage != null)
                _cardImage.sprite = Services.Get<AssetManager>().LoadSprite(cardData.portraitId);

            if (_elixirCostText != null)
                _elixirCostText.text = cardData.elixirCost.ToString();

            if (_ownedBadge != null)
                _ownedBadge.SetActive(owned);

            if (_canvasGroup != null)
                _canvasGroup.alpha = owned ? 1f : 0.5f;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!CardData.isEnabled) return;

            _originalPosition = _rectTransform.position;
            _canvasGroup.blocksRaycasts = false;
            _deckBuilder?.OnCardDragStart(this);
        }

        public void OnDrag(PointerEventData eventData)
        {
            _rectTransform.position = eventData.position;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            _rectTransform.position = _originalPosition;
            _canvasGroup.blocksRaycasts = true;
            _deckBuilder?.OnCardDragEnd(this);
        }
    }
}