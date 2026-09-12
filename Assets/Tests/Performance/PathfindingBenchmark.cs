using System.Collections.Generic;
using System.Diagnostics;
using NUnit.Framework;
using UnityEngine;
using CRClone.Battle.Simulation;
using CRClone.Core;
using CRClone.Data;
using CRClone.Tests.TestFixtures;

namespace CRClone.Tests.Performance
{
    [TestFixture]
    public class PathfindingBenchmark : BattleTestBase
    {
        [Test]
        public void Pathfinding_10k_Paths_Under_100ms()
        {
            InitializeSimulation(TestDecks.BalancedDeck, TestDecks.BalancedDeck);

            var pathfinding = GetPathfinding();
            pathfinding.Initialize();

            var sw = Stopwatch.StartNew();
            for (int i = 0; i < 10000; i++)
                pathfinding.FindPath(RandomPosition(), RandomPosition(), false);
            sw.Stop();

            UnityEngine.Debug.Log($"[PathfindingBenchmark] 10k paths: {sw.ElapsedMilliseconds}ms");

            Assert.Less(sw.ElapsedMilliseconds, 100,
                $"Pathfinding too slow: {sw.ElapsedMilliseconds}ms for 10k paths (limit 100ms)");
        }

        [Test]
        public void Pathfinding_Ground_Paths_Stay_Fast()
        {
            InitializeSimulation(TestDecks.BalancedDeck, TestDecks.BalancedDeck);

            var pathfinding = GetPathfinding();
            pathfinding.Initialize();

            var sw = Stopwatch.StartNew();
            for (int i = 0; i < 5000; i++)
                pathfinding.FindPath(RandomPosition(), RandomPosition(), false);
            sw.Stop();

            UnityEngine.Debug.Log($"[PathfindingBenchmark] 5k ground paths: {sw.ElapsedMilliseconds}ms");

            Assert.Less(sw.ElapsedMilliseconds, 60);
        }

        [Test]
        public void Pathfinding_With_Buildings_Obstacles()
        {
            InitializeSimulation(TestDecks.BalancedDeck, TestDecks.BalancedDeck);

            var pathfinding = GetPathfinding();
            pathfinding.Initialize();

            var buildings = new List<Building>();
            for (int i = 0; i < 20; i++)
            {
                var cardData = DataManager.GetCard(94); // Cannon
                if (cardData == null) continue;
                var stats = cardData.GetStats(11);
                buildings.Add(new Building((uint)(10000 + i), 1, cardData, stats, RandomPosition(), 11));
            }
            pathfinding.UpdateBuildingCollision(buildings);

            var sw = Stopwatch.StartNew();
            for (int i = 0; i < 5000; i++)
                pathfinding.FindPath(RandomPosition(), RandomPosition(), false);
            sw.Stop();

            UnityEngine.Debug.Log($"[PathfindingBenchmark] With buildings: {sw.ElapsedMilliseconds}ms");

            Assert.Less(sw.ElapsedMilliseconds, 100);
        }

        [Test]
        public void Pathfinding_River_Crossing_Paths()
        {
            InitializeSimulation(TestDecks.BalancedDeck, TestDecks.BalancedDeck);

            var pathfinding = GetPathfinding();
            pathfinding.Initialize();

            var sw = Stopwatch.StartNew();
            for (int i = 0; i < 2000; i++)
            {
                var start = new Vector2(UnityEngine.Random.Range(1f, 17f), UnityEngine.Random.Range(1f, 13f));
                var end = new Vector2(UnityEngine.Random.Range(1f, 17f), UnityEngine.Random.Range(19f, 31f));
                pathfinding.FindPath(start, end, false);
            }
            sw.Stop();

            UnityEngine.Debug.Log($"[PathfindingBenchmark] 2k river crossings: {sw.ElapsedMilliseconds}ms");

            Assert.Less(sw.ElapsedMilliseconds, 60);
        }

        private Pathfinding GetPathfinding()
        {
            var field = typeof(BattleSimulation).GetField("_pathfinding",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return field?.GetValue(Simulation) as Pathfinding;
        }

        private Vector2 RandomPosition()
        {
            return new Vector2(UnityEngine.Random.Range(0f, 18f), UnityEngine.Random.Range(0f, 32f));
        }
    }
}
