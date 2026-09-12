# Barbarians - Card Specification

## Basic Info
- **Card ID**: 26000053 (example)
- **Name**: Barbarians
- **Rarity**: Common
- **Type**: Troop (Melee/Swarm)
- **Elixir Cost**: 5
- **Unlock Arena**: Training Camp (Tutorial)

## Statistics (Tournament Standard / Level 11)
| Stat | Value |
|------|-------|
| Hitpoints | 704 each (×4 = 2816) |
| Damage | 156 each |
| Hit Speed | 1.4 sec |
| Range | Melee (1.2 tiles) |
| Speed | Medium |
| Deploy Time | 1 sec |
| Target | Ground only |
| Count | 4 Barbarians |

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

- **Sprite**: Barbarians character/model
- **Animations**: Idle, Walk, Attack, Hit, Death, Spawn
- **Effects**: Standard troop (melee/swarm) effects
- **Sound**: Character-appropriate audio cues
- **Voice Lines**: Character-specific lines

## Implementation Notes

```csharp
public class Barbarians : SwarmTroop
{
    // Implementation based on card stats
    protected override void InitializeStats()
    {
        MaxHP = 704 each (×4 = 2816); // Level 11
        Damage = 156 each;
        HitSpeed = 1.4 sec;
        Range = Melee (1.2 tiles);
        MoveSpeed = Medium;
        TargetType = TargetType.Groundonly;
        UnitCount = 4 Barbarians;
    }
}
```

## Level Scaling

| Level | HP | Damage |
|-------|-----|--------|
| 1 | [Base] | [Base] |
| ... | ... | ... |
| 11 | 704 each (×4 = 2816) | 156 each |
| 12 | [×1.1] | [×1.1] |
| 13 | [×1.21] | [×1.21] |
| 14 | [×1.33] | [×1.33] |

Formula: `Stat_Lvl_N = Stat_Lvl_1 * 1.1^(N-1)`

## Testing Checklist
- [ ] HP = 704 each (×4 = 2816) at tournament standard
- [ ] Damage = 156 each per hit
- [ ] Hit speed = 1.4 sec
- [ ] Range = Melee (1.2 tiles)
- [ ] Speed = Medium
- [ ] Targets Ground only
- [ ] Spawns 4 Barbarians units
- [ ] Visual: proper animations and effects
- [ ] Audio: character sounds and voice lines

