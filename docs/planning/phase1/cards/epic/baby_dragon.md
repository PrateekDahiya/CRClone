# Baby Dragon - Card Specification

## Basic Info
- **Card ID**: 26000019 (example)
- **Name**: Baby Dragon
- **Rarity**: Epic
- **Type**: Troop (Flying/Splash)
- **Elixir Cost**: 4
- **Unlock Arena**: Royal Arena (Arena 7)

## Statistics (Tournament Standard / Level 11)
| Stat | Value |
|------|-------|
| Hitpoints | 1352 |
| Damage | 156 (area 1.5 tiles) |
| Hit Speed | 1.8 sec |
| Range | 3.5 tiles |
| Speed | Fast |
| Deploy Time | 1 sec |
| Target | Air & Ground |

## Mechanics

3. Flying unit - can cross river, targeted by air defenses

## Interactions

### Key Interactions
- Standard interactions for epic troop (flying/splash)
- Refer to CARDS_DATABASE.md for detailed interaction chart

## Synergies

- Works well with tank units for protection
- Pairs with splash damage for swarm control
- Complements spell bait strategies

## Visual/Audio

- **Sprite**: Baby Dragon character/model
- **Animations**: Idle, Walk, Attack, Hit, Death, Spawn
- **Effects**: Standard troop (flying/splash) effects
- **Sound**: Character-appropriate audio cues
- **Voice Lines**: Character-specific lines

## Implementation Notes

```csharp
public class BabyDragon : FlyingTroop
{
    // Implementation based on card stats
    protected override void InitializeStats()
    {
        MaxHP = 1352; // Level 11
        Damage = 156 (area 1.5 tiles);
        HitSpeed = 1.8 sec;
        Range = 3.5 tiles;
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
| 11 | 1352 | 156 (area 1.5 tiles) |
| 12 | [×1.1] | [×1.1] |
| 13 | [×1.21] | [×1.21] |
| 14 | [×1.33] | [×1.33] |

Formula: `Stat_Lvl_N = Stat_Lvl_1 * 1.1^(N-1)`

## Testing Checklist
- [ ] HP = 1352 at tournament standard
- [ ] Damage = 156 (area 1.5 tiles) per hit
- [ ] Hit speed = 1.8 sec
- [ ] Range = 3.5 tiles
- [ ] Speed = Fast
- [ ] Targets Air & Ground
- [ ] Visual: proper animations and effects
- [ ] Audio: character sounds and voice lines

