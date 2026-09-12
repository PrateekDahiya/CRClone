# Clash Royale Clone - Database Schema (MySQL)

## 1. DATABASE OVERVIEW

**Host**: mysql-2655d9d0-prateekdahiya2722005-4340.k.aivencloud.com
**Database**: crclone
**Port**: 17886
**SSL**: Required
**User**: avnadmin

## 2. TABLES

### 2.1 Players & Accounts

```sql
-- Players table
CREATE TABLE players (
    player_id BIGINT UNSIGNED PRIMARY KEY AUTO_INCREMENT,
    username VARCHAR(32) NOT NULL UNIQUE,
    email VARCHAR(255) UNIQUE,
    password_hash VARCHAR(255) NOT NULL,
    auth_provider ENUM('email', 'google', 'apple', 'guest') DEFAULT 'email',
    auth_provider_id VARCHAR(255),
    
    -- Profile
    avatar_id INT UNSIGNED DEFAULT 1,
    name_color VARCHAR(7) DEFAULT '#FFFFFF',
    tag VARCHAR(16) UNIQUE, -- #ABC12345
    
    -- Progression
    experience BIGINT UNSIGNED DEFAULT 0,
    level TINYINT UNSIGNED DEFAULT 1,
    trophies INT DEFAULT 0,
    best_trophies INT DEFAULT 0,
    
    -- Currency
    gold BIGINT UNSIGNED DEFAULT 0,
    gems INT UNSIGNED DEFAULT 0,
    
    -- Clan
    clan_id BIGINT UNSIGNED NULL,
    clan_role ENUM('leader', 'co_leader', 'elder', 'member') NULL,
    
    -- Settings
    language VARCHAR(5) DEFAULT 'en',
    push_notifications BOOLEAN DEFAULT TRUE,
    
    -- Timestamps
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    last_login_at TIMESTAMP NULL,
    last_battle_at TIMESTAMP NULL,
    
    -- Bans/Moderation
    is_banned BOOLEAN DEFAULT FALSE,
    ban_reason VARCHAR(255) NULL,
    ban_expires_at TIMESTAMP NULL,
    
    INDEX idx_tag (tag),
    INDEX idx_clan (clan_id),
    INDEX idx_trophies (trophies DESC)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
```

```sql
-- Player devices (for push notifications)
CREATE TABLE player_devices (
    device_id BIGINT UNSIGNED PRIMARY KEY AUTO_INCREMENT,
    player_id BIGINT UNSIGNED NOT NULL,
    platform ENUM('ios', 'android', 'windows', 'mac', 'linux') NOT NULL,
    push_token VARCHAR(512) NOT NULL,
    app_version VARCHAR(32),
    device_model VARCHAR(128),
    os_version VARCHAR(64),
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    last_used_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    
    FOREIGN KEY (player_id) REFERENCES players(player_id) ON DELETE CASCADE,
    UNIQUE KEY uk_player_device (player_id, push_token)
) ENGINE=InnoDB;
```

### 2.2 Cards & Collection

```sql
-- Card definitions (static data, loaded from config)
CREATE TABLE cards (
    card_id INT UNSIGNED PRIMARY KEY, -- Matches game IDs
    name VARCHAR(64) NOT NULL,
    name_key VARCHAR(64) NOT NULL, -- For localization
    description TEXT,
    description_key VARCHAR(128),
    
    -- Classification
    rarity ENUM('common', 'rare', 'epic', 'legendary', 'champion') NOT NULL,
    type ENUM('troop', 'spell', 'building', 'champion') NOT NULL,
    arena TINYINT UNSIGNED DEFAULT 0, -- Unlock arena
    
    -- Stats (Level 1 base)
    elixir_cost TINYINT UNSIGNED NOT NULL,
    hitpoints INT UNSIGNED,
    damage INT UNSIGNED,
    hit_speed DECIMAL(4,2),
    range DECIMAL(4,2),
    speed ENUM('very_slow', 'slow', 'medium', 'fast', 'very_fast'),
    deploy_time TINYINT UNSIGNED DEFAULT 1,
    target ENUM('ground', 'air', 'both', 'buildings', 'any') DEFAULT 'both',
    count TINYINT UNSIGNED DEFAULT 1, -- For swarms
    
    -- Mechanics (JSON for flexibility)
    mechanics JSON, -- {"splash_radius": 1.5, "charge": true, "spawn_count": 3, ...}
    
    -- Visual
    sprite_id VARCHAR(64),
    portrait_id VARCHAR(64),
    
    -- Balance
    is_enabled BOOLEAN DEFAULT TRUE,
    release_date DATE,
    
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    
    INDEX idx_rarity (rarity),
    INDEX idx_type (type),
    INDEX idx_arena (arena)
) ENGINE=InnoDB;
```

