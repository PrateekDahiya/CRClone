# Mega Minion - Card Specification

## Basic Info
- **Card ID**: 26000063 (example)
- **Name**: Mega Minion
- **Rarity**: Common
- **Type**: Troop (Flying)
- **Elixir Cost**: 3
- **Unlock Arena**: Royal Arena (Arena 7)

## Statistics (Tournament Standard / Level 11)
| Stat | Value |
|------|-------|
| Hitpoints | 728 |
| Damage | 138 |
| Hit Speed | 1.5 sec |
| Range | 2 tiles |
| Speed | Medium |
| Deploy Time | 1 sec |
| Target | Air & Ground |

## Mechanics

3. Flying unit - can cross river, targeted by air defenses

## Interactions

### Key Interactions
- Standard interactions for common troop (flying)
- Refer to CARDS_DATABASE.md for detailed interaction chart

## Synergies

- Works well with tank units for protection
- Pairs with splash damage for swarm control
- Complements spell bait strategies

## Visual/Audio

- **Sprite**: Mega Minion character/model
- **Animations**: Idle, Walk, Attack, Hit, Death, Spawn
- **Effects**: Standard troop (flying) effects
- **Sound**: Character-appropriate audio cues
- **Voice Lines**: Character-specific lines

## Implementation Notes

```csharp
public class MegaMinion : FlyingTroop
{
    // Implementation based on card stats
    protected override void InitializeStats()
    {
        MaxHP = 728; // Level 11
        Damage = 138;
        HitSpeed = 1.5 sec;
        Range = 2 tiles;
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
| 11 | 728 | 138 |
| 12 | [×1.1] | [×1.1] |
| 13 | [×1.21] | [×1.21] |
| 14 | [×1.33] | [×1.33] |

Formula: `Stat_Lvl_N = Stat_Lvl_1 * 1.1^(N-1)`

## Testing Checklist
- [ ] HP = 728 at tournament standard
- [ ] Damage = 138 per hit
- [ ] Hit speed = 1.5 sec
- [ ] Range = 2 tiles
- [ ] Speed = Medium
- [ ] Targets Air & Ground
- [ ] Visual: proper animations and effects
- [ ] Audio: character sounds and voice lines

