-- Migration 001: Initial Schema
-- Players, cards, card_level_stats, player_cards, player_decks, battles, battle_events, replays, game_config, schema_migrations
-- Run on MySQL 8.0+

SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS = 0;

-- Schema migrations table (must be first)
CREATE TABLE IF NOT EXISTS `schema_migrations` (
    `version` VARCHAR(32) PRIMARY KEY,
    `name` VARCHAR(255) NOT NULL,
    `applied_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    `checksum` VARCHAR(64)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Players table
CREATE TABLE IF NOT EXISTS `players` (
    `id` VARCHAR(36) PRIMARY KEY,
    `username` VARCHAR(32) NOT NULL UNIQUE,
    `email` VARCHAR(255) UNIQUE,
    `password_hash` VARCHAR(255) NOT NULL,
    `auth_provider` ENUM('email', 'google', 'apple', 'guest') DEFAULT 'email',
    `auth_provider_id` VARCHAR(255) NULL,
    `trophies` INT DEFAULT 0,
    `best_trophies` INT DEFAULT 0,
    `level` TINYINT UNSIGNED DEFAULT 1,
    `experience` BIGINT UNSIGNED DEFAULT 0,
    `gold` BIGINT UNSIGNED DEFAULT 0,
    `gems` INT UNSIGNED DEFAULT 0,
    `avatar_id` INT UNSIGNED DEFAULT 1,
    `name_color` VARCHAR(7) DEFAULT '#FFFFFF',
    `tag` VARCHAR(16) UNIQUE,
    `language` VARCHAR(5) DEFAULT 'en',
    `push_notifications` BOOLEAN DEFAULT TRUE,
    `clan_id` BIGINT UNSIGNED NULL,
    `clan_role` ENUM('leader','co_leader','elder','member') NULL,
    `last_login_at` TIMESTAMP NULL,
    `last_battle_at` TIMESTAMP NULL,
    `created_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    `updated_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `is_banned` BOOLEAN DEFAULT FALSE,
    `ban_reason` VARCHAR(255) NULL,
    `ban_expires_at` TIMESTAMP NULL,
    
    INDEX `idx_trophies` (`trophies` DESC),
    INDEX `idx_username` (`username`),
    INDEX `idx_tag` (`tag`),
    INDEX `idx_clan` (`clan_id`),
    INDEX `idx_email` (`email`),
    INDEX `idx_auth_provider` (`auth_provider`, `auth_provider_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Player devices (for push notifications)
CREATE TABLE IF NOT EXISTS `player_devices` (
    `device_id` BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    `player_id` VARCHAR(36) NOT NULL,
    `platform` ENUM('ios', 'android', 'windows', 'mac', 'linux') NOT NULL,
    `push_token` VARCHAR(512) NOT NULL,
    `app_version` VARCHAR(32),
    `device_model` VARCHAR(128),
    `os_version` VARCHAR(64),
    `is_active` BOOLEAN DEFAULT TRUE,
    `created_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    `last_used_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    
    FOREIGN KEY (`player_id`) REFERENCES `players`(`id`) ON DELETE CASCADE,
    UNIQUE KEY `uk_player_device` (`player_id`, `push_token`)
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
    `sprite_id` VARCHAR(64),
    `portrait_id` VARCHAR(64),
    `is_enabled` BOOLEAN DEFAULT TRUE,
    `release_date` DATE,
    `created_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    `updated_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    
    INDEX `idx_rarity` (`rarity`),
    INDEX `idx_type` (`type`),
    INDEX `idx_arena` (`arena`),
    INDEX `idx_enabled` (`is_enabled`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Card level stats (precomputed for fast lookup)
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
    `ability_level` TINYINT UNSIGNED DEFAULT 1,
    `updated_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    
    PRIMARY KEY (`player_id`, `card_id`),
    FOREIGN KEY (`player_id`) REFERENCES `players`(`id`) ON DELETE CASCADE,
    FOREIGN KEY (`card_id`) REFERENCES `cards`(`card_id`) ON DELETE CASCADE,
    INDEX `idx_player_level` (`player_id`, `level` DESC)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Player decks (multiple saved decks, 8 slots each)
CREATE TABLE IF NOT EXISTS `player_decks` (
    `id` VARCHAR(36) PRIMARY KEY,
    `player_id` VARCHAR(36) NOT NULL,
    `name` VARCHAR(64) DEFAULT 'My Deck',
    `slot_1_card` INT UNSIGNED NULL,
    `slot_2_card` INT UNSIGNED NULL,
    `slot_3_card` INT UNSIGNED NULL,
    `slot_4_card` INT UNSIGNED NULL,
    `slot_5_card` INT UNSIGNED NULL,
    `slot_6_card` INT UNSIGNED NULL,
    `slot_7_card` INT UNSIGNED NULL,
    `slot_8_card` INT UNSIGNED NULL,
    `avg_elixir` DECIMAL(3,1),
    `has_champion` BOOLEAN DEFAULT FALSE,
    `is_active` BOOLEAN DEFAULT FALSE,
    `created_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    `updated_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    
    FOREIGN KEY (`player_id`) REFERENCES `players`(`id`) ON DELETE CASCADE,
    FOREIGN KEY (`slot_1_card`) REFERENCES `cards`(`card_id`),
    FOREIGN KEY (`slot_2_card`) REFERENCES `cards`(`card_id`),
    FOREIGN KEY (`slot_3_card`) REFERENCES `cards`(`card_id`),
    FOREIGN KEY (`slot_4_card`) REFERENCES `cards`(`card_id`),
    FOREIGN KEY (`slot_5_card`) REFERENCES `cards`(`card_id`),
    FOREIGN KEY (`slot_6_card`) REFERENCES `cards`(`card_id`),
    FOREIGN KEY (`slot_7_card`) REFERENCES `cards`(`card_id`),
    FOREIGN KEY (`slot_8_card`) REFERENCES `cards`(`card_id`),
    INDEX `idx_player_active` (`player_id`, `is_active`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Battles table
CREATE TABLE IF NOT EXISTS `battles` (
    `battle_id` BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    `battle_type` ENUM('ladder','2v2','tournament','challenge','friendly','clan_war','practice','bot') NOT NULL,
    `player_1_id` VARCHAR(36) NOT NULL,
    `player_2_id` VARCHAR(36) NULL,
    `player_3_id` VARCHAR(36) NULL,
    `player_4_id` VARCHAR(36) NULL,
    `player_1_deck_id` VARCHAR(36) NOT NULL,
    `player_2_deck_id` VARCHAR(36) NULL,
    `player_3_deck_id` VARCHAR(36) NULL,
    `player_4_deck_id` VARCHAR(36) NULL,
    `winner_team` TINYINT UNSIGNED NULL,
    `player_1_crowns` TINYINT UNSIGNED DEFAULT 0,
    `player_2_crowns` TINYINT UNSIGNED DEFAULT 0,
    `player_3_crowns` TINYINT UNSIGNED DEFAULT 0,
    `player_4_crowns` TINYINT UNSIGNED DEFAULT 0,
    `player_1_king_hp` INT UNSIGNED,
    `player_2_king_hp` INT UNSIGNED,
    `player_3_king_hp` INT UNSIGNED,
    `player_4_king_hp` INT UNSIGNED,
    `duration_seconds` INT UNSIGNED,
    `went_overtime` BOOLEAN DEFAULT FALSE,
    `player_1_trophy_change` SMALLINT DEFAULT 0,
    `player_2_trophy_change` SMALLINT DEFAULT 0,
    `player_3_trophy_change` SMALLINT DEFAULT 0,
    `player_4_trophy_change` SMALLINT DEFAULT 0,
    `replay_id` BIGINT UNSIGNED NULL,
    `replay_seed` BIGINT UNSIGNED,
    `player_1_trophies_at_start` INT,
    `player_2_trophies_at_start` INT,
    `player_3_trophies_at_start` INT,
    `player_4_trophies_at_start` INT,
    `created_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    FOREIGN KEY (`player_1_id`) REFERENCES `players`(`id`),
    FOREIGN KEY (`player_2_id`) REFERENCES `players`(`id`),
    FOREIGN KEY (`player_3_id`) REFERENCES `players`(`id`),
    FOREIGN KEY (`player_4_id`) REFERENCES `players`(`id`),
    FOREIGN KEY (`player_1_deck_id`) REFERENCES `player_decks`(`id`),
    FOREIGN KEY (`player_2_deck_id`) REFERENCES `player_decks`(`id`),
    FOREIGN KEY (`player_3_deck_id`) REFERENCES `player_decks`(`id`),
    FOREIGN KEY (`player_4_deck_id`) REFERENCES `player_decks`(`id`),
    INDEX `idx_player_date` (`player_1_id`, `created_at` DESC),
    INDEX `idx_type_date` (`battle_type`, `created_at` DESC),
    INDEX `idx_replay` (`replay_id`),
    INDEX `idx_player2_date` (`player_2_id`, `created_at` DESC)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Battle events (for replays/analytics)
CREATE TABLE IF NOT EXISTS `battle_events` (
    `event_id` BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    `battle_id` BIGINT UNSIGNED NOT NULL,
    `tick` INT UNSIGNED NOT NULL,
    `timestamp_ms` INT UNSIGNED NOT NULL,
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
    INDEX `idx_battle_tick` (`battle_id`, `tick`),
    INDEX `idx_battle_time` (`battle_id`, `timestamp_ms`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Replays
CREATE TABLE IF NOT EXISTS `replays` (
    `replay_id` BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    `battle_id` BIGINT UNSIGNED NOT NULL UNIQUE,
    `replay_data` LONGBLOB NOT NULL,
    `replay_version` VARCHAR(16) NOT NULL,
    `player_1_id` VARCHAR(36),
    `player_2_id` VARCHAR(36),
    `player_1_deck_hash` VARCHAR(64),
    `player_2_deck_hash` VARCHAR(64),
    `duration_seconds` INT UNSIGNED,
    `created_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    `expires_at` TIMESTAMP NULL,
    
    FOREIGN KEY (`battle_id`) REFERENCES `battles`(`battle_id`) ON DELETE CASCADE,
    INDEX `idx_player` (`player_1_id`, `created_at` DESC),
    INDEX `idx_deck` (`player_1_deck_hash`),
    INDEX `idx_expires` (`expires_at`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Chest types
CREATE TABLE IF NOT EXISTS `chest_types` (
    `chest_id` INT UNSIGNED PRIMARY KEY,
    `name` VARCHAR(64) NOT NULL,
    `name_key` VARCHAR(64),
    `description` TEXT,
    `unlock_time_seconds` INT UNSIGNED,
    `slots` TINYINT UNSIGNED,
    `reward_table` JSON,
    `sprite_id` VARCHAR(64),
    `is_enabled` BOOLEAN DEFAULT TRUE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Player chest slots (4 active + 1 queue)
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

-- Game config (server-side balance, hot-reloadable)
CREATE TABLE IF NOT EXISTS `game_config` (
    `config_key` VARCHAR(64) PRIMARY KEY,
    `config_value` JSON NOT NULL,
    `description` TEXT,
    `updated_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `updated_by` VARCHAR(64)
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
('tournament_standard_level', '11', 'Tournament standard card level'),
('chest_cycle', '[{"chest_id":1,"weight":50},{"chest_id":2,"weight":30},{"chest_id":3,"weight":15},{"chest_id":4,"weight":5}]', 'Chest cycle weights'),
('donation_limits', '{"common": 10, "rare": 1, "epic": 0, "legendary": 0, "champion": 0}', 'Max cards per donation request by rarity'),
('donation_rewards', '{"common": {"gold": 5, "xp": 1}, "rare": {"gold": 50, "xp": 10}, "epic": {"gold": 500, "xp": 100}, "legendary": {"gold": 2000, "xp": 500}, "champion": {"gold": 5000, "xp": 1000}}', 'Gold and XP rewards for donations'),
('trophy_formula', '{"base": 30, "diff_factor": 100, "min": 15, "max": 45, "overtime_bonus": 5}', 'Trophy calculation parameters'),
('xp_per_level', '[0, 100, 200, 400, 800, 1600, 3200, 6400, 12800, 25600, 51200, 102400, 204800, 409600, 819200]', 'XP required for each king level');

SET FOREIGN_KEY_CHECKS = 1;