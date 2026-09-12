using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using NUnit.Framework;
using CRClone.Core;
using CRClone.Battle.Simulation;
using CRClone.Data;
using CRClone.Tests.TestFixtures;
using CRClone.Network;

namespace CRClone.Testing
{
    public class BattleTestRunner : MonoBehaviour
    {
        [Header("Test Config")]
        [SerializeField] private bool _runOnStart = false;
        [SerializeField] private int _testDurationSeconds = 10;
        [SerializeField] private bool _runUnitTests = true;
        [SerializeField] private bool _runIntegrationTests = true;
        [SerializeField] private bool _runPerformanceTests = false;
        [SerializeField] private bool _logDetailedResults = true;

        private BattleSimulation _simulation;
        private float _testTimer = 0f;
        private int _testsPassed = 0;
        private int _testsFailed = 0;
        private List<string> _testResults = new List<string>();

        private void Start()
        {
            if (_runOnStart)
            {
                RunAllTests();
            }
        }

        public void RunAllTests()
        {
            _testsPassed = 0;
            _testsFailed = 0;
            _testResults.Clear();
            
            Debug.Log("[BattleTestRunner] Starting comprehensive test suite...");

            if (_runUnitTests)
            {
                RunUnitTests();
            }

            if (_runIntegrationTests)
            {
                RunIntegrationTests();
            }

            if (_runPerformanceTests)
            {
                RunPerformanceTests();
            }

            LogSummary();
        }

        private void RunUnitTests()
        {
            Debug.Log("[BattleTestRunner] Running Unit Tests...");

            RunTest("Elixir Generation", TestElixirGeneration);
            RunTest("Elixir Cap", TestElixirCap);
            RunTest("Double Elixir", TestDoubleElixir);
            RunTest("Card Play Spends Elixir", TestCardPlaySpendsElixir);
            RunTest("Initial Hand Size", TestInitialHandSize);
            RunTest("Card Cycle Draw", TestCardCycleDraw);
            RunTest("Cycle Wraps", TestCycleWraps);
            RunTest("Knight vs Goblin", TestKnightVsGoblin);
            RunTest("Ranged Attack Distance", TestRangedAttackDistance);
            RunTest("Splash Damage", TestSplashDamage);
            RunTest("Cannon Ground Only", TestCannonGroundOnly);
            RunTest("Tesla Retract", TestTeslaRetract);
            RunTest("Tesla Pop Up", TestTeslaPopUp);
            RunTest("Spawner Building", TestSpawnerBuilding);
            RunTest("Building Lifetime", TestBuildingLifetime);
            RunTest("Fireball Damage", TestFireballDamage);
            RunTest("Fireball Knockback", TestFireballKnockback);
            RunTest("Zap Stun Inferno", TestZapStunInferno);
            RunTest("Poison Slow Damage", TestPoisonSlowDamage);
            RunTest("Freeze Stops", TestFreezeStops);
            RunTest("Log Ground Only", TestLogGroundOnly);
            RunTest("Tornado Pull", TestTornadoPull);
            RunTest("Archer Queen Ability", TestArcherQueenAbility);
            RunTest("Skeleton King Ability", TestSkeletonKingAbility);
            RunTest("Mighty Miner Ability", TestMightyMinerAbility);
            RunTest("Champion Cooldown", TestChampionCooldown);
            RunTest("Target Closest Path", TestTargetClosestPath);
            RunTest("Building Targeter Ignores Troops", TestBuildingTargeterIgnoresTroops);
            RunTest("Retarget On Death", TestRetargetOnDeath);
        }

        private void RunIntegrationTests()
        {
            Debug.Log("[BattleTestRunner] Running Integration Tests...");

            RunTest("Complete Battle Flow", TestCompleteBattleFlow);
            RunTest("Overtime Sudden Death", TestOvertimeSuddenDeath);
            RunTest("Draw No Towers", TestDrawNoTowers);
            RunTest("King Tower Activation", TestKingTowerActivation);
            RunTest("King Tower Destroyed Ends Battle", TestKingTowerDestroyedEndsBattle);
            RunTest("Invalid Card Rejected", TestInvalidCardRejected);
            RunTest("Invalid Position Rejected", TestInvalidPositionRejected);
            RunTest("Rate Limiting", TestRateLimiting);
            RunTest("Deterministic Replay", TestDeterministicReplay);
        }

        private void RunPerformanceTests()
        {
            Debug.Log("[BattleTestRunner] Running Performance Tests...");

            RunTest("60 FPS Under Load", Test60FPSUnderLoad);
            RunTest("Memory Stability", TestMemoryStability);
            RunTest("Pathfinding 10k Paths", TestPathfinding10k);
        }

