# Goblin Drill - Card Specification

## Basic Info
- **Card ID**: 26000074 (example)
- **Name**: Goblin Drill
- **Rarity**: Epic
- **Type**: Building (Spawner/Burrowing)
- **Elixir Cost**: 4
- **Unlock Arena**: Royal Arena (Arena 7)

## Statistics (Tournament Standard / Level 11)
| Stat | Value |
|------|-------|
| Hitpoints | 1200 (drill), Goblin: 264 HP, 104 dmg |
| Lifetime | 30 sec or 3 waves |
| Mechanic | Burrows underground, invulnerable while moving, spawns at target |
| Spawn | 2 Goblins per wave (3 waves) |

## Mechanics

1. Burrows underground, invulnerable while moving, spawns at target
2. Building - stationary, has lifetime, targets by path distance
3. Death spawn / periodic spawn mechanic

## Interactions

### Key Interactions
- Standard interactions for epic building (spawner/burrowing)
- Refer to CARDS_DATABASE.md for detailed interaction chart

## Synergies

- Works well with tank units for protection
- Pairs with splash damage for swarm control
- Complements spell bait strategies

## Visual/Audio

- **Sprite**: Goblin Drill character/model
- **Animations**: Idle, Walk, Attack, Hit, Death, Spawn
- **Effects**: Standard building (spawner/burrowing) effects
- **Sound**: Character-appropriate audio cues
- **Voice Lines**: Character-specific lines

## Implementation Notes

```csharp
public class GoblinDrill : SpawnerBuilding
{
    // Implementation based on card stats
    protected override void InitializeStats()
    {
        MaxHP = 1200 (drill), Goblin: 264 HP, 104 dmg; // Level 11
        Lifetime = 30 sec or 3 waves;
    }
}
```

## Level Scaling

| Level | HP | Damage |
|-------|-----|--------|
| 1 | [Base] | [Base] |
| ... | ... | ... |
| 11 | 1200 (drill), Goblin: 264 HP, 104 dmg | [Base] |
| 12 | [×1.1] | [×1.1] |
| 13 | [×1.21] | [×1.21] |
| 14 | [×1.33] | [×1.33] |

Formula: `Stat_Lvl_N = Stat_Lvl_1 * 1.1^(N-1)`

## Testing Checklist
- [ ] HP = 1200 (drill), Goblin: 264 HP, 104 dmg at tournament standard
- [ ] Lifetime = 30 sec or 3 waves
- [ ] Visual: proper animations and effects
- [ ] Audio: character sounds and voice lines

