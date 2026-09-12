# Wizard - Card Specification

## Basic Info
- **Card ID**: 26000021 (example)
- **Name**: Wizard
- **Rarity**: Epic
- **Type**: Troop (Ranged/Splash)
- **Elixir Cost**: 5
- **Unlock Arena**: P.E.K.K.A's Playhouse (Arena 4)

## Statistics (Tournament Standard / Level 11)
| Stat | Value |
|------|-------|
| Hitpoints | 592 |
| Damage | 324 (area 1.5 tiles) |
| Hit Speed | 1.4 sec |
| Range | 5.5 tiles |
| Speed | Medium |
| Deploy Time | 1 sec |
| Target | Air & Ground |

## Mechanics


## Interactions

### Key Interactions
- Standard interactions for epic troop (ranged/splash)
- Refer to CARDS_DATABASE.md for detailed interaction chart

## Synergies

- Works well with tank units for protection
- Pairs with splash damage for swarm control
- Complements spell bait strategies

## Visual/Audio

- **Sprite**: Wizard character/model
- **Animations**: Idle, Walk, Attack, Hit, Death, Spawn
- **Effects**: Standard troop (ranged/splash) effects
- **Sound**: Character-appropriate audio cues
- **Voice Lines**: Character-specific lines

## Implementation Notes

```csharp
public class Wizard : RangedTroop
{
    // Implementation based on card stats
    protected override void InitializeStats()
    {
        MaxHP = 592; // Level 11
        Damage = 324 (area 1.5 tiles);
        HitSpeed = 1.4 sec;
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
| 11 | 592 | 324 (area 1.5 tiles) |
| 12 | [×1.1] | [×1.1] |
| 13 | [×1.21] | [×1.21] |
| 14 | [×1.33] | [×1.33] |

Formula: `Stat_Lvl_N = Stat_Lvl_1 * 1.1^(N-1)`

## Testing Checklist
- [ ] HP = 592 at tournament standard
- [ ] Damage = 324 (area 1.5 tiles) per hit
- [ ] Hit speed = 1.4 sec
- [ ] Range = 5.5 tiles
- [ ] Speed = Medium
- [ ] Targets Air & Ground
- [ ] Visual: proper animations and effects
- [ ] Audio: character sounds and voice lines

