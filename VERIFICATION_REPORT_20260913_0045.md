# Verification Report - 2026-09-13 - Cycle #2 (re-verification)

> Method: re-read actual code on `master` HEAD (`71cbde2`). Fix commits since Cycle #1 (`1806d03`):
> `e3c65c5` (122 CardData assets), `59bbfae` (IAP fail-closed), `df07d65`+`a284a4c` (Agent 2 fixes),
> `7e68609` (121 prefabs), `71cbde2` (manifest). No claims trusted; every verdict cites file+line+quote.

## Scoreboard
| Agent | Cycle #1 | Cycle #2 | Status |
|-------|----------|----------|--------|
| Agent 1 (Battle Simulation) | 14/16 | 14/16 | ⚠️ PARTIAL — both gaps STILL OPEN, no commits touched `Simulation/` |
| Agent 2 (Networking) | 7/11 | 11/11 | ✅ DONE — all 5 gaps FIXED |
| Agent 3 (UI/UX) | 8/8 | 8/8 | ✅ DONE — untouched, intact |
| Agent 4 (Assets) | 4/7 | 7/7 | ✅ DONE — all 3 gaps FIXED (1× P2 follow-up note) |
| Agent 5 (Backend) | 5/6 | 6/6 | ✅ DONE — P0 stub FIXED |
| Agent 6 (Testing) | 5/5 | 5/5 | ✅ DONE — test files now committed to master |

**Totals: 51/53 done. 2 scoring gaps remain (both Agent 1) + 2× P2 follow-up notes.**

### Fixes confirmed this cycle (evidence on file)
- **GAP-2.3 FIXED:** `protocol.proto:65-323` now defines explicit `BattleStartMessage`, `SaveDeckRequest/DeckSavedMessage`, `Clan/Shop/Quest/Season/Tournament/Replay/Player Message + *ResponseMessage` pairs with field numbers matching C# `[ProtoMember(N)]` (`MessageTypes.cs:284-414`); `Protocol.ts:36-126` type union + `TYPE_TO_PROTO` map; new contract test `server/tests/unit/protocol-contract.test.ts:55-249` asserts 3-side parity + round-trips.
- **GAP-2.6b FIXED:** hardcoded `?30:-30` gone (grep 0 hits in `server/src/battle/`); `BattleServer.ts:576-608,656` delegates to `ratingSystem.calculateTrophyChangeFromResult()` + `calculateELOChange()`; new `server/src/battle/CardDatabase.ts:1-75` (`getCardElixirCost`, 75 lines); `battleServer.test.ts:42-84` asserts underdog >30, favorite <30.
- **GAP-2.9 FIXED:** `MessageHandler.ts:138-174` rejects unauthenticated / missing `battleId` / forged-battleId (`!battle.hasPlayer(...)`) before touching state; `BattleServer.ts:290-316` uses real `getCardElixirCost(cardId)` (unknown cardId = reject); `battleFlow.test.ts:60-183` covers unowned/unknown/insufficient-elixir/rate-limit/forged/non-participant/unauthenticated.
- **GAP-2.10 FIXED:** `acknowledgedTick` updated at `BattleServer.ts:239,253`; `tick()` calls `processAcknowledgments()` (`:144-156,262-288`) which invokes `acknowledgeInput()` + `retryUnacknowledgedInputs(1000)`; `battleServer.test.ts:87-165` covers resend-until-ack and retry-exhaustion.
- **GAP-2.11 FIXED:** `NetworkClient.cs:284` TODO gone (grep TODO = 0); `ApplyAuthoritativeState` + `Reconciler.ApplySnapshot` (`:328-386`) applies positions/HP/elixir/tick, prunes acked inputs, requeues unacked (`:440-457`); `NetworkTests.cs:161-249` asserts snapshot convergence (pos/HP/elixir) + hash parity.
- **GAP-4.2 FIXED:** `Assets/Resources/Data/Cards/` holds exactly **122 `.asset`** (+122 `.meta`, 0 `.gitkeep`), `Card_001_The_Log`…`Card_122_Mighty_Miner`, spot-reads non-default.
- **GAP-4.4 FIXED:** **51 Units + 19 Buildings + 23 Spells + 26 Projectiles** (+2 Towers) real `.prefab`; `Unit_Knight.prefab` spot-check shows full component set.
- **GAP-4.6 FIXED:** `Assets/AssetManifest.csv` exists, **249 data rows** (122 cards + 121 prefabs + 6 shaders), 3/3 sample rows resolve to disk; counts reconcile 1:1.
- **GAP-5.4 FIXED:** `verifyReceipt` (`ShopService.ts:142-158`) fail-closed; `stubVerifyReceipt` (`:166-169`) returns false in prod + warn; real Apple (`:188-230`, secret, sandbox/prod URLs, `status===0` + txn match) and Google (`:238-301`, `purchaseState===0` + order/price match); `shopService.test.ts:37-196` asserts forged-rejected everywhere, prod stub never true.
- **No regressions:** previously-DONE items 2.1/2.2/2.4/2.5/2.7/2.8 intact; 5.1/5.2/5.3/5.5/5.6 intact (5 migrations, 0 new stubs); UI dirs untouched.

