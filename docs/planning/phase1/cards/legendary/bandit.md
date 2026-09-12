# Bandit - Card Specification

## Basic Info
- **Card ID**: 26000018 (example)
- **Name**: Bandit
- **Rarity**: Legendary
- **Type**: Troop (Melee)
- **Elixir Cost**: 3
- **Unlock Arena**: Spell Valley (Arena 5)

## Statistics (Tournament Standard / Level 11)
| Stat | Value |
|------|-------|
| Hitpoints | 720 |
| Damage | 212 (dash: 318) |
| Hit Speed | 1.1 sec |
| Range | Melee (1.2 tiles), Dash: 5 tiles |
| Speed | Fast |
| Deploy Time | 1 sec |
| Target | Ground only |
| Mechanic | Dash charge (5 tile range, 318 dmg, invulnerable during dash) |

## Mechanics

1. Dash charge (5 tile range, 318 dmg, invulnerable during dash)

## Interactions

### Key Interactions
- Standard interactions for legendary troop (melee)
- Refer to CARDS_DATABASE.md for detailed interaction chart

## Synergies

- Works well with tank units for protection
- Pairs with splash damage for swarm control
- Complements spell bait strategies

## Visual/Audio

- **Sprite**: Bandit character/model
- **Animations**: Idle, Walk, Attack, Hit, Death, Spawn
- **Effects**: Standard troop (melee) effects
- **Sound**: Character-appropriate audio cues
- **Voice Lines**: Character-specific lines

## Implementation Notes

```csharp
public class Bandit : MeleeTroop
{
    // Implementation based on card stats
    protected override void InitializeStats()
    {
        MaxHP = 720; // Level 11
        Damage = 212 (dash: 318);
        HitSpeed = 1.1 sec;
        Range = Melee (1.2 tiles), Dash: 5 tiles;
        MoveSpeed = Fast;
        TargetType = TargetType.Groundonly;
    }
}
```

## Level Scaling

| Level | HP | Damage |
|-------|-----|--------|
| 1 | [Base] | [Base] |
| ... | ... | ... |
| 11 | 720 | 212 (dash: 318) |
| 12 | [×1.1] | [×1.1] |
| 13 | [×1.21] | [×1.21] |
| 14 | [×1.33] | [×1.33] |

Formula: `Stat_Lvl_N = Stat_Lvl_1 * 1.1^(N-1)`

## Testing Checklist
- [ ] HP = 720 at tournament standard
- [ ] Damage = 212 (dash: 318) per hit
- [ ] Hit speed = 1.1 sec
- [ ] Range = Melee (1.2 tiles), Dash: 5 tiles
- [ ] Speed = Fast
- [ ] Targets Ground only
- [ ] Visual: proper animations and effects
- [ ] Audio: character sounds and voice lines