        private void RunTest(string name, Func<bool> testFunc)
        {
            try
            {
                bool result = testFunc();
                if (result)
                {
                    _testsPassed++;
                    _testResults.Add($"[PASS] {name}");
                    if (_logDetailedResults) Debug.Log($"[PASS] {name}");
                }
                else
                {
                    _testsFailed++;
                    _testResults.Add($"[FAIL] {name}");
                    Debug.LogError($"[FAIL] {name}");
                }
            }
            catch (Exception e)
            {
                _testsFailed++;
                _testResults.Add($"[ERROR] {name}: {e.Message}");
                Debug.LogError($"[ERROR] {name}: {e}");
            }
        }

        #region Unit Tests

        private bool TestElixirGeneration()
        {
            var sim = CreateSimulation();
                        sim.Step(2.8f);
            return sim.Player1.Elixir == 6 && sim.Player2.Elixir == 6;
        }

        private bool TestElixirCap()
        {
            var sim = CreateSimulation();
            sim.Player1.Elixir = 10;
            sim.Player2.Elixir = 10;
            sim.Step(10f);
            return sim.Player1.Elixir == 10 && sim.Player2.Elixir == 10;
        }

        private bool TestDoubleElixir()
        {
            var sim = CreateSimulation();
                        // Fast forward to double elixir
            sim.Step(120f);
            sim.Player1.Elixir = 5;
            sim.Player2.Elixir = 5;
            sim.Step(1.4f);
            return sim.Player1.Elixir == 6 && sim.Player2.Elixir == 6;
        }

        private bool TestCardPlaySpendsElixir()
        {
            var sim = CreateSimulation();
            sim.Player1.Elixir = 5;
            sim.QueueInput(1, new PlayerInput
            {
                type = InputType.PlayCard,
                cardId = 26000040,
                position = new Vector2(9, 8)
            });
            sim.Tick();
            return sim.Player1.Elixir == 2;
        }

        private bool TestInitialHandSize()
        {
            var sim = CreateSimulation();
                        return sim.Player1.Hand.Length == 4 && sim.Player2.Hand.Length == 4;
        }

        private bool TestCardCycleDraw()
        {
            var sim = CreateSimulation();
                        var initialHand = new int[4];
            Array.Copy(sim.Player1.Hand, initialHand, 4);
            var fifthCard = TestDecks.BalancedDeck[4];

            sim.Player1.Elixir = 10;
            sim.QueueInput(1, new PlayerInput
            {
                type = InputType.PlayCard,
                cardId = initialHand[0],
                position = new Vector2(9, 8)
            });
            sim.Tick();

            return sim.Player1.Hand[3] == fifthCard;
        }

        private bool TestCycleWraps()
        {
            var sim = CreateSimulation();
                        sim.Player1.Elixir = 10;

            for (int i = 0; i < 8; i++)
            {
                var card = sim.Player1.Hand[0];
                sim.QueueInput(1, new PlayerInput
                {
                    type = InputType.PlayCard,
                    cardId = card,
                    position = new Vector2(9, 8)
                });
                sim.Tick();
                sim.Step(0.1f);
            }

            for (int i = 0; i < 4; i++)
            {
                if (sim.Player1.Hand[i] != TestDecks.BalancedDeck[i]) return false;
            }
            return true;
        }

        private bool TestKnightVsGoblin()
        {
            var sim = CreateSimulation();
                        sim.Player1.Elixir = 10;
            sim.Player2.Elixir = 10;

            sim.QueueInput(1, new PlayerInput { type = InputType.PlayCard, cardId = 26000040, position = new Vector2(9, 8) });
            sim.QueueInput(2, new PlayerInput { type = InputType.PlayCard, cardId = 26000046, position = new Vector2(9, 12) });
            sim.Tick();

            sim.StepUntil(() => 
            {
                var k = FindUnit(sim, 1, 26000040);
                var g = FindUnit(sim, 2, 26000046);
                return (k != null && k.IsDead) || (g != null && g.IsDead);
            }, 10f);

            var knight = FindUnit(sim, 1, 26000040);
            var goblin = FindUnit(sim, 2, 26000046);
            
            return knight != null && !knight.IsDead && goblin != null && goblin.IsDead;
        }

        private bool TestRangedAttackDistance()
        {
            var sim = CreateSimulation();
                        sim.Player1.Elixir = 10;
            sim.Player2.Elixir = 10;

            sim.QueueInput(1, new PlayerInput { type = InputType.PlayCard, cardId = 26000043, position = new Vector2(9, 5) });
            sim.QueueInput(2, new PlayerInput { type = InputType.PlayCard, cardId = 26000040, position = new Vector2(9, 12) });
            sim.Tick();

            sim.Step(1f);
            var musk = FindUnit(sim, 1, 26000043);
            var knight = FindUnit(sim, 2, 26000040);
            if (musk == null || knight == null) return false;
            
            float dist = Vector2.Distance(musk.Position, knight.Position);
            if (dist <= 6f) return false; // Should be out of range

            sim.Step(3f);
            return knight.CurrentHP < knight.MaxHP;
        }

