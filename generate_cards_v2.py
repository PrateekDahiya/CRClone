#!/usr/bin/env python3
"""
Proper card generator that parses CARDS_DATABASE.md and creates individual card files
with correct final rarities based on "Rarity: X → Y (changed)" notation.
"""

import re
import os
from pathlib import Path

# Read database
with open("docs/planning/phase1/CARDS_DATABASE.md", "r", encoding="utf-8") as f:
    content = f.read()

# Parse cards
cards = []
current_section = ""
card_id = 0

# Split by lines
lines = content.split('\n')
i = 0
while i < len(lines):
    line = lines[i].strip()
    
    # Detect section
    if line.startswith('## '):
        if 'LEGENDARY' in line:
            current_section = 'Legendary'
        elif 'EPIC' in line:
            current_section = 'Epic'
        elif 'RARE' in line:
            current_section = 'Rare'
        elif 'COMMON' in line:
            current_section = 'Common'
        elif 'CHAMPION' in line:
            current_section = 'Champion'
    
    # Detect card header
    match = re.match(r'###\s+(\d+)\.\s+(.+)', line)
    if match:
        card_num = int(match.group(1))
        card_name = match.group(2).strip()
        
        # Skip "listed above" entries
        if 'listed above' in card_name.lower():
            i += 1
            while i < len(lines):
                next_line = lines[i].strip()
                if next_line.startswith('###') or next_line.startswith('##'):
                    break
                i += 1
            continue
        
        # Skip champion entries in epic section (they have their own section)
        if current_section == 'Epic' and 'Champion' in card_name:
            i += 1
            while i < len(lines):
                next_line = lines[i].strip()
                if next_line.startswith('###') or next_line.startswith('##'):
                    break
                i += 1
            continue
        
        # Parse card properties
        card_data = {
            'name': card_name,
            'number': card_num,
            'section': current_section,
            'properties': {}
        }
        
        i += 1
        while i < len(lines):
            next_line = lines[i].strip()
            if next_line.startswith('###') or next_line.startswith('##'):
                break
            
            prop_match = re.match(r'-\s+\*\*(.+?)\*\*:\s*(.+)', next_line)
            if prop_match:
                key = prop_match.group(1).strip()
                value = prop_match.group(2).strip()
                card_data['properties'][key] = value
            
            i += 1
        
        cards.append(card_data)
        continue
    
    i += 1

# Determine final rarity for each card
def get_final_rarity(card):
    rarity = card['properties'].get('Rarity', card['section'])
    # Check for "X → Y (changed)" pattern (using Unicode RIGHTWARDS ARROW)
    if '\u2192' in rarity and '(changed)' in rarity:
        # Extract the final rarity
        match = re.search(r'\u2192\s+(\w+)\s+\(changed\)', rarity)
        if match:
            return match.group(1)
    # Also handle cases where rarity is just "Common → Rare (changed)" etc.
    # The section might be wrong, trust the Rarity property
    if rarity in ['Legendary', 'Epic', 'Rare', 'Common', 'Champion']:
        return rarity
    # Default to section
    return card['section']

# Assign final rarities
for card in cards:
    card['final_rarity'] = get_final_rarity(card)

# Count by final rarity
rarity_counts = {}
for card in cards:
    r = card['final_rarity']
    rarity_counts[r] = rarity_counts.get(r, 0) + 1

print("Final rarity counts:")
for r, c in sorted(rarity_counts.items()):
    print(f"  {r}: {c}")

# Arena unlock mapping
arena_map = {
    'Legendary': 'Spell Valley (Arena 5)',
    'Epic': 'Royal Arena (Arena 7)',
    'Rare': 'Bone Pit (Arena 2)',
    'Common': 'Training Camp (Tutorial)',
    'Champion': 'Legendary Arena (Arena 15)'
}

