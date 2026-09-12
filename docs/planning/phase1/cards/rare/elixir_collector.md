# Elixir Collector - Card Specification

## Basic Info
- **Card ID**: 26000088 (example)
- **Name**: Elixir Collector
- **Rarity**: Rare
- **Type**: Building (Economy)
- **Elixir Cost**: 6
- **Unlock Arena**: Royal Arena (Arena 7)

## Statistics (Tournament Standard / Level 11)
| Stat | Value |
|------|-------|
| Hitpoints | 1440 |
| Deploy Time | 1 sec |
| Lifetime | 70 sec |
| Production | 1 elixir every 9.8 sec (8 total over lifetime) |
| Mechanic | Generates elixir profit over time |

## Mechanics

1. Generates elixir profit over time
2. Building - stationary, has lifetime, targets by path distance

## Interactions

### Key Interactions
- Standard interactions for rare building (economy)
- Refer to CARDS_DATABASE.md for detailed interaction chart

## Synergies

- Works well with tank units for protection
- Pairs with splash damage for swarm control
- Complements spell bait strategies

## Visual/Audio

- **Sprite**: Elixir Collector character/model
- **Animations**: Idle, Walk, Attack, Hit, Death, Spawn
- **Effects**: Standard building (economy) effects
- **Sound**: Character-appropriate audio cues
- **Voice Lines**: Character-specific lines

## Implementation Notes

```csharp
public class ElixirCollector : EconomyBuilding
{
    // Implementation based on card stats
    protected override void InitializeStats()
    {
        MaxHP = 1440; // Level 11
        Lifetime = 70 sec;
    }
}
```

## Level Scaling

| Level | HP | Damage |
|-------|-----|--------|
| 1 | [Base] | [Base] |
| ... | ... | ... |
| 11 | 1440 | [Base] |
| 12 | [×1.1] | [×1.1] |
| 13 | [×1.21] | [×1.21] |
| 14 | [×1.33] | [×1.33] |

Formula: `Stat_Lvl_N = Stat_Lvl_1 * 1.1^(N-1)`

## Testing Checklist
- [ ] HP = 1440 at tournament standard
- [ ] Lifetime = 70 sec
- [ ] Visual: proper animations and effects
- [ ] Audio: character sounds and voice lines

