# Magic Archer - Card Specification

## Basic Info
- **Card ID**: 26000017 (example)
- **Name**: Magic Archer
- **Rarity**: Legendary
- **Type**: Troop (Ranged/Piercing)
- **Elixir Cost**: 4
- **Unlock Arena**: Royal Arena (Arena 7)

## Statistics (Tournament Standard / Level 11)
| Stat | Value |
|------|-------|
| Hitpoints | 536 |
| Damage | 132 |
| Hit Speed | 1.2 sec |
| Range | 7 tiles (arrow travels 10 tiles) |
| Speed | Medium |
| Deploy Time | 1 sec |
| Target | Air & Ground |
| Mechanic | Arrows pierce through enemies (infinite pierce) |

## Mechanics

1. Arrows pierce through enemies (infinite pierce)
3. Piercing projectiles - arrows pass through multiple enemies

## Interactions

### Key Interactions
- Standard interactions for legendary troop (ranged/piercing)
- Refer to CARDS_DATABASE.md for detailed interaction chart

## Synergies

- Works well with tank units for protection
- Pairs with splash damage for swarm control
- Complements spell bait strategies

## Visual/Audio

- **Sprite**: Magic Archer character/model
- **Animations**: Idle, Walk, Attack, Hit, Death, Spawn
- **Effects**: Standard troop (ranged/piercing) effects
- **Sound**: Character-appropriate audio cues
- **Voice Lines**: Character-specific lines

## Implementation Notes

```csharp
public class MagicArcher : RangedTroop
{
    // Implementation based on card stats
    protected override void InitializeStats()
    {
        MaxHP = 536; // Level 11
        Damage = 132;
        HitSpeed = 1.2 sec;
        Range = 7 tiles (arrow travels 10 tiles);
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
| 11 | 536 | 132 |
| 12 | [×1.1] | [×1.1] |
| 13 | [×1.21] | [×1.21] |
| 14 | [×1.33] | [×1.33] |

Formula: `Stat_Lvl_N = Stat_Lvl_1 * 1.1^(N-1)`

## Testing Checklist
- [ ] HP = 536 at tournament standard
- [ ] Damage = 132 per hit
- [ ] Hit speed = 1.2 sec
- [ ] Range = 7 tiles (arrow travels 10 tiles)
- [ ] Speed = Medium
- [ ] Targets Air & Ground
- [ ] Visual: proper animations and effects
- [ ] Audio: character sounds and voice lines

