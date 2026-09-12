# Bug Reports (Agent 6 — Testing, CI/CD & DevOps)

> Per mission: implementation bugs found during test development are filed here
> with `bug` label equivalents. Agent 6 does not modify implementation code.

## BUG-001 [bug] [agent1] [agent2] BattleSimulation references non-existent `NetworkClient.PlayerInput` / `NetworkClient.InputType`
- **File:** `Assets/Scripts/Battle/Simulation/BattleSimulation.cs:41-42, 181, 187, 194, 197, 200, 934`
- **Symptom:** Unity project does not compile. `NetworkClient` (Assets/Scripts/Network/NetworkClient.cs)
  is a MonoBehaviour with no nested `PlayerInput` / `InputType` / `GameStateMessage` types.
- **Actual types:** `CRClone.Network.PlayerInput`, `CRClone.Network.InputType`
  (`Assets/Scripts/Network/MessageTypes.cs:63,157`), `CRClone.Network.GameStateMessage` (:176).
- **Expected:** `Queue<PlayerInput>`, `QueueInput(int, PlayerInput)`,
  `ApplyInput(int, PlayerInput)`, `Reconcile(GameStateMessage)`.
- **Impact:** Blocks ALL Unity simulation unit/integration tests (Agent 6 suite).
- **Found by:** Agent 6 test compilation pass.

## BUG-002 [bug] [agent1] BattleSimulation uses `InputType.UseChampionAbility` (does not exist)
- **File:** `Assets/Scripts/Battle/Simulation/BattleSimulation.cs:200`
- **Actual enum:** `CRClone.Network.InputType { PlayCard, CastSpell, ChampionAbility, Emote }`
  (`Assets/Scripts/Network/MessageTypes.cs:63-70`).
- **Expected:** `case InputType.ChampionAbility:` (or `Network.InputType.ChampionAbility`).
- **Impact:** Compile error; champion ability input never handled.

## BUG-003 [bug] [agent1] BattleSimulation.Reconcile references `NetworkClient.GameStateMessage`
- **File:** `Assets/Scripts/Battle/Simulation/BattleSimulation.cs:934`
- **Expected:** `Reconcile(GameStateMessage serverState)` with `using CRClone.Network;`
  (file already imports `CRClone.Network`, so unqualified `GameStateMessage` resolves).

## BUG-004 [bug] [agent2] [agent5] Matchmaker → BattleServer seed overflow (`BigInt(Infinity)` crash)
- **Files:**
  - `server/src/matchmaking/Matchmaker.ts:211` (`generateBattleSeed` returns `bigint`)
  - `server/src/matchmaking/Matchmaker.ts:216` (`Number(seed)` → `Infinity` for large 64-bit values)
  - `server/src/battle/BattleServer.ts:63` (`BigInt(seed)` throws `RangeError` on `Infinity`)
- **Symptom:** Any auto-matched battle crashes the server process:
  `RangeError: The number Infinity cannot be converted to a BigInt`.
- **Repro:** Add two close-trophy players to `Matchmaker` queue (see
  `server/tests/unit/matchmaking.test.ts`); `tryMatchSolo` → `createBattle` throws.
- **Suggested fix (Agent 2/5):** Keep seed as `bigint` end-to-end OR derive a 32-bit seed
  (e.g. `Number(seed & 0xffffffffn)` / `Number(seed % 1000000007n)`) before `Number()` conversion.
  Do NOT round-trip `bigint → Number → BigInt`.
- **Agent 6 workaround:** `server/tests/**/matchmaking*.test.ts` and `battleFlow*.test.ts`
  mock `../../src/battle/BattleServer` so queue logic remains testable until fixed.

## BUG-005 [bug] [agent1] TEST_SPEC API drift: `BattleSimulation.StartBattle()` does not exist
- **Reference:** `docs/planning/phase1/TEST_SPEC.md` uses `sim.StartBattle()`.
- **Reality:** `BattleSimulation.Initialize(...)` sets `Status = Playing` directly; no
  `StartBattle()` method exists.
- **Agent 6 resolution:** Tests call `Initialize(...)` only. Recommend updating TEST_SPEC
  or adding a `StartBattle()` alias in simulation (Agent 1 decision).

## Notes
- Unity simulation member coverage (IsRetracted, AbilityCooldown, Target, GuardsSpawned, etc.)
  used in Agent 6 tests follows `Assets/Scripts/Battle/Simulation/*.cs` as read on 2026-09-12.
  If Agent 1 renames members, tests will need mechanical updates.
- Server `PlayerService`, `ClanService`, `ShopService`, `QuestService`, `SeasonService`,
  `TournamentService`, `ReplayService` APIs used in tests were read from
  `server/src/services/*.ts` on 2026-09-12. Tests mock repository layers and assert
  validation/error paths (no live DB required).
