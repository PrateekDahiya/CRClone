using System;
using System.Collections.Generic;
using UnityEngine;

namespace CRClone.Core
{
    public enum GameState
    {
        Boot,
        MainMenu,
        Lobby,
        DeckBuilder,
        Matchmaking,
        BattleLoading,
        Battle,
        BattlePaused,
        BattleResult,
        Shop,
        Clan,
        Profile,
        Settings
    }

    public enum GameScene
    {
        Boot,
        MainMenu,
        Lobby,
        DeckBuilder,
        Battle,
        Shop,
        Clan,
        Profile
    }

    public enum PlayerSide
    {
        None = 0,
        Player1 = 1,
        Player2 = 2
    }

    public enum CardRarity
    {
        Common = 0,
        Rare = 1,
        Epic = 2,
        Legendary = 3,
        Champion = 4
    }

    public enum CardType
    {
        Troop = 0,
        Spell = 1,
        Building = 2,
        Champion = 3
    }

    public enum TargetType
    {
        Ground = 0,
        Air = 1,
        Both = 2,
        Buildings = 3,
        Any = 4
    }

    public enum SpeedType
    {
        VerySlow = 0,
        Slow = 1,
        Medium = 2,
        Fast = 3,
        VeryFast = 4
    }

    public enum EntityType
    {
        None = 0,
        Unit = 1,
        Building = 2,
        Projectile = 3,
        SpellEffect = 4,
        Tower = 5
    }

    public enum BattleStatus
    {
        Waiting = 0,
        Playing = 1,
        Paused = 2,
        Player1Won = 3,
        Player2Won = 4,
        Draw = 5
    }

    [Serializable]
    public struct Vector2Int
    {
        public int x;
        public int y;

        public Vector2Int(int x, int y) { this.x = x; this.y = y; }
        public static implicit operator Vector2(Vector2Int v) => new Vector2(v.x, v.y);
        public static implicit operator Vector2Int(Vector2 v) => new Vector2Int(Mathf.RoundToInt(v.x), Mathf.RoundToInt(v.y));
    }

    [Serializable]
    public struct FixedVector2
    {
        public int x; // Fixed point: 1/1024 precision
        public int y;

        public const int SCALE = 1024;

        public FixedVector2(float x, float y) { this.x = Mathf.RoundToInt(x * SCALE); this.y = Mathf.RoundToInt(y * SCALE); }
        public FixedVector2(int x, int y) { this.x = x; this.y = y; }
        
        public Vector2 ToVector2() => new Vector2(x / (float)SCALE, y / (float)SCALE);
        public static FixedVector2 operator +(FixedVector2 a, FixedVector2 b) => new FixedVector2(a.x + b.x, a.y + b.y);
        public static FixedVector2 operator -(FixedVector2 a, FixedVector2 b) => new FixedVector2(a.x - b.x, a.y - b.y);
        public static FixedVector2 operator *(FixedVector2 a, int b) => new FixedVector2(a.x * b, a.y * b);
        public float DistanceTo(FixedVector2 other) => Vector2.Distance(ToVector2(), other.ToVector2());
    }
}

namespace CRClone.Data
{
    using Core;

    [CreateAssetMenu(fileName = "CardData", menuName = "CRClone/Card Data")]
    public class CardData : ScriptableObject
    {
        [Header("Identity")]
        public int cardId;
        public string cardName;
        public string nameKey; // Localization key
        public string descriptionKey;

        [Header("Classification")]
        public CardRarity rarity;
        public CardType type;
        public int unlockArena;

        [Header("Base Stats (Level 1)")]
        public int elixirCost;
        public int baseHitpoints;
        public int baseDamage;
        public float baseHitSpeed;
        public float baseRange;
        public SpeedType speed;
        public int deployTime = 1;
        public TargetType targetType;
        public int count = 1; // For swarms

        [Header("Mechanics (JSON)")]
        public string mechanicsJson; // Splash radius, charge, spawn, etc.

        [Header("Visuals")]
        public string spriteId;
        public string portraitId;
        public string spineAssetName;
        public string[] animationClips;

        [Header("Audio")]
        public string deploySound;
        public string attackSound;
        public string hitSound;
        public string deathSound;
        public string[] voiceLines;

        [Header("Balance")]
        public bool isEnabled = true;
        public string releaseVersion;

        // Runtime computed
        [NonSerialized] public Dictionary<int, CardLevelStats> levelStats = new();

        public CardLevelStats GetStats(int level)
        {
            if (levelStats.TryGetValue(level, out var stats)) return stats;
            return levelStats[1]; // Fallback
        }
    }

    [Serializable]
    public class CardLevelStats
    {
        public int level;
        public int hitpoints;
        public int damage;
        public float hitSpeed;
        public float range;
        public int goldCost;
        public int cardsRequired;
    }

    [CreateAssetMenu(fileName = "GameConfig", menuName = "CRClone/Game Config")]
    public class GameConfig : ScriptableObject
    {
        [Header("Elixir")]
        public float elixirGenerationRate = 2.8f; // seconds per elixir
        public float doubleElixirRate = 1.4f;
        public float tripleElixirRate = 0.93f;
        public int startingElixir = 5;
        public int maxElixir = 10;

        [Header("Battle")]
        public float battleDuration = 180f; // 3 minutes
        public float overtimeDuration = 180f;
        public int maxDeckCards = 8;
        public int maxChampionsPerDeck = 1;
        public int handSize = 4;

        [Header("Towers")]
        public int princessTowerHP = 2584;
        public int kingTowerHP = 4384;
        public int towerDamage = 152;
        public float towerHitSpeed = 1.2f;
        public float towerRange = 7f;

        [Header("Deployment")]
        public float deployZoneDepth = 4f; // Tiles from river
        public float deployZoneDepthExpanded = 8f; // When princess tower down

        [Header("Network")]
        public int simulationTickRate = 60;
        public float maxDesyncThreshold = 0.1f;
        public int maxInputQueueSize = 10;

        [Header("Progression")]
        public int maxCardLevel = 14;
        public int tournamentStandardLevel = 11;
        public float levelStatMultiplier = 1.1f; // ~10% per level
    }
}

namespace CRClone.Core
{
    public static class GameConstants
    {
        public const float TILE_SIZE = 1f;
        public const int ARENA_WIDTH_TILES = 18;
        public const int ARENA_HEIGHT_TILES = 32;
        public const int RIVER_Y_MIN = 14;
        public const int RIVER_Y_MAX = 18;
        public const float BRIDGE_WIDTH = 2f;
        public const float DEPLOY_ZONE_Y_P1_MAX = 13f; // Player 1 (bottom)
        public const float DEPLOY_ZONE_Y_P2_MIN = 19f; // Player 2 (top)

        public const float FIXED_DT = 1f / 60f;
        public const int FIXED_POINT_SCALE = 1024;

        public static readonly Vector2 P1_KING_POS = new Vector2(9, 2);
        public static readonly Vector2 P2_KING_POS = new Vector2(9, 30);
        public static readonly Vector2 P1_PRINCESS_LEFT_POS = new Vector2(3, 13);
        public static readonly Vector2 P1_PRINCESS_RIGHT_POS = new Vector2(15, 13);
        public static readonly Vector2 P2_PRINCESS_LEFT_POS = new Vector2(3, 19);
        public static readonly Vector2 P2_PRINCESS_RIGHT_POS = new Vector2(15, 19);
    }
}