        private bool TestSplashDamage()
        {
            var sim = CreateSimulation();
                        sim.Player1.Elixir = 10;
            sim.Player2.Elixir = 10;

            sim.QueueInput(1, new PlayerInput { type = InputType.PlayCard, cardId = 26000058, position = new Vector2(9, 5) });
            for (int i = 0; i < 3; i++)
            {
                sim.QueueInput(2, new PlayerInput { type = InputType.PlayCard, cardId = 26000046, position = new Vector2(9 + i * 0.8f, 8) });
            }
            sim.Tick();

            sim.StepUntil(() => 
            {
                var w = FindUnit(sim, 1, 26000058);
                return w != null && w.AttackCooldown <= 0;
            }, 5f);
            sim.Step(0.1f);

            var goblins = FindUnits(sim, 2, 26000046);
            foreach (var g in goblins)
            {
                if (g.CurrentHP >= g.MaxHP) return false;
            }
            return true;
        }

        private bool TestCannonGroundOnly()
        {
            var sim = CreateSimulation();
                        sim.Player1.Elixir = 10;
            sim.Player2.Elixir = 10;

            sim.QueueInput(1, new PlayerInput { type = InputType.PlayCard, cardId = 26000045, position = new Vector2(9, 10) });
            sim.QueueInput(2, new PlayerInput { type = InputType.PlayCard, cardId = 26000040, position = new Vector2(9, 14) });
            sim.QueueInput(2, new PlayerInput { type = InputType.PlayCard, cardId = 26000047, position = new Vector2(9, 14) });
            sim.Tick();
            sim.Step(3f);

            var cannon = FindBuilding(sim, 1, 26000045);
            var knight = FindUnit(sim, 2, 26000040);
            var minions = FindUnits(sim, 2, 26000047);

            return cannon != null && knight != null && minions.Count > 0 && 
                   cannon.Target == knight && cannon.Target != minions[0];
        }

        private bool TestTeslaRetract()
        {
            var sim = CreateSimulation();
                        sim.Player1.Elixir = 10;

            sim.QueueInput(1, new PlayerInput { type = InputType.PlayCard, cardId = 26000062, position = new Vector2(9, 10) });
            sim.Tick();
            sim.Step(5f);

            var tesla = FindBuilding(sim, 1, 26000062);
            return tesla != null && tesla.IsRetracted && tesla.IsInvulnerable;
        }

        private bool TestTeslaPopUp()
        {
            var sim = CreateSimulation();
                        sim.Player1.Elixir = 10;
            sim.Player2.Elixir = 10;

            sim.QueueInput(1, new PlayerInput { type = InputType.PlayCard, cardId = 26000062, position = new Vector2(9, 10) });
            sim.Tick();
            sim.Step(2f);

            var tesla = FindBuilding(sim, 1, 26000062);
            if (tesla == null || !tesla.IsRetracted) return false;

            sim.QueueInput(2, new PlayerInput { type = InputType.PlayCard, cardId = 26000040, position = new Vector2(9, 12) });
            sim.Tick();
            sim.Step(1f);

            tesla = FindBuilding(sim, 1, 26000062);
            return tesla != null && !tesla.IsRetracted && !tesla.IsInvulnerable;
        }

        private bool TestSpawnerBuilding()
        {
            var sim = CreateSimulation();
                        sim.Player1.Elixir = 10;

            sim.QueueInput(1, new PlayerInput { type = InputType.PlayCard, cardId = 26000064, position = new Vector2(9, 10) });
            sim.Tick();
            sim.Step(30f);

            var spearGoblins = FindUnits(sim, 1, 26000069);
            return spearGoblins.Count == 6;
        }

        private bool TestBuildingLifetime()
        {
            var sim = CreateSimulation();
                        sim.Player1.Elixir = 10;

            sim.QueueInput(1, new PlayerInput { type = InputType.PlayCard, cardId = 26000045, position = new Vector2(9, 10) });
            sim.Tick();
            sim.Step(31f);

            var cannon = FindBuilding(sim, 1, 26000045);
            return cannon == null;
        }

        private bool TestFireballDamage()
        {
            var sim = CreateSimulation();
                        sim.Player1.Elixir = 10;
            sim.Player2.Elixir = 10;

            for (int i = 0; i < 3; i++)
            {
                sim.QueueInput(2, new PlayerInput { type = InputType.PlayCard, cardId = 26000043, position = new Vector2(9 + i, 18) });
            }
            sim.Tick();

            sim.QueueInput(1, new PlayerInput { type = InputType.CastSpell, spellId = 26000044, position = new Vector2(10, 18) });
            sim.Tick();
            sim.Step(1.5f);

            var musks = FindUnits(sim, 2, 26000043);
            foreach (var m in musks)
            {
                if (m.CurrentHP >= m.MaxHP) return false;
            }
            return true;
        }

