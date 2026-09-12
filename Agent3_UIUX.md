# Agent 3: UI/UX Implementation
## Workstream: All Game Screens, Battle HUD, Deck Builder, Animations, Accessibility

---

## 🎯 YOUR MISSION
Implement **every screen and UI component** from the UX spec - polished, animated, responsive, and accessible. This is what players see and interact with.

---

## 📁 FILES YOU OWN (Exclusive Write Access)
```
Assets/Scripts/UI/
├── UIManager.cs              # Screen management, toasts, loading
├── Screens/
│   ├── MainMenuScreen.cs     # Play button, navigation bar, chest slots
│   ├── LobbyScreen.cs        # Battle modes (1v1, 2v2, Tournament, Friendly, Practice)
│   ├── DeckBuilderScreen.cs  # 8-slot deck, collection grid, filters, validation
│   ├── BattleScreen.cs       # Battle scene UI container
│   ├── BattleResultScreen.cs # Crowns, trophy change, rewards, replay/share/rematch
│   ├── ShopScreen.cs         # Daily/Special/Chests tabs, purchase flow
│   ├── ClanScreen.cs         # Chat/Members/War/Capital tabs, donations
│   ├── ProfileScreen.cs      # Stats, battle log, settings
│   └── SettingsScreen.cs     # Graphics, audio, gameplay, privacy
├── Components/
│   ├── HandBar.cs            # 4-card hand, elixir bar, next card preview
│   ├── ElixirBar.cs          # 10-segment animated bar
│   ├── TowerHealthUI.cs      # Tower HP bars with crown icons
│   ├── BattleHUD.cs          # Timer, crowns, pause menu
│   ├── DeckSlotUI.cs         # Drag-drop deck slot (8 slots)
│   ├── CollectionCardUI.cs   # Draggable card in collection
│   ├── ChestSlotUI.cs        # Chest with unlock timer
│   ├── ChatMessageUI.cs      # Clan chat bubbles
│   └── ToastUI.cs            # Toast notifications
├── Animation/
│   ├── ScreenTransition.cs   # Slide/fade transitions
│   ├── CardDeployAnimation.cs # Card select → deploy → draw
│   └── UITweens.cs           # Scale, fade, shake, pulse
```

### Already Partially Done (Enhance/Complete)
```
Assets/Scripts/Battle/UI/
├── HandBar.cs        # Exists - enhance with animations, states
├── ElixirBar.cs      # Exists - add pulse on gain
├── TowerHealthUI.cs  # Exists - add damage flash
└── BattleHUD.cs      # Exists - add overtime styling
```

---

## ✅ DELIVERABLES CHECKLIST

### Main Menu / Lobby
- [ ] **MainMenu**: Animated battle button (pulse), navigation bar (5 tabs), chest slots (4), quest bar, news banner
- [ ] **Lobby**: 5 battle mode buttons (1v1, 2v2, Tournament, Friendly, Practice), deck builder shortcut, chest slots visible

### Deck Builder (Critical)
- [ ] **8-slot deck grid** with drag-drop from collection
- **Collection panel**: Grid of all owned cards (4 columns, scrollable)
- **Filters**: Rarity tabs (Common/Rare/Epic/Legendary/Champion), Type tabs (Troop/Spell/Building/Champion), Search box
- **Validation**: Real-time (8 cards, max 1 champion, elixir avg warning)
- **Stats display**: Avg elixir, champion indicator, card type balance
- **Actions**: Save (validate → send to server), Cancel (revert), Copy Link

### Battle HUD (Critical - Used Every Match)
- [ ] **Hand Bar**: 4 cards, elixir cost badge, selection glow, unaffordable dimming, deploy animation
- [ ] **Elixir Bar**: 10 segments, smooth fill animation, pulse on gain, numeric display
- [ ] **Next Card Preview**: Small grayed card at end of hand
- [ ] **Timer**: MM:SS, red in overtime, overtime label
- [ ] **Crowns**: 3 per side, filled/empty sprites, king crown on 3-crown win
- [ ] **Pause Menu**: Resume, Settings, Concede, Connected players

### Battle Result
- [ ] **Victory/Defeat/Draw** banner with animation
- [ ] **Crown Display**: Large crowns with player names
- [ ] **Trophy Change**: +32/-32 with animation
- [ ] **Rewards**: Chest, gold, XP with icons
- [ ] **Actions**: Watch Replay, Share, Rematch, Back to Lobby

### Shop
- [ ] **Tabs**: Daily, Special, Chests, Gems, Wild Cards
- [ ] **Offers**: Card offers (with count), Gold, Gems, Chests
- [ ] **Purchase Flow**: Confirm → IAP/Gold/Gems → Reward animation
- [ ] **Timers**: Daily refresh countdown, special offer countdown

### Clan
- [ ] **Tabs**: Chat, Members, War, Capital, Settings
- [ ] **Chat**: Messages, replay links, donation requests with [Donate] button
- [ ] **Members**: List with role, trophies, donations/week, promote/demote/kick
- [ ] **War**: River Race / Classic War UI (placeholder for Phase 3)

### Profile & Settings
- [ ] **Profile**: Avatar, name, tag, trophies, best, level, win rate, battle log
- [ ] **Settings**: Graphics (Low/Med/High/Ultra), FPS cap, VSync, Audio sliders, Gameplay (deploy mode, auto-target, left-handed), Privacy, Notifications

