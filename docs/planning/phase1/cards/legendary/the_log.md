# The Log - Card Specification

## Basic Info
- **Card ID**: 26000001 (example)
- **Name**: The Log
- **Rarity**: Legendary
- **Type**: Spell
- **Elixir Cost**: 2
- **Unlock Arena**: Spell Valley (Arena 5)

## Statistics (Tournament Standard / Level 11)
| Stat | Value |
|------|-------|
| Damage | 240 (Tournament: 240) |
| Range | 11.5 tiles (ground only) |
| Knockback | 0.5 tiles |
| Width | 11.5 tiles |
| Mechanic | Pushes back ground units, does not affect air |

## Mechanics

1. Pushes back ground units, does not affect air
2. Spell - instant or delayed effect, can be placed anywhere

## Interactions

### Key Interactions
- Standard interactions for legendary spell
- Refer to CARDS_DATABASE.md for detailed interaction chart

## Synergies

- Works well with tank units for protection
- Pairs with splash damage for swarm control
- Complements spell bait strategies

## Visual/Audio

- **Sprite**: The Log character/model
- **Animations**: Idle, Walk, Attack, Hit, Death, Spawn
- **Effects**: Standard spell effects
- **Sound**: Character-appropriate audio cues
- **Voice Lines**: Character-specific lines

## Implementation Notes

```csharp
public class TheLog : Spell
{
    // Implementation based on card stats
    protected override void InitializeStats()
    {
        Damage = 240 (Tournament: 240);
        Range = 11.5 tiles (ground only);
    }
}
```

## Level Scaling

| Level | HP | Damage |
|-------|-----|--------|
| 1 | [Base] | [Base] |
| ... | ... | ... |
| 11 | [Base] | 240 (Tournament: 240) |
| 12 | [×1.1] | [×1.1] |
| 13 | [×1.21] | [×1.21] |
| 14 | [×1.33] | [×1.33] |

Formula: `Stat_Lvl_N = Stat_Lvl_1 * 1.1^(N-1)`

## Testing Checklist
- [ ] Damage = 240 (Tournament: 240) per hit
- [ ] Range = 11.5 tiles (ground only)
- [ ] Visual: proper animations and effects
- [ ] Audio: character sounds and voice lines

