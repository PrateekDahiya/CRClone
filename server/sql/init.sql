-- Clash Royale Clone Database Initialization
-- Run this on MySQL 8.0+

SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS = 0;

-- Players table
CREATE TABLE IF NOT EXISTS `players` (
    `id` VARCHAR(36) PRIMARY KEY,
    `username` VARCHAR(32) NOT NULL UNIQUE,
    `email` VARCHAR(255) UNIQUE,
    `password_hash` VARCHAR(255) NOT NULL,
    `trophies` INT DEFAULT 0,
    `best_trophies` INT DEFAULT 0,
    `level` TINYINT UNSIGNED DEFAULT 1,
    `experience` BIGINT UNSIGNED DEFAULT 0,
    `gold` BIGINT UNSIGNED DEFAULT 0,
    `gems` INT UNSIGNED DEFAULT 0,
    `avatar_id` INT UNSIGNED DEFAULT 1,
    `name_color` VARCHAR(7) DEFAULT '#FFFFFF',
    `created_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    `last_login_at` TIMESTAMP NULL,
    `is_banned` BOOLEAN DEFAULT FALSE,
    `ban_reason` VARCHAR(255) NULL,
    `ban_expires_at` TIMESTAMP NULL,
    
    INDEX `idx_trophies` (`trophies` DESC),
    INDEX `idx_username` (`username`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Cards table (static data)
CREATE TABLE IF NOT EXISTS `cards` (
    `card_id` INT UNSIGNED PRIMARY KEY,
    `name` VARCHAR(64) NOT NULL,
    `name_key` VARCHAR(64) NOT NULL,
    `description` TEXT,
    `description_key` VARCHAR(128),
    `rarity` ENUM('common','rare','epic','legendary','champion') NOT NULL,
    `type` ENUM('troop','spell','building','champion') NOT NULL,
    `arena` TINYINT UNSIGNED DEFAULT 0,
    `elixir_cost` TINYINT UNSIGNED NOT NULL,
    `hitpoints` INT UNSIGNED,
    `damage` INT UNSIGNED,
    `hit_speed` DECIMAL(4,2),
    `range` DECIMAL(4,2),
    `speed` ENUM('very_slow','slow','medium','fast','very_fast'),
    `deploy_time` TINYINT UNSIGNED DEFAULT 1,
    `target_type` ENUM('ground','air','both','buildings','any') DEFAULT 'both',
    `count` TINYINT UNSIGNED DEFAULT 1,
    `mechanics` JSON,
    `is_enabled` BOOLEAN DEFAULT TRUE,
    `release_date` DATE,
    `created_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    `updated_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    
    INDEX `idx_rarity` (`rarity`),
    INDEX `idx_type` (`type`),
    INDEX `idx_arena` (`arena`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Card level stats
CREATE TABLE IF NOT EXISTS `card_level_stats` (
    `card_id` INT UNSIGNED NOT NULL,
    `level` TINYINT UNSIGNED NOT NULL,
    `hitpoints` INT UNSIGNED,
    `damage` INT UNSIGNED,
    `hit_speed` DECIMAL(4,2),
    `range` DECIMAL(4,2),
    `gold_cost` INT UNSIGNED,
    `cards_required` INT UNSIGNED,
    
    PRIMARY KEY (`card_id`, `level`),
    FOREIGN KEY (`card_id`) REFERENCES `cards`(`card_id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Player cards (collection)
CREATE TABLE IF NOT EXISTS `player_cards` (
    `player_id` VARCHAR(36) NOT NULL,
    `card_id` INT UNSIGNED NOT NULL,
    `count` INT UNSIGNED DEFAULT 0,
    `level` TINYINT UNSIGNED DEFAULT 1,
    `upgrade_progress` INT UNSIGNED DEFAULT 0,
    `updated_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    
    PRIMARY KEY (`player_id`, `card_id`),
    FOREIGN KEY (`player_id`) REFERENCES `players`(`id`) ON DELETE CASCADE,
    FOREIGN KEY (`card_id`) REFERENCES `cards`(`card_id`) ON DELETE CASCADE,
    INDEX `idx_player_level` (`player_id`, `level` DESC)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Player decks
CREATE TABLE IF NOT EXISTS `player_decks` (
    `id` VARCHAR(36) PRIMARY KEY,
    `player_id` VARCHAR(36) NOT NULL,
    `name` VARCHAR(64) DEFAULT 'My Deck',
    `card_ids` JSON NOT NULL,
    `avg_elixir` DECIMAL(3,1),
    `has_champion` BOOLEAN DEFAULT FALSE,
    `is_active` BOOLEAN DEFAULT FALSE,
    `created_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    `updated_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    
    FOREIGN KEY (`player_id`) REFERENCES `players`(`id`) ON DELETE CASCADE,
    INDEX `idx_player_active` (`player_id`, `is_active`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Battles table (partitioned by month)
CREATE TABLE IF NOT EXISTS `battles` (
    `battle_id` BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    `battle_type` ENUM('ladder','2v2','tournament','challenge','friendly','clan_war','practice') NOT NULL,
    `player1_id` VARCHAR(36) NOT NULL,
    `player2_id` VARCHAR(36) NULL,
    `player1_deck_id` VARCHAR(36) NOT NULL,
    `player2_deck_id` VARCHAR(36) NULL,
    `winner` ENUM('player1','player2','draw') NULL,
    `player1_crowns` TINYINT UNSIGNED DEFAULT 0,
    `player2_crowns` TINYINT UNSIGNED DEFAULT 0,
    `player1_king_hp` INT UNSIGNED,
    `player2_king_hp` INT UNSIGNED,
    `duration` INT UNSIGNED,
    `went_overtime` BOOLEAN DEFAULT FALSE,
    `player1_trophy_change` SMALLINT DEFAULT 0,
    `player2_trophy_change` SMALLINT DEFAULT 0,
    `replay_id` BIGINT UNSIGNED NULL,
    `replay_seed` BIGINT UNSIGNED,
    `player1_trophies_at_start` INT,
    `player2_trophies_at_start` INT,
    `created_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    FOREIGN KEY (`player1_id`) REFERENCES `players`(`id`),
    FOREIGN KEY (`player2_id`) REFERENCES `players`(`id`),
    FOREIGN KEY (`player1_deck_id`) REFERENCES `player_decks`(`id`),
    FOREIGN KEY (`player2_deck_id`) REFERENCES `player_decks`(`id`),
    INDEX `idx_player_date` (`player1_id`, `created_at` DESC),
    INDEX `idx_type_date` (`battle_type`, `created_at` DESC),
    INDEX `idx_replay` (`replay_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Battle events (for replays)
CREATE TABLE IF NOT EXISTS `battle_events` (
    `event_id` BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    `battle_id` BIGINT UNSIGNED NOT NULL,
    `tick` INT UNSIGNED NOT NULL,
    `player_id` VARCHAR(36) NOT NULL,
    `event_type` ENUM(
        'card_played','spell_cast','unit_spawned','unit_died',
        'building_placed','building_destroyed','tower_damaged',
        'tower_destroyed','king_activated','elixir_gained',
        'champion_ability_used'
    ) NOT NULL,
    `card_id` INT UNSIGNED NULL,
    `position_x` DECIMAL(5,2) NULL,
    `position_y` DECIMAL(5,2) NULL,
    `target_entity_id` BIGINT UNSIGNED NULL,
    `damage` INT NULL,
    `data` JSON,
    
    FOREIGN KEY (`battle_id`) REFERENCES `battles`(`battle_id`) ON DELETE CASCADE,
    FOREIGN KEY (`player_id`) REFERENCES `players`(`id`),
    FOREIGN KEY (`card_id`) REFERENCES `cards`(`card_id`),
    INDEX `idx_battle_tick` (`battle_id`, `tick`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Replays
CREATE TABLE IF NOT EXISTS `replays` (
    `replay_id` BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    `battle_id` BIGINT UNSIGNED NOT NULL UNIQUE,
    `replay_data` LONGBLOB NOT NULL,
    `replay_version` VARCHAR(16) NOT NULL,
    `player1_id` VARCHAR(36),
    `player2_id` VARCHAR(36),
    `player1_deck_hash` VARCHAR(64),
    `player2_deck_hash` VARCHAR(64),
    `duration` INT UNSIGNED,
    `created_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    `expires_at` TIMESTAMP NULL,
    
    FOREIGN KEY (`battle_id`) REFERENCES `battles`(`battle_id`) ON DELETE CASCADE,
    INDEX `idx_player` (`player1_id`, `created_at` DESC),
    INDEX `idx_deck` (`player1_deck_hash`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Chest types
CREATE TABLE IF NOT EXISTS `chest_types` (
    `chest_id` INT UNSIGNED PRIMARY KEY,
    `name` VARCHAR(64) NOT NULL,
    `name_key` VARCHAR(64),
    `unlock_time_seconds` INT UNSIGNED,
    `slots` TINYINT UNSIGNED,
    `reward_table` JSON,
    `sprite_id` VARCHAR(64),
    `is_enabled` BOOLEAN DEFAULT TRUE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Player chest slots
CREATE TABLE IF NOT EXISTS `player_chests` (
    `chest_slot_id` BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    `player_id` VARCHAR(36) NOT NULL,
    `chest_type_id` INT UNSIGNED NOT NULL,
    `slot_index` TINYINT UNSIGNED NOT NULL,
    `status` ENUM('locked','unlocking','ready','empty') DEFAULT 'empty',
    `unlock_started_at` TIMESTAMP NULL,
    `unlock_finishes_at` TIMESTAMP NULL,
    `queue_position` TINYINT UNSIGNED NULL,
    `created_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    `updated_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    
    FOREIGN KEY (`player_id`) REFERENCES `players`(`id`) ON DELETE CASCADE,
    FOREIGN KEY (`chest_type_id`) REFERENCES `chest_types`(`chest_id`),
    UNIQUE KEY `uk_player_slot` (`player_id`, `slot_index`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Chest rewards
CREATE TABLE IF NOT EXISTS `chest_rewards` (
    `reward_id` BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    `chest_slot_id` BIGINT UNSIGNED NOT NULL,
    `player_id` VARCHAR(36) NOT NULL,
    `reward_type` ENUM('card','gold','gems','wild_card') NOT NULL,
    `card_id` INT UNSIGNED NULL,
    `card_count` INT UNSIGNED DEFAULT 0,
    `gold_amount` INT UNSIGNED DEFAULT 0,
    `gems_amount` INT UNSIGNED DEFAULT 0,
    `rarity` ENUM('common','rare','epic','legendary','champion') NULL,
    `opened_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    FOREIGN KEY (`chest_slot_id`) REFERENCES `player_chests`(`chest_slot_id`) ON DELETE CASCADE,
    FOREIGN KEY (`player_id`) REFERENCES `players`(`id`) ON DELETE CASCADE,
    FOREIGN KEY (`card_id`) REFERENCES `cards`(`card_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Clans
CREATE TABLE IF NOT EXISTS `clans` (
    `clan_id` BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    `name` VARCHAR(64) NOT NULL,
    `tag` VARCHAR(16) NOT NULL UNIQUE,
    `description` TEXT,
    `type` ENUM('open','invite_only','closed') DEFAULT 'invite_only',
    `required_trophies` INT DEFAULT 0,
    `required_level` TINYINT UNSIGNED DEFAULT 1,
    `war_frequency` ENUM('always','weekly','never') DEFAULT 'weekly',
    `trophies` BIGINT UNSIGNED DEFAULT 0,
    `members_count` TINYINT UNSIGNED DEFAULT 0,
    `max_members` TINYINT UNSIGNED DEFAULT 50,
    `war_wins` INT UNSIGNED DEFAULT 0,
    `war_losses` INT UNSIGNED DEFAULT 0,
    `capital_level` TINYINT UNSIGNED DEFAULT 1,
    `badge_id` INT UNSIGNED DEFAULT 1,
    `created_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    `updated_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    
    INDEX `idx_tag` (`tag`),
    INDEX `idx_trophies` (`trophies` DESC),
    INDEX `idx_type` (`type`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Clan members
CREATE TABLE IF NOT EXISTS `clan_members` (
    `clan_id` BIGINT UNSIGNED NOT NULL,
    `player_id` VARCHAR(36) NOT NULL,
    `role` ENUM('leader','co_leader','elder','member') DEFAULT 'member',
    `donations` INT UNSIGNED DEFAULT 0,
    `donations_received` INT UNSIGNED DEFAULT 0,
    `weekly_donations` INT UNSIGNED DEFAULT 0,
    `weekly_donations_reset` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    `war_opt_in` BOOLEAN DEFAULT TRUE,
    `war_attacks_used` TINYINT UNSIGNED DEFAULT 0,
    `war_best_attack` INT UNSIGNED DEFAULT 0,
    `joined_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    PRIMARY KEY (`clan_id`, `player_id`),
    FOREIGN KEY (`clan_id`) REFERENCES `clans`(`clan_id`) ON DELETE CASCADE,
    FOREIGN KEY (`player_id`) REFERENCES `players`(`id`) ON DELETE CASCADE,
    INDEX `idx_role` (`clan_id`, `role`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Clan chat
CREATE TABLE IF NOT EXISTS `clan_chat` (
    `message_id` BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    `clan_id` BIGINT UNSIGNED NOT NULL,
    `player_id` VARCHAR(36) NOT NULL,
    `message_type` ENUM('text','replay','donation_request','donation','system') NOT NULL,
    `content` TEXT,
    `created_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    FOREIGN KEY (`clan_id`) REFERENCES `clans`(`clan_id`) ON DELETE CASCADE,
    FOREIGN KEY (`player_id`) REFERENCES `players`(`id`) ON DELETE CASCADE,
    INDEX `idx_clan_time` (`clan_id`, `created_at` DESC)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Clan donations
CREATE TABLE IF NOT EXISTS `clan_donations` (
    `request_id` BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    `clan_id` BIGINT UNSIGNED NOT NULL,
    `player_id` VARCHAR(36) NOT NULL,
    `card_id` INT UNSIGNED NOT NULL,
    `count_requested` TINYINT UNSIGNED NOT NULL,
    `count_filled` TINYINT UNSIGNED DEFAULT 0,
    `status` ENUM('open','filled','expired','cancelled') DEFAULT 'open',
    `created_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    `expires_at` TIMESTAMP NOT NULL,
    `filled_at` TIMESTAMP NULL,
    
    FOREIGN KEY (`clan_id`) REFERENCES `clans`(`clan_id`) ON DELETE CASCADE,
    FOREIGN KEY (`player_id`) REFERENCES `players`(`id`) ON DELETE CASCADE,
    FOREIGN KEY (`card_id`) REFERENCES `cards`(`card_id`),
    INDEX `idx_clan_status` (`clan_id`, `status`, `expires_at`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Donation fills
CREATE TABLE IF NOT EXISTS `clan_donation_fills` (
    `fill_id` BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    `request_id` BIGINT UNSIGNED NOT NULL,
    `donor_id` VARCHAR(36) NOT NULL,
    `count_filled` TINYINT UNSIGNED NOT NULL,
    `filled_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    FOREIGN KEY (`request_id`) REFERENCES `clan_donations`(`request_id`) ON DELETE CASCADE,
    FOREIGN KEY (`donor_id`) REFERENCES `players`(`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Quests
CREATE TABLE IF NOT EXISTS `quests` (
    `quest_id` INT UNSIGNED PRIMARY KEY,
    `name_key` VARCHAR(64),
    `description_key` VARCHAR(128),
    `quest_type` ENUM('daily','weekly','seasonal','achievement','tutorial') NOT NULL,
    `requirements` JSON,
    `rewards` JSON,
    `starts_at` TIMESTAMP NULL,
    `ends_at` TIMESTAMP NULL,
    `reset_cron` VARCHAR(64),
    `xp_reward` INT UNSIGNED DEFAULT 0,
    `is_enabled` BOOLEAN DEFAULT TRUE,
    
    INDEX `idx_type_schedule` (`quest_type`, `starts_at`, `ends_at`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Player quests
CREATE TABLE IF NOT EXISTS `player_quests` (
    `player_id` VARCHAR(36) NOT NULL,
    `quest_id` INT UNSIGNED NOT NULL,
    `progress` JSON,
    `status` ENUM('active','completed','claimed','expired') DEFAULT 'active',
    `started_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    `completed_at` TIMESTAMP NULL,
    `claimed_at` TIMESTAMP NULL,
    
    PRIMARY KEY (`player_id`, `quest_id`),
    FOREIGN KEY (`player_id`) REFERENCES `players`(`id`) ON DELETE CASCADE,
    FOREIGN KEY (`quest_id`) REFERENCES `quests`(`quest_id`),
    INDEX `idx_player_status` (`player_id`, `status`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Seasons
CREATE TABLE IF NOT EXISTS `seasons` (
    `season_id` INT UNSIGNED PRIMARY KEY,
    `name_key` VARCHAR(64),
    `theme` VARCHAR(64),
    `starts_at` TIMESTAMP NOT NULL,
    `ends_at` TIMESTAMP NOT NULL,
    `pass_tiers` INT UNSIGNED DEFAULT 35,
    `free_track_rewards` JSON,
    `paid_track_rewards` JSON,
    `pass_price_gems` INT UNSIGNED DEFAULT 500,
    `is_active` BOOLEAN DEFAULT FALSE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Player seasons
CREATE TABLE IF NOT EXISTS `player_seasons` (
    `player_id` VARCHAR(36) NOT NULL,
    `season_id` INT UNSIGNED NOT NULL,
    `tier_reached` TINYINT UNSIGNED DEFAULT 0,
    `is_paid_pass` BOOLEAN DEFAULT FALSE,
    `paid_claimed_tiers` JSON,
    `free_claimed_tiers` JSON,
    `crowns_earned` INT UNSIGNED DEFAULT 0,
    `updated_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    
    PRIMARY KEY (`player_id`, `season_id`),
    FOREIGN KEY (`player_id`) REFERENCES `players`(`id`) ON DELETE CASCADE,
    FOREIGN KEY (`season_id`) REFERENCES `seasons`(`season_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Tournaments
CREATE TABLE IF NOT EXISTS `tournaments` (
    `tournament_id` BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    `name` VARCHAR(128) NOT NULL,
    `description` TEXT,
    `tournament_type` ENUM('classic','grand','draft','2v2','custom') NOT NULL,
    `entry_cost_gems` INT UNSIGNED DEFAULT 0,
    `entry_cost_gold` INT UNSIGNED DEFAULT 0,
    `max_losses` TINYINT UNSIGNED DEFAULT 3,
    `max_wins` TINYINT UNSIGNED DEFAULT 12,
    `card_level_cap` TINYINT UNSIGNED DEFAULT 11,
    `allowed_cards` JSON NULL,
    `banned_cards` JSON NULL,
    `starts_at` TIMESTAMP NOT NULL,
    `ends_at` TIMESTAMP NOT NULL,
    `rewards` JSON,
    `is_enabled` BOOLEAN DEFAULT TRUE,
    `max_participants` INT UNSIGNED DEFAULT 0,
    `created_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    INDEX `idx_schedule` (`starts_at`, `ends_at`, `is_enabled`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Tournament entries
CREATE TABLE IF NOT EXISTS `tournament_entries` (
    `entry_id` BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    `tournament_id` BIGINT UNSIGNED NOT NULL,
    `player_id` VARCHAR(36) NOT NULL,
    `deck_id` VARCHAR(36) NULL,
    `wins` TINYINT UNSIGNED DEFAULT 0,
    `losses` TINYINT UNSIGNED DEFAULT 0,
    `status` ENUM('active','completed','retired','disqualified') DEFAULT 'active',
    `started_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    `completed_at` TIMESTAMP NULL,
    `final_rewards` JSON,
    
    FOREIGN KEY (`tournament_id`) REFERENCES `tournaments`(`tournament_id`) ON DELETE CASCADE,
    FOREIGN KEY (`player_id`) REFERENCES `players`(`id`) ON DELETE CASCADE,
    FOREIGN KEY (`deck_id`) REFERENCES `player_decks`(`deck_id`),
    UNIQUE KEY `uk_tournament_player` (`tournament_id`, `player_id`),
    INDEX `idx_status` (`tournament_id`, `status`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Player settings
CREATE TABLE IF NOT EXISTS `player_settings` (
    `player_id` VARCHAR(36) PRIMARY KEY,
    `graphics_quality` ENUM('low','medium','high','ultra') DEFAULT 'high',
    `frame_rate_cap` TINYINT UNSIGNED DEFAULT 60,
    `vsync` BOOLEAN DEFAULT TRUE,
    `show_damage_numbers` BOOLEAN DEFAULT TRUE,
    `camera_shake` BOOLEAN DEFAULT TRUE,
    `master_volume` TINYINT UNSIGNED DEFAULT 100,
    `music_volume` TINYINT UNSIGNED DEFAULT 80,
    `sfx_volume` TINYINT UNSIGNED DEFAULT 100,
    `voice_volume` TINYINT UNSIGNED DEFAULT 100,
    `mute_on_focus_loss` BOOLEAN DEFAULT TRUE,
    `deploy_mode` ENUM('tap','drag','both') DEFAULT 'both',
    `auto_target` ENUM('on','off') DEFAULT 'on',
    `left_handed_mode` BOOLEAN DEFAULT FALSE,
    `show_online_status` BOOLEAN DEFAULT TRUE,
    `allow_friend_requests` BOOLEAN DEFAULT TRUE,
    `allow_clan_invites` BOOLEAN DEFAULT TRUE,
    `notify_battle_ready` BOOLEAN DEFAULT TRUE,
    `notify_chest_ready` BOOLEAN DEFAULT TRUE,
    `notify_clan_chat` BOOLEAN DEFAULT TRUE,
    `notify_donation_request` BOOLEAN DEFAULT TRUE,
    `updated_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    
    FOREIGN KEY (`player_id`) REFERENCES `players`(`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Game config
CREATE TABLE IF NOT EXISTS `game_config` (
    `config_key` VARCHAR(64) PRIMARY KEY,
    `config_value` JSON NOT NULL,
    `description` TEXT,
    `updated_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `updated_by` VARCHAR(64)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Schema migrations
CREATE TABLE IF NOT EXISTS `schema_migrations` (
    `version` VARCHAR(32) PRIMARY KEY,
    `name` VARCHAR(255) NOT NULL,
    `applied_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    `checksum` VARCHAR(64)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Insert initial game config
INSERT IGNORE INTO `game_config` (`config_key`, `config_value`, `description`) VALUES
('elixir_generation_rate', '{"normal": 2.8, "double": 1.4, "triple": 0.93}', 'Elixir generation rates in seconds per elixir'),
('max_elixir', '10', 'Maximum elixir cap'),
('starting_elixir', '5', 'Starting elixir at battle start'),
('battle_duration_seconds', '180', 'Normal battle duration in seconds'),
('overtime_duration_seconds', '180', 'Overtime duration in seconds'),
('chest_slots', '4', 'Number of chest slots'),
('max_deck_cards', '8', 'Maximum cards in a deck'),
('max_champions_per_deck', '1', 'Maximum champions per deck'),
('hand_size', '4', 'Number of cards in hand'),
('tournament_standard_level', '11', 'Tournament standard card level');

SET FOREIGN_KEY_CHECKS = 1;