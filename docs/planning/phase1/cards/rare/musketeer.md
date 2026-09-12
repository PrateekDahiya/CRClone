# Musketeer - Card Specification

## Basic Info
- **Card ID**: 26000051 (example)
- **Name**: Musketeer
- **Rarity**: Rare
- **Type**: Troop (Ranged)
- **Elixir Cost**: 4
- **Unlock Arena**: Bone Pit (Arena 2)

## Statistics (Tournament Standard / Level 11)
| Stat | Value |
|------|-------|
| Hitpoints | 592 |
| Damage | 164 |
| Hit Speed | 1.1 sec |
| Range | 6 tiles |
| Speed | Medium |
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

- **Sprite**: Musketeer character/model
- **Animations**: Idle, Walk, Attack, Hit, Death, Spawn
- **Effects**: Standard troop (ranged) effects
- **Sound**: Character-appropriate audio cues
- **Voice Lines**: Character-specific lines

## Implementation Notes

```csharp
public class Musketeer : RangedTroop
{
    // Implementation based on card stats
    protected override void InitializeStats()
    {
        MaxHP = 592; // Level 11
        Damage = 164;
        HitSpeed = 1.1 sec;
        Range = 6 tiles;
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
| 11 | 592 | 164 |
| 12 | [×1.1] | [×1.1] |
| 13 | [×1.21] | [×1.21] |
| 14 | [×1.33] | [×1.33] |

Formula: `Stat_Lvl_N = Stat_Lvl_1 * 1.1^(N-1)`

## Testing Checklist
- [ ] HP = 592 at tournament standard
- [ ] Damage = 164 per hit
- [ ] Hit speed = 1.1 sec
- [ ] Range = 6 tiles
- [ ] Speed = Medium
- [ ] Targets Air & Ground
- [ ] Visual: proper animations and effects
- [ ] Audio: character sounds and voice lines

