# Clash Royale Clone - UI/UX Specification

## 1. SCREEN FLOW & NAVIGATION

### 1.1 Main Navigation Structure
```
┌─────────────────────────────────────────────────────────────┐
│                    MAIN MENU (Lobby)                        │
├─────────────────────────────────────────────────────────────┤
│  [Events] [Clan] [Shop] [Cards] [Battle] [Profile]         │
└─────────────────────────────────────────────────────────────┘
         │           │         │        │         │
         ▼           ▼         ▼        ▼         ▼
    ┌────────┐  ┌────────┐ ┌──────┐ ┌────────┐ ┌────────┐
    │Events  │  │ Clan   │ │ Shop │ │ Deck   │ │Profile │
    │Screen  │  │ Screen │ │Screen│ │Builder │ │Screen  │
    └────────┘  └────────┘ └──────┘ └────────┘ └────────┘
                    │                  │
                    ▼                  ▼
             ┌──────────┐        ┌──────────┐
             │ Clan War │        │ Card     │
             │ /Capital │        │ Details  │
             └──────────┘        └──────────┘
```

### 1.2 Battle Flow
```
┌─────────────┐     ┌─────────────┐     ┌─────────────┐
│ Matchmaking │ ──▶ │   Battle    │ ──▶ │   Result    │
│   Screen    │     │   (3 min)   │     │   Screen    │
└─────────────┘     └─────────────┘     └─────────────┘
                           │                    │
                           ▼                    ▼
                    ┌─────────────┐     ┌─────────────┐
                    │  Pause/     │     │  Replay/    │
                    │  Settings   │     │  Share      │
                    └─────────────┘     └─────────────┘
```

---

## 2. SCREEN SPECIFICATIONS

### 2.1 Main Menu (Lobby)
**Resolution**: 1920×1080 (scales to device)
**Orientation**: Landscape only

**Layout Zones**:
```
┌─────────────────────────────────────────────────────────────┐
│ TOP BAR (80px)                                              │
│ [Player Avatar] [Name] [Trophies] [Gems] [Gold] [Settings] │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  CENTER: Battle Button (Large, Animated)                    │
│  [1v1] [2v2] [Tournament] [Friendly] [Practice]            │
│                                                             │
├─────────────────────────────────────────────────────────────┤
│ BOTTOM NAV (120px)                                          │
│ [Events] [Clan] [Shop] [Cards] [Battle★] [Profile]         │
└─────────────────────────────────────────────────────────────┘
```

**Key Elements**:
- **Battle Button**: Pulsing, primary CTA, 200×200px
- **Chest Slots**: 4 slots above battle button (if space)
- **Quest Bar**: Horizontal scroll under battle button
- **News/Events Banner**: Top center, dismissible

### 2.2 Deck Builder Screen
**Layout**:
```
┌─────────────────────────────────────────────────────────────┐
│ TOP BAR: [← Back] Deck Builder [Save] [Copy Link]          │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  DECK PREVIEW (Left 60%)                                    │
│  ┌─────────────────────────────────────────────────────┐   │
│  │ [Card1] [Card2] [Card3] [Card4]                     │   │
│  │ [Card5] [Card6] [Card7] [Card8]                     │   │
│  │ Avg Elixir: 3.6  |  Champion: ✓  |  Rarity Balance  │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                             │
│  CARD COLLECTION (Right 40% - Scrollable)                   │
│  ┌─────────────────────────────────────────────────────┐   │
│  │ Filters: [All] [Troop] [Spell] [Building] [Champion]│   │
│  │ Rarity: [★] [★★] [★★★] [★★★★] [Champion]            │   │
│  │ Search: [________________]                          │   │
│  │                                                     │   │
│  │ [Card] [Card] [Card] [Card]  (Grid 4×N)            │   │
│  │ [Card] [Card] [Card] [Card]                        │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

**Interactions**:
- Drag card from collection → deck slot
- Click deck slot → remove card
- Click card in collection → show Card Details modal
- Hover/Long press → quick stats tooltip
- **Validation**: Real-time (8 cards, 1 champion max, elixir avg warning)

### 2.3 Card Details Modal
```
┌─────────────────────────────────────────────────────────────┐
│ [×]  CARD NAME                    Rarity: ★★★★  Level: 11  │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  LARGE CARD ART (Animated)                                  │
│                                                             │
│  STATS GRID:                                                │
│  ┌─────────┬─────────┬─────────┬─────────┐                 │
│  │ Elixir  │   HP    │ Damage  │  DPS    │                 │
│  │    4    │  1352   │  156×2  │  173    │                 │
│  ├─────────┼─────────┼─────────┼─────────┤                 │
│  │ Hit Spd │ Range   │ Speed   │ Target  │                 │
│  │  1.8s   │  3.5    │  Fast   │ Air/Grd │                 │
│  └─────────┴─────────┴─────────┴─────────┘                 │
│                                                             │
│  MECHANICS:                                                 │
│  • Splash damage (1.5 tile radius)                          │
│  • Flying unit                                              │
│  • Targets air & ground                                     │
│                                                             │
│  COUNTERS: [Wizard] [Musketeer] [Executioner] [Arrows]     │
│  SYNERGIES: [Tank] [Building] [Spell Bait]                  │
│                                                             │
│  UPGRADE PATH:                                              │
│  Level 11 → 12:  20 cards + 50,000 gold                    │
│  Level 12 → 13:  50 cards + 100,000 gold                   │
│                                                             │
│  [Upgrade] [Add to Deck] [View in Shop]                    │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### 2.4 Battle Screen (Core Gameplay)
**CRITICAL**: This is the main gameplay screen - must be optimized

