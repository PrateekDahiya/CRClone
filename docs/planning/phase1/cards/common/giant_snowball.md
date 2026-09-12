# Giant Snowball - Card Specification

## Basic Info
- **Card ID**: 26000057 (example)
- **Name**: Giant Snowball
- **Rarity**: Common
- **Type**: Spell
- **Elixir Cost**: 2
- **Unlock Arena**: Royal Arena (Arena 7)

## Statistics (Tournament Standard / Level 11)
| Stat | Value |
|------|-------|
| Damage | 156 + knockback |
| Range | Anywhere |
| Radius | 2.5 tiles |
| Travel Time | ~0.8 sec |
| Mechanic | Knockback + slow 35% for 2s |

## Mechanics

1. Knockback + slow 35% for 2s
2. Spell - instant or delayed effect, can be placed anywhere

## Interactions

### Key Interactions
- Standard interactions for common spell
- Refer to CARDS_DATABASE.md for detailed interaction chart

## Synergies

- Works well with tank units for protection
- Pairs with splash damage for swarm control
- Complements spell bait strategies

## Visual/Audio

- **Sprite**: Giant Snowball character/model
- **Animations**: Idle, Walk, Attack, Hit, Death, Spawn
- **Effects**: Standard spell effects
- **Sound**: Character-appropriate audio cues
- **Voice Lines**: Character-specific lines

## Implementation Notes

```csharp
public class GiantSnowball : Spell
{
    // Implementation based on card stats
    protected override void InitializeStats()
    {
        Damage = 156 + knockback;
        Range = Anywhere;
    }
}
```

## Level Scaling

| Level | HP | Damage |
|-------|-----|--------|
| 1 | [Base] | [Base] |
| ... | ... | ... |
| 11 | [Base] | 156 + knockback |
| 12 | [×1.1] | [×1.1] |
| 13 | [×1.21] | [×1.21] |
| 14 | [×1.33] | [×1.33] |

Formula: `Stat_Lvl_N = Stat_Lvl_1 * 1.1^(N-1)`

## Testing Checklist
- [ ] Damage = 156 + knockback per hit
- [ ] Range = Anywhere
- [ ] Visual: proper animations and effects
- [ ] Audio: character sounds and voice lines

