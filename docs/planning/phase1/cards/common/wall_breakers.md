# Wall Breakers - Card Specification

## Basic Info
- **Card ID**: 26000072 (example)
- **Name**: Wall Breakers
- **Rarity**: Common
- **Type**: Troop (Suicide/Building Target)
- **Elixir Cost**: 2
- **Unlock Arena**: Royal Arena (Arena 7)

## Statistics (Tournament Standard / Level 11)
| Stat | Value |
|------|-------|
| Hitpoints | 184 each (×2) |
| Damage | 224 each (×2 = 448 per pair) |
| Hit Speed | Once (suicide) |
| Range | Melee (1.2 tiles) |
| Speed | Very Fast |
| Deploy Time | 1 sec |
| Target | Buildings only |
| Count | 2 Wall Breakers |
| Mechanic | Suicide attack, double damage to walls/buildings |

## Mechanics

1. Suicide attack, double damage to walls/buildings
2. Swarm unit - multiple units deployed together
2. Building - stationary, has lifetime, targets by path distance

## Interactions

### Key Interactions
- Standard interactions for common troop (suicide/building target)
- Refer to CARDS_DATABASE.md for detailed interaction chart

## Synergies

- Works well with tank units for protection
- Pairs with splash damage for swarm control
- Complements spell bait strategies

## Visual/Audio

- **Sprite**: Wall Breakers character/model
- **Animations**: Idle, Walk, Attack, Hit, Death, Spawn
- **Effects**: Standard troop (suicide/building target) effects
- **Sound**: Character-appropriate audio cues
- **Voice Lines**: Character-specific lines

## Implementation Notes

```csharp
public class WallBreakers : SuicideTroop
{
    // Implementation based on card stats
    protected override void InitializeStats()
    {
        MaxHP = 184 each (×2); // Level 11
        Damage = 224 each (×2 = 448 per pair);
        HitSpeed = Once (suicide);
        Range = Melee (1.2 tiles);
        MoveSpeed = Very Fast;
        TargetType = TargetType.Buildingsonly;
        UnitCount = 2 Wall Breakers;
    }
}
```

## Level Scaling

| Level | HP | Damage |
|-------|-----|--------|
| 1 | [Base] | [Base] |
| ... | ... | ... |
| 11 | 184 each (×2) | 224 each (×2 = 448 per pair) |
| 12 | [×1.1] | [×1.1] |
| 13 | [×1.21] | [×1.21] |
| 14 | [×1.33] | [×1.33] |

Formula: `Stat_Lvl_N = Stat_Lvl_1 * 1.1^(N-1)`

## Testing Checklist
- [ ] HP = 184 each (×2) at tournament standard
- [ ] Damage = 224 each (×2 = 448 per pair) per hit
- [ ] Hit speed = Once (suicide)
- [ ] Range = Melee (1.2 tiles)
- [ ] Speed = Very Fast
- [ ] Targets Buildings only
- [ ] Spawns 2 Wall Breakers units
- [ ] Visual: proper animations and effects
- [ ] Audio: character sounds and voice lines

