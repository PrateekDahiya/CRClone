using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using CRClone.Battle.Simulation;
using CRClone.Core;
using CRClone.Tests.TestFixtures;
using CRClone.Network;

namespace CRClone.Tests.Integration
{
    [TestFixture]
    public class BattleFlowTests : BattleTestBase
    {
        [Test]
        public void Complete_1v1_Battle_From_Start_To_End()
        {
            InitializeSimulation(TestDecks.BalancedDeck, TestDecks.BalancedDeck, 12345);
                        SetPlayerElixir(1, 10);
            SetPlayerElixir(2, 10);
            
            // Play a simple game: Knight vs Knight
            PlayCard(1, 26000040, new Vector2(9, 8)); // Knight
            Step(2f);
            
            PlayCard(2, 26000040, new Vector2(9, 20)); // Knight
            Step(5f);
            
            // Fireball the enemy knight
            PlayCard(1, 26000044, new Vector2(9, 20)); // Fireball
            Step(2f);
            
            // Continue battle until end
            int maxTicks = 3600 * 3; // 3 minutes at 60Hz
            for (int i = 0; i < maxTicks && Simulation.Status == BattleStatus.Playing; i++)
            {
                Tick();
                
                // Occasionally play cards
                if (i % 300 == 0 && Simulation.Player1.Elixir >= 3)
                {
                    var card = Simulation.Player1.Hand[0];
                    PlayCard(1, card, new Vector2(9, 8));
                }
                if (i % 300 == 150 && Simulation.Player2.Elixir >= 3)
                {
                    var card = Simulation.Player2.Hand[0];
                    PlayCard(2, card, new Vector2(9, 20));
                }
            }
            
            // Battle should end
            Assert.AreNotEqual(BattleStatus.Playing, Simulation.Status, "Battle should have ended");
            Assert.IsTrue(Simulation.Status == BattleStatus.Player1Won || 
                         Simulation.Status == BattleStatus.Player2Won || 
                         Simulation.Status == BattleStatus.Draw);
        }

        [Test]
        public void Overtime_Sudden_Death_Works()
        {
            // Create scenario where both players have 1-1 crowns and low king HP
            InitializeSimulation(TestDecks.BalancedDeck, TestDecks.BalancedDeck);
                        // Manually set up overtime scenario by damaging towers
            foreach (var tower in Simulation.Towers)
            {
                if (tower.Type == TowerType.PrincessLeft || tower.Type == TowerType.PrincessRight)
                {
                    tower.TakeDamage(tower.MaxHP - 1, DamageType.Spell, 0);
                }
            }
            
            // Fast forward to overtime
            Step(181f); // Past 3 minutes
            
            Assert.IsTrue(Simulation.CurrentTick >= 180 * 60, "Should be in overtime");
            
            SetPlayerElixir(1, 10);
            SetPlayerElixir(2, 10);
            
            // First to damage king tower wins
            PlayCard(1, 26000055, new Vector2(9, 15)); // Hog Rider toward king
            
            int maxOvertimeTicks = 180 * 60; // 3 minutes overtime
            for (int i = 0; i < maxOvertimeTicks && Simulation.Status == BattleStatus.Playing; i++)
            {
                Tick();
            }
            
            Assert.AreNotEqual(BattleStatus.Playing, Simulation.Status);
            Assert.IsTrue(Simulation.Status == BattleStatus.Player1Won || 
                         Simulation.Status == BattleStatus.Player2Won);
        }

        [Test]
        public void Draw_When_No_Towers_Destroyed_In_Overtime()
        {
            InitializeSimulation(TestDecks.BuildingDeck, TestDecks.BuildingDeck);
                        // Fast forward to end of overtime (3 + 3 = 6 minutes)
            Step(361f);
            
            // Neither player attacks - just wait
            int maxTicks = 180 * 60;
            for (int i = 0; i < maxTicks && Simulation.Status == BattleStatus.Playing; i++)
            {
                Tick();
            }
            
            Assert.AreEqual(BattleStatus.Draw, Simulation.Status, "Should be draw when no towers destroyed in overtime");
        }

        [Test]
        public void King_Tower_Activation_Spawns_Guards()
        {
            InitializeSimulation(TestDecks.BalancedDeck, TestDecks.BalancedDeck);
                        // Damage King Tower to activate it
            Tower kingTower = null;
            foreach (var tower in Simulation.Towers)
            {
                if (tower.OwnerPlayerId == 2 && tower.Type == TowerType.King)
                {
                    kingTower = tower;
                    break;
                }
            }
            
            Assert.IsNotNull(kingTower);
            Assert.IsFalse(kingTower.IsActivated);
            
            // Damage king tower
            kingTower.TakeDamage(100, DamageType.Spell, 0);
            
            Assert.IsTrue(kingTower.IsActivated, "King Tower should activate when damaged");
            Assert.AreEqual(2, kingTower.GuardsSpawned.Count, "Should spawn 2 guards");
        }

        [Test]
        public void Battle_Ends_When_King_Tower_Destroyed()
        {
            InitializeSimulation(TestDecks.BalancedDeck, TestDecks.BalancedDeck);
                        // Destroy P2 King Tower directly
            Tower p2King = null;
            foreach (var tower in Simulation.Towers)
            {
                if (tower.OwnerPlayerId == 2 && tower.Type == TowerType.King)
                {
                    p2King = tower;
                    break;
                }
            }
            
            Assert.IsNotNull(p2King);
            p2King.TakeDamage(p2King.MaxHP, DamageType.Spell, 0);
            
            Tick(); // Process death
            
            Assert.AreEqual(BattleStatus.Player1Won, Simulation.Status, "Player 1 should win when P2 King destroyed");
        }

