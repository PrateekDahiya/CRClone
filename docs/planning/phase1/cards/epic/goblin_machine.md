# Goblin Machine - Card Specification

## Basic Info
- **Card ID**: 26000075 (example)
- **Name**: Goblin Machine
- **Rarity**: Epic
- **Type**: Troop (Vehicle/Spawn)
- **Elixir Cost**: 6
- **Unlock Arena**: Royal Arena (Arena 7)

## Statistics (Tournament Standard / Level 11)
| Stat | Value |
|------|-------|
| Hitpoints | 2240 (machine), 4× Goblin (264 HP each) |
| Damage | 120 (machine), Goblin: 104 |
| Hit Speed | 1.2 sec |
| Range | 5.5 tiles |
| Speed | Medium |
| Deploy Time | 1 sec |
| Target | Air & Ground |
| Mechanic | Vehicle with 4 goblins inside, spawns goblins on death |

## Mechanics

1. Vehicle with 4 goblins inside, spawns goblins on death

## Interactions

### Key Interactions
- Standard interactions for epic troop (vehicle/spawn)
- Refer to CARDS_DATABASE.md for detailed interaction chart

## Synergies

- Works well with tank units for protection
- Pairs with splash damage for swarm control
- Complements spell bait strategies

## Visual/Audio

- **Sprite**: Goblin Machine character/model
- **Animations**: Idle, Walk, Attack, Hit, Death, Spawn
- **Effects**: Standard troop (vehicle/spawn) effects
- **Sound**: Character-appropriate audio cues
- **Voice Lines**: Character-specific lines

## Implementation Notes

```csharp
public class GoblinMachine : VehicleTroop
{
    // Implementation based on card stats
    protected override void InitializeStats()
    {
        MaxHP = 2240 (machine), 4× Goblin (264 HP each); // Level 11
        Damage = 120 (machine), Goblin: 104;
        HitSpeed = 1.2 sec;
        Range = 5.5 tiles;
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
| 11 | 2240 (machine), 4× Goblin (264 HP each) | 120 (machine), Goblin: 104 |
| 12 | [×1.1] | [×1.1] |
| 13 | [×1.21] | [×1.21] |
| 14 | [×1.33] | [×1.33] |

Formula: `Stat_Lvl_N = Stat_Lvl_1 * 1.1^(N-1)`

## Testing Checklist
- [ ] HP = 2240 (machine), 4× Goblin (264 HP each) at tournament standard
- [ ] Damage = 120 (machine), Goblin: 104 per hit
- [ ] Hit speed = 1.2 sec
- [ ] Range = 5.5 tiles
- [ ] Speed = Medium
- [ ] Targets Air & Ground
- [ ] Visual: proper animations and effects
- [ ] Audio: character sounds and voice lines