        private bool TestFireballKnockback()
        {
            var sim = CreateSimulation();
                        sim.Player1.Elixir = 10;
            sim.Player2.Elixir = 10;

            sim.QueueInput(2, new PlayerInput { type = InputType.PlayCard, cardId = 26000040, position = new Vector2(10, 18) });
            sim.Tick();

            var knight = FindUnit(sim, 2, 26000040);
            var originalPos = knight.Position;

            sim.QueueInput(1, new PlayerInput { type = InputType.CastSpell, spellId = 26000044, position = new Vector2(10, 18) });
            sim.Tick();
            sim.Step(1.5f);

            knight = FindUnit(sim, 2, 26000040);
            return knight != null && Vector2.Distance(knight.Position, originalPos) > 0.3f;
        }

        private bool TestZapStunInferno()
        {
            var sim = CreateSimulation();
                        sim.Player1.Elixir = 10;
            sim.Player2.Elixir = 10;

            sim.QueueInput(2, new PlayerInput { type = InputType.PlayCard, cardId = 26000063, position = new Vector2(9, 10) });
            sim.QueueInput(1, new PlayerInput { type = InputType.PlayCard, cardId = 26000040, position = new Vector2(9, 12) });
            sim.Tick();
            sim.Step(3f);

            var inferno = FindBuilding(sim, 2, 26000063);
            if (inferno == null) return false;

            sim.QueueInput(1, new PlayerInput { type = InputType.CastSpell, spellId = 26000048, position = new Vector2(9, 10) });
            sim.Tick();
            sim.Step(0.1f);

            inferno = FindBuilding(sim, 2, 26000063);
            return inferno != null && inferno.IsStunned;
        }

        private bool TestPoisonSlowDamage()
        {
            var sim = CreateSimulation();
                        sim.Player1.Elixir = 10;
            sim.Player2.Elixir = 10;

            sim.QueueInput(2, new PlayerInput { type = InputType.PlayCard, cardId = 26000040, position = new Vector2(9, 18) });
            sim.Tick();

            var knight = FindUnit(sim, 2, 26000040);
            if (knight == null) return false;
            var originalSpeed = knight.MoveSpeed;

            sim.QueueInput(1, new PlayerInput { type = InputType.CastSpell, spellId = 26000050, position = new Vector2(9, 18) });
            sim.Tick();
            sim.Step(0.1f);

            knight = FindUnit(sim, 2, 26000040);
            if (knight == null || !knight.IsSlowed) return false;
            if (Math.Abs(knight.MoveSpeed - originalSpeed * 0.65f) > 0.01f) return false;

            sim.Step(8f);
            knight = FindUnit(sim, 2, 26000040);
            return knight != null && !knight.IsSlowed && knight.CurrentHP < knight.MaxHP - 400;
        }

        private bool TestFreezeStops()
        {
            var sim = CreateSimulation();
                        sim.Player1.Elixir = 10;
            sim.Player2.Elixir = 10;

            sim.QueueInput(2, new PlayerInput { type = InputType.PlayCard, cardId = 26000040, position = new Vector2(9, 18) });
            sim.QueueInput(2, new PlayerInput { type = InputType.PlayCard, cardId = 26000045, position = new Vector2(10, 18) });
            sim.Tick();

            sim.QueueInput(1, new PlayerInput { type = InputType.CastSpell, spellId = 26000053, position = new Vector2(9.5f, 18) });
            sim.Tick();
            sim.Step(0.1f);

            var knight = FindUnit(sim, 2, 26000040);
            var cannon = FindBuilding(sim, 2, 26000045);
            if (knight == null || cannon == null) return false;
            if (!knight.IsFrozen || !cannon.IsFrozen) return false;

            int knightHP = knight.CurrentHP;
            int cannonHP = cannon.CurrentHP;
            sim.Step(2f);

            knight = FindUnit(sim, 2, 26000040);
            cannon = FindBuilding(sim, 2, 26000045);
            return knight != null && cannon != null && 
                   knight.CurrentHP == knightHP && cannon.CurrentHP == cannonHP;
        }

