using System;
using UnityEngine;
using UnityEngine.UI;
using CRClone.Core;
using CRClone.Data;
using CRClone.UI.Animation;
using CRClone.UI.Screens;

namespace CRClone.UI.Components
{
    public class ShopOfferItemUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image _iconImage;
        [SerializeField] private Text _nameText;
        [SerializeField] private Text _descriptionText;
        [SerializeField] private Text _costText;
        [SerializeField] private Image _costCurrencyIcon;
        [SerializeField] private Button _purchaseButton;
        [SerializeField] private Text _purchaseButtonText;
        [SerializeField] private Image _rarityFrame;
        [SerializeField] private GameObject _ownedBadge;

        private ShopOffer _offer;
        private Action<ShopOffer> _onPurchaseClicked;

        public void Initialize(ShopOffer offer, Action<ShopOffer> onPurchaseClicked)
        {
            _offer = offer;
            _onPurchaseClicked = onPurchaseClicked;

            _purchaseButton?.onClick.AddListener(() => _onPurchaseClicked?.Invoke(_offer));

            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            if (_offer == null) return;

            if (_nameText != null) _nameText.text = _offer.name;
            if (_descriptionText != null) _descriptionText.text = _offer.description;
            if (_costText != null) _costText.text = _offer.cost.ToString("N0");

            if (_iconImage != null && _offer.iconSprite != null)
            {
                _iconImage.sprite = _offer.iconSprite;
            }

            if (_costCurrencyIcon != null)
            {
                // Set gold/gem icon based on cost type
            }

            if (_rarityFrame != null && _offer.rewardType == RewardType.Card)
            {
                var cardData = Services.Get<DataManager>().GetCard(_offer.rewardCardId);
                if (cardData != null)
                {
                    _rarityFrame.color = GetRarityColor(cardData.rarity);
                }
            }

            if (_purchaseButtonText != null)
            {
                _purchaseButtonText.text = "BUY";
            }

            if (_ownedBadge != null)
            {
                var playerData = Services.Get<GameManager>().LocalPlayer;
                bool owned = _offer.rewardType == RewardType.Card && 
                            playerData?.collection.ContainsKey(_offer.rewardCardId) == true;
                _ownedBadge.SetActive(owned);
            }
        }

        private Color GetRarityColor(CardRarity rarity)
        {
            if (AccessibilityManager.Instance != null)
            {
                return AccessibilityManager.Instance.GetRarityColor(rarity);
            }
            return rarity switch
            {
                CardRarity.Common => new Color(0.62f, 0.62f, 0.62f),
                CardRarity.Rare => new Color(0.13f, 0.59f, 0.95f),
                CardRarity.Epic => new Color(0.61f, 0.15f, 0.69f),
                CardRarity.Legendary => new Color(1f, 0.6f, 0f),
                CardRarity.Champion => new Color(0.91f, 0.12f, 0.39f),
                _ => Color.white
            };
        }
    }
}