using System;
using UnityEngine;
using UnityEngine.UI;
using CRClone.Core;
using CRClone.Data;

namespace CRClone.UI.Components
{
    public class DonateRequestModal : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Dropdown _cardDropdown;
        [SerializeField] private Slider _countSlider;
        [SerializeField] private Text _countText;
        [SerializeField] private Button _confirmButton;
        [SerializeField] private Button _cancelButton;

        private Action<int, int> _onConfirmed;

        private void Awake()
        {
            _confirmButton.OrNull()?.onClick.AddListener(OnConfirm);
            _cancelButton.OrNull()?.onClick.AddListener(OnCancel);
            _countSlider.OrNull()?.onValueChanged.AddListener(OnCountChanged);
        }

        public void Initialize(Action<int, int> onConfirmed)
        {
            _onConfirmed = onConfirmed;
            PopulateCardDropdown();
        }

        private void PopulateCardDropdown()
        {
            if (_cardDropdown == null) return;

            var playerData = Services.Get<GameManager>().LocalPlayer;
            var dataManager = Services.Get<DataManager>();

            _cardDropdown.ClearOptions();
            var options = new System.Collections.Generic.List<string>();

            foreach (var card in dataManager.GetAllCards())
            {
                if (!card.isEnabled) continue;
                if (playerData?.collection.ContainsKey(card.cardId) != true) continue;

                int count = playerData.collection[card.cardId];
                int maxDonate = GetMaxDonation(card.rarity);
                if (count < maxDonate) continue;

                options.Add($"{card.cardName} (x{count})");
            }

            _cardDropdown.AddOptions(options);
            _cardDropdown.value = 0;

            if (options.Count == 0)
            {
                _cardDropdown.interactable = false;
                _confirmButton.interactable = false;
            }
        }

        private void OnCountChanged(float value)
        {
            int count = Mathf.RoundToInt(value);
            if (_countText != null) _countText.text = $"Count: {count}";
        }

        private void OnConfirm()
        {
            if (_cardDropdown == null || _cardDropdown.options.Count == 0) return;

            int selectedIndex = _cardDropdown.value;
            int count = Mathf.RoundToInt(_countSlider.OrNull()?.value ?? 1);

            // Get the card ID from the selected index
            var playerData = Services.Get<GameManager>().LocalPlayer;
            var dataManager = Services.Get<DataManager>();
            int cardId = -1;
            int optionIndex = 0;

            foreach (var card in dataManager.GetAllCards())
            {
                if (!card.isEnabled) continue;
                if (playerData?.collection.ContainsKey(card.cardId) != true) continue;

                int ownedCount = playerData.collection[card.cardId];
                int maxDonate = GetMaxDonation(card.rarity);
                if (ownedCount < maxDonate) continue;

                if (optionIndex == selectedIndex)
                {
                    cardId = card.cardId;
                    break;
                }
                optionIndex++;
            }

            if (cardId > 0)
            {
                _onConfirmed?.Invoke(cardId, count);
                Destroy(gameObject);
            }
        }

        private void OnCancel()
        {
            Destroy(gameObject);
        }

        private int GetMaxDonation(CardRarity rarity)
        {
            return rarity switch
            {
                CardRarity.Common => 10,
                CardRarity.Rare => 1,
                CardRarity.Epic => 0,
                CardRarity.Legendary => 0,
                CardRarity.Champion => 0,
                _ => 0
            };
        }
    }
}