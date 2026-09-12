# Goblin Barrel - Card Specification

## Basic Info
- **Card ID**: 26000029 (example)
- **Name**: Goblin Barrel
- **Rarity**: Epic
- **Type**: Spell (Spawn)
- **Elixir Cost**: 3
- **Unlock Arena**: P.E.K.K.A's Playhouse (Arena 4)

## Statistics (Tournament Standard / Level 11)
| Stat | Value |
|------|-------|
| Range | Anywhere |
| Deploy Time | 1 sec |
| Mechanic | Throws barrel anywhere, spawns 3 goblins on impact |
| Spawns | 3 Goblins (264 HP, 104 dmg each) |

## Mechanics

1. Throws barrel anywhere, spawns 3 goblins on impact

## Interactions

### Key Interactions
- Standard interactions for epic spell (spawn)
- Refer to CARDS_DATABASE.md for detailed interaction chart

## Synergies

- Works well with tank units for protection
- Pairs with splash damage for swarm control
- Complements spell bait strategies

## Visual/Audio

- **Sprite**: Goblin Barrel character/model
- **Animations**: Idle, Walk, Attack, Hit, Death, Spawn
- **Effects**: Standard spell (spawn) effects
- **Sound**: Character-appropriate audio cues
- **Voice Lines**: Character-specific lines

## Implementation Notes

```csharp
public class GoblinBarrel : SpawnSpell
{
    // Implementation based on card stats
    protected override void InitializeStats()
    {
        Range = Anywhere;
    }
}
```

## Level Scaling

| Level | HP | Damage |
|-------|-----|--------|
| 1 | [Base] | [Base] |
| ... | ... | ... |
| 11 | [Base] | [Base] |
| 12 | [×1.1] | [×1.1] |
| 13 | [×1.21] | [×1.21] |
| 14 | [×1.33] | [×1.33] |

Formula: `Stat_Lvl_N = Stat_Lvl_1 * 1.1^(N-1)`

## Testing Checklist
- [ ] Range = Anywhere
- [ ] Visual: proper animations and effects
- [ ] Audio: character sounds and voice lines

