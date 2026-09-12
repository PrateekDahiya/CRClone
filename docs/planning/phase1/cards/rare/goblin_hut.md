# Goblin Hut - Card Specification

## Basic Info
- **Card ID**: 26000030 (example)
- **Name**: Goblin Hut
- **Rarity**: Rare
- **Type**: Building (Spawner)
- **Elixir Cost**: 5
- **Unlock Arena**: P.E.K.K.A's Playhouse (Arena 4)

## Statistics (Tournament Standard / Level 11)
| Stat | Value |
|------|-------|
| Hitpoints | 1296 |
| Deploy Time | 1 sec |
| Lifetime | 30 sec |
| Spawn | 1 Spear Goblin every 4.9 sec (max 6) |

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

- **Sprite**: Goblin Hut character/model
- **Animations**: Idle, Walk, Attack, Hit, Death, Spawn
- **Effects**: Standard building (spawner) effects
- **Sound**: Character-appropriate audio cues
- **Voice Lines**: Character-specific lines

## Implementation Notes

```csharp
public class GoblinHut : SpawnerBuilding
{
    // Implementation based on card stats
    protected override void InitializeStats()
    {
        MaxHP = 1296; // Level 11
        Lifetime = 30 sec;
    }
}
```

## Level Scaling

| Level | HP | Damage |
|-------|-----|--------|
| 1 | [Base] | [Base] |
| ... | ... | ... |
| 11 | 1296 | [Base] |
| 12 | [×1.1] | [×1.1] |
| 13 | [×1.21] | [×1.21] |
| 14 | [×1.33] | [×1.33] |

Formula: `Stat_Lvl_N = Stat_Lvl_1 * 1.1^(N-1)`

## Testing Checklist
- [ ] HP = 1296 at tournament standard
- [ ] Lifetime = 30 sec
- [ ] Visual: proper animations and effects
- [ ] Audio: character sounds and voice lines

