# Royal Ghost - Card Specification

## Basic Info
- **Card ID**: 26000016 (example)
- **Name**: Royal Ghost
- **Rarity**: Legendary
- **Type**: Troop (Melee)
- **Elixir Cost**: 3
- **Unlock Arena**: Spell Valley (Arena 5)

## Statistics (Tournament Standard / Level 11)
| Stat | Value |
|------|-------|
| Hitpoints | 864 |
| Damage | 212 |
| Hit Speed | 1.1 sec |
| Range | Melee (1.2 tiles) |
| Speed | Very Fast |
| Deploy Time | 1 sec |
| Target | Ground only |
| Mechanic | Invisible until attacking, 3 sec invisibility after hit |

## Mechanics

1. Invisible until attacking, 3 sec invisibility after hit

## Interactions

### Key Interactions
- Standard interactions for legendary troop (melee)
- Refer to CARDS_DATABASE.md for detailed interaction chart

## Synergies

- Works well with tank units for protection
- Pairs with splash damage for swarm control
- Complements spell bait strategies

## Visual/Audio

- **Sprite**: Royal Ghost character/model
- **Animations**: Idle, Walk, Attack, Hit, Death, Spawn
- **Effects**: Standard troop (melee) effects
- **Sound**: Character-appropriate audio cues
- **Voice Lines**: Character-specific lines

## Implementation Notes

```csharp
public class RoyalGhost : MeleeTroop
{
    // Implementation based on card stats
    protected override void InitializeStats()
    {
        MaxHP = 864; // Level 11
        Damage = 212;
        HitSpeed = 1.1 sec;
        Range = Melee (1.2 tiles);
        MoveSpeed = Very Fast;
        TargetType = TargetType.Groundonly;
    }
}
```

## Level Scaling

| Level | HP | Damage |
|-------|-----|--------|
| 1 | [Base] | [Base] |
| ... | ... | ... |
| 11 | 864 | 212 |
| 12 | [×1.1] | [×1.1] |
| 13 | [×1.21] | [×1.21] |
| 14 | [×1.33] | [×1.33] |

Formula: `Stat_Lvl_N = Stat_Lvl_1 * 1.1^(N-1)`

## Testing Checklist
- [ ] HP = 864 at tournament standard
- [ ] Damage = 212 per hit
- [ ] Hit speed = 1.1 sec
- [ ] Range = Melee (1.2 tiles)
- [ ] Speed = Very Fast
- [ ] Targets Ground only
- [ ] Visual: proper animations and effects
- [ ] Audio: character sounds and voice lines

