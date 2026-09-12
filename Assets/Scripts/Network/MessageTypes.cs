using System;
using System.Collections.Generic;
using ProtoBuf;
using UnityEngine;

namespace CRClone.Network
{
    // Enums matching server/types/index.ts exactly
    public enum BattleType
    {
        Ladder = 0,
        TwoVTwo = 1,
        Tournament = 2,
        Challenge = 3,
        Friendly = 4,
        Practice = 5,
        ClanWar = 6
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

    public enum EntityType
    {
        Unit = 0,
        Building = 1,
        Projectile = 2,
        SpellEffect = 3,
        Tower = 4
    }

    public enum TowerType
    {
        King = 0,
        PrincessLeft = 1,
        PrincessRight = 2
    }

    public enum InputType
    {
        PlayCard = 0,
        CastSpell = 1,
        ChampionAbility = 2,
        Emote = 3
    }

    // Vector2 for network serialization
    [ProtoContract]
    public struct NetworkVector2
    {
        [ProtoMember(1)] public float x;
        [ProtoMember(2)] public float y;

        public NetworkVector2(float x, float y) { this.x = x; this.y = y; }

        public static implicit operator Vector2(NetworkVector2 v) => new Vector2(v.x, v.y);
        public static implicit operator NetworkVector2(Vector2 v) => new NetworkVector2(v.x, v.y);
    }

    // Base message
    [ProtoContract]
    public abstract class NetworkMessage
    {
        [ProtoMember(1)] public string type { get; set; }
        [ProtoMember(2)] public uint requestId { get; set; }
        [ProtoMember(3)] public ulong timestamp { get; set; } = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }

    // Auth messages
    [ProtoContract]
    public class AuthMessage : NetworkMessage
    {
        public AuthMessage() { type = "auth"; }
        [ProtoMember(10)] public string token { get; set; }
    }

    [ProtoContract]
    public class AuthResponseMessage : NetworkMessage
    {
        public AuthResponseMessage() { type = "auth_response"; }
        [ProtoMember(10)] public bool success { get; set; }
        [ProtoMember(11)] public string playerId { get; set; }
        [ProtoMember(12)] public string error { get; set; }
    }

    // Matchmaking
    [ProtoContract]
    public class MatchmakingRequest : NetworkMessage
    {
        public MatchmakingRequest() { type = "matchmaking"; }
        [ProtoMember(20)] public BattleType battleType { get; set; }
    }

    [ProtoContract]
    public class MatchmakingStartedMessage : NetworkMessage
    {
        public MatchmakingStartedMessage() { type = "matchmaking_started"; }
        [ProtoMember(20)] public BattleType battleType { get; set; }
    }

    [ProtoContract]
    public class BattleFoundMessage : NetworkMessage
    {
        public BattleFoundMessage() { type = "battle_found"; }
        [ProtoMember(30)] public string battleId { get; set; }
        [ProtoMember(31)] public ulong seed { get; set; }
        [ProtoMember(32)] public PlayerBattleInfo player1 { get; set; }
        [ProtoMember(33)] public PlayerBattleInfo player2 { get; set; }
        [ProtoMember(34)] public bool is2v2 { get; set; }
    }

    [ProtoContract]
    public class PlayerBattleInfo
    {
        [ProtoMember(1)] public string playerId { get; set; }
        [ProtoMember(2)] public string username { get; set; }
        [ProtoMember(3)] public int trophies { get; set; }
        [ProtoMember(4)] public uint[] deck { get; set; }
        [ProtoMember(5)] public int kingTowerLevel { get; set; }
        [ProtoMember(6)] public int princessTowerLevel { get; set; }
    }

    // Input
    [ProtoContract]
    public class InputMessage : NetworkMessage
    {
        public InputMessage() { type = "input"; }
        [ProtoMember(40)] public uint tick { get; set; }
        [ProtoMember(41)] public PlayerInput input { get; set; }
    }

    [ProtoContract]
    public class PlayerInput
    {
        [ProtoMember(1)] public InputType type { get; set; }
        [ProtoMember(2)] public uint cardId { get; set; }
        [ProtoMember(3)] public uint spellId { get; set; }
        [ProtoMember(4)] public NetworkVector2 position { get; set; }
        [ProtoMember(5)] public NetworkVector2 targetPosition { get; set; }
        [ProtoMember(6)] public uint clientTick { get; set; }
    }

    [ProtoContract]
    public class InputAckMessage : NetworkMessage
    {
        public InputAckMessage() { type = "input_ack"; }
        [ProtoMember(42)] public uint ackTick { get; set; }
    }

