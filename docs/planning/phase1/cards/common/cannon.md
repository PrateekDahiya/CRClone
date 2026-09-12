# Cannon - Card Specification

## Basic Info
- **Card ID**: 26000083 (example)
- **Name**: Cannon
- **Rarity**: Common
- **Type**: Building (Defensive)
- **Elixir Cost**: 3
- **Unlock Arena**: Training Camp (Tutorial)

## Statistics (Tournament Standard / Level 11)
| Stat | Value |
|------|-------|
| Hitpoints | 1168 |
| Damage | 132 |
| Hit Speed | 0.8 sec |
| Range | 5.5 tiles |
| Deploy Time | 1 sec |
| Target | Ground only |
| Lifetime | 30 sec |

## Mechanics

2. Building - stationary, has lifetime, targets by path distance

## Interactions

### Key Interactions
- Standard interactions for common building (defensive)
- Refer to CARDS_DATABASE.md for detailed interaction chart

## Synergies

- Works well with tank units for protection
- Pairs with splash damage for swarm control
- Complements spell bait strategies

## Visual/Audio

- **Sprite**: Cannon character/model
- **Animations**: Idle, Walk, Attack, Hit, Death, Spawn
- **Effects**: Standard building (defensive) effects
- **Sound**: Character-appropriate audio cues
- **Voice Lines**: Character-specific lines

## Implementation Notes

```csharp
public class Cannon : DefensiveBuilding
{
    // Implementation based on card stats
    protected override void InitializeStats()
    {
        MaxHP = 1168; // Level 11
        Damage = 132;
        HitSpeed = 0.8 sec;
        Range = 5.5 tiles;
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
| 11 | 1168 | 132 |
| 12 | [×1.1] | [×1.1] |
| 13 | [×1.21] | [×1.21] |
| 14 | [×1.33] | [×1.33] |

Formula: `Stat_Lvl_N = Stat_Lvl_1 * 1.1^(N-1)`

## Testing Checklist
- [ ] HP = 1168 at tournament standard
- [ ] Damage = 132 per hit
- [ ] Hit speed = 0.8 sec
- [ ] Range = 5.5 tiles
- [ ] Targets Ground only
- [ ] Lifetime = 30 sec
- [ ] Visual: proper animations and effects
- [ ] Audio: character sounds and voice lines

