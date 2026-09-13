using System;
using System.Collections.Generic;
using UnityEngine;

namespace CRClone.Core
{
    public static class Services
    {
        private static readonly Dictionary<Type, object> _services = new();
        private static readonly Dictionary<Type, object> _singletons = new();

        public static void Register<T>(T service) where T : class
        {
            var type = typeof(T);
            if (_services.ContainsKey(type))
            {
                Debug.LogWarning($"[Services] Overwriting existing service: {type.Name}");
            }
            _services[type] = service;
        }

        public static void RegisterSingleton<T>(T service) where T : class
        {
            var type = typeof(T);
            if (_singletons.ContainsKey(type))
            {
                Debug.LogWarning($"[Services] Overwriting existing singleton: {type.Name}");
            }
            _singletons[type] = service;
            _services[type] = service;
        }

        public static T Get<T>() where T : class
        {
            var type = typeof(T);
            if (_services.TryGetValue(type, out var service))
                return service as T;
            
            if (_singletons.TryGetValue(type, out var singleton))
                return singleton as T;

            return null;
        }

        public static bool TryGet<T>(out T service) where T : class
        {
            var type = typeof(T);
            if (_services.TryGetValue(type, out var s))
            {
                service = s as T;
                return true;
            }
            if (_singletons.TryGetValue(type, out var single))
            {
                service = single as T;
                return true;
            }
            service = null;
            return false;
        }

        public static void Unregister<T>() where T : class
        {
            _services.Remove(typeof(T));
            _singletons.Remove(typeof(T));
        }

        public static void Clear()
        {
            _services.Clear();
            _singletons.Clear();
        }

        public static bool Has<T>() where T : class => _services.ContainsKey(typeof(T)) || _singletons.ContainsKey(typeof(T));
    }
}

namespace CRClone.Core
{
    public static class EventBus
    {
        // Battle Events
        public static event Action<CardPlayedEvent> OnCardPlayed;
        public static event Action<UnitSpawnedEvent> OnUnitSpawned;
        public static event Action<UnitDiedEvent> OnUnitDied;
        public static event Action<BuildingPlacedEvent> OnBuildingPlaced;
        public static event Action<BuildingDestroyedEvent> OnBuildingDestroyed;
        public static event Action<SpellCastEvent> OnSpellCast;
        public static event Action<TowerDamagedEvent> OnTowerDamaged;
        public static event Action<TowerDestroyedEvent> OnTowerDestroyed;
        public static event Action<KingTowerActivatedEvent> OnKingTowerActivated;
        public static event Action<BattleEndedEvent> OnBattleEnded;
        public static event Action<BattleTickEvent> OnBattleTick;
        public static event Action<ElixirChangedEvent> OnElixirChanged;
        public static event Action<ChampionAbilityUsedEvent> OnChampionAbilityUsed;

        // Network Events
        public static event Action<NetworkConnectedEvent> OnNetworkConnected;
        public static event Action<NetworkDisconnectedEvent> OnNetworkDisconnected;
        public static event Action<NetworkMessageEvent> OnNetworkMessage;
        public static event Action<ReconciliationEvent> OnReconciliation;

        // UI Events
        public static event Action<GameState> OnGameStateChanged;
        public static event Action<ScreenType> OnScreenChanged;
        public static event Action<string, object> OnToast;
        public static event Action<string> OnError;
        public static event Action<float> OnLoadingProgress;

        // Player Events
        public static event Action<PlayerDataChangedEvent> OnPlayerDataChanged;
        public static event Action<CardUnlockedEvent> OnCardUnlocked;
        public static event Action<CardUpgradedEvent> OnCardUpgraded;
        public static event Action<ChestUnlockedEvent> OnChestUnlocked;
        public static event Action<QuestCompletedEvent> OnQuestCompleted;

        // Battle Event Payloads
        public struct CardPlayedEvent
        {
            public int playerId;
            public int cardId;
            public Vector2 position;
            public int elixirCost;
            public uint tick;
        }

        public struct UnitSpawnedEvent
        {
            public uint entityId;
            public int playerId;
            public int cardId;
            public Vector2 position;
            public int level;
        }

        public struct UnitDiedEvent
        {
            public uint entityId;
            public int playerId;
            public int cardId;
            public Vector2 position;
            public DeathCause cause;
        }

        public enum DeathCause
        {
            Damage,
            Spell,
            LifetimeExpired,
            Suicide,
            Sacrifice
        }

        public struct BuildingPlacedEvent
        {
            public uint entityId;
            public int playerId;
            public int cardId;
            public Vector2 position;
            public float lifetime;
        }

        public struct BuildingDestroyedEvent
        {
            public uint entityId;
            public int playerId;
            public int cardId;
            public DeathCause cause;
        }

        public struct SpellCastEvent
        {
            public int playerId;
            public int spellId;
            public Vector2 position;
            public Vector2? targetPosition;
            public uint tick;
        }

        public struct TowerDamagedEvent
        {
            public int playerId; // Owner of tower
            public TowerType towerType;
            public int damage;
            public int remainingHP;
            public uint sourceEntityId;
        }

        public enum TowerType
        {
            King,
            PrincessLeft,
            PrincessRight
        }

        public struct TowerDestroyedEvent
        {
            public int playerId;
            public TowerType towerType;
            public uint sourceEntityId;
        }

        public struct KingTowerActivatedEvent
        {
            public int playerId;
            public ActivationCause cause;
        }

        public enum ActivationCause
        {
            Damaged,
            PrincessTowerDestroyed,
            TornadoPull,
            FishermanHook
        }

