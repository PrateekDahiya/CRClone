using System;
using UnityEngine;
using UnityEngine.UI;
using CRClone.Core;
using CRClone.Data;
using CRClone.UI.Animation;

namespace CRClone.UI.Components
{
    public class CardDetailsModal : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image _cardArtImage;
        [SerializeField] private Text _cardNameText;
        [SerializeField] private Text _rarityText;
        [SerializeField] private Text _levelText;
        [SerializeField] private Button _closeButton;
        [SerializeField] private Button _addToDeckButton;
        [SerializeField] private Button _upgradeButton;
        [SerializeField] private Button _viewInShopButton;

        [Header("Stats Grid")]
        [SerializeField] private Text _elixirStatText;
        [SerializeField] private Text _hpStatText;
        [SerializeField] private Text _damageStatText;
        [SerializeField] private Text _dpsStatText;
        [SerializeField] private Text _hitSpeedStatText;
        [SerializeField] private Text _rangeStatText;
        [SerializeField] private Text _speedStatText;
        [SerializeField] private Text _targetStatText;

        [Header("Mechanics")]
        [SerializeField] private Transform _mechanicsContainer;
        [SerializeField] private GameObject _mechanicItemPrefab;
        [SerializeField] private Text _mechanicsText;

        [Header("Counters & Synergies")]
        [SerializeField] private Transform _countersContainer;
        [SerializeField] private Transform _synergiesContainer;
        [SerializeField] private GameObject _counterCardPrefab;

        [Header("Upgrade Path")]
        [SerializeField] private Transform _upgradePathContainer;
        [SerializeField] private GameObject _upgradeLevelPrefab;
        [SerializeField] private Text _upgradeCostText;

        [Header("Animation")]
        [SerializeField] private float _openDuration = 0.2f;
        [SerializeField] private float _closeDuration = 0.15f;
        [SerializeField] private CanvasGroup _modalCanvasGroup;
        [SerializeField] private RectTransform _modalRect;

        private CardData _cardData;
        private DeckBuilderUI _deckBuilder;
        private int _currentLevel = 1;
        private CardLevelStats _currentStats;

        public void Initialize(CardData cardData, DeckBuilderUI deckBuilder)
        {
            _cardData = cardData;
            _deckBuilder = deckBuilder;
            _currentLevel = GetCardLevel(cardData.cardId);
            _currentStats = cardData.GetStats(_currentLevel);

            SetupUI();
            StartCoroutine(OpenAnimation());
        }

        private void SetupUI()
        {
            if (_modalCanvasGroup == null) _modalCanvasGroup = GetComponent<CanvasGroup>();
            if (_modalRect == null) _modalRect = GetComponent<RectTransform>();

            _closeButton?.onClick.AddListener(Close);
            _addToDeckButton?.onClick.AddListener(AddToDeck);
            _upgradeButton?.onClick.AddListener(UpgradeCard);
            _viewInShopButton?.onClick.AddListener(ViewInShop);

            UpdateCardInfo();
            UpdateStats();
            UpdateMechanics();
            UpdateCountersAndSynergies();
            UpdateUpgradePath();

            var playerData = Services.Get<GameManager>().LocalPlayer;
            bool canUpgrade = playerData != null && 
                             playerData.collection.ContainsKey(_cardData.cardId) && 
                             playerData.collection[_cardData.cardId] >= _currentStats.cardsRequired &&
                             playerData.gold >= _currentStats.goldCost &&
                             _currentLevel < 14;

            _upgradeButton?.gameObject.SetActive(canUpgrade);
            _upgradeButton?.interactable = canUpgrade;
        }

        private void UpdateCardInfo()
        {
            if (_cardNameText != null) _cardNameText.text = _cardData.cardName;
            
            if (_rarityText != null)
            {
                _rarityText.text = _cardData.rarity.ToString();
                _rarityText.color = GetRarityColor(_cardData.rarity);
            }

            if (_levelText != null) _levelText.text = $"Level {_currentLevel}";

            if (_cardArtImage != null)
            {
                _cardArtImage.sprite = Services.Get<AssetManager>().LoadSprite(_cardData.portraitId);
            }
        }

        private void UpdateStats()
        {
            if (_currentStats == null) return;

            if (_elixirStatText != null) _elixirStatText.text = _cardData.elixirCost.ToString();
            if (_hpStatText != null) _hpStatText.text = _currentStats.hitpoints.ToString("N0");
            if (_damageStatText != null) _damageStatText.text = _currentStats.damage.ToString("N0");
            
            float dps = _currentStats.hitSpeed > 0 ? _currentStats.damage / _currentStats.hitSpeed : 0;
            if (_dpsStatText != null) _dpsStatText.text = dps.ToString("F1");
            if (_hitSpeedStatText != null) _hitSpeedStatText.text = $"{_currentStats.hitSpeed:F1}s";
            if (_rangeStatText != null) _rangeStatText.text = $"{_currentStats.range:F1}";
            if (_speedStatText != null) _speedStatText.text = _cardData.speed.ToString();
            if (_targetStatText != null) _targetStatText.text = _cardData.targetType.ToString();
        }