### Animations & Polish
- [ ] **Screen Transitions**: Horizontal slide (300ms), modal fade+scale (200ms)
- [ ] **Card Deploy**: Select (scale 1.2x + glow) → Drag → Deploy (scale 0 + spawn at position)
- [ ] **Elixir Pulse**: Segment fill + subtle scale pulse
- [ ] **Tower Hit**: Shake + red flash
- [ ] **Tower Destroy**: Collapse + explosion (2s)
- [ ] **Card Draw**: Slide from right + flip (300ms)
- [ ] **Idle Animations**: Battle button pulse, chest bounce, card breathing

### Responsive & Accessibility
- [ ] **Breakpoints**: Mobile (<768), Landscape (768-1024), Tablet (1024-1366), Desktop (1366-1920), Large (>1920)
- [ ] **Safe Areas**: Notch (44px top), Home indicator (34px bottom)
- [ ] **Color Blind**: Patterns + colors for rarity (not color-only)
- [ ] **High Contrast Mode**: Toggle in settings
- [ ] **Reduce Motion**: Disables non-essential animations
- [ ] **Text Scaling**: 100%-200% system scaling support
- [ ] **Audio**: Master/Music/SFX/Voice sliders, mute on focus loss
- [ ] **Input**: Remappable keys, gamepad support, tap alternative for drag

---

## 📚 REFERENCE DOCS
| Doc | Purpose |
|-----|---------|
| `docs/planning/phase1/UI_UX_SPEC.md` | **FULL SPEC** - All screens, layouts, interactions, animations, breakpoints |
| `docs/planning/phase1/MECHANICS.md` | Battle HUD data (elixir, crowns, timer) |

---

## 🔗 DEPENDENCIES

| Dependency | Status | How You Use It |
|------------|--------|----------------|
| `EventBus` events | ✅ Done | Subscribe: `OnCardPlayed`, `OnUnitSpawned`, `OnSpellCast`, `OnTowerDamaged`, `OnTowerDestroyed`, `OnKingTowerActivated`, `OnBattleEnded`, `OnElixirChanged`, `OnBattleTick` |
| `NetworkClient` | ⏳ Agent 2 | Call `SendInput()` for card play, spell cast, champion ability |
| `DataManager` | ✅ Done | `GetCard()`, `GetAllCards()`, `ValidateDeck()` |
| `AssetManager` | ⏳ Agent 4 | `LoadSprite()`, `LoadPrefab()` for card portraits, unit prefabs |
| Agent 4 Assets | ⏳ Parallel | Card portraits, unit prefabs, UI atlas, fonts |

---

## 🧪 TESTING REQUIREMENTS
- **Playmode Tests**: Each screen loads, buttons work, navigation flows
- **UI Automation**: 
  - Deck building: drag 8 cards → save → verify in battle
  - Battle: deploy card → verify unit spawns → elixir deducted
  - Shop: purchase → verify gold/gems deducted
- **Responsive**: Test at all 5 breakpoints
- **Accessibility**: Color blind sim, high contrast, reduce motion, screen reader
- **Performance**: 60 FPS on battle screen with 50 units

---

## 🚫 DO NOT TOUCH
- `Assets/Scripts/Core/` - Frozen
- `Assets/Scripts/Battle/Simulation/` - Agent 1
- `Assets/Scripts/Network/` - Agent 2
- `Assets/Scripts/Systems/` - Agent 4 (except UI-specific components)
- `server/` - Agents 2 & 5

---

## 🌿 GIT WORKTREE SETUP (Run All 6 Agents Simultaneously)

**Each agent works in their own isolated worktree - no conflicts, no waiting.**

```bash
# Run ONCE per agent (each agent runs their own setup):

# Agent 3 - UI/UX
git worktree add ../CRClone-agent3 feature/ui-ux-implementation
cd ../CRClone-agent3
cp .env.example .env   # Fill in your DB credentials
# Start working...
```

**Each worktree is a complete, independent copy of the repo** - you can build, run tests, and commit independently. No stepping on each other's toes.

### Branch & Workflow (Per Worktree)
```bash
# Inside your worktree directory:
git checkout -b feature/ui-ux-implementation  # Already set by worktree add
# Work, commit frequently
git push origin feature/ui-ux-implementation
# Create PR when deliverables done
```

**Integration Points (Cross-Agent Sync via PRs):**
- Week 1: Mock data - build all screens with fake data, no network needed
- Week 2: Agent 2 provides `NetworkClient` → wire real matchmaking, battle actions
- Week 3: Agent 1 events → HUD updates in real-time
- Week 4: Agent 4 assets → replace placeholders with real art
- Continuous: Agent 6 writes E2E tests for your screens

---

## 📝 NOTES FOR YOU
- **Other agents working**: Agent 1 (simulation), Agent 2 (network), Agent 4 (assets), Agent 5 (backend), Agent 6 (tests)
- **Start with mock data** - `DeckBuilder` can use hardcoded card list until Agent 4 delivers `CardDatabase`
- **Battle HUD first** - it's the most used screen; Agent 1's `BattleTestRunner` can drive it
- **Use EventBus** - don't poll simulation; subscribe to events for real-time updates
- **Performance budget**: Battle HUD < 2ms/frame (see ARCHITECTURE.md Section 5.1)

---

## 📋 QUICK START
```csharp
// In DeckBuilderScreen.cs - test with mock data:
var mockCards = new[] { "Knight", "Archers", "Giant", "Musketeer", "Fireball", "Cannon", "Skeletons", "Minions" };
// Drag-drop to 8 slots → validate → save button enabled

// In BattleHUD - test with BattleTestRunner:
var sim = new BattleSimulation();
sim.Initialize(config, seed, deck1, deck2);
// Subscribe to EventBus.OnElixirChanged → update elixir bar
```

**Good luck! You're building the face of the game.** 🎨