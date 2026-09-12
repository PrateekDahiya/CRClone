# Royal Hogs - Card Specification

## Basic Info
- **Card ID**: 26000073 (example)
- **Name**: Royal Hogs
- **Rarity**: Rare
- **Type**: Troop (Building Target/Swarm)
- **Elixir Cost**: 5
- **Unlock Arena**: Royal Arena (Arena 7)

## Statistics (Tournament Standard / Level 11)
| Stat | Value |
|------|-------|
| Hitpoints | 560 each (×4 = 2240) |
| Damage | 160 each |
| Hit Speed | 1.4 sec |
| Range | Melee (1.2 tiles) |
| Speed | Very Fast |
| Deploy Time | 1 sec |
| Target | Buildings only |
| Count | 4 Royal Hogs |

## Mechanics

2. Swarm unit - multiple units deployed together
2. Building - stationary, has lifetime, targets by path distance

## Interactions

### Key Interactions
- Standard interactions for rare troop (building target/swarm)
- Refer to CARDS_DATABASE.md for detailed interaction chart

## Synergies

- Works well with tank units for protection
- Pairs with splash damage for swarm control
- Complements spell bait strategies

## Visual/Audio

- **Sprite**: Royal Hogs character/model
- **Animations**: Idle, Walk, Attack, Hit, Death, Spawn
- **Effects**: Standard troop (building target/swarm) effects
- **Sound**: Character-appropriate audio cues
- **Voice Lines**: Character-specific lines

## Implementation Notes

```csharp
public class RoyalHogs : BuildingTargetTroop
{
    // Implementation based on card stats
    protected override void InitializeStats()
    {
        MaxHP = 560 each (×4 = 2240); // Level 11
        Damage = 160 each;
        HitSpeed = 1.4 sec;
        Range = Melee (1.2 tiles);
        MoveSpeed = Very Fast;
        TargetType = TargetType.Buildingsonly;
        UnitCount = 4 Royal Hogs;
    }
}
```

## Level Scaling

| Level | HP | Damage |
|-------|-----|--------|
| 1 | [Base] | [Base] |
| ... | ... | ... |
| 11 | 560 each (×4 = 2240) | 160 each |
| 12 | [×1.1] | [×1.1] |
| 13 | [×1.21] | [×1.21] |
| 14 | [×1.33] | [×1.33] |

Formula: `Stat_Lvl_N = Stat_Lvl_1 * 1.1^(N-1)`

## Testing Checklist
- [ ] HP = 560 each (×4 = 2240) at tournament standard
- [ ] Damage = 160 each per hit
- [ ] Hit speed = 1.4 sec
- [ ] Range = Melee (1.2 tiles)
- [ ] Speed = Very Fast
- [ ] Targets Buildings only
- [ ] Spawns 4 Royal Hogs units
- [ ] Visual: proper animations and effects
- [ ] Audio: character sounds and voice lines