        [Test]
        public void Three_Crown_Win_When_All_Towers_Destroyed()
        {
            InitializeSimulation(TestDecks.BalancedDeck, TestDecks.BalancedDeck);
                        // Destroy all P2 towers
            foreach (var tower in Simulation.Towers)
            {
                if (tower.OwnerPlayerId == 2)
                {
                    tower.TakeDamage(tower.MaxHP, DamageType.Spell, 0);
                }
            }
            
            Tick();
            
            Assert.AreEqual(BattleStatus.Player1Won, Simulation.Status);
            // Winner gets 3 crowns
        }

        [Test]
        public void Elixir_Generation_Continues_During_Battle()
        {
            InitializeSimulation(TestDecks.BalancedDeck, TestDecks.BalancedDeck);
                        SetPlayerElixir(1, 0);
            SetPlayerElixir(2, 0);
            
            Step(5f); // Wait for elixir generation
            
            Assert.Greater(Simulation.Player1.Elixir, 0);
            Assert.Greater(Simulation.Player2.Elixir, 0);
        }

        [Test]
        public void Card_Cycle_Continues_Throughout_Battle()
        {
            InitializeSimulation(TestDecks.BalancedDeck, TestDecks.BalancedDeck);
                        SetPlayerElixir(1, 10);
            
            int initialHandCount = Simulation.Player1.Hand.Length;
            
            // Play cards throughout battle
            for (int i = 0; i < 20; i++)
            {
                if (Simulation.Player1.Elixir >= 3)
                {
                    var card = Simulation.Player1.Hand[0];
                    PlayCard(1, card, new Vector2(9, 8));
                }
                Step(3f); // Wait for elixir
            }
            
            // Hand should always have 4 cards
            Assert.AreEqual(initialHandCount, Simulation.Player1.Hand.Length);
        }

        [Test]
        public void Replay_Log_Captures_All_Events()
        {
            InitializeSimulation(TestDecks.BalancedDeck, TestDecks.BalancedDeck, 12345);
                        SetPlayerElixir(1, 10);
            PlayCard(1, 26000040, new Vector2(9, 8)); // Knight
            Step(1f);
            
            var replayLog = Simulation.GetReplayLog();
            
            Assert.Greater(replayLog.Count, 0, "Replay log should have events");
            
            // Should have battle start event
            bool hasBattleStart = false;
            foreach (var evt in replayLog)
            {
                if (evt.type == ReplayEventType.BattleStart)
                {
                    hasBattleStart = true;
                    break;
                }
            }
            Assert.IsTrue(hasBattleStart, "Replay log should contain BattleStart event");
        }

        [Test]
        public void Deterministic_Replay_Same_Seed_Produces_Same_Result()
        {
            // Run battle 1
            var sim1 = new BattleSimulation();
            sim1.Initialize(Config, 12345, TestDecks.BalancedDeck, TestDecks.BalancedDeck);
            
            SetPlayerElixirForSim(sim1, 1, 10);
            QueueInputForSim(sim1, 1, 26000040, new Vector2(9, 8));
            
            while (sim1.Status == BattleStatus.Playing && sim1.CurrentTick < 1000)
            {
                sim1.Tick();
            }
            
            var log1 = sim1.GetReplayLog();
            var status1 = sim1.Status;
            sim1.Dispose();
            
            // Run battle 2 with same seed
            var sim2 = new BattleSimulation();
            sim2.Initialize(Config, 12345, TestDecks.BalancedDeck, TestDecks.BalancedDeck);
            
            SetPlayerElixirForSim(sim2, 1, 10);
            QueueInputForSim(sim2, 1, 26000040, new Vector2(9, 8));
            
            while (sim2.Status == BattleStatus.Playing && sim2.CurrentTick < 1000)
            {
                sim2.Tick();
            }
            
            var log2 = sim2.GetReplayLog();
            var status2 = sim2.Status;
            sim2.Dispose();
            
            // Results must match exactly
            Assert.AreEqual(status1, status2, "Battle status must be deterministic");
            Assert.AreEqual(log1.Count, log2.Count, "Event count must match");
            
            for (int i = 0; i < log1.Count; i++)
            {
                Assert.AreEqual(log1[i].tick, log2[i].tick, $"Tick mismatch at event {i}");
                Assert.AreEqual(log1[i].type, log2[i].type, $"Type mismatch at event {i}");
                Assert.AreEqual(log1[i].playerId, log2[i].playerId, $"Player mismatch at event {i}");
            }
        }

        private void PlayCard(int playerId, int cardId, Vector2 position)
        {
            var input = new PlayerInput
            {
                type = InputType.PlayCard,
                cardId = cardId,
                position = position
            };
            Simulation.QueueInput(playerId, input);
            Tick();
        }

        private void SetPlayerElixirForSim(BattleSimulation sim, int playerId, int elixir)
        {
            var player = playerId == 1 ? sim.Player1 : sim.Player2;
            if (player != null) player.Elixir = elixir;
        }

        private void QueueInputForSim(BattleSimulation sim, int playerId, int cardId, Vector2 position)
        {
            var input = new PlayerInput
            {
                type = InputType.PlayCard,
                cardId = cardId,
                position = position
            };
            sim.QueueInput(playerId, input);
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
    }
}
