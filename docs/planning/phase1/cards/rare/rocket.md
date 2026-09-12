# Rocket - Card Specification

## Basic Info
- **Card ID**: 26000032 (example)
- **Name**: Rocket
- **Rarity**: Rare
- **Type**: Spell
- **Elixir Cost**: 6
- **Unlock Arena**: Royal Arena (Arena 7)

## Statistics (Tournament Standard / Level 11)
| Stat | Value |
|------|-------|
| Damage | 1080 (area 2 tiles) |
| Range | Anywhere |
| Radius | 2 tiles |
| Travel Time | ~1.5 sec |
| Mechanic | High damage, slow travel, 2 tile radius |

## Mechanics

1. High damage, slow travel, 2 tile radius
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

- **Sprite**: Rocket character/model
- **Animations**: Idle, Walk, Attack, Hit, Death, Spawn
- **Effects**: Standard spell effects
- **Sound**: Character-appropriate audio cues
- **Voice Lines**: Character-specific lines

## Implementation Notes

```csharp
public class Rocket : Spell
{
    // Implementation based on card stats
    protected override void InitializeStats()
    {
        Damage = 1080 (area 2 tiles);
        Range = Anywhere;
    }
}
```

## Level Scaling

| Level | HP | Damage |
|-------|-----|--------|
| 1 | [Base] | [Base] |
| ... | ... | ... |
| 11 | [Base] | 1080 (area 2 tiles) |
| 12 | [×1.1] | [×1.1] |
| 13 | [×1.21] | [×1.21] |
| 14 | [×1.33] | [×1.33] |

Formula: `Stat_Lvl_N = Stat_Lvl_1 * 1.1^(N-1)`

## Testing Checklist
- [ ] Damage = 1080 (area 2 tiles) per hit
- [ ] Range = Anywhere
- [ ] Visual: proper animations and effects
- [ ] Audio: character sounds and voice lines

