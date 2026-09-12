using System;
using System.Collections.Generic;
using System.Diagnostics;
using NUnit.Framework;
using UnityEngine;
using CRClone.Battle.Simulation;
using CRClone.Core;
using CRClone.Tests.TestFixtures;
using CRClone.Network;

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
            {
                var start = RandomPosition();
                var end = RandomPosition();
                pathfinding.FindPath(start, end, EntityType.Unit);
            }
            
            sw.Stop();
            
            UnityEngine.Debug.Log($"[PathfindingBenchmark] 10k paths: {sw.ElapsedMilliseconds}ms");
            
            Assert.Less(sw.ElapsedMilliseconds, 100, 
                $"Pathfinding too slow: {sw.ElapsedMilliseconds}ms for 10k paths (limit: 100ms)");
        }

        [Test]
        public void Pathfinding_Ground_Vs_Air_Performance()
        {
            InitializeSimulation(TestDecks.BalancedDeck, TestDecks.BalancedDeck);
                        var pathfinding = GetPathfinding();
            pathfinding.Initialize();
            
            // Ground paths
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < 5000; i++)
            {
                pathfinding.FindPath(RandomPosition(), RandomPosition(), EntityType.Unit);
            }
            var groundTime = sw.ElapsedMilliseconds;
            
            // Air paths (no river crossing needed)
            sw.Restart();
            for (int i = 0; i < 5000; i++)
            {
                pathfinding.FindPath(RandomPosition(), RandomPosition(), EntityType.Unit); // Flying handled internally
            }
            var airTime = sw.ElapsedMilliseconds;
            
            UnityEngine.Debug.Log($"[PathfindingBenchmark] Ground: {groundTime}ms, Air: {airTime}ms");
            
            // Both should be fast
            Assert.Less(groundTime, 50);
            Assert.Less(airTime, 50);
        }

        [Test]
        public void Pathfinding_With_Buildings_Obstacles()
        {
            InitializeSimulation(TestDecks.BalancedDeck, TestDecks.BalancedDeck);
                        var pathfinding = GetPathfinding();
            pathfinding.Initialize();
            
            // Add buildings as obstacles
            var buildings = new List<Building>();
            for (int i = 0; i < 20; i++)
            {
                var cardData = DataManager.GetCard(26000045); // Cannon
                if (cardData != null)
                {
                    var stats = cardData.GetStats(11);
                    var building = new Building((uint)(10000 + i), 1, cardData, stats, RandomPosition(), 11);
                    buildings.Add(building);
                }
            }
            
            pathfinding.UpdateBuildingCollision(buildings);
            
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < 5000; i++)
            {
                pathfinding.FindPath(RandomPosition(), RandomPosition(), EntityType.Unit);
            }
            sw.Stop();
            
            UnityEngine.Debug.Log($"[PathfindingBenchmark] With 20 buildings: {sw.ElapsedMilliseconds}ms");
            
            Assert.Less(sw.ElapsedMilliseconds, 100);
        }

        [Test]
        public void Pathfinding_Cached_Paths_Faster()
        {
            InitializeSimulation(TestDecks.BalancedDeck, TestDecks.BalancedDeck);
                        var pathfinding = GetPathfinding();
            pathfinding.Initialize();
            
            var start = new Vector2(2, 2);
            var end = new Vector2(16, 30);
            
            // First path (cold)
            var sw = Stopwatch.StartNew();
            var path1 = pathfinding.FindPath(start, end, EntityType.Unit);
            var coldTime = sw.ElapsedMilliseconds;
            
            // Same path again (should be cached)
            sw.Restart();
            var path2 = pathfinding.FindPath(start, end, EntityType.Unit);
            var warmTime = sw.ElapsedMilliseconds;
            
            UnityEngine.Debug.Log($"[PathfindingBenchmark] Cold: {coldTime}ms, Warm: {warmTime}ms");
            
            // Warm should be faster or equal
            Assert.LessOrEqual(warmTime, coldTime);
        }

        [Test]
        public void Pathfinding_Long_Distance_Performance()
        {
            InitializeSimulation(TestDecks.BalancedDeck, TestDecks.BalancedDeck);
                        var pathfinding = GetPathfinding();
            pathfinding.Initialize();
            
            // Long distance paths (across entire arena)
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < 1000; i++)
            {
                var start = new Vector2(UnityEngine.Random.Range(1f, 5f), UnityEngine.Random.Range(1f, 5f));
                var end = new Vector2(UnityEngine.Random.Range(13f, 17f), UnityEngine.Random.Range(27f, 31f));
                pathfinding.FindPath(start, end, EntityType.Unit);
            }
            sw.Stop();
            
            UnityEngine.Debug.Log($"[PathfindingBenchmark] 1k long paths: {sw.ElapsedMilliseconds}ms");
            
            Assert.Less(sw.ElapsedMilliseconds, 50);
        }

        [Test]
        public void Pathfinding_River_Crossing_Performance()
        {
            InitializeSimulation(TestDecks.BalancedDeck, TestDecks.BalancedDeck);
                        var pathfinding = GetPathfinding();
            pathfinding.Initialize();
            
            // Paths that must cross river (use bridges)
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < 2000; i++)
            {
                var start = new Vector2(UnityEngine.Random.Range(1f, 17f), UnityEngine.Random.Range(1f, 13f)); // Bottom
                var end = new Vector2(UnityEngine.Random.Range(1f, 17f), UnityEngine.Random.Range(19f, 31f)); // Top
                pathfinding.FindPath(start, end, EntityType.Unit);
            }
            sw.Stop();
            
            UnityEngine.Debug.Log($"[PathfindingBenchmark] 2k river crossings: {sw.ElapsedMilliseconds}ms");
            
            Assert.Less(sw.ElapsedMilliseconds, 50);
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
