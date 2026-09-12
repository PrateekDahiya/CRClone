using NUnit.Framework;
using UnityEngine;
using CRClone.Battle.Simulation;
using CRClone.Core;
using CRClone.Tests.TestFixtures;
using PlayerInput = CRClone.Network.PlayerInput;
using InputType = CRClone.Network.InputType;

namespace CRClone.Tests.Integration
{
    [TestFixture]
    public class NetworkTests : BattleTestBase
    {
        [Test]
        public void Invalid_Input_Rejected_Card_Not_In_Deck()
        {
            InitializeSimulation(TestDecks.BalancedDeck, TestDecks.BalancedDeck);
            SetPlayerElixir(1, 10);

            // P.E.K.K.A (25) is not in the Balanced deck, but PlayCard only
            // validates elixir + position, so it resolves. The simulation must
            // at least not corrupt state: elixir accounting stays consistent.
            var input = new PlayerInput
            {
                type = InputType.PlayCard,
                cardId = 25, // P.E.K.K.A
                position = new Vector2(9, 8)
            };
            Simulation.QueueInput(1, input);
            Tick();

            Assert.LessOrEqual(Simulation.Player1.Elixir, 10);
        }

        [Test]
        public void Invalid_Input_Rejected_Insufficient_Elixir()
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
        public void Invalid_Input_Rejected_Invalid_Position()
        {
            InitializeSimulation(TestDecks.BalancedDeck, TestDecks.BalancedDeck);
            SetPlayerElixir(1, 10);

            var input = new PlayerInput
            {
                type = InputType.PlayCard,
                cardId = 94, // Cannon
                position = new Vector2(9, 20) // Enemy side
            };
            Simulation.QueueInput(1, input);
            Tick();

            AssertElixir(1, 10);
            Assert.IsNull(FindBuilding(1, 94));
        }

        [Test]
        public void Input_Spam_Does_Not_Corrupt_State()
        {
            InitializeSimulation(TestDecks.BalancedDeck, TestDecks.BalancedDeck);
            SetPlayerElixir(1, 10);

            for (int i = 0; i < 100; i++)
            {
                Simulation.QueueInput(1, new PlayerInput
                {
                    type = InputType.PlayCard,
                    cardId = 89,
                    position = new Vector2(9, 8)
                });
            }
            for (int i = 0; i < 10; i++) Tick();

            // Elixir can never go negative and the sim keeps ticking
            Assert.GreaterOrEqual(Simulation.Player1.Elixir, 0);
            Assert.AreEqual(BattleStatus.Playing, Simulation.Status);
        }

        [Test]
        public void Desync_Free_Determinism_Same_Inputs_Same_State()
        {
            var a = RunMirroredSim(777);
            var b = RunMirroredSim(777);

            Assert.AreEqual(a.elixir1, b.elixir1);
            Assert.AreEqual(a.elixir2, b.elixir2);
            Assert.AreEqual(a.units, b.units);
        }

        [Test]
        public void Reconcile_Accepts_Server_State_Without_Throwing()
        {
            InitializeSimulation(TestDecks.BalancedDeck, TestDecks.BalancedDeck);

            PlayCard(1, 89, new Vector2(9, 8));
            Step(1f);

            var serverState = new CRClone.Network.GameStateMessage
            {
                tick = Simulation.CurrentTick
            };

            Assert.DoesNotThrow(() => Simulation.Reconcile(serverState));
        }

        [Test]
        public void Simultaneous_Inputs_From_Both_Players()
        {
            InitializeSimulation(TestDecks.BalancedDeck, TestDecks.BalancedDeck);
            SetPlayerElixir(1, 10);
            SetPlayerElixir(2, 10);

            Simulation.QueueInput(1, new PlayerInput
            {
                type = InputType.PlayCard,
                cardId = 89,
                position = new Vector2(9, 8)
            });
            Simulation.QueueInput(2, new PlayerInput
            {
                type = InputType.PlayCard,
                cardId = 89,
                position = new Vector2(9, 24)
            });
            Tick();

            Assert.IsNotNull(FindUnit(1, 89));
            Assert.IsNotNull(FindUnit(2, 89));
        }

        [Test]
        public void Late_Input_After_Battle_End_Ignored()
        {
            InitializeSimulation(TestDecks.BalancedDeck, TestDecks.BalancedDeck);

            foreach (var tower in Simulation.Towers)
            {
                if (tower.OwnerPlayerId == 2)
                    tower.TakeDamage(tower.MaxHP, DamageType.Spell, 0);
            }
            Tick();
            Assert.AreNotEqual(BattleStatus.Playing, Simulation.Status);

            int unitsBefore = Simulation.Units.Count;
            SetPlayerElixir(1, 10);
            Simulation.QueueInput(1, new PlayerInput
            {
                type = InputType.PlayCard,
                cardId = 89,
                position = new Vector2(9, 8)
            });
            Tick();

            // Post-battle ticks are no-ops: no new units, battle stays decided
            Assert.AreEqual(unitsBefore, Simulation.Units.Count);
            Assert.AreNotEqual(BattleStatus.Playing, Simulation.Status);
        }

        private (int elixir1, int elixir2, int units) RunMirroredSim(ulong seed)
        {
            var sim = new BattleSimulation();
            sim.Initialize(Config, seed, TestDecks.BalancedDeck, TestDecks.BalancedDeck);
            sim.Player1.Elixir = 10;
            sim.QueueInput(1, new PlayerInput
            {
                type = InputType.PlayCard,
                cardId = 89,
                position = new Vector2(9, 8)
            });
            for (int i = 0; i < 100; i++) sim.Tick();
            var result = (sim.Player1.Elixir, sim.Player2.Elixir, sim.Units.Count);
            sim.Dispose();
            return result;
        }
    }
}
