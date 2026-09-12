using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using CRClone.Battle.Simulation;
using CRClone.Core;
using CRClone.Tests.TestFixtures;
using PlayerInput = CRClone.Network.PlayerInput;
using InputType = CRClone.Network.InputType;

namespace CRClone.Tests.Unit
{
    [TestFixture]
    public class BuildingTests : BattleTestBase
    {
        [Test]
        public void Cannon_Attacks_Ground_Units_Only()
        {
            InitializeSimulation(TestDecks.BuildingDeck, TestDecks.BalancedDeck);

            PlayCard(1, 94, new Vector2(9, 10)); // Cannon, P1 zone
            var knight = PlayCard(2, 89, new Vector2(9, 20));
            var minions = PlayCard(2, 93, new Vector2(10, 20));
            var cannon = FindBuilding(1, 94);

            Assert.IsNotNull(cannon);
            Assert.IsNotNull(knight);
            Assert.IsNotNull(minions);

            Step(3f);

            Assert.AreEqual(knight, cannon.Target, "Cannon should target ground unit");
            Assert.AreNotEqual(minions, cannon.Target, "Cannon should not target air units");
        }

        [Test]
        public void Tesla_Retracts_When_No_Targets()
        {
            InitializeSimulation(TestDecks.BuildingDeck, TestDecks.BalancedDeck);

            PlayCard(1, 95, new Vector2(9, 10)); // Tesla, P1 zone

            Step(5f); // No enemies: Tesla stays retracted

            var tesla = FindBuilding(1, 95);
            Assert.IsNotNull(tesla);
            Assert.IsTrue(tesla.IsRetracted, "Tesla should retract when no targets");
            Assert.IsTrue(tesla.IsInvulnerableWhileRetracted, "Retracted Tesla should be invulnerable");
        }

        [Test]
        public void Tesla_Pops_Up_When_Target_In_Range()
        {
            InitializeSimulation(TestDecks.BuildingDeck, TestDecks.BalancedDeck);

            PlayCard(1, 95, new Vector2(9, 10));
            Step(2f);

            var tesla = FindBuilding(1, 95);
            Assert.IsNotNull(tesla);
            Assert.IsTrue(tesla.IsRetracted, "Tesla should be retracted initially");

            PlayCard(2, 89, new Vector2(9, 20)); // Knight marches into Tesla range
            Step(2f);

            tesla = FindBuilding(1, 95);
            Assert.IsNotNull(tesla);
            Assert.IsFalse(tesla.IsRetracted, "Tesla should pop up when target in range");
            Assert.IsFalse(tesla.IsInvulnerableWhileRetracted, "Popped up Tesla should be vulnerable");
        }

        [Test]
        public void Spawner_Building_Spawns_Units_Over_Time()
        {
            InitializeSimulation(TestDecks.BuildingDeck, TestDecks.BalancedDeck);

            PlayCard(1, 30, new Vector2(9, 10)); // Goblin Hut, 30s lifetime

            Step(30f); // Full lifetime: immediate + 5 waves every 4.9s

            var spearGoblins = FindUnitsByName(1, "Spear Goblins");
            Assert.AreEqual(6, spearGoblins.Count, "Goblin Hut should spawn 6 Spear Goblins");
        }

        [Test]
        public void Building_Lifetime_Expires()
        {
            InitializeSimulation(TestDecks.BuildingDeck, TestDecks.BalancedDeck);

            PlayCard(1, 94, new Vector2(9, 10)); // Cannon, 30s lifetime

            Step(31f);

            Assert.IsNull(FindBuilding(1, 94), "Cannon should be removed after lifetime expires");
        }

        [Test]
        public void Inferno_Tower_Engages_High_HP_Target()
        {
            InitializeSimulation(TestDecks.BuildingDeck, TestDecks.BalancedDeck);

            PlayCard(1, 31, new Vector2(9, 10)); // Inferno Tower
            var pekka = PlayCard(2, 25, new Vector2(9, 20)); // P.E.K.K.A tank

            Step(3f);

            var inferno = FindBuilding(1, 31);
            Assert.IsNotNull(inferno);
            Assert.IsNotNull(pekka);
            Assert.AreEqual(pekka, inferno.Target, "Inferno Tower should engage the tank");
            Assert.Less(pekka.CurrentHP, pekka.MaxHP, "Tank should take Inferno damage");
        }

        [Test]
        public void Elixir_Collector_Produces_Elixir()
        {
            InitializeSimulation(TestDecks.BuildingDeck, TestDecks.BalancedDeck);

            PlayCard(1, 99, new Vector2(9, 10)); // Elixir Collector costs 6
            int initialElixir = Simulation.Player1.Elixir;

            Step(10f); // Normal regen + collector tick(s)

            Assert.Greater(Simulation.Player1.Elixir, initialElixir,
                "Collector should produce elixir on top of regen");
        }

        [Test]
        public void Building_Cannot_Be_Placed_On_Enemy_Side()
        {
            InitializeSimulation(TestDecks.BuildingDeck, TestDecks.BalancedDeck);
            SetPlayerElixir(1, 10);

            var input = new PlayerInput
            {
                type = InputType.PlayCard,
                cardId = 94, // Cannon
                position = new Vector2(9, 20) // Enemy side
            };
            Simulation.QueueInput(1, input);
            Tick();

            Assert.IsNull(FindBuilding(1, 94), "Building should not be placed on enemy side");
            AssertElixir(1, 10);
        }

        [Test]
        public void Troop_Cannot_Be_Placed_Across_River()
        {
            InitializeSimulation(TestDecks.BalancedDeck, TestDecks.BalancedDeck);
            SetPlayerElixir(1, 10);

            var input = new PlayerInput
            {
                type = InputType.PlayCard,
                cardId = 89, // Knight
                position = new Vector2(9, 15) // Across river
            };
            Simulation.QueueInput(1, input);
            Tick();

            Assert.IsNull(FindUnit(1, 89), "Ground troop should not deploy across the river");
            AssertElixir(1, 10);
        }
    }
}
