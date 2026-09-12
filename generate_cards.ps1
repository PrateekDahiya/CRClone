#!/usr/bin/env pwsh

# Card Generator Script for CRclone
# Reads CARDS_DATABASE.md and generates individual card specification files

$databasePath = "docs/planning/phase1/CARDS_DATABASE.md"
$outputBase = "docs/planning/phase1/cards"

# Rarity folder mapping
$rarityFolders = @{
    "Legendary" = "legendary"
    "Epic" = "epic"
    "Rare" = "rare"
    "Common" = "common"
    "Champion" = "champion"
}

# Arena unlock mapping
$arenaUnlocks = @{
    "Training Camp" = "Training Camp (Tutorial)"
    "Goblin Stadium" = "Goblin Stadium (Arena 1)"
    "Bone Pit" = "Bone Pit (Arena 2)"
    "Barbarian Bowl" = "Barbarian Bowl (Arena 3)"
    "P.E.K.K.A's Playhouse" = "P.E.K.K.A's Playhouse (Arena 4)"
    "Spell Valley" = "Spell Valley (Arena 5)"
    "Builder's Workshop" = "Builder's Workshop (Arena 6)"
    "Royal Arena" = "Royal Arena (Arena 7)"
    "Frozen Peak" = "Frozen Peak (Arena 8)"
    "Jungle Arena" = "Jungle Arena (Arena 9)"
    "Hog Mountain" = "Hog Mountain (Arena 10)"
    "Electro Valley" = "Electro Valley (Arena 11)"
    "Spooky Town" = "Spooky Town (Arena 12)"
    "Rascal's Hideout" = "Rascal's Hideout (Arena 13)"
    "Serenity Peak" = "Serenity Peak (Arena 14)"
    "Legendary Arena" = "Legendary Arena (Arena 15)"
}

# Default arena by rarity
$defaultArena = @{
    "Legendary" = "Spell Valley (Arena 5)"
    "Epic" = "Royal Arena (Arena 7)"
    "Rare" = "Bone Pit (Arena 2)"
    "Common" = "Training Camp (Tutorial)"
    "Champion" = "Legendary Arena (Arena 15)"
}

# Read the database
$content = Get-Content $databasePath -Raw

# Parse cards using regex
$cards = @()
$currentRarity = ""
$currentCard = $null

# Split by lines and parse
$lines = $content -split "`n"
$inCard = $false
$cardData = @{}

foreach ($line in $lines) {
    $line = $line.Trim()
    
    # Detect rarity section
    if ($line -match '^##\s+(LEGENDARY|EPIC|RARE|COMMON|CHAMPION)\s+CARDS') {
        $currentRarity = $matches[1]
        continue
    }
    
    # Detect card start (### N. Name)
    if ($line -match '^###\s+\d+\.\s+(.+)$') {
        # Save previous card
        if ($cardData.Count -gt 0) {
            $cardData["Rarity"] = $currentRarity
            $cards += [pscustomobject]$cardData
        }
        
        # Start new card
        $cardData = @{
            "Name" = $matches[1].Trim()
            "Number" = $line -replace '^###\s+(\d+)\..*', '$1'
        }
        $inCard = $true
        continue
    }
    
    # Parse card properties
    if ($inCard -and $line -match '^\-\s+\*\*(.+?)\*\*:\s*(.+)$') {
        $key = $matches[1].Trim()
        $value = $matches[2].Trim()
        $cardData[$key] = $value
    }
}

# Don't forget last card
if ($cardData.Count -gt 0) {
    $cardData["Rarity"] = $currentRarity
    $cards += [pscustomobject]$cardData
}

# Filter out duplicates and already existing
$existingFiles = Get-ChildItem "$outputBase/**/*.md" -Recurse -ErrorAction SilentlyContinue | ForEach-Object { $_.BaseName.ToLower() }
$cardsToCreate = @()

foreach ($card in $cards) {
    $fileName = $card.Name.ToLower().Replace(' ', '_').Replace('.', '').Replace('(', '').Replace(')', '').Replace("'", '').Replace('é', 'e')
    $folder = $rarityFolders[$card.Rarity]
    $fullPath = "$outputBase/$folder/$fileName.md"
    
    if (-not (Test-Path $fullPath)) {
        $cardsToCreate += $card
    }
}

Write-Host "Found $($cards.Count) total cards in database"
Write-Host "Already exist: $($existingFiles.Count) files"
Write-Host "Need to create: $($cardsToCreate.Count) files"

