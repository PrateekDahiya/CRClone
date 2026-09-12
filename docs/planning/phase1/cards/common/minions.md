# Minions - Card Specification

## Basic Info
- **Card ID**: 26000082 (example)
- **Name**: Minions
- **Rarity**: Common
- **Type**: Troop (Flying/Swarm)
- **Elixir Cost**: 3
- **Unlock Arena**: Training Camp (Tutorial)

## Statistics (Tournament Standard / Level 11)
| Stat | Value |
|------|-------|
| Hitpoints | 140 each (×3 = 420) |
| Damage | 70 each |
| Hit Speed | 1 sec |
| Range | 2 tiles |
| Speed | Fast |
| Deploy Time | 1 sec |
| Target | Air & Ground |
| Count | 3 Minions |

## Mechanics

2. Swarm unit - multiple units deployed together
3. Flying unit - can cross river, targeted by air defenses

## Interactions

### Key Interactions
- Standard interactions for common troop (flying/swarm)
- Refer to CARDS_DATABASE.md for detailed interaction chart

## Synergies

- Works well with tank units for protection
- Pairs with splash damage for swarm control
- Complements spell bait strategies

## Visual/Audio

- **Sprite**: Minions character/model
- **Animations**: Idle, Walk, Attack, Hit, Death, Spawn
- **Effects**: Standard troop (flying/swarm) effects
- **Sound**: Character-appropriate audio cues
- **Voice Lines**: Character-specific lines

## Implementation Notes

```csharp
public class Minions : SwarmTroop
{
    // Implementation based on card stats
    protected override void InitializeStats()
    {
        MaxHP = 140 each (×3 = 420); // Level 11
        Damage = 70 each;
        HitSpeed = 1 sec;
        Range = 2 tiles;
        MoveSpeed = Fast;
        TargetType = TargetType.AirAndGround;
        UnitCount = 3 Minions;
    }
}
```

## Level Scaling

| Level | HP | Damage |
|-------|-----|--------|
| 1 | [Base] | [Base] |
| ... | ... | ... |
| 11 | 140 each (×3 = 420) | 70 each |
| 12 | [×1.1] | [×1.1] |
| 13 | [×1.21] | [×1.21] |
| 14 | [×1.33] | [×1.33] |

Formula: `Stat_Lvl_N = Stat_Lvl_1 * 1.1^(N-1)`

## Testing Checklist
- [ ] HP = 140 each (×3 = 420) at tournament standard
- [ ] Damage = 70 each per hit
- [ ] Hit speed = 1 sec
- [ ] Range = 2 tiles
- [ ] Speed = Fast
- [ ] Targets Air & Ground
- [ ] Spawns 3 Minions units
- [ ] Visual: proper animations and effects
- [ ] Audio: character sounds and voice lines