```sql
-- Player card collection
CREATE TABLE player_cards (
    player_id BIGINT UNSIGNED NOT NULL,
    card_id INT UNSIGNED NOT NULL,
    count INT UNSIGNED DEFAULT 0, -- Number of copies owned
    level TINYINT UNSIGNED DEFAULT 1, -- Current level
    upgrade_progress INT UNSIGNED DEFAULT 0, -- Cards toward next level
    
    -- For champions: ability level tracked separately
    ability_level TINYINT UNSIGNED DEFAULT 1,
    
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    
    PRIMARY KEY (player_id, card_id),
    FOREIGN KEY (player_id) REFERENCES players(player_id) ON DELETE CASCADE,
    FOREIGN KEY (card_id) REFERENCES cards(card_id) ON DELETE CASCADE,
    INDEX idx_player_level (player_id, level DESC)
) ENGINE=InnoDB;
```

```sql
-- Card level stats (precomputed for fast lookup)
CREATE TABLE card_level_stats (
    card_id INT UNSIGNED NOT NULL,
    level TINYINT UNSIGNED NOT NULL, -- 1-14
    
    hitpoints INT UNSIGNED,
    damage INT UNSIGNED,
    hit_speed DECIMAL(4,2),
    range DECIMAL(4,2),
    
    -- Upgrade costs
    gold_cost INT UNSIGNED,
    cards_required INT UNSIGNED,
    
    PRIMARY KEY (card_id, level),
    FOREIGN KEY (card_id) REFERENCES cards(card_id) ON DELETE CASCADE
) ENGINE=InnoDB;
```

### 2.3 Decks

```sql
-- Player decks (multiple saved decks)
CREATE TABLE player_decks (
    deck_id BIGINT UNSIGNED PRIMARY KEY AUTO_INCREMENT,
    player_id BIGINT UNSIGNED NOT NULL,
    name VARCHAR(64) DEFAULT 'My Deck',
    is_active BOOLEAN DEFAULT FALSE,
    
    -- Card slots (8 cards)
    slot_1_card INT UNSIGNED,
    slot_2_card INT UNSIGNED,
    slot_3_card INT UNSIGNED,
    slot_4_card INT UNSIGNED,
    slot_5_card INT UNSIGNED,
    slot_6_card INT UNSIGNED,
    slot_7_card INT UNSIGNED,
    slot_8_card INT UNSIGNED,
    
    -- Computed
    avg_elixir DECIMAL(3,1),
    has_champion BOOLEAN DEFAULT FALSE,
    
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    
    FOREIGN KEY (player_id) REFERENCES players(player_id) ON DELETE CASCADE,
    FOREIGN KEY (slot_1_card) REFERENCES cards(card_id),
    FOREIGN KEY (slot_2_card) REFERENCES cards(card_id),
    FOREIGN KEY (slot_3_card) REFERENCES cards(card_id),
    FOREIGN KEY (slot_4_card) REFERENCES cards(card_id),
    FOREIGN KEY (slot_5_card) REFERENCES cards(card_id),
    FOREIGN KEY (slot_6_card) REFERENCES cards(card_id),
    FOREIGN KEY (slot_7_card) REFERENCES cards(card_id),
    FOREIGN KEY (slot_8_card) REFERENCES cards(card_id),
    
    INDEX idx_player_active (player_id, is_active)
) ENGINE=InnoDB;
```

### 2.4 Battles & Matchmaking

```sql
-- Battle history
CREATE TABLE battles (
    battle_id BIGINT UNSIGNED PRIMARY KEY AUTO_INCREMENT,
    battle_type ENUM('ladder', '2v2', 'tournament', 'challenge', 'friendly', 'clan_war', 'practice', 'bot') NOT NULL,
    
    -- Players
    player_1_id BIGINT UNSIGNED NOT NULL,
    player_2_id BIGINT UNSIGNED NULL, -- NULL for bot/practice
    player_1_deck_id BIGINT UNSIGNED NOT NULL,
    player_2_deck_id BIGINT UNSIGNED NULL,
    
    -- For 2v2
    player_3_id BIGINT UNSIGNED NULL,
    player_4_id BIGINT UNSIGNED NULL,
    team_1_crowns TINYINT UNSIGNED DEFAULT 0,
    team_2_crowns TINYINT UNSIGNED DEFAULT 0,
    
    -- Result
    winner_team TINYINT UNSIGNED, -- 1 or 2 (NULL for draw)
    player_1_crowns TINYINT UNSIGNED DEFAULT 0,
    player_2_crowns TINYINT UNSIGNED DEFAULT 0,
    
    -- King tower HP at end
    player_1_king_hp INT UNSIGNED,
    player_2_king_hp INT UNSIGNED,
    
    -- Duration
    duration_seconds INT UNSIGNED,
    went_overtime BOOLEAN DEFAULT FALSE,
    
    -- Trophy change
    player_1_trophy_change SMALLINT DEFAULT 0,
    player_2_trophy_change SMALLINT DEFAULT 0,
    
    -- Replay
    replay_id BIGINT UNSIGNED NULL,
    replay_seed BIGINT UNSIGNED,
    
    -- Matchmaking
    player_1_trophies_at_start INT,
    player_2_trophies_at_start INT,
    
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    FOREIGN KEY (player_1_id) REFERENCES players(player_id),
    FOREIGN KEY (player_2_id) REFERENCES players(player_id),
    FOREIGN KEY (player_1_deck_id) REFERENCES player_decks(deck_id),
    FOREIGN KEY (player_2_deck_id) REFERENCES player_decks(deck_id),
    
    INDEX idx_player_date (player_1_id, created_at DESC),
    INDEX idx_type_date (battle_type, created_at DESC),
    INDEX idx_replay (replay_id)
) ENGINE=InnoDB;
```

