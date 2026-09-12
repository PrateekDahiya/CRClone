# CONTEXT.md — Assets/Tests (Agent 6)

## Purpose
Unity EditMode test suite for the deterministic battle simulation.
Specs: `docs/planning/phase1/TEST_SPEC.md`. Architecture: `docs/planning/phase1/ARCHITECTURE.md §10`.

## Structure
- `AssemblyDefinition.asmdef` — `CRClone.Tests` assembly (Editor only, references `CRClone.Core`).
- `TestFixtures/TestDecks.cs` — Balanced / SpellHeavy / Tank / Cycle / Champion / Building decks.
- `TestFixtures/TestScenarios.cs` — Standard1v1, TankVsSwarm, AirVsGround, SpellBait.
- `TestFixtures/TestDecks.cs` — real asset IDs 1-122 (Knight=89, Skeletons=92, ...).
- `TestFixtures/BattleTestBase.cs` — NUnit base: config factory, Services
  registration, `InitializeSimulation`, `Tick` / `Step` / `StepUntil`, elixir
  helpers, `PlayCard`/`CastSpell`/`UseChampionAbility` with elixir top-up,
  `FindUnit(s)`/`FindBuilding`/`FindTower` lookups.
- `TestFixtures/BattleTestRunner.cs` — thin manual in-editor smoke test
  (legacy `Assets/Scripts/Testing/BattleTestRunner.cs` remains untouched).
- `Unit/` — ElixirSystem, CardCycle, UnitCombat, Building, Spell, Champion, Targeting tests.
- `Integration/` — BattleFlow (1v1, overtime, draw, determinism), Network (validation, rate limit, desync, reconcile).
- `Performance/` — SimulationBenchmark (60fps), MemoryStability (<50MB/5min), PathfindingBenchmark (10k<100ms).

## Conventions
- Tests use `CRClone.Network.PlayerInput` / `InputType` via file-local using
  aliases (never `using CRClone.Network;`, which collides with Core enums).
- `TowerType`/`BattleStatus`/`EntityType` resolve to Simulation/Core meanings;
  Core-vs-Network duplication is tracked in BUG_REPORTS.md BUG-007.
- No `StartBattle()` — `Initialize()` puts the sim in `Playing` (BUG-005).
- `BattleTestBase` registers ConfigManager + DataManager in `Services` (the
  sim resolves both at runtime) and tops elixir to 10 per played card so tests
  exercise mechanics, not the economy — except ElixirSystemTests, which drive
  the economy explicitly.
- Valid deploys: P1 y<=13, P2 y>=19, spells anywhere (see IsValidDeployPosition).
- Units spawn at card level 1; combat assertions use base asset stats.
- AGENT6 compat fixes in sim code (Agent 1 to review): input message types,
  Fireball/Rocket arrival damage (BUG-008), elixir regen accumulator (BUG-009).
- Blocked-on-upstream specs: Graveyard/SK spawn counts (BUG-006). Tests assert
  the activatable parts until the card-name lookups are fixed.

## Running
- Unity Editor: Window → General → Test Runner → Run All (EditMode).
- CLI: `Unity -batchmode -runTests -projectPath . -testPlatform EditMode -testResults results.xml`.
- Coverage gate: >80% on simulation code. Perf gate: tick avg <16.67ms, regression <5%.
