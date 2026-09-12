# Electro Dragon - Card Specification

## Basic Info
- **Card ID**: 26000046 (example)
- **Name**: Electro Dragon
- **Rarity**: Epic
- **Type**: Troop (Flying/Chain)
- **Elixir Cost**: 5
- **Unlock Arena**: Royal Arena (Arena 7)

## Statistics (Tournament Standard / Level 11)
| Stat | Value |
|------|-------|
| Hitpoints | 1456 |
| Damage | 176 (chains to 2 targets) |
| Hit Speed | 2.4 sec |
| Range | 3.5 tiles (chain: 4 tiles) |
| Speed | Medium |
| Deploy Time | 1 sec |
| Target | Air & Ground |
| Mechanic | Lightning chains to 2 additional targets |

## Mechanics

1. Lightning chains to 2 additional targets
3. Flying unit - can cross river, targeted by air defenses

## Interactions

### Key Interactions
- Standard interactions for epic troop (flying/chain)
- Refer to CARDS_DATABASE.md for detailed interaction chart

## Synergies

- Works well with tank units for protection
- Pairs with splash damage for swarm control
- Complements spell bait strategies

## Visual/Audio

- **Sprite**: Electro Dragon character/model
- **Animations**: Idle, Walk, Attack, Hit, Death, Spawn
- **Effects**: Standard troop (flying/chain) effects
- **Sound**: Character-appropriate audio cues
- **Voice Lines**: Character-specific lines

## Implementation Notes

```csharp
public class ElectroDragon : FlyingTroop
{
    // Implementation based on card stats
    protected override void InitializeStats()
    {
        MaxHP = 1456; // Level 11
        Damage = 176 (chains to 2 targets);
        HitSpeed = 2.4 sec;
        Range = 3.5 tiles (chain: 4 tiles);
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
| 11 | 1456 | 176 (chains to 2 targets) |
| 12 | [×1.1] | [×1.1] |
| 13 | [×1.21] | [×1.21] |
| 14 | [×1.33] | [×1.33] |

Formula: `Stat_Lvl_N = Stat_Lvl_1 * 1.1^(N-1)`

## Testing Checklist
- [ ] HP = 1456 at tournament standard
- [ ] Damage = 176 (chains to 2 targets) per hit
- [ ] Hit speed = 2.4 sec
- [ ] Range = 3.5 tiles (chain: 4 tiles)
- [ ] Speed = Medium
- [ ] Targets Air & Ground
- [ ] Visual: proper animations and effects
- [ ] Audio: character sounds and voice lines