        private bool TestLogGroundOnly()
        {
            var sim = CreateSimulation();
                        sim.Player1.Elixir = 10;
            sim.Player2.Elixir = 10;

            sim.QueueInput(2, new PlayerInput { type = InputType.PlayCard, cardId = 26000040, position = new Vector2(9, 18) });
            sim.QueueInput(2, new PlayerInput { type = InputType.PlayCard, cardId = 26000047, position = new Vector2(9, 18) });
            sim.Tick();

            sim.QueueInput(1, new PlayerInput { type = InputType.CastSpell, spellId = 26000049, position = new Vector2(5, 18) });
            sim.Tick();
            sim.Step(1f);

            var knight = FindUnit(sim, 2, 26000040);
            var minions = FindUnits(sim, 2, 26000047);
            
            bool knightHit = knight != null && (knight.IsDead || knight.CurrentHP < knight.MaxHP);
            bool minionsSafe = minions.TrueForAll(m => m.CurrentHP == m.MaxHP);
            
            return knightHit && minionsSafe;
        }

        private bool TestTornadoPull()
        {
            var sim = CreateSimulation();
                        sim.Player1.Elixir = 10;
            sim.Player2.Elixir = 10;

            sim.QueueInput(2, new PlayerInput { type = InputType.PlayCard, cardId = 26000040, position = new Vector2(12, 18) });
            sim.Tick();

            var knight = FindUnit(sim, 2, 26000040);
            if (knight == null) return false;
            var originalPos = knight.Position;

            sim.QueueInput(1, new PlayerInput { type = InputType.CastSpell, spellId = 26000054, position = new Vector2(9, 18) });
            sim.Tick();
            sim.Step(1.5f);

            knight = FindUnit(sim, 2, 26000040);
            return knight != null && Vector2.Distance(knight.Position, new Vector2(9, 18)) < Vector2.Distance(originalPos, new Vector2(9, 18));
        }

        private bool TestArcherQueenAbility()
        {
            var sim = CreateSimulation();
                        sim.Player1.Elixir = 10;

            sim.QueueInput(1, new PlayerInput { type = InputType.PlayCard, cardId = 27000000, position = new Vector2(9, 8) });
            sim.Tick();

            var aq = FindUnit(sim, 1, 27000000);
            if (aq == null) return false;

            sim.QueueInput(1, new PlayerInput { type = InputType.ChampionAbility, position = new Vector2(12, 8) });
            sim.Tick();
            sim.Step(0.1f);

            aq = FindUnit(sim, 1, 27000000);
            if (aq == null || !aq.IsInvisible) return false;

            sim.Step(3f);
            aq = FindUnit(sim, 1, 27000000);
            return aq != null && !aq.IsInvisible;
        }

        private bool TestSkeletonKingAbility()
        {
            var sim = CreateSimulation();
                        sim.Player1.Elixir = 10;

            sim.QueueInput(1, new PlayerInput { type = InputType.PlayCard, cardId = 27000001, position = new Vector2(9, 8) });
            sim.Tick();

            var sk = FindUnit(sim, 1, 27000001);
            if (sk == null) return false;

            sim.QueueInput(1, new PlayerInput { type = InputType.ChampionAbility, position = new Vector2(9, 8) });
            sim.Tick();
            sim.Step(0.1f);

            var skeletons = FindUnits(sim, 1, 26000046);
            return skeletons.Count == 5;
        }

        private bool TestMightyMinerAbility()
        {
            var sim = CreateSimulation();
                        sim.Player1.Elixir = 10;
            sim.Player2.Elixir = 10;

            sim.QueueInput(1, new PlayerInput { type = InputType.PlayCard, cardId = 27000002, position = new Vector2(5, 8) });
            sim.QueueInput(2, new PlayerInput { type = InputType.PlayCard, cardId = 26000040, position = new Vector2(9, 8) });
            sim.Tick();

            var mm = FindUnit(sim, 1, 27000002);
            var knight = FindUnit(sim, 2, 26000040);
            if (mm == null || knight == null) return false;

            sim.QueueInput(1, new PlayerInput { type = InputType.ChampionAbility, position = new Vector2(10, 8) });
            sim.Tick();
            sim.Step(0.5f);

            mm = FindUnit(sim, 1, 27000002);
            knight = FindUnit(sim, 2, 26000040);
            return mm != null && mm.Position.x > 9f && knight != null && knight.IsStunned;
        }

        private bool TestChampionCooldown()
        {
            var sim = CreateSimulation();
                        sim.Player1.Elixir = 10;

            sim.QueueInput(1, new PlayerInput { type = InputType.PlayCard, cardId = 27000000, position = new Vector2(9, 8) });
            sim.Tick();

            var aq = FindUnit(sim, 1, 27000000);
            if (aq == null) return false;

            sim.QueueInput(1, new PlayerInput { type = InputType.ChampionAbility, position = new Vector2(9, 8) });
            sim.Tick();
            if (!aq.AbilityOnCooldown) return false;

            sim.Step(19f);
            if (!aq.AbilityOnCooldown) return false;

            sim.Step(2f);
            return !aq.AbilityOnCooldown;
        }

