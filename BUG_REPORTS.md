# Bug Reports (Agent 6 — Testing, CI/CD & DevOps)

> Implementation bugs found during test development. Per mission, Agent 6 files
> bugs instead of changing game code — with three exceptions below (marked
> FIXED-BY-AGENT6) where the defect made it impossible for ANY test to run at
> all. Those fixes are minimal, commented with `AGENT6-COMPAT` / `AGENT6-FIX`,
> and flagged for owner review.

## FIXED-BY-AGENT6 (needs owner review)

### BUG-001 [bug] [agent1] [agent2] BattleSimulation referenced non-existent `NetworkClient.PlayerInput` / `NetworkClient.InputType` — FIXED
- **File:** `Assets/Scripts/Battle/Simulation/BattleSimulation.cs:41-42, 181, 187, 194, 197, 200, 985`
- `NetworkClient` is a MonoBehaviour with no nested types. Real types:
  `CRClone.Network.PlayerInput`, `CRClone.Network.InputType`
  (`Assets/Scripts/Network/MessageTypes.cs:63,157`), `CRClone.Network.GameStateMessage` (:176).
- **Fix applied:** use the Network message types directly; `uint` card/spell IDs
  cast to the `int` parameters the sim methods take.
- **Agent 1/2:** confirm and keep, or rewire input plumbing properly.
- **Same broken `NetworkClient.X` convention also exists (untouched, for owners)
  in:** `Battle/Input/InputManager.cs:189,201,204` (`PlayerInput`, `InputType`),
  `UI/DeckBuilderUI.cs:411` (`SaveDeckRequest`), `UI/UIManager.cs:274`
  (`MatchmakingRequest`), `UI/Components/ChatMessageUI.cs:112`, `UI/Screens/BattleResultScreen.cs:265,279`
  (`ReplayRequest`, `RematchRequest`), `UI/Screens/ClanScreen.cs:274`
  (`ClanChatMessage`). Each needs the same `CRClone.Network.*` treatment.

### BUG-002 [bug] [agent1] `InputType.UseChampionAbility` did not exist — FIXED
- Real enum value is `InputType.ChampionAbility` (MessageTypes.cs:63-70).
- **Fix applied:** `case InputType.ChampionAbility:`.

### BUG-008 [bug] [agent1] Fireball/Rocket never dealt damage — FIXED
- Traveling damage spells are not instant, and `SpellEffect.Tick` had no
  arrival logic, so `ApplyInstantEffect` never ran for them.
- **Fix applied** (`SpellEffect.cs` Tick): once a non-instant `SpellType.Damage`
  spell's travel time elapses, apply the impact effect once at the target point.
  Only Fireball/Rocket match (exact `== SpellType.Damage` + non-instant);
  instant, DoT, Utility and Spawn spells are unaffected.

### BUG-009 [bug] [agent1] Elixir never regenerated (economy dead) — FIXED
- `UpdateElixir` added one tick's fraction (~0.006) to the integer Elixir and
  floored it, discarding the fraction every tick. Elixir stayed frozen forever.
- **Fix applied:** fractional accumulators per player (`AccumulateElixir`),
  whole elixir credited when a full point accrues; fraction reset at max cap.
  Verified rates: 1 elixir / 2.8s normal, / 1.4s double, / 0.93s triple
  (within fixed-point truncation).

## OPEN (filed for owners — tests assert spec and stay red/gated until fixed)

### BUG-004 [bug] [agent2] [agent5] Matchmaker → BattleServer seed overflow (`BigInt(Infinity)` crash)
- `server/src/matchmaking/Matchmaker.ts:211,216` + `server/src/battle/BattleServer.ts:63`.
- `generateBattleSeed` returns `bigint`; `Number(hugeBigint)` → `Infinity`;
  `BigInt(Infinity)` throws `RangeError`, crashing the server on every auto-match.
- **Suggested fix:** keep the seed as `bigint` end-to-end, or derive a 32-bit seed
  (e.g. `Number(seed & 0xffffffffn)`) before `Number()` conversion.
- **Agent 6 workaround:** `server/tests/**/matchmaking*.test.ts` and battle-flow
  integration mock `BattleServer` so queue logic stays testable.

### BUG-005 [bug] [agent1] TEST_SPEC API drift: `BattleSimulation.StartBattle()` does not exist
- `docs/planning/phase1/TEST_SPEC.md` uses `sim.StartBattle()`; `Initialize(...)`
  sets `Status = Playing` directly. Tests call `Initialize(...)` only.
- Recommend updating TEST_SPEC or adding a `StartBattle()` alias (Agent 1).

### BUG-006 [bug] [agent1] [agent4] Spawn lookups use card names that do not exist
- Code looks up singular names; `Assets/Resources/Data/Cards` only has the
  plural/collective names. Each lookup returns null and the spawn is skipped:
  | Lookup | Used by | Existing card |
  |---|---|---|
  | `"Skeleton"` | Graveyard, Skeleton King ability, Witch death, Tombstone | `Skeletons` (92) |
  | `"Bat"` | Night Witch death | `Bats` (82) |
  | `"Lava Pup"` | Lava Hound death | — (no Lava Pup card) |
  | `"Goblin"` | Goblin Drill waves | `Goblins` (91) / `Goblin Gang` (76) |
  | `"Royal Recruit"` | Royal Delivery landing | — (no such card) |
  | `"Barbarian"` | Barbarian Barrel landing | `Barbarians` (56) |
- Working lookups (verified): `"Spear Goblins"` (65), `"Fire Spirit"` (11), `"Guards"` (67).
- Agent 6 tests assert activatable behavior (cooldowns/costs) where spawns are
  blocked, and full spec counts (e.g. Graveyard ×15) once this is fixed.

### BUG-007 [bug] [agent2] [agent3] `CRClone.Core` vs `CRClone.Network` enum duplication
- Both namespaces declare `BattleStatus`, `CardRarity`, `CardType`, `EntityType`
  (plus `TowerType` also in `CRClone.Battle.Simulation`) **with different values**
  (e.g. Core `EntityType.Unit=1` vs Network `EntityType.Unit=0`).
- Any file importing both namespaces that names one of these fails with CS0104.
  `BattleSimulation.cs` was pinned to the Core meanings via using-aliases
  (its logic was written against Core values); `InputManager.cs` (Battle/Input)
  and `LobbyScreen.cs` (UI) still use ambiguous names and need the same
  treatment by their owners. Long term: de-duplicate or fully qualify.

## Notes
- Unity card IDs are 1–122 (`Assets/Resources/Data/Cards/*.asset`, e.g. Knight=89,
  Skeletons=92, Fireball=57, Tesla=95, Archer Queen=51); the `260000xx` scheme from
  early docs is obsolete. Agent 6 decks/tests use real IDs (see `TestDecks.cs`).
- All simulation units spawn at card level 1 (`PlayCard` uses `GetStats(1)`), so
  combat assertions use base stats, not tournament-standard projections.
- Server service APIs used in tests were read from `server/src/services/*.ts` on
  2026-09-12; repository layers are mocked, so no live DB is required.
