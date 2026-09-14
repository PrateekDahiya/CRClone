using System;
using System.Collections.Generic;
using ProtoBuf;
using UnityEngine;
using CRClone.Core;

namespace CRClone.Network
{
    // Enums matching server/types/index.ts exactly
    //
    // ISSUE-103: BattleStatus, CardRarity, CardType and EntityType used to be
    // duplicated here with EntityType off by one vs Core (Network Unit=0.. vs
    // Core None=0, Unit=1..). The canonical definitions now live ONLY in
    // CRClone.Core (Assets/Scripts/Core/GameTypes.cs); the message classes
    // below reference those Core types directly (protobuf-net serializes enums
    // by numeric value, so the Core values ARE the wire contract — server
    // EntityTypeInternal must match Core numbering, see Agent 2).
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
        [ProtoMember(35)] public CRClone.Core.BattleType battleType { get; set; }
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

    // Clan
    [ProtoContract]
    public class ClanMessage : NetworkMessage
    {
        public ClanMessage() { type = MessageTypes.Clan; }
        [ProtoMember(200)] public string action { get; set; }
        [ProtoMember(201)] public byte[] data { get; set; }
    }

    [ProtoContract]
    public class ClanResponseMessage : NetworkMessage
    {
        public ClanResponseMessage() { type = MessageTypes.ClanResponse; }
        [ProtoMember(200)] public string action { get; set; }
        [ProtoMember(201)] public bool success { get; set; }
        [ProtoMember(202)] public byte[] data { get; set; }
    }

    // Shop
    [ProtoContract]
    public class ShopMessage : NetworkMessage
    {
        public ShopMessage() { type = MessageTypes.Shop; }
        [ProtoMember(300)] public string action { get; set; }
        [ProtoMember(301)] public byte[] data { get; set; }
    }

    [ProtoContract]
    public class ShopResponseMessage : NetworkMessage
    {
        public ShopResponseMessage() { type = MessageTypes.ShopResponse; }
        [ProtoMember(300)] public string action { get; set; }
        [ProtoMember(301)] public byte[] data { get; set; }
    }

    // Quest
    [ProtoContract]
    public class QuestMessage : NetworkMessage
    {
        public QuestMessage() { type = MessageTypes.Quest; }
        [ProtoMember(400)] public string action { get; set; }
        [ProtoMember(401)] public byte[] data { get; set; }
    }

    [ProtoContract]
    public class QuestResponseMessage : NetworkMessage
    {
        public QuestResponseMessage() { type = MessageTypes.QuestResponse; }
        [ProtoMember(400)] public string action { get; set; }
        [ProtoMember(401)] public byte[] data { get; set; }
    }

    // Season
    [ProtoContract]
    public class SeasonMessage : NetworkMessage
    {
        public SeasonMessage() { type = MessageTypes.Season; }
        [ProtoMember(500)] public string action { get; set; }
        [ProtoMember(501)] public byte[] data { get; set; }
    }

    [ProtoContract]
    public class SeasonResponseMessage : NetworkMessage
    {
        public SeasonResponseMessage() { type = MessageTypes.SeasonResponse; }
        [ProtoMember(500)] public string action { get; set; }
        [ProtoMember(501)] public byte[] data { get; set; }
    }

    // Tournament
    [ProtoContract]
    public class TournamentMessage : NetworkMessage
    {
        public TournamentMessage() { type = MessageTypes.Tournament; }
        [ProtoMember(600)] public string action { get; set; }
        [ProtoMember(601)] public byte[] data { get; set; }
    }

    [ProtoContract]
    public class TournamentResponseMessage : NetworkMessage
    {
        public TournamentResponseMessage() { type = MessageTypes.TournamentResponse; }
        [ProtoMember(600)] public string action { get; set; }
        [ProtoMember(601)] public byte[] data { get; set; }
    }

    // Replay
    [ProtoContract]
    public class ReplayMessage : NetworkMessage
    {
        public ReplayMessage() { type = MessageTypes.Replay; }
        [ProtoMember(700)] public string action { get; set; }
        [ProtoMember(701)] public byte[] data { get; set; }
    }

    [ProtoContract]
    public class ReplayResponseMessage : NetworkMessage
    {
        public ReplayResponseMessage() { type = MessageTypes.ReplayResponse; }
        [ProtoMember(700)] public string action { get; set; }
        [ProtoMember(701)] public byte[] data { get; set; }
    }

    // Player
    [ProtoContract]
    public class PlayerMessage : NetworkMessage
    {
        public PlayerMessage() { type = MessageTypes.Player; }
        [ProtoMember(800)] public string action { get; set; }
        [ProtoMember(801)] public byte[] data { get; set; }
    }

    [ProtoContract]
    public class PlayerResponseMessage : NetworkMessage
    {
        public PlayerResponseMessage() { type = MessageTypes.PlayerResponse; }
        [ProtoMember(800)] public string action { get; set; }
        [ProtoMember(801)] public byte[] data { get; set; }
    }

    // Replay (consumed by ChatMessageUI, BattleResultScreen, ProfileScreen)
    [ProtoContract]
    public class ReplayRequest
    {
        [ProtoMember(1)] public long replayId { get; set; }
        [ProtoMember(2)] public string replayCode { get; set; }
    }

    // Battle rematch (consumed by BattleResultScreen)
    [ProtoContract]
    public class RematchRequest
    {
        [ProtoMember(1)] public long battleId { get; set; }
    }

    // Player rename (consumed by ProfileScreen)
    [ProtoContract]
    public class ChangeNameRequest
    {
        [ProtoMember(1)] public string newName { get; set; }
    }

    // Shop (consumed by ShopScreen)
    [ProtoContract]
    public class ShopPurchaseRequest
    {
        [ProtoMember(1)] public string offerId;
        [ProtoMember(2)] public string currency;
    }

    // Clan (consumed by ClanScreen)
    [ProtoContract]
    public class ClanChatMessage
    {
        [ProtoMember(1)] public string message { get; set; }
    }

    [ProtoContract]
    public class ClanDonationRequest
    {
        [ProtoMember(1)] public int cardId { get; set; }
        [ProtoMember(2)] public int count { get; set; }
    }

    [ProtoContract]
    public class ClanDonate
    {
        [ProtoMember(1)] public int cardId { get; set; }
        [ProtoMember(2)] public string recipientId { get; set; }
        [ProtoMember(3)] public int count { get; set; }
    }

    [ProtoContract]
    public class ClanWarAction
    {
        [ProtoMember(1)] public string action { get; set; }
    }

    [ProtoContract]
    public class ClanMemberAction
    {
        [ProtoMember(1)] public string targetPlayerId { get; set; }
        [ProtoMember(2)] public string action { get; set; }
    }

    // Helper to identify message types
    // NOTE: this set is the wire-contract source of truth for the client.
    // Every constant here must have a matching proto message in
    // server/src/network/protocol.proto and a matching entry in
    // server/src/network/Protocol.ts MessageType (see protocol-contract test).
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
        public const string Clan = "clan";
        public const string ClanResponse = "clan_response";
        public const string Shop = "shop";
        public const string ShopResponse = "shop_response";
        public const string Quest = "quest";
        public const string QuestResponse = "quest_response";
        public const string Season = "season";
        public const string SeasonResponse = "season_response";
        public const string Tournament = "tournament";
        public const string TournamentResponse = "tournament_response";
        public const string Replay = "replay";
        public const string ReplayResponse = "replay_response";
        public const string Player = "player";
        public const string PlayerResponse = "player_response";
    }
}