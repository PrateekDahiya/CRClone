# Goblin Cage - Card Specification

## Basic Info
- **Card ID**: 26000045 (example)
- **Name**: Goblin Cage
- **Rarity**: Rare
- **Type**: Building (Spawner/Tank)
- **Elixir Cost**: 4
- **Unlock Arena**: Royal Arena (Arena 7)

## Statistics (Tournament Standard / Level 11)
| Stat | Value |
|------|-------|
| Hitpoints | 2080 (cage), 1248 (Goblin Brawler) |
| Deploy Time | 1 sec |
| Lifetime | 30 sec (or until destroyed) |
| Brawler Stats | 1248 HP, 216 dmg, 1.4 hit speed, melee |
| Spawn | Goblin Brawler on death |

## Mechanics

2. Building - stationary, has lifetime, targets by path distance
3. Death spawn / periodic spawn mechanic

## Interactions

### Key Interactions
- Standard interactions for rare building (spawner/tank)
- Refer to CARDS_DATABASE.md for detailed interaction chart

## Synergies

- Works well with tank units for protection
- Pairs with splash damage for swarm control
- Complements spell bait strategies

## Visual/Audio

- **Sprite**: Goblin Cage character/model
- **Animations**: Idle, Walk, Attack, Hit, Death, Spawn
- **Effects**: Standard building (spawner/tank) effects
- **Sound**: Character-appropriate audio cues
- **Voice Lines**: Character-specific lines

## Implementation Notes

```csharp
public class GoblinCage : SpawnerBuilding
{
    // Implementation based on card stats
    protected override void InitializeStats()
    {
        MaxHP = 2080 (cage), 1248 (Goblin Brawler); // Level 11
        Lifetime = 30 sec (or until destroyed);
    }
}
```

## Level Scaling

| Level | HP | Damage |
|-------|-----|--------|
| 1 | [Base] | [Base] |
| ... | ... | ... |
| 11 | 2080 (cage), 1248 (Goblin Brawler) | [Base] |
| 12 | [×1.1] | [×1.1] |
| 13 | [×1.21] | [×1.21] |
| 14 | [×1.33] | [×1.33] |

Formula: `Stat_Lvl_N = Stat_Lvl_1 * 1.1^(N-1)`

## Testing Checklist
- [ ] HP = 2080 (cage), 1248 (Goblin Brawler) at tournament standard
- [ ] Lifetime = 30 sec (or until destroyed)
- [ ] Visual: proper animations and effects
- [ ] Audio: character sounds and voice lines

