# Witch - Card Specification

## Basic Info
- **Card ID**: 26000022 (example)
- **Name**: Witch
- **Rarity**: Epic
- **Type**: Troop (Ranged/Spawn)
- **Elixir Cost**: 5
- **Unlock Arena**: Royal Arena (Arena 7)

## Statistics (Tournament Standard / Level 11)
| Stat | Value |
|------|-------|
| Hitpoints | 644 |
| Damage | 142 (splash 1 tile) |
| Hit Speed | 1.7 sec |
| Range | 5.5 tiles |
| Speed | Medium |
| Deploy Time | 1 sec |
| Target | Air & Ground |
| Mechanic | Spawns 3 Skeletons every 7 sec (max 6), death spawns 4 Skeletons |

## Mechanics

1. Spawns 3 Skeletons every 7 sec (max 6), death spawns 4 Skeletons

## Interactions

### Key Interactions
- Standard interactions for epic troop (ranged/spawn)
- Refer to CARDS_DATABASE.md for detailed interaction chart

## Synergies

- Works well with tank units for protection
- Pairs with splash damage for swarm control
- Complements spell bait strategies

## Visual/Audio

- **Sprite**: Witch character/model
- **Animations**: Idle, Walk, Attack, Hit, Death, Spawn
- **Effects**: Standard troop (ranged/spawn) effects
- **Sound**: Character-appropriate audio cues
- **Voice Lines**: Character-specific lines

## Implementation Notes

```csharp
public class Witch : SpawnerTroop
{
    // Implementation based on card stats
    protected override void InitializeStats()
    {
        MaxHP = 644; // Level 11
        Damage = 142 (splash 1 tile);
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
| 11 | 644 | 142 (splash 1 tile) |
| 12 | [×1.1] | [×1.1] |
| 13 | [×1.21] | [×1.21] |
| 14 | [×1.33] | [×1.33] |

Formula: `Stat_Lvl_N = Stat_Lvl_1 * 1.1^(N-1)`

## Testing Checklist
- [ ] HP = 644 at tournament standard
- [ ] Damage = 142 (splash 1 tile) per hit
- [ ] Hit speed = 1.7 sec
- [ ] Range = 5.5 tiles
- [ ] Speed = Medium
- [ ] Targets Air & Ground
- [ ] Visual: proper animations and effects
- [ ] Audio: character sounds and voice lines

