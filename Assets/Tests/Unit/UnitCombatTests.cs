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
    public class UnitCombatTests : BattleTestBase
    {
        [Test]
        public void Knight_Defeats_Single_Skeleton()
        {
            InitializeSimulation(TestDecks.BalancedDeck, TestDecks.BalancedDeck);

            // Knight L11 (~3940hp/~415dmg) vs Skeleton L11 (~174hp): one-shot
            var knight = PlayCard(1, 89, new Vector2(9, 8));
            var skeleton = PlayCard(2, 92, new Vector2(9, 20));
            Assert.IsNotNull(knight);
            Assert.IsNotNull(skeleton);

            StepUntil(() => knight.IsDead || skeleton.IsDead, maxSeconds: 15f);

            Assert.IsTrue(skeleton.IsDead, "Skeleton should die to Knight");
            Assert.IsFalse(knight.IsDead, "Knight should survive a single Skeleton");
            Assert.Greater(knight.CurrentHP, knight.MaxHP * 0.5f);
        }

        [Test]
        public void Five_Goblins_Defeat_Knight()
        {
            InitializeSimulation(TestDecks.BalancedDeck, TestDecks.BalancedDeck);

            var knight = PlayCard(1, 89, new Vector2(9, 8));
            var goblins = new List<Unit>();
            for (int i = 0; i < 5; i++)
                goblins.Add(PlayCard(2, 91, new Vector2(8 + i * 0.5f, 20)));
            Assert.IsNotNull(knight);
            Assert.AreEqual(5, goblins.Count);

            StepUntil(() => knight.IsDead || goblins.TrueForAll(g => g == null || g.IsDead), maxSeconds: 20f);

            Assert.IsTrue(knight.IsDead, "Knight should die to 5 Goblins");
        }

        [Test]
        public void Ranged_Unit_Attacks_From_Distance()
        {
            InitializeSimulation(TestDecks.BalancedDeck, TestDecks.BalancedDeck);

            // Musketeer range 6 vs Knight melee; deploy 14 apart
            var musketeer = PlayCard(1, 54, new Vector2(9, 5));
            var knight = PlayCard(2, 89, new Vector2(9, 20));
            Assert.IsNotNull(musketeer);
            Assert.IsNotNull(knight);

            // After a single tick they are still out of range and unharmed
            float dist = Vector2.Distance(musketeer.Position, knight.Position);
            Assert.Greater(dist, 6f, "Should start out of Musketeer range");
            Assert.AreEqual(musketeer.MaxHP, musketeer.CurrentHP);
            Assert.AreEqual(knight.MaxHP, knight.CurrentHP);

            Step(3f); // Knight closes in; Musketeer opens fire

            Assert.Less(knight.CurrentHP, knight.MaxHP, "Knight should take Musketeer damage");
        }

        [Test]
        public void Splash_Damage_Hits_Multiple_Units()
        {
            InitializeSimulation(TestDecks.BalancedDeck, TestDecks.BalancedDeck);

            var wizard = PlayCard(1, 21, new Vector2(9, 5));
            var skeletons = new List<Unit>();
            for (int i = 0; i < 3; i++)
                skeletons.Add(PlayCard(2, 92, new Vector2(9 + i * 0.5f, 20)));
            Assert.IsNotNull(wizard);
            Assert.AreEqual(3, skeletons.Count);

            Step(4f); // Wizard engages; splash should tag all three

            int damaged = 0;
            foreach (var s in skeletons)
            {
                if (s.IsDead || s.CurrentHP < s.MaxHP) damaged++;
            }
            Assert.AreEqual(3, damaged, "All 3 Skeletons should take splash damage");
        }

        [Test]
        public void Melee_Unit_Moves_Toward_Distant_Target()
        {
            InitializeSimulation(TestDecks.BalancedDeck, TestDecks.BalancedDeck);

            var knight = PlayCard(1, 89, new Vector2(9, 5));
            var archers = PlayCard(2, 90, new Vector2(9, 25));
            Assert.IsNotNull(knight);
            Assert.IsNotNull(archers);

            Step(1f);

            Assert.AreNotEqual(UnitState.Idle, knight.State, "Knight should be moving or attacking");
        }

        [Test]
        public void Cannon_Ignores_Air_Units()
        {
            InitializeSimulation(TestDecks.BuildingDeck, TestDecks.BalancedDeck);

            var cannon = FindBuildingAfterPlay(1, 94, new Vector2(9, 10));
            var knight = PlayCard(2, 89, new Vector2(9, 20));
            var minions = PlayCard(2, 93, new Vector2(10, 20));
            Assert.IsNotNull(cannon);
            Assert.IsNotNull(knight);
            Assert.IsNotNull(minions);

            Step(3f);

            Assert.AreEqual(knight, cannon.Target, "Cannon should target the ground unit");
            Assert.AreNotEqual(minions, cannon.Target, "Cannon should not target air units");
            Assert.AreEqual(minions.MaxHP, minions.CurrentHP, "Minions should be unharmed by Cannon");
        }

        [Test]
        public void Unit_Retargets_When_Target_Dies()
        {
            InitializeSimulation(TestDecks.BalancedDeck, TestDecks.BalancedDeck);

            var wizard = PlayCard(1, 21, new Vector2(9, 5));
            var skel1 = PlayCard(2, 92, new Vector2(9, 20));
            var skel2 = PlayCard(2, 92, new Vector2(10, 20));
            Assert.IsNotNull(wizard);
            Assert.IsNotNull(skel1);
            Assert.IsNotNull(skel2);

            // Wizard one-shots Skeletons; wait until exactly one remains
            StepUntil(() => FindUnits(2, 92).Count < 2, maxSeconds: 10f);

            var remaining = FindUnits(2, 92);
            Assert.AreEqual(1, remaining.Count);
            Assert.AreEqual(remaining[0], wizard.Target, "Wizard should retarget to the remaining Skeleton");
        }

        private Building FindBuildingAfterPlay(int playerId, int cardId, Vector2 position)
        {
            PlayCard(playerId, cardId, position);
            return FindBuilding(playerId, cardId);
        }
    }
}