```
┌─────────────────────────────────────────────────────────────┐
│ TOP BAR (60px)                                              │
│ [Pause]                    [Timer: 3:00]                    │
│ [P1 Name] [P1 Crowns] ████████ [P2 Crowns] [P2 Name]       │
│ [P1 King HP]              VS              [P2 King HP]      │
├─────────────────────────────────────────────────────────────│
│                                                             │
│                    ARENA (Game World)                       │
│                                                             │
│  ┌─────────────────────────────────────────────────────┐   │
│  │                                                     │   │
│  │  ENEMY SIDE                                         │   │
│  │  [King Tower] [Princess] [Princess]                 │   │
│  │                                                     │   │
│  │  ────────────── RIVER ──────────────                │   │
│  │                                                     │   │
│  │  PLAYER SIDE                                        │   │
│  │  [Princess] [Princess] [King Tower]                 │   │
│  │                                                     │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                             │
├─────────────────────────────────────────────────────────────┤
│ HAND BAR (140px) - FIXED BOTTOM                             │
│                                                             │
│  ELIXIR: ██████████  (10 segments, animated fill)          │
│                                                             │
│  [Card1] [Card2] [Card3] [Card4]  [Next: Card5]           │
│   3■      4■      2■      5■       (grayed, small)         │
│  ████    ████    ████    ████                                │
│                                                             │
│  Card States:                                               │
│  • Affordable: Full color, elixir cost white               │
│  • Unaffordable: Desaturated, elixir cost red              │
│  • Selected: Glow border, enlarged, follows cursor         │
│  • Cooldown: Gray overlay with timer (mirror/clone)        │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

**Battle Screen Key Behaviors**:
- **Card Selection**: Tap/click → card follows cursor → tap valid spot → deploy
- **Drag Deploy**: Hold + drag to position → release to deploy
- **Spell Targeting**: Shows radius preview at cursor
- **Invalid Placement**: Red indicator, shake feedback
- **Elixir Pulse**: Subtle animation on elixir gain
- **Tower Target Laser**: Thin line from tower to target

### 2.5 Battle Result Screen
```
┌─────────────────────────────────────────────────────────────┐
│ VICTORY / DEFEAT / DRAW (Large, Animated)                   │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  CROWNS:  [★] [★] [☆]    vs    [★] [☆] [☆]                 │
│                                                             │
│  TROPHY CHANGE:  +32  /  -32                               │
│                                                             │
│  REWARDS:                                                   │
│  ┌─────────────────────────────────────────────────────┐   │
│  │ [Chest: Golden]  [Gold: 1,240]  [XP: 450]          │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                             │
│  BATTLE LOG:                                                │
│  [Timeline with key events: first blood, tower down, etc.] │
│                                                             │
│  [Watch Replay] [Share] [Rematch] [Back to Lobby]          │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### 2.6 Shop Screen
```
┌─────────────────────────────────────────────────────────────┐
│ TOP BAR: [←] Shop [Gems: 1,250] [Gold: 50,000]             │
├─────────────────────────────────────────────────────────────┤
│ TABS: [Daily] [Special] [Chests] [Gems] [Wild Cards]       │
│                                                             │
│ DAILY OFFERS (Refreshes daily):                             │
│ ┌─────────┐ ┌─────────┐ ┌─────────┐ ┌─────────┐            │
│ │ Card    │ │ Card    │ │ Gold    │ │ Gem     │            │
│ │ Offer   │ │ Offer   │ │ Offer   │ │ Offer   │            │
│ │         │ │         │ │         │ │         │            │
│ │ [Buy]   │ │ [Buy]   │ │ [Buy]   │ │ [Buy]   │            │
│ └─────────┘ └─────────┘ └─────────┘ └─────────┘            │
│                                                             │
│ SPECIAL OFFERS (Limited time):                              │
│ [Large banner offers with timer]                            │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### 2.7 Clan Screen
```
┌─────────────────────────────────────────────────────────────┐
│ TOP BAR: [←] Clan Name [Trophy Req] [Members: 42/50]       │
├─────────────────────────────────────────────────────────────┤
│ TABS: [Chat] [Members] [War] [Capital] [Settings]          │
│                                                             │
│ CHAT TAB:                                                   │
│ ┌─────────────────────────────────────────────────────┐   │
│ │ [System] Welcome to clan!                    10:30  │   │
│ │ [Player] Anyone want to friendly?               10:31  │   │
│ │ [Player] [Replay Link] Check this!              10:32  │   │
│ │ [Donation] Player requested Giant               10:33  │   │
│ │    [Donate Button]                                 │   │
│ └─────────────────────────────────────────────────────┘   │
│ [Message Input] [Send] [Donate Request]                    │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

