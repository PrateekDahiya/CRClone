# Inferno Dragon - Card Specification

## Basic Info
- **Card ID**: 26000006 (example)
- **Name**: Inferno Dragon
- **Rarity**: Legendary
- **Type**: Troop (Ranged)
- **Elixir Cost**: 4
- **Unlock Arena**: Spell Valley (Arena 5)

## Statistics (Tournament Standard / Level 11)
| Stat | Value |
|------|-------|
| Hitpoints | 1100 |
| Damage | 50 → 100 → 200 → 400 → 800 → 1600 (ramping) |
| Hit Speed | 0.4 sec |
| Range | 3.5 tiles |
| Speed | Fast |
| Deploy Time | 1 sec |
| Target | Air & Ground |
| Mechanic | Damage ramps up over time, resets on target switch |

## Mechanics

1. Damage ramps up over time, resets on target switch

## Interactions

### Key Interactions
- Standard interactions for legendary troop (ranged)
- Refer to CARDS_DATABASE.md for detailed interaction chart

## Synergies

- Works well with tank units for protection
- Pairs with splash damage for swarm control
- Complements spell bait strategies

## Visual/Audio

- **Sprite**: Inferno Dragon character/model
- **Animations**: Idle, Walk, Attack, Hit, Death, Spawn
- **Effects**: Standard troop (ranged) effects
- **Sound**: Character-appropriate audio cues
- **Voice Lines**: Character-specific lines

## Implementation Notes

```csharp
public class InfernoDragon : RangedTroop
{
    // Implementation based on card stats
    protected override void InitializeStats()
    {
        MaxHP = 1100; // Level 11
        Damage = 50 → 100 → 200 → 400 → 800 → 1600 (ramping);
        HitSpeed = 0.4 sec;
        Range = 3.5 tiles;
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
| 11 | 1100 | 50 → 100 → 200 → 400 → 800 → 1600 (ramping) |
| 12 | [×1.1] | [×1.1] |
| 13 | [×1.21] | [×1.21] |
| 14 | [×1.33] | [×1.33] |

Formula: `Stat_Lvl_N = Stat_Lvl_1 * 1.1^(N-1)`

## Testing Checklist
- [ ] HP = 1100 at tournament standard
- [ ] Damage = 50 → 100 → 200 → 400 → 800 → 1600 (ramping) per hit
- [ ] Hit speed = 0.4 sec
- [ ] Range = 3.5 tiles
- [ ] Speed = Fast
- [ ] Targets Air & Ground
- [ ] Visual: proper animations and effects
- [ ] Audio: character sounds and voice lines

