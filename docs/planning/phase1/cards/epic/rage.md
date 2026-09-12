# Rage - Card Specification

## Basic Info
- **Card ID**: 26000040 (example)
- **Name**: Rage
- **Rarity**: Epic
- **Type**: Spell
- **Elixir Cost**: 2
- **Unlock Arena**: P.E.K.K.A's Playhouse (Arena 4)

## Statistics (Tournament Standard / Level 11)
| Stat | Value |
|------|-------|
| Range | Anywhere |
| Radius | 3.5 tiles |
| Duration | 6 sec |
| Effect | +50% attack speed, +30% move speed |

## Mechanics

2. Spell - instant or delayed effect, can be placed anywhere

## Interactions

### Key Interactions
- Standard interactions for epic spell
- Refer to CARDS_DATABASE.md for detailed interaction chart

## Synergies

- Works well with tank units for protection
- Pairs with splash damage for swarm control
- Complements spell bait strategies

## Visual/Audio

- **Sprite**: Rage character/model
- **Animations**: Idle, Walk, Attack, Hit, Death, Spawn
- **Effects**: Standard spell effects
- **Sound**: Character-appropriate audio cues
- **Voice Lines**: Character-specific lines

## Implementation Notes

```csharp
public class Rage : Spell
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

