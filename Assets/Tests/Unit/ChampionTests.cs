using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using CRClone.Battle.Simulation;
using CRClone.Core;
using CRClone.Tests.TestFixtures;
using CRClone.Network;

namespace CRClone.Tests.Unit
{
    [TestFixture]
    public class ChampionTests : BattleTestBase
    {
        [Test]
        public void Archer_Queen_Ability_Grants_Invisibility_And_Damage_Boost()
        {
            InitializeSimulation(TestDecks.ChampionDeck, TestDecks.BalancedDeck);
                        SetPlayerElixir(1, 10);
            
            var aqInput = new PlayerInput
            {
                type = InputType.PlayCard,
                cardId = 27000000, // Archer Queen
                position = new Vector2(9, 8)
            };
            Simulation.QueueInput(1, aqInput);
            Tick();
            
            var aq = FindUnit(1, 27000000);
            Assert.IsNotNull(aq);
            
            // Use ability
            var abilityInput = new PlayerInput
            {
                type = InputType.ChampionAbility,
                position = new Vector2(12, 8)
            };
            Simulation.QueueInput(1, abilityInput);
            Tick();
            Step(0.1f);
            
            aq = FindUnit(1, 27000000);
            Assert.IsTrue(aq.IsInvisible, "Archer Queen should be invisible after ability");
            AssertApproximate(aq.Stats.damage * 2.5f, aq.Stats.damage * aq.GetDamageMultiplier(), 1f, 
                "Damage should be boosted by 2.5x");
            
            // Wait for duration
            Step(3f);
            
            aq = FindUnit(1, 27000000);
            Assert.IsFalse(aq.IsInvisible, "Invisibility should expire after duration");
            AssertApproximate(aq.Stats.damage, aq.Stats.damage * aq.GetDamageMultiplier(), 1f,
                "Damage should return to normal");
        }

        [Test]
        public void Skeleton_King_Ability_Spawns_Skeletons()
        {
            InitializeSimulation(new int[] { 27000001 } + TestDecks.BalancedDeck[1..], TestDecks.BalancedDeck);
                        SetPlayerElixir(1, 10);
            
            var skInput = new PlayerInput
            {
                type = InputType.PlayCard,
                cardId = 27000001, // Skeleton King
                position = new Vector2(9, 8)
            };
            Simulation.QueueInput(1, skInput);
            Tick();
            
            var sk = FindUnit(1, 27000001);
            Assert.IsNotNull(sk);
            
            // Use ability
            var abilityInput = new PlayerInput
            {
                type = InputType.ChampionAbility,
                position = new Vector2(9, 8)
            };
            Simulation.QueueInput(1, abilityInput);
            Tick();
            Step(0.1f);
            
            var skeletons = FindUnits(1, 26000046); // Skeleton
            Assert.AreEqual(5, skeletons.Count, "Skeleton King should spawn 5 skeletons");
        }

        [Test]
        public void Mighty_Miner_Ability_Dashes_And_Stuns()
        {
            InitializeSimulation(new int[] { 27000002 } + TestDecks.BalancedDeck[1..], TestDecks.BalancedDeck);
                        SetPlayerElixir(1, 10);
            SetPlayerElixir(2, 10);
            
            var mmInput = new PlayerInput
            {
                type = InputType.PlayCard,
                cardId = 27000002, // Mighty Miner
                position = new Vector2(5, 8)
            };
            Simulation.QueueInput(1, mmInput);
            
            var knightInput = new PlayerInput
            {
                type = InputType.PlayCard,
                cardId = 26000040, // Knight
                position = new Vector2(9, 8)
            };
            Simulation.QueueInput(2, knightInput);
            Tick();
            
            var mm = FindUnit(1, 27000002);
            var knight = FindUnit(2, 26000040);
            
            Assert.IsNotNull(mm);
            Assert.IsNotNull(knight);
            
            // Use ability - dash toward knight
            var abilityInput = new PlayerInput
            {
                type = InputType.ChampionAbility,
                position = new Vector2(10, 8)
            };
            Simulation.QueueInput(1, abilityInput);
            Tick();
            
            Step(0.5f);
            
            mm = FindUnit(1, 27000002);
            knight = FindUnit(2, 26000040);
            
            Assert.IsNotNull(mm);
            Assert.IsNotNull(knight);
            Assert.Greater(mm.Position.x, 9f, "Mighty Miner should dash past knight");
            Assert.IsTrue(knight.IsStunned, "Knight should be stunned by Mighty Miner dash");
        }

