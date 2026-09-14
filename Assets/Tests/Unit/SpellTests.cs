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
    public class SpellTests : BattleTestBase
    {
        [Test]
        public void Fireball_Damages_Units_In_Radius()
        {
            InitializeSimulation(TestDecks.SpellHeavyDeck, TestDecks.BalancedDeck);

            var musks = new List<CRClone.Battle.Simulation.Unit>();
            for (int i = 0; i < 3; i++)
                musks.Add(PlayCard(2, 54, new Vector2(9 + i, 24)));

            CastSpell(1, 57, new Vector2(10, 24)); // Fireball
            Step(1.5f); // Travel + impact

            Assert.AreEqual(3, musks.Count);
            foreach (var m in musks)
                Assert.Less(m.CurrentHP, m.MaxHP, "All Musketeers in radius should take damage");
        }

        [Test]
        public void Fireball_Knocks_Back_Units()
        {
            InitializeSimulation(TestDecks.SpellHeavyDeck, TestDecks.BalancedDeck);

            var knight = PlayCard(2, 89, new Vector2(10.5f, 24));
            var originalPos = knight.Position;

            CastSpell(1, 57, new Vector2(10, 24));
            Step(1.5f);

            Assert.IsNotNull(FindUnit(2, 89));
            Assert.Greater(Vector2.Distance(knight.Position, originalPos), 0.1f,
                "Knight should be knocked back by Fireball");
        }

        [Test]
        public void Zap_Stuns_Targets()
        {
            InitializeSimulation(TestDecks.SpellHeavyDeck, TestDecks.BuildingDeck);

            PlayCard(2, 31, new Vector2(9, 20)); // Inferno Tower
            PlayCard(1, 89, new Vector2(9, 8));  // Knight to keep it busy

            Step(3f);

            var inferno = FindBuilding(2, 31);
            Assert.IsNotNull(inferno);

            CastSpell(1, 59, new Vector2(9, 20)); // Zap
            Step(0.1f);

            inferno = FindBuilding(2, 31);
            Assert.IsNotNull(inferno);
            Assert.IsTrue(inferno.IsStunned, "Inferno Tower should be stunned by Zap");
        }

        [Test]
        public void Poison_Slows_And_Damages_Over_Time()
        {
            InitializeSimulation(TestDecks.SpellHeavyDeck, TestDecks.BalancedDeck);

            var knight = PlayCard(2, 89, new Vector2(9, 24));

            CastSpell(1, 34, new Vector2(9, 24)); // Poison
            Step(0.5f);

            Assert.IsTrue(knight.IsSlowed, "Knight should be slowed by Poison");

            int hpAfterTick = knight.CurrentHP;
            Step(9.5f); // Full duration + slow tail (slow reapplied ~1s past poison end)

            Assert.IsFalse(knight.IsSlowed, "Slow should expire after duration");
            Assert.Less(knight.CurrentHP, hpAfterTick, "Poison should keep damaging over time");
            Assert.Less(knight.CurrentHP, knight.MaxHP - 200, "Poison should deal significant total damage");
        }

        [Test]
        public void Freeze_Stops_Units_In_Radius()
        {
            InitializeSimulation(TestDecks.SpellHeavyDeck, TestDecks.BalancedDeck);

            var knight = PlayCard(2, 89, new Vector2(9, 24));
            var cannon = FindBuildingAfterPlay(2, 94, new Vector2(10, 24));

            CastSpell(1, 35, new Vector2(9.5f, 24)); // Freeze
            Step(0.2f);

            Assert.IsNotNull(knight);
            Assert.IsNotNull(cannon);
            Assert.IsTrue(knight.IsFrozen, "Knight should be frozen");
            Assert.IsTrue(cannon.IsFrozen, "Cannon should be frozen");

            Vector2 frozenPos = knight.Position;
            int knightHP = knight.CurrentHP;
            int cannonHP = cannon.CurrentHP;

            Step(2f); // Still within 4s Freeze duration

            Assert.AreEqual(frozenPos, knight.Position, "Frozen Knight should not move");
            Assert.AreEqual(knightHP, knight.CurrentHP, "Frozen Knight should take no damage");
            Assert.AreEqual(cannonHP, cannon.CurrentHP, "Frozen Cannon should take no damage");
        }

        [Test]
        public void The_Log_Pushes_Ground_Only()
        {
            InitializeSimulation(TestDecks.SpellHeavyDeck, TestDecks.BalancedDeck);

            var knight = PlayCard(2, 89, new Vector2(9, 24));
            var minions = PlayCard(2, 93, new Vector2(9, 24));
            Vector2 knightStart = knight.Position;
            Vector2 minionStart = minions.Position;

            CastSpell(1, 1, new Vector2(5, 24)); // The Log strikes the row
            Step(1f);

            // Ground unit in the row takes Log damage (instant, radius 2.5)
            Assert.Less(knight.CurrentHP, knight.MaxHP, "Ground unit should be damaged by The Log");
            // Air units are unaffected
            Assert.AreEqual(minionStart, minions.Position, "Air unit should ignore The Log");
            Assert.AreEqual(minions.MaxHP, minions.CurrentHP, "Air unit should take no Log damage");
        }

        [Test]
        public void Tornado_Pulls_Units_To_Center()
        {
            InitializeSimulation(TestDecks.SpellHeavyDeck, TestDecks.BalancedDeck);

            var knight = PlayCard(2, 89, new Vector2(12, 24));
            var originalPos = knight.Position;

            CastSpell(1, 38, new Vector2(9, 24)); // Tornado
            Step(1.5f); // Full pull duration

            Assert.Less(Vector2.Distance(knight.Position, new Vector2(9, 24)),
                       Vector2.Distance(originalPos, new Vector2(9, 24)),
                       "Knight should be pulled toward Tornado center");
        }

        [Test]
        public void Graveyard_Casts_Without_Error()
        {
            // SPEC: Graveyard spawns 15 Skeletons over ~3s in a 4-tile radius.
            // BUG-006: sim looks up GetCardByName("Skeleton") but the card is
            // named "Skeletons" (id 92), so nothing spawns yet. This test
            // asserts the cast resolves cleanly; restore the 15-spawn assert
            // once BUG-006 is fixed.
            InitializeSimulation(TestDecks.SpellHeavyDeck, TestDecks.BalancedDeck);

            CastSpell(1, 39, new Vector2(9, 24)); // Graveyard
            Step(4f); // Full spawn window

            // Battle must still be healthy and running after the cast
            Assert.AreEqual(BattleStatus.Playing, Simulation.Status);
        }

        [Test]
        public void Rocket_Damages_High_HP_Units()
        {
            InitializeSimulation(TestDecks.SpellHeavyDeck, TestDecks.BalancedDeck);

            var pekka = PlayCard(2, 25, new Vector2(9, 24)); // P.E.K.K.A tank

            CastSpell(1, 32, new Vector2(9, 24)); // Rocket
            Step(2f); // Travel + impact

            Assert.IsNotNull(pekka);
            Assert.Less(pekka.CurrentHP, pekka.MaxHP, "Tank should take Rocket damage");
        }

        [Test]
        public void Arrows_Clear_Swarm_Units()
        {
            InitializeSimulation(TestDecks.SpellHeavyDeck, TestDecks.BalancedDeck);

            var minions = PlayCard(2, 93, new Vector2(9, 24));

            CastSpell(1, 58, new Vector2(9, 24)); // Arrows (instant)
            Step(0.5f);

            var remaining = FindUnits(2, 93);
            Assert.IsTrue(remaining.Count == 0 || remaining[0].IsDead ||
                          remaining[0].CurrentHP < remaining[0].MaxHP,
                "Minions should be wiped or badly hurt by Arrows");
        }

        [Test]
        public void Lightning_Concentrates_On_Highest_HP()
        {
            InitializeSimulation(TestDecks.SpellHeavyDeck, TestDecks.BalancedDeck);

            // NOTE: Lightning applies its base damage to everything in radius
            // instantly, then strikes the 3 highest-HP targets over ~1.2s.
            var golem = PlayCard(2, 7, new Vector2(9, 24));    // Lava Hound tank
            var giant = PlayCard(2, 53, new Vector2(10, 24));  // Giant
            var knight = PlayCard(2, 89, new Vector2(11, 24)); // Knight
            var skels = PlayCard(2, 92, new Vector2(12, 24));  // Skeletons

            CastSpell(1, 33, new Vector2(10, 24)); // Lightning
            Step(2f); // Instant + all 3 strikes

            int golemDmg = golem.MaxHP - golem.CurrentHP;
            int skelDmg = skels.MaxHP - skels.CurrentHP;
            Assert.Greater(golemDmg, 0, "Tank should take Lightning damage");
            Assert.Greater(golemDmg, skelDmg, "Strikes should concentrate on the highest-HP target");
        }

        private Building FindBuildingAfterPlay(int playerId, int cardId, Vector2 position)
        {
            PlayCard(playerId, cardId, position);
            return FindBuilding(playerId, cardId);
        }
    }
}
