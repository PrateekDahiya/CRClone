# Prince - Card Specification

## Basic Info
- **Card ID**: 26000020 (example)
- **Name**: Prince
- **Rarity**: Epic
- **Type**: Troop (Melee/Charge)
- **Elixir Cost**: 5
- **Unlock Arena**: P.E.K.K.A's Playhouse (Arena 4)

## Statistics (Tournament Standard / Level 11)
| Stat | Value |
|------|-------|
| Hitpoints | 1648 |
| Damage | 270 (charge: 540) |
| Hit Speed | 1.4 sec |
| Range | Melee (1.2 tiles), Charge: 3.5 tiles |
| Speed | Medium (Fast when charging) |
| Deploy Time | 1 sec |
| Target | Ground only |
| Mechanic | Charge after 3.5 tiles movement, double damage |

## Mechanics

1. Charge after 3.5 tiles movement, double damage
3. Charge mechanic - gains speed and damage after moving 3.5 tiles

## Interactions

### Key Interactions
- Standard interactions for epic troop (melee/charge)
- Refer to CARDS_DATABASE.md for detailed interaction chart

## Synergies

- Works well with tank units for protection
- Pairs with splash damage for swarm control
- Complements spell bait strategies

## Visual/Audio

- **Sprite**: Prince character/model
- **Animations**: Idle, Walk, Attack, Hit, Death, Spawn
- **Effects**: Standard troop (melee/charge) effects
- **Sound**: Character-appropriate audio cues
- **Voice Lines**: Character-specific lines

## Implementation Notes

```csharp
public class Prince : ChargeTroop
{
    // Implementation based on card stats
    protected override void InitializeStats()
    {
        MaxHP = 1648; // Level 11
        Damage = 270 (charge: 540);
        HitSpeed = 1.4 sec;
        Range = Melee (1.2 tiles), Charge: 3.5 tiles;
        MoveSpeed = Medium (Fast when charging);
        TargetType = TargetType.Groundonly;
    }
}
```

## Level Scaling

| Level | HP | Damage |
|-------|-----|--------|
| 1 | [Base] | [Base] |
| ... | ... | ... |
| 11 | 1648 | 270 (charge: 540) |
| 12 | [×1.1] | [×1.1] |
| 13 | [×1.21] | [×1.21] |
| 14 | [×1.33] | [×1.33] |

Formula: `Stat_Lvl_N = Stat_Lvl_1 * 1.1^(N-1)`

## Testing Checklist
- [ ] HP = 1648 at tournament standard
- [ ] Damage = 270 (charge: 540) per hit
- [ ] Hit speed = 1.4 sec
- [ ] Range = Melee (1.2 tiles), Charge: 3.5 tiles
- [ ] Speed = Medium (Fast when charging)
- [ ] Targets Ground only
- [ ] Visual: proper animations and effects
- [ ] Audio: character sounds and voice lines

