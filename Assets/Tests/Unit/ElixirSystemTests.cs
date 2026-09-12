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
    public class ElixirSystemTests : BattleTestBase
    {
        [Test]
        public void Elixir_Generates_At_Correct_Rate_Normal()
        {
            InitializeSimulation(TestDecks.BalancedDeck, TestDecks.BalancedDeck);
                        // 1 elixir per 2.8 seconds at normal rate
            // Starting with 5 elixir, after 2.8 seconds should have 6
            Step(2.8f);
            
            AssertElixir(1, 6);
            AssertElixir(2, 6);
        }

        [Test]
        public void Elixir_Caps_At_Max()
        {
            InitializeSimulation(TestDecks.BalancedDeck, TestDecks.BalancedDeck);
                        SetPlayerElixir(1, 10);
            SetPlayerElixir(2, 10);
            
            // Wait 10 seconds - should stay at 10
            Step(10f);
            
            AssertElixir(1, 10);
            AssertElixir(2, 10);
        }

        [Test]
        public void Double_Elixir_Generates_Twice_As_Fast()
        {
            InitializeSimulation(TestDecks.BalancedDeck, TestDecks.BalancedDeck);
                        // Fast forward to double elixir period (last 60 seconds of 3 min battle)
            Step(120f); // 2 minutes
            
            SetPlayerElixir(1, 5);
            SetPlayerElixir(2, 5);
            
            // At double elixir rate (1.4s per elixir), 1.4s should give +1 elixir
            Step(1.4f);
            
            AssertElixir(1, 6);
            AssertElixir(2, 6);
        }

        [Test]
        public void Triple_Elixir_Generates_Three_Times_As_Fast()
        {
            InitializeSimulation(TestDecks.BalancedDeck, TestDecks.BalancedDeck);
                        // Fast forward to overtime (triple elixir)
            Step(360f); // 3 minutes + overtime start
            
            SetPlayerElixir(1, 5);
            SetPlayerElixir(2, 5);
            
            // At triple elixir rate (0.93s per elixir), ~1s should give +1 elixir
            Step(1f);
            
            AssertElixir(1, 6);
            AssertElixir(2, 6);
        }

        [Test]
        public void Elixir_Spent_On_Card_Play()
        {
            InitializeSimulation(TestDecks.BalancedDeck, TestDecks.BalancedDeck);
                        SetPlayerElixir(1, 5);
            
            // Play Knight (3 elixir)
            var input = new PlayerInput
            {
                type = InputType.PlayCard,
                cardId = 26000040, // Knight
                position = new Vector2(9, 8)
            };
            Simulation.QueueInput(1, input);
            Tick();
            
            AssertElixir(1, 2); // 5 - 3 = 2
        }

        [Test]
        public void Cannot_Play_Card_Without_Elixir()
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
            
            // Elixir should remain unchanged
            AssertElixir(1, 2);
        }

        [Test]
        public void Elixir_Not_Spent_On_Invalid_Position()
        {
            InitializeSimulation(TestDecks.BalancedDeck, TestDecks.BalancedDeck);
                        SetPlayerElixir(1, 10);
            
            // Try to place building across river (invalid)
            var input = new PlayerInput
            {
                type = InputType.PlayCard,
                cardId = 26000045, // Cannon
                position = new Vector2(9, 20) // Enemy side
            };
            Simulation.QueueInput(1, input);
            Tick();
            
            // Elixir should remain unchanged
            AssertElixir(1, 10);
        }

        [Test]
        public void Elixir_Collector_Produces_Elixir_Over_Time()
        {
            InitializeSimulation(TestDecks.BalancedDeck, TestDecks.BalancedDeck);
                        SetPlayerElixir(1, 10);
            
            // Place Elixir Collector
            var input = new PlayerInput
            {
                type = InputType.PlayCard,
                cardId = 26000067, // Elixir Collector
                position = new Vector2(9, 10)
            };
            Simulation.QueueInput(1, input);
            Tick();
            
            // Collector costs 6, so we have 4 elixir
            AssertElixir(1, 4);
            
            // Wait for collector to produce (should produce 1 elixir every ~9.8s)
            // Actually the collector produces 1 elixir every ~9.8 seconds, total 2 elixir over lifetime
            Step(10f);
            
            // Should have gained 1 elixir from collector
            AssertElixir(1, 5, 1); // Allow some tolerance
        }
    }
}
