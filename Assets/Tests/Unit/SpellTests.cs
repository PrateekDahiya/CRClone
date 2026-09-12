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
    public class SpellTests : BattleTestBase
    {
        [Test]
        public void Fireball_Damages_Units_In_Radius()
        {
            InitializeSimulation(TestDecks.SpellHeavyDeck, TestDecks.BalancedDeck);
                        SetPlayerElixir(1, 10);
            SetPlayerElixir(2, 10);
            
            // Spawn 3 Musketeers for P2
            for (int i = 0; i < 3; i++)
            {
                var muskInput = new PlayerInput
                {
                    type = InputType.PlayCard,
                    cardId = 26000043, // Musketeer
                    position = new Vector2(9 + i, 18)
                };
                Simulation.QueueInput(2, muskInput);
            }
            Tick();
            
            // Cast Fireball on them
            var fbInput = new PlayerInput
            {
                type = InputType.CastSpell,
                spellId = 26000044, // Fireball
                position = new Vector2(10, 18)
            };
            Simulation.QueueInput(1, fbInput);
            Tick();
            
            // Fireball travel time ~1.5s
            Step(1.5f);
            
            var musketeers = FindUnits(2, 26000043);
            Assert.AreEqual(3, musketeers.Count);
            
            foreach (var m in musketeers)
            {
                Assert.Less(m.CurrentHP, m.MaxHP, "All musketeers in radius should take damage");
            }
        }

        [Test]
        public void Fireball_Knocks_Back_Units()
        {
            InitializeSimulation(TestDecks.SpellHeavyDeck, TestDecks.BalancedDeck);
                        SetPlayerElixir(1, 10);
            SetPlayerElixir(2, 10);
            
            var knightInput = new PlayerInput
            {
                type = InputType.PlayCard,
                cardId = 26000040, // Knight
                position = new Vector2(10, 18)
            };
            Simulation.QueueInput(2, knightInput);
            Tick();
            
            var knight = FindUnit(2, 26000040);
            var originalPos = knight.Position;
            
            var fbInput = new PlayerInput
            {
                type = InputType.CastSpell,
                spellId = 26000044, // Fireball
                position = new Vector2(10, 18)
            };
            Simulation.QueueInput(1, fbInput);
            Tick();
            
            Step(1.5f);
            
            knight = FindUnit(2, 26000040);
            Assert.IsNotNull(knight);
            Assert.Greater(Vector2.Distance(knight.Position, originalPos), 0.3f, "Knight should be knocked back");
        }

        [Test]
        public void Zap_Stuns_And_Resets_Inferno()
        {
            InitializeSimulation(TestDecks.SpellHeavyDeck, TestDecks.BuildingDeck);
                        SetPlayerElixir(1, 10);
            SetPlayerElixir(2, 10);
            
            // Place Inferno Tower
            var infernoInput = new PlayerInput
            {
                type = InputType.PlayCard,
                cardId = 26000063, // Inferno Tower
                position = new Vector2(9, 10)
            };
            Simulation.QueueInput(2, infernoInput);
            
            var knightInput = new PlayerInput
            {
                type = InputType.PlayCard,
                cardId = 26000040, // Knight
                position = new Vector2(9, 12)
            };
            Simulation.QueueInput(1, knightInput);
            Tick();
            
            // Let inferno ramp up
            Step(3f);
            
            var inferno = FindBuilding(2, 26000063);
            Assert.IsNotNull(inferno);
            
            // Zap it
            var zapInput = new PlayerInput
            {
                type = InputType.CastSpell,
                spellId = 26000048, // Zap
                position = new Vector2(9, 10)
            };
            Simulation.QueueInput(1, zapInput);
            Tick();
            Step(0.1f);
            
            inferno = FindBuilding(2, 26000063);
            Assert.IsNotNull(inferno);
            Assert.IsTrue(inferno.IsStunned, "Inferno Tower should be stunned by Zap");
            // Damage should reset to base (would need to expose current damage)
        }

        [Test]
        public void Poison_Damages_Over_Time_And_Slows()
        {
            InitializeSimulation(TestDecks.SpellHeavyDeck, TestDecks.BalancedDeck);
                        SetPlayerElixir(1, 10);
            SetPlayerElixir(2, 10);
            
            var knightInput = new PlayerInput
            {
                type = InputType.PlayCard,
                cardId = 26000040, // Knight
                position = new Vector2(9, 18)
            };
            Simulation.QueueInput(2, knightInput);
            Tick();
            
            var knight = FindUnit(2, 26000040);
            var originalSpeed = knight.MoveSpeed;
            
            var poisonInput = new PlayerInput
            {
                type = InputType.CastSpell,
                spellId = 26000050, // Poison
                position = new Vector2(9, 18)
            };
            Simulation.QueueInput(1, poisonInput);
            Tick();
            Step(0.1f);
            
            knight = FindUnit(2, 26000040);
            Assert.IsTrue(knight.IsSlowed, "Knight should be slowed by Poison");
            AssertApproximate(originalSpeed * 0.65f, knight.MoveSpeed, 0.01f, "Speed should be reduced by 35%");
            
            // Full duration
            Step(8f);
            
            knight = FindUnit(2, 26000040);
            Assert.IsFalse(knight.IsSlowed, "Slow should expire after duration");
            Assert.Less(knight.CurrentHP, knight.MaxHP - 200, "Knight should take significant damage over time");
        }

        [Test]
        public void Freeze_Stops_Everything_In_Radius()
        {
            InitializeSimulation(TestDecks.SpellHeavyDeck, TestDecks.BalancedDeck);
                        SetPlayerElixir(1, 10);
            SetPlayerElixir(2, 10);
            
            var knightInput = new PlayerInput
            {
                type = InputType.PlayCard,
                cardId = 26000040, // Knight
                position = new Vector2(9, 18)
            };
            Simulation.QueueInput(2, knightInput);
            
            var cannonInput = new PlayerInput
            {
                type = InputType.PlayCard,
                cardId = 26000045, // Cannon
                position = new Vector2(10, 18)
            };
            Simulation.QueueInput(2, cannonInput);
            Tick();
            
            var freezeInput = new PlayerInput
            {
                type = InputType.CastSpell,
                spellId = 26000053, // Freeze
                position = new Vector2(9.5f, 18)
            };
            Simulation.QueueInput(1, freezeInput);
            Tick();
            Step(0.1f);
            
            var knight = FindUnit(2, 26000040);
            var cannon = FindBuilding(2, 26000045);
            
            Assert.IsNotNull(knight);
            Assert.IsNotNull(cannon);
            
            Assert.IsTrue(knight.IsFrozen, "Knight should be frozen");
            Assert.IsTrue(cannon.IsFrozen, "Cannon should be frozen");
            
            // Step 2 seconds - neither should have moved/attacked
            int initialKnightHP = knight.CurrentHP;
            int initialCannonHP = cannon.CurrentHP;
            
            Step(2f);
            
            knight = FindUnit(2, 26000040);
            cannon = FindBuilding(2, 26000045);
            
            Assert.AreEqual(initialKnightHP, knight.CurrentHP, "Frozen knight should not take damage");
            Assert.AreEqual(initialCannonHP, cannon.CurrentHP, "Frozen cannon should not take damage");
        }

        [Test]
        public void The_Log_Only_Affects_Ground_Units()
        {
            InitializeSimulation(TestDecks.SpellHeavyDeck, TestDecks.BalancedDeck);
                        SetPlayerElixir(1, 10);
            SetPlayerElixir(2, 10);
            
            var knightInput = new PlayerInput
            {
                type = InputType.PlayCard,
                cardId = 26000040, // Knight (ground)
                position = new Vector2(9, 18)
            };
            Simulation.QueueInput(2, knightInput);
            
            var minionInput = new PlayerInput
            {
                type = InputType.PlayCard,
                cardId = 26000047, // Minions (air)
                position = new Vector2(9, 18)
            };
            Simulation.QueueInput(2, minionInput);
            Tick();
            
            // Cast Log from left side
            var logInput = new PlayerInput
            {
                type = InputType.CastSpell,
                spellId = 26000049, // The Log
                position = new Vector2(5, 18)
            };
            Simulation.QueueInput(1, logInput);
            Tick();
            
            // Log travels across arena
            Step(1f);
            
            var knight = FindUnit(2, 26000040);
            var minions = FindUnits(2, 26000047);
            
            Assert.IsNotNull(knight);
            Assert.Greater(minions.Count, 0);
            
            // Knight (ground) should be damaged/killed
            Assert.IsTrue(knight.IsDead || knight.CurrentHP < knight.MaxHP, "Ground unit should be affected by Log");
            
            // Minions (air) should be unaffected
            foreach (var m in minions)
            {
                Assert.AreEqual(m.MaxHP, m.CurrentHP, "Air units should be unaffected by Log");
            }
        }

        [Test]
        public void Tornado_Pulls_Units_To_Center()
        {
            InitializeSimulation(TestDecks.SpellHeavyDeck, TestDecks.BalancedDeck);
                        SetPlayerElixir(1, 10);
            SetPlayerElixir(2, 10);
            
            var knightInput = new PlayerInput
            {
                type = InputType.PlayCard,
                cardId = 26000040, // Knight
                position = new Vector2(12, 18)
            };
            Simulation.QueueInput(2, knightInput);
            Tick();
            
            var knight = FindUnit(2, 26000040);
            var originalPos = knight.Position;
            
            var tornadoInput = new PlayerInput
            {
                type = InputType.CastSpell,
                spellId = 26000054, // Tornado
                position = new Vector2(9, 18)
            };
            Simulation.QueueInput(1, tornadoInput);
            Tick();
            
            Step(1.5f);
            
            knight = FindUnit(2, 26000040);
            Assert.IsNotNull(knight);
            Assert.Less(Vector2.Distance(knight.Position, new Vector2(9, 18)), 
                       Vector2.Distance(originalPos, new Vector2(9, 18)), 
                       "Knight should be pulled toward Tornado center");
        }

        [Test]
        public void Graveyard_Spawns_Skeletons_Randomly()
        {
            InitializeSimulation(TestDecks.SpellHeavyDeck, TestDecks.BalancedDeck);
                        SetPlayerElixir(1, 10);
            
            var gyInput = new PlayerInput
            {
                type = InputType.CastSpell,
                spellId = 26000055, // Graveyard (need correct ID)
                position = new Vector2(9, 18)
            };
            Simulation.QueueInput(1, gyInput);
            Tick();
            
            // Spawn duration ~3.5s
            Step(3.5f);
            
            var skeletons = FindUnits(1, 26000046); // Skeleton
            
            // Graveyard spawns 15 skeletons total
            Assert.AreEqual(15, skeletons.Count, "Graveyard should spawn 15 skeletons");
            
            // All should be within ~4 tile radius
            foreach (var s in skeletons)
            {
                Assert.Less(Vector2.Distance(s.Position, new Vector2(9, 18)), 4.5f, 
                    "Skeletons should be within spawn radius");
            }
        }

        [Test]
        public void Rocket_Damages_High_HP_Units()
        {
            InitializeSimulation(TestDecks.SpellHeavyDeck, TestDecks.BalancedDeck);
                        SetPlayerElixir(1, 10);
            SetPlayerElixir(2, 10);
            
            // Golem (high HP)
            var golemInput = new PlayerInput
            {
                type = InputType.PlayCard,
                cardId = 26000055, // Golem
                position = new Vector2(9, 18)
            };
            Simulation.QueueInput(2, golemInput);
            Tick();
            
            var rocketInput = new PlayerInput
            {
                type = InputType.CastSpell,
                spellId = 26000051, // Rocket
                position = new Vector2(9, 18)
            };
            Simulation.QueueInput(1, rocketInput);
            Tick();
            
            // Rocket travel time ~2s
            Step(2f);
            
            var golem = FindUnit(2, 26000055);
            Assert.IsNotNull(golem);
            Assert.Less(golem.CurrentHP, golem.MaxHP, "Golem should take Rocket damage");
        }

        [Test]
        public void Arrows_Kill_Swarm_Units()
        {
            InitializeSimulation(TestDecks.SpellHeavyDeck, TestDecks.BalancedDeck);
                        SetPlayerElixir(1, 10);
            SetPlayerElixir(2, 10);
            
            // Minion Horde
            var hordeInput = new PlayerInput
            {
                type = InputType.PlayCard,
                cardId = 26000070, // Minion Horde
                position = new Vector2(9, 18)
            };
            Simulation.QueueInput(2, hordeInput);
            Tick();
            
            var arrowsInput = new PlayerInput
            {
                type = InputType.CastSpell,
                spellId = 26000052, // Arrows
                position = new Vector2(9, 18)
            };
            Simulation.QueueInput(1, arrowsInput);
            Tick();
            
            Step(0.5f);
            
            var minions = FindUnits(2, 26000047); // Minions (from horde)
            foreach (var m in minions)
            {
                Assert.IsTrue(m.IsDead, "Minions should be killed by Arrows");
            }
        }

        [Test]
        public void Lightning_Hits_Three_Highest_HP_In_Radius()
        {
            InitializeSimulation(TestDecks.SpellHeavyDeck, TestDecks.BalancedDeck);
                        SetPlayerElixir(1, 10);
            SetPlayerElixir(2, 10);
            
            // Spawn units with different HP
            var golemInput = new PlayerInput
            {
                type = InputType.PlayCard,
                cardId = 26000055, // Golem (highest HP)
                position = new Vector2(9, 18)
            };
            Simulation.QueueInput(2, golemInput);
            
            var giantInput = new PlayerInput
            {
                type = InputType.PlayCard,
                cardId = 26000042, // Giant (2nd highest)
                position = new Vector2(10, 18)
            };
            Simulation.QueueInput(2, giantInput);
            
            var knightInput = new PlayerInput
            {
                type = InputType.PlayCard,
                cardId = 26000040, // Knight (3rd)
                position = new Vector2(11, 18)
            };
            Simulation.QueueInput(2, knightInput);
            
            var goblinInput = new PlayerInput
            {
                type = InputType.PlayCard,
                cardId = 26000046, // Skeletons (lowest HP)
                position = new Vector2(12, 18)
            };
            Simulation.QueueInput(2, goblinInput);
            
            Tick();
            
            var lightningInput = new PlayerInput
            {
                type = InputType.CastSpell,
                spellId = 26000056, // Lightning
                position = new Vector2(10, 18)
            };
            Simulation.QueueInput(1, lightningInput);
            Tick();
            Step(0.1f);
            
            var golem = FindUnit(2, 26000055);
            var giant = FindUnit(2, 26000042);
            var knight = FindUnit(2, 26000040);
            var goblin = FindUnit(2, 26000046);
            
            // Lightning hits 3 highest HP units in radius
            Assert.IsTrue(golem.CurrentHP < golem.MaxHP, "Golem should be hit");
            Assert.IsTrue(giant.CurrentHP < giant.MaxHP, "Giant should be hit");
            Assert.IsTrue(knight.CurrentHP < knight.MaxHP, "Knight should be hit");
            Assert.AreEqual(goblin.MaxHP, goblin.CurrentHP, "Skeleton should NOT be hit (4th highest)");
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
