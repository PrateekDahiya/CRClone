# Mother Witch - Card Specification

## Basic Info
- **Card ID**: 26000014 (example)
- **Name**: Mother Witch
- **Rarity**: Legendary
- **Type**: Troop (Ranged)
- **Elixir Cost**: 4
- **Unlock Arena**: Royal Arena (Arena 7)

## Statistics (Tournament Standard / Level 11)
| Stat | Value |
|------|-------|
| Hitpoints | 672 |
| Damage | 128 |
| Hit Speed | 1.5 sec |
| Range | 5.5 tiles |
| Speed | Medium |
| Deploy Time | 1 sec |
| Target | Air & Ground |
| Mechanic | Curses enemies → spawn Cursed Hogs on death (3 hogs, 400 HP each) |

## Mechanics

1. Curses enemies → spawn Cursed Hogs on death (3 hogs, 400 HP each)

## Interactions

### Key Interactions
- Standard interactions for legendary troop (ranged)
- Refer to CARDS_DATABASE.md for detailed interaction chart

## Synergies

- Works well with tank units for protection
- Pairs with splash damage for swarm control
- Complements spell bait strategies

## Visual/Audio

- **Sprite**: Mother Witch character/model
- **Animations**: Idle, Walk, Attack, Hit, Death, Spawn
- **Effects**: Standard troop (ranged) effects
- **Sound**: Character-appropriate audio cues
- **Voice Lines**: Character-specific lines

## Implementation Notes

```csharp
public class MotherWitch : RangedTroop
{
    // Implementation based on card stats
    protected override void InitializeStats()
    {
        MaxHP = 672; // Level 11
        Damage = 128;
        HitSpeed = 1.5 sec;
        Range = 5.5 tiles;
        MoveSpeed = Medium;
        TargetType = TargetType.AirAndGround;
    }
}
```

## Level Scaling

| Level | HP | Damage |
|-------|-----|--------|
| 1 | [Base] | [Base] |
| ... | ... | ... |
| 11 | 672 | 128 |
| 12 | [×1.1] | [×1.1] |
| 13 | [×1.21] | [×1.21] |
| 14 | [×1.33] | [×1.33] |

Formula: `Stat_Lvl_N = Stat_Lvl_1 * 1.1^(N-1)`

## Testing Checklist
- [ ] HP = 672 at tournament standard
- [ ] Damage = 128 per hit
- [ ] Hit speed = 1.5 sec
- [ ] Range = 5.5 tiles
- [ ] Speed = Medium
- [ ] Targets Air & Ground
- [ ] Visual: proper animations and effects
- [ ] Audio: character sounds and voice lines

