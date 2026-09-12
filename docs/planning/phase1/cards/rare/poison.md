# Poison - Card Specification

## Basic Info
- **Card ID**: 26000034 (example)
- **Name**: Poison
- **Rarity**: Rare
- **Type**: Spell
- **Elixir Cost**: 4
- **Unlock Arena**: Royal Arena (Arena 7)

## Statistics (Tournament Standard / Level 11)
| Stat | Value |
|------|-------|
| Damage | 65/sec × 8 sec = 520 total |
| Range | Anywhere |
| Radius | 3.5 tiles |
| Duration | 8 sec |
| Mechanic | Damage over time, slows 35% |

## Mechanics

1. Damage over time, slows 35%
2. Spell - instant or delayed effect, can be placed anywhere

## Interactions

### Key Interactions
- Standard interactions for rare spell
- Refer to CARDS_DATABASE.md for detailed interaction chart

## Synergies

- Works well with tank units for protection
- Pairs with splash damage for swarm control
- Complements spell bait strategies

## Visual/Audio

- **Sprite**: Poison character/model
- **Animations**: Idle, Walk, Attack, Hit, Death, Spawn
- **Effects**: Standard spell effects
- **Sound**: Character-appropriate audio cues
- **Voice Lines**: Character-specific lines

## Implementation Notes

```csharp
public class Poison : Spell
{
    // Implementation based on card stats
    protected override void InitializeStats()
    {
        Damage = 65/sec × 8 sec = 520 total;
        Range = Anywhere;
    }
}
```

## Level Scaling

| Level | HP | Damage |
|-------|-----|--------|
| 1 | [Base] | [Base] |
| ... | ... | ... |
| 11 | [Base] | 65/sec × 8 sec = 520 total |
| 12 | [×1.1] | [×1.1] |
| 13 | [×1.21] | [×1.21] |
| 14 | [×1.33] | [×1.33] |

Formula: `Stat_Lvl_N = Stat_Lvl_1 * 1.1^(N-1)`

## Testing Checklist
- [ ] Damage = 65/sec × 8 sec = 520 total per hit
- [ ] Range = Anywhere
- [ ] Visual: proper animations and effects
- [ ] Audio: character sounds and voice lines