```sql
-- Detailed battle events (for replay/analytics)
CREATE TABLE battle_events (
    event_id BIGINT UNSIGNED PRIMARY KEY AUTO_INCREMENT,
    battle_id BIGINT UNSIGNED NOT NULL,
    timestamp_ms INT UNSIGNED NOT NULL, -- Milliseconds from battle start
    
    player_id BIGINT UNSIGNED NOT NULL,
    event_type ENUM(
        'card_played', 'spell_cast', 'unit_spawned', 'unit_died',
        'building_placed', 'building_destroyed', 'tower_damaged',
        'tower_destroyed', 'king_activated', 'elixir_gained',
        'champion_ability_used'
    ) NOT NULL,
    
    -- Event data (JSON for flexibility)
    card_id INT UNSIGNED NULL,
    position_x DECIMAL(5,2) NULL,
    position_y DECIMAL(5,2) NULL,
    target_id BIGINT UNSIGNED NULL, -- Entity ID in battle
    damage INT NULL,
    data JSON, -- Additional context
    
    FOREIGN KEY (battle_id) REFERENCES battles(battle_id) ON DELETE CASCADE,
    FOREIGN KEY (player_id) REFERENCES players(player_id),
    FOREIGN KEY (card_id) REFERENCES cards(card_id),
    
    INDEX idx_battle_time (battle_id, timestamp_ms)
) ENGINE=InnoDB;
```

```sql
-- Replays (stored separately for size)
CREATE TABLE replays (
    replay_id BIGINT UNSIGNED PRIMARY KEY AUTO_INCREMENT,
    battle_id BIGINT UNSIGNED NOT NULL UNIQUE,
    
    -- Compressed replay data
    replay_data LONGBLOB NOT NULL, -- Protocol buffer / compressed JSON
    replay_version VARCHAR(16) NOT NULL,
    
    -- Metadata for search
    player_1_id BIGINT UNSIGNED,
    player_2_id BIGINT UNSIGNED,
    player_1_deck_hash VARCHAR(64), -- Hash of 8 card IDs
    player_2_deck_hash VARCHAR(64),
    duration_seconds INT UNSIGNED,
    
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    expires_at TIMESTAMP NULL, -- Auto-delete old replays
    
    FOREIGN KEY (battle_id) REFERENCES battles(battle_id) ON DELETE CASCADE,
    INDEX idx_player (player_1_id, created_at DESC),
    INDEX idx_deck (player_1_deck_hash)
) ENGINE=InnoDB;
```

### 2.5 Chests & Rewards

```sql
-- Chest definitions
CREATE TABLE chest_types (
    chest_id INT UNSIGNED PRIMARY KEY,
    name VARCHAR(64) NOT NULL,
    name_key VARCHAR(64),
    
    unlock_time_seconds INT UNSIGNED, -- 0 = instant
    slots TINYINT UNSIGNED, -- Number of reward slots
    
    -- Reward weights (JSON)
    reward_table JSON, -- [{"type":"card","rarity":"common","weight":100},...]
    
    -- Visual
    sprite_id VARCHAR(64),
    
    is_enabled BOOLEAN DEFAULT TRUE
) ENGINE=InnoDB;
```

```sql
-- Player chest slots (4 active + 1 queue)
CREATE TABLE player_chests (
    chest_slot_id BIGINT UNSIGNED PRIMARY KEY AUTO_INCREMENT,
    player_id BIGINT UNSIGNED NOT NULL,
    chest_type_id INT UNSIGNED NOT NULL,
    slot_index TINYINT UNSIGNED NOT NULL, -- 0-3 active, 4 queued
    status ENUM('locked', 'unlocking', 'ready', 'empty') DEFAULT 'empty',
    
    unlock_started_at TIMESTAMP NULL,
    unlock_finishes_at TIMESTAMP NULL,
    
    -- For queue
    queue_position TINYINT UNSIGNED NULL,
    
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    
    FOREIGN KEY (player_id) REFERENCES players(player_id) ON DELETE CASCADE,
    FOREIGN KEY (chest_type_id) REFERENCES chest_types(chest_id),
    UNIQUE KEY uk_player_slot (player_id, slot_index)
) ENGINE=InnoDB;
```

