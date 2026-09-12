# Battle Ram - Card Specification

## Basic Info
- **Card ID**: 26000069 (example)
- **Name**: Battle Ram
- **Rarity**: Common
- **Type**: Troop (Building Target/Spawn)
- **Elixir Cost**: 4
- **Unlock Arena**: Royal Arena (Arena 7)

## Statistics (Tournament Standard / Level 11)
| Stat | Value |
|------|-------|
| Hitpoints | 1184 (ram), 2× Barbarians (704 HP each) |
| Damage | 280 (charge: 560), Barbarian: 156 |
| Hit Speed | 1.4 sec |
| Range | Melee (1.2 tiles), Charge: 3.5 tiles |
| Speed | Medium (Fast charging) |
| Deploy Time | 1 sec |
| Target | Buildings only (ram) |
| Mechanic | Charge damage, spawns 2 Barbarians on death |

## Mechanics

1. Charge damage, spawns 2 Barbarians on death
2. Building - stationary, has lifetime, targets by path distance

## Interactions

### Key Interactions
- Standard interactions for common troop (building target/spawn)
- Refer to CARDS_DATABASE.md for detailed interaction chart

## Synergies

- Works well with tank units for protection
- Pairs with splash damage for swarm control
- Complements spell bait strategies

## Visual/Audio

- **Sprite**: Battle Ram character/model
- **Animations**: Idle, Walk, Attack, Hit, Death, Spawn
- **Effects**: Standard troop (building target/spawn) effects
- **Sound**: Character-appropriate audio cues
- **Voice Lines**: Character-specific lines

## Implementation Notes

```csharp
public class BattleRam : BuildingTargetTroop
{
    // Implementation based on card stats
    protected override void InitializeStats()
    {
        MaxHP = 1184 (ram), 2× Barbarians (704 HP each); // Level 11
        Damage = 280 (charge: 560), Barbarian: 156;
        HitSpeed = 1.4 sec;
        Range = Melee (1.2 tiles), Charge: 3.5 tiles;
        MoveSpeed = Medium (Fast charging);
        TargetType = TargetType.Buildingsonly(ram);
    }
}
```

## Level Scaling

| Level | HP | Damage |
|-------|-----|--------|
| 1 | [Base] | [Base] |
| ... | ... | ... |
| 11 | 1184 (ram), 2× Barbarians (704 HP each) | 280 (charge: 560), Barbarian: 156 |
| 12 | [×1.1] | [×1.1] |
| 13 | [×1.21] | [×1.21] |
| 14 | [×1.33] | [×1.33] |

Formula: `Stat_Lvl_N = Stat_Lvl_1 * 1.1^(N-1)`

## Testing Checklist
- [ ] HP = 1184 (ram), 2× Barbarians (704 HP each) at tournament standard
- [ ] Damage = 280 (charge: 560), Barbarian: 156 per hit
- [ ] Hit speed = 1.4 sec
- [ ] Range = Melee (1.2 tiles), Charge: 3.5 tiles
- [ ] Speed = Medium (Fast charging)
- [ ] Targets Buildings only (ram)
- [ ] Visual: proper animations and effects
- [ ] Audio: character sounds and voice lines

