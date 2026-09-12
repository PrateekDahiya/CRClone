# Balloon - Card Specification

## Basic Info
- **Card ID**: 26000024 (example)
- **Name**: Balloon
- **Rarity**: Epic
- **Type**: Troop (Flying/Building Target)
- **Elixir Cost**: 5
- **Unlock Arena**: P.E.K.K.A's Playhouse (Arena 4)

## Statistics (Tournament Standard / Level 11)
| Stat | Value |
|------|-------|
| Hitpoints | 1624 |
| Damage | 600 (death bomb: 280) |
| Hit Speed | 3 sec |
| Range | Melee (1.2 tiles) |
| Speed | Medium |
| Deploy Time | 1 sec |
| Target | Buildings only |
| Mechanic | Death bomb on destruction, targets buildings only |

## Mechanics

1. Death bomb on destruction, targets buildings only
3. Flying unit - can cross river, targeted by air defenses
2. Building - stationary, has lifetime, targets by path distance

## Interactions

### Key Interactions
- Standard interactions for epic troop (flying/building target)
- Refer to CARDS_DATABASE.md for detailed interaction chart

## Synergies

- Works well with tank units for protection
- Pairs with splash damage for swarm control
- Complements spell bait strategies

## Visual/Audio

- **Sprite**: Balloon character/model
- **Animations**: Idle, Walk, Attack, Hit, Death, Spawn
- **Effects**: Standard troop (flying/building target) effects
- **Sound**: Character-appropriate audio cues
- **Voice Lines**: Character-specific lines

## Implementation Notes

```csharp
public class Balloon : FlyingTroop
{
    // Implementation based on card stats
    protected override void InitializeStats()
    {
        MaxHP = 1624; // Level 11
        Damage = 600 (death bomb: 280);
        HitSpeed = 3 sec;
        Range = Melee (1.2 tiles);
        MoveSpeed = Medium;
        TargetType = TargetType.Buildingsonly;
    }
}
```

## Level Scaling

| Level | HP | Damage |
|-------|-----|--------|
| 1 | [Base] | [Base] |
| ... | ... | ... |
| 11 | 1624 | 600 (death bomb: 280) |
| 12 | [×1.1] | [×1.1] |
| 13 | [×1.21] | [×1.21] |
| 14 | [×1.33] | [×1.33] |

Formula: `Stat_Lvl_N = Stat_Lvl_1 * 1.1^(N-1)`

## Testing Checklist
- [ ] HP = 1624 at tournament standard
- [ ] Damage = 600 (death bomb: 280) per hit
- [ ] Hit speed = 3 sec
- [ ] Range = Melee (1.2 tiles)
- [ ] Speed = Medium
- [ ] Targets Buildings only
- [ ] Visual: proper animations and effects
- [ ] Audio: character sounds and voice lines

