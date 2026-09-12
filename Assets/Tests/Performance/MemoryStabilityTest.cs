using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using CRClone.Battle.Simulation;
using CRClone.Core;
using CRClone.Tests.TestFixtures;
using PlayerInput = CRClone.Network.PlayerInput;
using InputType = CRClone.Network.InputType;

namespace CRClone.Tests.Performance
{
    [TestFixture]
    public class MemoryStabilityTest : BattleTestBase
    {
        [Test]
        public void Memory_Stable_Over_Long_Battle()
        {
            InitializeSimulation(TestDecks.BalancedDeck, TestDecks.BalancedDeck);

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            long initialMemory = GC.GetTotalMemory(true);

            // 5 minute battle with steady card play (18000 ticks at 60Hz)
            for (int tick = 0; tick < 18000; tick++)
            {
                if (tick % 60 == 0) // Every second
                {
                    if (Simulation.Player1.Elixir >= 3)
                        PlayCard(1, Simulation.Player1.Hand[0], new Vector2(9, 8));
                    if (Simulation.Player2.Elixir >= 3)
                        PlayCard(2, Simulation.Player2.Hand[0], new Vector2(9, 24));

                    if (tick % 300 == 0 && Simulation.Player1.Elixir >= 4)
                    {
                        SetPlayerElixir(1, 10);
                        Simulation.QueueInput(1, new PlayerInput
                        {
                            type = InputType.PlayCard,
                            cardId = 57, // Fireball
                            position = new Vector2(9, 24)
                        });
                    }
                }

                Simulation.Tick(BattleSimulation.FIXED_DT);

                if (Simulation.Status != BattleStatus.Playing)
                    break; // Battle decided early; memory still must be sane
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            long finalMemory = GC.GetTotalMemory(true);

            long growth = finalMemory - initialMemory;
            UnityEngine.Debug.Log($"[MemoryTest] Growth over long battle: {growth / (1024 * 1024)}MB");

            Assert.Less(growth, 50 * 1024 * 1024, $"Memory grew by {growth / (1024 * 1024)}MB (limit 50MB)");
        }

        [Test]
        public void Event_Logs_Do_Not_Grow_Unbounded()
        {
            InitializeSimulation(TestDecks.BalancedDeck, TestDecks.BalancedDeck);

            for (int i = 0; i < 500; i++)
            {
                if (Simulation.Player1.Elixir >= 3)
                    PlayCard(1, Simulation.Player1.Hand[0], new Vector2(9, 8));
                if (Simulation.Player2.Elixir >= 3)
                    PlayCard(2, Simulation.Player2.Hand[0], new Vector2(9, 24));
                Step(0.5f);
                if (Simulation.Status != BattleStatus.Playing) break;
            }

            UnityEngine.Debug.Log($"[MemoryTest] Events: {Simulation.EventLog.Count}, Replay: {Simulation.GetReplayLog().Count}");

            Assert.Less(Simulation.EventLog.Count, 20000, "Event log should stay bounded");
            Assert.Less(Simulation.GetReplayLog().Count, 60000, "Replay log should stay bounded");
        }
    }
}
