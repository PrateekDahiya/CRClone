# Valkyrie - Card Specification

## Basic Info
- **Card ID**: 26000028 (example)
- **Name**: Valkyrie
- **Rarity**: Rare
- **Type**: Troop (Melee/Area)
- **Elixir Cost**: 4
- **Unlock Arena**: P.E.K.K.A's Playhouse (Arena 4)

## Statistics (Tournament Standard / Level 11)
| Stat | Value |
|------|-------|
| Hitpoints | 1760 |
| Damage | 216 (360° spin) |
| Hit Speed | 1.5 sec |
| Range | Melee (1.2 tiles, 360°) |
| Speed | Medium |
| Deploy Time | 1 sec |
| Target | Ground only |
| Mechanic | 360° splash attack |

## Mechanics

1. 360° splash attack

## Interactions

### Key Interactions
- Standard interactions for rare troop (melee/area)
- Refer to CARDS_DATABASE.md for detailed interaction chart

## Synergies

- Works well with tank units for protection
- Pairs with splash damage for swarm control
- Complements spell bait strategies

## Visual/Audio

- **Sprite**: Valkyrie character/model
- **Animations**: Idle, Walk, Attack, Hit, Death, Spawn
- **Effects**: Standard troop (melee/area) effects
- **Sound**: Character-appropriate audio cues
- **Voice Lines**: Character-specific lines

## Implementation Notes

```csharp
public class Valkyrie : MeleeTroop
{
    // Implementation based on card stats
    protected override void InitializeStats()
    {
        MaxHP = 1760; // Level 11
        Damage = 216 (360° spin);
        HitSpeed = 1.5 sec;
        Range = Melee (1.2 tiles, 360°);
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
| 11 | 1760 | 216 (360° spin) |
| 12 | [×1.1] | [×1.1] |
| 13 | [×1.21] | [×1.21] |
| 14 | [×1.33] | [×1.33] |

Formula: `Stat_Lvl_N = Stat_Lvl_1 * 1.1^(N-1)`

## Testing Checklist
- [ ] HP = 1760 at tournament standard
- [ ] Damage = 216 (360° spin) per hit
- [ ] Hit speed = 1.5 sec
- [ ] Range = Melee (1.2 tiles, 360°)
- [ ] Speed = Medium
- [ ] Targets Ground only
- [ ] Visual: proper animations and effects
- [ ] Audio: character sounds and voice lines

