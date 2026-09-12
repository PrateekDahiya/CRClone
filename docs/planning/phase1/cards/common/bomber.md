# Bomber - Card Specification

## Basic Info
- **Card ID**: 26000061 (example)
- **Name**: Bomber
- **Rarity**: Common
- **Type**: Troop (Ranged/Area)
- **Elixir Cost**: 2
- **Unlock Arena**: Training Camp (Tutorial)

## Statistics (Tournament Standard / Level 11)
| Stat | Value |
|------|-------|
| Hitpoints | 200 |
| Damage | 240 (area 1.5 tiles) |
| Hit Speed | 1.9 sec |
| Range | 4.5 tiles |
| Speed | Medium |
| Deploy Time | 1 sec |
| Target | Ground only |

## Mechanics


## Interactions

### Key Interactions
- Standard interactions for common troop (ranged/area)
- Refer to CARDS_DATABASE.md for detailed interaction chart

## Synergies

- Works well with tank units for protection
- Pairs with splash damage for swarm control
- Complements spell bait strategies

## Visual/Audio

- **Sprite**: Bomber character/model
- **Animations**: Idle, Walk, Attack, Hit, Death, Spawn
- **Effects**: Standard troop (ranged/area) effects
- **Sound**: Character-appropriate audio cues
- **Voice Lines**: Character-specific lines

## Implementation Notes

```csharp
public class Bomber : Troop
{
    // Implementation based on card stats
    protected override void InitializeStats()
    {
        MaxHP = 200; // Level 11
        Damage = 240 (area 1.5 tiles);
        HitSpeed = 1.9 sec;
        Range = 4.5 tiles;
        MoveSpeed = Medium;
        TargetType = TargetType.Groundonly;
    }
}
```

## Level Scaling

| Level | HP | Damage |
|-------|-----|--------|
| 1 | [Base] | [Base] |
| ... | ... | ... |
| 11 | 200 | 240 (area 1.5 tiles) |
| 12 | [×1.1] | [×1.1] |
| 13 | [×1.21] | [×1.21] |
| 14 | [×1.33] | [×1.33] |

Formula: `Stat_Lvl_N = Stat_Lvl_1 * 1.1^(N-1)`

## Testing Checklist
- [ ] HP = 200 at tournament standard
- [ ] Damage = 240 (area 1.5 tiles) per hit
- [ ] Hit speed = 1.9 sec
- [ ] Range = 4.5 tiles
- [ ] Speed = Medium
- [ ] Targets Ground only
- [ ] Visual: proper animations and effects
- [ ] Audio: character sounds and voice lines

