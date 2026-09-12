# Bats - Card Specification

## Basic Info
- **Card ID**: 26000071 (example)
- **Name**: Bats
- **Rarity**: Common
- **Type**: Troop (Flying/Swarm)
- **Elixir Cost**: 2
- **Unlock Arena**: Spell Valley (Arena 5)

## Statistics (Tournament Standard / Level 11)
| Stat | Value |
|------|-------|
| Hitpoints | 67 each (×5 = 335) |
| Damage | 67 each |
| Hit Speed | 1.1 sec |
| Range | 1.2 tiles |
| Speed | Fast |
| Deploy Time | 1 sec |
| Target | Air & Ground |
| Count | 5 Bats |

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

- **Sprite**: Bats character/model
- **Animations**: Idle, Walk, Attack, Hit, Death, Spawn
- **Effects**: Standard troop (flying/swarm) effects
- **Sound**: Character-appropriate audio cues
- **Voice Lines**: Character-specific lines

## Implementation Notes

```csharp
public class Bats : SwarmTroop
{
    // Implementation based on card stats
    protected override void InitializeStats()
    {
        MaxHP = 67 each (×5 = 335); // Level 11
        Damage = 67 each;
        HitSpeed = 1.1 sec;
        Range = 1.2 tiles;
        MoveSpeed = Fast;
        TargetType = TargetType.AirAndGround;
        UnitCount = 5 Bats;
    }
}
```

## Level Scaling

| Level | HP | Damage |
|-------|-----|--------|
| 1 | [Base] | [Base] |
| ... | ... | ... |
| 11 | 67 each (×5 = 335) | 67 each |
| 12 | [×1.1] | [×1.1] |
| 13 | [×1.21] | [×1.21] |
| 14 | [×1.33] | [×1.33] |

Formula: `Stat_Lvl_N = Stat_Lvl_1 * 1.1^(N-1)`

## Testing Checklist
- [ ] HP = 67 each (×5 = 335) at tournament standard
- [ ] Damage = 67 each per hit
- [ ] Hit speed = 1.1 sec
- [ ] Range = 1.2 tiles
- [ ] Speed = Fast
- [ ] Targets Air & Ground
- [ ] Spawns 5 Bats units
- [ ] Visual: proper animations and effects
- [ ] Audio: character sounds and voice lines

