# Ice Spirit - Card Specification

## Basic Info
- **Card ID**: 26000010 (example)
- **Name**: Ice Spirit
- **Rarity**: Common
- **Type**: Troop (Melee/Spell)
- **Elixir Cost**: 1
- **Unlock Arena**: Spell Valley (Arena 5)

## Statistics (Tournament Standard / Level 11)
| Stat | Value |
|------|-------|
| Hitpoints | 132 |
| Damage | 66 (freeze: 1.5 sec) |
| Hit Speed | 1.1 sec |
| Range | Melee (2.5 tiles jump) |
| Speed | Very Fast |
| Deploy Time | 1 sec |
| Target | Ground only |
| Mechanic | Jumps to target, freezes for 1.5 sec on death/hit |

## Mechanics

1. Jumps to target, freezes for 1.5 sec on death/hit
2. Spell - instant or delayed effect, can be placed anywhere

## Interactions

### Key Interactions
- Standard interactions for common troop (melee/spell)
- Refer to CARDS_DATABASE.md for detailed interaction chart

## Synergies

- Works well with tank units for protection
- Pairs with splash damage for swarm control
- Complements spell bait strategies

## Visual/Audio

- **Sprite**: Ice Spirit character/model
- **Animations**: Idle, Walk, Attack, Hit, Death, Spawn
- **Effects**: Standard troop (melee/spell) effects
- **Sound**: Character-appropriate audio cues
- **Voice Lines**: Character-specific lines

## Implementation Notes

```csharp
public class IceSpirit : MeleeTroop
{
    // Implementation based on card stats
    protected override void InitializeStats()
    {
        MaxHP = 132; // Level 11
        Damage = 66 (freeze: 1.5 sec);
        HitSpeed = 1.1 sec;
        Range = Melee (2.5 tiles jump);
        MoveSpeed = Very Fast;
        TargetType = TargetType.Groundonly;
    }
}
```

## Level Scaling

| Level | HP | Damage |
|-------|-----|--------|
| 1 | [Base] | [Base] |
| ... | ... | ... |
| 11 | 132 | 66 (freeze: 1.5 sec) |
| 12 | [×1.1] | [×1.1] |
| 13 | [×1.21] | [×1.21] |
| 14 | [×1.33] | [×1.33] |

Formula: `Stat_Lvl_N = Stat_Lvl_1 * 1.1^(N-1)`

## Testing Checklist
- [ ] HP = 132 at tournament standard
- [ ] Damage = 66 (freeze: 1.5 sec) per hit
- [ ] Hit speed = 1.1 sec
- [ ] Range = Melee (2.5 tiles jump)
- [ ] Speed = Very Fast
- [ ] Targets Ground only
- [ ] Visual: proper animations and effects
- [ ] Audio: character sounds and voice lines