# Specific arena overrides based on card
specific_arenas = {
    'The Log': 'Spell Valley (Arena 5)',
    'Princess': 'Spell Valley (Arena 5)',
    'Ice Wizard': 'Spell Valley (Arena 5)',
    'Miner': 'Spell Valley (Arena 5)',
    'Sparky': "P.E.K.K.A's Playhouse (Arena 4)",
    'Inferno Dragon': 'Spell Valley (Arena 5)',
    'Lava Hound': 'Royal Arena (Arena 7)',
    'Mega Knight': 'Royal Arena (Arena 7)',
    'Electro Wizard': 'Spell Valley (Arena 5)',
    'Mother Witch': 'Royal Arena (Arena 7)',
    'Ram Rider': 'Royal Arena (Arena 7)',
    'Royal Ghost': 'Spell Valley (Arena 5)',
    'Magic Archer': 'Royal Arena (Arena 7)',
    'Bandit': 'Spell Valley (Arena 5)',
    'Lightning': 'Royal Arena (Arena 7)',
    'Tornado': 'Spell Valley (Arena 5)',
    'Graveyard': 'Royal Arena (Arena 7)',
    'Ghost': 'Spell Valley (Arena 5)',
    # Epics
    'Baby Dragon': 'Royal Arena (Arena 7)',
    'Prince': "P.E.K.K.A's Playhouse (Arena 4)",
    'Wizard': "P.E.K.K.A's Playhouse (Arena 4)",
    'Witch': 'Royal Arena (Arena 7)',
    'Giant Skeleton': 'Royal Arena (Arena 7)',
    'Balloon': "P.E.K.K.A's Playhouse (Arena 4)",
    'P.E.K.K.A': 'Royal Arena (Arena 7)',
    'Minion Horde': 'Royal Arena (Arena 7)',
    'Goblin Barrel': "P.E.K.K.A's Playhouse (Arena 4)",
    'Freeze': "P.E.K.K.A's Playhouse (Arena 4)",
    'Mirror': 'Royal Arena (Arena 7)',
    'Clone': 'Royal Arena (Arena 7)',
    'Rage': "P.E.K.K.A's Playhouse (Arena 4)",
    'Dark Prince': 'Royal Arena (Arena 7)',
    'Three Musketeers': 'Royal Arena (Arena 7)',
    'Electro Dragon': 'Royal Arena (Arena 7)',
    'Fisherman': 'Royal Arena (Arena 7)',
    'Skeleton Dragons': 'Royal Arena (Arena 7)',
    'Royal Delivery': 'Royal Arena (Arena 7)',
    'Goblin Drill': 'Royal Arena (Arena 7)',
    'Goblin Machine': 'Royal Arena (Arena 7)',
    'Phoenix': 'Royal Arena (Arena 7)',
    'Little Prince': 'Royal Arena (Arena 7)',
    # Rares
    'Giant': 'Bone Pit (Arena 2)',
    'Musketeer': 'Bone Pit (Arena 2)',
    'Mini P.E.K.K.A': "P.E.K.K.A's Playhouse (Arena 4)",
    'Fireball': 'Bone Pit (Arena 2)',
    'Earthquake': 'Royal Arena (Arena 7)',
    'Skeleton Army': 'Bone Pit (Arena 2)',
    'Flying Machine': 'Royal Arena (Arena 7)',
    'Cannon Cart': 'Royal Arena (Arena 7)',
    'Royal Hogs': 'Royal Arena (Arena 7)',
    # Commons
    'Knight': 'Training Camp (Tutorial)',
    'Archers': 'Training Camp (Tutorial)',
    'Goblins': 'Training Camp (Tutorial)',
    'Skeletons': 'Training Camp (Tutorial)',
    'Minions': 'Training Camp (Tutorial)',
    'Cannon': 'Training Camp (Tutorial)',
    'Bomb Tower': 'Royal Arena (Arena 7)',
    'Mortar': "P.E.K.K.A's Playhouse (Arena 4)",
    'X-Bow': 'Royal Arena (Arena 7)',
    'Elixir Collector': 'Royal Arena (Arena 7)',
    'Furnace': "P.E.K.K.A's Playhouse (Arena 4)",
    'Tombstone': 'Bone Pit (Arena 2)',
    'Ice Spirit': 'Spell Valley (Arena 5)',
    'Fire Spirit': 'Spell Valley (Arena 5)',
    'Heal Spirit': 'Spell Valley (Arena 5)',
    'Electro Spirit': 'Spell Valley (Arena 5)',
    'Hog Rider': "P.E.K.K.A's Playhouse (Arena 4)",
    'Valkyrie': "P.E.K.K.A's Playhouse (Arena 4)",
    'Goblin Hut': "P.E.K.K.A's Playhouse (Arena 4)",
    'Inferno Tower': 'Royal Arena (Arena 7)',
    'Rocket': 'Royal Arena (Arena 7)',
    'Poison': 'Royal Arena (Arena 7)',
    'Elite Barbarians': 'Royal Arena (Arena 7)',
    'Dart Goblin': 'Royal Arena (Arena 7)',
    'Goblin Cage': 'Royal Arena (Arena 7)',
    'Bats': 'Spell Valley (Arena 5)',
    'Wall Breakers': 'Royal Arena (Arena 7)',
    'Barbarian Barrel': 'Royal Arena (Arena 7)',
    'Giant Snowball': 'Royal Arena (Arena 7)',
    'Zap': 'Training Camp (Tutorial)',
    'Arrows': 'Training Camp (Tutorial)',
    'Bomber': 'Training Camp (Tutorial)',
    'Spear Goblins': 'Training Camp (Tutorial)',
    'Mega Minion': 'Royal Arena (Arena 7)',
    'Guards': 'Royal Arena (Arena 7)',
    'Goblin Gang': 'Royal Arena (Arena 7)',
    'Battle Ram': 'Royal Arena (Arena 7)',
    # Champions
    'Archer Queen': 'Legendary Arena (Arena 15)',
    'Skeleton King': 'Legendary Arena (Arena 15)',
    'Mighty Miner': 'Legendary Arena (Arena 15)',
}

