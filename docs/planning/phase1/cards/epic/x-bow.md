# X-Bow - Card Specification

## Basic Info
- **Card ID**: 26000087 (example)
- **Name**: X-Bow
- **Rarity**: Epic
- **Type**: Building (Siege)
- **Elixir Cost**: 6
- **Unlock Arena**: Royal Arena (Arena 7)

## Statistics (Tournament Standard / Level 11)
| Stat | Value |
|------|-------|
| Hitpoints | 1440 |
| Damage | 58 (very fast) |
| Hit Speed | 0.25 sec |
| Range | 11.5 tiles |
| Deploy Time | 2 sec |
| Target | Ground only |
| Lifetime | 30 sec |
| Mechanic | Very fast attack, long range, must deploy on own side |

## Mechanics

1. Very fast attack, long range, must deploy on own side
2. Building - stationary, has lifetime, targets by path distance

## Interactions

### Key Interactions
- Standard interactions for epic building (siege)
- Refer to CARDS_DATABASE.md for detailed interaction chart

## Synergies

- Works well with tank units for protection
- Pairs with splash damage for swarm control
- Complements spell bait strategies

## Visual/Audio

- **Sprite**: X-Bow character/model
- **Animations**: Idle, Walk, Attack, Hit, Death, Spawn
- **Effects**: Standard building (siege) effects
- **Sound**: Character-appropriate audio cues
- **Voice Lines**: Character-specific lines

## Implementation Notes

```csharp
public class XBow : SiegeBuilding
{
    // Implementation based on card stats
    protected override void InitializeStats()
    {
        MaxHP = 1440; // Level 11
        Damage = 58 (very fast);
        HitSpeed = 0.25 sec;
        Range = 11.5 tiles;
        TargetType = TargetType.Groundonly;
        Lifetime = 30 sec;
    }
}
```

## Level Scaling

| Level | HP | Damage |
|-------|-----|--------|
| 1 | [Base] | [Base] |
| ... | ... | ... |
| 11 | 1440 | 58 (very fast) |
| 12 | [×1.1] | [×1.1] |
| 13 | [×1.21] | [×1.21] |
| 14 | [×1.33] | [×1.33] |

Formula: `Stat_Lvl_N = Stat_Lvl_1 * 1.1^(N-1)`

## Testing Checklist
- [ ] HP = 1440 at tournament standard
- [ ] Damage = 58 (very fast) per hit
- [ ] Hit speed = 0.25 sec
- [ ] Range = 11.5 tiles
- [ ] Targets Ground only
- [ ] Lifetime = 30 sec
- [ ] Visual: proper animations and effects
- [ ] Audio: character sounds and voice lines

