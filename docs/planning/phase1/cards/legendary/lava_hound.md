# Lava Hound - Card Specification

## Basic Info
- **Card ID**: 26000007 (example)
- **Name**: Lava Hound
- **Rarity**: Legendary
- **Type**: Troop (Flying/Tank)
- **Elixir Cost**: 7
- **Unlock Arena**: Royal Arena (Arena 7)

## Statistics (Tournament Standard / Level 11)
| Stat | Value |
|------|-------|
| Hitpoints | 3248 |
| Damage | 44 (pups: 66 each × 8) |
| Hit Speed | 1.8 sec (Lava Hound), 1.1 sec (Lava Pups) |
| Range | 2.5 tiles (Lava Hound), 2.5 tiles (Puups) |
| Speed | Slow |
| Deploy Time | 2 sec |
| Target | Buildings only (Lava Hound), Air & Ground (Puups) |
| Mechanic | Death spawns 8 Lava Pups, targets buildings only |

## Mechanics

1. Death spawns 8 Lava Pups, targets buildings only
3. Flying unit - can cross river, targeted by air defenses

## Interactions

### Key Interactions
- Standard interactions for legendary troop (flying/tank)
- Refer to CARDS_DATABASE.md for detailed interaction chart

## Synergies

- Works well with tank units for protection
- Pairs with splash damage for swarm control
- Complements spell bait strategies

## Visual/Audio

- **Sprite**: Lava Hound character/model
- **Animations**: Idle, Walk, Attack, Hit, Death, Spawn
- **Effects**: Standard troop (flying/tank) effects
- **Sound**: Character-appropriate audio cues
- **Voice Lines**: Character-specific lines

## Implementation Notes

```csharp
public class LavaHound : FlyingTroop
{
    // Implementation based on card stats
    protected override void InitializeStats()
    {
        MaxHP = 3248; // Level 11
        Damage = 44 (pups: 66 each × 8);
        HitSpeed = 1.8 sec (Lava Hound), 1.1 sec (Lava Pups);
        Range = 2.5 tiles (Lava Hound), 2.5 tiles (Puups);
        MoveSpeed = Slow;
        TargetType = TargetType.Buildingsonly(LavaHound),AirAndGround(Puups);
    }
}
```

## Level Scaling

| Level | HP | Damage |
|-------|-----|--------|
| 1 | [Base] | [Base] |
| ... | ... | ... |
| 11 | 3248 | 44 (pups: 66 each × 8) |
| 12 | [×1.1] | [×1.1] |
| 13 | [×1.21] | [×1.21] |
| 14 | [×1.33] | [×1.33] |

Formula: `Stat_Lvl_N = Stat_Lvl_1 * 1.1^(N-1)`

## Testing Checklist
- [ ] HP = 3248 at tournament standard
- [ ] Damage = 44 (pups: 66 each × 8) per hit
- [ ] Hit speed = 1.8 sec (Lava Hound), 1.1 sec (Lava Pups)
- [ ] Range = 2.5 tiles (Lava Hound), 2.5 tiles (Puups)
- [ ] Speed = Slow
- [ ] Targets Buildings only (Lava Hound), Air & Ground (Puups)
- [ ] Visual: proper animations and effects
- [ ] Audio: character sounds and voice lines