```sql
-- Chest rewards (when opened)
CREATE TABLE chest_rewards (
    reward_id BIGINT UNSIGNED PRIMARY KEY AUTO_INCREMENT,
    chest_slot_id BIGINT UNSIGNED NOT NULL,
    player_id BIGINT UNSIGNED NOT NULL,
    
    reward_type ENUM('card', 'gold', 'gems', 'wild_card') NOT NULL,
    card_id INT UNSIGNED NULL,
    card_count INT UNSIGNED DEFAULT 0,
    gold_amount INT UNSIGNED DEFAULT 0,
    gems_amount INT UNSIGNED DEFAULT 0,
    rarity ENUM('common', 'rare', 'epic', 'legendary', 'champion') NULL,
    
    opened_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    FOREIGN KEY (chest_slot_id) REFERENCES player_chests(chest_slot_id) ON DELETE CASCADE,
    FOREIGN KEY (player_id) REFERENCES players(player_id) ON DELETE CASCADE,
    FOREIGN KEY (card_id) REFERENCES cards(card_id)
) ENGINE=InnoDB;
```

### 2.6 Shop & Offers

```sql
-- Shop offers (daily/special)
CREATE TABLE shop_offers (
    offer_id BIGINT UNSIGNED PRIMARY KEY AUTO_INCREMENT,
    offer_type ENUM('daily', 'special', 'bundle', 'gem', 'gold') NOT NULL,
    
    -- Cost
    cost_gems INT UNSIGNED DEFAULT 0,
    cost_gold INT UNSIGNED DEFAULT 0,
    cost_real_money DECIMAL(10,2) DEFAULT 0, -- For IAP
    
    -- Rewards (JSON array)
    rewards JSON NOT NULL, -- [{"type":"card","card_id":1,"count":10},...]
    
    -- Limits
    purchase_limit INT UNSIGNED DEFAULT 1, -- 0 = unlimited
    per_player_limit INT UNSIGNED DEFAULT 1,
    
    -- Schedule
    starts_at TIMESTAMP NOT NULL,
    ends_at TIMESTAMP NOT NULL,
    
    -- Display
    display_order INT DEFAULT 0,
    banner_image VARCHAR(128),
    is_enabled BOOLEAN DEFAULT TRUE,
    
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    INDEX idx_schedule (starts_at, ends_at, is_enabled)
) ENGINE=InnoDB;
```

```sql
-- Player purchase history
CREATE TABLE player_purchases (
    purchase_id BIGINT UNSIGNED PRIMARY KEY AUTO_INCREMENT,
    player_id BIGINT UNSIGNED NOT NULL,
    offer_id BIGINT UNSIGNED NOT NULL,
    
    currency_type ENUM('gems', 'gold', 'real_money') NOT NULL,
    amount_spent INT UNSIGNED NOT NULL,
    
    -- For IAP verification
    transaction_id VARCHAR(255) NULL,
    platform ENUM('ios', 'android', 'web') NULL,
    receipt_data TEXT NULL,
    is_verified BOOLEAN DEFAULT FALSE,
    
    purchased_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    FOREIGN KEY (player_id) REFERENCES players(player_id) ON DELETE CASCADE,
    FOREIGN KEY (offer_id) REFERENCES shop_offers(offer_id),
    INDEX idx_player_date (player_id, purchased_at DESC)
) ENGINE=InnoDB;
```

### 2.7 Clan System

```sql
-- Clans
CREATE TABLE clans (
    clan_id BIGINT UNSIGNED PRIMARY KEY AUTO_INCREMENT,
    name VARCHAR(64) NOT NULL,
    tag VARCHAR(16) NOT NULL UNIQUE, -- #ABC1234
    description TEXT,
    description_key VARCHAR(128),
    
    -- Settings
    type ENUM('open', 'invite_only', 'closed') DEFAULT 'invite_only',
    required_trophies INT DEFAULT 0,
    required_level TINYINT UNSIGNED DEFAULT 1,
    war_frequency ENUM('always', 'weekly', 'never') DEFAULT 'weekly',
    
    -- Stats
    trophies BIGINT UNSIGNED DEFAULT 0,
    members_count TINYINT UNSIGNED DEFAULT 0,
    max_members TINYINT UNSIGNED DEFAULT 50,
    
    -- Clan War / Capital
    war_wins INT UNSIGNED DEFAULT 0,
    war_losses INT UNSIGNED DEFAULT 0,
    capital_level TINYINT UNSIGNED DEFAULT 1,
    
    -- Badge
    badge_id INT UNSIGNED DEFAULT 1,
    
    -- Timestamps
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    
    INDEX idx_tag (tag),
    INDEX idx_trophies (trophies DESC),
    INDEX idx_type (type)
) ENGINE=InnoDB;
```