## Gaps & Corrective Prompts

### Agent 1 — 2 gaps STILL OPEN (14/16)

`git log 1806d03..HEAD -- Assets/Scripts/Battle/Simulation/` = **empty**; grep `footprintRadius|tiebreak` in `Simulation/` = **0 hits**. Code is byte-identical to Cycle #1. Prompts re-issued unchanged:

#### GAP-1.4 [⚠️ PARTIAL] (P1) RE-ISSUED — Deploy validation missing building-footprint/overlap check
- **File:** `Assets/Scripts/Battle/Simulation/BattleSimulation.cs` → `IsValidDeployPosition()` (lines 286-337, `return true;` at 336)
- **Expected (per `docs/planning/phase1/MECHANICS.md`):** deployment on occupied footprints rejected.
- **Found:** unchanged — zone/river checks only, no overlap loop.
- **Corrective prompt for Agent 1:**
> Agent 1 — In `IsValidDeployPosition()` in `Assets/Scripts/Battle/Simulation/BattleSimulation.cs` (lines 286-337, ending `return true;` at line 336): after the existing river/zone checks and before `return true`, add a footprint-overlap rejection. Per `docs/planning/phase1/MECHANICS.md` deploy rules, iterate `sim._buildings` and `sim._towers` (and live `sim._units` for troops) and `return false` if `Vector2.Distance(position, existing.position) < (card.footprintRadius + existing.footprintRadius)` (use `GameConstants` radii already defined for towers/buildings; default 0.5f for troops). Spells stay exempt. Verify with a new `DeployValidationTests.cs` case: deploying a Cannon at an occupied tile returns false, deploying at a free tile returns true.
>
> Workflow: push directly to the `master` branch when done. Other agents are working on the same codebase concurrently — commit and push ONLY your own changed files (use `git add <your files>`, never `git add -A`), and pull/rebase before pushing if master has moved.

#### GAP-1.5 [⚠️ PARTIAL] (P2) RE-ISSUED — Win condition: dead branch, no regulation crown-compare / HP tiebreak
- **File:** `Assets/Scripts/Battle/Simulation/BattleSimulation.cs` → `CheckWinCondition()` (lines 763-842; dead `else if` at 811, instant-draw at 808)
- **Expected (per `docs/planning/phase1/MECHANICS.md`):** regulation-expiry crown win; overtime HP tiebreak before draw.
- **Found:** unchanged.
- **Corrective prompt for Agent 1:**
> Agent 1 — In `CheckWinCondition()` in `Assets/Scripts/Battle/Simulation/BattleSimulation.cs` (lines 786-815): per `docs/planning/phase1/MECHANICS.md` win conditions, (1) add a regulation-time branch — when `_currentTick` first reaches `_config.battleDuration * TICK_RATE` and crowns are unequal, end the battle immediately with the crown leader as winner instead of falling into the sudden-death path at lines 798-804; (2) replace the overtime-timeout instant-draw at line 808 with a lowest-tower-HP tiebreak (sum surviving tower HP per side; higher total wins; draw only if exactly equal); (3) delete or fix the unreachable `else if` at line 811 whose comment claims "Normal time ended" but whose condition can never fire before overtime. Verify with `BattleFlowTests.cs` cases: regulation-expiry crown win, overtime sudden-death win, overtime-timeout HP-tiebreak win, exact-equal draw.
>
> Workflow: push directly to the `master` branch when done. Other agents are working on the same codebase concurrently — commit and push ONLY your own changed files (use `git add <your files>`, never `git add -A`), and pull/rebase before pushing if master has moved.

