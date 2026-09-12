# Barbarian Barrel - Card Specification

## Basic Info
- **Card ID**: 26000058 (example)
- **Name**: Barbarian Barrel
- **Rarity**: Common
- **Type**: Spell (Spawn)
- **Elixir Cost**: 2
- **Unlock Arena**: Royal Arena (Arena 7)

## Statistics (Tournament Standard / Level 11)
| Stat | Value |
|------|-------|
| Damage | 200 (barrel roll) |
| Range | 5 tiles (linear) |
| Mechanic | Rolls 5 tiles, spawns barbarian at end |
| Spawns | 1 Barbarian (704 HP, 156 dmg) |

## Mechanics

1. Rolls 5 tiles, spawns barbarian at end

## Interactions

### Key Interactions
- Standard interactions for common spell (spawn)
- Refer to CARDS_DATABASE.md for detailed interaction chart

## Synergies

- Works well with tank units for protection
- Pairs with splash damage for swarm control
- Complements spell bait strategies

## Visual/Audio

- **Sprite**: Barbarian Barrel character/model
- **Animations**: Idle, Walk, Attack, Hit, Death, Spawn
- **Effects**: Standard spell (spawn) effects
- **Sound**: Character-appropriate audio cues
- **Voice Lines**: Character-specific lines

## Implementation Notes

```csharp
public class BarbarianBarrel : SpawnSpell
{
    // Implementation based on card stats
    protected override void InitializeStats()
    {
        Damage = 200 (barrel roll);
        Range = 5 tiles (linear);
    }
}
```

## Level Scaling

| Level | HP | Damage |
|-------|-----|--------|
| 1 | [Base] | [Base] |
| ... | ... | ... |
| 11 | [Base] | 200 (barrel roll) |
| 12 | [×1.1] | [×1.1] |
| 13 | [×1.21] | [×1.21] |
| 14 | [×1.33] | [×1.33] |

Formula: `Stat_Lvl_N = Stat_Lvl_1 * 1.1^(N-1)`

## Testing Checklist
- [ ] Damage = 200 (barrel roll) per hit
- [ ] Range = 5 tiles (linear)
- [ ] Visual: proper animations and effects
- [ ] Audio: character sounds and voice lines

