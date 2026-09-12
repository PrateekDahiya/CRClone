# P.E.K.K.A - Card Specification

## Basic Info
- **Card ID**: 26000025 (example)
- **Name**: P.E.K.K.A
- **Rarity**: Epic
- **Type**: Troop (Melee/Tank)
- **Elixir Cost**: 7
- **Unlock Arena**: Royal Arena (Arena 7)

## Statistics (Tournament Standard / Level 11)
| Stat | Value |
|------|-------|
| Hitpoints | 3472 |
| Damage | 648 |
| Hit Speed | 1.8 sec |
| Range | Melee (1.2 tiles) |
| Speed | Slow |
| Deploy Time | 2 sec |
| Target | Ground only |

## Mechanics


## Interactions

### Key Interactions
- Standard interactions for epic troop (melee/tank)
- Refer to CARDS_DATABASE.md for detailed interaction chart

## Synergies

- Works well with tank units for protection
- Pairs with splash damage for swarm control
- Complements spell bait strategies

## Visual/Audio

- **Sprite**: P.E.K.K.A character/model
- **Animations**: Idle, Walk, Attack, Hit, Death, Spawn
- **Effects**: Standard troop (melee/tank) effects
- **Sound**: Character-appropriate audio cues
- **Voice Lines**: Character-specific lines

## Implementation Notes

```csharp
public class PEKKA : Troop
{
    // Implementation based on card stats
    protected override void InitializeStats()
    {
        MaxHP = 3472; // Level 11
        Damage = 648;
        HitSpeed = 1.8 sec;
        Range = Melee (1.2 tiles);
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
| 11 | 3472 | 648 |
| 12 | [×1.1] | [×1.1] |
| 13 | [×1.21] | [×1.21] |
| 14 | [×1.33] | [×1.33] |

Formula: `Stat_Lvl_N = Stat_Lvl_1 * 1.1^(N-1)`

## Testing Checklist
- [ ] HP = 3472 at tournament standard
- [ ] Damage = 648 per hit
- [ ] Hit speed = 1.8 sec
- [ ] Range = Melee (1.2 tiles)
- [ ] Speed = Slow
- [ ] Targets Ground only
- [ ] Visual: proper animations and effects
- [ ] Audio: character sounds and voice lines

