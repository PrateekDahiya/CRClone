# Goblins - Card Specification

## Basic Info
- **Card ID**: 26000080 (example)
- **Name**: Goblins
- **Rarity**: Common
- **Type**: Troop (Melee/Swarm)
- **Elixir Cost**: 2
- **Unlock Arena**: Training Camp (Tutorial)

## Statistics (Tournament Standard / Level 11)
| Stat | Value |
|------|-------|
| Hitpoints | 264 each (×3 = 792) |
| Damage | 104 each |
| Hit Speed | 1.1 sec |
| Range | Melee (1.2 tiles) |
| Speed | Very Fast |
| Deploy Time | 1 sec |
| Target | Ground only |
| Count | 3 Goblins |

## Mechanics

2. Swarm unit - multiple units deployed together

## Interactions

### Key Interactions
- Standard interactions for common troop (melee/swarm)
- Refer to CARDS_DATABASE.md for detailed interaction chart

## Synergies

- Works well with tank units for protection
- Pairs with splash damage for swarm control
- Complements spell bait strategies

## Visual/Audio

- **Sprite**: Goblins character/model
- **Animations**: Idle, Walk, Attack, Hit, Death, Spawn
- **Effects**: Standard troop (melee/swarm) effects
- **Sound**: Character-appropriate audio cues
- **Voice Lines**: Character-specific lines

## Implementation Notes

```csharp
public class Goblins : SwarmTroop
{
    // Implementation based on card stats
    protected override void InitializeStats()
    {
        MaxHP = 264 each (×3 = 792); // Level 11
        Damage = 104 each;
        HitSpeed = 1.1 sec;
        Range = Melee (1.2 tiles);
        MoveSpeed = Very Fast;
        TargetType = TargetType.Groundonly;
        UnitCount = 3 Goblins;
    }
}
```

## Level Scaling

| Level | HP | Damage |
|-------|-----|--------|
| 1 | [Base] | [Base] |
| ... | ... | ... |
| 11 | 264 each (×3 = 792) | 104 each |
| 12 | [×1.1] | [×1.1] |
| 13 | [×1.21] | [×1.21] |
| 14 | [×1.33] | [×1.33] |

Formula: `Stat_Lvl_N = Stat_Lvl_1 * 1.1^(N-1)`

## Testing Checklist
- [ ] HP = 264 each (×3 = 792) at tournament standard
- [ ] Damage = 104 each per hit
- [ ] Hit speed = 1.1 sec
- [ ] Range = Melee (1.2 tiles)
- [ ] Speed = Very Fast
- [ ] Targets Ground only
- [ ] Spawns 3 Goblins units
- [ ] Visual: proper animations and effects
- [ ] Audio: character sounds and voice lines