        private void UpdateMechanics()
        {
            if (_mechanicsText != null && !string.IsNullOrEmpty(_cardData.mechanicsJson))
            {
                _mechanicsText.text = _cardData.mechanicsJson;
            }
        }

        private void UpdateCountersAndSynergies()
        {
        }

        private void UpdateUpgradePath()
        {
            if (_upgradePathContainer == null || _upgradeLevelPrefab == null) return;

            foreach (Transform child in _upgradePathContainer)
            {
                Destroy(child.gameObject);
            }

            var playerData = Services.Get<GameManager>().LocalPlayer;
            int ownedCount = playerData?.collection.ContainsKey(_cardData.cardId) == true ? playerData.collection[_cardData.cardId] : 0;

            for (int level = _currentLevel + 1; level <= Mathf.Min(_currentLevel + 3, 14); level++)
            {
                var stats = _cardData.GetStats(level);
                var levelGO = Instantiate(_upgradeLevelPrefab, _upgradePathContainer);
                var levelUI = levelGO.GetComponent<UpgradeLevelUI>();
                if (levelUI != null)
                {
                    bool canAfford = ownedCount >= stats.cardsRequired && (playerData?.gold ?? 0) >= stats.goldCost;
                    levelUI.Initialize(level, stats, canAfford);
                }
            }
        }

        private int GetCardLevel(int cardId)
        {
            var playerData = Services.Get<GameManager>().LocalPlayer;
            if (playerData?.cardLevels != null && playerData.cardLevels.ContainsKey(cardId))
            {
                return playerData.cardLevels[cardId];
            }
            return 1;
        }

        private Color GetRarityColor(CardRarity rarity)
        {
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

        private void AddToDeck()
        {
            _deckBuilder?.ShowCardDetails(_cardData);
            Close();
        }

        private void UpgradeCard()
        {
            var playerData = Services.Get<GameManager>().LocalPlayer;
            if (playerData == null) return;

            int cost = _currentStats.goldCost;
            int cardsNeeded = _currentStats.cardsRequired;

            if (playerData.gold >= cost && playerData.collection[_cardData.cardId] >= cardsNeeded)
            {
                playerData.gold -= cost;
                playerData.collection[_cardData.cardId] -= cardsNeeded;
                playerData.cardLevels[_cardData.cardId] = _currentLevel + 1;

                EventBus.Raise(new EventBus.CardUpgradedEvent
                {
                    cardId = _cardData.cardId,
                    oldLevel = _currentLevel,
                    newLevel = _currentLevel + 1
                });

                UISoundPlayer.Instance?.PlaySuccess();
                EventBus.RaiseToast($"{_cardData.cardName} upgraded to Level {_currentLevel + 1}!");

                _currentLevel++;
                _currentStats = _cardData.GetStats(_currentLevel);
                SetupUI();
            }
            else
            {
                UISoundPlayer.Instance?.PlayError();
                EventBus.RaiseToast("Not enough resources to upgrade!");
            }
        }

        private void ViewInShop()
        {
            Close();
            Services.Get<GameManager>().ChangeState(GameState.Shop);
        }

        private void Close()
        {
            StartCoroutine(CloseAnimation());
        }

        private System.Collections.IEnumerator OpenAnimation()
        {
            if (_modalCanvasGroup == null) yield break;

            _modalCanvasGroup.alpha = 0f;
            _modalRect.localScale = Vector3.one * 0.8f;

            yield return _modalCanvasGroup.FadeTo(1f, _openDuration);
            yield return _modalRect.ScaleTo(Vector3.one, _openDuration, AnimationCurves.EaseOutBack);
        }

        private System.Collections.IEnumerator CloseAnimation()
        {
            if (_modalCanvasGroup == null)
            {
                Destroy(gameObject);
                yield break;
            }

            yield return _modalCanvasGroup.FadeTo(0f, _closeDuration);
            yield return _modalRect.ScaleTo(Vector3.one * 0.8f, _closeDuration, AnimationCurves.EaseInBack);

            _deckBuilder?.CloseCardDetails();
            Destroy(gameObject);
        }
    }

    public class UpgradeLevelUI : MonoBehaviour
    {
        [SerializeField] private Text _levelText;
        [SerializeField] private Text _hpText;
        [SerializeField] private Text _damageText;
        [SerializeField] private Text _costText;
        [SerializeField] private Button _upgradeButton;

        public void Initialize(int level, CardLevelStats stats, bool canAfford)
        {
            if (_levelText != null) _levelText.text = $"Level {level}";
            if (_hpText != null) _hpText.text = $"HP: {stats.hitpoints:N0}";
            if (_damageText != null) _damageText.text = $"DMG: {stats.damage:N0}";
            if (_costText != null) _costText.text = $"{stats.cardsRequired} cards + {stats.goldCost:N0} gold";

            if (_upgradeButton != null)
            {
                _upgradeButton.interactable = canAfford;
            }
        }
    }
}