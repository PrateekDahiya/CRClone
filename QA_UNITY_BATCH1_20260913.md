# QA Unity Batch #1 — 2026-09-13 - `feature/qa-integration`

Source: `unityErrors.md` (1580 lines, committed as evidence). Prev. layer (3 syntax errors) fixed via `feature/qa-fix-csharp-compile`.

## Disposition: ~150 errors are missing PACKAGES, not code bugs
Files already carry correct `using UnityEngine.UI;` / `EventSystems` / `TMPro` lines — the assemblies were absent (repo never had a working Unity session; `Packages/` + settings first generated 2026-09-13).

## Fixes landed (4 branches, file-disjoint, all merged)
| Branch | Content | Commit |
|---|---|---|
| `feature/qa-fix-pkgs` | manifest += ugui 1.0.0, inputsystem 1.7.0, tmp 3.0.6, addressables 1.21.21; vendored `Assets/Plugins/protobuf-net.dll` 3.2.45 netstandard2.0 (3.2.42 unpublished; Serialization.cs uses standard API only) | `0d82b6c` |
| `feature/qa-fix-battle-using` | Added usings only: BattleHUD/ElixirBar/HandBar/CardDeployAnimation (+System.Collections), ElixirBar/HandBar/InputManager/PoolableComponents (+Sim/Data/UI); `NetworkClient.InputType` → `InputType` ×3 | `963838b` |
| `feature/qa-fix-ui-refs` | AccessibilityManager +`using CRClone.Core` (CardRarity); ChatMessageUI +`using CRClone.UI.Screens` (ClanScreen) | `6d6c578` |
| `feature/qa-fix-netmisc` | Deleted dup `implicit operator FixedVector2(UnityEngine.Vector2)` (CS0557; proven identical sig, no custom Vector2 in scope) | `ae40205` |

## NEW issues filed (found during fix — same duplication disease as ISSUE-103)
- **ISSUE-104 [P1] — `PlayerState` in TWO namespaces** (`CRClone.Battle.Simulation` :1031 AND `CRClone.Network` :189). Any file importing both gets ambiguity errors. Needs owner decision: canonicalize to one (likely Simulation; Network keeps DTO or maps). Do NOT blindly add `using CRClone.Network;` to ElixirBar/HandBar.
- **ISSUE-105 [P2] — second `FixedVector2` struct** (`GameTypes.cs:110` vs `FixedMath.cs` `CRClone.Core.Math.FixedVector2`). Ambiguity hazard; needs dedup decision by Agent 1.

## User actions in Editor (on reopen)
1. Input-System backend prompt → **Yes** (restart); 2. TMP-Essentials import dialog → **Import**; 3. addressables init → accept defaults.
4. `Assets/Plugins/protobuf-net.dll.meta` will generate locally — QA commits it next cycle.
5. Paste the NEXT error batch (expect residuals behind the package layer).

*QA Agent 7 — fixes by directed agents, diffs reviewed on merge; compile proof belongs to Unity.*
