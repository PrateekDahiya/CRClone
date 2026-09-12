# Spear Goblins - Card Specification

## Basic Info
- **Card ID**: 26000062 (example)
- **Name**: Spear Goblins
- **Rarity**: Common
- **Type**: Troop (Ranged/Swarm)
- **Elixir Cost**: 2
- **Unlock Arena**: Training Camp (Tutorial)

## Statistics (Tournament Standard / Level 11)
| Stat | Value |
|------|-------|
| Hitpoints | 72 each (×3 = 216) |
| Damage | 50 each |
| Hit Speed | 1.3 sec |
| Range | 5.5 tiles |
| Speed | Very Fast |
| Deploy Time | 1 sec |
| Target | Air & Ground |
| Count | 3 Spear Goblins |

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

- **Sprite**: Spear Goblins character/model
- **Animations**: Idle, Walk, Attack, Hit, Death, Spawn
- **Effects**: Standard troop (ranged/swarm) effects
- **Sound**: Character-appropriate audio cues
- **Voice Lines**: Character-specific lines

## Implementation Notes

```csharp
public class SpearGoblins : Troop
{
    // Implementation based on card stats
    protected override void InitializeStats()
    {
        MaxHP = 72 each (×3 = 216); // Level 11
        Damage = 50 each;
        HitSpeed = 1.3 sec;
        Range = 5.5 tiles;
        MoveSpeed = Very Fast;
        TargetType = TargetType.AirAndGround;
        UnitCount = 3 Spear Goblins;
    }
}
```

## Level Scaling

| Level | HP | Damage |
|-------|-----|--------|
| 1 | [Base] | [Base] |
| ... | ... | ... |
| 11 | 72 each (×3 = 216) | 50 each |
| 12 | [×1.1] | [×1.1] |
| 13 | [×1.21] | [×1.21] |
| 14 | [×1.33] | [×1.33] |

Formula: `Stat_Lvl_N = Stat_Lvl_1 * 1.1^(N-1)`

## Testing Checklist
- [ ] HP = 72 each (×3 = 216) at tournament standard
- [ ] Damage = 50 each per hit
- [ ] Hit speed = 1.3 sec
- [ ] Range = 5.5 tiles
- [ ] Speed = Very Fast
- [ ] Targets Air & Ground
- [ ] Spawns 3 Spear Goblins units
- [ ] Visual: proper animations and effects
- [ ] Audio: character sounds and voice lines

