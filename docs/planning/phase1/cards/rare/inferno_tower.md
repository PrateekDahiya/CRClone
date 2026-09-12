# Inferno Tower - Card Specification

## Basic Info
- **Card ID**: 26000031 (example)
- **Name**: Inferno Tower
- **Rarity**: Rare
- **Type**: Building (Defensive)
- **Elixir Cost**: 5
- **Unlock Arena**: Royal Arena (Arena 7)

## Statistics (Tournament Standard / Level 11)
| Stat | Value |
|------|-------|
| Hitpoints | 1840 |
| Damage | 50 → 100 → 200 → 400 → 800 → 1600 (ramping) |
| Hit Speed | 0.4 sec |
| Range | 6 tiles |
| Deploy Time | 1 sec |
| Target | Air & Ground |
| Lifetime | 25 sec |
| Mechanic | Ramps damage like Inferno Dragon |

## Mechanics

1. Ramps damage like Inferno Dragon
2. Building - stationary, has lifetime, targets by path distance
3. Damage ramps up over continuous attack, resets on target change/stun

## Interactions

### Key Interactions
- Standard interactions for rare building (defensive)
- Refer to CARDS_DATABASE.md for detailed interaction chart

## Synergies

- Works well with tank units for protection
- Pairs with splash damage for swarm control
- Complements spell bait strategies

## Visual/Audio

- **Sprite**: Inferno Tower character/model
- **Animations**: Idle, Walk, Attack, Hit, Death, Spawn
- **Effects**: Standard building (defensive) effects
- **Sound**: Character-appropriate audio cues
- **Voice Lines**: Character-specific lines

## Implementation Notes

```csharp
public class InfernoTower : DefensiveBuilding
{
    // Implementation based on card stats
    protected override void InitializeStats()
    {
        MaxHP = 1840; // Level 11
        Damage = 50 → 100 → 200 → 400 → 800 → 1600 (ramping);
        HitSpeed = 0.4 sec;
        Range = 6 tiles;
        TargetType = TargetType.AirAndGround;
        Lifetime = 25 sec;
    }
}
```

## Level Scaling

| Level | HP | Damage |
|-------|-----|--------|
| 1 | [Base] | [Base] |
| ... | ... | ... |
| 11 | 1840 | 50 → 100 → 200 → 400 → 800 → 1600 (ramping) |
| 12 | [×1.1] | [×1.1] |
| 13 | [×1.21] | [×1.21] |
| 14 | [×1.33] | [×1.33] |

Formula: `Stat_Lvl_N = Stat_Lvl_1 * 1.1^(N-1)`

## Testing Checklist
- [ ] HP = 1840 at tournament standard
- [ ] Damage = 50 → 100 → 200 → 400 → 800 → 1600 (ramping) per hit
- [ ] Hit speed = 0.4 sec
- [ ] Range = 6 tiles
- [ ] Targets Air & Ground
- [ ] Lifetime = 25 sec
- [ ] Visual: proper animations and effects
- [ ] Audio: character sounds and voice lines

