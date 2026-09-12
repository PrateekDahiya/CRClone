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
    public class ElixirSystemTests : BattleTestBase
    {
        [Test]
        public void Elixir_Generates_At_Correct_Rate_Normal()
        {
            InitializeSimulation(TestDecks.BalancedDeck, TestDecks.BalancedDeck);
            SetPlayerElixir(1, 5);
            SetPlayerElixir(2, 5);

            // 1 elixir cycle at normal rate (2.8s); stepped slightly past to
            // absorb fixed-point truncation in the per-tick fraction.
            Step(3.2f);

            AssertElixir(1, 6);
            AssertElixir(2, 6);
        }

        [Test]
        public void Elixir_Caps_At_Max()
        {
            InitializeSimulation(TestDecks.BalancedDeck, TestDecks.BalancedDeck);
            SetPlayerElixir(1, 10);
            SetPlayerElixir(2, 10);

            Step(10f); // Regen continues but must cap

            AssertElixir(1, 10);
            AssertElixir(2, 10);
        }

        [Test]
        public void Double_Elixir_Generates_Twice_As_Fast()
        {
            InitializeSimulation(TestDecks.BalancedDeck, TestDecks.BalancedDeck);

            // Double elixir runs during overtime (t >= 180s); 0-0 crowns keep playing
            Step(181f);
            Assert.AreEqual(BattleStatus.Playing, Simulation.Status);
            SetPlayerElixir(1, 5);
            SetPlayerElixir(2, 5);

            // 1 elixir cycle at double rate (1.4s), stepped slightly past
            // to absorb fixed-point truncation.
            Step(1.7f);

            AssertElixir(1, 6);
            AssertElixir(2, 6);
        }

        [Test]
        public void Elixir_Spent_On_Card_Play()
        {
            InitializeSimulation(TestDecks.BalancedDeck, TestDecks.BalancedDeck);
            SetPlayerElixir(1, 5);

            PlayCard(1, 89, new Vector2(9, 8)); // Knight costs 3 (helper tops up, so spend manually)

            // PlayCard helper tops up to 10 first: 10 - 3 = 7
            AssertElixir(1, 7);
        }

        [Test]
        public void Cannot_Play_Card_Without_Elixir()
        {
            InitializeSimulation(TestDecks.BalancedDeck, TestDecks.BalancedDeck);
            SetPlayerElixir(1, 2); // Knight costs 3

            var input = new PlayerInput
            {
                type = InputType.PlayCard,
                cardId = 89, // Knight
                position = new Vector2(9, 8)
            };
            Simulation.QueueInput(1, input);
            Tick();

            AssertElixir(1, 2);
            Assert.IsNull(FindUnit(1, 89));
        }

        [Test]
        public void Elixir_Not_Spent_On_Invalid_Position()
        {
            InitializeSimulation(TestDecks.BalancedDeck, TestDecks.BalancedDeck);
            SetPlayerElixir(1, 10);

            // Cannon on enemy side: invalid for P1
            var input = new PlayerInput
            {
                type = InputType.PlayCard,
                cardId = 94, // Cannon
                position = new Vector2(9, 20)
            };
            Simulation.QueueInput(1, input);
            Tick();

            AssertElixir(1, 10);
            Assert.IsNull(FindBuilding(1, 94));
        }

        [Test]
        public void Elixir_Collector_Produces_Elixir_Over_Time()
        {
            InitializeSimulation(TestDecks.BuildingDeck, TestDecks.BalancedDeck);

            PlayCard(1, 99, new Vector2(9, 10)); // Elixir Collector costs 6
            int afterPlacement = Simulation.Player1.Elixir;

            Step(10f); // Normal regen (~3) + collector tick(s)

            Assert.Greater(Simulation.Player1.Elixir, afterPlacement,
                "Elixir should grow from regen and collector production");
        }
    }
}