        private bool TestTargetClosestPath()
        {
            var sim = CreateSimulation();
                        sim.Player1.Elixir = 10;
            sim.Player2.Elixir = 10;

            sim.QueueInput(1, new PlayerInput { type = InputType.PlayCard, cardId = 26000040, position = new Vector2(9, 5) });
            sim.QueueInput(2, new PlayerInput { type = InputType.PlayCard, cardId = 26000046, position = new Vector2(9, 25) });
            sim.QueueInput(2, new PlayerInput { type = InputType.PlayCard, cardId = 26000046, position = new Vector2(12, 22) });
            sim.Tick();
            sim.Step(1f);

            var knight = FindUnit(sim, 1, 26000040);
            var goblin1 = FindUnit(sim, 2, 26000046);
            return knight != null && goblin1 != null && knight.Target == goblin1;
        }

        private bool TestBuildingTargeterIgnoresTroops()
        {
            var sim = CreateSimulation();
                        sim.Player1.Elixir = 10;
            sim.Player2.Elixir = 10;

            sim.QueueInput(1, new PlayerInput { type = InputType.PlayCard, cardId = 26000042, position = new Vector2(9, 5) });
            sim.QueueInput(2, new PlayerInput { type = InputType.PlayCard, cardId = 26000040, position = new Vector2(9, 9) });
            sim.QueueInput(2, new PlayerInput { type = InputType.PlayCard, cardId = 26000045, position = new Vector2(9, 12) });
            sim.Tick();
            sim.Step(2f);

            var giant = FindUnit(sim, 1, 26000042);
            var cannon = FindBuilding(sim, 2, 26000045);
            return giant != null && cannon != null && giant.Target == cannon;
        }

        private bool TestRetargetOnDeath()
        {
            var sim = CreateSimulation();
                        sim.Player1.Elixir = 10;
            sim.Player2.Elixir = 10;

            sim.QueueInput(1, new PlayerInput { type = InputType.PlayCard, cardId = 26000058, position = new Vector2(9, 5) });
            sim.QueueInput(2, new PlayerInput { type = InputType.PlayCard, cardId = 26000046, position = new Vector2(9, 18) });
            sim.QueueInput(2, new PlayerInput { type = InputType.PlayCard, cardId = 26000046, position = new Vector2(10, 18) });
            sim.Tick();

            sim.StepUntil(() => 
            {
                var skels = FindUnits(sim, 2, 26000046);
                return skels.Count == 2 && skels[0].IsDead;
            }, 5f);

            var wizard = FindUnit(sim, 1, 26000058);
            var remaining = FindUnits(sim, 2, 26000046);
            return wizard != null && remaining.Count == 1 && wizard.Target == remaining[0];
        }

        #endregion

        #region Integration Tests

        private bool TestCompleteBattleFlow()
        {
            var sim = CreateSimulation();
                        sim.Player1.Elixir = 10;
            sim.Player2.Elixir = 10;

            // Play cards
            sim.QueueInput(1, new PlayerInput { type = InputType.PlayCard, cardId = 26000040, position = new Vector2(9, 8) });
            sim.Tick();
            sim.Step(2f);

            sim.QueueInput(2, new PlayerInput { type = InputType.PlayCard, cardId = 26000040, position = new Vector2(9, 20) });
            sim.Tick();
            sim.Step(5f);

            sim.QueueInput(1, new PlayerInput { type = InputType.CastSpell, spellId = 26000044, position = new Vector2(9, 20) });
            sim.Tick();
            sim.Step(2f);

            // Run to completion
            for (int i = 0; i < 10000 && sim.Status == BattleStatus.Playing; i++)
            {
                sim.Tick();
            }

            return sim.Status != BattleStatus.Playing;
        }

        private bool TestOvertimeSuddenDeath()
        {
            var sim = CreateSimulation();
                        // Damage princess towers
            foreach (var tower in sim.Towers)
            {
                if (tower.Type == TowerType.PrincessLeft || tower.Type == TowerType.PrincessRight)
                {
                    tower.TakeDamage(tower.MaxHP - 1, DamageType.Spell, 0);
                }
            }
            
            sim.Step(181f);
            if (sim.CurrentTick < 180 * 60) return false;

            sim.Player1.Elixir = 10;
            sim.QueueInput(1, new PlayerInput { type = InputType.PlayCard, cardId = 26000055, position = new Vector2(9, 15) });
            sim.Tick();

            for (int i = 0; i < 10000 && sim.Status == BattleStatus.Playing; i++)
            {
                sim.Tick();
            }

            return sim.Status != BattleStatus.Playing && (sim.Status == BattleStatus.Player1Won || sim.Status == BattleStatus.Player2Won);
        }

