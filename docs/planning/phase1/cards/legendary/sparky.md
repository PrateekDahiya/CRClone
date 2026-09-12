# Sparky - Card Specification

## Basic Info
- **Card ID**: 26000005 (example)
- **Name**: Sparky
- **Rarity**: Legendary
- **Type**: Troop (Ranged/Area)
- **Elixir Cost**: 6
- **Unlock Arena**: P.E.K.K.A's Playhouse (Arena 4)

## Statistics (Tournament Standard / Level 11)
| Stat | Value |
|------|-------|
| Hitpoints | 1232 |
| Damage | 1300 (area) |
| Hit Speed | 4 sec (charge time) |
| Range | 5 tiles |
| Speed | Slow |
| Deploy Time | 2 sec |
| Target | Ground only |
| Mechanic | Charges 4 sec, shoots high-damage beam, reset on stun/knockback |

## Mechanics

1. Charges 4 sec, shoots high-damage beam, reset on stun/knockback

## Interactions

### Key Interactions
- Standard interactions for legendary troop (ranged/area)
- Refer to CARDS_DATABASE.md for detailed interaction chart

## Synergies

- Works well with tank units for protection
- Pairs with splash damage for swarm control
- Complements spell bait strategies

## Visual/Audio

- **Sprite**: Sparky character/model
- **Animations**: Idle, Walk, Attack, Hit, Death, Spawn
- **Effects**: Standard troop (ranged/area) effects
- **Sound**: Character-appropriate audio cues
- **Voice Lines**: Character-specific lines

## Implementation Notes

```csharp
public class Sparky : Troop
{
    // Implementation based on card stats
    protected override void InitializeStats()
    {
        MaxHP = 1232; // Level 11
        Damage = 1300 (area);
        HitSpeed = 4 sec (charge time);
        Range = 5 tiles;
        MoveSpeed = Slow;
        TargetType = TargetType.Groundonly;
    }
}
```

## Level Scaling

| Level | HP | Damage |
|-------|-----|--------|
| 1 | [Base] | [Base] |
| ... | ... | ... |
| 11 | 1232 | 1300 (area) |
| 12 | [×1.1] | [×1.1] |
| 13 | [×1.21] | [×1.21] |
| 14 | [×1.33] | [×1.33] |

Formula: `Stat_Lvl_N = Stat_Lvl_1 * 1.1^(N-1)`

## Testing Checklist
- [ ] HP = 1232 at tournament standard
- [ ] Damage = 1300 (area) per hit
- [ ] Hit speed = 4 sec (charge time)
- [ ] Range = 5 tiles
- [ ] Speed = Slow
- [ ] Targets Ground only
- [ ] Visual: proper animations and effects
- [ ] Audio: character sounds and voice lines