---

## 3. INTERACTION PATTERNS

### 3.1 Card Deployment Flow
```
User taps card in hand
       │
       ▼
Card enlarges, follows cursor/finger
       │
       ▼
Show deploy zone highlight (green valid, red invalid)
       │
       ▼
For spells: Show radius preview at cursor
       │
       ▼
User taps valid location
       │
       ▼
✓ Play deploy sound
✓ Deduct elixir
✓ Spawn unit at location
✓ Draw next card
✓ Card goes to graveyard (cycle)
```

### 3.2 Card Selection States
| State | Visual | Interaction |
|-------|--------|-------------|
| Default | Normal size, full color | Tap to select |
| Hover (PC) | Slight scale (1.05x), glow | - |
| Selected | Scale 1.2x, border glow, follows cursor | Drag to aim, tap to place |
| Unaffordable | Desaturated, red elixir cost | Tap = shake, no select |
| Deploying | Spawn animation at position | - |
| Cooldown | Gray overlay, timer text | Cannot select |

### 3.3 Touch Controls (Mobile)
- **Tap Card**: Select
- **Drag**: Aim placement
- **Release**: Deploy (if valid)
- **Cancel Drag**: Drag back to hand bar → release
- **Two-finger Pan**: Camera movement (spectator/replay)
- **Pinch**: Zoom (spectator/replay)

### 3.4 Keyboard/Mouse Controls (PC)
- **1-4 Keys**: Select card 1-4
- **Mouse Move**: Aim
- **Left Click**: Deploy
- **Right Click / ESC**: Cancel selection
- **Space**: Pause (single player)
- **F1-F4**: Quick emotes

---

## 4. VISUAL DESIGN SYSTEM

### 4.1 Color Palette
```css
/* Primary */
--cr-gold: #FFD700;
--cr-gold-dark: #C5A600;
--cr-blue: #007AFF;
--cr-blue-dark: #0056CC;
--cr-red: #FF3B30;
--cr-red-dark: #CC2E26;

/* Background */
--cr-bg-dark: #0D1B2A;
--cr-bg-medium: #1B2A3A;
--cr-bg-light: #2A3F54;

/* UI */
--cr-card-common: #9E9E9E;
--cr-card-rare: #2196F3;
--cr-card-epic: #9C27B0;
--cr-card-legendary: #FF9800;
--cr-card-champion: #E91E63;

/* Text */
--cr-text-primary: #FFFFFF;
--cr-text-secondary: #B0BEC5;
--cr-text-disabled: #607D8B;

/* Status */
--cr-health-green: #4CAF50;
--cr-health-yellow: #FFC107;
--cr-health-red: #F44336;
--cr-elixir: #00E5FF;
```

### 4.2 Typography
- **Primary Font**: "Supercell Magic" (or similar rounded sans-serif)
- **Heading**: 32px, Bold
- **Subheading**: 20px, Medium
- **Body**: 16px, Regular
- **Small**: 12px, Regular
- **Numbers (Stats)**: Tabular figures, 18px Medium

