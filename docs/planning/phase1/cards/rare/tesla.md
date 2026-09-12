# Tesla - Card Specification

## Basic Info
- **Card ID**: 26000084 (example)
- **Name**: Tesla
- **Rarity**: Rare
- **Type**: Building (Defensive)
- **Elixir Cost**: 4
- **Unlock Arena**: Bone Pit (Arena 2)

## Statistics (Tournament Standard / Level 11)
| Stat | Value |
|------|-------|
| Hitpoints | 840 |
| Damage | 160 |
| Hit Speed | 0.8 sec |
| Range | 5.5 tiles |
| Deploy Time | 1 sec |
| Target | Air & Ground |
| Lifetime | 25 sec (retracts when not attacking) |
| Mechanic | Retracts underground when no targets, invulnerable while retracted |

## Mechanics

1. Retracts underground when no targets, invulnerable while retracted
2. Building - stationary, has lifetime, targets by path distance

## Interactions

### Key Interactions
- Standard interactions for rare building (defensive)
- Refer to CARDS_DATABASE.md for detailed interaction chart

## Synergies

- Works well with tank units for protection
- Pairs with splash damage for swarm control
- Complements spell bait strategies

## Visual/Audio

- **Sprite**: Tesla character/model
- **Animations**: Idle, Walk, Attack, Hit, Death, Spawn
- **Effects**: Standard building (defensive) effects
- **Sound**: Character-appropriate audio cues
- **Voice Lines**: Character-specific lines

## Implementation Notes

```csharp
public class Tesla : DefensiveBuilding
{
    // Implementation based on card stats
    protected override void InitializeStats()
    {
        MaxHP = 840; // Level 11
        Damage = 160;
        HitSpeed = 0.8 sec;
        Range = 5.5 tiles;
        TargetType = TargetType.AirAndGround;
        Lifetime = 25 sec (retracts when not attacking);
    }
}
```

## Level Scaling

| Level | HP | Damage |
|-------|-----|--------|
| 1 | [Base] | [Base] |
| ... | ... | ... |
| 11 | 840 | 160 |
| 12 | [×1.1] | [×1.1] |
| 13 | [×1.21] | [×1.21] |
| 14 | [×1.33] | [×1.33] |

Formula: `Stat_Lvl_N = Stat_Lvl_1 * 1.1^(N-1)`

## Testing Checklist
- [ ] HP = 840 at tournament standard
- [ ] Damage = 160 per hit
- [ ] Hit speed = 0.8 sec
- [ ] Range = 5.5 tiles
- [ ] Targets Air & Ground
- [ ] Lifetime = 25 sec (retracts when not attacking)
- [ ] Visual: proper animations and effects
- [ ] Audio: character sounds and voice lines