### P2 follow-up notes (non-scoring, cheap to close)

#### FOLLOW-2.11 (P2) Client requeue path asserted only indirectly
- **File:** `Assets/Tests/Integration/NetworkTests.cs:161-249` calls `Reconciler.ApplySnapshot` directly; `NetworkClient.ApplyAuthoritativeState`/`RequeueUnackedInputs`/`_lastAckedTick` path (NetworkClient.cs:328-457) has no direct assertion.
- **Corrective prompt for Agent 2:**
> Agent 2 — In `Assets/Tests/Integration/NetworkTests.cs`, add a test that drives `NetworkClient.ApplyAuthoritativeState` (currently `Assets/Scripts/Network/NetworkClient.cs:328-386`) instead of calling `Reconciler.ApplySnapshot` directly: queue 3 inputs, deliver an authoritative `GameStateMessage` with `tick` covering the first, assert `_lastAckedTick` advanced, the first input was pruned, and the remaining two were requeued via `RequeueUnackedInputs` (lines 440-457) and re-applied. Per `Agent2_Networking.md` deliverable 2.11.
>
> Workflow: push directly to the `master` branch when done. Other agents are working on the same codebase concurrently — commit and push ONLY your own changed files (use `git add <your files>`, never `git add -A`), and pull/rebase before pushing if master has moved.

#### FOLLOW-4.x (P2) 122 cards vs 121 prefabs — one card without a dedicated prefab
- **Files:** 122 `.asset` vs 51+19+23+26+2 = 121 `.prefab`; manifest reconciles exactly (249 rows), so the delta is generator-intentional, but no record says which card shares/reuses and why.
- **Corrective prompt for Agent 4:**
> Agent 4 — Identify which of the 122 cards in `Assets/Resources/Data/Cards/` has no dedicated prefab in `Assets/Prefabs/` (121 prefabs total: 51 Units + 19 Buildings + 23 Spells + 26 Projectiles + 2 Towers) and confirm the reuse is intentional (e.g. shares another card's prefab or needs none). If intentional, document the mapping in one line per reused card in `Assets/AssetManifest.csv` (or a `PrefabMapping.md` next to it); if it is a generator skip/bug in `Assets/Editor/PrefabGenerator.cs` `GenerateAllPrefabs()`, fix it and commit the missing prefab. Per `Agent4_Assets.md` deliverable 4.4.
>
> Workflow: push directly to the `master` branch when done. Other agents are working on the same codebase concurrently — commit and push ONLY your own changed files (use `git add <your files>`, never `git add -A`), and pull/rebase before pushing if master has moved.

### Agents 2, 3, 4, 5, 6 — no scoring gaps ✅
No corrective prompts. Agents 2/4/5 fixes verified above; Agents 3/6 untouched and intact.

## Re-verification checklist for Cycle #3
- [ ] Agent 1: overlap rejection test + regulation/HP-tiebreak tests pass on master
- [ ] Agent 2: requeue-path assertion test passes
- [ ] Agent 4: prefab mapping documented / missing prefab committed
- [ ] Full suite green: Unity EditMode + `npm run test` + k6 smoke + Playwright critical journeys

## Sign-off
- [x] Every agent scored (x/y format, both cycles)
- [x] Every ❌/⚠️ has exactly one corrective prompt (2 re-issued + 2 P2 follow-ups, no bundling)
- [x] Each prompt contains: exact file + function + line, expected behavior with doc citation, quoted finding, concrete fix
- [x] No implementation code touched (only this report file added)
