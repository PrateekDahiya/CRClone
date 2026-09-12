# Archers - Card Specification

## Basic Info
- **Card ID**: 26000079 (example)
- **Name**: Archers
- **Rarity**: Common
- **Type**: Troop (Ranged/Swarm)
- **Elixir Cost**: 3
- **Unlock Arena**: Training Camp (Tutorial)

## Statistics (Tournament Standard / Level 11)
| Stat | Value |
|------|-------|
| Hitpoints | 256 each (×2 = 512) |
| Damage | 82 each |
| Hit Speed | 1.2 sec |
| Range | 5 tiles |
| Speed | Medium |
| Deploy Time | 1 sec |
| Target | Air & Ground |
| Count | 2 Archers |

## Mechanics

2. Swarm unit - multiple units deployed together

## Interactions

### Key Interactions
- Standard interactions for common troop (ranged/swarm)
- Refer to CARDS_DATABASE.md for detailed interaction chart

## Synergies

- Works well with tank units for protection
- Pairs with splash damage for swarm control
- Complements spell bait strategies

## Visual/Audio

- **Sprite**: Archers character/model
- **Animations**: Idle, Walk, Attack, Hit, Death, Spawn
- **Effects**: Standard troop (ranged/swarm) effects
- **Sound**: Character-appropriate audio cues
- **Voice Lines**: Character-specific lines

## Implementation Notes

```csharp
public class Archers : Troop
{
    // Implementation based on card stats
    protected override void InitializeStats()
    {
        MaxHP = 256 each (×2 = 512); // Level 11
        Damage = 82 each;
        HitSpeed = 1.2 sec;
        Range = 5 tiles;
        MoveSpeed = Medium;
        TargetType = TargetType.AirAndGround;
        UnitCount = 2 Archers;
    }
}
```

## Level Scaling

| Level | HP | Damage |
|-------|-----|--------|
| 1 | [Base] | [Base] |
| ... | ... | ... |
| 11 | 256 each (×2 = 512) | 82 each |
| 12 | [×1.1] | [×1.1] |
| 13 | [×1.21] | [×1.21] |
| 14 | [×1.33] | [×1.33] |

Formula: `Stat_Lvl_N = Stat_Lvl_1 * 1.1^(N-1)`

## Testing Checklist
- [ ] HP = 256 each (×2 = 512) at tournament standard
- [ ] Damage = 82 each per hit
- [ ] Hit speed = 1.2 sec
- [ ] Range = 5 tiles
- [ ] Speed = Medium
- [ ] Targets Air & Ground
- [ ] Spawns 2 Archers units
- [ ] Visual: proper animations and effects
- [ ] Audio: character sounds and voice lines

