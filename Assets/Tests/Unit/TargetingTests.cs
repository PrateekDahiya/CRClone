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
    public class TargetingTests : BattleTestBase
    {
        [Test]
        public void Units_Target_Closest_By_Path_Distance()
        {
            InitializeSimulation(TestDecks.BalancedDeck, TestDecks.BalancedDeck);
                        SetPlayerElixir(1, 10);
            SetPlayerElixir(2, 10);
            
            // Knight at bottom, two Goblins at top - one straight, one around river
            var knightInput = new PlayerInput
            {
                type = InputType.PlayCard,
                cardId = 26000040, // Knight
                position = new Vector2(9, 5)
            };
            Simulation.QueueInput(1, knightInput);
            
            var goblin1Input = new PlayerInput
            {
                type = InputType.PlayCard,
                cardId = 26000046, // Skeletons (as goblin substitute)
                position = new Vector2(9, 25) // Straight line path
            };
            Simulation.QueueInput(2, goblin1Input);
            
            var goblin2Input = new PlayerInput
            {
                type = InputType.PlayCard,
                cardId = 26000046,
                position = new Vector2(12, 22) // Around river
            };
            Simulation.QueueInput(2, goblin2Input);
            
            Tick();
            Step(1f);
            
            var knight = FindUnit(1, 26000040);
            var goblin1 = FindUnit(2, 26000046); // First one spawned
            
            Assert.IsNotNull(knight);
            // Goblin1 is closer by path (river blocks direct path to goblin2)
            Assert.AreEqual(goblin1, knight.Target, "Should target closest by path distance");
        }

        [Test]
        public void Building_Targeters_Ignore_Troops()
        {
            InitializeSimulation(TestDecks.BalancedDeck, TestDecks.BuildingDeck);
                        SetPlayerElixir(1, 10);
            SetPlayerElixir(2, 10);
            
            // Giant (building targeter) vs Knight (troop) and Cannon (building)
            var giantInput = new PlayerInput
            {
                type = InputType.PlayCard,
                cardId = 26000042, // Giant
                position = new Vector2(9, 5)
            };
            Simulation.QueueInput(1, giantInput);
            
            var knightInput = new PlayerInput
            {
                type = InputType.PlayCard,
                cardId = 26000040, // Knight
                position = new Vector2(9, 9)
            };
            Simulation.QueueInput(2, knightInput);
            
            var cannonInput = new PlayerInput
            {
                type = InputType.PlayCard,
                cardId = 26000045, // Cannon
                position = new Vector2(9, 12)
            };
            Simulation.QueueInput(2, cannonInput);
            
            Tick();
            Step(2f);
            
            var giant = FindUnit(1, 26000042);
            var cannon = FindBuilding(2, 26000045);
            
            Assert.IsNotNull(giant);
            Assert.IsNotNull(cannon);
            
            // Giant targets buildings only
            Assert.AreEqual(cannon, giant.Target, "Giant should target buildings, not troops");
        }

        [Test]
        public void Retarget_On_Target_Death()
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
            
            var skel1Input = new PlayerInput
            {
                type = InputType.PlayCard,
                cardId = 26000046, // Skeletons
                position = new Vector2(9, 18)
            };
            Simulation.QueueInput(2, skel1Input);
            
            var skel2Input = new PlayerInput
            {
                type = InputType.PlayCard,
                cardId = 26000046,
                position = new Vector2(10, 18)
            };
            Simulation.QueueInput(2, skel2Input);
            
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
            Assert.AreEqual(remainingSkels[0], wizard.Target, "Should retarget to remaining skeleton");
        }

        [Test]
        public void Units_Prioritize_Closer_Targets_Over_Higher_Priority()
        {
            InitializeSimulation(TestDecks.BalancedDeck, TestDecks.BalancedDeck);
                        SetPlayerElixir(1, 10);
            SetPlayerElixir(2, 10);
            
            // Musketeer - normal troop targeter
            // Closer Goblin vs Further Knight
            var muskInput = new PlayerInput
            {
                type = InputType.PlayCard,
                cardId = 26000043, // Musketeer
                position = new Vector2(9, 5)
            };
            Simulation.QueueInput(1, muskInput);
            
            var goblinInput = new PlayerInput
            {
                type = InputType.PlayCard,
                cardId = 26000046, // Skeletons (closer)
                position = new Vector2(9, 10)
            };
            Simulation.QueueInput(2, goblinInput);
            
            var knightInput = new PlayerInput
            {
                type = InputType.PlayCard,
                cardId = 26000040, // Knight (further but higher priority target)
                position = new Vector2(9, 18)
            };
            Simulation.QueueInput(2, knightInput);
            
            Tick();
            Step(1f);
            
            var musk = FindUnit(1, 26000043);
            var goblin = FindUnit(2, 26000046);
            
            Assert.IsNotNull(musk);
            Assert.IsNotNull(goblin);
            
            // Should target closer goblin despite knight being "better" target
            Assert.AreEqual(goblin, musk.Target, "Should prioritize closer target");
        }

        [Test]
        public void Air_Units_Targeted_By_Anti_Air_Only()
        {
            InitializeSimulation(TestDecks.BalancedDeck, TestDecks.BalancedDeck);
                        SetPlayerElixir(1, 10);
            SetPlayerElixir(2, 10);
            
            // Minions (air) vs Musketeer (anti-air) and Knight (ground only)
            var minionInput = new PlayerInput
            {
                type = InputType.PlayCard,
                cardId = 26000047, // Minions
                position = new Vector2(9, 18)
            };
            Simulation.QueueInput(2, minionInput);
            
            var muskInput = new PlayerInput
            {
                type = InputType.PlayCard,
                cardId = 26000043, // Musketeer (can target air)
                position = new Vector2(9, 5)
            };
            Simulation.QueueInput(1, muskInput);
            
            var knightInput = new PlayerInput
            {
                type = InputType.PlayCard,
                cardId = 26000040, // Knight (ground only)
                position = new Vector2(10, 5)
            };
            Simulation.QueueInput(1, knightInput);
            
            Tick();
            Step(1f);
            
            var musk = FindUnit(1, 26000043);
            var knight = FindUnit(1, 26000040);
            var minions = FindUnits(2, 26000047);
            
            Assert.IsNotNull(musk);
            Assert.IsNotNull(knight);
            Assert.Greater(minions.Count, 0);
            
            // Musketeer should target minions (can target air)
            Assert.AreEqual(minions[0], musk.Target, "Musketeer should target air units");
            
            // Knight should NOT target minions (ground only)
            Assert.AreNotEqual(minions[0], knight.Target, "Knight should not target air units");
        }

        [Test]
        public void Invisible_Units_Cannot_Be_Targeted()
        {
            InitializeSimulation(TestDecks.ChampionDeck, TestDecks.BalancedDeck);
                        SetPlayerElixir(1, 10);
            SetPlayerElixir(2, 10);
            
            var aqInput = new PlayerInput
            {
                type = InputType.PlayCard,
                cardId = 27000000, // Archer Queen
                position = new Vector2(9, 8)
            };
            Simulation.QueueInput(1, aqInput);
            
            var muskInput = new PlayerInput
            {
                type = InputType.PlayCard,
                cardId = 26000043, // Musketeer
                position = new Vector2(9, 18)
            };
            Simulation.QueueInput(2, muskInput);
            Tick();
            
            var aq = FindUnit(1, 27000000);
            var musk = FindUnit(2, 26000043);
            
            Assert.IsNotNull(aq);
            Assert.IsNotNull(musk);
            
            // Use ability - AQ becomes invisible
            var abilityInput = new PlayerInput
            {
                type = InputType.ChampionAbility,
                position = new Vector2(9, 8)
            };
            Simulation.QueueInput(1, abilityInput);
            Tick();
            Step(0.1f);
            
            aq = FindUnit(1, 27000000);
            Assert.IsTrue(aq.IsInvisible);
            
            // Musketeer should not be able to target invisible AQ
            // AQ should not be musk's target (musk will look for other targets)
            Assert.AreNotEqual(aq, musk.Target, "Invisible units should not be targetable");
        }

        [Test]
        public void Splash_Damage_Reveals_Invisible_Units()
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
            
            var wizardInput = new PlayerInput
            {
                type = InputType.PlayCard,
                cardId = 26000058, // Wizard (splash)
                position = new Vector2(9, 18)
            };
            Simulation.QueueInput(2, wizardInput);
            Tick();
            
            var aq = FindUnit(1, 27000000);
            var wizard = FindUnit(2, 26000058);
            
            Assert.IsNotNull(aq);
            Assert.IsNotNull(wizard);
            
            // Use ability - AQ becomes invisible
            var abilityInput = new PlayerInput
            {
                type = InputType.ChampionAbility,
                position = new Vector2(9, 8)
            };
            Simulation.QueueInput(1, abilityInput);
            Tick();
            Step(0.1f);
            
            aq = FindUnit(1, 27000000);
            Assert.IsTrue(aq.IsInvisible);
            
            // Wizard attacks near AQ (splash damage)
            StepUntil(() => wizard.AttackCooldown <= 0, maxSeconds: 5f);
            Step(0.1f);
            
            aq = FindUnit(1, 27000000);
            Assert.IsFalse(aq.IsInvisible, "Splash damage should reveal invisible units");
        }

        [Test]
        public void King_Tower_Targets_Units_In_Range()
        {
            InitializeSimulation(TestDecks.BalancedDeck, TestDecks.BalancedDeck);
                        SetPlayerElixir(2, 10);
            
            // Spawn Knight near enemy King Tower
            var knightInput = new PlayerInput
            {
                type = InputType.PlayCard,
                cardId = 26000040,
                position = new Vector2(9, 15) // Near P1 King Tower
            };
            Simulation.QueueInput(2, knightInput);
            Tick();
            
            Step(1f);
            
            // Find P1 King Tower
            Tower kingTower = null;
            foreach (var tower in Simulation.Towers)
            {
                if (tower.OwnerPlayerId == 1 && tower.Type == TowerType.King)
                {
                    kingTower = tower;
                    break;
                }
            }
            
            Assert.IsNotNull(kingTower);
            
            var knight = FindUnit(2, 26000040);
            Assert.IsNotNull(knight);
            
            // King Tower should target knight in range
            Assert.AreEqual(knight, kingTower.Target, "King Tower should target units in range");
        }

        [Test]
        public void Princess_Towers_Target_Units_In_Range()
        {
            InitializeSimulation(TestDecks.BalancedDeck, TestDecks.BalancedDeck);
                        SetPlayerElixir(2, 10);
            
            // Spawn Knight near enemy Princess Tower
            var knightInput = new PlayerInput
            {
                type = InputType.PlayCard,
                cardId = 26000040,
                position = new Vector2(3, 15) // Near P1 Left Princess Tower
            };
            Simulation.QueueInput(2, knightInput);
            Tick();
            
            Step(1f);
            
            // Find P1 Left Princess Tower
            Tower princessTower = null;
            foreach (var tower in Simulation.Towers)
            {
                if (tower.OwnerPlayerId == 1 && tower.Type == TowerType.PrincessLeft)
                {
                    princessTower = tower;
                    break;
                }
            }
            
            Assert.IsNotNull(princessTower);
            
            var knight = FindUnit(2, 26000040);
            Assert.IsNotNull(knight);
            
            Assert.AreEqual(knight, princessTower.Target, "Princess Tower should target units in range");
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
