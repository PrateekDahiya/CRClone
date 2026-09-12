# Ice Wizard - Card Specification

## Basic Info
- **Card ID**: 26000003 (example)
- **Name**: Ice Wizard
- **Rarity**: Legendary
- **Type**: Troop (Ranged)
- **Elixir Cost**: 3
- **Unlock Arena**: Spell Valley (Arena 5)

## Statistics (Tournament Standard / Level 11)
| Stat | Value |
|------|-------|
| Hitpoints | 598 |
| Damage | 88 (x2 = 176 per attack) |
| Hit Speed | 1.7 sec |
| Range | 5.5 tiles |
| Speed | Medium |
| Deploy Time | 1 sec |
| Target | Air & Ground |
| Mechanic | 35% slow on hit (1.5 sec duration), splash radius 1 tile |

## Mechanics

1. 35% slow on hit (1.5 sec duration), splash radius 1 tile

## Interactions

### Key Interactions
- Standard interactions for legendary troop (ranged)
- Refer to CARDS_DATABASE.md for detailed interaction chart

## Synergies

- Works well with tank units for protection
- Pairs with splash damage for swarm control
- Complements spell bait strategies

## Visual/Audio

- **Sprite**: Ice Wizard character/model
- **Animations**: Idle, Walk, Attack, Hit, Death, Spawn
- **Effects**: Standard troop (ranged) effects
- **Sound**: Character-appropriate audio cues
- **Voice Lines**: Character-specific lines

## Implementation Notes

```csharp
public class IceWizard : RangedTroop
{
    // Implementation based on card stats
    protected override void InitializeStats()
    {
        MaxHP = 598; // Level 11
        Damage = 88 (x2 = 176 per attack);
        HitSpeed = 1.7 sec;
        Range = 5.5 tiles;
        MoveSpeed = Medium;
        TargetType = TargetType.AirAndGround;
    }
}
```

## Level Scaling

| Level | HP | Damage |
|-------|-----|--------|
| 1 | [Base] | [Base] |
| ... | ... | ... |
| 11 | 598 | 88 (x2 = 176 per attack) |
| 12 | [×1.1] | [×1.1] |
| 13 | [×1.21] | [×1.21] |
| 14 | [×1.33] | [×1.33] |

Formula: `Stat_Lvl_N = Stat_Lvl_1 * 1.1^(N-1)`

## Testing Checklist
- [ ] HP = 598 at tournament standard
- [ ] Damage = 88 (x2 = 176 per attack) per hit
- [ ] Hit speed = 1.7 sec
- [ ] Range = 5.5 tiles
- [ ] Speed = Medium
- [ ] Targets Air & Ground
- [ ] Visual: proper animations and effects
- [ ] Audio: character sounds and voice lines

