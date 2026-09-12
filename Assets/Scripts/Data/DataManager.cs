using System;
using System.Collections.Generic;
using UnityEngine;
using CRClone.Data;
using CRClone.Core;

namespace CRClone.Data
{
    public class DataManager : MonoBehaviour
    {
        private Dictionary<int, CardData> _cardDatabase = new();
        private Dictionary<int, CardData> _cardsById = new();
        private List<CardData> _allCards = new();
        private GameConfig _config;

        public void Initialize()
        {
            LoadCardDatabase();
        }

        private void LoadCardDatabase()
        {
            // Load from Resources
            var cards = Resources.LoadAll<CardData>("Data/Cards");
            foreach (var card in cards)
            {
                _cardDatabase[card.cardId] = card;
                _cardsById[card.cardId] = card;
                _allCards.Add(card);
            }

            Debug.Log($"[DataManager] Loaded {_allCards.Count} cards");

            // Build level stats for each card
            foreach (var card in _allCards)
            {
                BuildLevelStats(card);
            }
        }

        private void BuildLevelStats(CardData card)
        {
            card.levelStats.Clear();
            var config = Services.Get<ConfigManager>().GetConfig();

            for (int level = 1; level <= config.maxCardLevel; level++)
            {
                float multiplier = Mathf.Pow(config.levelStatMultiplier, level - 1);
                
                var stats = new CardLevelStats
                {
                    level = level,
                    hitpoints = Mathf.RoundToInt(card.baseHitpoints * multiplier),
                    damage = Mathf.RoundToInt(card.baseDamage * multiplier),
                    hitSpeed = card.baseHitSpeed, // Hit speed usually doesn't scale
                    range = card.baseRange,
                    goldCost = CalculateGoldCost(card.rarity, level),
                    cardsRequired = CalculateCardsRequired(card.rarity, level)
                };

                card.levelStats[level] = stats;
            }
        }

        private int CalculateGoldCost(CardRarity rarity, int level)
        {
            // Base costs per rarity (approximate Clash Royale values)
            int baseCost = rarity switch
            {
                CardRarity.Common => 50,
                CardRarity.Rare => 250,
                CardRarity.Epic => 1000,
                CardRarity.Legendary => 20000,
                CardRarity.Champion => 50000,
                _ => 100
            };

            // Exponential scaling
            return Mathf.RoundToInt(baseCost * Mathf.Pow(1.5f, level - 1));
        }

        private int CalculateCardsRequired(CardRarity rarity, int level)
        {
            int baseCards = rarity switch
            {
                CardRarity.Common => 2,
                CardRarity.Rare => 2,
                CardRarity.Epic => 2,
                CardRarity.Legendary => 1,
                CardRarity.Champion => 1,
                _ => 2
            };

            // Scaling
            return Mathf.RoundToInt(baseCards * Mathf.Pow(1.8f, level - 1));
        }

        public CardData GetCard(int cardId)
        {
            _cardsById.TryGetValue(cardId, out var card);
            return card;
        }

        public CardData GetCardByName(string name)
        {
            foreach (var card in _allCards)
            {
                if (card.cardName == name) return card;
            }
            return null;
        }

        public IReadOnlyList<CardData> GetAllCards() => _allCards;

        public IReadOnlyList<CardData> GetCardsByRarity(CardRarity rarity)
        {
            var result = new List<CardData>();
            foreach (var card in _allCards)
            {
                if (card.rarity == rarity) result.Add(card);
            }
            return result;
        }

        public IReadOnlyList<CardData> GetCardsByType(CardType type)
        {
            var result = new List<CardData>();
            foreach (var card in _allCards)
            {
                if (card.type == type) result.Add(card);
            }
            return result;
        }

        public CardLevelStats GetCardStats(int cardId, int level)
        {
            var card = GetCard(cardId);
            return card?.GetStats(level);
        }

        public bool ValidateDeck(int[] cardIds, out string error)
        {
            error = null;

            if (cardIds == null || cardIds.Length != 8)
            {
                error = "Deck must have exactly 8 cards";
                return false;
            }

            int championCount = 0;
            var config = Services.Get<ConfigManager>().GetConfig();

            for (int i = 0; i < cardIds.Length; i++)
            {
                int id = cardIds[i];
                if (id <= 0) continue; // Empty slot

                var card = GetCard(id);
                if (card == null)
                {
                    error = $"Invalid card ID: {id}";
                    return false;
                }

                if (!card.isEnabled)
                {
                    error = $"Card {card.cardName} is disabled";
                    return false;
                }

                if (card.rarity == CardRarity.Champion)
                    championCount++;
            }

            if (championCount > config.maxChampionsPerDeck)
            {
                error = $"Maximum {config.maxChampionsPerDeck} champion(s) per deck";
                return false;
            }

            return true;
        }

        public float CalculateAvgElixir(int[] cardIds)
        {
            float total = 0;
            int count = 0;
            foreach (int id in cardIds)
            {
                if (id > 0)
                {
                    var card = GetCard(id);
                    if (card != null)
                    {
                        total += card.elixirCost;
                        count++;
                    }
                }
            }
            return count > 0 ? total / count : 0;
        }

        // Battle result saving
        public void SaveBattleResult(BattleResult result)
        {
            // TODO: Send to server / save locally
            Debug.Log($"[DataManager] Battle saved: {result.result}, Crowns: {result.player1Crowns}-{result.player2Crowns}");
        }
    }
}