# Mega Knight - Card Specification

## Basic Info
- **Card ID**: 26000008 (example)
- **Name**: Mega Knight
- **Rarity**: Legendary
- **Type**: Troop (Melee/Area)
- **Elixir Cost**: 7
- **Unlock Arena**: Royal Arena (Arena 7)

## Statistics (Tournament Standard / Level 11)
| Stat | Value |
|------|-------|
| Hitpoints | 3344 |
| Damage | 264 (spawn jump: 480) |
| Hit Speed | 1.7 sec |
| Range | Melee (1.2 tiles), Jump: 3-4 tiles |
| Speed | Medium |
| Deploy Time | 1 sec |
| Target | Ground only |
| Mechanic | Spawn jump deals 480 area damage, jump attack every 3rd hit |

## Mechanics

1. Spawn jump deals 480 area damage, jump attack every 3rd hit

## Interactions

### Key Interactions
- Standard interactions for legendary troop (melee/area)
- Refer to CARDS_DATABASE.md for detailed interaction chart

## Synergies

- Works well with tank units for protection
- Pairs with splash damage for swarm control
- Complements spell bait strategies

## Visual/Audio

- **Sprite**: Mega Knight character/model
- **Animations**: Idle, Walk, Attack, Hit, Death, Spawn
- **Effects**: Standard troop (melee/area) effects
- **Sound**: Character-appropriate audio cues
- **Voice Lines**: Character-specific lines

## Implementation Notes

```csharp
public class MegaKnight : MeleeTroop
{
    // Implementation based on card stats
    protected override void InitializeStats()
    {
        MaxHP = 3344; // Level 11
        Damage = 264 (spawn jump: 480);
        HitSpeed = 1.7 sec;
        Range = Melee (1.2 tiles), Jump: 3-4 tiles;
        MoveSpeed = Medium;
        TargetType = TargetType.Groundonly;
    }
}
```

## Level Scaling

| Level | HP | Damage |
|-------|-----|--------|
| 1 | [Base] | [Base] |
| ... | ... | ... |
| 11 | 3344 | 264 (spawn jump: 480) |
| 12 | [×1.1] | [×1.1] |
| 13 | [×1.21] | [×1.21] |
| 14 | [×1.33] | [×1.33] |

Formula: `Stat_Lvl_N = Stat_Lvl_1 * 1.1^(N-1)`

## Testing Checklist
- [ ] HP = 3344 at tournament standard
- [ ] Damage = 264 (spawn jump: 480) per hit
- [ ] Hit speed = 1.7 sec
- [ ] Range = Melee (1.2 tiles), Jump: 3-4 tiles
- [ ] Speed = Medium
- [ ] Targets Ground only
- [ ] Visual: proper animations and effects
- [ ] Audio: character sounds and voice lines