# Generate card files
$cardId = 26000000
foreach ($card in $cardsToCreate) {
    $cardId++
    $folder = $rarityFolders[$card.Rarity]
    $fileName = $card.Name.ToLower().Replace(' ', '_').Replace('.', '').Replace('(', '').Replace(')', '').Replace("'", '').Replace('é', 'e').Replace('–', '-').Replace('×', 'x')
    $fullPath = "$outputBase/$folder/$fileName.md"
    
    # Determine unlock arena
    $unlockArena = $defaultArena[$card.Rarity]
    if ($card.ContainsKey("Unlock Arena")) {
        $unlockArena = $card["Unlock Arena"]
    } elseif ($arenaUnlocks.ContainsKey($card["Unlock Arena"])) {
        $unlockArena = $arenaUnlocks[$card["Unlock Arena"]]
    }
    
    # Generate content
    $cardContent = GenerateCardContent $card $cardId $unlockArena
    
    # Write file
    Set-Content -Path $fullPath -Value $cardContent -Encoding UTF8
    Write-Host "Created: $fullPath"
}

function GenerateCardContent($card, $cardId, $unlockArena) {
    $name = $card.Name
    $rarity = $card.Rarity
    $type = if ($card.ContainsKey("Type")) { $card.Type } else { "Troop" }
    $elixir = if ($card.ContainsKey("Elixir")) { $card.Elixir } else { "?" }
    
    # Build statistics table
    $stats = "| Stat | Value |\n|------|-------|\n"
    
    # Add relevant stats based on type
    if ($card.ContainsKey("HP")) { $stats += "| Hitpoints | $($card.HP) |\n" }
    if ($card.ContainsKey("Damage")) { 
        $stats += "| Damage | $($card.Damage) |\n" 
    } elseif ($card.ContainsKey("Damage (per arrow)")) {
        $stats += "| Damage (per arrow) | $($card["Damage (per arrow)"]) |\n"
        $stats += "| Arrows per Attack | $($card["Arrows per Attack"]) |\n"
        $stats += "| Total Damage/Attack | $($card["Total Damage/Attack"]) |\n"
    } elseif ($card.ContainsKey("Spawns")) {
        $stats += "| Spawns | $($card.Spawns) |\n"
    } elseif ($card.ContainsKey("Composition")) {
        $stats += "| Composition | $($card.Composition) |\n"
    }
    
    if ($card.ContainsKey("Hit Speed")) { $stats += "| Hit Speed | $($card["Hit Speed"]) |\n" }
    if ($card.ContainsKey("DPS")) { $stats += "| DPS | $($card.DPS) |\n" }
    if ($card.ContainsKey("Range")) { $stats += "| Range | $($card.Range) |\n" }
    if ($card.ContainsKey("Speed")) { $stats += "| Speed | $($card.Speed) |\n" }
    if ($card.ContainsKey("Deploy Time")) { $stats += "| Deploy Time | $($card["Deploy Time"]) |\n" }
    if ($card.ContainsKey("Target")) { $stats += "| Target | $($card.Target) |\n" }
    if ($card.ContainsKey("Splash Radius")) { $stats += "| Splash Radius | $($card["Splash Radius"]) |\n" }
    if ($card.ContainsKey("Projectile Speed")) { $stats += "| Projectile Speed | $($card["Projectile Speed"]) |\n" }
    if ($card.ContainsKey("Lifetime")) { $stats += "| Lifetime | $($card.Lifetime) |\n" }
    if ($card.ContainsKey("Count")) { $stats += "| Count | $($card.Count) |\n" }
    if ($card.ContainsKey("Radius")) { $stats += "| Radius | $($card.Radius) |\n" }
    if ($card.ContainsKey("Duration")) { $stats += "| Duration | $($card.Duration) |\n" }
    if ($card.ContainsKey("Effect")) { $stats += "| Effect | $($card.Effect) |\n" }
    if ($card.ContainsKey("Mechanic")) { $stats += "| Mechanic | $($card.Mechanic) |\n" }
    if ($card.ContainsKey("Travel Time")) { $stats += "| Travel Time | $($card["Travel Time"]) |\n" }
    if ($card.ContainsKey("Knockback")) { $stats += "| Knockback | $($card.Knockback) |\n" }
    if ($card.ContainsKey("Width")) { $stats += "| Width | $($card.Width) |\n" }
    if ($card.ContainsKey("Production")) { $stats += "| Production | $($card.Production) |\n" }
    if ($card.ContainsKey("Death Spawns")) { $stats += "| Death Spawns | $($card["Death Spawns"]) |\n" }
    if ($card.ContainsKey("Brawler Stats")) { $stats += "| Brawler Stats | $($card["Brawler Stats"]) |\n" }
    if ($card.ContainsKey("Ability")) { $stats += "| Ability | $($card.Ability) |\n" }
    if ($card.ContainsKey("Mechanic")) { 
        # Already handled
    }
    
    # Build mechanics section
    $mechanics = "## Mechanics\n\n"
    if ($card.ContainsKey("Mechanic")) {
        $mechanics += "1. $($card.Mechanic)\n"
    } else {
        $mechanics += "1. Standard $($type.ToLower()) mechanics\n"
    }
    
    # Add type-specific mechanics
    if ($type -like "*Swarm*" -or $card.ContainsKey("Count")) {
        $mechanics += "2. Swarm unit - multiple units deployed together\n"
    }
    if ($type -like "*Flying*") {
        $mechanics += "3. Flying unit - can cross river, targeted by air defenses\n"
    }
    if ($type -like "*Building*") {
        $mechanics += "2. Building - stationary, has lifetime, targets by path distance\n"
    }
    if ($type -like "*Spell*") {
        $mechanics += "2. Spell - instant or delayed effect, can be placed anywhere\n"
    }
    if ($type -like "*Champion*") {
        $mechanics += "2. Champion - has special ability with elixir cost and cooldown\n"
        $mechanics += "3. Max 1 Champion per deck\n"
    }
    if ($card.ContainsKey("Death Spawns") -or $card.ContainsKey("Spawn")) {
        $mechanics += "3. Death spawn / periodic spawn mechanic\n"
    }
    
    $mechanics += "\n"
    
    # Build interactions (simplified template)
    $interactions = "## Interactions\n\n### Key Interactions\n- Standard interactions for $($rarity.ToLower()) $($type.ToLower())\n- Refer to CARDS_DATABASE.md for detailed interaction chart\n\n"
    
    # Build synergies
    $synergies = "## Synergies\n\n- Works well with tank units for protection\n- Pairs with splash damage for swarm control\n- Complements spell bait strategies\n\n"
    
    # Build visual/audio
    $visual = "## Visual/Audio\n\n- **Sprite**: $name character/model\n- **Animations**: Idle, Walk, Attack, Hit, Death, Spawn\n- **Effects**: Standard $($type.ToLower()) effects\n- **Sound**: Character-appropriate audio cues\n- **Voice Lines**: Character-specific lines\n\n"
    
    # Build implementation notes
    $impl = "## Implementation Notes\n```csharp\npublic class $($name.Replace(' ', '').Replace('-', '').Replace('(', '').Replace(')', '').Replace("'", '')) : $($type.Split(' ')[0]) \n{\n    // Implementation based on card stats\n    protected override void InitializeStats()\n    {\n"
    
    if ($card.ContainsKey("HP")) { $impl += "        MaxHP = $($card.HP); // Level 11\n" }
    if ($card.ContainsKey("Damage")) { $impl += "        Damage = $($card.Damage);\n" }
    elseif ($card.ContainsKey("Damage (per arrow)")) { $impl += "        Damage = $($card["Damage (per arrow)"]); // per arrow\n" }
    if ($card.ContainsKey("Hit Speed")) { $impl += "        HitSpeed = $($card["Hit Speed"]);\n" }
    if ($card.ContainsKey("Range")) { $impl += "        Range = $($card.Range);\n" }
    if ($card.ContainsKey("Speed")) { $impl += "        MoveSpeed = $($card.Speed);\n" }
    if ($card.ContainsKey("Target")) { $impl += "        TargetType = TargetType.$($card.Target.Replace(' ', '').Replace('&', 'And'));\n" }
    
    $impl += "    }\n}\n```\n\n"
    
    # Build level scaling
    $scaling = "## Level Scaling\n\n| Level | HP | Damage |\n|-------|-----|--------|\n| 1 | [Base] | [Base] |\n| ... | ... | ... |\n| 11 | $($card.HP) | $($card.Damage) |\n| 12 | [×1.1] | [×1.1] |\n| 13 | [×1.21] | [×1.21] |\n| 14 | [×1.33] | [×1.33] |\n\nFormula: `Stat_Lvl_N = Stat_Lvl_1 * 1.1^(N-1)`\n\n"
    
    # Build testing checklist
    $testing = "## Testing Checklist\n"
    if ($card.ContainsKey("HP")) { $testing += "- [ ] HP = $($card.HP) at tournament standard\n" }
    if ($card.ContainsKey("Damage")) { $testing += "- [ ] Damage = $($card.Damage) per hit\n" }
    if ($card.ContainsKey("Hit Speed")) { $testing += "- [ ] Hit speed = $($card["Hit Speed"])\n" }
    if ($card.ContainsKey("Range")) { $testing += "- [ ] Range = $($card.Range)\n" }
    if ($card.ContainsKey("Speed")) { $testing += "- [ ] Speed = $($card.Speed)\n" }
    if ($card.ContainsKey("Target")) { $testing += "- [ ] Targets $($card.Target)\n" }
    if ($card.ContainsKey("Count")) { $testing += "- [ ] Spawns $($card.Count) units\n" }
    if ($card.ContainsKey("Lifetime")) { $testing += "- [ ] Lifetime = $($card.Lifetime)\n" }
    $testing += "- [ ] Visual: proper animations and effects\n- [ ] Audio: character sounds and voice lines\n"
    
    # Assemble full content
    $fullContent = @"
# $name - Card Specification

## Basic Info
- **Card ID**: $cardId (example)
- **Name**: $name
- **Rarity**: $rarity
- **Type**: $type
- **Elixir Cost**: $elixir
- **Unlock Arena**: $unlockArena

## Statistics (Tournament Standard / Level 11)
$stats

$mechanics
$interactions
$synergies
$visual
$impl
$scaling
$testing
"@
    
    return $fullContent
}

Write-Host "Generation complete!"