        public struct BattleEndedEvent
        {
            public BattleStatus result;
            public int player1Crowns;
            public int player2Crowns;
            public int player1TrophyChange;
            public int player2TrophyChange;
            public float duration;
            public bool wentOvertime;
            public long replayId;
            // Additive: consumed by BattleResultScreen (:80 player1Name/player2Name,
            // :150 rewards -> BattleRewardItemUI.Initialize(ChestReward),
            // :164/:171 keyEvents -> BattleLogItemUI.Initialize(BattleLogEvent),
            // :264 replayId, :279 battleId).
            public string player1Name;
            public string player2Name;
            public long battleId;
            public List<ChestReward> rewards;
            // Tech-debt: Core -> UI reference (fully-qualified, no using added).
            public List<CRClone.UI.Screens.BattleLogEvent> keyEvents;
        }

        public struct BattleTickEvent
        {
            public uint tick;
            public float deltaTime;
        }

        public struct ElixirChangedEvent
        {
            public int playerId;
            public int currentElixir;
            public int previousElixir;
            public ElixirChangeReason reason;
        }

        public enum ElixirChangeReason
        {
            Generation,
            CardPlayed,
            ElixirCollector,
            SpellEffect,
            Refund
        }

        public struct ChampionAbilityUsedEvent
        {
            public int playerId;
            public int championCardId;
            public Vector2 targetPosition;
            public int elixirCost;
        }

        // Network Event Payloads
        public struct NetworkConnectedEvent
        {
            public string serverAddress;
            public bool isReconnection;
        }

        public struct NetworkDisconnectedEvent
        {
            public string reason;
            public bool wasClean;
        }

        public struct NetworkMessageEvent
        {
            public string messageType;
            public byte[] data;
        }

        public struct ReconciliationEvent
        {
            public uint serverTick;
            public uint clientTick;
            public int entityCount;
            public bool fullResync;
        }

        // UI Event Payloads
        public struct PlayerDataChangedEvent
        {
            public string fieldName;
            public object oldValue;
            public object newValue;
        }

        public struct CardUnlockedEvent
        {
            public int cardId;
            public int count;
        }

        public struct CardUpgradedEvent
        {
            public int cardId;
            public int oldLevel;
            public int newLevel;
        }

        public struct ChestUnlockedEvent
        {
            public int chestTypeId;
            public List<ChestReward> rewards;
        }

        public struct ChestReward
        {
            public RewardType type;
            public int cardId;
            public int count;
            public int gold;
            public int gems;
            public CardRarity rarity;
        }

        public enum RewardType
        {
            Card,
            Gold,
            Gems,
            WildCard
        }

        public struct QuestCompletedEvent
        {
            public int questId;
            public List<ChestReward> rewards;
        }

        // Raise Methods
        public static void Raise(CardPlayedEvent e) => OnCardPlayed?.Invoke(e);
        public static void Raise(UnitSpawnedEvent e) => OnUnitSpawned?.Invoke(e);
        public static void Raise(UnitDiedEvent e) => OnUnitDied?.Invoke(e);
        public static void Raise(BuildingPlacedEvent e) => OnBuildingPlaced?.Invoke(e);
        public static void Raise(BuildingDestroyedEvent e) => OnBuildingDestroyed?.Invoke(e);
        public static void Raise(SpellCastEvent e) => OnSpellCast?.Invoke(e);
        public static void Raise(TowerDamagedEvent e) => OnTowerDamaged?.Invoke(e);
        public static void Raise(TowerDestroyedEvent e) => OnTowerDestroyed?.Invoke(e);
        public static void Raise(KingTowerActivatedEvent e) => OnKingTowerActivated?.Invoke(e);
        public static void Raise(BattleEndedEvent e) => OnBattleEnded?.Invoke(e);
        public static void Raise(BattleTickEvent e) => OnBattleTick?.Invoke(e);
        public static void Raise(ElixirChangedEvent e) => OnElixirChanged?.Invoke(e);
        public static void Raise(ChampionAbilityUsedEvent e) => OnChampionAbilityUsed?.Invoke(e);

        public static void Raise(NetworkConnectedEvent e) => OnNetworkConnected?.Invoke(e);
        public static void Raise(NetworkDisconnectedEvent e) => OnNetworkDisconnected?.Invoke(e);
        public static void Raise(NetworkMessageEvent e) => OnNetworkMessage?.Invoke(e);
        public static void Raise(ReconciliationEvent e) => OnReconciliation?.Invoke(e);

        public static void Raise(GameState state) => OnGameStateChanged?.Invoke(state);
        public static void Raise(ScreenType screen) => OnScreenChanged?.Invoke(screen);
        public static void RaiseToast(string message, object data = null) => OnToast?.Invoke(message, data);
        public static void RaiseError(string error) => OnError?.Invoke(error);
        public static void RaiseLoadingProgress(float progress) => OnLoadingProgress?.Invoke(progress);

        public static void Raise(PlayerDataChangedEvent e) => OnPlayerDataChanged?.Invoke(e);
        public static void Raise(CardUnlockedEvent e) => OnCardUnlocked?.Invoke(e);
        public static void Raise(CardUpgradedEvent e) => OnCardUpgraded?.Invoke(e);
        public static void Raise(ChestUnlockedEvent e) => OnChestUnlocked?.Invoke(e);
        public static void Raise(QuestCompletedEvent e) => OnQuestCompleted?.Invoke(e);
    }

    public enum ScreenType
    {
        MainMenu,
        Lobby,
        DeckBuilder,
        Battle,
        BattleResult,
        Shop,
        Clan,
        Profile,
        Settings,
        ChestUnlock,
        QuestLog,
        Tournament
    }
}