### 4.3 Iconography
- **Style**: Flat, rounded, consistent stroke (2px)
- **Size**: 24×24px base, scales with context
- **Categories**: Elixir, HP, Damage, Speed, Range, Target, Rarity

### 4.4 Rarity Visual Language
| Rarity | Frame Color | Glow | Particles |
|--------|-------------|------|-----------|
| Common | Gray | None | None |
| Rare | Blue | Subtle pulse | Blue sparkles |
| Epic | Purple | Medium pulse | Purple swirl |
| Legendary | Orange/Gold | Strong pulse | Golden particles |
| Champion | Pink/Magenta | Intense pulse | Rainbow shimmer |

---

## 5. ANIMATION SPECIFICATIONS

### 5.1 Battle Screen Animations
| Element | Animation | Duration | Easing |
|---------|-----------|----------|--------|
| Card Select | Scale 1→1.2, glow fade in | 150ms | ease-out |
| Card Deploy | Scale 1.2→0, fade, spawn at pos | 200ms | ease-in |
| Elixir Gain | Segment fill + pulse | 200ms | ease-out |
| Tower Hit | Shake + flash red | 100ms | ease-out |
| Tower Destroy | Collapse + explosion | 2000ms | custom |
| Spell Cast | Windup → effect → fade | 300-1000ms | varies |
| Unit Death | Ragdoll/fade + particles | 500ms | ease-in |
| Card Draw | Slide from right + flip | 300ms | ease-out |

### 5.2 UI Transitions
- **Screen Transition**: Slide horizontal (300ms, ease-in-out)
- **Modal Open**: Fade + scale (200ms, ease-out)
- **Modal Close**: Fade + scale (150ms, ease-in)
- **Tab Switch**: Crossfade (150ms)
- **List Scroll**: Momentum scroll, 60fps

### 5.3 Idle/Ambient Animations
- **Lobby Battle Button**: Pulse scale (1.0→1.05, 2s loop)
- **Chest Slots**: Gentle bounce (when ready)
- **Card Art (Collection)**: Subtle breathing (idle animation)
- **River Water**: Continuous flow shader
- **Torches/Flags**: Wind sway

---

## 6. RESPONSIVE DESIGN

### 6.1 Breakpoints
| Device | Width | Scale | Layout Adjustments |
|--------|-------|-------|-------------------|
| Mobile Portrait | < 768px | 0.8x | Stack elements, larger touch targets |
| Mobile Landscape | 768-1024px | 1.0x | Standard |
| Tablet | 1024-1366px | 1.1x | Extra padding, larger cards |
| Desktop | 1366-1920px | 1.2x | Side panels, hover states |
| Large Desktop | > 1920px | 1.3x | Max width container, centered |

### 6.2 Safe Areas
- **Notch/Dynamic Island**: 44px top, 34px bottom padding
- **Home Indicator**: 34px bottom
- **System Gestures**: Avoid bottom 48px for critical actions

---

## 7. ACCESSIBILITY

### 7.1 Visual
- **Color Blind**: Patterns + colors for rarity (not color-only)
- **High Contrast Mode**: Increased contrast ratios
- **Text Scaling**: Support 100%-200% system scaling
- **Reduce Motion**: Disable non-essential animations

### 7.2 Audio
- **Volume Sliders**: Master, Music, SFX, Voice
- **Mute Toggle**: Quick mute
- **Subtitles**: For announcer/voice lines

### 7.3 Input
- **Remappable Keys**: Full keyboard customization
- **Gamepad Support**: Full navigation + battle controls
- **Touch Alternatives**: All drag actions have tap alternative

---

## 8. LOCALIZATION

### 8.1 Supported Languages
1. English (Base)
2. Chinese (Simplified)
3. Chinese (Traditional)
4. Korean
5. Japanese
6. Spanish (ES/LATAM)
7. Portuguese (BR)
8. French
9. German
10. Russian
11. Turkish
12. Arabic
13. Thai
14. Vietnamese
15. Indonesian

### 8.2 Localization Notes
- **Text Expansion**: UI fits 30% longer text
- **Font Fallbacks**: Noto Sans for CJK, system fonts
- **RTL Support**: Arabic (mirror layouts)
- **Number Formats**: Localized decimals, thousands
- **Date/Time**: Local formats

---

## 9. PERFORMANCE BUDGETS

