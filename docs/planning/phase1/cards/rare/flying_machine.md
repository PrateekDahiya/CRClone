# Flying Machine - Card Specification

## Basic Info
- **Card ID**: 26000067 (example)
- **Name**: Flying Machine
- **Rarity**: Rare
- **Type**: Troop (Flying/Ranged)
- **Elixir Cost**: 4
- **Unlock Arena**: Royal Arena (Arena 7)

## Statistics (Tournament Standard / Level 11)
| Stat | Value |
|------|-------|
| Hitpoints | 512 |
| Damage | 120 |
| Hit Speed | 0.8 sec |
| Range | 6 tiles |
| Speed | Fast |
| Deploy Time | 1 sec |
| Target | Air & Ground |

## Mechanics

3. Flying unit - can cross river, targeted by air defenses

## Interactions

### Key Interactions
- Standard interactions for rare troop (flying/ranged)
- Refer to CARDS_DATABASE.md for detailed interaction chart

## Synergies

- Works well with tank units for protection
- Pairs with splash damage for swarm control
- Complements spell bait strategies

## Visual/Audio

- **Sprite**: Flying Machine character/model
- **Animations**: Idle, Walk, Attack, Hit, Death, Spawn
- **Effects**: Standard troop (flying/ranged) effects
- **Sound**: Character-appropriate audio cues
- **Voice Lines**: Character-specific lines

## Implementation Notes

```csharp
public class FlyingMachine : Troop
{
    // Implementation based on card stats
    protected override void InitializeStats()
    {
        MaxHP = 512; // Level 11
        Damage = 120;
        HitSpeed = 0.8 sec;
        Range = 6 tiles;
        MoveSpeed = Fast;
        TargetType = TargetType.AirAndGround;
    }
}
```

## Level Scaling

| Level | HP | Damage |
|-------|-----|--------|
| 1 | [Base] | [Base] |
| ... | ... | ... |
| 11 | 512 | 120 |
| 12 | [×1.1] | [×1.1] |
| 13 | [×1.21] | [×1.21] |
| 14 | [×1.33] | [×1.33] |

Formula: `Stat_Lvl_N = Stat_Lvl_1 * 1.1^(N-1)`

## Testing Checklist
- [ ] HP = 512 at tournament standard
- [ ] Damage = 120 per hit
- [ ] Hit speed = 0.8 sec
- [ ] Range = 6 tiles
- [ ] Speed = Fast
- [ ] Targets Air & Ground
- [ ] Visual: proper animations and effects
- [ ] Audio: character sounds and voice lines

