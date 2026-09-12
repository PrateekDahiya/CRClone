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
    public class SimulationBenchmark : BattleTestBase
    {
        [Test]
        public void BattleSimulation_60FPS_Under_Load()
        {
            InitializeSimulation(TestDecks.BalancedDeck, TestDecks.BalancedDeck);
                        // Spawn max units (50 per side)
            for (int i = 0; i < 50; i++)
            {
                SpawnUnitDirect(1, 26000040, new Vector2(9 + (i % 5) * 2, 5 + (i / 5) * 2));
                SpawnUnitDirect(2, 26000040, new Vector2(9 + (i % 5) * 2, 25 - (i / 5) * 2));
            }
            
            var sw = Stopwatch.StartNew();
            int ticks = 3600; // 60 seconds at 60Hz
            
            for (int tick = 0; tick < ticks; tick++)
            {
                Simulation.Tick(BattleSimulation.FIXED_DT);
            }
            sw.Stop();
            
            var avgMsPerTick = sw.ElapsedMilliseconds / (double)ticks;
            var maxMsPerTick = 16.67; // Budget for 60 FPS
            
            UnityEngine.Debug.Log($"[Benchmark] Avg tick time: {avgMsPerTick:F4}ms, Max allowed: {maxMsPerTick}ms");
            
            Assert.Less(avgMsPerTick, maxMsPerTick, 
                $"Simulation too slow: {avgMsPerTick:F4}ms per tick (budget: {maxMsPerTick}ms)");
        }

        [Test]
        public void BattleSimulation_100_Units_Performance()
        {
            InitializeSimulation(TestDecks.BalancedDeck, TestDecks.BalancedDeck);
                        // Spawn 100 units total
            for (int i = 0; i < 50; i++)
            {
                SpawnUnitDirect(1, 26000040, RandomPosition(1));
                SpawnUnitDirect(2, 26000040, RandomPosition(2));
            }
            
            var sw = Stopwatch.StartNew();
            int ticks = 1800; // 30 seconds
            
            for (int tick = 0; tick < ticks; tick++)
            {
                Simulation.Tick(BattleSimulation.FIXED_DT);
            }
            sw.Stop();
            
            var avgMsPerTick = sw.ElapsedMilliseconds / (double)ticks;
            
            UnityEngine.Debug.Log($"[Benchmark] 100 units - Avg tick time: {avgMsPerTick:F4}ms");
            
            Assert.Less(avgMsPerTick, 16.67, "Should maintain 60 FPS with 100 units");
        }

        [Test]
        public void BattleSimulation_With_Spells_And_Buildings()
        {
            InitializeSimulation(TestDecks.BuildingDeck, TestDecks.SpellHeavyDeck);
                        // Place buildings and cast spells
            for (int i = 0; i < 10; i++)
            {
                SpawnBuildingDirect(1, 26000045, new Vector2(9 + (i % 3) * 2, 10 + (i / 3) * 2));
                SpawnBuildingDirect(2, 26000062, new Vector2(9 + (i % 3) * 2, 20 - (i / 3) * 2));
            }
            
            var sw = Stopwatch.StartNew();
            int ticks = 3600; // 60 seconds
            
            for (int tick = 0; tick < ticks; tick++)
            {
                Simulation.Tick(BattleSimulation.FIXED_DT);
                
                // Cast spells periodically
                if (tick % 300 == 0)
                {
                    Simulation.QueueInput(1, new PlayerInput
                    {
                        type = InputType.CastSpell,
                        spellId = 26000044,
                        position = new Vector2(9, 20)
                    });
                    Simulation.QueueInput(2, new PlayerInput
                    {
                        type = InputType.CastSpell,
                        spellId = 26000044,
                        position = new Vector2(9, 10)
                    });
                }
            }
            sw.Stop();
            
            var avgMsPerTick = sw.ElapsedMilliseconds / (double)ticks;
            
            UnityEngine.Debug.Log($"[Benchmark] Buildings + Spells - Avg tick time: {avgMsPerTick:F4}ms");
            
            Assert.Less(avgMsPerTick, 16.67, "Should maintain 60 FPS with buildings and spells");
        }

        private void SpawnUnitDirect(int playerId, int cardId, Vector2 position)
        {
            var cardData = DataManager.GetCard(cardId);
            if (cardData == null) return;
            
            var stats = cardData.GetStats(11);
            var unit = new Unit(Simulation.GetNextEntityIdForTest(), playerId, cardData, stats, position, 11);
            
            // Use reflection to add to simulation
            var unitsField = typeof(BattleSimulation).GetField("_units", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var entitiesField = typeof(BattleSimulation).GetField("_entities", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (unitsField != null && entitiesField != null)
            {
                var units = (List<Unit>)unitsField.GetValue(Simulation);
                var entities = (Dictionary<uint, Entity>)entitiesField.GetValue(Simulation);
                
                units.Add(unit);
                entities[unit.Id] = unit;
            }
        }

        private void SpawnBuildingDirect(int playerId, int cardId, Vector2 position)
        {
            var cardData = DataManager.GetCard(cardId);
            if (cardData == null) return;
            
            var stats = cardData.GetStats(11);
            var building = new Building(Simulation.GetNextEntityIdForTest(), playerId, cardData, stats, position, 11);
            
            var buildingsField = typeof(BattleSimulation).GetField("_buildings", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var entitiesField = typeof(BattleSimulation).GetField("_entities", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (buildingsField != null && entitiesField != null)
            {
                var buildings = (List<Building>)buildingsField.GetValue(Simulation);
                var entities = (Dictionary<uint, Entity>)entitiesField.GetValue(Simulation);
                
                buildings.Add(building);
                entities[building.Id] = building;
            }
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