### 9.1 Battle Screen (60 FPS Target)
| Component | Budget |
|-----------|--------|
| Game Simulation | 4ms |
| Rendering | 8ms |
| UI Overlay | 2ms |
| Audio | 1ms |
| Network | 1ms |
| **Total** | **< 16.67ms** |

### 9.2 Memory
- **Textures**: < 512MB (compressed)
- **Audio**: < 100MB
- **Heap**: < 200MB

### 9.3 Load Times
- **Initial Load**: < 10s
- **Battle Load**: < 3s
- **Screen Transitions**: < 500ms

---

## 10. ASSET SPECIFICATIONS

### 10.1 Card Art
- **Portrait**: 512×512 (source), 256×256 (runtime)
- **Landscape (Battle)**: 1024×512 (source), 512×256 (runtime)
- **Format**: ASTC (mobile), DXT/BC7 (PC)
- **Animation**: 30fps, 1-2 sec loops (idle, attack, death)

### 10.2 UI Assets
- **Atlases**: 2048×2048 max
- **Format**: ASTC 4×4 / BC7
- **9-slice**: For scalable panels/buttons
- **Icons**: 128×128 source, multiple mips

### 10.3 Battle Assets
- **Units**: Sprite sheets (8-16 frames per anim)
- **Buildings**: 3 states (idle, attacking, destroyed)
- **Projectiles**: Single frame + trail renderer
- **Spells**: Particle systems (not sprite sheets)
- **Towers**: 4 states (idle, attacking, damaged, destroyed)

---

## 11. SETTINGS / OPTIONS

### 11.1 Game Settings
- **Graphics**: Low / Medium / High / Ultra
- **Frame Rate**: 30 / 60 / 120 / Unlimited
- **VSync**: On / Off
- **Resolution**: Native / Scaled
- **Fullscreen**: Borderless / Exclusive / Windowed

### 11.2 Gameplay Settings
- **Card Deploy**: Tap-to-place / Drag-to-place / Both
- **Spell Aiming**: Cursor / Drag / Hybrid
- **Auto-Target**: On / Off (for accessibility)
- **Camera Shake**: On / Off
- **Damage Numbers**: On / Off / Critical Only

### 11.3 Audio Settings
- **Master Volume**: 0-100%
- **Music Volume**: 0-100%
- **SFX Volume**: 0-100%
- **Voice Volume**: 0-100%
- **Mute on Focus Loss**: On / Off

---

## 12. ERROR STATES & EMPTY STATES

### 12.1 Network Errors
- **Connection Lost**: Reconnecting overlay (max 10s)
- **Matchmaking Timeout**: Retry / Cancel
- **Desync Detected**: "Resyncing..." with progress

### 12.2 Empty States
- **No Clan**: "Join or Create Clan" CTA
- **No Friends**: "Add Friends" CTA
- **No Replays**: "Play a battle to see replays"
- **Shop Empty**: "Check back tomorrow"

### 12.3 Validation Errors
- **Deck Invalid**: Inline error messages
- **Name Taken**: Inline with suggestion
- **Insufficient Resources**: Toast + highlight

---

## 13. ONBOARDING / TUTORIAL

### 13.1 Tutorial Flow
1. **Welcome**: Basic movement + deploy
2. **Elixir**: Wait for elixir, deploy Knight
3. **Combat**: Defend against Goblin Gang
4. **Spells**: Use Fireball on tower
5. **Buildings**: Place Cannon
6. **Win Condition**: Take tower
7. **Deck Building**: Build first deck
8. **First Multiplayer**: Matchmade bot match

### 13.2 Tutorial UI
- **Hand Arrow**: Points to current action
- **Highlight**: Dim everything except target
- **Text Bubble**: Short instruction (1-2 lines)
- **Skip Button**: Top right (after step 3)

---

## 14. METRICS / ANALYTICS HOOKS

### 14.1 Key Events to Track
- `battle_start` (mode, deck_hash)
- `card_played` (card, position, elixir, time)
- `spell_cast` (card, targets_hit, value)
- `tower_damaged` (tower, damage, remaining_hp)
- `tower_destroyed` (tower, time)
- `battle_end` (result, crowns, duration)
- `deck_save` (deck_hash)
- `card_upgrade` (card, from_level, to_level)
- `shop_purchase` (item, currency, amount)
- `clan_join` / `clan_create`

---

*This UI/UX spec covers all major screens and interactions. Each screen should have detailed component specs and state diagrams for implementation.*