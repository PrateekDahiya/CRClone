# Dark Prince - Card Specification

## Basic Info
- **Card ID**: 26000041 (example)
- **Name**: Dark Prince
- **Rarity**: Epic
- **Type**: Troop (Melee/Area/Charge)
- **Elixir Cost**: 4
- **Unlock Arena**: Royal Arena (Arena 7)

## Statistics (Tournament Standard / Level 11)
| Stat | Value |
|------|-------|
| Hitpoints | 1600 |
| Damage | 208 (charge: 416) |
| Hit Speed | 1.4 sec |
| Range | Melee (1.2 tiles, 360°), Charge: 3.5 tiles |
| Speed | Medium (Fast charging) |
| Deploy Time | 1 sec |
| Target | Ground only |
| Mechanic | Charge, 360° splash, shield (absorbs 300 dmg) |

## Mechanics

1. Charge, 360° splash, shield (absorbs 300 dmg)
3. Charge mechanic - gains speed and damage after moving 3.5 tiles

## Interactions

### Key Interactions
- Standard interactions for epic troop (melee/area/charge)
- Refer to CARDS_DATABASE.md for detailed interaction chart

## Synergies

- Works well with tank units for protection
- Pairs with splash damage for swarm control
- Complements spell bait strategies

## Visual/Audio

- **Sprite**: Dark Prince character/model
- **Animations**: Idle, Walk, Attack, Hit, Death, Spawn
- **Effects**: Standard troop (melee/area/charge) effects
- **Sound**: Character-appropriate audio cues
- **Voice Lines**: Character-specific lines

## Implementation Notes

```csharp
public class DarkPrince : Troop
{
    // Implementation based on card stats
    protected override void InitializeStats()
    {
        MaxHP = 1600; // Level 11
        Damage = 208 (charge: 416);
        HitSpeed = 1.4 sec;
        Range = Melee (1.2 tiles, 360°), Charge: 3.5 tiles;
        MoveSpeed = Medium (Fast charging);
        TargetType = TargetType.Groundonly;
    }
}
```

## Level Scaling

| Level | HP | Damage |
|-------|-----|--------|
| 1 | [Base] | [Base] |
| ... | ... | ... |
| 11 | 1600 | 208 (charge: 416) |
| 12 | [×1.1] | [×1.1] |
| 13 | [×1.21] | [×1.21] |
| 14 | [×1.33] | [×1.33] |

Formula: `Stat_Lvl_N = Stat_Lvl_1 * 1.1^(N-1)`

## Testing Checklist
- [ ] HP = 1600 at tournament standard
- [ ] Damage = 208 (charge: 416) per hit
- [ ] Hit speed = 1.4 sec
- [ ] Range = Melee (1.2 tiles, 360°), Charge: 3.5 tiles
- [ ] Speed = Medium (Fast charging)
- [ ] Targets Ground only
- [ ] Visual: proper animations and effects
- [ ] Audio: character sounds and voice lines

