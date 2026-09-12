# Ram Rider - Card Specification

## Basic Info
- **Card ID**: 26000015 (example)
- **Name**: Ram Rider
- **Rarity**: Legendary
- **Type**: Troop (Melee/Ranged Hybrid)
- **Elixir Cost**: 5
- **Unlock Arena**: Royal Arena (Arena 7)

## Statistics (Tournament Standard / Level 11)
| Stat | Value |
|------|-------|
| Hitpoints | 1456 |
| Damage | 160 (melee), 120 (snare projectile) |
| Hit Speed | 1.4 sec (melee), 3 sec (snare) |
| Range | Melee (1.2 tiles), Snare: 5 tiles |
| Speed | Fast |
| Deploy Time | 1 sec |
| Target | Ground only |
| Mechanic | Snare projectile slows 35% for 2.5s, charge mechanic |

## Mechanics

1. Snare projectile slows 35% for 2.5s, charge mechanic

## Interactions

### Key Interactions
- Standard interactions for legendary troop (melee/ranged hybrid)
- Refer to CARDS_DATABASE.md for detailed interaction chart

## Synergies

- Works well with tank units for protection
- Pairs with splash damage for swarm control
- Complements spell bait strategies

## Visual/Audio

- **Sprite**: Ram Rider character/model
- **Animations**: Idle, Walk, Attack, Hit, Death, Spawn
- **Effects**: Standard troop (melee/ranged hybrid) effects
- **Sound**: Character-appropriate audio cues
- **Voice Lines**: Character-specific lines

## Implementation Notes

```csharp
public class RamRider : HybridTroop
{
    // Implementation based on card stats
    protected override void InitializeStats()
    {
        MaxHP = 1456; // Level 11
        Damage = 160 (melee), 120 (snare projectile);
        HitSpeed = 1.4 sec (melee), 3 sec (snare);
        Range = Melee (1.2 tiles), Snare: 5 tiles;
        MoveSpeed = Fast;
        TargetType = TargetType.Groundonly;
    }
}
```

## Level Scaling

| Level | HP | Damage |
|-------|-----|--------|
| 1 | [Base] | [Base] |
| ... | ... | ... |
| 11 | 1456 | 160 (melee), 120 (snare projectile) |
| 12 | [×1.1] | [×1.1] |
| 13 | [×1.21] | [×1.21] |
| 14 | [×1.33] | [×1.33] |

Formula: `Stat_Lvl_N = Stat_Lvl_1 * 1.1^(N-1)`

## Testing Checklist
- [ ] HP = 1456 at tournament standard
- [ ] Damage = 160 (melee), 120 (snare projectile) per hit
- [ ] Hit speed = 1.4 sec (melee), 3 sec (snare)
- [ ] Range = Melee (1.2 tiles), Snare: 5 tiles
- [ ] Speed = Fast
- [ ] Targets Ground only
- [ ] Visual: proper animations and effects
- [ ] Audio: character sounds and voice lines

