using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using CRClone.Core;
using CRClone.Data;
using CRClone.Network;
using CRClone.Systems;
using CRClone.UI.Animation;
using CRClone.UI.Components;

namespace CRClone.UI
{
    public class DeckBuilderUI : MonoBehaviour, IDropHandler
    {
        [Header("Deck Slots")]
        [SerializeField] private Transform _deckSlotsContainer;
        [SerializeField] private GameObject _deckSlotPrefab;
        [SerializeField] private Text _avgElixirText;
        [SerializeField] private Text _championWarningText;
        [SerializeField] private Text _cardCountText;

        [Header("Deck Stats")]
        [SerializeField] private Transform _troopCountIcon;
        [SerializeField] private Text _troopCountText;
        [SerializeField] private Transform _spellCountIcon;
        [SerializeField] private Text _spellCountText;
        [SerializeField] private Transform _buildingCountIcon;
        [SerializeField] private Text _buildingCountText;
        [SerializeField] private Transform _championCountIcon;
        [SerializeField] private Text _championCountText;

        [Header("Card Collection")]
        [SerializeField] private Transform _collectionContainer;
        [SerializeField] private GameObject _collectionCardPrefab;
        [SerializeField] private Dropdown _rarityFilter;
        [SerializeField] private Dropdown _typeFilter;
        [SerializeField] private InputField _searchInput;
        [SerializeField] private Toggle _showUnownedToggle;

        [Header("Actions")]
        [SerializeField] private Button _saveButton;
        [SerializeField] private Button _cancelButton;
        [SerializeField] private Button _copyLinkButton;
        [SerializeField] private Button _clearDeckButton;

        [Header("Card Details Modal")]
        [SerializeField] private GameObject _cardDetailsModalPrefab;

        private DeckSlotUI[] _deckSlots = new DeckSlotUI[8];
        private List<CollectionCardUI> _collectionCards = new List<CollectionCardUI>();
        private int[] _currentDeck = new int[8];
        private int[] _originalDeck = new int[8];
        private CardRarity _selectedRarity = CardRarity.Common;
        private CardType _selectedType = CardType.Troop;
        private string _searchText = "";
        private bool _showUnowned = false;
        private GameObject _activeCardDetailsModal;

        private void Awake()
        {
            for (int i = 0; i < 8; i++)
            {
                var slotGO = Instantiate(_deckSlotPrefab, _deckSlotsContainer);
                _deckSlots[i] = slotGO.GetComponent<DeckSlotUI>();
                _deckSlots[i].Initialize(i, this);
            }

            SetupFilters();
            SetupActionButtons();
        }

        private void SetupFilters()
        {
            if (_rarityFilter != null)
            {
                _rarityFilter.ClearOptions();
                _rarityFilter.AddOptions(new List<string> { "All", "Common", "Rare", "Epic", "Legendary", "Champion" });
                _rarityFilter.onValueChanged.AddListener(OnRarityFilterChanged);
            }

            if (_typeFilter != null)
            {
                _typeFilter.ClearOptions();
                _typeFilter.AddOptions(new List<string> { "All", "Troop", "Spell", "Building", "Champion" });
                _typeFilter.onValueChanged.AddListener(OnTypeFilterChanged);
            }

            _searchInput.OrNull()?.onValueChanged.AddListener(OnSearchChanged);
            _showUnownedToggle.OrNull()?.onValueChanged.AddListener(OnShowUnownedChanged);
        }

        private void SetupActionButtons()
        {
            _saveButton.OrNull()?.onClick.AddListener(OnSaveClicked);
            _cancelButton.OrNull()?.onClick.AddListener(OnCancelClicked);
            _copyLinkButton.OrNull()?.onClick.AddListener(OnCopyLinkClicked);
            _clearDeckButton.OrNull()?.onClick.AddListener(OnClearDeckClicked);
        }

        public void Initialize()
        {
            LoadCurrentDeck();
            PopulateCollection();
            UpdateDeckDisplay();
            ValidateDeck();
        }

        private void LoadCurrentDeck()
        {
            var playerData = Services.Get<GameManager>().LocalPlayer;
            if (playerData?.activeDeck != null)
            {
                Array.Copy(playerData.activeDeck.cardIds, _currentDeck, 8);
                Array.Copy(_currentDeck, _originalDeck, 8);
            }
            else
            {
                Array.Clear(_currentDeck, 0, 8);
                Array.Clear(_originalDeck, 0, 8);
            }
        }

