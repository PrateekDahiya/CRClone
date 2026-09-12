using System;
using UnityEngine;
using UnityEngine.UI;
using CRClone.UI.Animation;

namespace CRClone.UI.Components
{
    public class OfferItemUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image _iconImage;
        [SerializeField] private Text _titleText;
        [SerializeField] private Text _descriptionText;
        [SerializeField] private Text _costText;
        [SerializeField] private Image _currencyIcon;
        [SerializeField] private Button _purchaseButton;
        [SerializeField] private Text _purchaseButtonText;
        [SerializeField] private GameObject _soldOutOverlay;
        [SerializeField] private GameObject _timerContainer;
        [SerializeField] private Text _timerText;
        [SerializeField] private Image _rarityFrame;

        private ShopScreen.ShopOffer _offer;
        private Action<ShopScreen.ShopOffer> _onPurchaseClicked;
        private Coroutine _timerCoroutine;

        public void Initialize(ShopScreen.ShopOffer offer, Action<ShopScreen.ShopOffer> onPurchaseClicked)
        {
            _offer = offer;
            _onPurchaseClicked = onPurchaseClicked;

            _purchaseButton?.onClick.AddListener(() => _onPurchaseClicked?.Invoke(_offer));

            UpdateVisuals();
            StartTimer();
        }

        private void UpdateVisuals()
        {
            if (_offer == null) return;

            if (_titleText != null) _titleText.text = _offer.title;
            if (_costText != null) _costText.text = _offer.cost.ToString("N0");

            if (_iconImage != null && _offer.iconSprite != null)
            {
                _iconImage.sprite = _offer.iconSprite;
            }

            if (_purchaseButton != null)
            {
                _purchaseButton.interactable = !_offer.purchased;
                if (_purchaseButtonText != null)
                {
                    _purchaseButtonText.text = _offer.purchased ? "PURCHASED" : "BUY";
                }
            }

            if (_soldOutOverlay != null)
            {
                _soldOutOverlay.SetActive(_offer.purchased);
            }

            if (_timerContainer != null)
            {
                _timerContainer.gameObject.SetActive(_offer.timeRemaining > TimeSpan.Zero);
            }

            if (_rarityFrame != null && _offer.type == ShopScreen.OfferType.Card)
            {
                var cardData = Services.Get<DataManager>().GetCard(_offer.cardId);
                if (cardData != null)
                {
                    Color frameColor = cardData.rarity switch
                    {
                        CardRarity.Common => new Color(0.62f, 0.62f, 0.62f),
                        CardRarity.Rare => new Color(0.13f, 0.59f, 0.95f),
                        CardRarity.Epic => new Color(0.61f, 0.15f, 0.69f),
                        CardRarity.Legendary => new Color(1f, 0.6f, 0f),
                        CardRarity.Champion => new Color(0.91f, 0.12f, 0.39f),
                        _ => Color.white
                    };
                    _rarityFrame.color = frameColor;
                }
            }

            if (_descriptionText != null)
            {
                switch (_offer.type)
                {
                    case ShopScreen.OfferType.Card:
                        var cardData = Services.Get<DataManager>().GetCard(_offer.cardId);
                        _descriptionText.text = cardData != null ? $"{cardData.cardName} x{_offer.cardCount}" : "";
                        break;
                    case ShopScreen.OfferType.Gold:
                        _descriptionText.text = $"{_offer.goldAmount:N0} Gold";
                        break;
                    case ShopScreen.OfferType.Gems:
                        _descriptionText.text = $"{_offer.gemAmount:N0} Gems";
                        break;
                    case ShopScreen.OfferType.Chest:
                        _descriptionText.text = "Chest";
                        break;
                    case ShopScreen.OfferType.WildCard:
                        _descriptionText.text = $"Wild Card x{_offer.cardCount}";
                        break;
                }
            }

            if (_currencyIcon != null)
            {
            }
        }

        private void StartTimer()
        {
            if (_timerCoroutine != null) StopCoroutine(_timerCoroutine);
            if (_offer.timeRemaining > TimeSpan.Zero)
            {
                _timerCoroutine = StartCoroutine(TimerRoutine());
            }
        }

        private System.Collections.IEnumerator TimerRoutine()
        {
            while (_offer.timeRemaining > TimeSpan.Zero)
            {
                if (_timerText != null)
                {
                    _timerText.text = FormatTime(_offer.timeRemaining);
                }
                yield return new WaitForSeconds(1f);
                _offer.timeRemaining = _offer.timeRemaining.Subtract(TimeSpan.FromSeconds(1));
            }

            if (_timerContainer != null)
            {
                _timerContainer.gameObject.SetActive(false);
            }
        }

        private string FormatTime(TimeSpan time)
        {
            if (time.TotalHours >= 1)
                return $"{(int)time.TotalHours}h {time.Minutes}m";
            else
                return $"{time.Minutes}m {time.Seconds}s";
        }

        private void OnDestroy()
        {
            if (_timerCoroutine != null) StopCoroutine(_timerCoroutine);
        }
    }
}