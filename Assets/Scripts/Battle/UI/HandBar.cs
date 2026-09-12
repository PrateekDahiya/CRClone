using System;
using UnityEngine;
using UnityEngine.UI;
using CRClone.Core;
using CRClone.Data;

namespace CRClone.Battle.UI
{
    public class HandBar : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private RectTransform _cardContainer;
        [SerializeField] private GameObject _handCardPrefab;
        [SerializeField] private Image _elixirBarFill;
        [SerializeField] private Text _elixirText;
        [SerializeField] private GameObject _nextCardPreview;
        [SerializeField] private Image _nextCardImage;

        [Header("Animation")]
        [SerializeField] private float _cardSelectScale = 1.2f;
        [SerializeField] private float _selectionAnimationDuration = 0.15f;
        [SerializeField] private float _deployAnimationDuration = 0.3f;

        private HandCardUI[] _handCards = new HandCardUI[4];
        private BattleSimulation _simulation;
        private PlayerState _localPlayer;

        private void Awake()
        {
            _simulation = Services.Get<GameManager>().BattleSim;
            _localPlayer = _simulation?.Player1;

            // Create card slots
            for (int i = 0; i < 4; i++)
            {
                var cardGO = Instantiate(_handCardPrefab, _cardContainer);
                _handCards[i] = cardGO.GetComponent<HandCardUI>();
                _handCards[i].Initialize(i, this);
            }
        }

        private void Update()
        {
            if (_localPlayer == null) return;

            UpdateElixirBar();
            UpdateHandCards();
            UpdateNextCardPreview();
        }

        private void UpdateElixirBar()
        {
            if (_elixirBarFill != null)
            {
                float targetFill = _localPlayer.Elixir / 10f;
                _elixirBarFill.fillAmount = Mathf.Lerp(_elixirBarFill.fillAmount, targetFill, Time.deltaTime * 5f);
            }

            if (_elixirText != null)
            {
                _elixirText.text = Mathf.Floor(_localPlayer.Elixir).ToString();
            }
        }

        private void UpdateHandCards()
        {
            for (int i = 0; i < 4; i++)
            {
                int cardId = i < _localPlayer.Hand.Length ? _localPlayer.Hand[i] : 0;
                
                if (cardId > 0)
                {
                    var cardData = Services.Get<DataManager>().GetCard(cardId);
                    _handCards[i].SetCard(cardData, _localPlayer.Elixir >= cardData.elixirCost);
                }
                else
                {
                    _handCards[i].ClearCard();
                }
            }
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

        // Called by InputManager
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
        }

        public void AnimateCardReturn(int index)
        {
            if (index >= 0 && index < 4)
            {
                _handCards[index].PlayReturnAnimation();
            }
        }
    }

    public class HandCardUI : MonoBehaviour
    {
        [SerializeField] private Image _cardImage;
        [SerializeField] private Text _elixirCostText;
        [SerializeField] private GameObject _selectionGlow;
        [SerializeField] private GameObject _unaffordableOverlay;
        [SerializeField] private CanvasGroup _canvasGroup;

        private RectTransform _rectTransform;
        private int _index;
        private HandBar _handBar;
        private CardData _cardData;

        public void Initialize(int index, HandBar handBar)
        {
            _index = index;
            _handBar = handBar;
            _rectTransform = GetComponent<RectTransform>();
        }

        public void SetCard(CardData cardData, bool affordable)
        {
            _cardData = cardData;
            
            if (_cardImage != null && cardData != null)
            {
                _cardImage.sprite = Services.Get<AssetManager>().LoadSprite(cardData.portraitId);
                _cardImage.enabled = true;
            }

            if (_elixirCostText != null)
            {
                _elixirCostText.text = cardData?.elixirCost.ToString() ?? "";
            }

            if (_unaffordableOverlay != null)
            {
                _unaffordableOverlay.SetActive(!affordable);
            }

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = affordable ? 1f : 0.6f;
                _canvasGroup.interactable = affordable;
                _canvasGroup.blocksRaycasts = affordable;
            }
        }

        public void ClearCard()
        {
            _cardData = null;
            if (_cardImage != null) _cardImage.enabled = false;
            if (_elixirCostText != null) _elixirCostText.text = "";
            if (_unaffordableOverlay != null) _unaffordableOverlay.SetActive(false);
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0.3f;
                _canvasGroup.interactable = false;
                _canvasGroup.blocksRaycasts = false;
            }
        }

        public void SetSelected(bool selected)
        {
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

        public void PlayInsufficientElixirAnimation()
        {
            // Shake animation
            StartCoroutine(ShakeAnimation());
        }

        public void PlayDeployAnimation()
        {
            // Scale down and fade
            StartCoroutine(DeployAnimationCoroutine());
        }

        public void PlayReturnAnimation()
        {
            // Bounce back
            StartCoroutine(ReturnAnimationCoroutine());
        }

        public bool IsPointerOver(Vector2 screenPos)
        {
            return RectTransformUtility.RectangleContainsScreenPoint(_rectTransform, screenPos);
        }

        private System.Collections.IEnumerator ShakeAnimation()
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

        private System.Collections.IEnumerator DeployAnimationCoroutine()
        {
            float duration = _handBar._deployAnimationDuration;
            float elapsed = 0f;
            Vector3 startScale = transform.localScale;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
                _canvasGroup.alpha = Mathf.Lerp(1f, 0f, t);
                yield return null;
            }
            
            transform.localScale = startScale;
            _canvasGroup.alpha = 1f;
        }

        private System.Collections.IEnumerator ReturnAnimationCoroutine()
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
    }
}