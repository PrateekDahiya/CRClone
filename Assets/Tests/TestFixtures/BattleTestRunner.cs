using System.Collections.Generic;
using UnityEngine;
using CRClone.Core;
using CRClone.Battle.Simulation;
using CRClone.Data;
using CRClone.Tests.TestFixtures;
using PlayerInput = CRClone.Network.PlayerInput;
using InputType = CRClone.Network.InputType;

namespace CRClone.Testing
{
    /// <summary>
    /// Manual in-editor smoke-test runner. The authoritative automated suite
    /// lives in Assets/Tests/Unit + Assets/Tests/Integration (NUnit).
    /// Attach to a GameObject in a test scene with _runOnStart enabled to
    /// play a short scripted battle and dump the result to the console.
    /// Card IDs match Assets/Resources/Data/Cards (Knight=89, Archers=90,
    /// Giant=53, Musketeer=54, Fireball=57, Cannon=94, Skeletons=92, Minions=93).
    /// </summary>
    public class BattleTestRunner : MonoBehaviour
    {
        [Header("Test Config")]
        [SerializeField] private bool _runOnStart = false;
        [SerializeField] private int _testDurationSeconds = 20;
        [SerializeField] private ulong _seed = 12345;

        private BattleSimulation _simulation;
        private float _testTimer;
        private int _ticksRun;

        private void Start()
        {
            if (_runOnStart)
                RunSmokeTest();
        }

        public void RunSmokeTest()
        {
            Debug.Log("[BattleTestRunner] Starting smoke test...");

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

            var holders = new GameObject("SmokeTestServices");
            var configManager = holders.AddComponent<ConfigManager>();
            configManager.Initialize(config);
            Services.Register<ConfigManager>(configManager);
            var dataManager = holders.AddComponent<DataManager>();
            dataManager.Initialize();
            Services.Register<DataManager>(dataManager);

            _simulation = new BattleSimulation();
            _simulation.Initialize(config, _seed, TestDecks.BalancedDeck, TestDecks.BalancedDeck);

            // Scripted opener: Knight vs Knight, then Fireball (valid zones: P1 y<=13, P2 y>=19)
            QueuePlay(1, 89, new Vector2(9, 8));
            QueuePlay(2, 89, new Vector2(9, 24));

            _testTimer = _testDurationSeconds;
            _ticksRun = 0;
            Debug.Log("[BattleTestRunner] Smoke test running...");
        }

        private void QueuePlay(int playerId, int cardId, Vector2 position)
        {
            var player = playerId == 1 ? _simulation.Player1 : _simulation.Player2;
            player.Elixir = 10;
            _simulation.QueueInput(playerId, new PlayerInput
            {
                type = InputType.PlayCard,
                cardId = (uint)cardId,
                position = position
            });
        }

        private void Update()
        {
            if (_simulation == null || _simulation.Status != BattleStatus.Playing)
                return;

            _simulation.Tick(BattleSimulation.FIXED_DT);
            _ticksRun++;
            _testTimer -= Time.deltaTime;

            // Mid-test Fireball for coverage
            if (_ticksRun == 300)
            {
                QueuePlay(1, 57, new Vector2(9, 24));
                Debug.Log("[BattleTestRunner] Fireball cast at tick 300");
            }

            if (_testTimer <= 0)
                EndTest();
        }

        private void EndTest()
        {
            Debug.Log($"[BattleTestRunner] Done after {_ticksRun} ticks. Status={_simulation.Status} " +
                      $"P1 elixir={_simulation.Player1.Elixir} P2 elixir={_simulation.Player2.Elixir} " +
                      $"units={_simulation.Units.Count} buildings={_simulation.Buildings.Count} " +
                      $"events={_simulation.EventLog.Count} replay={_simulation.GetReplayLog().Count}");

            foreach (var unit in _simulation.Units)
            {
                Debug.Log($"  Unit {unit.CardData.cardName} (P{unit.OwnerPlayerId}): " +
                          $"HP {unit.CurrentHP}/{unit.MaxHP} @ {unit.Position} [{unit.State}]");
            }

            Services.Unregister<ConfigManager>();
            Services.Unregister<DataManager>();
            _simulation.Dispose();
            _simulation = null;
        }

        private void OnDestroy()
        {
            _simulation?.Dispose();
            _simulation = null;
        }
    }
}
