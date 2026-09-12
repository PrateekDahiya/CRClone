# Dart Goblin - Card Specification

## Basic Info
- **Card ID**: 26000044 (example)
- **Name**: Dart Goblin
- **Rarity**: Rare
- **Type**: Troop (Ranged)
- **Elixir Cost**: 3
- **Unlock Arena**: Royal Arena (Arena 7)

## Statistics (Tournament Standard / Level 11)
| Stat | Value |
|------|-------|
| Hitpoints | 256 |
| Damage | 116 |
| Hit Speed | 0.7 sec |
| Range | 6.5 tiles |
| Speed | Very Fast |
| Deploy Time | 1 sec |
| Target | Air & Ground |

## Mechanics


## Interactions

### Key Interactions
- Standard interactions for rare troop (ranged)
- Refer to CARDS_DATABASE.md for detailed interaction chart

## Synergies

- Works well with tank units for protection
- Pairs with splash damage for swarm control
- Complements spell bait strategies

## Visual/Audio

- **Sprite**: Dart Goblin character/model
- **Animations**: Idle, Walk, Attack, Hit, Death, Spawn
- **Effects**: Standard troop (ranged) effects
- **Sound**: Character-appropriate audio cues
- **Voice Lines**: Character-specific lines

## Implementation Notes

```csharp
public class DartGoblin : RangedTroop
{
    // Implementation based on card stats
    protected override void InitializeStats()
    {
        MaxHP = 256; // Level 11
        Damage = 116;
        HitSpeed = 0.7 sec;
        Range = 6.5 tiles;
        MoveSpeed = Very Fast;
        TargetType = TargetType.AirAndGround;
    }
}
```

## Level Scaling

| Level | HP | Damage |
|-------|-----|--------|
| 1 | [Base] | [Base] |
| ... | ... | ... |
| 11 | 256 | 116 |
| 12 | [×1.1] | [×1.1] |
| 13 | [×1.21] | [×1.21] |
| 14 | [×1.33] | [×1.33] |

Formula: `Stat_Lvl_N = Stat_Lvl_1 * 1.1^(N-1)`

## Testing Checklist
- [ ] HP = 256 at tournament standard
- [ ] Damage = 116 per hit
- [ ] Hit speed = 0.7 sec
- [ ] Range = 6.5 tiles
- [ ] Speed = Very Fast
- [ ] Targets Air & Ground
- [ ] Visual: proper animations and effects
- [ ] Audio: character sounds and voice lines

