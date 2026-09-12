using System;
using System.IO;
using System.Collections.Generic;
using ProtoBuf;
using UnityEngine;

namespace CRClone.Network
{
    public static class Serialization
    {
        private static readonly Dictionary<Type, int> _typeRegistry = new Dictionary<Type, int>();

        static Serialization()
        {
            // Pre-register all message types for performance
            RegisterType<AuthMessage>();
            RegisterType<AuthResponseMessage>();
            RegisterType<MatchmakingRequest>();
            RegisterType<MatchmakingStartedMessage>();
            RegisterType<BattleFoundMessage>();
            RegisterType<BattleStartMessage>();
            RegisterType<InputMessage>();
            RegisterType<InputAckMessage>();
            RegisterType<GameStateMessage>();
            RegisterType<ReconcileMessage>();
            RegisterType<BattleEndMessage>();
            RegisterType<ErrorMessage>();
            RegisterType<HeartbeatMessage>();
            RegisterType<PongMessage>();
            RegisterType<SaveDeckRequest>();
            RegisterType<DeckSavedMessage>();
            RegisterType<ClanMessage>();
            RegisterType<ClanResponseMessage>();
            RegisterType<ShopMessage>();
            RegisterType<ShopResponseMessage>();
            RegisterType<QuestMessage>();
            RegisterType<QuestResponseMessage>();
            RegisterType<SeasonMessage>();
            RegisterType<SeasonResponseMessage>();
            RegisterType<TournamentMessage>();
            RegisterType<TournamentResponseMessage>();
            RegisterType<ReplayMessage>();
            RegisterType<ReplayResponseMessage>();
            RegisterType<PlayerMessage>();
            RegisterType<PlayerResponseMessage>();
        }

        private static void RegisterType<T>() where T : NetworkMessage
        {
            _typeRegistry[typeof(T)] = _typeRegistry.Count;
        }

        public static byte[] Serialize<T>(T obj) where T : class
        {
            if (obj == null) return new byte[0];

            using (var ms = new MemoryStream())
            {
                Serializer.Serialize(ms, obj);
                return ms.ToArray();
            }
        }

        public static T Deserialize<T>(byte[] data) where T : class
        {
            if (data == null || data.Length == 0) return null;

            using (var ms = new MemoryStream(data))
            {
                return Serializer.Deserialize<T>(ms);
            }
        }

        public static byte[] SerializeLengthPrefixed<T>(T obj) where T : class
        {
            var data = Serialize(obj);
            var length = data.Length;
            var result = new byte[length + 4];
            Buffer.BlockCopy(BitConverter.GetBytes(length), 0, result, 0, 4);
            Buffer.BlockCopy(data, 0, result, 4, length);
            return result;
        }

        public static T DeserializeLengthPrefixed<T>(byte[] data, int offset, int count) where T : class
        {
            if (count < 4) return null;

            var length = BitConverter.ToInt32(data, offset);
            if (offset + 4 + length > offset + count) return null;

            var messageData = new byte[length];
            Buffer.BlockCopy(data, offset + 4, messageData, 0, length);
            return Deserialize<T>(messageData);
        }

        public static NetworkMessage DeserializeByType(byte[] data)
        {
            if (data == null || data.Length == 0) return null;

            // First deserialize as base to get type
            using (var ms = new MemoryStream(data))
            {
                // We need to peek at the type field
                // For simplicity, deserialize as a generic container
                var baseMsg = Serializer.Deserialize<NetworkMessageContainer>(ms);
                if (baseMsg == null) return null;

                return DeserializeByTypeName(baseMsg.type, data);
            }
        }

