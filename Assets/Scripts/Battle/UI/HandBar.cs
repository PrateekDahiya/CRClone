using System;
using UnityEngine;
using UnityEngine.UI;
using CRClone.Core;
using CRClone.Data;
using CRClone.UI.Animation;

namespace CRClone.Battle.UI
{
    public class HandBar : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private RectTransform _cardContainer;
        [SerializeField] private GameObject _handCardPrefab;
        [SerializeField] private ElixirBar _elixirBar;
        [SerializeField] private GameObject _nextCardPreview;
        [SerializeField] private Image _nextCardImage;
        [SerializeField] private Text _nextCardLabel;

        [Header("Animation Settings")]
        [SerializeField] private float _cardSelectScale = 1.2f;
        [SerializeField] private float _selectionAnimationDuration = 0.15f;
        [SerializeField] private float _deployAnimationDuration = 0.2f;
        [SerializeField] private float _cardDrawDuration = 0.3f;
        [SerializeField] private AnimationCurve _selectionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] private AnimationCurve _deployCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Visual States")]
        [SerializeField] private Color _affordableColor = Color.white;
        [SerializeField] private Color _unaffordableColor = new Color(1f, 0.3f, 0.3f);
        [SerializeField] private Color _selectedGlowColor = new Color(1f, 1f, 0.5f, 0.8f);

        private HandCardUI[] _handCards = new HandCardUI[4];
        private BattleSimulation _simulation;
        private PlayerState _localPlayer;
        private int _selectedCardIndex = -1;
        private Vector2 _dragStartPosition;
        private bool _isDragging;
        private int _currentElixir;

        private void Awake()
        {
            _simulation = Services.Get<GameManager>().BattleSim;
            _localPlayer = _simulation?.Player1;

            for (int i = 0; i < 4; i++)
            {
                var cardGO = Instantiate(_handCardPrefab, _cardContainer);
                _handCards[i] = cardGO.GetComponent<HandCardUI>();
                _handCards[i].Initialize(i, this);
            }

            if (_nextCardPreview != null)
            {
                _nextCardPreview.SetActive(false);
            }

            SubscribeToEvents();
            UpdateHandCards();
            UpdateNextCardPreview();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        private void SubscribeToEvents()
        {
            EventBus.OnElixirChanged += OnElixirChanged;
            EventBus.OnCardPlayed += OnCardPlayed;
        }

        private void UnsubscribeFromEvents()
        {
            EventBus.OnElixirChanged -= OnElixirChanged;
            EventBus.OnCardPlayed -= OnCardPlayed;
        }

        private void OnElixirChanged(EventBus.ElixirChangedEvent evt)
        {
            if (evt.playerId != 1) return; // Only local player

            _currentElixir = evt.currentElixir;
            
            if (_elixirBar != null)
            {
                _elixirBar.SetElixir(evt.currentElixir);
            }

            UpdateHandCardsAffordability();
        }

        private void OnCardPlayed(EventBus.CardPlayedEvent evt)
        {
            if (evt.playerId != 1) return;

            // Find which card was played and animate
            for (int i = 0; i < 4; i++)
            {
                int cardId = i < _localPlayer?.Hand.Length ? _localPlayer.Hand[i] : 0;
                if (cardId == evt.cardId)
                {
                    OnCardDeployed(i);
                    break;
                }
            }
        }

        private void UpdateHandCardsAffordability()
        {
            for (int i = 0; i < 4; i++)
            {
                int cardId = i < _localPlayer?.Hand.Length ? _localPlayer.Hand[i] : 0;
                if (cardId > 0)
                {
                    var cardData = Services.Get<DataManager>().GetCard(cardId);
                    bool affordable = _currentElixir >= cardData.elixirCost;
                    _handCards[i].SetAffordable(affordable);
                }
            }
        }

        private void Update()
        {
            if (_localPlayer == null) return;

            UpdateNextCardPreview();
        }

        private void UpdateNextCardPreview()
        {
            if (_nextCardPreview != null && _nextCardImage != null)
            {
                int nextIndex = _localPlayer.NextCardIndex;
                if (nextIndex >= 0 && nextIndex < _localPlayer.Deck.Length)
                {
                    int nextCardId = _localPlayer.Deck[nextIndex];
                    var cardData = Services.Get<DataManager>().GetCard(nextCardId);
                    if (cardData != null)
                    {
                        _nextCardImage.sprite = Services.Get<AssetManager>().LoadSprite(cardData.portraitId);
                        _nextCardPreview.SetActive(true);
                        if (_nextCardLabel != null) _nextCardLabel.text = "NEXT";
                    }
                    else
                    {
                        _nextCardPreview.SetActive(false);
                    }
                }
                else
                {
                    _nextCardPreview.SetActive(false);
                }
            }
        }

        public int GetCardIndexAtScreenPos(Vector2 screenPos)
        {
            for (int i = 0; i < 4; i++)
            {
                if (_handCards[i].IsPointerOver(screenPos))
                    return i;
            }
            return -1;
        }

        public int GetCardIdAtIndex(int index)
        {
            if (index >= 0 && index < 4 && _localPlayer != null)
            {
                return index < _localPlayer.Hand.Length ? _localPlayer.Hand[index] : 0;
            }
            return 0;
        }

        public void SetCardSelected(int index, bool selected)
        {
            if (index >= 0 && index < 4)
            {
                _handCards[index].SetSelected(selected);
            }

            _selectedCardIndex = selected ? index : -1;
        }

        public void ShowInsufficientElixir(int index)
        {
            if (index >= 0 && index < 4)
            {
                _handCards[index].PlayInsufficientElixirAnimation();
            }
        }

        public void OnCardDeployed(int index)
        {
            if (index >= 0 && index < 4)
            {
                _handCards[index].PlayDeployAnimation();
            }

            StartCoroutine(DrawNextCardAnimation(index));
        }

        public void AnimateCardReturn(int index)
        {
            if (index >= 0 && index < 4)
            {
                _handCards[index].PlayReturnAnimation();
            }
        }

        private System.Collections.IEnumerator DrawNextCardAnimation(int deployedIndex)
        {
            yield return new WaitForSeconds(0.1f);

            int newCardIndex = deployedIndex;
            if (newCardIndex < 4 && _localPlayer != null)
            {
                int newCardId = newCardIndex < _localPlayer.Hand.Length ? _localPlayer.Hand[newCardIndex] : 0;
                if (newCardId > 0)
                {
                    var cardData = Services.Get<DataManager>().GetCard(newCardId);
                    _handCards[newCardIndex].PlayDrawAnimation(cardData, _currentElixir >= cardData.elixirCost);
                }
            }
        }

        public void OnDragStart(int cardIndex, Vector2 screenPos)
        {
            _isDragging = true;
            _dragStartPosition = screenPos;
            SetCardSelected(cardIndex, true);
        }

        public void OnDrag(Vector2 screenPos)
        {
            if (!_isDragging || _selectedCardIndex < 0) return;

            _handCards[_selectedCardIndex].FollowCursor(screenPos);
        }

        public void OnDragEnd(Vector2 screenPos, bool validPlacement)
        {
            _isDragging = false;

            if (_selectedCardIndex >= 0)
            {
                if (validPlacement)
                {
                    OnCardDeployed(_selectedCardIndex);
                }
                else
                {
                    AnimateCardReturn(_selectedCardIndex);
                }

                SetCardSelected(_selectedCardIndex, false);
            }
        }

        private void UpdateHandCards()
        {
            for (int i = 0; i < 4; i++)
            {
                int cardId = i < _localPlayer?.Hand.Length ? _localPlayer.Hand[i] : 0;

                if (cardId > 0)
                {
                    var cardData = Services.Get<DataManager>().GetCard(cardId);
                    bool affordable = _currentElixir >= cardData.elixirCost;
                    _handCards[i].SetCard(cardData, affordable, i == _selectedCardIndex);
                }
                else
                {
                    _handCards[i].ClearCard();
                }
            }
        }
    }

    public class HandCardUI : MonoBehaviour
    {
        [SerializeField] private Image _cardImage;
        [SerializeField] private Text _elixirCostText;
        [SerializeField] private GameObject _selectionGlow;
        [SerializeField] private GameObject _unaffordableOverlay;
        [SerializeField] private Image _unaffordableColorOverlay;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private RectTransform _cardRectTransform;

        private int _index;
        private HandBar _handBar;
        private CardData _cardData;
        private bool _isSelected;
        private bool _isAffordable;

        public void Initialize(int index, HandBar handBar)
        {
            _index = index;
            _handBar = handBar;
            _cardRectTransform = GetComponent<RectTransform>();
        }

        public void SetCard(CardData cardData, bool affordable, bool selected = false)
        {
            _cardData = cardData;
            _isAffordable = affordable;
            _isSelected = selected;

            if (_cardImage != null && cardData != null)
            {
                _cardImage.sprite = Services.Get<AssetManager>().LoadSprite(cardData.portraitId);
                _cardImage.enabled = true;
            }

            if (_elixirCostText != null)
            {
                _elixirCostText.text = cardData?.elixirCost.ToString() ?? "";
                _elixirCostText.color = affordable ? _handBar._affordableColor : _handBar._unaffordableColor;
            }

            if (_unaffordableOverlay != null)
            {
                _unaffordableOverlay.SetActive(!affordable);
            }

            if (_unaffordableColorOverlay != null)
            {
                _unaffordableColorOverlay.color = affordable ? Color.clear : new Color(0.5f, 0.1f, 0.1f, 0.7f);
            }

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = affordable ? 1f : 0.6f;
                _canvasGroup.interactable = affordable;
                _canvasGroup.blocksRaycasts = affordable;
            }

            if (_selectionGlow != null)
            {
                _selectionGlow.SetActive(selected);
            }

            if (selected)
            {
                transform.localScale = Vector3.one * _handBar._cardSelectScale;
            }
            else
            {
                transform.localScale = Vector3.one;
            }
        }

        public void ClearCard()
        {
            _cardData = null;
            if (_cardImage != null) _cardImage.enabled = false;
            if (_elixirCostText != null) _elixirCostText.text = "";
            if (_unaffordableOverlay != null) _unaffordableOverlay.SetActive(false);
            if (_selectionGlow != null) _selectionGlow.SetActive(false);
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0.3f;
                _canvasGroup.interactable = false;
                _canvasGroup.blocksRaycasts = false;
            }
            transform.localScale = Vector3.one;
        }

        public void SetSelected(bool selected)
        {
            _isSelected = selected;

            if (_selectionGlow != null)
                _selectionGlow.SetActive(selected);

            if (selected)
            {
                transform.localScale = Vector3.one * _handBar._cardSelectScale;
            }
            else
            {
                transform.localScale = Vector3.one;
            }
        }

        public void SetAffordable(bool affordable)
        {
            _isAffordable = affordable;

            if (_elixirCostText != null && _cardData != null)
            {
                _elixirCostText.color = affordable ? _handBar._affordableColor : _handBar._unaffordableColor;
            }

            if (_unaffordableOverlay != null)
            {
                _unaffordableOverlay.SetActive(!affordable);
            }

            if (_unaffordableColorOverlay != null)
            {
                _unaffordableColorOverlay.color = affordable ? Color.clear : new Color(0.5f, 0.1f, 0.1f, 0.7f);
            }

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = affordable ? 1f : 0.6f;
                _canvasGroup.interactable = affordable;
                _canvasGroup.blocksRaycasts = affordable;
            }
        }

        public void FollowCursor(Vector2 screenPos)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _cardRectTransform.parent as RectTransform,
                screenPos,
                null,
                out Vector2 localPos);
            _cardRectTransform.anchoredPosition = localPos;
        }

        public void PlayInsufficientElixirAnimation()
        {
            StartCoroutine(ShakeAnimation());
        }

        public void PlayDeployAnimation()
        {
            StartCoroutine(DeployAnimationCoroutine());
        }

        public void PlayReturnAnimation()
        {
            StartCoroutine(ReturnAnimationCoroutine());
        }

        public void PlayDrawAnimation(CardData cardData, bool affordable)
        {
            StartCoroutine(DrawAnimationCoroutine(cardData, affordable));
        }

        public bool IsPointerOver(Vector2 screenPos)
        {
            return RectTransformUtility.RectangleContainsScreenPoint(_cardRectTransform, screenPos);
        }

        private IEnumerator ShakeAnimation()
        {
            Vector3 originalPos = transform.localPosition;
            for (int i = 0; i < 3; i++)
            {
                transform.localPosition = originalPos + Vector3.right * 10f;
                yield return new WaitForSeconds(0.03f);
                transform.localPosition = originalPos - Vector3.right * 10f;
                yield return new WaitForSeconds(0.03f);
            }
            transform.localPosition = originalPos;
        }

        private IEnumerator DeployAnimationCoroutine()
        {
            float duration = _handBar._deployAnimationDuration;
            float elapsed = 0f;
            Vector3 startScale = transform.localScale;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = _handBar._deployCurve.Evaluate(elapsed / duration);
                transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
                _canvasGroup.alpha = Mathf.Lerp(1f, 0f, t);
                yield return null;
            }

            transform.localScale = startScale;
            _canvasGroup.alpha = 1f;
        }

        private IEnumerator ReturnAnimationCoroutine()
        {
            float duration = 0.2f;
            float elapsed = 0f;
            Vector3 startScale = transform.localScale;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float scale = Mathf.Lerp(0f, 1f, t);
                transform.localScale = Vector3.one * scale;
                yield return null;
            }

            transform.localScale = Vector3.one;
        }

        private IEnumerator DrawAnimationCoroutine(CardData cardData, bool affordable)
        {
            _cardData = cardData;
            _isAffordable = affordable;

            if (_cardImage != null && cardData != null)
            {
                _cardImage.sprite = Services.Get<AssetManager>().LoadSprite(cardData.portraitId);
                _cardImage.enabled = true;
            }

            if (_elixirCostText != null)
            {
                _elixirCostText.text = cardData?.elixirCost.ToString() ?? "";
                _elixirCostText.color = affordable ? _handBar._affordableColor : _handBar._unaffordableColor;
            }

            transform.localScale = Vector3.zero;
            _canvasGroup.alpha = 0f;

            float elapsed = 0f;
            while (elapsed < _handBar._cardDrawDuration)
            {
                elapsed += Time.deltaTime;
                float t = _handBar._selectionCurve.Evaluate(elapsed / _handBar._cardDrawDuration);
                transform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, t);
                _canvasGroup.alpha = Mathf.Lerp(0f, affordable ? 1f : 0.6f, t);
                yield return null;
            }

            transform.localScale = Vector3.one;
            _canvasGroup.alpha = affordable ? 1f : 0.6f;
            _canvasGroup.interactable = affordable;
            _canvasGroup.blocksRaycasts = affordable;

            if (_unaffordableOverlay != null)
            {
                _unaffordableOverlay.SetActive(!affordable);
            }
        }
    }
}