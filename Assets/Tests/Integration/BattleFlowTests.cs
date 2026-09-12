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
    public class BattleFlowTests : BattleTestBase
    {
        [Test]
        public void Complete_1v1_Battle_Reaches_Decision()
        {
            InitializeSimulation(TestDecks.BalancedDeck, TestDecks.BalancedDeck, 12345);

            PlayCard(1, 89, new Vector2(9, 8)); // Knight
            Step(2f);
            PlayCard(2, 89, new Vector2(9, 20)); // Knight
            Step(5f);
            CastSpell(1, 57, new Vector2(9, 20)); // Fireball
            Step(2f);

            // Let both sides keep pressuring until the battle ends
            int maxTicks = 60 * 60 * 4; // 4 minutes at 60Hz
            for (int i = 0; i < maxTicks && Simulation.Status == BattleStatus.Playing; i++)
            {
                if (i % 300 == 0 && Simulation.Player1.Elixir >= 3)
                    PlayCard(1, Simulation.Player1.Hand[0], new Vector2(9, 8));
                if (i % 300 == 150 && Simulation.Player2.Elixir >= 3)
                    PlayCard(2, Simulation.Player2.Hand[0], new Vector2(9, 24));
                Tick();
            }

            Assert.AreNotEqual(BattleStatus.Playing, Simulation.Status, "Battle should reach a decision");
        }

        [Test]
        public void Overtime_Sudden_Death_First_Tower_Wins()
        {
            InitializeSimulation(TestDecks.BalancedDeck, TestDecks.BalancedDeck);

            Step(181f); // Past regulation with 0-0 crowns: sudden death, still playing
            Assert.AreEqual(BattleStatus.Playing, Simulation.Status);

            // First tower destroyed in overtime decides it
            var p2Princess = FindTower(2, TowerType.PrincessLeft);
            Assert.IsNotNull(p2Princess);
            p2Princess.TakeDamage(p2Princess.MaxHP, DamageType.Spell, 0);
            Tick();

            Assert.AreEqual(BattleStatus.Player1Won, Simulation.Status);
        }

        [Test]
        public void Draw_When_Nothing_Destroyed()
        {
            InitializeSimulation(TestDecks.BuildingDeck, TestDecks.BuildingDeck);

            // No cards played: symmetric full-HP towers through regulation + overtime
            Step(361f);

            Assert.AreEqual(BattleStatus.Draw, Simulation.Status);
        }

        [Test]
        public void Regulation_Leader_Wins_At_Overtime_Start()
        {
            InitializeSimulation(TestDecks.BalancedDeck, TestDecks.BalancedDeck);

            // P1 takes a crown during regulation; battle continues until 3:00
            var p2Princess = FindTower(2, TowerType.PrincessRight);
            p2Princess.TakeDamage(p2Princess.MaxHP, DamageType.Spell, 0);
            Step(60f);
            Assert.AreEqual(BattleStatus.Playing, Simulation.Status, "Regulation continues despite crowns");

            Step(121f); // Past 180s mark: leader wins immediately
            Assert.AreEqual(BattleStatus.Player1Won, Simulation.Status);
        }

        [Test]
        public void King_Tower_Activation_Spawns_Guards()
        {
            InitializeSimulation(TestDecks.BalancedDeck, TestDecks.BalancedDeck);

            var king = FindTower(2, TowerType.King);
            Assert.IsNotNull(king);
            Assert.IsFalse(king.IsActivated);

            king.TakeDamage(100, DamageType.Spell, 0);

            Assert.IsTrue(king.IsActivated, "King Tower should activate when damaged");
            var guards = FindUnitsByName(2, "Guards");
            Assert.Greater(guards.Count, 0, "Activation should spawn Guards");
        }

        [Test]
        public void Battle_Ends_When_King_Tower_Destroyed()
        {
            InitializeSimulation(TestDecks.BalancedDeck, TestDecks.BalancedDeck);

            var p2King = FindTower(2, TowerType.King);
            Assert.IsNotNull(p2King);
            p2King.TakeDamage(p2King.MaxHP, DamageType.Spell, 0);

            Tick(); // Process death + win check

            Assert.AreEqual(BattleStatus.Player1Won, Simulation.Status);
        }

        [Test]
        public void Elixir_Generation_Continues_During_Battle()
        {
            InitializeSimulation(TestDecks.BalancedDeck, TestDecks.BalancedDeck);
            SetPlayerElixir(1, 0);
            SetPlayerElixir(2, 0);

            Step(5f);

            Assert.Greater(Simulation.Player1.Elixir, 0);
            Assert.Greater(Simulation.Player2.Elixir, 0);
        }

        [Test]
        public void Replay_Log_Captures_Events()
        {
            InitializeSimulation(TestDecks.BalancedDeck, TestDecks.BalancedDeck, 12345);

            PlayCard(1, 89, new Vector2(9, 8));
            Step(1f);

            var replayLog = Simulation.GetReplayLog();
            Assert.Greater(replayLog.Count, 0, "Replay log should have events");

            bool hasBattleStart = false;
            foreach (var evt in replayLog)
            {
                if (evt.type == ReplayEventType.BattleStart) { hasBattleStart = true; break; }
            }
            Assert.IsTrue(hasBattleStart, "Replay log should contain BattleStart event");
        }

        [Test]
        public void Deterministic_Replay_Same_Seed_Produces_Same_Result()
        {
            var log1 = RunScriptedBattle(12345);
            var log2 = RunScriptedBattle(12345);

            Assert.AreEqual(log1.status, log2.status, "Battle status must be deterministic");
            Assert.AreEqual(log1.events.Count, log2.events.Count, "Event count must match");
            for (int i = 0; i < log1.events.Count; i++)
            {
                Assert.AreEqual(log1.events[i].tick, log2.events[i].tick, $"Tick mismatch at event {i}");
                Assert.AreEqual(log1.events[i].type, log2.events[i].type, $"Type mismatch at event {i}");
                Assert.AreEqual(log1.events[i].playerId, log2.events[i].playerId, $"Player mismatch at event {i}");
            }
        }

        private (BattleStatus status, System.Collections.Generic.IReadOnlyList<ReplayEvent> events) RunScriptedBattle(ulong seed)
        {
            var sim = new BattleSimulation();
            sim.Initialize(Config, seed, TestDecks.BalancedDeck, TestDecks.BalancedDeck);
            sim.Player1.Elixir = 10;
            sim.QueueInput(1, new PlayerInput { type = InputType.PlayCard, cardId = 89, position = new Vector2(9, 8) });

            int guard = 0;
            while (sim.Status == BattleStatus.Playing && sim.CurrentTick < 1000 && guard++ < 1100)
                sim.Tick();

            var result = (sim.Status, sim.GetReplayLog());
            sim.Dispose();
            return result;
        }
    }
}
