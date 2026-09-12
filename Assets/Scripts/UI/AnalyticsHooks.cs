using System;
using System.Collections.Generic;
using UnityEngine;

namespace CRClone.UI
{
    public class AnalyticsHooks : MonoBehaviour
    {
        public static AnalyticsHooks Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private bool _enabled = true;
        [SerializeField] private bool _debugLog = false;

        private Dictionary<string, object> _sessionData = new Dictionary<string, object>();
        private float _sessionStartTime;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _sessionStartTime = Time.realtimeSinceStartup;
            _sessionData["session_id"] = Guid.NewGuid().ToString();
            _sessionData["start_time"] = DateTime.UtcNow.ToString("o");
            _sessionData["platform"] = Application.platform.ToString();
            _sessionData["device_model"] = SystemInfo.deviceModel;
            _sessionData["os_version"] = SystemInfo.operatingSystem;
            _sessionData["graphics_device"] = SystemInfo.graphicsDeviceName;
            _sessionData["memory_mb"] = SystemInfo.systemMemorySize;
        }

        private void Start()
        {
            TrackEvent("session_start");
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                TrackEvent("session_pause", new Dictionary<string, object>
                {
                    { "session_duration", Time.realtimeSinceStartup - _sessionStartTime }
                });
            }
            else
            {
                TrackEvent("session_resume");
            }
        }

        private void OnApplicationQuit()
        {
            TrackEvent("session_end", new Dictionary<string, object>
            {
                { "session_duration", Time.realtimeSinceStartup - _sessionStartTime }
            });
        }

        public void TrackEvent(string eventName, Dictionary<string, object> parameters = null)
        {
            if (!_enabled) return;

            var eventData = new Dictionary<string, object>(_sessionData)
            {
                { "event_name", eventName },
                { "timestamp", DateTime.UtcNow.ToString("o") },
                { "session_time", Time.realtimeSinceStartup - _sessionStartTime }
            };

            if (parameters != null)
            {
                foreach (var kvp in parameters)
                {
                    eventData[kvp.Key] = kvp.Value;
                }
            }

            if (_debugLog)
            {
                Debug.Log($"[Analytics] {eventName}: {MiniJSON.Json.Serialize(eventData)}");
            }

            SendToAnalyticsProvider(eventData);
        }

        public void TrackBattleStart(BattleType battleType, int[] deck1, int[] deck2, int playerTrophies, int opponentTrophies)
        {
            TrackEvent("battle_start", new Dictionary<string, object>
            {
                { "battle_type", battleType.ToString() },
                { "deck1_hash", GetDeckHash(deck1) },
                { "deck2_hash", GetDeckHash(deck2) },
                { "player_trophies", playerTrophies },
                { "opponent_trophies", opponentTrophies },
                { "player_deck_avg_elixir", CalculateAvgElixir(deck1) },
                { "opponent_deck_avg_elixir", CalculateAvgElixir(deck2) }
            });
        }

        public void TrackCardPlayed(int cardId, Vector2 position, int elixirCost, float battleTime, int playerElixirBefore, int playerElixirAfter)
        {
            TrackEvent("card_played", new Dictionary<string, object>
            {
                { "card_id", cardId },
                { "position_x", position.x },
                { "position_y", position.y },
                { "elixir_cost", elixirCost },
                { "battle_time", battleTime },
                { "elixir_before", playerElixirBefore },
                { "elixir_after", playerElixirAfter },
                { "elixir_efficiency", elixirCost > 0 ? 1f / elixirCost : 0f }
            });
        }

        public void TrackSpellCast(int spellId, Vector2 position, Vector2? targetPosition, int targetsHit, float valueGenerated, float battleTime)
        {
            TrackEvent("spell_cast", new Dictionary<string, object>
            {
                { "spell_id", spellId },
                { "position_x", position.x },
                { "position_y", position.y },
                { "target_x", targetPosition?.x ?? 0 },
                { "target_y", targetPosition?.y ?? 0 },
                { "targets_hit", targetsHit },
                { "value_generated", valueGenerated },
                { "battle_time", battleTime }
            });
        }

        public void TrackTowerDamaged(int towerType, int damage, int remainingHP, float battleTime, int sourceCardId = 0)
        {
            TrackEvent("tower_damaged", new Dictionary<string, object>
            {
                { "tower_type", towerType },
                { "damage", damage },
                { "remaining_hp", remainingHP },
                { "battle_time", battleTime },
                { "source_card_id", sourceCardId }
            });
        }

        public void TrackTowerDestroyed(int towerType, float battleTime, int sourceCardId = 0)
        {
            TrackEvent("tower_destroyed", new Dictionary<string, object>
            {
                { "tower_type", towerType },
                { "battle_time", battleTime },
                { "source_card_id", sourceCardId }
            });
        }

        public void TrackBattleEnd(BattleResult result, int playerCrowns, int opponentCrowns, int trophyChange, float duration, bool wentOvertime, long replayId)
        {
            TrackEvent("battle_end", new Dictionary<string, object>
            {
                { "result", result.ToString() },
                { "player_crowns", playerCrowns },
                { "opponent_crowns", opponentCrowns },
                { "trophy_change", trophyChange },
                { "duration", duration },
                { "went_overtime", wentOvertime },
                { "replay_id", replayId },
                { "is_three_crown", playerCrowns == 3 || opponentCrowns == 3 }
            });
        }

        public void TrackDeckSave(int[] deck)
        {
            TrackEvent("deck_save", new Dictionary<string, object>
            {
                { "deck_hash", GetDeckHash(deck) },
                { "avg_elixir", CalculateAvgElixir(deck) },
                { "has_champion", HasChampion(deck) },
                { "card_types", GetCardTypeDistribution(deck) }
            });
        }

        public void TrackCardUpgrade(int cardId, int fromLevel, int toLevel, int goldCost, int cardsUsed)
        {
            TrackEvent("card_upgrade", new Dictionary<string, object>
            {
                { "card_id", cardId },
                { "from_level", fromLevel },
                { "to_level", toLevel },
                { "gold_cost", goldCost },
                { "cards_used", cardsUsed }
            });
        }

        public void TrackShopPurchase(string itemId, string itemType, int cost, string currency, int quantity)
        {
            TrackEvent("shop_purchase", new Dictionary<string, object>
            {
                { "item_id", itemId },
                { "item_type", itemType },
                { "cost", cost },
                { "currency", currency },
                { "quantity", quantity }
            });
        }

        public void TrackClanJoin(string clanId, string clanName)
        {
            TrackEvent("clan_join", new Dictionary<string, object>
            {
                { "clan_id", clanId },
                { "clan_name", clanName }
            });
        }

        public void TrackClanCreate(string clanId, string clanName)
        {
            TrackEvent("clan_create", new Dictionary<string, object>
            {
                { "clan_id", clanId },
                { "clan_name", clanName }
            });
        }

        public void TrackDonation(int cardId, int count, string recipientId)
        {
            TrackEvent("donation", new Dictionary<string, object>
            {
                { "card_id", cardId },
                { "count", count },
                { "recipient_id", recipientId }
            });
        }

        public void TrackScreenView(string screenName, string previousScreen = null)
        {
            TrackEvent("screen_view", new Dictionary<string, object>
            {
                { "screen_name", screenName },
                { "previous_screen", previousScreen ?? "none" }
            });
        }

        public void TrackButtonClick(string buttonId, string screenName)
        {
            TrackEvent("button_click", new Dictionary<string, object>
            {
                { "button_id", buttonId },
                { "screen_name", screenName }
            });
        }

        public void TrackError(string errorCode, string errorMessage, string context)
        {
            TrackEvent("error", new Dictionary<string, object>
            {
                { "error_code", errorCode },
                { "error_message", errorMessage },
                { "context", context }
            });
        }

        public void TrackPerformance(string metricName, float value, string unit)
        {
            TrackEvent("performance", new Dictionary<string, object>
            {
                { "metric_name", metricName },
                { "value", value },
                { "unit", unit }
            });
        }

        public void SetUserProperty(string key, object value)
        {
            _sessionData[key] = value;
        }

        public void SetUserId(string userId)
        {
            _sessionData["user_id"] = userId;
        }

        private void SendToAnalyticsProvider(Dictionary<string, object> eventData)
        {
        }

        private string GetDeckHash(int[] deck)
        {
            if (deck == null) return "empty";
            Array.Sort(deck);
            return string.Join(",", deck);
        }

        private float CalculateAvgElixir(int[] deck)
        {
            if (deck == null || deck.Length == 0) return 0f;

            var dataManager = Services.Get<DataManager>();
            if (dataManager == null) return 0f;

            float total = 0f;
            int count = 0;

            foreach (int cardId in deck)
            {
                if (cardId > 0)
                {
                    var card = dataManager.GetCard(cardId);
                    if (card != null)
                    {
                        total += card.elixirCost;
                        count++;
                    }
                }
            }

            return count > 0 ? total / count : 0f;
        }

        private bool HasChampion(int[] deck)
        {
            if (deck == null) return false;

            var dataManager = Services.Get<DataManager>();
            if (dataManager == null) return false;

            foreach (int cardId in deck)
            {
                if (cardId > 0)
                {
                    var card = dataManager.GetCard(cardId);
                    if (card?.rarity == CardRarity.Champion) return true;
                }
            }
            return false;
        }

        private string GetCardTypeDistribution(int[] deck)
        {
            if (deck == null) return "{}";

            var dataManager = Services.Get<DataManager>();
            if (dataManager == null) return "{}";

            var counts = new Dictionary<CardType, int>();

            foreach (int cardId in deck)
            {
                if (cardId > 0)
                {
                    var card = dataManager.GetCard(cardId);
                    if (card != null)
                    {
                        if (!counts.ContainsKey(card.type))
                            counts[card.type] = 0;
                        counts[card.type]++;
                    }
                }
            }

            var parts = new List<string>();
            foreach (var kvp in counts)
            {
                parts.Add($"{kvp.Key}:{kvp.Value}");
            }
            return "{" + string.Join(",", parts) + "}";
        }
    }

    public enum BattleType
    {
        Ladder,
        TwoVTwo,
        Tournament,
        Friendly,
        Practice
    }

    public enum BattleResult
    {
        Victory,
        Defeat,
        Draw
    }
}

namespace MiniJSON
{
    public static class Json
    {
        public static string Serialize(object obj)
        {
            return UnityEngine.JsonUtility.ToJson(obj);
        }

        public static T Deserialize<T>(string json)
        {
            return UnityEngine.JsonUtility.FromJson<T>(json);
        }
    }
}