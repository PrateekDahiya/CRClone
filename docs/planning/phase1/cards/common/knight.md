# Knight - Card Specification

## Basic Info
- **Card ID**: 26000078 (example)
- **Name**: Knight
- **Rarity**: Common
- **Type**: Troop (Melee/Tank)
- **Elixir Cost**: 3
- **Unlock Arena**: Training Camp (Tutorial)

## Statistics (Tournament Standard / Level 11)
| Stat | Value |
|------|-------|
| Hitpoints | 1520 |
| Damage | 160 |
| Hit Speed | 1.1 sec |
| Range | Melee (1.2 tiles) |
| Speed | Medium |
| Deploy Time | 1 sec |
| Target | Ground only |

## Mechanics


## Interactions

### Key Interactions
- Standard interactions for common troop (melee/tank)
- Refer to CARDS_DATABASE.md for detailed interaction chart

## Synergies

- Works well with tank units for protection
- Pairs with splash damage for swarm control
- Complements spell bait strategies

## Visual/Audio

- **Sprite**: Knight character/model
- **Animations**: Idle, Walk, Attack, Hit, Death, Spawn
- **Effects**: Standard troop (melee/tank) effects
- **Sound**: Character-appropriate audio cues
- **Voice Lines**: Character-specific lines

## Implementation Notes

```csharp
public class Knight : Troop
{
    // Implementation based on card stats
    protected override void InitializeStats()
    {
        MaxHP = 1520; // Level 11
        Damage = 160;
        HitSpeed = 1.1 sec;
        Range = Melee (1.2 tiles);
        MoveSpeed = Medium;
        TargetType = TargetType.Groundonly;
    }
}
```

## Level Scaling

| Level | HP | Damage |
|-------|-----|--------|
| 1 | [Base] | [Base] |
| ... | ... | ... |
| 11 | 1520 | 160 |
| 12 | [×1.1] | [×1.1] |
| 13 | [×1.21] | [×1.21] |
| 14 | [×1.33] | [×1.33] |

Formula: `Stat_Lvl_N = Stat_Lvl_1 * 1.1^(N-1)`

## Testing Checklist
- [ ] HP = 1520 at tournament standard
- [ ] Damage = 160 per hit
- [ ] Hit speed = 1.1 sec
- [ ] Range = Melee (1.2 tiles)
- [ ] Speed = Medium
- [ ] Targets Ground only
- [ ] Visual: proper animations and effects
- [ ] Audio: character sounds and voice lines

