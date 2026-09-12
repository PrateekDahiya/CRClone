# Mortar - Card Specification

## Basic Info
- **Card ID**: 26000086 (example)
- **Name**: Mortar
- **Rarity**: Rare
- **Type**: Building (Siege)
- **Elixir Cost**: 4
- **Unlock Arena**: P.E.K.K.A's Playhouse (Arena 4)

## Statistics (Tournament Standard / Level 11)
| Stat | Value |
|------|-------|
| Hitpoints | 1256 |
| Damage | 220 (area 1.5 tiles) |
| Hit Speed | 5 sec |
| Range | 4-11.5 tiles (dead zone 0-4) |
| Deploy Time | 1 sec |
| Target | Ground only |
| Lifetime | 30 sec |
| Mechanic | Dead zone 4 tiles, long range siege |

## Mechanics

1. Dead zone 4 tiles, long range siege
2. Building - stationary, has lifetime, targets by path distance

## Interactions

### Key Interactions
- Standard interactions for rare building (siege)
- Refer to CARDS_DATABASE.md for detailed interaction chart

## Synergies

- Works well with tank units for protection
- Pairs with splash damage for swarm control
- Complements spell bait strategies

## Visual/Audio

- **Sprite**: Mortar character/model
- **Animations**: Idle, Walk, Attack, Hit, Death, Spawn
- **Effects**: Standard building (siege) effects
- **Sound**: Character-appropriate audio cues
- **Voice Lines**: Character-specific lines

## Implementation Notes

```csharp
public class Mortar : SiegeBuilding
{
    // Implementation based on card stats
    protected override void InitializeStats()
    {
        MaxHP = 1256; // Level 11
        Damage = 220 (area 1.5 tiles);
        HitSpeed = 5 sec;
        Range = 4-11.5 tiles (dead zone 0-4);
        TargetType = TargetType.Groundonly;
        Lifetime = 30 sec;
    }
}
```

## Level Scaling

| Level | HP | Damage |
|-------|-----|--------|
| 1 | [Base] | [Base] |
| ... | ... | ... |
| 11 | 1256 | 220 (area 1.5 tiles) |
| 12 | [×1.1] | [×1.1] |
| 13 | [×1.21] | [×1.21] |
| 14 | [×1.33] | [×1.33] |

Formula: `Stat_Lvl_N = Stat_Lvl_1 * 1.1^(N-1)`

## Testing Checklist
- [ ] HP = 1256 at tournament standard
- [ ] Damage = 220 (area 1.5 tiles) per hit
- [ ] Hit speed = 5 sec
- [ ] Range = 4-11.5 tiles (dead zone 0-4)
- [ ] Targets Ground only
- [ ] Lifetime = 30 sec
- [ ] Visual: proper animations and effects
- [ ] Audio: character sounds and voice lines

