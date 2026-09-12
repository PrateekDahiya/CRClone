using System.Collections.Generic;
using NUnit.Framework;
using CRClone.Battle.Simulation;
using CRClone.Core;
using CRClone.Tests.TestFixtures;
using CRClone.Network;

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
            
            // Play first card in hand
            SetPlayerElixir(1, 10);
            var input = new PlayerInput
            {
                type = InputType.PlayCard,
                cardId = initialHand[0],
                position = new Vector2(9, 8)
            };
            Simulation.QueueInput(1, input);
            Tick();
            
            // Hand should have shifted, new card at position 3
            Assert.AreEqual(initialHand[1], Simulation.Player1.Hand[0]);
            Assert.AreEqual(initialHand[2], Simulation.Player1.Hand[1]);
            Assert.AreEqual(initialHand[3], Simulation.Player1.Hand[2]);
            Assert.AreEqual(fifthCard, Simulation.Player1.Hand[3]);
        }

        [Test]
        public void Cycle_Wraps_Around_After_8_Cards()
        {
            InitializeSimulation(TestDecks.BalancedDeck, TestDecks.BalancedDeck);
                        SetPlayerElixir(1, 10);
            
            // Play all 8 cards
            for (int i = 0; i < 8; i++)
            {
                var cardToPlay = Simulation.Player1.Hand[0];
                var input = new PlayerInput
                {
                    type = InputType.PlayCard,
                    cardId = cardToPlay,
                    position = new Vector2(9, 8)
                };
                Simulation.QueueInput(1, input);
                Tick();
                
                // Small step to process
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
                        SetPlayerElixir(1, 10);
            
            // Play 12 cards (1.5 cycles)
            for (int i = 0; i < 12; i++)
            {
                var cardToPlay = Simulation.Player1.Hand[0];
                var input = new PlayerInput
                {
                    type = InputType.PlayCard,
                    cardId = cardToPlay,
                    position = new Vector2(9, 8)
                };
                Simulation.QueueInput(1, input);
                Tick();
                Step(0.1f);
            }
            
            // Should be on 3rd cycle, cards 4,5,6,7
            Assert.AreEqual(TestDecks.BalancedDeck[4], Simulation.Player1.Hand[0]);
            Assert.AreEqual(TestDecks.BalancedDeck[5], Simulation.Player1.Hand[1]);
            Assert.AreEqual(TestDecks.BalancedDeck[6], Simulation.Player1.Hand[2]);
            Assert.AreEqual(TestDecks.BalancedDeck[7], Simulation.Player1.Hand[3]);
        }

        [Test]
        public void Opponent_Cycle_Independent()
        {
            InitializeSimulation(TestDecks.BalancedDeck, TestDecks.SpellHeavyDeck);
                        SetPlayerElixir(1, 10);
            SetPlayerElixir(2, 10);
            
            // Player 1 plays 2 cards
            for (int i = 0; i < 2; i++)
            {
                var input = new PlayerInput
                {
                    type = InputType.PlayCard,
                    cardId = Simulation.Player1.Hand[0],
                    position = new Vector2(9, 8)
                };
                Simulation.QueueInput(1, input);
                Tick();
            }
            
            // Player 2 plays 1 card
            var input2 = new PlayerInput
            {
                type = InputType.PlayCard,
                cardId = Simulation.Player2.Hand[0],
                position = new Vector2(9, 20)
            };
            Simulation.QueueInput(2, input2);
            Tick();
            
            // Player 1 hand advanced by 2, Player 2 by 1
            Assert.AreEqual(TestDecks.BalancedDeck[2], Simulation.Player1.Hand[0]);
            Assert.AreEqual(TestDecks.SpellHeavyDeck[1], Simulation.Player2.Hand[0]);
        }

        [Test]
        public void Spell_Cards_Also_Follow_Cycle()
        {
            InitializeSimulation(TestDecks.SpellHeavyDeck, TestDecks.BalancedDeck);
                        SetPlayerElixir(1, 10);
            
            // Play Fireball (first in spell heavy deck)
            var input = new PlayerInput
            {
                type = InputType.PlayCard,
                cardId = 26000044, // Fireball
                position = new Vector2(9, 20)
            };
            Simulation.QueueInput(1, input);
            Tick();
            
            // Next card in cycle should be Zap (2nd in deck)
            Assert.AreEqual(26000048, Simulation.Player1.Hand[3]); // Zap
        }

        [Test]
        public void Building_Cards_Also_Follow_Cycle()
        {
            InitializeSimulation(TestDecks.BuildingDeck, TestDecks.BalancedDeck);
                        SetPlayerElixir(1, 10);
            
            // Play Cannon (first in building deck)
            var input = new PlayerInput
            {
                type = InputType.PlayCard,
                cardId = 26000045, // Cannon
                position = new Vector2(9, 10)
            };
            Simulation.QueueInput(1, input);
            Tick();
            
            // Next card should be Tesla
            Assert.AreEqual(26000062, Simulation.Player1.Hand[3]); // Tesla
        }
    }
}
