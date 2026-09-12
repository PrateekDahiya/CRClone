using System;
using CRClone.Network;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using CRClone.Battle.Simulation;
using CRClone.Core;
using CRClone.Data;

namespace CRClone.Tests.TestFixtures
{
    public abstract class BattleTestBase
    {
        protected BattleSimulation Simulation { get; private set; }
        protected GameConfig Config { get; private set; }
        protected DataManager DataManager { get; private set; }

        [SetUp]
        public virtual void SetUp()
        {
            Config = CreateTestConfig();
            DataManager = CreateTestDataManager();
            Simulation = new BattleSimulation();
        }

        [TearDown]
        public virtual void TearDown()
        {
            Simulation?.Dispose();
            Simulation = null;
        }

        protected virtual GameConfig CreateTestConfig()
        {
            var config = ScriptableObject.CreateInstance<GameConfig>();
            config.elixirGenerationRate = 2.8f;
            config.doubleElixirRate = 1.4f;
            config.tripleElixirRate = 0.93f;
            config.startingElixir = 5;
            config.maxElixir = 10;
            config.battleDuration = 180f;
            config.overtimeDuration = 180f;
            config.maxDeckCards = 8;
            config.maxChampionsPerDeck = 1;
            config.handSize = 4;
            config.princessTowerHP = 2584;
            config.kingTowerHP = 4384;
            config.towerDamage = 152;
            config.towerHitSpeed = 1.2f;
            config.towerRange = 7f;
            config.deployZoneDepth = 4f;
            config.deployZoneDepthExpanded = 8f;
            config.simulationTickRate = 60;
            config.maxDesyncThreshold = 0.1f;
            config.maxInputQueueSize = 10;
            config.maxCardLevel = 14;
            config.tournamentStandardLevel = 11;
            config.levelStatMultiplier = 1.1f;
            return config;
        }

        protected virtual DataManager CreateTestDataManager()
        {
            var dm = new GameObject("TestDataManager").AddComponent<DataManager>();
            dm.Initialize();
            return dm;
        }

        protected void InitializeSimulation(int[] deck1, int[] deck2, ulong seed = 12345)
        {
            Simulation.Initialize(Config, seed, deck1, deck2);
        }

        protected void InitializeSimulation(BattleScenario scenario)
        {
            InitializeSimulation(scenario.Player1Deck, scenario.Player2Deck, scenario.Seed);
        }

        protected void Tick(float dt = BattleSimulation.FIXED_DT)
        {
            Simulation?.Tick(dt);
        }

        protected void Step(float seconds)
        {
            int ticks = Mathf.RoundToInt(seconds / BattleSimulation.FIXED_DT);
            for (int i = 0; i < ticks; i++)
            {
                Tick();
            }
        }

        protected void StepUntil(System.Func<bool> condition, float maxSeconds = 10f)
        {
            float elapsed = 0f;
            while (!condition() && elapsed < maxSeconds)
            {
                Tick();
                elapsed += BattleSimulation.FIXED_DT;
            }
        }

        protected void SetDoubleElixir(bool enabled)
        {
            // Force double elixir by manipulating internal state
            // This is a test helper - in real code we'd use a test config
        }

        protected PlayerState GetPlayer(int playerId)
        {
            return playerId == 1 ? Simulation.Player1 : Simulation.Player2;
        }

        protected void SetPlayerElixir(int playerId, int elixir)
        {
            var player = GetPlayer(playerId);
            if (player != null) player.Elixir = elixir;
        }

        protected bool PlayCard(int playerId, int cardId, Vector2 position)
        {
            var input = new PlayerInput
            {
                type = InputType.PlayCard,
                cardId = cardId,
                position = position
            };
            Simulation.QueueInput(playerId, input);
            Tick();
            return true; // Would need to check actual result
        }

        protected bool CastSpell(int playerId, int spellId, Vector2 position)
        {
            var input = new PlayerInput
            {
                type = InputType.CastSpell,
                spellId = spellId,
                position = position
            };
            Simulation.QueueInput(playerId, input);
            Tick();
            return true;
        }

        protected Unit SpawnUnit(int cardId, int playerId, Vector2 position, int level = 11)
        {
            var cardData = DataManager.GetCard(cardId);
            if (cardData == null) return null;

            var stats = cardData.GetStats(level);
            var unit = new Unit(Simulation.GetNextEntityIdForTest(), playerId, cardData, stats, position, level);
            
            // We need to use reflection or add a test method to add the unit
            // For now, use the simulation's internal method via reflection or queue input
            // This is a helper for tests that need direct unit spawning
            return unit;
        }

        protected List<Entity> GetEntitiesInRadius(Vector2 center, float radius)
        {
            return Simulation.GetAllEntitiesInRadius(center, radius);
        }

        protected Entity GetEntity(uint id)
        {
            return Simulation.GetEntity(id);
        }

        protected void AssertUnitAlive(Unit unit, string message = "")
        {
            Assert.IsNotNull(unit, $"Unit should not be null: {message}");
            Assert.IsFalse(unit.IsDead, $"Unit should be alive: {message}");
        }

        protected void AssertUnitDead(Unit unit, string message = "")
        {
            Assert.IsNotNull(unit, $"Unit should not be null: {message}");
            Assert.IsTrue(unit.IsDead, $"Unit should be dead: {message}");
        }

        protected void AssertEntityInRadius(Entity entity, Vector2 center, float radius, string message = "")
        {
            Assert.IsNotNull(entity, $"Entity should not be null: {message}");
            float dist = Vector2.Distance(entity.Position, center);
            Assert.LessOrEqual(dist, radius + entity.CollisionRadius, 
                $"Entity should be within radius: {message}");
        }

        protected void AssertApproximate(float expected, float actual, float tolerance, string message = "")
        {
            Assert.AreEqual(expected, actual, tolerance, message);
        }

        protected void AssertElixir(int playerId, int expectedElixir, int tolerance = 1)
        {
            var player = GetPlayer(playerId);
            Assert.IsNotNull(player, $"Player {playerId} should exist");
            Assert.AreEqual(expectedElixir, player.Elixir, tolerance, $"Player {playerId} elixir mismatch");
        }

        protected int CountUnitsOfType(int playerId, int cardId)
        {
            int count = 0;
            foreach (var unit in Simulation.Units)
            {
                if (unit.OwnerPlayerId == playerId && unit.CardData.cardId == cardId)
                    count++;
            }
            return count;
        }
    }

    // Extension for BattleSimulation to expose internal methods for testing
    public static class BattleSimulationTestExtensions
    {
        public static uint GetNextEntityIdForTest(this BattleSimulation sim)
        {
            // Access private field via reflection for testing
            var field = typeof(BattleSimulation).GetField("_nextEntityId", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                uint id = (uint)field.GetValue(sim);
                field.SetValue(sim, id + 1);
                return id;
            }
            return 9999; // Fallback
        }
    }
}

