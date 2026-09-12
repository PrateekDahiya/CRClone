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
    public class UnitCombatTests : BattleTestBase
    {
        [Test]
        public void Knight_Defeats_Single_Goblin()
        {
            InitializeSimulation(TestDecks.BalancedDeck, TestDecks.BalancedDeck);
                        // Spawn Knight (P1) and Goblin (P2) close to each other
            // We'll use queue input to spawn units
            SetPlayerElixir(1, 10);
            SetPlayerElixir(2, 10);
            
            var knightInput = new PlayerInput
            {
                type = InputType.PlayCard,
                cardId = 26000040, // Knight
                position = new Vector2(9, 8)
            };
            Simulation.QueueInput(1, knightInput);
            
            var goblinInput = new PlayerInput
            {
                type = InputType.PlayCard,
                cardId = 26000046, // Skeletons (using as goblin substitute - would need actual goblin ID)
                position = new Vector2(9, 12)
            };
            Simulation.QueueInput(2, goblinInput);
            
            Tick();
            
            // Step until one dies
            StepUntil(() => 
            {
                var knight = FindUnit(1, 26000040);
                var goblin = FindUnit(2, 26000046);
                return (knight != null && knight.IsDead) || (goblin != null && goblin.IsDead);
            }, maxSeconds: 10f);
            
            var finalKnight = FindUnit(1, 26000040);
            var finalGoblin = FindUnit(2, 26000046);
            
            Assert.IsNotNull(finalKnight, "Knight should exist");
            Assert.IsNotNull(finalGoblin, "Goblin should exist");
            
            // Knight should win against single goblin/skeleton
            Assert.IsFalse(finalKnight.IsDead, "Knight should survive");
            Assert.IsTrue(finalGoblin.IsDead, "Goblin should die");
        }

        [Test]
        public void Three_Goblins_Defeat_Knight()
        {
            InitializeSimulation(TestDecks.BalancedDeck, TestDecks.BalancedDeck);
                        SetPlayerElixir(1, 10);
            SetPlayerElixir(2, 10);
            
            // Spawn Knight
            var knightInput = new PlayerInput
            {
                type = InputType.PlayCard,
                cardId = 26000040, // Knight
                position = new Vector2(9, 8)
            };
            Simulation.QueueInput(1, knightInput);
            
            // Spawn 3 Skeletons (using as goblin substitute)
            for (int i = 0; i < 3; i++)
            {
                var goblinInput = new PlayerInput
                {
                    type = InputType.PlayCard,
                    cardId = 26000046, // Skeletons
                    position = new Vector2(9 + i * 0.5f, 12)
                };
                Simulation.QueueInput(2, goblinInput);
            }
            
            Tick();
            
            StepUntil(() => 
            {
                var knight = FindUnit(1, 26000040);
                var goblins = FindUnits(2, 26000046);
                return (knight != null && knight.IsDead) || goblins.TrueForAll(g => g.IsDead);
            }, maxSeconds: 10f);
            
            var finalKnight = FindUnit(1, 26000040);
            var finalGoblins = FindUnits(2, 26000046);
            
            Assert.IsNotNull(finalKnight);
            
            // 3 goblins should defeat knight
            Assert.IsTrue(finalKnight.IsDead, "Knight should die to 3 goblins");
        }

        [Test]
        public void Ranged_Unit_Attacks_From_Distance()
        {
            InitializeSimulation(TestDecks.BalancedDeck, TestDecks.BalancedDeck);
                        SetPlayerElixir(1, 10);
            SetPlayerElixir(2, 10);
            
            // Musketeer (ranged, 6 tile range) vs Knight (melee)
            var musketeerInput = new PlayerInput
            {
                type = InputType.PlayCard,
                cardId = 26000043, // Musketeer
                position = new Vector2(9, 5)
            };
            Simulation.QueueInput(1, musketeerInput);
            
            var knightInput = new PlayerInput
            {
                type = InputType.PlayCard,
                cardId = 26000040, // Knight
                position = new Vector2(9, 12)
            };
            Simulation.QueueInput(2, knightInput);
            
            Tick();
            
            // Step 1 second - knight walks toward musketeer
            Step(1f);
            
            var musketeer = FindUnit(1, 26000043);
            var knight = FindUnit(2, 26000040);
            
            Assert.IsNotNull(musketeer);
            Assert.IsNotNull(knight);
            
            // Distance should be ~7 tiles, musketeer range is 6, so not in range yet
            float dist = Vector2.Distance(musketeer.Position, knight.Position);
            Assert.Greater(dist, 6f, "Should be out of range initially");
            
            // Step more - knight gets in range
            Step(3f);
            
            // Musketeer should have attacked
            Assert.Less(knight.CurrentHP, knight.MaxHP, "Knight should have taken damage from musketeer");
        }

        [Test]
        public void Splash_Damage_Hits_Multiple_Units()
        {
            InitializeSimulation(TestDecks.BalancedDeck, TestDecks.BalancedDeck);
                        SetPlayerElixir(1, 10);
            SetPlayerElixir(2, 10);
            
            // Wizard (splash) vs 3 Skeletons
            var wizardInput = new PlayerInput
            {
                type = InputType.PlayCard,
                cardId = 26000058, // Wizard
                position = new Vector2(9, 5)
            };
            Simulation.QueueInput(1, wizardInput);
            
            for (int i = 0; i < 3; i++)
            {
                var goblinInput = new PlayerInput
                {
                    type = InputType.PlayCard,
                    cardId = 26000046, // Skeletons
                    position = new Vector2(9 + i * 0.8f, 8)
                };
                Simulation.QueueInput(2, goblinInput);
            }
            
            Tick();
            
            // Wait for wizard to attack
            StepUntil(() => 
            {
                var wizard = FindUnit(1, 26000058);
                return wizard != null && wizard.AttackCooldown <= 0;
            }, maxSeconds: 5f);
            
            Step(0.1f); // Process attack
            
            var goblins = FindUnits(2, 26000046);
            
            // All 3 goblins should take splash damage
            foreach (var g in goblins)
            {
                Assert.Less(g.CurrentHP, g.MaxHP, "All goblins should take splash damage");
            }
        }

        [Test]
        public void Melee_Unit_Must_Reach_Target()
        {
            InitializeSimulation(TestDecks.BalancedDeck, TestDecks.BalancedDeck);
                        SetPlayerElixir(1, 10);
            SetPlayerElixir(2, 10);
            
            var knightInput = new PlayerInput
            {
                type = InputType.PlayCard,
                cardId = 26000040, // Knight
                position = new Vector2(9, 5)
            };
            Simulation.QueueInput(1, knightInput);
            
            var archerInput = new PlayerInput
            {
                type = InputType.PlayCard,
                cardId = 26000041, // Archers
                position = new Vector2(9, 25)
            };
            Simulation.QueueInput(2, archerInput);
            
            Tick();
            
            // Knight should move toward archers
            Step(2f);
            
            var knight = FindUnit(1, 26000040);
            Assert.IsNotNull(knight);
            
            // Knight should be moving (not idle)
            Assert.AreNotEqual(UnitState.Idle, knight.State);
        }

        [Test]
        public void Flying_Unit_Ignores_Ground_Only_Targets()
        {
            // Minions (flying) should be able to target air and ground
            // But ground-only units cannot target them
            InitializeSimulation(TestDecks.BalancedDeck, TestDecks.BalancedDeck);
                        SetPlayerElixir(1, 10);
            SetPlayerElixir(2, 10);
            
            // Minions (flying)
            var minionInput = new PlayerInput
            {
                type = InputType.PlayCard,
                cardId = 26000047, // Minions
                position = new Vector2(9, 8)
            };
            Simulation.QueueInput(1, minionInput);
            
            // Cannon (building, ground only)
            var cannonInput = new PlayerInput
            {
                type = InputType.PlayCard,
                cardId = 26000045, // Cannon
                position = new Vector2(9, 12)
            };
            Simulation.QueueInput(2, cannonInput);
            
            Tick();
            
            Step(2f);
            
            // Cannon should NOT target minions (air unit)
            var cannon = FindBuilding(2, 26000045);
            var minions = FindUnits(1, 26000047);
            
            Assert.IsNotNull(cannon);
            Assert.AreNotEqual(minions[0], cannon.Target, "Cannon should not target air units");
        }

        [Test]
        public void Unit_Retargets_When_Target_Dies()
        {
            InitializeSimulation(TestDecks.BalancedDeck, TestDecks.BalancedDeck);
                        SetPlayerElixir(1, 10);
            SetPlayerElixir(2, 10);
            
            // Wizard vs 2 Skeletons
            var wizardInput = new PlayerInput
            {
                type = InputType.PlayCard,
                cardId = 26000058, // Wizard
                position = new Vector2(9, 5)
            };
            Simulation.QueueInput(1, wizardInput);
            
            for (int i = 0; i < 2; i++)
            {
                var skelInput = new PlayerInput
                {
                    type = InputType.PlayCard,
                    cardId = 26000046, // Skeletons
                    position = new Vector2(9 + i, 8)
                };
                Simulation.QueueInput(2, skelInput);
            }
            
            Tick();
            
            // Wait for first skeleton to die
            StepUntil(() => 
            {
                var skels = FindUnits(2, 26000046);
                return skels.Count == 2 && skels[0].IsDead;
            }, maxSeconds: 5f);
            
            var wizard = FindUnit(1, 26000058);
            var remainingSkels = FindUnits(2, 26000046);
            
            Assert.IsNotNull(wizard);
            Assert.AreEqual(1, remainingSkels.Count);
            Assert.AreEqual(remainingSkels[0], wizard.Target, "Wizard should retarget to remaining skeleton");
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
