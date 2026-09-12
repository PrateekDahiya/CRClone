using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using CRClone.Battle.Simulation;
using CRClone.Core;
using CRClone.Tests.TestFixtures;
using CRClone.Network;

namespace CRClone.Tests.Performance
{
    [TestFixture]
    public class MemoryStabilityTest : BattleTestBase
    {
        [Test]
        public void Memory_Stable_Over_Long_Battle()
        {
            InitializeSimulation(TestDecks.BalancedDeck, TestDecks.BalancedDeck);
                        // Force GC and get initial memory
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            long initialMemory = GC.GetTotalMemory(true);
            
            // Simulate 5 minute battle with heavy activity
            // 18000 ticks at 60Hz = 300 seconds = 5 minutes
            for (int tick = 0; tick < 18000; tick++)
            {
                if (tick % 60 == 0) // Every second
                {
                    // Spawn units and cast spells periodically
                    if (Simulation.Player1.Elixir >= 3)
                    {
                        var card = Simulation.Player1.Hand[0];
                        PlayCard(1, card, RandomPosition(1));
                    }
                    if (Simulation.Player2.Elixir >= 3)
                    {
                        var card = Simulation.Player2.Hand[0];
                        PlayCard(2, card, RandomPosition(2));
                    }
                    
                    // Cast spells occasionally
                    if (tick % 300 == 0 && Simulation.Player1.Elixir >= 4)
                    {
                        Simulation.QueueInput(1, new PlayerInput
                        {
                            type = InputType.CastSpell,
                            spellId = 26000044, // Fireball
                            position = RandomPosition(2)
                        });
                    }
                }
                
                Simulation.Tick(BattleSimulation.FIXED_DT);
            }
            
            // Force GC and measure final memory
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            long finalMemory = GC.GetTotalMemory(true);
            
            long growth = finalMemory - initialMemory;
            long growthMB = growth / (1024 * 1024);
            
            UnityEngine.Debug.Log($"[MemoryTest] Initial: {initialMemory / (1024*1024)}MB, Final: {finalMemory / (1024*1024)}MB, Growth: {growthMB}MB");
            
            // Should not grow more than 50MB
            Assert.Less(growth, 50 * 1024 * 1024, 
                $"Memory grew by {growthMB}MB over 5 minute battle (limit: 50MB)");
        }

        [Test]
        public void No_Memory_Leak_Per_Frame()
        {
            InitializeSimulation(TestDecks.BalancedDeck, TestDecks.BalancedDeck);
                        // Warm up
            for (int i = 0; i < 100; i++)
            {
                Simulation.Tick(BattleSimulation.FIXED_DT);
            }
            
            GC.Collect();
            long baselineMemory = GC.GetTotalMemory(true);
            
            // Run 1000 frames and measure per-frame allocation
            List<long> frameMemory = new List<long>();
            
            for (int i = 0; i < 1000; i++)
            {
                Simulation.Tick(BattleSimulation.FIXED_DT);
                
                if (i % 100 == 0)
                {
                    frameMemory.Add(GC.GetTotalMemory(false));
                }
            }
            
            // Calculate average growth per frame (excluding GC cycles)
            long totalGrowth = frameMemory[frameMemory.Count - 1] - frameMemory[0];
            double avgGrowthPerFrame = totalGrowth / 1000.0;
            
            UnityEngine.Debug.Log($"[MemoryTest] Avg growth per frame: {avgGrowthPerFrame:F0} bytes");
            
            // Should be essentially zero (no allocations per frame in steady state)
            Assert.Less(Math.Abs(avgGrowthPerFrame), 1024, 
                $"Memory growing at {avgGrowthPerFrame:F0} bytes/frame (should be ~0)");
        }

        [Test]
        public void Entity_Pools_Reused_Correctly()
        {
            InitializeSimulation(TestDecks.BalancedDeck, TestDecks.BalancedDeck);
                        SetPlayerElixir(1, 10);
            SetPlayerElixir(2, 10);
            
            // Spawn and kill many units
            for (int cycle = 0; cycle < 100; cycle++)
            {
                // Spawn units
                for (int i = 0; i < 10; i++)
                {
                    PlayCard(1, 26000040, RandomPosition(1)); // Knight
                    PlayCard(2, 26000040, RandomPosition(2));
                }
                
                // Let them fight and die
                StepUntil(() => 
                {
                    return Simulation.Units.Count == 0;
                }, maxSeconds: 10f);
                
                Step(1f); // Cleanup tick
            }
            
            // Entity IDs should not grow unbounded (they're allocated sequentially but should be reasonable)
            uint maxEntityId = 0;
            foreach (var entity in Simulation.GetType().GetField("_entities", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .GetValue(Simulation) as System.Collections.IDictionary)
            {
                var dictEntry = (System.Collections.DictionaryEntry)entity;
                if (dictEntry.Key is uint id && id > maxEntityId)
                    maxEntityId = id;
            }
            
            UnityEngine.Debug.Log($"[MemoryTest] Max entity ID after 100 cycles: {maxEntityId}");
            
            // IDs should not be excessively high (indicating leaks or not cleaning up)
            Assert.Less(maxEntityId, 10000, "Entity IDs should not grow excessively");
        }

        [Test]
        public void Event_Logs_Do_Not_Grow_Unbounded()
        {
            InitializeSimulation(TestDecks.BalancedDeck, TestDecks.BalancedDeck);
                        SetPlayerElixir(1, 10);
            SetPlayerElixir(2, 10);
            
            // Play many cards to generate events
            for (int i = 0; i < 1000; i++)
            {
                if (Simulation.Player1.Elixir >= 3)
                {
                    var card = Simulation.Player1.Hand[0];
                    PlayCard(1, card, RandomPosition(1));
                }
                if (Simulation.Player2.Elixir >= 3)
                {
                    var card = Simulation.Player2.Hand[0];
                    PlayCard(2, card, RandomPosition(2));
                }
                Step(0.5f);
            }
            
            var eventLog = Simulation.EventLog;
            var replayLog = Simulation.GetReplayLog();
            
            UnityEngine.Debug.Log($"[MemoryTest] Event log: {eventLog.Count} events, Replay log: {replayLog.Count} events");
            
            // Logs should be bounded or capped
            // In a real implementation, there would be a max size
            Assert.Less(eventLog.Count, 10000, "Event log should not grow unbounded");
            Assert.Less(replayLog.Count, 10000, "Replay log should not grow unbounded");
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
            Simulation.Tick(BattleSimulation.FIXED_DT);
        }

        private Vector2 RandomPosition(int playerId)
        {
            if (playerId == 1)
            {
                return new Vector2(UnityEngine.Random.Range(2f, 16f), UnityEngine.Random.Range(2f, 12f));
            }
            return new Vector2(UnityEngine.Random.Range(2f, 16f), UnityEngine.Random.Range(20f, 30f));
        }
    }
}