        private static NetworkMessage DeserializeByTypeName(string typeName, byte[] data)
        {
            using (var ms = new MemoryStream(data))
            {
                switch (typeName)
                {
                    case MessageTypes.Auth: return Serializer.Deserialize<AuthMessage>(ms);
                    case MessageTypes.AuthResponse: return Serializer.Deserialize<AuthResponseMessage>(ms);
                    case MessageTypes.Matchmaking: return Serializer.Deserialize<MatchmakingRequest>(ms);
                    case MessageTypes.MatchmakingStarted: return Serializer.Deserialize<MatchmakingStartedMessage>(ms);
                    case MessageTypes.BattleFound: return Serializer.Deserialize<BattleFoundMessage>(ms);
                    case MessageTypes.BattleStart: return Serializer.Deserialize<BattleStartMessage>(ms);
                    case MessageTypes.Input: return Serializer.Deserialize<InputMessage>(ms);
                    case MessageTypes.InputAck: return Serializer.Deserialize<InputAckMessage>(ms);
                    case MessageTypes.GameState: return Serializer.Deserialize<GameStateMessage>(ms);
                    case MessageTypes.Reconcile: return Serializer.Deserialize<ReconcileMessage>(ms);
                    case MessageTypes.BattleEnd: return Serializer.Deserialize<BattleEndMessage>(ms);
                    case MessageTypes.Error: return Serializer.Deserialize<ErrorMessage>(ms);
                    case MessageTypes.Heartbeat: return Serializer.Deserialize<HeartbeatMessage>(ms);
                    case MessageTypes.Pong: return Serializer.Deserialize<PongMessage>(ms);
                    case MessageTypes.SaveDeck: return Serializer.Deserialize<SaveDeckRequest>(ms);
                    case MessageTypes.DeckSaved: return Serializer.Deserialize<DeckSavedMessage>(ms);
                    case MessageTypes.Clan: return Serializer.Deserialize<ClanMessage>(ms);
                    case MessageTypes.ClanResponse: return Serializer.Deserialize<ClanResponseMessage>(ms);
                    case MessageTypes.Shop: return Serializer.Deserialize<ShopMessage>(ms);
                    case MessageTypes.ShopResponse: return Serializer.Deserialize<ShopResponseMessage>(ms);
                    case MessageTypes.Quest: return Serializer.Deserialize<QuestMessage>(ms);
                    case MessageTypes.QuestResponse: return Serializer.Deserialize<QuestResponseMessage>(ms);
                    case MessageTypes.Season: return Serializer.Deserialize<SeasonMessage>(ms);
                    case MessageTypes.SeasonResponse: return Serializer.Deserialize<SeasonResponseMessage>(ms);
                    case MessageTypes.Tournament: return Serializer.Deserialize<TournamentMessage>(ms);
                    case MessageTypes.TournamentResponse: return Serializer.Deserialize<TournamentResponseMessage>(ms);
                    case MessageTypes.Replay: return Serializer.Deserialize<ReplayMessage>(ms);
                    case MessageTypes.ReplayResponse: return Serializer.Deserialize<ReplayResponseMessage>(ms);
                    case MessageTypes.Player: return Serializer.Deserialize<PlayerMessage>(ms);
                    case MessageTypes.PlayerResponse: return Serializer.Deserialize<PlayerResponseMessage>(ms);
                    default:
                        UnityEngine.Debug.LogWarning($"[Serialization] Unknown message type: {typeName}");
                        return null;
                }
            }
        }

        // Container for peeking message type
        [ProtoContract]
        private class NetworkMessageContainer
        {
            [ProtoMember(1)] public string type { get; set; }
        }

        // Extension methods for common types
        public static byte[] SerializeVector2(Vector2 v)
        {
            using (var ms = new MemoryStream())
            {
                var nv = new NetworkVector2(v.x, v.y);
                Serializer.Serialize(ms, nv);
                return ms.ToArray();
            }
        }

        public static Vector2 DeserializeVector2(byte[] data)
        {
            using (var ms = new MemoryStream(data))
            {
                var nv = Serializer.Deserialize<NetworkVector2>(ms);
                return new Vector2(nv.x, nv.y);
            }
        }

        public static byte[] SerializePlayerInput(PlayerInput input)
        {
            using (var ms = new MemoryStream())
            {
                Serializer.Serialize(ms, input);
                return ms.ToArray();
            }
        }

        public static PlayerInput DeserializePlayerInput(byte[] data)
        {
            using (var ms = new MemoryStream(data))
            {
                return Serializer.Deserialize<PlayerInput>(ms);
            }
        }

        // Batch serialization for multiple messages
        public static byte[] SerializeBatch(IEnumerable<NetworkMessage> messages)
        {
            using (var ms = new MemoryStream())
            {
                var count = 0;
                var messageData = new List<byte[]>();

                foreach (var msg in messages)
                {
                    var data = Serialize(msg);
                    messageData.Add(data);
                    count++;
                }

                // Write count
                var countBytes = BitConverter.GetBytes(count);
                ms.Write(countBytes, 0, 4);

                // Write each message with length prefix
                foreach (var data in messageData)
                {
                    var lengthBytes = BitConverter.GetBytes(data.Length);
                    ms.Write(lengthBytes, 0, 4);
                    ms.Write(data, 0, data.Length);
                }

                return ms.ToArray();
            }
        }

        public static List<NetworkMessage> DeserializeBatch(byte[] data)
        {
            var messages = new List<NetworkMessage>();
            using (var ms = new MemoryStream(data))
            {
                var countBytes = new byte[4];
                if (ms.Read(countBytes, 0, 4) != 4) return messages;

                var count = BitConverter.ToInt32(countBytes, 0);

                for (int i = 0; i < count; i++)
                {
                    var lengthBytes = new byte[4];
                    if (ms.Read(lengthBytes, 0, 4) != 4) break;

                    var length = BitConverter.ToInt32(lengthBytes, 0);
                    var msgData = new byte[length];
                    if (ms.Read(msgData, 0, length) != length) break;

                    var msg = DeserializeByType(msgData);
                    if (msg != null) messages.Add(msg);
                }
            }
            return messages;
        }
    }
}