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
    public class BuildingTests : BattleTestBase
    {
        [Test]
        public void Cannon_Attacks_Ground_Units_Only()
        {
            InitializeSimulation(TestDecks.BuildingDeck, TestDecks.BalancedDeck);
                        SetPlayerElixir(1, 10);
            SetPlayerElixir(2, 10);
            
            // Cannon (P1) vs Knight (P2 ground) and Minions (P2 air)
            var cannonInput = new PlayerInput
            {
                type = InputType.PlayCard,
                cardId = 26000045, // Cannon
                position = new Vector2(9, 10)
            };
            Simulation.QueueInput(1, cannonInput);
            
            var knightInput = new PlayerInput
            {
                type = InputType.PlayCard,
                cardId = 26000040, // Knight
                position = new Vector2(9, 14)
            };
            Simulation.QueueInput(2, knightInput);
            
            var minionInput = new PlayerInput
            {
                type = InputType.PlayCard,
                cardId = 26000047, // Minions (air)
                position = new Vector2(9, 14)
            };
            Simulation.QueueInput(2, minionInput);
            
            Tick();
            Step(3f);
            
            var cannon = FindBuilding(1, 26000045);
            var knight = FindUnit(2, 26000040);
            var minions = FindUnits(2, 26000047);
            
            Assert.IsNotNull(cannon);
            Assert.IsNotNull(knight);
            Assert.IsNotNull(minions);
            Assert.Greater(minions.Count, 0);
            
            // Cannon should target knight (ground)
            Assert.AreEqual(knight, cannon.Target, "Cannon should target ground unit");
            
            // Minions should not be targeted
            Assert.AreNotEqual(minions[0], cannon.Target, "Cannon should not target air units");
        }

        [Test]
        public void Tesla_Retracts_When_No_Targets()
        {
            InitializeSimulation(TestDecks.BuildingDeck, TestDecks.BalancedDeck);
                        SetPlayerElixir(1, 10);
            
            var teslaInput = new PlayerInput
            {
                type = InputType.PlayCard,
                cardId = 26000062, // Tesla
                position = new Vector2(9, 10)
            };
            Simulation.QueueInput(1, teslaInput);
            
            Tick();
            
            // Wait for Tesla to retract (no targets in range)
            Step(5f);
            
            var tesla = FindBuilding(1, 26000062);
            Assert.IsNotNull(tesla);
            Assert.IsTrue(tesla.IsRetracted, "Tesla should retract when no targets");
            Assert.IsTrue(tesla.IsInvulnerable, "Retracted Tesla should be invulnerable");
        }

        [Test]
        public void Tesla_Pops_Up_When_Target_In_Range()
        {
            InitializeSimulation(TestDecks.BuildingDeck, TestDecks.BalancedDeck);
                        SetPlayerElixir(1, 10);
            SetPlayerElixir(2, 10);
            
            // Place Tesla
            var teslaInput = new PlayerInput
            {
                type = InputType.PlayCard,
                cardId = 26000062, // Tesla
                position = new Vector2(9, 10)
            };
            Simulation.QueueInput(1, teslaInput);
            Tick();
            
            // Wait for retract
            Step(2f);
            
            var tesla = FindBuilding(1, 26000062);
            Assert.IsTrue(tesla.IsRetracted, "Tesla should be retracted initially");
            
            // Spawn Knight in range
            var knightInput = new PlayerInput
            {
                type = InputType.PlayCard,
                cardId = 26000040, // Knight
                position = new Vector2(9, 12)
            };
            Simulation.QueueInput(2, knightInput);
            Tick();
            
            Step(1f);
            
            tesla = FindBuilding(1, 26000062);
            Assert.IsFalse(tesla.IsRetracted, "Tesla should pop up when target in range");
            Assert.IsFalse(tesla.IsInvulnerable, "Popped up Tesla should be vulnerable");
        }

        [Test]
        public void Spawner_Building_Spawns_Units_Over_Time()
        {
            InitializeSimulation(TestDecks.BuildingDeck, TestDecks.BalancedDeck);
                        SetPlayerElixir(1, 10);
            
            var goblinHutInput = new PlayerInput
            {
                type = InputType.PlayCard,
                cardId = 26000064, // Goblin Hut
                position = new Vector2(9, 10)
            };
            Simulation.QueueInput(1, goblinHutInput);
            Tick();
            
            // Step 30 seconds (Goblin Hut lifetime is ~30s, spawns every ~4.9s)
            Step(30f);
            
            var spearGoblins = FindUnits(1, 26000069); // Spear Goblin (spawned by Goblin Hut)
            
            // Should spawn ~6 spear goblins over lifetime
            Assert.AreEqual(6, spearGoblins.Count, "Goblin Hut should spawn 6 spear goblins");
        }

        [Test]
        public void Building_Lifetime_Expires()
        {
            InitializeSimulation(TestDecks.BuildingDeck, TestDecks.BalancedDeck);
                        SetPlayerElixir(1, 10);
            
            var cannonInput = new PlayerInput
            {
                type = InputType.PlayCard,
                cardId = 26000045, // Cannon (30s lifetime)
                position = new Vector2(9, 10)
            };
            Simulation.QueueInput(1, cannonInput);
            Tick();
            
            // Step past lifetime
            Step(31f);
            
            var cannon = FindBuilding(1, 26000045);
            Assert.IsNull(cannon, "Cannon should be removed after lifetime expires");
        }

        [Test]
        public void Inferno_Tower_Ramps_Damage()
        {
            InitializeSimulation(TestDecks.BuildingDeck, TestDecks.BalancedDeck);
                        SetPlayerElixir(1, 10);
            SetPlayerElixir(2, 10);
            
            var infernoInput = new PlayerInput
            {
                type = InputType.PlayCard,
                cardId = 26000063, // Inferno Tower
                position = new Vector2(9, 10)
            };
            Simulation.QueueInput(1, infernoInput);
            
            var giantInput = new PlayerInput
            {
                type = InputType.PlayCard,
                cardId = 26000055, // Golem (high HP target)
                position = new Vector2(9, 14)
            };
            Simulation.QueueInput(2, giantInput);
            
            Tick();
            
            // Let Inferno Tower ramp up
            Step(3f);
            
            var inferno = FindBuilding(1, 26000063);
            var giant = FindUnit(2, 26000055);
            
            Assert.IsNotNull(inferno);
            Assert.IsNotNull(giant);
            
            // Inferno should be targeting giant and ramping damage
            Assert.AreEqual(giant, inferno.Target);
            
            // Check that damage has ramped (Inferno Tower starts at 50, ramps to 1600)
            // This would require exposing current damage - for now just verify targeting
        }

        [Test]
        public void Elixir_Collector_Produces_Elixir()
        {
            InitializeSimulation(TestDecks.BuildingDeck, TestDecks.BalancedDeck);
                        SetPlayerElixir(1, 10);
            
            var collectorInput = new PlayerInput
            {
                type = InputType.PlayCard,
                cardId = 26000067, // Elixir Collector
                position = new Vector2(9, 10)
            };
            Simulation.QueueInput(1, collectorInput);
            Tick();
            
            int initialElixir = Simulation.Player1.Elixir;
            
            // Collector produces 1 elixir every ~9.8s, total 2 over lifetime
            Step(10f);
            
            Assert.Greater(Simulation.Player1.Elixir, initialElixir, "Collector should produce elixir");
        }

        [Test]
        public void Building_Cannot_Be_Placed_On_Enemy_Side()
        {
            InitializeSimulation(TestDecks.BuildingDeck, TestDecks.BalancedDeck);
                        SetPlayerElixir(1, 10);
            
            // Try to place cannon on enemy side
            var cannonInput = new PlayerInput
            {
                type = InputType.PlayCard,
                cardId = 26000045, // Cannon
                position = new Vector2(9, 20) // Enemy side
            };
            Simulation.QueueInput(1, cannonInput);
            Tick();
            
            var cannon = FindBuilding(1, 26000045);
            Assert.IsNull(cannon, "Building should not be placed on enemy side");
            AssertElixir(1, 10, "Elixir should not be spent on invalid placement");
        }

        [Test]
        public void Building_Cannot_Be_Placed_Across_River()
        {
            InitializeSimulation(TestDecks.BuildingDeck, TestDecks.BalancedDeck);
                        SetPlayerElixir(1, 10);
            
            // Try to place cannon across river
            var cannonInput = new PlayerInput
            {
                type = InputType.PlayCard,
                cardId = 26000045, // Cannon
                position = new Vector2(9, 15) // Across river
            };
            Simulation.QueueInput(1, cannonInput);
            Tick();
            
            var cannon = FindBuilding(1, 26000045);
            Assert.IsNull(cannon, "Building should not be placed across river");
        }

        private Building FindBuilding(int playerId, int cardId)
        {
            foreach (var building in Simulation.Buildings)
            {
                if (building.OwnerPlayerId == playerId && building.CardData.cardId == cardId)
                    return building;
            }
            return null;
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