```sql
-- Clan members
CREATE TABLE clan_members (
    clan_id BIGINT UNSIGNED NOT NULL,
    player_id BIGINT UNSIGNED NOT NULL,
    role ENUM('leader', 'co_leader', 'elder', 'member') DEFAULT 'member',
    
    -- Contribution
    donations INT UNSIGNED DEFAULT 0,
    donations_received INT UNSIGNED DEFAULT 0,
    weekly_donations INT UNSIGNED DEFAULT 0,
    weekly_donations_reset TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    -- War
    war_opt_in BOOLEAN DEFAULT TRUE,
    war_attacks_used TINYINT UNSIGNED DEFAULT 0,
    war_best_attack INT UNSIGNED DEFAULT 0,
    
    joined_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    PRIMARY KEY (clan_id, player_id),
    FOREIGN KEY (clan_id) REFERENCES clans(clan_id) ON DELETE CASCADE,
    FOREIGN KEY (player_id) REFERENCES players(player_id) ON DELETE CASCADE,
    INDEX idx_role (clan_id, role)
) ENGINE=InnoDB;
```

```sql
-- Clan chat messages
CREATE TABLE clan_chat (
    message_id BIGINT UNSIGNED PRIMARY KEY AUTO_INCREMENT,
    clan_id BIGINT UNSIGNED NOT NULL,
    player_id BIGINT UNSIGNED NOT NULL,
    
    message_type ENUM('text', 'replay', 'donation_request', 'donation', 'system') NOT NULL,
    content TEXT, -- Text or JSON for structured
    
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    FOREIGN KEY (clan_id) REFERENCES clans(clan_id) ON DELETE CASCADE,
    FOREIGN KEY (player_id) REFERENCES players(player_id) ON DELETE CASCADE,
    INDEX idx_clan_time (clan_id, created_at DESC)
) ENGINE=InnoDB;
```

```sql
-- Donation requests
CREATE TABLE clan_donations (
    request_id BIGINT UNSIGNED PRIMARY KEY AUTO_INCREMENT,
    clan_id BIGINT UNSIGNED NOT NULL,
    player_id BIGINT UNSIGNED NOT NULL, -- Requester
    card_id INT UNSIGNED NOT NULL,
    count_requested TINYINT UNSIGNED NOT NULL,
    count_filled TINYINT UNSIGNED DEFAULT 0,
    
    status ENUM('open', 'filled', 'expired', 'cancelled') DEFAULT 'open',
    
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    expires_at TIMESTAMP NOT NULL, -- 24 hours typically
    filled_at TIMESTAMP NULL,
    
    FOREIGN KEY (clan_id) REFERENCES clans(clan_id) ON DELETE CASCADE,
    FOREIGN KEY (player_id) REFERENCES players(player_id) ON DELETE CASCADE,
    FOREIGN KEY (card_id) REFERENCES cards(card_id),
    INDEX idx_clan_status (clan_id, status, expires_at)
) ENGINE=InnoDB;
```

```sql
-- Donation fulfillments
CREATE TABLE clan_donation_fills (
    fill_id BIGINT UNSIGNED PRIMARY KEY AUTO_INCREMENT,
    request_id BIGINT UNSIGNED NOT NULL,
    donor_id BIGINT UNSIGNED NOT NULL,
    count_filled TINYINT UNSIGNED NOT NULL,
    
    filled_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    FOREIGN KEY (request_id) REFERENCES clan_donations(request_id) ON DELETE CASCADE,
    FOREIGN KEY (donor_id) REFERENCES players(player_id) ON DELETE CASCADE
) ENGINE=InnoDB;
```

### 2.8 Quests & Events

```sql
-- Quest definitions
CREATE TABLE quests (
    quest_id INT UNSIGNED PRIMARY KEY,
    name_key VARCHAR(64),
    description_key VARCHAR(128),
    
    quest_type ENUM('daily', 'weekly', 'seasonal', 'achievement', 'tutorial') NOT NULL,
    
    -- Requirements (JSON)
    requirements JSON, -- {"type": "win_battles", "count": 3, "mode": "ladder"}
    
    -- Rewards
    rewards JSON, -- [{"type":"gold","amount":500},{"type":"chest","chest_id":1}]
    
    -- Schedule
    starts_at TIMESTAMP NULL, -- NULL = always available
    ends_at TIMESTAMP NULL,
    reset_cron VARCHAR(64), -- For recurring: "0 0 * * *" = daily
    
    xp_reward INT UNSIGNED DEFAULT 0,
    is_enabled BOOLEAN DEFAULT TRUE,
    
    INDEX idx_type_schedule (quest_type, starts_at, ends_at)
) ENGINE=InnoDB;
```

```sql
-- Player quest progress
CREATE TABLE player_quests (
    player_id BIGINT UNSIGNED NOT NULL,
    quest_id INT UNSIGNED NOT NULL,
    
    progress JSON, -- {"current": 2, "target": 3}
    status ENUM('active', 'completed', 'claimed', 'expired') DEFAULT 'active',
    
    started_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    completed_at TIMESTAMP NULL,
    claimed_at TIMESTAMP NULL,
    
    PRIMARY KEY (player_id, quest_id),
    FOREIGN KEY (player_id) REFERENCES players(player_id) ON DELETE CASCADE,
    FOREIGN KEY (quest_id) REFERENCES quests(quest_id),
    INDEX idx_player_status (player_id, status)
) ENGINE=InnoDB;
```