        private void PopulateCollection()
        {
            var dataManager = Services.Get<DataManager>();
            var playerData = Services.Get<GameManager>().LocalPlayer;

            foreach (var card in dataManager.GetAllCards())
            {
                if (!card.isEnabled) continue;

                bool owned = playerData != null && playerData.collection.ContainsKey(card.cardId);
                if (!owned && !_showUnowned) continue;

                var cardGO = Instantiate(_collectionCardPrefab, _collectionContainer);
                var cardUI = cardGO.GetComponent<CollectionCardUI>();
                if (cardUI == null) cardUI = cardGO.AddComponent<CollectionCardUI>();
                cardUI.Initialize(card, owned, this);
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

                if (!_showUnowned && !cardUI.IsOwned)
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
            _selectedRarity = value == 0 ? CardRarity.Common : (CardRarity)(value - 1);
            ApplyFilters();
        }

        private void OnTypeFilterChanged(int value)
        {
            _selectedType = value == 0 ? CardType.Troop : (CardType)(value - 1);
            ApplyFilters();
        }

        private void OnSearchChanged(string text)
        {
            _searchText = text;
            ApplyFilters();
        }

        private void OnShowUnownedChanged(bool value)
        {
            _showUnowned = value;
            ApplyFilters();
        }

        public void OnCardDragStart(CollectionCardUI cardUI)
        {
            HighlightValidSlots(cardUI.CardData);
        }

        public void OnCardDragEnd(CollectionCardUI cardUI)
        {
            ClearSlotHighlights();
        }

        private void HighlightValidSlots(CardData cardData)
        {
            bool isChampion = cardData.rarity == CardRarity.Champion;
            int currentChampions = CountChampionsInDeck();

            for (int i = 0; i < 8; i++)
            {
                if (_currentDeck[i] == 0 || _deckSlots[i].CanAcceptCard(cardData))
                {
                    if (isChampion && currentChampions >= 1 && _currentDeck[i] == 0)
                    {
                        _deckSlots[i].SetHighlight(false, true);
                    }
                    else
                    {
                        _deckSlots[i].SetHighlight(true, false);
                    }
                }
            }
        }

        private void ClearSlotHighlights()
        {
            for (int i = 0; i < 8; i++)
            {
                _deckSlots[i].SetHighlight(false, false);
            }
        }

        public void OnDrop(PointerEventData eventData)
        {
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

            if (cardData.rarity == CardRarity.Champion)
            {
                int championCount = CountChampionsInDeck(slotIndex);
                if (championCount >= 1)
                {
                    UISoundPlayer.Instance?.PlayError();
                    EventBus.RaiseToast("Maximum 1 Champion allowed!");
                    return false;
                }
            }

            _currentDeck[slotIndex] = cardId;
            UpdateDeckDisplay();
            ValidateDeck();
            UISoundPlayer.Instance?.PlayCardSelect();
            return true;
        }

        public void RemoveCardFromSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= 8) return;
            _currentDeck[slotIndex] = 0;
            UpdateDeckDisplay();
            ValidateDeck();
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
            int troopCount = 0, spellCount = 0, buildingCount = 0;

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

