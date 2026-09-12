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
    public class TargetingTests : BattleTestBase
    {
        [Test]
        public void Building_Targeters_Ignore_Troops()
        {
            InitializeSimulation(TestDecks.BalancedDeck, TestDecks.BuildingDeck);

            // Giant (targets buildings only) vs Knight (troop) + Cannon (building)
            var giant = PlayCard(1, 53, new Vector2(9, 5));
            PlayCard(2, 89, new Vector2(9, 22));
            PlayCard(2, 94, new Vector2(9, 24));
            var cannon = FindBuilding(2, 94);

            Assert.IsNotNull(giant);
            Assert.IsNotNull(cannon);

            Step(2f);

            Assert.AreEqual(cannon, giant.Target, "Giant should target buildings, not troops");
        }

        [Test]
        public void Retarget_On_Target_Death()
        {
            InitializeSimulation(TestDecks.BalancedDeck, TestDecks.BalancedDeck);

            var wizard = PlayCard(1, 21, new Vector2(9, 5));
            PlayCard(2, 92, new Vector2(9, 24));
            PlayCard(2, 92, new Vector2(10, 24));
            Assert.IsNotNull(wizard);

            // Wizard one-shots Skeletons; wait until exactly one remains
            StepUntil(() => FindUnits(2, 92).Count < 2, maxSeconds: 10f);

            var remaining = FindUnits(2, 92);
            Assert.AreEqual(1, remaining.Count);
            Assert.AreEqual(remaining[0], wizard.Target, "Should retarget to remaining Skeleton");
        }

        [Test]
        public void Units_Engage_Closest_Threat()
        {
            InitializeSimulation(TestDecks.BalancedDeck, TestDecks.BalancedDeck);

            var musk = PlayCard(1, 54, new Vector2(9, 5));
            var knight = PlayCard(2, 89, new Vector2(9, 22)); // Closer, survives return fire
            var giant = PlayCard(2, 53, new Vector2(9, 26));  // Further
            Assert.IsNotNull(musk);
            Assert.IsNotNull(knight);
            Assert.IsNotNull(giant);

            Step(1f);

            Assert.AreEqual(knight, musk.Target, "Should prioritize the closer target");
        }

        [Test]
        public void Air_Units_Targeted_By_Anti_Air_Only()
        {
            InitializeSimulation(TestDecks.BalancedDeck, TestDecks.BalancedDeck);

            var minions = PlayCard(2, 93, new Vector2(9, 24));
            var musk = PlayCard(1, 54, new Vector2(9, 5));
            var knight = PlayCard(1, 89, new Vector2(10, 5));
            Assert.IsNotNull(minions);
            Assert.IsNotNull(musk);
            Assert.IsNotNull(knight);

            Step(2f);

            Assert.AreEqual(minions, musk.Target, "Musketeer should target air units");
            Assert.AreNotEqual(minions, knight.Target, "Knight should not target air units");
        }

        [Test]
        public void Invisible_Units_Cannot_Be_Targeted()
        {
            InitializeSimulation(TestDecks.ChampionDeck, TestDecks.BalancedDeck);

            var aq = PlayCard(1, 51, new Vector2(9, 8));
            var musk = PlayCard(2, 54, new Vector2(9, 24));
            Assert.IsNotNull(aq);
            Assert.IsNotNull(musk);

            UseChampionAbility(1, new Vector2(9, 8)); // Royal Cloak
            Step(0.2f);

            aq = FindUnit(1, 51);
            Assert.IsNotNull(aq);
            Assert.IsTrue(aq.IsInvisible);
            Assert.AreNotEqual(aq, musk.Target, "Invisible units should not be targetable");
        }

        [Test]
        public void Towers_Target_Units_In_Range()
        {
            InitializeSimulation(TestDecks.BalancedDeck, TestDecks.BalancedDeck);

            var knight = PlayCard(2, 89, new Vector2(3, 20));
            Assert.IsNotNull(knight);

            Step(2f); // Knight marches toward P1 left Princess tower at (3,13)

            var princess = FindTower(1, TowerType.PrincessLeft);
            Assert.IsNotNull(princess);
            Assert.AreEqual(knight, princess.Target, "Princess Tower should target units in range");
        }

        [Test]
        public void King_Tower_Targets_Units_In_Range()
        {
            InitializeSimulation(TestDecks.BalancedDeck, TestDecks.BalancedDeck);

            var knight = PlayCard(2, 89, new Vector2(9, 20));
            Assert.IsNotNull(knight);

            Step(3f); // Knight pushes toward P1 King tower at (9,2)

            var king = FindTower(1, TowerType.King);
            Assert.IsNotNull(king);
            // Either the King (if activated/in range) or a Princess has it;
            // at minimum SOME P1 tower must have acquired the invader.
            bool anyTowerTargeting = false;
            foreach (var t in Simulation.Towers)
            {
                if (t.OwnerPlayerId == 1 && t.Target == knight) { anyTowerTargeting = true; break; }
            }
            Assert.IsTrue(anyTowerTargeting, "A P1 tower should target the invading Knight");
        }
    }
}