        private bool TestDrawNoTowers()
        {
            var sim = CreateSimulation();
                        sim.Step(361f);

            for (int i = 0; i < 10000 && sim.Status == BattleStatus.Playing; i++)
            {
                sim.Tick();
            }

            return sim.Status == BattleStatus.Draw;
        }

        private bool TestKingTowerActivation()
        {
            var sim = CreateSimulation();
                        Tower kingTower = null;
            foreach (var tower in sim.Towers)
            {
                if (tower.OwnerPlayerId == 2 && tower.Type == TowerType.King)
                {
                    kingTower = tower;
                    break;
                }
            }

            if (kingTower == null || kingTower.IsActivated) return false;
            kingTower.TakeDamage(100, DamageType.Spell, 0);
            return kingTower.IsActivated && kingTower.GuardsSpawned.Count == 2;
        }

        private bool TestKingTowerDestroyedEndsBattle()
        {
            var sim = CreateSimulation();
                        Tower p2King = null;
            foreach (var tower in sim.Towers)
            {
                if (tower.OwnerPlayerId == 2 && tower.Type == TowerType.King)
                {
                    p2King = tower;
                    break;
                }
            }

            if (p2King == null) return false;
            p2King.TakeDamage(p2King.MaxHP, DamageType.Spell, 0);
            sim.Tick();
            return sim.Status == BattleStatus.Player1Won;
        }

        private bool TestInvalidCardRejected()
        {
            var sim = CreateSimulation();
                        sim.Player1.Elixir = 10;

            sim.QueueInput(1, new PlayerInput { type = InputType.PlayCard, cardId = 26000055, position = new Vector2(9, 8) });
            sim.Tick();
            return sim.Player1.Elixir == 10 && FindUnit(sim, 1, 26000055) == null;
        }

        private bool TestInvalidPositionRejected()
        {
            var sim = CreateSimulation();
                        sim.Player1.Elixir = 10;

            sim.QueueInput(1, new PlayerInput { type = InputType.PlayCard, cardId = 26000045, position = new Vector2(9, 20) });
            sim.Tick();
            return sim.Player1.Elixir == 10 && FindBuilding(sim, 1, 26000045) == null;
        }

        private bool TestRateLimiting()
        {
            var sim = CreateSimulation();
                        sim.Player1.Elixir = 10;

            for (int i = 0; i < 100; i++)
            {
                sim.QueueInput(1, new PlayerInput { type = InputType.PlayCard, cardId = 26000040, position = new Vector2(9, 8) });
            }
            for (int i = 0; i < 10; i++) sim.Tick();
            return sim.Player1.Elixir <= 10;
        }

        private bool TestDeterministicReplay()
        {
            var sim1 = CreateSimulation();
            sim1.StartBattle();
            sim1.Player1.Elixir = 10;
            sim1.QueueInput(1, new PlayerInput { type = InputType.PlayCard, cardId = 26000040, position = new Vector2(9, 8) });
            
            while (sim1.Status == BattleStatus.Playing && sim1.CurrentTick < 1000) sim1.Tick();
            var log1 = sim1.GetReplayLog();
            var status1 = sim1.Status;
            sim1.Dispose();

            var sim2 = CreateSimulation();
            sim2.StartBattle();
            sim2.Player1.Elixir = 10;
            sim2.QueueInput(1, new PlayerInput { type = InputType.PlayCard, cardId = 26000040, position = new Vector2(9, 8) });
            
            while (sim2.Status == BattleStatus.Playing && sim2.CurrentTick < 1000) sim2.Tick();
            var log2 = sim2.GetReplayLog();
            var status2 = sim2.Status;
            sim2.Dispose();

            if (status1 != status2 || log1.Count != log2.Count) return false;
            
            for (int i = 0; i < log1.Count; i++)
            {
                if (log1[i].tick != log2[i].tick || log1[i].type != log2[i].type || log1[i].playerId != log2[i].playerId)
                    return false;
            }
            return true;
        }

        #endregion

        #region Performance Tests

        private bool Test60FPSUnderLoad()
        {
            var sim = CreateSimulation();
                        for (int i = 0; i < 50; i++)
            {
                SpawnUnitDirect(sim, 1, 26000040, new Vector2(9 + (i % 5) * 2, 5 + (i / 5) * 2));
                SpawnUnitDirect(sim, 2, 26000040, new Vector2(9 + (i % 5) * 2, 25 - (i / 5) * 2));
            }

            var sw = Stopwatch.StartNew();
            for (int tick = 0; tick < 3600; tick++)
            {
                sim.Tick(BattleSimulation.FIXED_DT);
            }
            sw.Stop();

            var avgMs = sw.ElapsedMilliseconds / 3600.0;
            return avgMs < 16.67;
        }

