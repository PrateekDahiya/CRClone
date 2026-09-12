# Furnace - Card Specification

## Basic Info
- **Card ID**: 26000089 (example)
- **Name**: Furnace
- **Rarity**: Rare
- **Type**: Building (Spawner)
- **Elixir Cost**: 4
- **Unlock Arena**: P.E.K.K.A's Playhouse (Arena 4)

## Statistics (Tournament Standard / Level 11)
| Stat | Value |
|------|-------|
| Hitpoints | 1200 |
| Deploy Time | 1 sec |
| Lifetime | 40 sec |
| Spawn | 2 Fire Spirits every 10 sec (max 4 waves) |

## Mechanics

2. Building - stationary, has lifetime, targets by path distance
3. Death spawn / periodic spawn mechanic

## Interactions

### Key Interactions
- Standard interactions for rare building (spawner)
- Refer to CARDS_DATABASE.md for detailed interaction chart

## Synergies

- Works well with tank units for protection
- Pairs with splash damage for swarm control
- Complements spell bait strategies

## Visual/Audio

- **Sprite**: Furnace character/model
- **Animations**: Idle, Walk, Attack, Hit, Death, Spawn
- **Effects**: Standard building (spawner) effects
- **Sound**: Character-appropriate audio cues
- **Voice Lines**: Character-specific lines

## Implementation Notes

```csharp
public class Furnace : SpawnerBuilding
{
    // Implementation based on card stats
    protected override void InitializeStats()
    {
        MaxHP = 1200; // Level 11
        Lifetime = 40 sec;
    }
}
```

## Level Scaling

| Level | HP | Damage |
|-------|-----|--------|
| 1 | [Base] | [Base] |
| ... | ... | ... |
| 11 | 1200 | [Base] |
| 12 | [×1.1] | [×1.1] |
| 13 | [×1.21] | [×1.21] |
| 14 | [×1.33] | [×1.33] |

Formula: `Stat_Lvl_N = Stat_Lvl_1 * 1.1^(N-1)`

## Testing Checklist
- [ ] HP = 1200 at tournament standard
- [ ] Lifetime = 40 sec
- [ ] Visual: proper animations and effects
- [ ] Audio: character sounds and voice lines

