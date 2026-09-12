# Little Prince - Card Specification

## Basic Info
- **Card ID**: 26000077 (example)
- **Name**: Little Prince
- **Rarity**: Epic
- **Type**: Troop (Melee/Charge)
- **Elixir Cost**: 3
- **Unlock Arena**: Royal Arena (Arena 7)

## Statistics (Tournament Standard / Level 11)
| Stat | Value |
|------|-------|
| Hitpoints | 896 |
| Damage | 160 (charge: 320) |
| Hit Speed | 1.2 sec |
| Range | Melee (1.2 tiles), Charge: 3.5 tiles |
| Speed | Fast (Very Fast charging) |
| Deploy Time | 1 sec |
| Target | Ground only |
| Mechanic | Charge mechanic, smaller Prince variant |

## Mechanics

1. Charge mechanic, smaller Prince variant
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

- **Sprite**: Little Prince character/model
- **Animations**: Idle, Walk, Attack, Hit, Death, Spawn
- **Effects**: Standard troop (melee/charge) effects
- **Sound**: Character-appropriate audio cues
- **Voice Lines**: Character-specific lines

## Implementation Notes

```csharp
public class LittlePrince : ChargeTroop
{
    // Implementation based on card stats
    protected override void InitializeStats()
    {
        MaxHP = 896; // Level 11
        Damage = 160 (charge: 320);
        HitSpeed = 1.2 sec;
        Range = Melee (1.2 tiles), Charge: 3.5 tiles;
        MoveSpeed = Fast (Very Fast charging);
        TargetType = TargetType.Groundonly;
    }
}
```

## Level Scaling

| Level | HP | Damage |
|-------|-----|--------|
| 1 | [Base] | [Base] |
| ... | ... | ... |
| 11 | 896 | 160 (charge: 320) |
| 12 | [×1.1] | [×1.1] |
| 13 | [×1.21] | [×1.21] |
| 14 | [×1.33] | [×1.33] |

Formula: `Stat_Lvl_N = Stat_Lvl_1 * 1.1^(N-1)`

## Testing Checklist
- [ ] HP = 896 at tournament standard
- [ ] Damage = 160 (charge: 320) per hit
- [ ] Hit speed = 1.2 sec
- [ ] Range = Melee (1.2 tiles), Charge: 3.5 tiles
- [ ] Speed = Fast (Very Fast charging)
- [ ] Targets Ground only
- [ ] Visual: proper animations and effects
- [ ] Audio: character sounds and voice lines