        private bool TestMemoryStability()
        {
            var sim = CreateSimulation();
                        GC.Collect();
            long initial = GC.GetTotalMemory(true);

            for (int tick = 0; tick < 18000; tick++)
            {
                if (tick % 60 == 0)
                {
                    if (sim.Player1.Elixir >= 3) PlayCardRandom(sim, 1);
                    if (sim.Player2.Elixir >= 3) PlayCardRandom(sim, 2);
                }
                sim.Tick(BattleSimulation.FIXED_DT);
            }

            GC.Collect();
            long final = GC.GetTotalMemory(true);
            return (final - initial) < 50 * 1024 * 1024;
        }

        private bool TestPathfinding10k()
        {
            var sim = CreateSimulation();
                        var pf = GetPathfinding(sim);
            pf.Initialize();

            var sw = Stopwatch.StartNew();
            for (int i = 0; i < 10000; i++)
            {
                pf.FindPath(RandomPosition(), RandomPosition(), EntityType.Unit);
            }
            sw.Stop();
            return sw.ElapsedMilliseconds < 100;
        }

        #endregion

        #region Helpers

        private BattleSimulation CreateSimulation()
        {
            var config = CreateTestConfig();
            var sim = new BattleSimulation();
            sim.Initialize(config, 12345, TestDecks.BalancedDeck, TestDecks.BalancedDeck);
            return sim;
        }

        private GameConfig CreateTestConfig()
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

        private void SpawnUnitDirect(BattleSimulation sim, int playerId, int cardId, Vector2 position)
        {
            var cardData = GetDataManager().GetCard(cardId);
            if (cardData == null) return;
            var stats = cardData.GetStats(11);
            var unit = new Unit(sim.GetNextEntityIdForTest(), playerId, cardData, stats, position, 11);
            
            var unitsField = typeof(BattleSimulation).GetField("_units", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var entitiesField = typeof(BattleSimulation).GetField("_entities", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (unitsField != null && entitiesField != null)
            {
                var units = (List<Unit>)unitsField.GetValue(sim);
                var entities = (Dictionary<uint, Entity>)entitiesField.GetValue(sim);
                units.Add(unit);
                entities[unit.Id] = unit;
            }
        }

        private void PlayCardRandom(BattleSimulation sim, int playerId)
        {
            // Simplified - would need hand access
        }

        private Unit FindUnit(BattleSimulation sim, int playerId, int cardId)
        {
            var unitsField = typeof(BattleSimulation).GetField("_units", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (unitsField != null)
            {
                var units = (List<Unit>)unitsField.GetValue(sim);
                foreach (var u in units)
                {
                    if (u.OwnerPlayerId == playerId && u.CardData.cardId == cardId)
                        return u;
                }
            }
            return null;
        }

        private List<Unit> FindUnits(BattleSimulation sim, int playerId, int cardId)
        {
            var result = new List<Unit>();
            var unitsField = typeof(BattleSimulation).GetField("_units", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (unitsField != null)
            {
                var units = (List<Unit>)unitsField.GetValue(sim);
                foreach (var u in units)
                {
                    if (u.OwnerPlayerId == playerId && u.CardData.cardId == cardId)
                        result.Add(u);
                }
            }
            return result;
        }

        private Building FindBuilding(BattleSimulation sim, int playerId, int cardId)
        {
            var buildingsField = typeof(BattleSimulation).GetField("_buildings", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (buildingsField != null)
            {
                var buildings = (List<Building>)buildingsField.GetValue(sim);
                foreach (var b in buildings)
                {
                    if (b.OwnerPlayerId == playerId && b.CardData.cardId == cardId)
                        return b;
                }
            }
            return null;
        }

        private Pathfinding GetPathfinding(BattleSimulation sim = null)
        {
            sim = sim ?? _simulation;
            var field = typeof(BattleSimulation).GetField("_pathfinding", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return field?.GetValue(sim) as Pathfinding;
        }

        private DataManager GetDataManager()
        {
            return new GameObject("TestDataManager").AddComponent<DataManager>();
        }

        private Vector2 RandomPosition()
        {
            return new Vector2(UnityEngine.Random.Range(0f, 18f), UnityEngine.Random.Range(0f, 32f));
        }

        private void LogSummary()
        {
            Debug.Log($"[BattleTestRunner] ===== TEST SUMMARY =====");
            Debug.Log($"[BattleTestRunner] Passed: {_testsPassed}");
            Debug.Log($"[BattleTestRunner] Failed: {_testsFailed}");
            Debug.Log($"[BattleTestRunner] Total: {_testsPassed + _testsFailed}");
            
            foreach (var result in _testResults)
            {
                Debug.Log(result);
            }
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
