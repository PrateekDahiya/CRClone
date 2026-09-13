using System;
using NUnit.Framework;
using CoreEntityType = CRClone.Core.EntityType;
using CoreBattleStatus = CRClone.Core.BattleStatus;
using CoreCardType = CRClone.Core.CardType;
using CoreCardRarity = CRClone.Core.CardRarity;
using CoreBattleType = CRClone.Core.BattleType;
using NetTowerType = CRClone.Network.TowerType;
using NetInputType = CRClone.Network.InputType;
using NetBattleType = CRClone.Network.BattleType;
using SimTowerType = CRClone.Battle.Simulation.TowerType;
using EventTowerType = CRClone.Core.EventBus.TowerType;

namespace CRClone.Tests.Unit
{
    /// <summary>
    /// ISSUE-103: Core vs Network shared-enum numeric parity.
    /// The canonical definitions live ONLY in CRClone.Core (GameTypes.cs);
    /// CRClone.Network message classes reference those Core types directly.
    /// These tests fail on any drift (name or numeric value) between the
    /// gameplay enums and the wire contract. Server-side numeric parity for
    /// EntityTypeInternal is covered by Agent 2 (TS test, do not duplicate).
    /// Conventions: file-local using aliases, never `using CRClone.Network;`.
    /// </summary>
    [TestFixture]
    public class EnumParityTests
    {
        [Test]
        public void EntityType_HasCanonicalWireValues()
        {
            Assert.AreEqual(0, (int)CoreEntityType.None);
            Assert.AreEqual(1, (int)CoreEntityType.Unit);
            Assert.AreEqual(2, (int)CoreEntityType.Building);
            Assert.AreEqual(3, (int)CoreEntityType.Projectile);
            Assert.AreEqual(4, (int)CoreEntityType.SpellEffect);
            Assert.AreEqual(5, (int)CoreEntityType.Tower);
            Assert.AreEqual(6, Enum.GetValues(typeof(CoreEntityType)).Length);
        }

        [Test]
        public void BattleStatus_HasCanonicalWireValues()
        {
            Assert.AreEqual(0, (int)CoreBattleStatus.Waiting);
            Assert.AreEqual(1, (int)CoreBattleStatus.Playing);
            Assert.AreEqual(2, (int)CoreBattleStatus.Paused);
            Assert.AreEqual(3, (int)CoreBattleStatus.Player1Won);
            Assert.AreEqual(4, (int)CoreBattleStatus.Player2Won);
            Assert.AreEqual(5, (int)CoreBattleStatus.Draw);
            Assert.AreEqual(6, Enum.GetValues(typeof(CoreBattleStatus)).Length);
        }

        [Test]
        public void CardType_HasCanonicalWireValues()
        {
            Assert.AreEqual(0, (int)CoreCardType.Troop);
            Assert.AreEqual(1, (int)CoreCardType.Spell);
            Assert.AreEqual(2, (int)CoreCardType.Building);
            Assert.AreEqual(3, (int)CoreCardType.Champion);
            Assert.AreEqual(4, Enum.GetValues(typeof(CoreCardType)).Length);
        }

        [Test]
        public void CardRarity_HasCanonicalWireValues()
        {
            Assert.AreEqual(0, (int)CoreCardRarity.Common);
            Assert.AreEqual(1, (int)CoreCardRarity.Rare);
            Assert.AreEqual(2, (int)CoreCardRarity.Epic);
            Assert.AreEqual(3, (int)CoreCardRarity.Legendary);
            Assert.AreEqual(4, (int)CoreCardRarity.Champion);
            Assert.AreEqual(5, Enum.GetValues(typeof(CoreCardRarity)).Length);
        }

        [Test]
        public void NetworkMessages_ReferenceCoreCanonicalEnums()
        {
            // EntityState.type and GameStateMessage.status must be the Core
            // enums: exactly one canonical definition per enum (no Network
            // duplicates left to drift).
            Assert.AreEqual(
                typeof(CoreEntityType),
                typeof(CRClone.Network.EntityState).GetProperty("type").PropertyType);
            Assert.AreEqual(
                typeof(CoreBattleStatus),
                typeof(CRClone.Network.GameStateMessage).GetProperty("status").PropertyType);

            var networkAssembly = typeof(NetInputType).Assembly;
            Assert.IsNull(networkAssembly.GetType("CRClone.Network.EntityType"), "Network EntityType duplicate resurrected");
            Assert.IsNull(networkAssembly.GetType("CRClone.Network.BattleStatus"), "Network BattleStatus duplicate resurrected");
            Assert.IsNull(networkAssembly.GetType("CRClone.Network.CardType"), "Network CardType duplicate resurrected");
            Assert.IsNull(networkAssembly.GetType("CRClone.Network.CardRarity"), "Network CardRarity duplicate resurrected");
        }

        [Test]
        public void TowerType_NumericsMatchAcrossDefinitions()
        {
            // TowerType exists in Network (wire), Simulation (gameplay) and
            // EventBus (events) with identical values; guard against drift.
            string[] names = Enum.GetNames(typeof(NetTowerType));
            Assert.AreEqual(new[] { "King", "PrincessLeft", "PrincessRight" }, names);
            foreach (string name in names)
            {
                int expected = (int)Enum.Parse(typeof(NetTowerType), name);
                Assert.AreEqual(expected, (int)Enum.Parse(typeof(SimTowerType), name), $"Simulation TowerType.{name}");
                Assert.AreEqual(expected, (int)Enum.Parse(typeof(EventTowerType), name), $"EventBus TowerType.{name}");
            }
            Assert.AreEqual(names.Length, Enum.GetValues(typeof(SimTowerType)).Length);
            Assert.AreEqual(names.Length, Enum.GetValues(typeof(EventTowerType)).Length);
        }

        [Test]
        public void InputType_HasCanonicalWireValues()
        {
            // InputType is defined only in CRClone.Network; pin the wire values.
            Assert.AreEqual(0, (int)NetInputType.PlayCard);
            Assert.AreEqual(1, (int)NetInputType.CastSpell);
            Assert.AreEqual(2, (int)NetInputType.ChampionAbility);
            Assert.AreEqual(3, (int)NetInputType.Emote);
            Assert.AreEqual(4, Enum.GetValues(typeof(NetInputType)).Length);
        }

        [Test]
        public void BattleType_NumericsMatch_NetworkVsCore()
        {
            // BattleType lives in CRClone.Core (canonical) and CRClone.Network
            // (wire); every name must exist in both with identical values.
            string[] netNames = Enum.GetNames(typeof(NetBattleType));
            string[] coreNames = Enum.GetNames(typeof(CoreBattleType));
            Assert.AreEqual(7, netNames.Length);
            Assert.AreEqual(7, coreNames.Length);
            foreach (string name in netNames)
            {
                int expected = (int)Enum.Parse(typeof(NetBattleType), name);
                Assert.AreEqual(expected, (int)Enum.Parse(typeof(CoreBattleType), name), $"Core BattleType.{name}");
            }
        }
    }
}