### 2.9 Season / Battle Pass

```sql
-- Seasons
CREATE TABLE seasons (
    season_id INT UNSIGNED PRIMARY KEY,
    name_key VARCHAR(64),
    theme VARCHAR(64),
    
    starts_at TIMESTAMP NOT NULL,
    ends_at TIMESTAMP NOT NULL,
    
    -- Battle Pass
    pass_tiers INT UNSIGNED DEFAULT 35,
    free_track_rewards JSON, -- Tier -> rewards
    paid_track_rewards JSON,
    pass_price_gems INT UNSIGNED DEFAULT 500,
    
    is_active BOOLEAN DEFAULT FALSE
) ENGINE=InnoDB;
```

```sql
-- Player season progress
CREATE TABLE player_seasons (
    player_id BIGINT UNSIGNED NOT NULL,
    season_id INT UNSIGNED NOT NULL,
    
    tier_reached TINYINT UNSIGNED DEFAULT 0,
    is_paid_pass BOOLEAN DEFAULT FALSE,
    paid_claimed_tiers JSON, -- [1,2,3...]
    free_claimed_tiers JSON,
    
    crowns_earned INT UNSIGNED DEFAULT 0,
    
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    
    PRIMARY KEY (player_id, season_id),
    FOREIGN KEY (player_id) REFERENCES players(player_id) ON DELETE CASCADE,
    FOREIGN KEY (season_id) REFERENCES seasons(season_id)
) ENGINE=InnoDB;
```

### 2.10 Tournament / Challenge

```sql
-- Challenges/Tournaments
CREATE TABLE tournaments (
    tournament_id BIGINT UNSIGNED PRIMARY KEY AUTO_INCREMENT,
    name VARCHAR(128) NOT NULL,
    description TEXT,
    
    tournament_type ENUM('classic', 'grand', 'draft', '2v2', 'custom') NOT NULL,
    
    -- Entry
    entry_cost_gems INT UNSIGNED DEFAULT 0,
    entry_cost_gold INT UNSIGNED DEFAULT 0,
    max_losses TINYINT UNSIGNED DEFAULT 3,
    max_wins TINYINT UNSIGNED DEFAULT 12,
    
    -- Rules
    card_level_cap TINYINT UNSIGNED DEFAULT 11, -- Tournament standard
    allowed_cards JSON NULL, -- NULL = all cards
    banned_cards JSON NULL,
    
    -- Schedule
    starts_at TIMESTAMP NOT NULL,
    ends_at TIMESTAMP NOT NULL,
    
    -- Rewards
    rewards JSON, -- [{"wins": 12, "rewards": [...]}]
    
    is_enabled BOOLEAN DEFAULT TRUE,
    max_participants INT UNSIGNED DEFAULT 0, -- 0 = unlimited
    
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    INDEX idx_schedule (starts_at, ends_at, is_enabled)
) ENGINE=InnoDB;
```

```sql
-- Player tournament entries
CREATE TABLE tournament_entries (
    entry_id BIGINT UNSIGNED PRIMARY KEY AUTO_INCREMENT,
    tournament_id BIGINT UNSIGNED NOT NULL,
    player_id BIGINT UNSIGNED NOT NULL,
    
    deck_id BIGINT UNSIGNED NULL, -- For draft: NULL until built
    wins TINYINT UNSIGNED DEFAULT 0,
    losses TINYINT UNSIGNED DEFAULT 0,
    
    status ENUM('active', 'completed', 'retired', 'disqualified') DEFAULT 'active',
    
    started_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    completed_at TIMESTAMP NULL,
    
    final_rewards JSON, -- Claimed rewards
    
    FOREIGN KEY (tournament_id) REFERENCES tournaments(tournament_id) ON DELETE CASCADE,
    FOREIGN KEY (player_id) REFERENCES players(player_id) ON DELETE CASCADE,
    FOREIGN KEY (deck_id) REFERENCES player_decks(deck_id),
    UNIQUE KEY uk_tournament_player (tournament_id, player_id),
    INDEX idx_status (tournament_id, status)
) ENGINE=InnoDB;
```

### 2.11 Settings & Misc