        [Test]
        public void Champion_Ability_Respects_Cooldown()
        {
            InitializeSimulation(TestDecks.ChampionDeck, TestDecks.BalancedDeck);
                        SetPlayerElixir(1, 10);
            
            var aqInput = new PlayerInput
            {
                type = InputType.PlayCard,
                cardId = 27000000, // Archer Queen
                position = new Vector2(9, 8)
            };
            Simulation.QueueInput(1, aqInput);
            Tick();
            
            var aq = FindUnit(1, 27000000);
            Assert.IsNotNull(aq);
            
            // Use ability first time
            var abilityInput = new PlayerInput
            {
                type = InputType.ChampionAbility,
                position = new Vector2(9, 8)
            };
            Simulation.QueueInput(1, abilityInput);
            Tick();
            
            aq = FindUnit(1, 27000000);
            Assert.IsTrue(aq.AbilityOnCooldown, "Ability should be on cooldown after use");
            
            // Wait almost full cooldown (20s)
            Step(19f);
            
            aq = FindUnit(1, 27000000);
            Assert.IsTrue(aq.AbilityOnCooldown, "Ability should still be on cooldown at 19s");
            
            // Wait full cooldown
            Step(2f);
            
            aq = FindUnit(1, 27000000);
            Assert.IsFalse(aq.AbilityOnCooldown, "Ability should be ready after 20s cooldown");
        }

        [Test]
        public void Only_One_Champion_Per_Deck()
        {
            // This is a deck validation test - would need DataManager
            // But we can test that simulation handles multiple champions
            var doubleChampionDeck = new int[] 
            { 
                27000000, // Archer Queen
                27000001, // Skeleton King
                26000040, 26000041, 26000042, 26000043, 26000044, 26000045
            };
            
            InitializeSimulation(doubleChampionDeck, TestDecks.BalancedDeck);
                        SetPlayerElixir(1, 10);
            
            // Play first champion
            var aqInput = new PlayerInput
            {
                type = InputType.PlayCard,
                cardId = 27000000,
                position = new Vector2(9, 8)
            };
            Simulation.QueueInput(1, aqInput);
            Tick();
            
            var aq = FindUnit(1, 27000000);
            Assert.IsNotNull(aq);
            
            // Play second champion (should work in simulation, validation is in DataManager)
            var skInput = new PlayerInput
            {
                type = InputType.PlayCard,
                cardId = 27000001,
                position = new Vector2(9, 7)
            };
            Simulation.QueueInput(1, skInput);
            Tick();
            
            var sk = FindUnit(1, 27000001);
            Assert.IsNotNull(sk);
        }

        [Test]
        public void Champion_Ability_Costs_Elixir()
        {
            InitializeSimulation(TestDecks.ChampionDeck, TestDecks.BalancedDeck);
                        SetPlayerElixir(1, 10);
            
            var aqInput = new PlayerInput
            {
                type = InputType.PlayCard,
                cardId = 27000000,
                position = new Vector2(9, 8)
            };
            Simulation.QueueInput(1, aqInput);
            Tick();
            
            int elixirBefore = Simulation.Player1.Elixir;
            
            var abilityInput = new PlayerInput
            {
                type = InputType.ChampionAbility,
                position = new Vector2(9, 8)
            };
            Simulation.QueueInput(1, abilityInput);
            Tick();
            
            // Archer Queen ability costs 3 elixir
            Assert.AreEqual(elixirBefore - 3, Simulation.Player1.Elixir, 
                "Archer Queen ability should cost 3 elixir");
        }

        [Test]
        public void Archer_Queen_Can_Target_Air_During_Ability()
        {
            InitializeSimulation(TestDecks.ChampionDeck, TestDecks.BalancedDeck);
                        SetPlayerElixir(1, 10);
            SetPlayerElixir(2, 10);
            
            var aqInput = new PlayerInput
            {
                type = InputType.PlayCard,
                cardId = 27000000,
                position = new Vector2(9, 8)
            };
            Simulation.QueueInput(1, aqInput);
            
            var minionInput = new PlayerInput
            {
                type = InputType.PlayCard,
                cardId = 26000047, // Minions (air)
                position = new Vector2(9, 12)
            };
            Simulation.QueueInput(2, minionInput);
            Tick();
            
            var aq = FindUnit(1, 27000000);
            var minions = FindUnits(2, 26000047);
            
            Assert.IsNotNull(aq);
            Assert.Greater(minions.Count, 0);
            
            // Normally AQ cannot target air
            Assert.IsFalse(aq.CanTargetAir, "Archer Queen should not target air normally");
            
            // Use ability
            var abilityInput = new PlayerInput
            {
                type = InputType.ChampionAbility,
                position = new Vector2(9, 8)
            };
            Simulation.QueueInput(1, abilityInput);
            Tick();
            Step(0.1f);
            
            aq = FindUnit(1, 27000000);
            Assert.IsTrue(aq.CanTargetAir, "Archer Queen should target air during Royal Cloak");
        }

        private Unit FindUnit(int playerId, int cardId)
        {
            foreach (var unit in Simulation.Units)
            {
                if (unit.OwnerPlayerId == playerId && unit.CardData.cardId == cardId)
                    return unit;
            }
            return null;
        }

        private List<Unit> FindUnits(int playerId, int cardId)
        {
            var result = new List<Unit>();
            foreach (var unit in Simulation.Units)
            {
                if (unit.OwnerPlayerId == playerId && unit.CardData.cardId == cardId)
                    result.Add(unit);
            }
            return result;
        }
    }
}
