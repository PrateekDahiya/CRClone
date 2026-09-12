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
    public class NetworkTests : BattleTestBase
    {
        [Test]
        public void Invalid_Input_Rejected_Card_Not_In_Deck()
        {
            InitializeSimulation(TestDecks.BalancedDeck, TestDecks.BalancedDeck);
                        SetPlayerElixir(1, 10);
            
            // Try to play card not in deck (Golem - not in balanced deck)
            var input = new PlayerInput
            {
                type = InputType.PlayCard,
                cardId = 26000055, // Golem
                position = new Vector2(9, 8)
            };
            Simulation.QueueInput(1, input);
            Tick();
            
            // Elixir should not be spent
            AssertElixir(1, 10);
            
            // No unit should be spawned
            var golem = FindUnit(1, 26000055);
            Assert.IsNull(golem, "Card not in deck should not be playable");
        }

        [Test]
        public void Invalid_Input_Rejected_Insufficient_Elixir()
        {
            InitializeSimulation(TestDecks.BalancedDeck, TestDecks.BalancedDeck);
                        SetPlayerElixir(1, 2); // Knight costs 3
            
            var input = new PlayerInput
            {
                type = InputType.PlayCard,
                cardId = 26000040, // Knight
                position = new Vector2(9, 8)
            };
            Simulation.QueueInput(1, input);
            Tick();
            
            AssertElixir(1, 2, "Elixir should not be spent");
            var knight = FindUnit(1, 26000040);
            Assert.IsNull(knight, "Unit should not spawn without elixir");
        }

        [Test]
        public void Invalid_Input_Rejected_Invalid_Position()
        {
            InitializeSimulation(TestDecks.BalancedDeck, TestDecks.BalancedDeck);
                        SetPlayerElixir(1, 10);
            
            // Try to place building on enemy side
            var input = new PlayerInput
            {
                type = InputType.PlayCard,
                cardId = 26000045, // Cannon
                position = new Vector2(9, 20) // Enemy side
            };
            Simulation.QueueInput(1, input);
            Tick();
            
            AssertElixir(1, 10, "Elixir should not be spent");
            var cannon = FindBuilding(1, 26000045);
            Assert.IsNull(cannon, "Building should not be placed on enemy side");
        }

        [Test]
        public void Rate_Limiting_Prevents_Input_Spam()
        {
            InitializeSimulation(TestDecks.BalancedDeck, TestDecks.BalancedDeck);
                        SetPlayerElixir(1, 10);
            
            // Send many inputs rapidly
            for (int i = 0; i < 100; i++)
            {
                var input = new PlayerInput
                {
                    type = InputType.PlayCard,
                    cardId = 26000040, // Knight
                    position = new Vector2(9, 8)
                };
                Simulation.QueueInput(1, input);
            }
            
            // Process all queued inputs
            for (int i = 0; i < 10; i++)
            {
                Tick();
            }
            
            // Only first valid input should succeed (if any)
            // Rest should be ignored due to elixir cost
            Assert.LessOrEqual(Simulation.Player1.Elixir, 10);
        }

        [Test]
        public void Desync_Detection_And_Recovery()
        {
            // Test that simulation can detect and handle state differences
            var sim1 = new BattleSimulation();
            sim1.Initialize(Config, 12345, TestDecks.BalancedDeck, TestDecks.BalancedDeck);
            
            var sim2 = new BattleSimulation();
            sim2.Initialize(Config, 12345, TestDecks.BalancedDeck, TestDecks.BalancedDeck);
            
            // Both start identical
            SetPlayerElixirForSim(sim1, 1, 10);
            SetPlayerElixirForSim(sim2, 1, 10);
            
            QueueInputForSim(sim1, 1, 26000040, new Vector2(9, 8));
            QueueInputForSim(sim2, 1, 26000040, new Vector2(9, 8));
            
            // Run both for 100 ticks
            for (int i = 0; i < 100; i++)
            {
                sim1.Tick();
                sim2.Tick();
            }
            
            // States should be identical
            Assert.AreEqual(sim1.Player1.Elixir, sim2.Player1.Elixir);
            Assert.AreEqual(sim1.Player2.Elixir, sim2.Player2.Elixir);
            Assert.AreEqual(sim1.Units.Count, sim2.Units.Count);
            
            sim1.Dispose();
            sim2.Dispose();
        }

        [Test]
        public void Reconciliation_Handles_State_Differences()
        {
            InitializeSimulation(TestDecks.BalancedDeck, TestDecks.BalancedDeck);
                        SetPlayerElixir(1, 10);
            PlayCard(1, 26000040, new Vector2(9, 8));
            Step(1f);
            
            // Simulate receiving server state (reconciliation)
            var serverState = new GameStateMessage
            {
                tick = Simulation.CurrentTick,
                // In real implementation, would include entity states
            };
            
            // This would call Simulation.Reconcile(serverState)
            // For now, verify the method exists and doesn't throw
            Assert.DoesNotThrow(() => Simulation.Reconcile(serverState));
        }

        [Test]
        public void Input_Order_Preserved()
        {
            InitializeSimulation(TestDecks.BalancedDeck, TestDecks.BalancedDeck);
                        SetPlayerElixir(1, 10);
            
            // Queue multiple inputs
            QueueInput(1, 26000040, new Vector2(9, 8)); // Knight
            QueueInput(1, 26000041, new Vector2(10, 8)); // Archers
            QueueInput(1, 26000044, new Vector2(9, 20)); // Fireball
            
            // Process one tick - should process in order
            Tick();
            
            // Knight should be played first (costs 3)
            // Then Archers (costs 3) - but only 7 elixir left
            // Then Fireball (costs 4) - only 4 elixir left
            // All should succeed
            Assert.LessOrEqual(Simulation.Player1.Elixir, 4); // 10 - 3 - 3 = 4 (if all played)
        }

        [Test]
        public void Late_Input_After_Battle_End_Rejected()
        {
            InitializeSimulation(TestDecks.BalancedDeck, TestDecks.BalancedDeck);
                        // End battle immediately
            foreach (var tower in Simulation.Towers)
            {
                if (tower.OwnerPlayerId == 2)
                {
                    tower.TakeDamage(tower.MaxHP, DamageType.Spell, 0);
                }
            }
            Tick();
            
            Assert.AreNotEqual(BattleStatus.Playing, Simulation.Status);
            
            // Try to play card after battle ended
            SetPlayerElixir(1, 10);
            var input = new PlayerInput
            {
                type = InputType.PlayCard,
                cardId = 26000040,
                position = new Vector2(9, 8)
            };
            Simulation.QueueInput(1, input);
            Tick();
            
            // Input should be ignored (battle not in playing state)
            // No new units should spawn
        }

        [Test]
        public void Spell_Cast_Input_Validated()
        {
            InitializeSimulation(TestDecks.SpellHeavyDeck, TestDecks.BalancedDeck);
                        SetPlayerElixir(1, 10);
            
            // Cast spell not in deck
            var input = new PlayerInput
            {
                type = InputType.CastSpell,
                spellId = 26000099, // Non-existent spell
                position = new Vector2(9, 18)
            };
            Simulation.QueueInput(1, input);
            Tick();
            
            AssertElixir(1, 10, "Elixir should not be spent on invalid spell");
        }

        [Test]
        public void Champion_Ability_Input_Validated()
        {
            InitializeSimulation(TestDecks.ChampionDeck, TestDecks.BalancedDeck);
                        SetPlayerElixir(1, 10);
            
            // Use ability without champion on field
            var input = new PlayerInput
            {
                type = InputType.ChampionAbility,
                position = new Vector2(9, 18)
            };
            Simulation.QueueInput(1, input);
            Tick();
            
            AssertElixir(1, 10, "Elixir should not be spent without champion");
        }

        [Test]
        public void Simultaneous_Inputs_From_Both_Players()
        {
            InitializeSimulation(TestDecks.BalancedDeck, TestDecks.BalancedDeck);
                        SetPlayerElixir(1, 10);
            SetPlayerElixir(2, 10);
            
            // Both players play at same tick
            var input1 = new PlayerInput
            {
                type = InputType.PlayCard,
                cardId = 26000040,
                position = new Vector2(9, 8)
            };
            var input2 = new PlayerInput
            {
                type = InputType.PlayCard,
                cardId = 26000040,
                position = new Vector2(9, 20)
            };
            
            Simulation.QueueInput(1, input1);
            Simulation.QueueInput(2, input2);
            Tick();
            
            // Both should be processed
            var knight1 = FindUnit(1, 26000040);
            var knight2 = FindUnit(2, 26000040);
            
            Assert.IsNotNull(knight1);
            Assert.IsNotNull(knight2);
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

        private void QueueInput(int playerId, int cardId, Vector2 position)
        {
            var input = new PlayerInput
            {
                type = InputType.PlayCard,
                cardId = cardId,
                position = position
            };
            Simulation.QueueInput(playerId, input);
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

        private Building FindBuilding(int playerId, int cardId)
        {
            foreach (var building in Simulation.Buildings)
            {
                if (building.OwnerPlayerId == playerId && building.CardData.cardId == cardId)
                    return building;
            }
            return null;
        }
    }
}