# Rarity folder mapping
rarity_folder = {
    'Legendary': 'legendary',
    'Epic': 'epic',
    'Rare': 'rare',
    'Common': 'common',
    'Champion': 'champion'
}

# Type mapping for implementation
type_base_class = {
    'Spell': 'Spell',
    'Troop': 'Troop',
    'Troop (Ranged)': 'RangedTroop',
    'Troop (Melee)': 'MeleeTroop',
    'Troop (Ranged/Splash)': 'RangedTroop',
    'Troop (Melee/Area)': 'MeleeTroop',
    'Troop (Flying/Splash)': 'FlyingTroop',
    'Troop (Flying)': 'FlyingTroop',
    'Troop (Flying/Tank)': 'FlyingTroop',
    'Troop (Flying/Chain)': 'FlyingTroop',
    'Troop (Melee/Ranged Hybrid)': 'HybridTroop',
    'Troop (Melee/Spell)': 'MeleeTroop',
    'Troop (Ranged/Spawn)': 'SpawnerTroop',
    'Troop (Melee/Death Damage)': 'MeleeTroop',
    'Troop (Melee/Charge)': 'ChargeTroop',
    'Troop (Melee/Swarm)': 'SwarmTroop',
    'Troop (Flying/Swarm)': 'SwarmTroop',
    'Troop (Flying/Building Target)': 'FlyingTroop',
    'Troop (Swarm)': 'SwarmTroop',
    'Troop (Mixed Swarm)': 'SwarmTroop',
    'Troop (Ranged/Piercing)': 'RangedTroop',
    'Troop (Melee/Hook)': 'MeleeTroop',
    'Troop (Building Target/Spawn)': 'BuildingTargetTroop',
    'Troop (Suicide/Building Target)': 'SuicideTroop',
    'Troop (Building Target/Swarm)': 'BuildingTargetTroop',
    'Troop (Vehicle/Spawn)': 'VehicleTroop',
    'Troop (Flying/Rebirth)': 'FlyingTroop',
    'Troop (Melee/Shield)': 'MeleeTroop',
    'Troop (Melee/Invisible)': 'MeleeTroop',
    'Troop/Building Hybrid': 'HybridTroop',
    'Building (Defensive)': 'DefensiveBuilding',
    'Building (Spawner)': 'SpawnerBuilding',
    'Building (Siege)': 'SiegeBuilding',
    'Building (Economy)': 'EconomyBuilding',
    'Building (Spawner/Tank)': 'SpawnerBuilding',
    'Building (Spawner/Burrowing)': 'SpawnerBuilding',
    'Spell (Spawn)': 'SpawnSpell',
    'Spell (Special)': 'Spell',
    'Champion (Troop)': 'ChampionTroop',
}

# Card ID counter
card_id_counter = 26000000

def sanitize_filename(name):
    return name.lower().replace(' ', '_').replace('.', '').replace('(', '').replace(')', '').replace("'", '').replace('é', 'e').replace('–', '-').replace('×', 'x').replace('/', '_')

