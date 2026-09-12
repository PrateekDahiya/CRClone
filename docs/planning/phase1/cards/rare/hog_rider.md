# Hog Rider - Card Specification

## Basic Info
- **Card ID**: 26000027 (example)
- **Name**: Hog Rider
- **Rarity**: Rare
- **Type**: Troop (Melee/Building Target)
- **Elixir Cost**: 4
- **Unlock Arena**: P.E.K.K.A's Playhouse (Arena 4)

## Statistics (Tournament Standard / Level 11)
| Stat | Value |
|------|-------|
| Hitpoints | 1456 |
| Damage | 260 |
| Hit Speed | 1.6 sec |
| Range | Melee (1.2 tiles) |
| Speed | Very Fast |
| Deploy Time | 1 sec |
| Target | Buildings only |

## Mechanics

2. Building - stationary, has lifetime, targets by path distance

## Interactions

### Key Interactions
- Standard interactions for rare troop (melee/building target)
- Refer to CARDS_DATABASE.md for detailed interaction chart

## Synergies

- Works well with tank units for protection
- Pairs with splash damage for swarm control
- Complements spell bait strategies

## Visual/Audio

- **Sprite**: Hog Rider character/model
- **Animations**: Idle, Walk, Attack, Hit, Death, Spawn
- **Effects**: Standard troop (melee/building target) effects
- **Sound**: Character-appropriate audio cues
- **Voice Lines**: Character-specific lines

## Implementation Notes

```csharp
public class HogRider : Troop
{
    // Implementation based on card stats
    protected override void InitializeStats()
    {
        MaxHP = 1456; // Level 11
        Damage = 260;
        HitSpeed = 1.6 sec;
        Range = Melee (1.2 tiles);
        MoveSpeed = Very Fast;
        TargetType = TargetType.Buildingsonly;
    }
}
```

## Level Scaling

| Level | HP | Damage |
|-------|-----|--------|
| 1 | [Base] | [Base] |
| ... | ... | ... |
| 11 | 1456 | 260 |
| 12 | [×1.1] | [×1.1] |
| 13 | [×1.21] | [×1.21] |
| 14 | [×1.33] | [×1.33] |

Formula: `Stat_Lvl_N = Stat_Lvl_1 * 1.1^(N-1)`

## Testing Checklist
- [ ] HP = 1456 at tournament standard
- [ ] Damage = 260 per hit
- [ ] Hit speed = 1.6 sec
- [ ] Range = Melee (1.2 tiles)
- [ ] Speed = Very Fast
- [ ] Targets Buildings only
- [ ] Visual: proper animations and effects
- [ ] Audio: character sounds and voice lines