```sql
-- Player settings (JSON for flexibility)
CREATE TABLE player_settings (
    player_id BIGINT UNSIGNED PRIMARY KEY,
    
    -- Graphics
    graphics_quality ENUM('low', 'medium', 'high', 'ultra') DEFAULT 'high',
    frame_rate_cap TINYINT UNSIGNED DEFAULT 60,
    vsync BOOLEAN DEFAULT TRUE,
    show_damage_numbers BOOLEAN DEFAULT TRUE,
    camera_shake BOOLEAN DEFAULT TRUE,
    
    -- Audio
    master_volume TINYINT UNSIGNED DEFAULT 100,
    music_volume TINYINT UNSIGNED DEFAULT 80,
    sfx_volume TINYINT UNSIGNED DEFAULT 100,
    voice_volume TINYINT UNSIGNED DEFAULT 100,
    mute_on_focus_loss BOOLEAN DEFAULT TRUE,
    
    -- Gameplay
    deploy_mode ENUM('tap', 'drag', 'both') DEFAULT 'both',
    auto_target ENUM('on', 'off') DEFAULT 'on',
    left_handed_mode BOOLEAN DEFAULT FALSE,
    
    -- Privacy
    show_online_status BOOLEAN DEFAULT TRUE,
    allow_friend_requests BOOLEAN DEFAULT TRUE,
    allow_clan_invites BOOLEAN DEFAULT TRUE,
    
    -- Notifications
    notify_battle_ready BOOLEAN DEFAULT TRUE,
    notify_chest_ready BOOLEAN DEFAULT TRUE,
    notify_clan_chat BOOLEAN DEFAULT TRUE,
    notify_donation_request BOOLEAN DEFAULT TRUE,
    
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    
    FOREIGN KEY (player_id) REFERENCES players(player_id) ON DELETE CASCADE
) ENGINE=InnoDB;
```

```sql
-- Game configuration (server-side balance)
CREATE TABLE game_config (
    config_key VARCHAR(64) PRIMARY KEY,
    config_value JSON NOT NULL,
    description TEXT,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    updated_by VARCHAR(64)
) ENGINE=InnoDB;

-- Example configs:
-- "elixir_generation_rate": {"normal": 2.8, "double": 1.4, "triple": 0.93}
-- "max_elixir": 10
-- "starting_elixir": 5
-- "battle_duration_seconds": 180
-- "overtime_duration_seconds": 180
-- "chest_slots": 4
-- "max_deck_cards": 8
-- "max_champions_per_deck": 1
```

### 2.12 Leaderboards

```sql
-- Global leaderboards (refreshed periodically)
CREATE TABLE leaderboards (
    leaderboard_id INT UNSIGNED PRIMARY KEY AUTO_INCREMENT,
    leaderboard_type ENUM('trophies', 'wins', 'win_streak', 'donations', 'war_wins') NOT NULL,
    scope ENUM('global', 'country', 'clan', 'friends') NOT NULL,
    scope_id BIGINT UNSIGNED NULL, -- country code, clan_id, etc.
    
    player_id BIGINT UNSIGNED NOT NULL,
    rank INT UNSIGNED NOT NULL,
    score BIGINT NOT NULL,
    
    snapshot_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    FOREIGN KEY (player_id) REFERENCES players(player_id),
    UNIQUE KEY uk_lb_player (leaderboard_type, scope, scope_id, player_id),
    INDEX idx_lb_rank (leaderboard_type, scope, scope_id, rank)
) ENGINE=InnoDB;
```

## 3. INDEXING STRATEGY

### Critical Indexes
- `players`: trophies (DESC), clan_id, tag
- `player_cards`: player_id + level (for deck building)
- `battles`: player_1_id + created_at, battle_type + created_at
- `battle_events`: battle_id + timestamp_ms
- `player_chests`: player_id + slot_index
- `clan_members`: clan_id + role
- `clan_chat`: clan_id + created_at
- `tournament_entries`: tournament_id + status

## 4. PARTITIONING (For Scale)

```sql
-- Partition battles by month
ALTER TABLE battles PARTITION BY RANGE (YEAR(created_at)*100 + MONTH(created_at)) (
    PARTITION p202401 VALUES LESS THAN (202402),
    PARTITION p202402 VALUES LESS THAN (202403),
    -- ... add monthly partitions
    PARTITION p_future VALUES LESS THAN MAXVALUE
);

-- Partition battle_events by battle_id ranges (or time)
ALTER TABLE battle_events PARTITION BY HASH(battle_id) PARTITIONS 16;
```

## 5. STORED PROCEDURES / FUNCTIONS

```sql
-- Get player's card count for deck building
DELIMITER //
CREATE FUNCTION GetPlayerCardCount(p_player_id BIGINT, p_card_id INT)
RETURNS INT
READS SQL DATA
DETERMINISTIC
BEGIN
    DECLARE v_count INT DEFAULT 0;
    SELECT count INTO v_count FROM player_cards 
    WHERE player_id = p_player_id AND card_id = p_card_id;
    RETURN v_count;
END//
DELIMITER ;

-- Calculate trophy change (simplified ELO)
DELIMITER //
CREATE FUNCTION CalculateTrophyChange(
    p_winner_trophies INT, 
    p_loser_trophies INT, 
    p_is_overtime BOOLEAN
) RETURNS INT
READS SQL DATA
DETERMINISTIC
BEGIN
    DECLARE v_diff INT;
    DECLARE v_change INT;
    
    SET v_diff = p_winner_trophies - p_loser_trophies;
    
    -- Base change: 30 trophies
    -- Adjust by trophy difference: +/- 1 per 100 trophy diff
    SET v_change = 30 - FLOOR(v_diff / 100);
    
    -- Clamp
    IF v_change > 45 THEN SET v_change = 45; END IF;
    IF v_change < 15 THEN SET v_change = 15; END IF;
    
    -- Overtime bonus
    IF p_is_overtime THEN SET v_change = v_change + 5; END IF;
    
    RETURN v_change;
END//
DELIMITER ;
```

