# Cannon Cart - Card Specification

## Basic Info
- **Card ID**: 26000068 (example)
- **Name**: Cannon Cart
- **Rarity**: Rare
- **Type**: Troop/Building Hybrid
- **Elixir Cost**: 5
- **Unlock Arena**: Royal Arena (Arena 7)

## Statistics (Tournament Standard / Level 11)
| Stat | Value |
|------|-------|
| Hitpoints | 1040 (cart), 800 (cannon mode) |
| Damage | 140 |
| Hit Speed | 0.8 sec |
| Range | 5.5 tiles |
| Speed | Medium |
| Deploy Time | 1 sec |
| Target | Ground only (cart), Air & Ground (cannon) |
| Mechanic | Transforms to stationary cannon when wheels destroyed |

## Mechanics

1. Transforms to stationary cannon when wheels destroyed
2. Building - stationary, has lifetime, targets by path distance

## Interactions

### Key Interactions
- Standard interactions for rare troop/building hybrid
- Refer to CARDS_DATABASE.md for detailed interaction chart

## Synergies

- Works well with tank units for protection
- Pairs with splash damage for swarm control
- Complements spell bait strategies

## Visual/Audio

- **Sprite**: Cannon Cart character/model
- **Animations**: Idle, Walk, Attack, Hit, Death, Spawn
- **Effects**: Standard troop/building hybrid effects
- **Sound**: Character-appropriate audio cues
- **Voice Lines**: Character-specific lines

## Implementation Notes

```csharp
public class CannonCart : HybridTroop
{
    // Implementation based on card stats
    protected override void InitializeStats()
    {
        MaxHP = 1040 (cart), 800 (cannon mode); // Level 11
        Damage = 140;
        HitSpeed = 0.8 sec;
        Range = 5.5 tiles;
        MoveSpeed = Medium;
        TargetType = TargetType.Groundonly(cart),AirAndGround(cannon);
    }
}
```

## Level Scaling

| Level | HP | Damage |
|-------|-----|--------|
| 1 | [Base] | [Base] |
| ... | ... | ... |
| 11 | 1040 (cart), 800 (cannon mode) | 140 |
| 12 | [×1.1] | [×1.1] |
| 13 | [×1.21] | [×1.21] |
| 14 | [×1.33] | [×1.33] |

Formula: `Stat_Lvl_N = Stat_Lvl_1 * 1.1^(N-1)`

## Testing Checklist
- [ ] HP = 1040 (cart), 800 (cannon mode) at tournament standard
- [ ] Damage = 140 per hit
- [ ] Hit speed = 0.8 sec
- [ ] Range = 5.5 tiles
- [ ] Speed = Medium
- [ ] Targets Ground only (cart), Air & Ground (cannon)
- [ ] Visual: proper animations and effects
- [ ] Audio: character sounds and voice lines

