# Skeletons - Card Specification

## Basic Info
- **Card ID**: 26000081 (example)
- **Name**: Skeletons
- **Rarity**: Common
- **Type**: Troop (Swarm)
- **Elixir Cost**: 1
- **Unlock Arena**: Training Camp (Tutorial)

## Statistics (Tournament Standard / Level 11)
| Stat | Value |
|------|-------|
| Hitpoints | 67 each (×4 = 268) |
| Damage | 67 each |
| Hit Speed | 1.1 sec |
| Range | Melee (1.2 tiles) |
| Speed | Fast |
| Deploy Time | 1 sec |
| Target | Ground only |
| Count | 4 Skeletons |

## Mechanics

2. Swarm unit - multiple units deployed together

## Interactions

### Key Interactions
- Standard interactions for common troop (swarm)
- Refer to CARDS_DATABASE.md for detailed interaction chart

## Synergies

- Works well with tank units for protection
- Pairs with splash damage for swarm control
- Complements spell bait strategies

## Visual/Audio

- **Sprite**: Skeletons character/model
- **Animations**: Idle, Walk, Attack, Hit, Death, Spawn
- **Effects**: Standard troop (swarm) effects
- **Sound**: Character-appropriate audio cues
- **Voice Lines**: Character-specific lines

## Implementation Notes

```csharp
public class Skeletons : SwarmTroop
{
    // Implementation based on card stats
    protected override void InitializeStats()
    {
        MaxHP = 67 each (×4 = 268); // Level 11
        Damage = 67 each;
        HitSpeed = 1.1 sec;
        Range = Melee (1.2 tiles);
        MoveSpeed = Fast;
        TargetType = TargetType.Groundonly;
        UnitCount = 4 Skeletons;
    }
}
```

## Level Scaling

| Level | HP | Damage |
|-------|-----|--------|
| 1 | [Base] | [Base] |
| ... | ... | ... |
| 11 | 67 each (×4 = 268) | 67 each |
| 12 | [×1.1] | [×1.1] |
| 13 | [×1.21] | [×1.21] |
| 14 | [×1.33] | [×1.33] |

Formula: `Stat_Lvl_N = Stat_Lvl_1 * 1.1^(N-1)`

## Testing Checklist
- [ ] HP = 67 each (×4 = 268) at tournament standard
- [ ] Damage = 67 each per hit
- [ ] Hit speed = 1.1 sec
- [ ] Range = Melee (1.2 tiles)
- [ ] Speed = Fast
- [ ] Targets Ground only
- [ ] Spawns 4 Skeletons units
- [ ] Visual: proper animations and effects
- [ ] Audio: character sounds and voice lines