## 6. TRIGGERS

```sql
-- Update player trophy count on battle end
DELIMITER //
CREATE TRIGGER trg_battle_trophy_update
AFTER INSERT ON battles
FOR EACH ROW
BEGIN
    IF NEW.player_1_trophy_change != 0 THEN
        UPDATE players SET trophies = trophies + NEW.player_1_trophy_change
        WHERE player_id = NEW.player_1_id;
    END IF;
    
    IF NEW.player_2_trophy_change != 0 AND NEW.player_2_id IS NOT NULL THEN
        UPDATE players SET trophies = trophies + NEW.player_2_trophy_change
        WHERE player_id = NEW.player_2_id;
    END IF;
    
    -- Update best trophies
    UPDATE players SET best_trophies = GREATEST(best_trophies, trophies)
    WHERE player_id IN (NEW.player_1_id, NEW.player_2_id);
END//
DELIMITER ;
```

```sql
-- Update clan member count
DELIMITER //
CREATE TRIGGER trg_clan_member_count
AFTER INSERT ON clan_members
FOR EACH ROW
BEGIN
    UPDATE clans SET members_count = members_count + 1
    WHERE clan_id = NEW.clan_id;
END//
DELIMITER ;

CREATE TRIGGER trg_clan_member_remove
AFTER DELETE ON clan_members
FOR EACH ROW
BEGIN
    UPDATE clans SET members_count = members_count - 1
    WHERE clan_id = OLD.clan_id;
END//
DELIMITER ;
```

## 7. VIEWS (For Common Queries)

```sql
-- Player deck with card details
CREATE VIEW v_player_deck_details AS
SELECT 
    pd.deck_id,
    pd.player_id,
    pd.name,
    pd.is_active,
    pd.avg_elixir,
    pd.has_champion,
    c1.name as card1_name, c1.rarity as card1_rarity, c1.type as card1_type,
    c2.name as card2_name, c2.rarity as card2_rarity, c2.type as card2_type,
    c3.name as card3_name, c3.rarity as card3_rarity, c3.type as card3_type,
    c4.name as card4_name, c4.rarity as card4_rarity, c4.type as card4_type,
    c5.name as card5_name, c5.rarity as card5_rarity, c5.type as card5_type,
    c6.name as card6_name, c6.rarity as card6_rarity, c6.type as card6_type,
    c7.name as card7_name, c7.rarity as card7_rarity, c7.type as card7_type,
    c8.name as card8_name, c8.rarity as card8_rarity, c8.type as card8_type
FROM player_decks pd
LEFT JOIN cards c1 ON pd.slot_1_card = c1.card_id
LEFT JOIN cards c2 ON pd.slot_2_card = c2.card_id
LEFT JOIN cards c3 ON pd.slot_3_card = c3.card_id
LEFT JOIN cards c4 ON pd.slot_4_card = c4.card_id
LEFT JOIN cards c5 ON pd.slot_5_card = c5.card_id
LEFT JOIN cards c6 ON pd.slot_6_card = c6.card_id
LEFT JOIN cards c7 ON pd.slot_7_card = c7.card_id
LEFT JOIN cards c8 ON pd.slot_8_card = c8.card_id;
```

```sql
-- Player collection summary
CREATE VIEW v_player_collection AS
SELECT 
    pc.player_id,
    c.card_id,
    c.name,
    c.rarity,
    c.type,
    pc.count,
    pc.level,
    cls.hitpoints,
    cls.damage,
    pc.upgrade_progress,
    cls.cards_required as cards_for_next_level,
    cls.gold_cost as gold_for_next_level
FROM player_cards pc
JOIN cards c ON pc.card_id = c.card_id
LEFT JOIN card_level_stats cls ON c.card_id = cls.card_id AND pc.level + 1 = cls.level
WHERE pc.count > 0;
```

## 8. MIGRATION STRATEGY

### Version Tracking
```sql
CREATE TABLE schema_migrations (
    version VARCHAR(32) PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    applied_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    checksum VARCHAR(64)
);
```

### Migration Files
- `V1__initial_schema.sql`
- `V2__add_clan_system.sql`
- `V3__add_tournaments.sql`
- `V4__add_season_pass.sql`
- `V5__add_replay_system.sql`

---

*This schema supports all Clash Royale features. Adjust partitioning and indexes based on actual load testing.*