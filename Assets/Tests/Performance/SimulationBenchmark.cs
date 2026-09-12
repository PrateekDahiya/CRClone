using System.Collections.Generic;
using System.Diagnostics;
using NUnit.Framework;
using UnityEngine;
using CRClone.Battle.Simulation;
using CRClone.Core;
using CRClone.Data;
using CRClone.Tests.TestFixtures;
using PlayerInput = CRClone.Network.PlayerInput;
using InputType = CRClone.Network.InputType;

namespace CRClone.Tests.Performance
{
    [TestFixture]
    public class SimulationBenchmark : BattleTestBase
    {
        [Test]
        public void BattleSimulation_60FPS_Under_Load()
        {
            InitializeSimulation(TestDecks.BalancedDeck, TestDecks.BalancedDeck);

            // 50v50 Knights across both halves
            for (int i = 0; i < 50; i++)
            {
                SpawnUnitDirect(1, 89, new Vector2(9 + (i % 5) * 2, 5 + (i / 5)));
                SpawnUnitDirect(2, 89, new Vector2(9 + (i % 5) * 2, 25 - (i / 5)));
            }

            var sw = Stopwatch.StartNew();
            int ticks = 3600; // 60 seconds at 60Hz
            for (int tick = 0; tick < ticks; tick++)
                Simulation.Tick(BattleSimulation.FIXED_DT);
            sw.Stop();

            var avgMsPerTick = sw.ElapsedMilliseconds / (double)ticks;
            UnityEngine.Debug.Log($"[Benchmark] Avg tick time: {avgMsPerTick:F4}ms (budget 16.67ms)");

            Assert.Less(avgMsPerTick, 16.67, $"Simulation too slow: {avgMsPerTick:F4}ms per tick");
        }

        [Test]
        public void BattleSimulation_With_Spells_And_Buildings()
        {
            InitializeSimulation(TestDecks.BuildingDeck, TestDecks.SpellHeavyDeck);

            for (int i = 0; i < 10; i++)
            {
                SpawnBuildingDirect(1, 94, new Vector2(7 + (i % 3) * 2, 8 + (i / 3) * 2));
                SpawnBuildingDirect(2, 95, new Vector2(7 + (i % 3) * 2, 22 - (i / 3) * 2));
            }

            var sw = Stopwatch.StartNew();
            int ticks = 3600;
            for (int tick = 0; tick < ticks; tick++)
            {
                Simulation.Tick(BattleSimulation.FIXED_DT);
                if (tick % 300 == 0)
                {
                    SetPlayerElixir(1, 10);
                    SetPlayerElixir(2, 10);
                    Simulation.QueueInput(1, new PlayerInput
                    {
                        type = InputType.PlayCard,
                        cardId = 57,
                        position = new Vector2(9, 24)
                    });
                    Simulation.QueueInput(2, new PlayerInput
                    {
                        type = InputType.PlayCard,
                        cardId = 57,
                        position = new Vector2(9, 8)
                    });
                }
            }
            sw.Stop();

            var avgMsPerTick = sw.ElapsedMilliseconds / (double)ticks;
            UnityEngine.Debug.Log($"[Benchmark] Buildings + Spells avg tick: {avgMsPerTick:F4}ms");

            Assert.Less(avgMsPerTick, 16.67, "Should maintain 60 FPS with buildings and spells");
        }

        private void SpawnUnitDirect(int playerId, int cardId, Vector2 position)
        {
            var cardData = DataManager.GetCard(cardId);
            if (cardData == null) return;

            var stats = cardData.GetStats(11);
            var field = typeof(BattleSimulation).GetField("_nextEntityId",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            uint id = (uint)field.GetValue(Simulation);
            field.SetValue(Simulation, id + 1);

            var unit = new Unit(id, playerId, cardData, stats, position, 11);
            var unitsField = typeof(BattleSimulation).GetField("_units",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var entitiesField = typeof(BattleSimulation).GetField("_entities",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            ((List<Unit>)unitsField.GetValue(Simulation)).Add(unit);
            ((Dictionary<uint, Entity>)entitiesField.GetValue(Simulation))[unit.Id] = unit;
        }

        private void SpawnBuildingDirect(int playerId, int cardId, Vector2 position)
        {
            var cardData = DataManager.GetCard(cardId);
            if (cardData == null) return;

            var stats = cardData.GetStats(11);
            var field = typeof(BattleSimulation).GetField("_nextEntityId",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            uint id = (uint)field.GetValue(Simulation);
            field.SetValue(Simulation, id + 1);

            var building = new Building(id, playerId, cardData, stats, position, 11);
            var buildingsField = typeof(BattleSimulation).GetField("_buildings",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var entitiesField = typeof(BattleSimulation).GetField("_entities",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            ((List<Building>)buildingsField.GetValue(Simulation)).Add(building);
            ((Dictionary<uint, Entity>)entitiesField.GetValue(Simulation))[building.Id] = building;
        }
    }
}
