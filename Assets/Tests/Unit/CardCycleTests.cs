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
    public class CardCycleTests : BattleTestBase
    {
        [Test]
        public void Initial_Hand_Has_4_Cards()
        {
            InitializeSimulation(TestDecks.BalancedDeck, TestDecks.BalancedDeck);

            Assert.AreEqual(4, Simulation.Player1.Hand.Length);
            Assert.AreEqual(4, Simulation.Player2.Hand.Length);
        }

        [Test]
        public void Initial_Hand_Contains_First_4_Cards_From_Deck()
        {
            InitializeSimulation(TestDecks.BalancedDeck, TestDecks.BalancedDeck);

            for (int i = 0; i < 4; i++)
            {
                Assert.AreEqual(TestDecks.BalancedDeck[i], Simulation.Player1.Hand[i]);
                Assert.AreEqual(TestDecks.BalancedDeck[i], Simulation.Player2.Hand[i]);
            }
        }

        [Test]
        public void Playing_Card_Draws_Next_In_Cycle()
        {
            InitializeSimulation(TestDecks.BalancedDeck, TestDecks.BalancedDeck);

            var initialHand = new int[4];
            System.Array.Copy(Simulation.Player1.Hand, initialHand, 4);
            var fifthCard = TestDecks.BalancedDeck[4]; // 5th card in deck

            PlayCard(1, initialHand[0], new Vector2(9, 8));

            Assert.AreEqual(initialHand[1], Simulation.Player1.Hand[0]);
            Assert.AreEqual(initialHand[2], Simulation.Player1.Hand[1]);
            Assert.AreEqual(initialHand[3], Simulation.Player1.Hand[2]);
            Assert.AreEqual(fifthCard, Simulation.Player1.Hand[3]);
        }

        [Test]
        public void Cycle_Wraps_Around_After_8_Cards()
        {
            InitializeSimulation(TestDecks.BalancedDeck, TestDecks.BalancedDeck);

            // Play all 8 cards (helper tops up elixir each play)
            for (int i = 0; i < 8; i++)
            {
                PlayCard(1, Simulation.Player1.Hand[0], new Vector2(9, 8));
                Step(0.1f);
            }

            // Hand should contain first 4 cards again (cycle wrapped)
            for (int i = 0; i < 4; i++)
            {
                Assert.AreEqual(TestDecks.BalancedDeck[i], Simulation.Player1.Hand[i]);
            }
        }

        [Test]
        public void Cycle_Continues_After_Wrap()
        {
            InitializeSimulation(TestDecks.BalancedDeck, TestDecks.BalancedDeck);

            // Play 12 cards (1.5 cycles)
            for (int i = 0; i < 12; i++)
            {
                PlayCard(1, Simulation.Player1.Hand[0], new Vector2(9, 8));
                Step(0.1f);
            }

            // Should be into the 2nd cycle: cards 4,5,6,7
            Assert.AreEqual(TestDecks.BalancedDeck[4], Simulation.Player1.Hand[0]);
            Assert.AreEqual(TestDecks.BalancedDeck[5], Simulation.Player1.Hand[1]);
            Assert.AreEqual(TestDecks.BalancedDeck[6], Simulation.Player1.Hand[2]);
            Assert.AreEqual(TestDecks.BalancedDeck[7], Simulation.Player1.Hand[3]);
        }

        [Test]
        public void Opponent_Cycle_Independent()
        {
            InitializeSimulation(TestDecks.BalancedDeck, TestDecks.SpellHeavyDeck);

            // Player 1 plays 2 cards (valid P1 zone)
            PlayCard(1, Simulation.Player1.Hand[0], new Vector2(9, 8));
            PlayCard(1, Simulation.Player1.Hand[0], new Vector2(9, 8));

            // Player 2 plays 1 card (valid P2 zone)
            PlayCard(2, Simulation.Player2.Hand[0], new Vector2(9, 24));

            // Player 1 hand advanced by 2, Player 2 by 1
            Assert.AreEqual(TestDecks.BalancedDeck[2], Simulation.Player1.Hand[0]);
            Assert.AreEqual(TestDecks.SpellHeavyDeck[1], Simulation.Player2.Hand[0]);
        }

        [Test]
        public void Spell_Cards_Also_Follow_Cycle()
        {
            InitializeSimulation(TestDecks.SpellHeavyDeck, TestDecks.BalancedDeck);

            // Play Fireball (first in spell heavy deck); spells deploy anywhere
            CastSpell(1, 57, new Vector2(9, 24));

            // Next card in cycle should be Zap (2nd in deck)
            Assert.AreEqual(59, Simulation.Player1.Hand[3]); // Zap
        }

        [Test]
        public void Building_Cards_Also_Follow_Cycle()
        {
            InitializeSimulation(TestDecks.BuildingDeck, TestDecks.BalancedDeck);

            // Play Cannon (first in building deck)
            PlayCard(1, 94, new Vector2(9, 10));

            // Next card should be Tesla
            Assert.AreEqual(95, Simulation.Player1.Hand[3]); // Tesla
        }
    }
}
