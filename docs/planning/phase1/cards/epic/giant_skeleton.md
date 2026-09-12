# Giant Skeleton - Card Specification

## Basic Info
- **Card ID**: 26000023 (example)
- **Name**: Giant Skeleton
- **Rarity**: Epic
- **Type**: Troop (Melee/Death Damage)
- **Elixir Cost**: 6
- **Unlock Arena**: Royal Arena (Arena 7)

## Statistics (Tournament Standard / Level 11)
| Stat | Value |
|------|-------|
| Hitpoints | 3168 |
| Damage | 170 (death bomb: 1040) |
| Hit Speed | 1.5 sec |
| Range | Melee (1.2 tiles) |
| Speed | Slow |
| Deploy Time | 2 sec |
| Target | Ground only |
| Mechanic | Death bomb 1040 damage in 3 tile radius after 3 sec delay |

## Mechanics

1. Death bomb 1040 damage in 3 tile radius after 3 sec delay

## Interactions

### Key Interactions
- Standard interactions for epic troop (melee/death damage)
- Refer to CARDS_DATABASE.md for detailed interaction chart

## Synergies

- Works well with tank units for protection
- Pairs with splash damage for swarm control
- Complements spell bait strategies

## Visual/Audio

- **Sprite**: Giant Skeleton character/model
- **Animations**: Idle, Walk, Attack, Hit, Death, Spawn
- **Effects**: Standard troop (melee/death damage) effects
- **Sound**: Character-appropriate audio cues
- **Voice Lines**: Character-specific lines

## Implementation Notes

```csharp
public class GiantSkeleton : MeleeTroop
{
    // Implementation based on card stats
    protected override void InitializeStats()
    {
        MaxHP = 3168; // Level 11
        Damage = 170 (death bomb: 1040);
        HitSpeed = 1.5 sec;
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
| 11 | 3168 | 170 (death bomb: 1040) |
| 12 | [×1.1] | [×1.1] |
| 13 | [×1.21] | [×1.21] |
| 14 | [×1.33] | [×1.33] |

Formula: `Stat_Lvl_N = Stat_Lvl_1 * 1.1^(N-1)`

## Testing Checklist
- [ ] HP = 3168 at tournament standard
- [ ] Damage = 170 (death bomb: 1040) per hit
- [ ] Hit speed = 1.5 sec
- [ ] Range = Melee (1.2 tiles)
- [ ] Speed = Slow
- [ ] Targets Ground only
- [ ] Visual: proper animations and effects
- [ ] Audio: character sounds and voice lines

