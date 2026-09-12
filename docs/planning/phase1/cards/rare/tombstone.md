# Tombstone - Card Specification

## Basic Info
- **Card ID**: 26000090 (example)
- **Name**: Tombstone
- **Rarity**: Rare
- **Type**: Building (Spawner)
- **Elixir Cost**: 3
- **Unlock Arena**: Bone Pit (Arena 2)

## Statistics (Tournament Standard / Level 11)
| Stat | Value |
|------|-------|
| Hitpoints | 672 |
| Deploy Time | 1 sec |
| Lifetime | 20 sec |
| Death Spawns | 4 Skeletons |
| Spawn | 1 Skeleton every 2.9 sec (max 6) |

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

- **Sprite**: Tombstone character/model
- **Animations**: Idle, Walk, Attack, Hit, Death, Spawn
- **Effects**: Standard building (spawner) effects
- **Sound**: Character-appropriate audio cues
- **Voice Lines**: Character-specific lines

## Implementation Notes

```csharp
public class Tombstone : SpawnerBuilding
{
    // Implementation based on card stats
    protected override void InitializeStats()
    {
        MaxHP = 672; // Level 11
        Lifetime = 20 sec;
    }
}
```

## Level Scaling

| Level | HP | Damage |
|-------|-----|--------|
| 1 | [Base] | [Base] |
| ... | ... | ... |
| 11 | 672 | [Base] |
| 12 | [×1.1] | [×1.1] |
| 13 | [×1.21] | [×1.21] |
| 14 | [×1.33] | [×1.33] |

Formula: `Stat_Lvl_N = Stat_Lvl_1 * 1.1^(N-1)`

## Testing Checklist
- [ ] HP = 672 at tournament standard
- [ ] Lifetime = 20 sec
- [ ] Visual: proper animations and effects
- [ ] Audio: character sounds and voice lines

