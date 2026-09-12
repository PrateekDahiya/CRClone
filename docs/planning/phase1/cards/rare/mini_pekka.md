# Mini P.E.K.K.A - Card Specification

## Basic Info
- **Card ID**: 26000052 (example)
- **Name**: Mini P.E.K.K.A
- **Rarity**: Rare
- **Type**: Troop (Melee)
- **Elixir Cost**: 4
- **Unlock Arena**: P.E.K.K.A's Playhouse (Arena 4)

## Statistics (Tournament Standard / Level 11)
| Stat | Value |
|------|-------|
| Hitpoints | 1280 |
| Damage | 572 |
| Hit Speed | 1.8 sec |
| Range | Melee (1.2 tiles) |
| Speed | Fast |
| Deploy Time | 1 sec |
| Target | Ground only |

## Mechanics


## Interactions

### Key Interactions
- Standard interactions for rare troop (melee)
- Refer to CARDS_DATABASE.md for detailed interaction chart

## Synergies

- Works well with tank units for protection
- Pairs with splash damage for swarm control
- Complements spell bait strategies

## Visual/Audio

- **Sprite**: Mini P.E.K.K.A character/model
- **Animations**: Idle, Walk, Attack, Hit, Death, Spawn
- **Effects**: Standard troop (melee) effects
- **Sound**: Character-appropriate audio cues
- **Voice Lines**: Character-specific lines

## Implementation Notes

```csharp
public class MiniPEKKA : MeleeTroop
{
    // Implementation based on card stats
    protected override void InitializeStats()
    {
        MaxHP = 1280; // Level 11
        Damage = 572;
        HitSpeed = 1.8 sec;
        Range = Melee (1.2 tiles);
        MoveSpeed = Fast;
        TargetType = TargetType.Groundonly;
    }
}
```

## Level Scaling

| Level | HP | Damage |
|-------|-----|--------|
| 1 | [Base] | [Base] |
| ... | ... | ... |
| 11 | 1280 | 572 |
| 12 | [×1.1] | [×1.1] |
| 13 | [×1.21] | [×1.21] |
| 14 | [×1.33] | [×1.33] |

Formula: `Stat_Lvl_N = Stat_Lvl_1 * 1.1^(N-1)`

## Testing Checklist
- [ ] HP = 1280 at tournament standard
- [ ] Damage = 572 per hit
- [ ] Hit speed = 1.8 sec
- [ ] Range = Melee (1.2 tiles)
- [ ] Speed = Fast
- [ ] Targets Ground only
- [ ] Visual: proper animations and effects
- [ ] Audio: character sounds and voice lines

