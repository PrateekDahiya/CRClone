# Guards - Card Specification

## Basic Info
- **Card ID**: 26000064 (example)
- **Name**: Guards
- **Rarity**: Common
- **Type**: Troop (Melee/Shield)
- **Elixir Cost**: 3
- **Unlock Arena**: Royal Arena (Arena 7)

## Statistics (Tournament Standard / Level 11)
| Stat | Value |
|------|-------|
| Hitpoints | 336 each (×3), Shield: 120 |
| Damage | 92 each |
| Hit Speed | 1.2 sec |
| Range | Melee (1.2 tiles) |
| Speed | Medium |
| Deploy Time | 1 sec |
| Target | Ground only |
| Count | 3 Guards |
| Mechanic | Shield absorbs one hit, then breaks |

## Mechanics

1. Shield absorbs one hit, then breaks
2. Swarm unit - multiple units deployed together

## Interactions

### Key Interactions
- Standard interactions for common troop (melee/shield)
- Refer to CARDS_DATABASE.md for detailed interaction chart

## Synergies

- Works well with tank units for protection
- Pairs with splash damage for swarm control
- Complements spell bait strategies

## Visual/Audio

- **Sprite**: Guards character/model
- **Animations**: Idle, Walk, Attack, Hit, Death, Spawn
- **Effects**: Standard troop (melee/shield) effects
- **Sound**: Character-appropriate audio cues
- **Voice Lines**: Character-specific lines

## Implementation Notes

```csharp
public class Guards : MeleeTroop
{
    // Implementation based on card stats
    protected override void InitializeStats()
    {
        MaxHP = 336 each (×3), Shield: 120; // Level 11
        Damage = 92 each;
        HitSpeed = 1.2 sec;
        Range = Melee (1.2 tiles);
        MoveSpeed = Medium;
        TargetType = TargetType.Groundonly;
        UnitCount = 3 Guards;
    }
}
```

## Level Scaling

| Level | HP | Damage |
|-------|-----|--------|
| 1 | [Base] | [Base] |
| ... | ... | ... |
| 11 | 336 each (×3), Shield: 120 | 92 each |
| 12 | [×1.1] | [×1.1] |
| 13 | [×1.21] | [×1.21] |
| 14 | [×1.33] | [×1.33] |

Formula: `Stat_Lvl_N = Stat_Lvl_1 * 1.1^(N-1)`

## Testing Checklist
- [ ] HP = 336 each (×3), Shield: 120 at tournament standard
- [ ] Damage = 92 each per hit
- [ ] Hit speed = 1.2 sec
- [ ] Range = Melee (1.2 tiles)
- [ ] Speed = Medium
- [ ] Targets Ground only
- [ ] Spawns 3 Guards units
- [ ] Visual: proper animations and effects
- [ ] Audio: character sounds and voice lines