    // Game State
    [ProtoContract]
    public class GameStateMessage : NetworkMessage
    {
        public GameStateMessage() { type = "game_state"; }
        [ProtoMember(50)] public uint tick { get; set; }
        [ProtoMember(51)] public EntityState[] entities { get; set; }
        [ProtoMember(52)] public EntityState[] projectiles { get; set; }
        [ProtoMember(53)] public PlayerState player1 { get; set; }
        [ProtoMember(54)] public PlayerState player2 { get; set; }
        [ProtoMember(55)] public BattleStatus status { get; set; }
    }

    [ProtoContract]
    public class EntityState
    {
        [ProtoMember(1)] public uint id { get; set; }
        [ProtoMember(2)] public EntityType type { get; set; }
        [ProtoMember(3)] public uint owner { get; set; }
        [ProtoMember(4)] public NetworkVector2 position { get; set; }
        [ProtoMember(5)] public NetworkVector2 velocity { get; set; }
        [ProtoMember(6)] public int hp { get; set; }
        [ProtoMember(7)] public int maxHp { get; set; }
        [ProtoMember(8)] public uint targetId { get; set; }
        [ProtoMember(9)] public bool isDead { get; set; }
        // Unit specific
        [ProtoMember(10)] public string state { get; set; }
        [ProtoMember(11)] public float attackCooldown { get; set; }
        // Building specific
        [ProtoMember(12)] public float lifetime { get; set; }
        [ProtoMember(13)] public bool isRetracted { get; set; }
        [ProtoMember(14)] public float spawnTimer { get; set; }
        // Projectile specific
        [ProtoMember(15)] public uint sourceId { get; set; }
        [ProtoMember(16)] public bool isBeam { get; set; }
        // Spell specific
        [ProtoMember(17)] public string spellType { get; set; }
        [ProtoMember(18)] public float radius { get; set; }
        [ProtoMember(19)] public float remainingTime { get; set; }
    }

    [ProtoContract]
    public class PlayerState
    {
        [ProtoMember(1)] public int playerId { get; set; }
        [ProtoMember(2)] public int elixir { get; set; }
        [ProtoMember(3)] public uint[] hand { get; set; }
        [ProtoMember(4)] public uint[] deck { get; set; }
        [ProtoMember(5)] public int nextCardIndex { get; set; }
        [ProtoMember(6)] public bool kingTowerActivated { get; set; }
    }

    // Reconciliation
    [ProtoContract]
    public class ReconcileMessage : NetworkMessage
    {
        public ReconcileMessage() { type = "reconcile"; }
        [ProtoMember(60)] public uint tick { get; set; }
        [ProtoMember(61)] public EntityState[] entities { get; set; }
    }

    // Battle End
    [ProtoContract]
    public class BattleEndMessage : NetworkMessage
    {
        public BattleEndMessage() { type = "battle_end"; }
        [ProtoMember(70)] public string battleId { get; set; }
        [ProtoMember(71)] public BattleResult result { get; set; }
    }

    [ProtoContract]
    public class BattleResult
    {
        [ProtoMember(1)] public string winner { get; set; } // "player1", "player2", "draw"
        [ProtoMember(2)] public int player1Crowns { get; set; }
        [ProtoMember(3)] public int player2Crowns { get; set; }
        [ProtoMember(4)] public int player1TrophyChange { get; set; }
        [ProtoMember(5)] public int player2TrophyChange { get; set; }
        [ProtoMember(6)] public int duration { get; set; }
        [ProtoMember(7)] public bool wentOvertime { get; set; }
        [ProtoMember(8)] public string replayId { get; set; }
    }

    // Error
    [ProtoContract]
    public class ErrorMessage : NetworkMessage
    {
        public ErrorMessage() { type = "error"; }
        [ProtoMember(80)] public string message { get; set; }
        [ProtoMember(81)] public string code { get; set; }
    }

    // Heartbeat
    [ProtoContract]
    public class HeartbeatMessage : NetworkMessage
    {
        public HeartbeatMessage() { type = "heartbeat"; }
    }

    [ProtoContract]
    public class PongMessage : NetworkMessage
    {
        public PongMessage() { type = "pong"; }
    }

    // Deck
    [ProtoContract]
    public class SaveDeckRequest : NetworkMessage
    {
        public SaveDeckRequest() { type = "save_deck"; }
        [ProtoMember(90)] public uint[] cardIds { get; set; }
    }

