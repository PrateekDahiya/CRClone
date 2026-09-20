# QA Unity Error Tracker — per-signature fix record

Generated 2026-09-13 from unityErrors.md + unityErrors2.md + unityErrors3.md.
212 unique signatures, 1493 pasted lines. Batches: B/b1, B2, B3.
“Current ~325” reconciles: protobuf (~200) + TMPro/InputSystem/Addressables (~40) + fresh-compile remainder.

Status legend: FIXED-<commit> = merged to qa-integration, cleared from later batches.
PENDING-USER-ENV = package resolved on disk (PackageCache verified), needs Editor restart + full recompile.
FIXING-NOW = agent dispatched this cycle. STALE-SUSPECT = type exists in tree + identical refs clean elsewhere (verify on recompile).
BENIGN-P3-OPEN = warning, cosmetic, unscheduled. ANTICIPATED = spotted ahead of compiler, not yet erroring.

| # | Signature (file | code | symbol) | Batches | n | Group | Status | Fix record |
|---|-------------------------------|---------|---|-------|--------|------------|
| 1 | `Assets/Scripts/Network/MessageTypes.cs \| CS0246 \| ProtoMemberAttribute` | B+B2+B3 | 333 | protobuf-net DLL import | FIXING-NOW | DLL vendored 0d82b6c; .meta Editor-enable fix outgoing (this cycle) |
| 2 | `Assets/Scripts/Network/MessageTypes.cs \| CS0246 \| ProtoMember` | B+B2+B3 | 333 | protobuf-net DLL import | FIXING-NOW | DLL vendored 0d82b6c; .meta Editor-enable fix outgoing (this cycle) |
| 3 | `Assets/Scripts/Network/MessageTypes.cs \| CS0246 \| ProtoContractAttribute` | B+B2+B3 | 111 | protobuf-net DLL import | FIXING-NOW | DLL vendored 0d82b6c; .meta Editor-enable fix outgoing (this cycle) |
| 4 | `Assets/Scripts/Network/MessageTypes.cs \| CS0246 \| ProtoContract` | B+B2+B3 | 111 | protobuf-net DLL import | FIXING-NOW | DLL vendored 0d82b6c; .meta Editor-enable fix outgoing (this cycle) |
| 5 | `Assets/Scripts/UI/Components/CardDetailsModal.cs \| CS0246 \| Text` | B | 17 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 6 | `Assets/Scripts/UI/Screens/ProfileScreen.cs \| CS0246 \| Text` | B | 15 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 7 | `Assets/Scripts/UI/Screens/MainMenuScreen.cs \| CS0246 \| Button` | B | 14 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 8 | `Assets/Scripts/UI/Screens/SettingsScreen.cs \| CS0246 \| Toggle` | B | 13 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 9 | `Assets/Scripts/UI/UIManager.cs \| CS0246 \| ScreenType` | B+B2 | 12 | missing using / moved type | FIXED-963838b/6d6c578/913bd53/947bc3c | usings added; ScreenType to namespace level |
| 10 | `Assets/Scripts/UI/Components/ClanMemberItemUI.cs \| CS0246 \| ClanScreen` | B+B2 | 12 | false nesting/missing using | FIXED-e8de439/913bd53 | prefix strip + Screens using |
| 11 | `Assets/Scripts/UI/Components/PlayerListItemUI.cs \| CS0246 \| ClanScreen` | B+B2 | 12 | false nesting/missing using | FIXED-e8de439/913bd53 | prefix strip + Screens using |
| 12 | `Assets/Scripts/UI/Screens/ClanScreen.cs \| CS0246 \| Button` | B | 11 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 13 | `Assets/Scripts/UI/Screens/BattleResultScreen.cs \| CS0246 \| Text` | B | 10 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 14 | `Assets/Scripts/UI/Screens/SettingsScreen.cs \| CS0246 \| Button` | B | 10 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 15 | `Assets/Scripts/UI/DeckBuilderUI.cs \| CS0246 \| Text` | B | 9 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 16 | `Assets/Scripts/UI/UIAudioManager.cs \| CS0246 \| Slider` | B | 9 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 17 | `Assets/Scripts/UI/Components/OfferItemUI.cs \| CS0246 \| ShopScreen` | B+B2 | 8 | false nesting/missing using | FIXED-e8de439/913bd53 | prefix strip + Screens using |
| 18 | `Assets/Scripts/UI/Components/ShopOfferItemUI.cs \| CS0246 \| ShopScreen` | B+B2 | 8 | false nesting/missing using | FIXED-e8de439/913bd53 | prefix strip + Screens using |
| 19 | `Assets/Scripts/UI/Screens/LobbyScreen.cs \| CS0246 \| Button` | B | 8 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 20 | `Assets/Scripts/UI/Screens/ShopScreen.cs \| CS0246 \| Button` | B | 7 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 21 | `Assets/Scripts/UI/Screens/SettingsScreen.cs \| CS0246 \| Dropdown` | B | 7 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 22 | `Assets/Scripts/UI/AccessibilityManager.cs \| CS0246 \| CardRarity` | B | 6 | missing using / moved type | FIXED-963838b/6d6c578/913bd53/947bc3c | usings added; ScreenType to namespace level |
| 23 | `Assets/Scripts/UI/DeckBuilderUI.cs \| CS0246 \| PointerEventData` | B | 6 | uGUI/EventSystems pkg | FIXED-0d82b6c | manifest (file already imports EventSystems) |
| 24 | `Assets/Scripts/UI/InputManager.cs \| CS0246 \| Key` | B+B2+B3 | 6 | InputSystem 1.7.0 pkg | PENDING-USER-ENV | resolved on disk; needs Editor RESTART (backend switch) + recompile |
| 25 | `Assets/Scripts/UI/InputManager.cs \| CS0246 \| GamepadButton` | B+B2+B3 | 6 | InputSystem 1.7.0 pkg | PENDING-USER-ENV | resolved on disk; needs Editor RESTART (backend switch) + recompile |
| 26 | `Assets/Scripts/UI/Components/ChatMessageUI.cs \| CS0246 \| Text` | B | 6 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 27 | `Assets/Scripts/UI/LocalizationManager.cs \| CS0246 \| TMPro` | B+B2+B3 | 6 | TextMeshPro 3.0.6 pkg | PENDING-USER-ENV | resolved on disk; needs Editor recompile (+TMP-Essentials import for runtime) |
| 28 | `Assets/Scripts/UI/Screens/BattleResultScreen.cs \| CS0246 \| Image` | B | 6 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 29 | `Assets/Scripts/UI/UIManager.cs \| CS0246 \| TMPro` | B+B2+B3 | 6 | TextMeshPro 3.0.6 pkg | PENDING-USER-ENV | resolved on disk; needs Editor recompile (+TMP-Essentials import for runtime) |
| 30 | `Assets/Scripts/UI/Components/LoadingUI.cs \| CS0246 \| TMP_Text` | B+B2+B3 | 6 | TextMeshPro 3.0.6 pkg | PENDING-USER-ENV | resolved on disk; needs Editor recompile (+TMP-Essentials import for runtime) |
| 31 | `Assets/Scripts/Network/ReconnectionManager.cs \| CS0426 \| BattleData` | B+B2+B3 | 6 | type exists in tree | STALE-SUSPECT | GameManager.BattleData exists; NetworkClient identical refs clean — verify on recompile |
| 32 | `Assets/Scripts/UI/Screens/LobbyScreen.cs \| CS0246 \| Text` | B | 6 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 33 | `Assets/Scripts/Battle/UI/BattleHUD.cs \| CS0246 \| Text` | B | 5 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 34 | `Assets/Scripts/UI/Components/CardDetailsModal.cs \| CS0246 \| Button` | B | 5 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 35 | `Assets/Scripts/UI/DeckBuilderUI.cs \| CS0246 \| Button` | B | 5 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 36 | `Assets/Scripts/UI/Components/OfferItemUI.cs \| CS0246 \| Text` | B | 5 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 37 | `Assets/Scripts/UI/Screens/BattleScreen.cs \| CS0246 \| Button` | B | 5 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 38 | `Assets/Scripts/UI/Screens/ClanScreen.cs \| CS0246 \| Text` | B | 5 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 39 | `Assets/Scripts/UI/Screens/MainMenuScreen.cs \| CS0246 \| Text` | B | 5 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 40 | `Assets/Scripts/UI/Screens/SettingsScreen.cs \| CS0246 \| Slider` | B | 5 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 41 | `Assets/Scripts/Battle/UI/BattleHUD.cs \| CS0246 \| Image` | B | 4 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 42 | `Assets/Scripts/Battle/UI/BattleHUD.cs \| CS0246 \| Button` | B | 4 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 43 | `Assets/Scripts/Battle/UI/HandBar.cs \| CS0246 \| IEnumerator` | B | 4 | missing using | FIXED-963838b/913bd53/6a3ed01 | System.Collections added |
| 44 | `Assets/Scripts/UI/LocalizationManager.cs \| CS0246 \| Text` | B+B2 | 4 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 45 | `Assets/Scripts/UI/Components/ChestSlotUI.cs \| CS0246 \| IEnumerator` | B+B2 | 4 | missing using | FIXED-963838b/913bd53/6a3ed01 | System.Collections added |
| 46 | `Assets/Scripts/UI/Components/ClanMemberItemUI.cs \| CS0246 \| Text` | B | 4 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 47 | `Assets/Scripts/UI/Screens/BattleResultScreen.cs \| CS0246 \| Button` | B | 4 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 48 | `Assets/Scripts/UI/Components/PlayerListItemUI.cs \| CS0246 \| Text` | B | 4 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 49 | `Assets/Scripts/UI/Components/QuestBarUI.cs \| CS0246 \| Image` | B | 4 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 50 | `Assets/Scripts/UI/Components/QuestBarUI.cs \| CS0246 \| Text` | B | 4 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 51 | `Assets/Scripts/UI/Components/ShopOfferItemUI.cs \| CS0246 \| Text` | B | 4 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 52 | `Assets/Scripts/UI/Screens/ProfileScreen.cs \| CS0246 \| Button` | B | 4 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 53 | `Assets/Scripts/UI/Screens/MainMenuScreen.cs \| CS0246 \| ScreenType` | B+B2 | 4 | missing using / moved type | FIXED-963838b/6d6c578/913bd53/947bc3c | usings added; ScreenType to namespace level |
| 54 | `Assets/Scripts/UI/Screens/ShopScreen.cs \| CS0246 \| Text` | B | 4 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 55 | `Assets/Scripts/Systems/AssetManager.cs \| CS0234 \| AddressableAssets` | B+B2+B3 | 3 | Addressables 1.21.21 pkg | PENDING-USER-ENV | resolved on disk; needs recompile |
| 56 | `Assets/Scripts/Systems/AssetManager.cs \| CS0234 \| ResourceManagement` | B+B2+B3 | 3 | Addressables 1.21.21 pkg | PENDING-USER-ENV | resolved on disk; needs recompile |
| 57 | `Assets/Scripts/UI/Components/LoadingUI.cs \| CS0246 \| TMPro` | B+B2+B3 | 3 | TextMeshPro 3.0.6 pkg | PENDING-USER-ENV | resolved on disk; needs Editor recompile (+TMP-Essentials import for runtime) |
| 58 | `Assets/Scripts/UI/Components/ToastUI.cs \| CS0246 \| TMPro` | B+B2+B3 | 3 | TextMeshPro 3.0.6 pkg | PENDING-USER-ENV | resolved on disk; needs Editor recompile (+TMP-Essentials import for runtime) |
| 59 | `Assets/Scripts/UI/InputManager.cs \| CS0234 \| InputSystem` | B+B2+B3 | 3 | InputSystem 1.7.0 pkg | PENDING-USER-ENV | resolved on disk; needs Editor RESTART (backend switch) + recompile |
| 60 | `Assets/Scripts/Systems/AssetManager.cs \| CS0246 \| AsyncOperationHandle` | B+B2+B3 | 3 | Addressables 1.21.21 pkg | PENDING-USER-ENV | resolved on disk; needs recompile |
| 61 | `Assets/Scripts/Battle/UI/ElixirBar.cs \| CS0246 \| Image` | B | 3 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 62 | `Assets/Scripts/Battle/UI/HandBar.cs \| CS0246 \| Image` | B | 3 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 63 | `Assets/Scripts/Battle/Simulation/Projectile.cs \| CS0108 \| 'Projectile.IsDead' hides inherited member 'Entity.IsDead'. ` | B+B2+B3 | 3 | hides-inherited-member warning | BENIGN-P3-OPEN | cosmetic `new` keyword cleanup, unscheduled |
| 64 | `Assets/Scripts/UI/Animation/CardDeployAnimation.cs \| CS0246 \| Image` | B+B2+B3 | 3 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 65 | `Assets/Scripts/Battle/Simulation/SpellEffect.cs \| CS0108 \| 'SpellEffect.Type' hides inherited member 'Entity.Type'. Use` | B+B2+B3 | 3 | hides-inherited-member warning | BENIGN-P3-OPEN | cosmetic `new` keyword cleanup, unscheduled |
| 66 | `Assets/Scripts/Battle/Simulation/Tower.cs \| CS0108 \| 'Tower.Type' hides inherited member 'Entity.Type'. Use the n` | B+B2+B3 | 3 | hides-inherited-member warning | BENIGN-P3-OPEN | cosmetic `new` keyword cleanup, unscheduled |
| 67 | `Assets/Scripts/UI/InputManager.cs \| CS0246 \| Selectable` | B+B2+B3 | 3 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 68 | `Assets/Scripts/UI/DeckBuilderUI.cs \| CS0246 \| Image` | B | 3 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 69 | `Assets/Scripts/UI/InputManager.cs \| CS0246 \| PointerEventData` | B | 3 | uGUI/EventSystems pkg | FIXED-0d82b6c | manifest (file already imports EventSystems) |
| 70 | `Assets/Scripts/UI/InputManager.cs \| CS0246 \| InputActionAsset` | B+B2+B3 | 3 | InputSystem 1.7.0 pkg | PENDING-USER-ENV | resolved on disk; needs Editor RESTART (backend switch) + recompile |
| 71 | `Assets/Scripts/UI/InputManager.cs \| CS0246 \| InputAction` | B+B2+B3 | 3 | InputSystem 1.7.0 pkg | PENDING-USER-ENV | resolved on disk; needs Editor RESTART (backend switch) + recompile |
| 72 | `Assets/Scripts/UI/Components/ClanMemberItemUI.cs \| CS0246 \| Button` | B | 3 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 73 | `Assets/Scripts/UI/Components/NewsBannerUI.cs \| CS0246 \| Text` | B | 3 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 74 | `Assets/Scripts/UI/Components/OfferItemUI.cs \| CS0246 \| Image` | B | 3 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 75 | `Assets/Scripts/UI/Components/PlayerListItemUI.cs \| CS0246 \| Button` | B | 3 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 76 | `Assets/Scripts/UI/Screens/ChestUnlockScreen.cs \| CS0246 \| Image` | B | 3 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 77 | `Assets/Scripts/UI/Screens/ChestUnlockScreen.cs \| CS0246 \| Text` | B | 3 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 78 | `Assets/Scripts/UI/Components/ShopOfferItemUI.cs \| CS0246 \| Image` | B | 3 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 79 | `Assets/Scripts/UI/Components/ToastUI.cs \| CS0246 \| TMP_Text` | B+B2+B3 | 3 | TextMeshPro 3.0.6 pkg | PENDING-USER-ENV | resolved on disk; needs Editor recompile (+TMP-Essentials import for runtime) |
| 80 | `Assets/Scripts/Network/ReconnectionManager.cs \| CS0246 \| NetworkDisconnectedEvent` | B+B2+B3 | 3 | types exist in tree | STALE-SUSPECT | structs exist Services.cs:261-285; verify on recompile |
| 81 | `Assets/Scripts/Network/ReconnectionManager.cs \| CS0246 \| NetworkConnectedEvent` | B+B2+B3 | 3 | types exist in tree | STALE-SUSPECT | structs exist Services.cs:261-285; verify on recompile |
| 82 | `Assets/Scripts/UI/Screens/ProfileScreen.cs \| CS0426 \| PlayerData` | B+B2+B3 | 3 | missing DTO | FIXED-6a3ed01 | PlayerData DTO + PlayerLocalData inheritance |
| 83 | `Assets/Scripts/Network/ReconnectionManager.cs \| CS0246 \| ReconciliationEvent` | B+B2+B3 | 3 | types exist in tree | STALE-SUSPECT | structs exist Services.cs:261-285; verify on recompile |
| 84 | `Assets/Scripts/Network/Serialization.cs \| CS0246 \| ProtoContractAttribute` | B+B2+B3 | 3 | protobuf-net DLL import | FIXING-NOW | DLL vendored 0d82b6c; .meta Editor-enable fix outgoing (this cycle) |
| 85 | `Assets/Scripts/Network/Serialization.cs \| CS0246 \| ProtoContract` | B+B2+B3 | 3 | protobuf-net DLL import | FIXING-NOW | DLL vendored 0d82b6c; .meta Editor-enable fix outgoing (this cycle) |
| 86 | `Assets/Scripts/Network/Serialization.cs \| CS0246 \| ProtoMemberAttribute` | B+B2+B3 | 3 | protobuf-net DLL import | FIXING-NOW | DLL vendored 0d82b6c; .meta Editor-enable fix outgoing (this cycle) |
| 87 | `Assets/Scripts/Network/Serialization.cs \| CS0246 \| ProtoMember` | B+B2+B3 | 3 | protobuf-net DLL import | FIXING-NOW | DLL vendored 0d82b6c; .meta Editor-enable fix outgoing (this cycle) |
| 88 | `Assets/Scripts/UI/UIAudioManager.cs \| CS0246 \| Toggle` | B | 3 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 89 | `Assets/Scripts/Battle/Presentation/HealthBar.cs \| CS0246 \| Image` | B | 2 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 90 | `Assets/Scripts/Battle/Input/InputManager.cs \| CS0246 \| CardData` | B | 2 | missing using / moved type | FIXED-963838b/6d6c578/913bd53/947bc3c | usings added; ScreenType to namespace level |
| 91 | `Assets/Scripts/Battle/UI/ElixirBar.cs \| CS0246 \| IEnumerator` | B | 2 | missing using | FIXED-963838b/913bd53/6a3ed01 | System.Collections added |
| 92 | `Assets/Scripts/Battle/UI/HandBar.cs \| CS0246 \| Text` | B | 2 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 93 | `Assets/Scripts/Battle/UI/TowerHealthUI.cs \| CS0246 \| Image` | B | 2 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 94 | `Assets/Scripts/UI/DeckBuilderUI.cs \| CS0246 \| IDropHandler` | B | 2 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 95 | `Assets/Scripts/UI/Animation/UITweens.cs \| CS0246 \| Graphic` | B | 2 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 96 | `Assets/Scripts/UI/Animation/UITweens.cs \| CS0246 \| Image` | B | 2 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 97 | `Assets/Scripts/UI/DeckBuilderUI.cs \| CS0246 \| Dropdown` | B | 2 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 98 | `Assets/Scripts/UI/Components/ChatMessageUI.cs \| CS0246 \| ClanScreen` | B | 2 | false nesting/missing using | FIXED-e8de439/913bd53 | prefix strip + Screens using |
| 99 | `Assets/Scripts/UI/Components/ChatMessageUI.cs \| CS0246 \| Image` | B | 2 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 100 | `Assets/Scripts/UI/Components/ChatMessageUI.cs \| CS0246 \| Button` | B | 2 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 101 | `Assets/Scripts/UI/Screens/ProfileScreen.cs \| CS0101 \| The namespace 'CRClone.UI.Screens' already contains a defini` | B+B2 | 2 | duplicate class | FIXED-947bc3c | ProfileScreen copy renamed ProfileBattleLogItemUI |
| 102 | `Assets/Scripts/UI/InputManager.cs \| CS0102 \| The type 'TouchAlternativeHandler' already contains a defini` | B+B2 | 2 | duplicate member | FIXED-947bc3c | event renamed OnDragged (zero subscribers) |
| 103 | `Assets/Scripts/UI/Components/ChangeNameModal.cs \| CS0246 \| Button` | B | 2 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 104 | `Assets/Scripts/UI/Components/ChestSlotUI.cs \| CS0246 \| EventBus` | B+B2 | 2 | missing using / moved type | FIXED-963838b/6d6c578/913bd53/947bc3c | usings added; ScreenType to namespace level |
| 105 | `Assets/Scripts/UI/Components/ChestSlotUI.cs \| CS0246 \| Image` | B | 2 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 106 | `Assets/Scripts/UI/UIManager.cs \| CS0246 \| Text` | B | 2 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 107 | `Assets/Scripts/UI/Components/LoadingUI.cs \| CS0246 \| IEnumerator` | B+B2 | 2 | missing using | FIXED-963838b/913bd53/6a3ed01 | System.Collections added |
| 108 | `Assets/Scripts/UI/Components/ClanMemberItemUI.cs \| CS0246 \| Image` | B | 2 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 109 | `Assets/Scripts/UI/Components/LoadingUI.cs \| CS0246 \| Text` | B | 2 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 110 | `Assets/Scripts/UI/Components/DonateRequestModal.cs \| CS0246 \| Button` | B | 2 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 111 | `Assets/Scripts/UI/Components/NewsBannerUI.cs \| CS0246 \| Button` | B | 2 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 112 | `Assets/Scripts/UI/Components/PlayerListItemUI.cs \| CS0246 \| Image` | B | 2 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 113 | `Assets/Scripts/UI/Components/QuestBarUI.cs \| CS0246 \| EventBus` | B+B2 | 2 | missing using / moved type | FIXED-963838b/6d6c578/913bd53/947bc3c | usings added; ScreenType to namespace level |
| 114 | `Assets/Scripts/UI/Components/ToastUI.cs \| CS0246 \| IEnumerator` | B+B2 | 2 | missing using | FIXED-963838b/913bd53/6a3ed01 | System.Collections added |
| 115 | `Assets/Scripts/UI/Components/QuestBarUI.cs \| CS0246 \| Button` | B | 2 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 116 | `Assets/Scripts/UI/Screens/ChestUnlockScreen.cs \| CS0246 \| Button` | B | 2 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 117 | `Assets/Scripts/UI/Screens/ProfileScreen.cs \| CS0246 \| Image` | B | 2 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 118 | `Assets/Scripts/UI/Screens/MainMenuScreen.cs \| CS0246 \| Image` | B | 2 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 119 | `Assets/Scripts/UI/Screens/ShopScreen.cs \| CS0305 \| Using the generic type 'IEnumerator<T>' requires 1 type argu` | B+B2 | 2 | missing using | FIXED-6a3ed01 | System.Collections added |
| 120 | `Assets/Scripts/UI/Screens/ClanScreen.cs \| CS0246 \| InputField` | B | 2 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 121 | `Assets/Scripts/UI/Screens/LobbyScreen.cs \| CS0246 \| ChestSlotUI` | B+B2 | 2 | missing using / moved type | FIXED-963838b/6d6c578/913bd53/947bc3c | usings added; ScreenType to namespace level |
| 122 | `Assets/Scripts/UI/Screens/MainMenuScreen.cs \| CS0246 \| ChestSlotUI` | B+B2 | 2 | missing using / moved type | FIXED-963838b/6d6c578/913bd53/947bc3c | usings added; ScreenType to namespace level |
| 123 | `Assets/Scripts/UI/Components/ChatMessageUI.cs \| CS0426 \| ChatMessage` | B2 | 2 | false nesting | FIXED-e8de439 | ClanScreen.ChatMessage → ChatMessage |
| 124 | `Assets/Scripts/Battle/Input/InputManager.cs \| CS0234 \| EventSystems` | B | 1 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 125 | `Assets/Scripts/Battle/Presentation/BuildingView.cs \| CS0234 \| UI` | B | 1 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 126 | `Assets/Scripts/Battle/Presentation/HealthBar.cs \| CS0234 \| UI` | B | 1 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 127 | `Assets/Scripts/Battle/UI/BattleHUD.cs \| CS0234 \| UI` | B | 1 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 128 | `Assets/Scripts/Battle/UI/ElixirBar.cs \| CS0234 \| UI` | B | 1 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 129 | `Assets/Scripts/Battle/UI/HandBar.cs \| CS0234 \| UI` | B | 1 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 130 | `Assets/Scripts/Battle/UI/TowerHealthUI.cs \| CS0234 \| UI` | B | 1 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 131 | `Assets/Scripts/Network/MessageTypes.cs \| CS0246 \| ProtoBuf` | B | 1 | protobuf-net DLL import | FIXING-NOW | DLL vendored 0d82b6c; .meta Editor-enable fix outgoing (this cycle) |
| 132 | `Assets/Scripts/Network/Serialization.cs \| CS0246 \| ProtoBuf` | B | 1 | protobuf-net DLL import | FIXING-NOW | DLL vendored 0d82b6c; .meta Editor-enable fix outgoing (this cycle) |
| 133 | `Assets/Scripts/UI/AccessibilityManager.cs \| CS0234 \| UI` | B | 1 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 134 | `Assets/Scripts/UI/Animation/CardDeployAnimation.cs \| CS0234 \| EventSystems` | B | 1 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 135 | `Assets/Scripts/UI/Animation/ScreenTransition.cs \| CS0234 \| UI` | B | 1 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 136 | `Assets/Scripts/UI/Animation/UITweens.cs \| CS0234 \| UI` | B | 1 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 137 | `Assets/Scripts/UI/Components/CardDetailsModal.cs \| CS0234 \| UI` | B | 1 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 138 | `Assets/Scripts/UI/Components/ChangeNameModal.cs \| CS0234 \| UI` | B | 1 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 139 | `Assets/Scripts/UI/Components/ChatMessageUI.cs \| CS0234 \| UI` | B | 1 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 140 | `Assets/Scripts/UI/Components/ChestSlotUI.cs \| CS0234 \| UI` | B | 1 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 141 | `Assets/Scripts/UI/Components/ClanMemberItemUI.cs \| CS0234 \| UI` | B | 1 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 142 | `Assets/Scripts/UI/Components/DonateRequestModal.cs \| CS0234 \| UI` | B | 1 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 143 | `Assets/Scripts/UI/Components/LoadingUI.cs \| CS0234 \| UI` | B | 1 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 144 | `Assets/Scripts/UI/Components/NewsBannerUI.cs \| CS0234 \| UI` | B | 1 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 145 | `Assets/Scripts/UI/Components/OfferItemUI.cs \| CS0234 \| UI` | B | 1 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 146 | `Assets/Scripts/UI/Components/PlayerListItemUI.cs \| CS0234 \| UI` | B | 1 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 147 | `Assets/Scripts/UI/Components/QuestBarUI.cs \| CS0234 \| UI` | B | 1 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 148 | `Assets/Scripts/UI/Components/ShopOfferItemUI.cs \| CS0234 \| UI` | B | 1 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 149 | `Assets/Scripts/UI/Components/ToastUI.cs \| CS0234 \| UI` | B | 1 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 150 | `Assets/Scripts/UI/DeckBuilderUI.cs \| CS0234 \| UI` | B | 1 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 151 | `Assets/Scripts/UI/DeckBuilderUI.cs \| CS0234 \| EventSystems` | B | 1 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 152 | `Assets/Scripts/UI/InputManager.cs \| CS0234 \| EventSystems` | B | 1 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 153 | `Assets/Scripts/UI/ResponsiveLayout.cs \| CS0234 \| UI` | B | 1 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 154 | `Assets/Scripts/UI/Screens/BattleResultScreen.cs \| CS0234 \| UI` | B | 1 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 155 | `Assets/Scripts/UI/Screens/BattleScreen.cs \| CS0234 \| UI` | B | 1 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 156 | `Assets/Scripts/UI/Screens/ChestUnlockScreen.cs \| CS0234 \| UI` | B | 1 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 157 | `Assets/Scripts/UI/Screens/ClanScreen.cs \| CS0234 \| UI` | B | 1 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 158 | `Assets/Scripts/UI/Screens/LobbyScreen.cs \| CS0234 \| UI` | B | 1 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 159 | `Assets/Scripts/UI/Screens/MainMenuScreen.cs \| CS0234 \| UI` | B | 1 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 160 | `Assets/Scripts/UI/Screens/ProfileScreen.cs \| CS0234 \| UI` | B | 1 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 161 | `Assets/Scripts/UI/Screens/SettingsScreen.cs \| CS0234 \| UI` | B | 1 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 162 | `Assets/Scripts/UI/Screens/ShopScreen.cs \| CS0234 \| UI` | B | 1 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 163 | `Assets/Scripts/UI/UIAudioManager.cs \| CS0234 \| UI` | B | 1 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 164 | `Assets/Scripts/UI/UIManager.cs \| CS0234 \| UI` | B | 1 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 165 | `Assets/Scripts/Battle/UI/BattleHUD.cs \| CS0246 \| IEnumerator` | B | 1 | missing using | FIXED-963838b/913bd53/6a3ed01 | System.Collections added |
| 166 | `Assets/Scripts/Battle/Input/InputManager.cs \| CS0246 \| BattleSimulation` | B | 1 | missing using / moved type | FIXED-963838b/6d6c578/913bd53/947bc3c | usings added; ScreenType to namespace level |
| 167 | `Assets/Scripts/Battle/Input/InputManager.cs \| CS0426 \| InputType` | B | 1 | wrong qualifier | FIXED-963838b | NetworkClient.InputType → InputType |
| 168 | `Assets/Scripts/Battle/Input/InputManager.cs \| CS0246 \| HandBar` | B | 1 | missing using / moved type | FIXED-963838b/6d6c578/913bd53/947bc3c | usings added; ScreenType to namespace level |
| 169 | `Assets/Scripts/Core/FixedMath.cs \| CS0557 \| Duplicate user-defined conversion in type 'FixedVector2'` | B | 1 | duplicate conversion | FIXED-ae40205 | deleted dup operator line |
| 170 | `Assets/Scripts/Battle/Presentation/PoolableComponents.cs \| CS0246 \| Unit` | B | 1 | missing using / moved type | FIXED-963838b/6d6c578/913bd53/947bc3c | usings added; ScreenType to namespace level |
| 171 | `Assets/Scripts/Battle/Presentation/PoolableComponents.cs \| CS0246 \| Building` | B | 1 | missing using / moved type | FIXED-963838b/6d6c578/913bd53/947bc3c | usings added; ScreenType to namespace level |
| 172 | `Assets/Scripts/Battle/Presentation/PoolableComponents.cs \| CS0246 \| SpellEffect` | B | 1 | missing using / moved type | FIXED-963838b/6d6c578/913bd53/947bc3c | usings added; ScreenType to namespace level |
| 173 | `Assets/Scripts/Battle/Presentation/PoolableComponents.cs \| CS0246 \| Projectile` | B | 1 | missing using / moved type | FIXED-963838b/6d6c578/913bd53/947bc3c | usings added; ScreenType to namespace level |
| 174 | `Assets/Scripts/Battle/Presentation/PoolableComponents.cs \| CS0246 \| Tower` | B | 1 | missing using / moved type | FIXED-963838b/6d6c578/913bd53/947bc3c | usings added; ScreenType to namespace level |
| 175 | `Assets/Scripts/Battle/UI/ElixirBar.cs \| CS0246 \| Text` | B | 1 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 176 | `Assets/Scripts/Battle/UI/ElixirBar.cs \| CS0246 \| PlayerState` | B | 1 | missing using / moved type | FIXED-963838b/6d6c578/913bd53/947bc3c | usings added; ScreenType to namespace level |
| 177 | `Assets/Scripts/UI/AccessibilityManager.cs \| CS0246 \| Image` | B | 1 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 178 | `Assets/Scripts/Battle/UI/HandBar.cs \| CS0246 \| BattleSimulation` | B | 1 | missing using / moved type | FIXED-963838b/6d6c578/913bd53/947bc3c | usings added; ScreenType to namespace level |
| 179 | `Assets/Scripts/UI/AccessibilityManager.cs \| CS0246 \| Text` | B | 1 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 180 | `Assets/Scripts/Battle/UI/HandBar.cs \| CS0246 \| PlayerState` | B | 1 | missing using / moved type | FIXED-963838b/6d6c578/913bd53/947bc3c | usings added; ScreenType to namespace level |
| 181 | `Assets/Scripts/Battle/UI/TowerHealthUI.cs \| CS0246 \| Text` | B | 1 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 182 | `Assets/Scripts/UI/Animation/CardDeployAnimation.cs \| CS0246 \| IEnumerator` | B | 1 | missing using | FIXED-963838b/913bd53/6a3ed01 | System.Collections added |
| 183 | `Assets/Scripts/UI/Animation/CardDeployAnimation.cs \| CS0246 \| HandBar` | B | 1 | missing using / moved type | FIXED-963838b/6d6c578/913bd53/947bc3c | usings added; ScreenType to namespace level |
| 184 | `Assets/Scripts/UI/DeckBuilderUI.cs \| CS0246 \| IBeginDragHandler` | B | 1 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 185 | `Assets/Scripts/UI/DeckBuilderUI.cs \| CS0246 \| IDragHandler` | B | 1 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 186 | `Assets/Scripts/UI/DeckBuilderUI.cs \| CS0246 \| IEndDragHandler` | B | 1 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 187 | `Assets/Scripts/UI/DeckBuilderUI.cs \| CS0246 \| IPointerClickHandler` | B | 1 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 188 | `Assets/Scripts/UI/Components/CardDetailsModal.cs \| CS0246 \| Image` | B | 1 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 189 | `Assets/Scripts/UI/DeckBuilderUI.cs \| CS0246 \| InputField` | B | 1 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 190 | `Assets/Scripts/UI/InputManager.cs \| CS0246 \| IPointerDownHandler` | B | 1 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 191 | `Assets/Scripts/UI/InputManager.cs \| CS0246 \| IPointerUpHandler` | B | 1 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 192 | `Assets/Scripts/UI/InputManager.cs \| CS0246 \| IDragHandler` | B | 1 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 193 | `Assets/Scripts/UI/DeckBuilderUI.cs \| CS0246 \| Toggle` | B | 1 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 194 | `Assets/Scripts/UI/ResponsiveLayout.cs \| CS0246 \| CanvasScaler` | B | 1 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 195 | `Assets/Scripts/UI/Components/ChangeNameModal.cs \| CS0246 \| InputField` | B | 1 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 196 | `Assets/Scripts/UI/Components/ChangeNameModal.cs \| CS0246 \| Text` | B | 1 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 197 | `Assets/Scripts/UI/Components/ChestSlotUI.cs \| CS0246 \| Text` | B | 1 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 198 | `Assets/Scripts/UI/UIAudioManager.cs \| CS0246 \| Button` | B | 1 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 199 | `Assets/Scripts/UI/Components/DonateRequestModal.cs \| CS0246 \| Dropdown` | B | 1 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 200 | `Assets/Scripts/UI/Components/LoadingUI.cs \| CS0246 \| Image` | B | 1 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 201 | `Assets/Scripts/UI/Components/ChestSlotUI.cs \| CS0246 \| Button` | B | 1 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 202 | `Assets/Scripts/UI/Components/DonateRequestModal.cs \| CS0246 \| Slider` | B | 1 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 203 | `Assets/Scripts/UI/Components/DonateRequestModal.cs \| CS0246 \| Text` | B | 1 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 204 | `Assets/Scripts/UI/UIManager.cs \| CS0246 \| Image` | B | 1 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 205 | `Assets/Scripts/UI/Components/NewsBannerUI.cs \| CS0246 \| Image` | B | 1 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 206 | `Assets/Scripts/UI/Components/NewsBannerUI.cs \| CS0246 \| Toggle` | B | 1 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 207 | `Assets/Scripts/UI/Components/QuestBarUI.cs \| CS0246 \| ScrollRect` | B | 1 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 208 | `Assets/Scripts/UI/Components/OfferItemUI.cs \| CS0246 \| Button` | B | 1 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 209 | `Assets/Scripts/UI/Components/ToastUI.cs \| CS0246 \| Text` | B | 1 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 210 | `Assets/Scripts/UI/Components/ToastUI.cs \| CS0246 \| Image` | B | 1 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 211 | `Assets/Scripts/UI/Components/ShopOfferItemUI.cs \| CS0246 \| Button` | B | 1 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |
| 212 | `Assets/Scripts/UI/Screens/LobbyScreen.cs \| CS0246 \| Image` | B | 1 | uGUI 1.0.0 pkg (+usings) | FIXED-0d82b6c+usings | manifest + per-file UnityEngine.UI usings (956f53b for stragglers) |