                        switch (card.type)
                        {
                            case CardType.Troop: troopCount++; break;
                            case CardType.Spell: spellCount++; break;
                            case CardType.Building: buildingCount++; break;
                            case CardType.Champion: championCount++; break;
                        }
                    }
                }
            }

            float avgElixir = cardCount > 0 ? totalElixir / cardCount : 0;

            if (_avgElixirText != null) _avgElixirText.text = $"Avg Elixir: {avgElixir:F1}";
            if (_cardCountText != null) _cardCountText.text = $"{cardCount}/8 Cards";
            if (_troopCountText != null) _troopCountText.text = troopCount.ToString();
            if (_spellCountText != null) _spellCountText.text = spellCount.ToString();
            if (_buildingCountText != null) _buildingCountText.text = buildingCount.ToString();
            if (_championCountText != null) _championCountText.text = championCount.ToString();

            bool hasChampionWarning = championCount > 1;
            if (_championWarningText != null)
            {
                _championWarningText.gameObject.SetActive(hasChampionWarning);
                _championWarningText.text = hasChampionWarning ? "Max 1 Champion!" : "";
            }

            UpdateSaveButtonState(cardCount, championCount);
        }

        private void UpdateSaveButtonState(int cardCount, int championCount)
        {
            bool isValid = cardCount == 8 && championCount <= 1;
            if (_saveButton != null)
            {
                _saveButton.interactable = isValid;
            }
        }

        private int CountChampionsInDeck(int excludeSlot = -1)
        {
            int count = 0;
            var dataManager = Services.Get<DataManager>();

            for (int i = 0; i < 8; i++)
            {
                if (i == excludeSlot) continue;
                if (_currentDeck[i] > 0)
                {
                    var card = dataManager.GetCard(_currentDeck[i]);
                    if (card?.rarity == CardRarity.Champion) count++;
                }
            }
            return count;
        }

        public bool ValidateDeck()
        {
            var dataManager = Services.Get<DataManager>();
            return dataManager.ValidateDeck(_currentDeck, out _);
        }

        private void OnSaveClicked()
        {
            var dataManager = Services.Get<DataManager>();
            if (dataManager.ValidateDeck(_currentDeck, out string error))
            {
                var playerData = Services.Get<GameManager>().LocalPlayer;
                if (playerData != null)
                {
                    float avgElixir = 0f;
                    int cardCount = 0;
                    for (int i = 0; i < 8; i++)
                    {
                        if (_currentDeck[i] > 0)
                        {
                            var card = dataManager.GetCard(_currentDeck[i]);
                            if (card != null)
                            {
                                avgElixir += card.elixirCost;
                                cardCount++;
                            }
                        }
                    }
                    avgElixir = cardCount > 0 ? avgElixir / cardCount : 0f;

                    playerData.activeDeck = new DeckData
                    {
                        cardIds = (int[])_currentDeck.Clone(),
                        avgElixir = avgElixir,
                        hasChampion = CountChampionsInDeck() > 0
                    };
                }

                Array.Copy(_currentDeck, _originalDeck, 8);
                Services.Get<NetworkClient>().Send(new SaveDeckRequest { cardIds = _currentDeck.Select(id => (uint)id).ToArray() });
                EventBus.RaiseToast("Deck saved!");
                UISoundPlayer.Instance?.PlaySuccess();
            }
            else
            {
                EventBus.RaiseError(error);
                UISoundPlayer.Instance?.PlayError();
            }
        }

        private void OnCancelClicked()
        {
            Array.Copy(_originalDeck, _currentDeck, 8);
            UpdateDeckDisplay();
            ValidateDeck();
            UISoundPlayer.Instance?.PlayButtonClick();
        }

        private void OnClearDeckClicked()
        {
            Array.Clear(_currentDeck, 0, 8);
            UpdateDeckDisplay();
            ValidateDeck();
            UISoundPlayer.Instance?.PlayButtonClick();
        }

        private void OnCopyLinkClicked()
        {
            string link = GenerateDeckLink(_currentDeck);
            GUIUtility.systemCopyBuffer = link;
            EventBus.RaiseToast("Deck link copied to clipboard!");
            UISoundPlayer.Instance?.PlaySuccess();
        }

        private string GenerateDeckLink(int[] deck)
        {
            return $"crclone://deck/{string.Join(",", deck)}";
        }

        public void ShowCardDetails(CardData cardData)
        {
            if (_cardDetailsModalPrefab == null) return;

            if (_activeCardDetailsModal != null)
            {
                Destroy(_activeCardDetailsModal);
            }

            _activeCardDetailsModal = Instantiate(_cardDetailsModalPrefab, transform);
            var modal = _activeCardDetailsModal.GetComponent<CardDetailsModal>();
            if (modal != null)
            {
                modal.Initialize(cardData, this);
            }
        }

        public void CloseCardDetails()
        {
            if (_activeCardDetailsModal != null)
            {
                Destroy(_activeCardDetailsModal);
                _activeCardDetailsModal = null;
            }
        }

        private void OnDisable()
        {
            CloseCardDetails();
        }
    }

    public class DeckSlotUI : MonoBehaviour, IDropHandler
    {
        [SerializeField] private Image _cardImage;
        [SerializeField] private Text _elixirCostText;
        [SerializeField] private Button _removeButton;
        [SerializeField] private GameObject _highlightValid;
        [SerializeField] private GameObject _highlightInvalid;

        public int SlotIndex { get; private set; }
        private DeckBuilderUI _deckBuilder;

        public void Initialize(int index, DeckBuilderUI builder)
        {
            SlotIndex = index;
            _deckBuilder = builder;
            _removeButton.OrNull()?.onClick.AddListener(() => _deckBuilder.RemoveCardFromSlot(SlotIndex));
            SetHighlight(false, false);
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

        public void SetHighlight(bool valid, bool invalid)
        {
            _highlightValid.OrNull()?.SetActive(valid);
            _highlightInvalid.OrNull()?.SetActive(invalid);
        }

        public bool CanAcceptCard(CardData cardData)
        {
            return true;
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

    public class CollectionCardUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
    {
        [SerializeField] private Image _cardImage;
        [SerializeField] private Text _elixirCostText;
        [SerializeField] private GameObject _ownedBadge;
        [SerializeField] private GameObject _unownedOverlay;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private GameObject _rarityFrame;
        [SerializeField] private Image _typeIcon;

        public CardData CardData { get; private set; }
        public bool IsOwned { get; private set; }

        private DeckBuilderUI _deckBuilder;
        private RectTransform _rectTransform;
        private Vector3 _originalPosition;
        private bool _isDragging;

        public void Initialize(CardData cardData, bool owned, DeckBuilderUI builder)
        {
            CardData = cardData;
            IsOwned = owned;
            _deckBuilder = builder;
            _rectTransform = GetComponent<RectTransform>();

            if (_cardImage != null)
                _cardImage.sprite = Services.Get<AssetManager>().LoadSprite(cardData.portraitId);

            if (_elixirCostText != null)
                _elixirCostText.text = cardData.elixirCost.ToString();

            if (_ownedBadge != null)
                _ownedBadge.SetActive(owned);

            if (_unownedOverlay != null)
                _unownedOverlay.SetActive(!owned);

            if (_typeIcon != null)
            {
                _typeIcon.sprite = GetTypeIcon(cardData.type);
            }

            if (_canvasGroup != null)
                _canvasGroup.alpha = owned ? 1f : 0.5f;

            ApplyRarityVisual(cardData.rarity);
        }

        private Sprite GetTypeIcon(CardType type)
        {
            return null;
        }

        private void ApplyRarityVisual(CardRarity rarity)
        {
            if (_rarityFrame == null) return;

            Color frameColor = rarity switch
            {
                CardRarity.Common => new Color(0.62f, 0.62f, 0.62f),
                CardRarity.Rare => new Color(0.13f, 0.59f, 0.95f),
                CardRarity.Epic => new Color(0.61f, 0.15f, 0.69f),
                CardRarity.Legendary => new Color(1f, 0.6f, 0f),
                CardRarity.Champion => new Color(0.91f, 0.12f, 0.39f),
                _ => Color.white
            };

            _rarityFrame.GetComponent<Image>().color = frameColor;

            var patternSprite = AccessibilityManager.Instance?.GetRarityPattern(rarity);
            if (patternSprite != null && AccessibilityManager.Instance?.ColorBlindMode != ColorBlindMode.None)
            {
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!IsOwned || !CardData.isEnabled) return;

            _originalPosition = _rectTransform.position;
            _canvasGroup.blocksRaycasts = false;
            _isDragging = true;
            _deckBuilder?.OnCardDragStart(this);

            transform.SetAsLastSibling();
            StartCoroutine(DragScaleAnimation());
        }

        private System.Collections.IEnumerator DragScaleAnimation()
        {
            yield return transform.ScaleTo(Vector3.one * 1.15f, 0.1f, AnimationCurves.EaseOutBack);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_isDragging) return;
            _rectTransform.position = eventData.position;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!_isDragging) return;

            _isDragging = false;
            _rectTransform.position = _originalPosition;
            _canvasGroup.blocksRaycasts = true;
            _deckBuilder?.OnCardDragEnd(this);

            StartCoroutine(ReturnScaleAnimation());
        }

        private System.Collections.IEnumerator ReturnScaleAnimation()
        {
            yield return transform.ScaleTo(Vector3.one, 0.15f, AnimationCurves.EaseOutBack);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Right || 
                (eventData.clickCount == 2 && IsOwned))
            {
                _deckBuilder?.ShowCardDetails(CardData);
            }
        }
    }
}