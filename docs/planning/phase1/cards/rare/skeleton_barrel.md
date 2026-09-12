# Skeleton Barrel - Card Specification

## Basic Info
- **Card ID**: 26000066 (example)
- **Name**: Skeleton Barrel
- **Rarity**: Rare
- **Type**: Spell (Spawn/Flying)
- **Elixir Cost**: 3
- **Unlock Arena**: Bone Pit (Arena 2)

## Statistics (Tournament Standard / Level 11)
| Stat | Value |
|------|-------|
| Hitpoints | 400 (barrel) |
| Range | Anywhere (flying) |
| Speed | Fast |
| Target | Buildings only (barrel) |
| Death Spawns | 6 Skeletons (67 HP, 67 dmg each) |
| Mechanic | Flies to target, drops skeletons on death/destruction |

## Mechanics

1. Flies to target, drops skeletons on death/destruction
3. Flying unit - can cross river, targeted by air defenses
3. Death spawn / periodic spawn mechanic

## Interactions

### Key Interactions
- Standard interactions for rare spell (spawn/flying)
- Refer to CARDS_DATABASE.md for detailed interaction chart

## Synergies

- Works well with tank units for protection
- Pairs with splash damage for swarm control
- Complements spell bait strategies

## Visual/Audio

- **Sprite**: Skeleton Barrel character/model
- **Animations**: Idle, Walk, Attack, Hit, Death, Spawn
- **Effects**: Standard spell (spawn/flying) effects
- **Sound**: Character-appropriate audio cues
- **Voice Lines**: Character-specific lines

## Implementation Notes

```csharp
public class SkeletonBarrel : Troop
{
    // Implementation based on card stats
    protected override void InitializeStats()
    {
        MaxHP = 400 (barrel); // Level 11
        Range = Anywhere (flying);
        MoveSpeed = Fast;
        TargetType = TargetType.Buildingsonly(barrel);
    }
}
```

## Level Scaling

| Level | HP | Damage |
|-------|-----|--------|
| 1 | [Base] | [Base] |
| ... | ... | ... |
| 11 | 400 (barrel) | [Base] |
| 12 | [×1.1] | [×1.1] |
| 13 | [×1.21] | [×1.21] |
| 14 | [×1.33] | [×1.33] |

Formula: `Stat_Lvl_N = Stat_Lvl_1 * 1.1^(N-1)`

## Testing Checklist
- [ ] HP = 400 (barrel) at tournament standard
- [ ] Range = Anywhere (flying)
- [ ] Speed = Fast
- [ ] Targets Buildings only (barrel)
- [ ] Visual: proper animations and effects
- [ ] Audio: character sounds and voice lines

