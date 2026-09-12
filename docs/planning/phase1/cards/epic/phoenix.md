# Phoenix - Card Specification

## Basic Info
- **Card ID**: 26000076 (example)
- **Name**: Phoenix
- **Rarity**: Epic
- **Type**: Troop (Flying/Rebirth)
- **Elixir Cost**: 4
- **Unlock Arena**: Royal Arena (Arena 7)

## Statistics (Tournament Standard / Level 11)
| Stat | Value |
|------|-------|
| Hitpoints | 896 |
| Damage | 168 (splash 1 tile) |
| Hit Speed | 1.5 sec |
| Range | 3.5 tiles |
| Speed | Fast |
| Deploy Time | 1 sec |
| Target | Air & Ground |
| Mechanic | Death → Egg (544 HP) → Rebirths as Phoenix (full HP) |

## Mechanics

1. Death → Egg (544 HP) → Rebirths as Phoenix (full HP)
3. Flying unit - can cross river, targeted by air defenses

## Interactions

### Key Interactions
- Standard interactions for epic troop (flying/rebirth)
- Refer to CARDS_DATABASE.md for detailed interaction chart

## Synergies

- Works well with tank units for protection
- Pairs with splash damage for swarm control
- Complements spell bait strategies

## Visual/Audio

- **Sprite**: Phoenix character/model
- **Animations**: Idle, Walk, Attack, Hit, Death, Spawn
- **Effects**: Standard troop (flying/rebirth) effects
- **Sound**: Character-appropriate audio cues
- **Voice Lines**: Character-specific lines

## Implementation Notes

```csharp
public class Phoenix : FlyingTroop
{
    // Implementation based on card stats
    protected override void InitializeStats()
    {
        MaxHP = 896; // Level 11
        Damage = 168 (splash 1 tile);
        HitSpeed = 1.5 sec;
        Range = 3.5 tiles;
        MoveSpeed = Fast;
        TargetType = TargetType.AirAndGround;
    }
}
```

## Level Scaling

| Level | HP | Damage |
|-------|-----|--------|
| 1 | [Base] | [Base] |
| ... | ... | ... |
| 11 | 896 | 168 (splash 1 tile) |
| 12 | [×1.1] | [×1.1] |
| 13 | [×1.21] | [×1.21] |
| 14 | [×1.33] | [×1.33] |

Formula: `Stat_Lvl_N = Stat_Lvl_1 * 1.1^(N-1)`

## Testing Checklist
- [ ] HP = 896 at tournament standard
- [ ] Damage = 168 (splash 1 tile) per hit
- [ ] Hit speed = 1.5 sec
- [ ] Range = 3.5 tiles
- [ ] Speed = Fast
- [ ] Targets Air & Ground
- [ ] Visual: proper animations and effects
- [ ] Audio: character sounds and voice lines

