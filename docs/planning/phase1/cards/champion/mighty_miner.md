# Mighty Miner - Card Specification

## Basic Info
- **Card ID**: 26000093 (example)
- **Name**: Mighty Miner
- **Rarity**: Champion
- **Type**: Troop
- **Elixir Cost**: ?
- **Unlock Arena**: Legendary Arena (Arena 15)

## Statistics (Tournament Standard / Level 11)
| Stat | Value |
|------|-------|

## Mechanics


## Interactions

### Key Interactions
- Standard interactions for champion troop
- Refer to CARDS_DATABASE.md for detailed interaction chart

## Synergies

- Works well with tank units for protection
- Pairs with splash damage for swarm control
- Complements spell bait strategies

## Visual/Audio

- **Sprite**: Mighty Miner character/model
- **Animations**: Idle, Walk, Attack, Hit, Death, Spawn
- **Effects**: Standard troop effects
- **Sound**: Character-appropriate audio cues
- **Voice Lines**: Character-specific lines

## Implementation Notes

```csharp
public class MightyMiner : Troop
{
    // Implementation based on card stats
    protected override void InitializeStats()
    {
    }
}
```

## Level Scaling

| Level | HP | Damage |
|-------|-----|--------|
| 1 | [Base] | [Base] |
| ... | ... | ... |
| 11 | [Base] | [Base] |
| 12 | [×1.1] | [×1.1] |
| 13 | [×1.21] | [×1.21] |
| 14 | [×1.33] | [×1.33] |

Formula: `Stat_Lvl_N = Stat_Lvl_1 * 1.1^(N-1)`

## Testing Checklist
- [ ] Visual: proper animations and effects
- [ ] Audio: character sounds and voice lines

