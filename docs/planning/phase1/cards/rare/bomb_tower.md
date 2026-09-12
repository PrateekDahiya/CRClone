# Bomb Tower - Card Specification

## Basic Info
- **Card ID**: 26000085 (example)
- **Name**: Bomb Tower
- **Rarity**: Rare
- **Type**: Building (Defensive/Splash)
- **Elixir Cost**: 4
- **Unlock Arena**: Royal Arena (Arena 7)

## Statistics (Tournament Standard / Level 11)
| Stat | Value |
|------|-------|
| Hitpoints | 1440 |
| Damage | 224 (area 1.5 tiles) |
| Hit Speed | 1.7 sec |
| Range | 6 tiles |
| Deploy Time | 1 sec |
| Target | Ground only |
| Lifetime | 30 sec |

## Mechanics

2. Building - stationary, has lifetime, targets by path distance

## Interactions

### Key Interactions
- Standard interactions for rare building (defensive/splash)
- Refer to CARDS_DATABASE.md for detailed interaction chart

## Synergies

- Works well with tank units for protection
- Pairs with splash damage for swarm control
- Complements spell bait strategies

## Visual/Audio

- **Sprite**: Bomb Tower character/model
- **Animations**: Idle, Walk, Attack, Hit, Death, Spawn
- **Effects**: Standard building (defensive/splash) effects
- **Sound**: Character-appropriate audio cues
- **Voice Lines**: Character-specific lines

## Implementation Notes

```csharp
public class BombTower : Troop
{
    // Implementation based on card stats
    protected override void InitializeStats()
    {
        MaxHP = 1440; // Level 11
        Damage = 224 (area 1.5 tiles);
        HitSpeed = 1.7 sec;
        Range = 6 tiles;
        TargetType = TargetType.Groundonly;
        Lifetime = 30 sec;
    }
}
```

## Level Scaling

| Level | HP | Damage |
|-------|-----|--------|
| 1 | [Base] | [Base] |
| ... | ... | ... |
| 11 | 1440 | 224 (area 1.5 tiles) |
| 12 | [×1.1] | [×1.1] |
| 13 | [×1.21] | [×1.21] |
| 14 | [×1.33] | [×1.33] |

Formula: `Stat_Lvl_N = Stat_Lvl_1 * 1.1^(N-1)`

## Testing Checklist
- [ ] HP = 1440 at tournament standard
- [ ] Damage = 224 (area 1.5 tiles) per hit
- [ ] Hit speed = 1.7 sec
- [ ] Range = 6 tiles
- [ ] Targets Ground only
- [ ] Lifetime = 30 sec
- [ ] Visual: proper animations and effects
- [ ] Audio: character sounds and voice lines

