# CONTEXT.md — Assets/Tests (Agent 6)

## Purpose
Unity EditMode test suite for the deterministic battle simulation.
Specs: `docs/planning/phase1/TEST_SPEC.md`. Architecture: `docs/planning/phase1/ARCHITECTURE.md §10`.

## Structure
- `AssemblyDefinition.asmdef` — `CRClone.Tests` assembly (Editor only, references `CRClone.Core`).
- `TestFixtures/TestDecks.cs` — Balanced / SpellHeavy / Tank / Cycle / Champion / Building decks.
- `TestFixtures/TestScenarios.cs` — Standard1v1, TankVsSwarm, AirVsGround, SpellBait, Overtime, Draw.
- `TestFixtures/BattleTestBase.cs` — NUnit base: config factory, `InitializeSimulation`,
  `Tick` / `Step` / `StepUntil`, elixir helpers, reflection helper `GetNextEntityIdForTest`.
- `TestFixtures/BattleTestRunner.cs` — MonoBehaviour runner (legacy `Assets/Scripts/Testing/BattleTestRunner.cs` remains untouched).
- `Unit/` — ElixirSystem, CardCycle, UnitCombat, Building, Spell, Champion, Targeting tests.
- `Integration/` — BattleFlow (1v1, overtime, draw, determinism), Network (validation, rate limit, desync, reconcile).
- `Performance/` — SimulationBenchmark (60fps), MemoryStability (<50MB/5min), PathfindingBenchmark (10k<100ms).

## Conventions
- Tests use `CRClone.Network.PlayerInput` / `CRClone.Network.InputType` (see BUG_REPORTS.md BUG-001/002).
- No `StartBattle()` — `Initialize()` puts the sim in `Playing` (BUG-005).
- `BattleSimulation.QueueInput` signature is currently broken upstream (BUG-001); tests assume the fixed signature.
- Unity project does NOT compile until BUG-001–003 are fixed by Agent 1/2. Do not "fix" by editing simulation code here.

## Running
- Unity Editor: Window → General → Test Runner → Run All (EditMode).
- CLI: `Unity -batchmode -runTests -projectPath . -testPlatform EditMode -testResults results.xml`.
- Coverage gate: >80% on simulation code. Perf gate: tick avg <16.67ms, regression <5%.
