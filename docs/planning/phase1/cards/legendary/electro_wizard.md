# Electro Wizard - Card Specification

## Basic Info
- **Card ID**: 26000009 (example)
- **Name**: Electro Wizard
- **Rarity**: Legendary
- **Type**: Troop (Ranged)
- **Elixir Cost**: 4
- **Unlock Arena**: Spell Valley (Arena 5)

## Statistics (Tournament Standard / Level 11)
| Stat | Value |
|------|-------|
| Hitpoints | 592 |
| Damage | 147 (×2 = 294 per attack) |
| Hit Speed | 1.7 sec |
| Range | 5.5 tiles |
| Speed | Fast |
| Deploy Time | 1 sec |
| Target | Air & Ground |
| Mechanic | Spawn zap (230 dmg, 0.5s stun), attacks split between 2 targets |

## Mechanics

1. Spawn zap (230 dmg, 0.5s stun), attacks split between 2 targets

## Interactions

### Key Interactions
- Standard interactions for legendary troop (ranged)
- Refer to CARDS_DATABASE.md for detailed interaction chart

## Synergies

- Works well with tank units for protection
- Pairs with splash damage for swarm control
- Complements spell bait strategies

## Visual/Audio

- **Sprite**: Electro Wizard character/model
- **Animations**: Idle, Walk, Attack, Hit, Death, Spawn
- **Effects**: Standard troop (ranged) effects
- **Sound**: Character-appropriate audio cues
- **Voice Lines**: Character-specific lines

## Implementation Notes

```csharp
public class ElectroWizard : RangedTroop
{
    // Implementation based on card stats
    protected override void InitializeStats()
    {
        MaxHP = 592; // Level 11
        Damage = 147 (×2 = 294 per attack);
        HitSpeed = 1.7 sec;
        Range = 5.5 tiles;
        MoveSpeed = Fast;
        TargetType = TargetType.AirAndGround;
    }
}
```

## Level Scaling

| Level | HP | Damage |
|-------|-----|--------|
| 1 | [Base] | [Base] |
| ... | ... | ... |
| 11 | 592 | 147 (×2 = 294 per attack) |
| 12 | [×1.1] | [×1.1] |
| 13 | [×1.21] | [×1.21] |
| 14 | [×1.33] | [×1.33] |

Formula: `Stat_Lvl_N = Stat_Lvl_1 * 1.1^(N-1)`

## Testing Checklist
- [ ] HP = 592 at tournament standard
- [ ] Damage = 147 (×2 = 294 per attack) per hit
- [ ] Hit speed = 1.7 sec
- [ ] Range = 5.5 tiles
- [ ] Speed = Fast
- [ ] Targets Air & Ground
- [ ] Visual: proper animations and effects
- [ ] Audio: character sounds and voice lines

