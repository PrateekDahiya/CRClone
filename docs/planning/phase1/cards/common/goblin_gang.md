# Goblin Gang - Card Specification

## Basic Info
- **Card ID**: 26000065 (example)
- **Name**: Goblin Gang
- **Rarity**: Common
- **Type**: Troop (Mixed Swarm)
- **Elixir Cost**: 3
- **Unlock Arena**: Royal Arena (Arena 7)

## Statistics (Tournament Standard / Level 11)
| Stat | Value |
|------|-------|
| Deploy Time | 1 sec |
| Count | 6 total |
| Composition | 3 Goblins (melee) + 3 Spear Goblins (ranged) |

## Mechanics

2. Swarm unit - multiple units deployed together

## Interactions

### Key Interactions
- Standard interactions for common troop (mixed swarm)
- Refer to CARDS_DATABASE.md for detailed interaction chart

## Synergies

- Works well with tank units for protection
- Pairs with splash damage for swarm control
- Complements spell bait strategies

## Visual/Audio

- **Sprite**: Goblin Gang character/model
- **Animations**: Idle, Walk, Attack, Hit, Death, Spawn
- **Effects**: Standard troop (mixed swarm) effects
- **Sound**: Character-appropriate audio cues
- **Voice Lines**: Character-specific lines

## Implementation Notes

```csharp
public class GoblinGang : SwarmTroop
{
    // Implementation based on card stats
    protected override void InitializeStats()
    {
        UnitCount = 6 total;
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
- [ ] Spawns 6 total units
- [ ] Visual: proper animations and effects
- [ ] Audio: character sounds and voice lines

