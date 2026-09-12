# Princess - Card Specification

## Basic Info
- **Card ID**: 26000002 (example)
- **Name**: Princess
- **Rarity**: Legendary
- **Type**: Troop (Ranged)
- **Elixir Cost**: 3
- **Unlock Arena**: Spell Valley (Arena 5)

## Statistics (Tournament Standard / Level 11)
| Stat | Value |
|------|-------|
| Hitpoints | 216 |
| Damage | 140 (x3 arrows = 420 total per attack) |
| Hit Speed | 3 sec |
| Range | 9 tiles (longest range troop) |
| Speed | Medium |
| Deploy Time | 1 sec |
| Target | Air & Ground |
| Mechanic | Splash damage (1.5 tile radius) |

## Mechanics

1. Splash damage (1.5 tile radius)

## Interactions

### Key Interactions
- Standard interactions for legendary troop (ranged)
- Refer to CARDS_DATABASE.md for detailed interaction chart

## Synergies

- Works well with tank units for protection
- Pairs with splash damage for swarm control
- Complements spell bait strategies

## Visual/Audio

- **Sprite**: Princess character/model
- **Animations**: Idle, Walk, Attack, Hit, Death, Spawn
- **Effects**: Standard troop (ranged) effects
- **Sound**: Character-appropriate audio cues
- **Voice Lines**: Character-specific lines

## Implementation Notes

```csharp
public class Princess : RangedTroop
{
    // Implementation based on card stats
    protected override void InitializeStats()
    {
        MaxHP = 216; // Level 11
        Damage = 140 (x3 arrows = 420 total per attack);
        HitSpeed = 3 sec;
        Range = 9 tiles (longest range troop);
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
| 11 | 216 | 140 (x3 arrows = 420 total per attack) |
| 12 | [×1.1] | [×1.1] |
| 13 | [×1.21] | [×1.21] |
| 14 | [×1.33] | [×1.33] |

Formula: `Stat_Lvl_N = Stat_Lvl_1 * 1.1^(N-1)`

## Testing Checklist
- [ ] HP = 216 at tournament standard
- [ ] Damage = 140 (x3 arrows = 420 total per attack) per hit
- [ ] Hit speed = 3 sec
- [ ] Range = 9 tiles (longest range troop)
- [ ] Speed = Medium
- [ ] Targets Air & Ground
- [ ] Visual: proper animations and effects
- [ ] Audio: character sounds and voice lines

