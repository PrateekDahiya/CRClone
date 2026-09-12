# Fisherman - Card Specification

## Basic Info
- **Card ID**: 26000047 (example)
- **Name**: Fisherman
- **Rarity**: Epic
- **Type**: Troop (Melee/Hook)
- **Elixir Cost**: 3
- **Unlock Arena**: Royal Arena (Arena 7)

## Statistics (Tournament Standard / Level 11)
| Stat | Value |
|------|-------|
| Hitpoints | 768 |
| Damage | 156 (hook pull) |
| Hit Speed | 1.5 sec |
| Range | Melee (1.2 tiles), Hook: 7 tiles |
| Speed | Medium |
| Deploy Time | 1 sec |
| Target | Ground only |
| Mechanic | Hook pulls enemy 4 tiles, stuns 0.5s |

## Mechanics

1. Hook pulls enemy 4 tiles, stuns 0.5s

## Interactions

### Key Interactions
- Standard interactions for epic troop (melee/hook)
- Refer to CARDS_DATABASE.md for detailed interaction chart

## Synergies

- Works well with tank units for protection
- Pairs with splash damage for swarm control
- Complements spell bait strategies

## Visual/Audio

- **Sprite**: Fisherman character/model
- **Animations**: Idle, Walk, Attack, Hit, Death, Spawn
- **Effects**: Standard troop (melee/hook) effects
- **Sound**: Character-appropriate audio cues
- **Voice Lines**: Character-specific lines

## Implementation Notes

```csharp
public class Fisherman : MeleeTroop
{
    // Implementation based on card stats
    protected override void InitializeStats()
    {
        MaxHP = 768; // Level 11
        Damage = 156 (hook pull);
        HitSpeed = 1.5 sec;
        Range = Melee (1.2 tiles), Hook: 7 tiles;
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
| 11 | 768 | 156 (hook pull) |
| 12 | [×1.1] | [×1.1] |
| 13 | [×1.21] | [×1.21] |
| 14 | [×1.33] | [×1.33] |

Formula: `Stat_Lvl_N = Stat_Lvl_1 * 1.1^(N-1)`

## Testing Checklist
- [ ] HP = 768 at tournament standard
- [ ] Damage = 156 (hook pull) per hit
- [ ] Hit speed = 1.5 sec
- [ ] Range = Melee (1.2 tiles), Hook: 7 tiles
- [ ] Speed = Medium
- [ ] Targets Ground only
- [ ] Visual: proper animations and effects
- [ ] Audio: character sounds and voice lines