    [ProtoContract]
    public class DeckSavedMessage : NetworkMessage
    {
        public DeckSavedMessage() { type = "deck_saved"; }
        [ProtoMember(90)] public bool success { get; set; }
    }

    // Battle Start (sent after battle_found)
    [ProtoContract]
    public class BattleStartMessage : NetworkMessage
    {
        public BattleStartMessage() { type = "battle_start"; }
        [ProtoMember(100)] public string battleId { get; set; }
        [ProtoMember(101)] public uint tick { get; set; }
        [ProtoMember(102)] public PlayerState player1 { get; set; }
        [ProtoMember(103)] public PlayerState player2 { get; set; }
    }

    // Clan (placeholder)
    [ProtoContract]
    public class ClanMessage : NetworkMessage
    {
        [ProtoMember(200)] public string action { get; set; }
        [ProtoMember(201)] public byte[] data { get; set; }
    }

    [ProtoContract]
    public class ClanResponseMessage : NetworkMessage
    {
        [ProtoMember(200)] public string action { get; set; }
        [ProtoMember(201)] public bool success { get; set; }
        [ProtoMember(202)] public byte[] data { get; set; }
    }

    // Shop (placeholder)
    [ProtoContract]
    public class ShopMessage : NetworkMessage
    {
        [ProtoMember(300)] public string action { get; set; }
        [ProtoMember(301)] public byte[] data { get; set; }
    }

    [ProtoContract]
    public class ShopResponseMessage : NetworkMessage
    {
        [ProtoMember(300)] public string action { get; set; }
        [ProtoMember(301)] public byte[] data { get; set; }
    }

    // Quest (placeholder)
    [ProtoContract]
    public class QuestMessage : NetworkMessage
    {
        [ProtoMember(400)] public string action { get; set; }
        [ProtoMember(401)] public byte[] data { get; set; }
    }

    [ProtoContract]
    public class QuestResponseMessage : NetworkMessage
    {
        [ProtoMember(400)] public string action { get; set; }
        [ProtoMember(401)] public byte[] data { get; set; }
    }

    // Season (placeholder)
    [ProtoContract]
    public class SeasonMessage : NetworkMessage
    {
        [ProtoMember(500)] public string action { get; set; }
        [ProtoMember(501)] public byte[] data { get; set; }
    }

    [ProtoContract]
    public class SeasonResponseMessage : NetworkMessage
    {
        [ProtoMember(500)] public string action { get; set; }
        [ProtoMember(501)] public byte[] data { get; set; }
    }

    // Tournament (placeholder)
    [ProtoContract]
    public class TournamentMessage : NetworkMessage
    {
        [ProtoMember(600)] public string action { get; set; }
        [ProtoMember(601)] public byte[] data { get; set; }
    }

    [ProtoContract]
    public class TournamentResponseMessage : NetworkMessage
    {
        [ProtoMember(600)] public string action { get; set; }
        [ProtoMember(601)] public byte[] data { get; set; }
    }

    // Replay (placeholder)
    [ProtoContract]
    public class ReplayMessage : NetworkMessage
    {
        [ProtoMember(700)] public string action { get; set; }
        [ProtoMember(701)] public byte[] data { get; set; }
    }

    [ProtoContract]
    public class ReplayResponseMessage : NetworkMessage
    {
        [ProtoMember(700)] public string action { get; set; }
        [ProtoMember(701)] public byte[] data { get; set; }
    }

    // Player (placeholder)
    [ProtoContract]
    public class PlayerMessage : NetworkMessage
    {
        [ProtoMember(800)] public string action { get; set; }
        [ProtoMember(801)] public byte[] data { get; set; }
    }

    [ProtoContract]
    public class PlayerResponseMessage : NetworkMessage
    {
        [ProtoMember(800)] public string action { get; set; }
        [ProtoMember(801)] public byte[] data { get; set; }
    }

    // Helper to identify message types
    public static class MessageTypes
    {
        public const string Auth = "auth";
        public const string AuthResponse = "auth_response";
        public const string Matchmaking = "matchmaking";
        public const string MatchmakingStarted = "matchmaking_started";
        public const string BattleFound = "battle_found";
        public const string BattleStart = "battle_start";
        public const string Input = "input";
        public const string InputAck = "input_ack";
        public const string GameState = "game_state";
        public const string Reconcile = "reconcile";
        public const string BattleEnd = "battle_end";
        public const string Error = "error";
        public const string Heartbeat = "heartbeat";
        public const string Pong = "pong";
        public const string SaveDeck = "save_deck";
        public const string DeckSaved = "deck_saved";
    }
}