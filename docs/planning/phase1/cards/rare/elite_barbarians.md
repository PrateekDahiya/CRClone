# Elite Barbarians - Card Specification

## Basic Info
- **Card ID**: 26000043 (example)
- **Name**: Elite Barbarians
- **Rarity**: Rare
- **Type**: Troop (Melee/Swarm)
- **Elixir Cost**: 6
- **Unlock Arena**: Royal Arena (Arena 7)

## Statistics (Tournament Standard / Level 11)
| Stat | Value |
|------|-------|
| Hitpoints | 1344 each (×2) |
| Damage | 230 each |
| Hit Speed | 1.4 sec |
| Range | Melee (1.2 tiles) |
| Speed | Very Fast |
| Deploy Time | 1 sec |
| Target | Ground only |
| Count | 2 Elite Barbarians |

## Mechanics

2. Swarm unit - multiple units deployed together

## Interactions

### Key Interactions
- Standard interactions for rare troop (melee/swarm)
- Refer to CARDS_DATABASE.md for detailed interaction chart

## Synergies

- Works well with tank units for protection
- Pairs with splash damage for swarm control
- Complements spell bait strategies

## Visual/Audio

- **Sprite**: Elite Barbarians character/model
- **Animations**: Idle, Walk, Attack, Hit, Death, Spawn
- **Effects**: Standard troop (melee/swarm) effects
- **Sound**: Character-appropriate audio cues
- **Voice Lines**: Character-specific lines

## Implementation Notes

```csharp
public class EliteBarbarians : SwarmTroop
{
    // Implementation based on card stats
    protected override void InitializeStats()
    {
        MaxHP = 1344 each (×2); // Level 11
        Damage = 230 each;
        HitSpeed = 1.4 sec;
        Range = Melee (1.2 tiles);
        MoveSpeed = Very Fast;
        TargetType = TargetType.Groundonly;
        UnitCount = 2 Elite Barbarians;
    }
}
```

## Level Scaling

| Level | HP | Damage |
|-------|-----|--------|
| 1 | [Base] | [Base] |
| ... | ... | ... |
| 11 | 1344 each (×2) | 230 each |
| 12 | [×1.1] | [×1.1] |
| 13 | [×1.21] | [×1.21] |
| 14 | [×1.33] | [×1.33] |

Formula: `Stat_Lvl_N = Stat_Lvl_1 * 1.1^(N-1)`

## Testing Checklist
- [ ] HP = 1344 each (×2) at tournament standard
- [ ] Damage = 230 each per hit
- [ ] Hit speed = 1.4 sec
- [ ] Range = Melee (1.2 tiles)
- [ ] Speed = Very Fast
- [ ] Targets Ground only
- [ ] Spawns 2 Elite Barbarians units
- [ ] Visual: proper animations and effects
- [ ] Audio: character sounds and voice lines

