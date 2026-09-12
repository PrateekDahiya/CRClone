# Royal Delivery - Card Specification

## Basic Info
- **Card ID**: 26000049 (example)
- **Name**: Royal Delivery
- **Rarity**: Epic
- **Type**: Spell (Spawn)
- **Elixir Cost**: 3
- **Unlock Arena**: Royal Arena (Arena 7)

## Statistics (Tournament Standard / Level 11)
| Stat | Value |
|------|-------|
| Damage | 256 (drop) |
| Range | Anywhere |
| Radius | 2.5 tiles |
| Mechanic | Drops recruit + damage on landing |
| Spawns | Royal Recruit (832 HP, 136 dmg) |

## Mechanics

1. Drops recruit + damage on landing

## Interactions

### Key Interactions
- Standard interactions for epic spell (spawn)
- Refer to CARDS_DATABASE.md for detailed interaction chart

## Synergies

- Works well with tank units for protection
- Pairs with splash damage for swarm control
- Complements spell bait strategies

## Visual/Audio

- **Sprite**: Royal Delivery character/model
- **Animations**: Idle, Walk, Attack, Hit, Death, Spawn
- **Effects**: Standard spell (spawn) effects
- **Sound**: Character-appropriate audio cues
- **Voice Lines**: Character-specific lines

## Implementation Notes

```csharp
public class RoyalDelivery : SpawnSpell
{
    // Implementation based on card stats
    protected override void InitializeStats()
    {
        Damage = 256 (drop);
        Range = Anywhere;
    }
}
```

## Level Scaling

| Level | HP | Damage |
|-------|-----|--------|
| 1 | [Base] | [Base] |
| ... | ... | ... |
| 11 | [Base] | 256 (drop) |
| 12 | [×1.1] | [×1.1] |
| 13 | [×1.21] | [×1.21] |
| 14 | [×1.33] | [×1.33] |

Formula: `Stat_Lvl_N = Stat_Lvl_1 * 1.1^(N-1)`

## Testing Checklist
- [ ] Damage = 256 (drop) per hit
- [ ] Range = Anywhere
- [ ] Visual: proper animations and effects
- [ ] Audio: character sounds and voice lines

