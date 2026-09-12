using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using CRClone.Battle.Simulation;
using CRClone.Core;
using CRClone.Tests.TestFixtures;
using PlayerInput = CRClone.Network.PlayerInput;
using InputType = CRClone.Network.InputType;

namespace CRClone.Tests.Unit
{
    /// <summary>
    /// Same-seed determinism regression tests (ISSUE-601).
    /// Runs BattleSimulation twice with the same seed + scripted inputs and
    /// asserts per-tick state hashes (positions/HP/elixir) are identical.
    /// A different-seed run must differ (anti-vacuous guard).
    /// </summary>
    [TestFixture]
    public class DeterminismTests : BattleTestBase
    {
        private static readonly int[] Checkpoints = { 100, 500, 1000 };
        private const ulong SeedA = 12345;
        private const ulong SeedB = 99999;

        [Test]
        public void SameSeed_SameInputs_Produce_Identical_StateHashes()
        {
            var scenario = TestScenarios.Standard1v1;
            var hashesA = RunScriptedBattle(scenario.Player1Deck, scenario.Player2Deck, SeedA);
            var hashesB = RunScriptedBattle(scenario.Player1Deck, scenario.Player2Deck, SeedA);

            Assert.AreEqual(Checkpoints.Length, hashesA.Count, "Run A should capture all checkpoints");
            Assert.AreEqual(Checkpoints.Length, hashesB.Count, "Run B should capture all checkpoints");
            foreach (int tick in Checkpoints)
            {
                Assert.AreEqual(hashesA[tick], hashesB[tick],
                    $"State hash mismatch at tick {tick} for same seed + inputs");
            }
        }

        [Test]
        public void DifferentSeed_Produces_Different_StateHash()
        {
            var scenario = TestScenarios.Standard1v1;
            var hashesA = RunScriptedBattle(scenario.Player1Deck, scenario.Player2Deck, SeedA);
            var hashesC = RunScriptedBattle(scenario.Player1Deck, scenario.Player2Deck, SeedB);

            var trajectoryA = string.Join("|", CheckpointsToList(hashesA));
            var trajectoryC = string.Join("|", CheckpointsToList(hashesC));
            Assert.AreNotEqual(trajectoryA, trajectoryC,
                "Different seeds must diverge (spawn jitter is RNG-driven); equal hashes suggest a vacuous test");
        }

        private List<string> CheckpointsToList(Dictionary<int, string> hashes)
        {
            var list = new List<string>();
            foreach (int tick in Checkpoints)
                list.Add(hashes[tick]);
            return list;
        }

        /// <summary>
        /// Runs a scripted battle headlessly and captures a state hash at each
        /// checkpoint tick. Valid deploys: P1 y &lt;= 13, P2 y &gt;= 19.
        /// </summary>
        private Dictionary<int, string> RunScriptedBattle(int[] deck1, int[] deck2, ulong seed)
        {
            var sim = new BattleSimulation();
            sim.Initialize(Config, seed, deck1, deck2);
            var hashes = new Dictionary<int, string>();
            try
            {
                // Tick 0 scripted plays (elixir topped up; sim consumes real costs).
                QueuePlay(sim, 1, 89, new Vector2(9, 8));   // Knight
                QueuePlay(sim, 2, 89, new Vector2(9, 24));  // Knight

                int guard = 0;
                while (sim.CurrentTick < 1000 && sim.Status == BattleStatus.Playing && guard++ < 1200)
                {
                    if (sim.CurrentTick == 300)
                    {
                        QueuePlay(sim, 1, 90, new Vector2(5, 8));   // Archers
                        QueuePlay(sim, 2, 54, new Vector2(14, 24)); // Musketeer
                    }
                    if (sim.CurrentTick == 600)
                    {
                        QueuePlay(sim, 1, 53, new Vector2(9, 10));  // Giant
                        QueuePlay(sim, 2, 57, new Vector2(9, 10));  // Fireball
                    }
                    sim.Tick();
                    foreach (int checkpoint in Checkpoints)
                    {
                        if ((int)sim.CurrentTick == checkpoint && !hashes.ContainsKey(checkpoint))
                            hashes[checkpoint] = CaptureStateHash(sim);
                    }
                }
            }
            finally
            {
                sim.Dispose();
            }
            return hashes;
        }

        private static void QueuePlay(BattleSimulation sim, int playerId, int cardId, Vector2 position)
        {
            var player = playerId == 1 ? sim.Player1 : sim.Player2;
            player.Elixir = 10;
            sim.QueueInput(playerId, new PlayerInput
            {
                type = InputType.PlayCard,
                cardId = (uint)cardId,
                position = position
            });
        }

        /// <summary>
        /// Canonical snapshot: tick, both elixirs, and every unit/building/tower
        /// (id, owner, F4-rounded position, HP, dead flag), entities sorted by id.
        /// FNV-1a hex over the snapshot.
        /// </summary>
        private static string CaptureStateHash(BattleSimulation sim)
        {
            var sb = new StringBuilder();
            sb.Append("tick=").Append(sim.CurrentTick).Append(';');
            sb.Append("p1=").Append(sim.Player1.Elixir).Append(';');
            sb.Append("p2=").Append(sim.Player2.Elixir).Append(';');

            var entities = new List<Entity>();
            foreach (var u in sim.Units) entities.Add(u);
            foreach (var b in sim.Buildings) entities.Add(b);
            foreach (var t in sim.Towers) entities.Add(t);
            entities.Sort((a, b) => a.Id.CompareTo(b.Id));

            foreach (var e in entities)
            {
                sb.Append(e.Id).Append(':').Append(e.OwnerPlayerId).Append(':')
                  .Append(e.Position.x.ToString("F4")).Append(',').Append(e.Position.y.ToString("F4")).Append(':')
                  .Append(e.CurrentHP).Append(':').Append(e.IsDead ? '1' : '0').Append(';');
            }

            uint hash = 2166136261;
            string snapshot = sb.ToString();
            foreach (char c in snapshot)
            {
                hash ^= c;
                hash *= 16777619;
            }
            return hash.ToString("x8");
        }
    }
}
