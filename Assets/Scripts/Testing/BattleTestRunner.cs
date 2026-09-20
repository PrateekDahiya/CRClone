using UnityEngine;
using UnityEngine.SceneManagement;
using CRClone.Core;
using CRClone.Data;
using CRClone.Battle.Simulation;

namespace CRClone.Testing
{
    public class BattleTestRunner : MonoBehaviour
    {
        [Header("Test Config")]
        [SerializeField] private bool _runOnStart = true;
        [SerializeField] private int _testDurationSeconds = 10;

        private BattleSimulation _simulation;
        private float _testTimer = 0f;

        private void Start()
        {
            if (_runOnStart)
            {
                RunTest();
            }
        }

        public void RunTest()
        {
            Debug.Log("[BattleTestRunner] Starting battle simulation test...");

            // Create test decks
            int[] deck1 = { 26000040, 26000041, 26000042, 26000043, 26000044, 26000045, 26000046, 26000047 }; // Knight, Archers, Giant, etc.
            int[] deck2 = { 26000040, 26000041, 26000042, 26000043, 26000044, 26000045, 26000046, 26000047 };

            // Initialize simulation
            var config = Services.Get<ConfigManager>().GetConfig();
            _simulation = new BattleSimulation();
            _simulation.Initialize(config, 12345, deck1, deck2);

            // Spawn test units
            var dataManager = Services.Get<DataManager>();
            
            // Player 1: Knight at bottom
            var knightCard = dataManager.GetCard(26000040); // Knight
            if (knightCard != null)
            {
                var knightStats = knightCard.GetStats(11);
                _simulation.SpawnUnit(knightCard, 1, new Vector2(9, 5), 11);
            }

            // Player 2: Knight at top
            if (knightCard != null)
            {
                var knightStats = knightCard.GetStats(11);
                _simulation.SpawnUnit(knightCard, 2, new Vector2(9, 27), 11);
            }

            _testTimer = _testDurationSeconds;
            Debug.Log("[BattleTestRunner] Test initialized, running simulation...");
        }

        private void Update()
        {
            if (_simulation != null && _simulation.Status == BattleStatus.Playing)
            {
                _simulation.Tick(BattleSimulation.FIXED_DT);
                _testTimer -= Time.deltaTime;

                if (_testTimer <= 0)
                {
                    EndTest();
                }
            }
        }

        private void EndTest()
        {
            Debug.Log($"[BattleTestRunner] Test complete. Status: {_simulation.Status}, Tick: {_simulation.CurrentTick}");
            Debug.Log($"Player 1 Elixir: {_simulation.Player1.Elixir:F1}, Units: {_simulation.Units.Count}");
            Debug.Log($"Player 2 Elixir: {_simulation.Player2.Elixir:F1}, Units: {_simulation.Units.Count}");

            foreach (var unit in _simulation.Units)
            {
                Debug.Log($"Unit {unit.CardData.cardName} (P{unit.OwnerPlayerId}): HP {unit.CurrentHP}/{unit.MaxHP}, Pos: {unit.Position}, State: {unit.State}");
            }

            _simulation.Dispose();
            _simulation = null;
        }

        private void OnDestroy()
        {
            if (_simulation != null)
            {
                _simulation.Dispose();
            }
        }
    }
}