def generate_card_file(card):
    global card_id_counter
    card_id_counter += 1
    
    name = card['name']
    final_rarity = card['final_rarity']
    folder = rarity_folder.get(final_rarity, 'common')
    props = card['properties']
    
    card_type = props.get('Type', 'Troop')
    elixir = props.get('Elixir', '?')
    unlock_arena = specific_arenas.get(name, arena_map.get(final_rarity, 'Training Camp (Tutorial)'))
    base_class = type_base_class.get(card_type, 'Troop')
    
    # Build stats table
    stats_lines = ["| Stat | Value |", "|------|-------|"]
    
    stat_mapping = {
        'HP': 'Hitpoints',
        'Damage': 'Damage',
        'Damage (per arrow)': 'Damage (per arrow)',
        'Arrows per Attack': 'Arrows per Attack',
        'Total Damage/Attack': 'Total Damage/Attack',
        'Hit Speed': 'Hit Speed',
        'DPS': 'DPS',
        'Range': 'Range',
        'Speed': 'Speed',
        'Deploy Time': 'Deploy Time',
        'Target': 'Target',
        'Splash Radius': 'Splash Radius',
        'Projectile Speed': 'Projectile Speed',
        'Lifetime': 'Lifetime',
        'Count': 'Count',
        'Radius': 'Radius',
        'Duration': 'Duration',
        'Effect': 'Effect',
        'Travel Time': 'Travel Time',
        'Knockback': 'Knockback',
        'Width': 'Width',
        'Production': 'Production',
        'Death Spawns': 'Death Spawns',
        'Brawler Stats': 'Brawler Stats',
        'Ability': 'Ability',
        'Mechanic': 'Mechanic',
        'Spawn': 'Spawn',
        'Spawns': 'Spawns',
        'Composition': 'Composition',
        'HP (cart)': 'HP (cart)',
        'HP (cannon mode)': 'HP (cannon mode)',
    }
    
    for prop_key, display_name in stat_mapping.items():
        if prop_key in props:
            stats_lines.append(f"| {display_name} | {props[prop_key]} |")
    
    stats_table = '\n'.join(stats_lines)
    
    # Mechanics
    mechanics = "## Mechanics\n\n"
    if 'Mechanic' in props:
        mechanics += f"1. {props['Mechanic']}\n"
    
    # Add type-specific mechanics
    if 'Swarm' in card_type or 'Count' in props:
        mechanics += "2. Swarm unit - multiple units deployed together\n"
    if 'Flying' in card_type:
        mechanics += "3. Flying unit - can cross river, targeted by air defenses\n"
    if 'Building' in card_type:
        mechanics += "2. Building - stationary, has lifetime, targets by path distance\n"
    if 'Spell' in card_type and 'Spawn' not in card_type:
        mechanics += "2. Spell - instant or delayed effect, can be placed anywhere\n"
    if 'Champion' in card_type:
        mechanics += "2. Champion - has special ability with elixir cost and cooldown\n"
        mechanics += "3. Max 1 Champion per deck\n"
    if 'Death Spawns' in props or 'Spawn' in props:
        mechanics += "3. Death spawn / periodic spawn mechanic\n"
    if 'Charge' in card_type:
        mechanics += "3. Charge mechanic - gains speed and damage after moving 3.5 tiles\n"
    if 'Invisible' in card_type:
        mechanics += "3. Invisibility - hidden until attacking, brief invisibility after hit\n"
    if 'Piercing' in card_type:
        mechanics += "3. Piercing projectiles - arrows pass through multiple enemies\n"
    if 'Ramp' in props.get('Mechanic', '') or 'ramping' in props.get('Mechanic', '').lower():
        mechanics += "3. Damage ramps up over continuous attack, resets on target change/stun\n"
    
    mechanics += "\n"
    
    # Interactions (template)
    interactions = f"""## Interactions

### Key Interactions
- Standard interactions for {final_rarity.lower()} {card_type.lower()}
- Refer to CARDS_DATABASE.md for detailed interaction chart

"""
    
    # Synergies
    synergies = f"""## Synergies

- Works well with tank units for protection
- Pairs with splash damage for swarm control
- Complements spell bait strategies

"""
    
    # Visual/Audio
    visual = f"""## Visual/Audio

- **Sprite**: {name} character/model
- **Animations**: Idle, Walk, Attack, Hit, Death, Spawn
- **Effects**: Standard {card_type.lower()} effects
- **Sound**: Character-appropriate audio cues
- **Voice Lines**: Character-specific lines

"""
    
    # Implementation
    class_name = name.replace(' ', '').replace('-', '').replace('(', '').replace(')', '').replace("'", '').replace('.', '')
    impl = f"""## Implementation Notes

```csharp
public class {class_name} : {base_class}
{{
    // Implementation based on card stats
    protected override void InitializeStats()
    {{
"""
    
    if 'HP' in props:
        impl += f"        MaxHP = {props['HP']}; // Level 11\n"
    if 'Damage' in props:
        impl += f"        Damage = {props['Damage']};\n"
    elif 'Damage (per arrow)' in props:
        impl += f"        Damage = {props['Damage (per arrow)']}; // per arrow\n"
    if 'Hit Speed' in props:
        impl += f"        HitSpeed = {props['Hit Speed']};\n"
    if 'Range' in props:
        impl += f"        Range = {props['Range']};\n"
    if 'Speed' in props:
        impl += f"        MoveSpeed = {props['Speed']};\n"
    if 'Target' in props:
        target = props['Target'].replace(' ', '').replace('&', 'And')
        impl += f"        TargetType = TargetType.{target};\n"
    if 'Count' in props:
        impl += f"        UnitCount = {props['Count']};\n"
    if 'Lifetime' in props:
        impl += f"        Lifetime = {props['Lifetime']};\n"
    
    impl += """    }
}
```

"""
    
    # Level scaling
    hp = props.get('HP', '[Base]')
    dmg = props.get('Damage', props.get('Damage (per arrow)', '[Base]'))
    scaling = f"""## Level Scaling

| Level | HP | Damage |
|-------|-----|--------|
| 1 | [Base] | [Base] |
| ... | ... | ... |
| 11 | {hp} | {dmg} |
| 12 | [×1.1] | [×1.1] |
| 13 | [×1.21] | [×1.21] |
| 14 | [×1.33] | [×1.33] |

Formula: `Stat_Lvl_N = Stat_Lvl_1 * 1.1^(N-1)`

"""
    
    # Testing checklist
    testing = "## Testing Checklist\n"
    if 'HP' in props:
        testing += f"- [ ] HP = {props['HP']} at tournament standard\n"
    if 'Damage' in props:
        testing += f"- [ ] Damage = {props['Damage']} per hit\n"
    elif 'Damage (per arrow)' in props:
        testing += f"- [ ] Damage = {props['Damage (per arrow)']} per arrow\n"
    if 'Hit Speed' in props:
        testing += f"- [ ] Hit speed = {props['Hit Speed']}\n"
    if 'Range' in props:
        testing += f"- [ ] Range = {props['Range']}\n"
    if 'Speed' in props:
        testing += f"- [ ] Speed = {props['Speed']}\n"
    if 'Target' in props:
        testing += f"- [ ] Targets {props['Target']}\n"
    if 'Count' in props:
        testing += f"- [ ] Spawns {props['Count']} units\n"
    if 'Lifetime' in props:
        testing += f"- [ ] Lifetime = {props['Lifetime']}\n"
    testing += "- [ ] Visual: proper animations and effects\n- [ ] Audio: character sounds and voice lines\n"
    
    # Full content
    full_content = f"""# {name} - Card Specification

## Basic Info
- **Card ID**: {card_id_counter} (example)
- **Name**: {name}
- **Rarity**: {final_rarity}
- **Type**: {card_type}
- **Elixir Cost**: {elixir}
- **Unlock Arena**: {unlock_arena}

## Statistics (Tournament Standard / Level 11)
{stats_table}

{mechanics}{interactions}{synergies}{visual}{impl}{scaling}{testing}
"""
    
    # Write file
    filename = sanitize_filename(name) + '.md'
    filepath = Path(f"docs/planning/phase1/cards/{folder}/{filename}")
    filepath.parent.mkdir(parents=True, exist_ok=True)
    filepath.write_text(full_content, encoding='utf-8')
    print(f"Created: {filepath}")

# Generate all cards
for card in cards:
    generate_card_file(card)

print(f"\nTotal cards generated: {len(cards)}")
print("Done!")