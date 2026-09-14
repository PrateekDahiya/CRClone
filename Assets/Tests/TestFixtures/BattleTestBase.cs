using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using CRClone.Battle.Simulation;
using CRClone.Core;
using CRClone.Data;
using PlayerInput = CRClone.Network.PlayerInput;
using InputType = CRClone.Network.InputType;
using BattleStatus = CRClone.Core.BattleStatus;
using Unit = CRClone.Battle.Simulation.Unit;

namespace CRClone.Tests.TestFixtures
{
    public abstract class BattleTestBase
    {
        protected BattleSimulation Simulation { get; private set; }
        protected GameConfig Config { get; private set; }
        protected DataManager DataManager { get; private set; }

        private GameObject _testObjects;

        [SetUp]
        public virtual void SetUp()
        {
            _testObjects = new GameObject("TestServices");
            Config = CreateTestConfig();

            var configManager = _testObjects.AddComponent<ConfigManager>();
            configManager.Initialize(Config);
            Services.Register<ConfigManager>(configManager);

            DataManager = _testObjects.AddComponent<DataManager>();
            DataManager.Initialize();
            Services.Register<DataManager>(DataManager);

            Simulation = new BattleSimulation();
        }

        [TearDown]
        public virtual void TearDown()
        {
            Simulation?.Dispose();
            Simulation = null;

            Services.Unregister<ConfigManager>();
            Services.Unregister<DataManager>();
            if (_testObjects != null)
            {
                UnityEngine.Object.DestroyImmediate(_testObjects);
                _testObjects = null;
            }
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

        protected void InitializeSimulation(int[] deck1, int[] deck2, ulong seed = 12345)
        {
            Simulation.Initialize(Config, seed, deck1, deck2);
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

        protected PlayerState GetPlayer(int playerId)
        {
            return playerId == 1 ? Simulation.Player1 : Simulation.Player2;
        }

        protected void SetPlayerElixir(int playerId, int elixir)
        {
            var player = GetPlayer(playerId);
            if (player != null) player.Elixir = elixir;
        }

        /// <summary>
        /// Queues a PlayCard input with topped-up elixir so tests exercise
        /// mechanics rather than the economy. Returns the spawned unit if any.
        /// Valid P1 deploy: y &lt;= 13. Valid P2 deploy: y &gt;= 19. Spells: anywhere.
        /// </summary>
        protected Unit PlayCard(int playerId, int cardId, Vector2 position)
        {
            SetPlayerElixir(playerId, 10);
            var input = new PlayerInput
            {
                type = InputType.PlayCard,
                cardId = (uint)cardId,
                position = position
            };
            Simulation.QueueInput(playerId, input);
            Tick();
            return FindUnit(playerId, cardId) ?? FindBuildingAsUnit(playerId, cardId);
        }

        protected void CastSpell(int playerId, int spellId, Vector2 position)
        {
            SetPlayerElixir(playerId, 10);
            var input = new PlayerInput
            {
                type = InputType.PlayCard,
                cardId = (uint)spellId,
                position = position
            };
            Simulation.QueueInput(playerId, input);
            Tick();
        }

        protected void UseChampionAbility(int playerId, Vector2 position)
        {
            SetPlayerElixir(playerId, 10);
            var input = new PlayerInput
            {
                type = InputType.ChampionAbility,
                position = position
            };
            Simulation.QueueInput(playerId, input);
            Tick();
        }

        protected Unit FindUnit(int playerId, int cardId)
        {
            foreach (var unit in Simulation.Units)
            {
                if (unit.OwnerPlayerId == playerId && unit.CardData.cardId == cardId)
                    return unit;
            }
            return null;
        }

        protected List<Unit> FindUnits(int playerId, int cardId)
        {
            var result = new List<Unit>();
            foreach (var unit in Simulation.Units)
            {
                if (unit.OwnerPlayerId == playerId && unit.CardData.cardId == cardId)
                    result.Add(unit);
            }
            return result;
        }

        protected List<Unit> FindUnitsByName(int playerId, string cardName)
        {
            var result = new List<Unit>();
            foreach (var unit in Simulation.Units)
            {
                if (unit.OwnerPlayerId == playerId && unit.CardData.cardName == cardName)
                    result.Add(unit);
            }
            return result;
        }

        protected Building FindBuilding(int playerId, int cardId)
        {
            foreach (var building in Simulation.Buildings)
            {
                if (building.OwnerPlayerId == playerId && building.CardData.cardId == cardId)
                    return building;
            }
            return null;
        }

        private Unit FindBuildingAsUnit(int playerId, int cardId)
        {
            return null; // Buildings are not units; kept for API symmetry
        }

        protected Tower FindTower(int playerId, TowerType type)
        {
            foreach (var tower in Simulation.Towers)
            {
                if (tower.OwnerPlayerId == playerId && tower.Type == type)
                    return tower;
            }
            return null;
        }

        protected void AssertElixir(int playerId, int expectedElixir, int tolerance = 1)
        {
            var player = GetPlayer(playerId);
            Assert.IsNotNull(player, $"Player {playerId} should exist");
            Assert.AreEqual(expectedElixir, player.Elixir, tolerance, $"Player {playerId} elixir mismatch");
        }
    }
}
