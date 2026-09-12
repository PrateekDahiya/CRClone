# Giant - Card Specification

## Basic Info
- **Card ID**: 26000050 (example)
- **Name**: Giant
- **Rarity**: Rare
- **Type**: Troop (Tank/Building Target)
- **Elixir Cost**: 5
- **Unlock Arena**: Bone Pit (Arena 2)

## Statistics (Tournament Standard / Level 11)
| Stat | Value |
|------|-------|
| Hitpoints | 3392 |
| Damage | 210 |
| Hit Speed | 1.5 sec |
| Range | Melee (1.2 tiles) |
| Speed | Slow |
| Deploy Time | 1 sec |
| Target | Buildings only |

## Mechanics

2. Building - stationary, has lifetime, targets by path distance

## Interactions

### Key Interactions
- Standard interactions for rare troop (tank/building target)
- Refer to CARDS_DATABASE.md for detailed interaction chart

## Synergies

- Works well with tank units for protection
- Pairs with splash damage for swarm control
- Complements spell bait strategies

## Visual/Audio

- **Sprite**: Giant character/model
- **Animations**: Idle, Walk, Attack, Hit, Death, Spawn
- **Effects**: Standard troop (tank/building target) effects
- **Sound**: Character-appropriate audio cues
- **Voice Lines**: Character-specific lines

## Implementation Notes

```csharp
public class Giant : Troop
{
    // Implementation based on card stats
    protected override void InitializeStats()
    {
        MaxHP = 3392; // Level 11
        Damage = 210;
        HitSpeed = 1.5 sec;
        Range = Melee (1.2 tiles);
        MoveSpeed = Slow;
        TargetType = TargetType.Buildingsonly;
    }
}
```

## Level Scaling

| Level | HP | Damage |
|-------|-----|--------|
| 1 | [Base] | [Base] |
| ... | ... | ... |
| 11 | 3392 | 210 |
| 12 | [×1.1] | [×1.1] |
| 13 | [×1.21] | [×1.21] |
| 14 | [×1.33] | [×1.33] |

Formula: `Stat_Lvl_N = Stat_Lvl_1 * 1.1^(N-1)`

## Testing Checklist
- [ ] HP = 3392 at tournament standard
- [ ] Damage = 210 per hit
- [ ] Hit speed = 1.5 sec
- [ ] Range = Melee (1.2 tiles)
- [ ] Speed = Slow
- [ ] Targets Buildings only
- [ ] Visual: proper animations and effects
- [ ] Audio: character sounds and voice lines

