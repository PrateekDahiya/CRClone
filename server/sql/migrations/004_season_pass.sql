-- Migration 004: Season Pass / Battle Pass
-- Seasons, player_seasons, season_rewards

SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS = 0;

-- Seasons
CREATE TABLE IF NOT EXISTS `seasons` (
    `season_id` INT UNSIGNED PRIMARY KEY,
    `name_key` VARCHAR(64),
    `theme` VARCHAR(64),
    `starts_at` TIMESTAMP NOT NULL,
    `ends_at` TIMESTAMP NOT NULL,
    `pass_tiers` INT UNSIGNED DEFAULT 35,
    `free_track_rewards` JSON NOT NULL,
    `paid_track_rewards` JSON NOT NULL,
    `pass_price_gems` INT UNSIGNED DEFAULT 500,
    `is_active` BOOLEAN DEFAULT FALSE,
    `created_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    `updated_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    
    INDEX `idx_schedule` (`starts_at`, `ends_at`, `is_active`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Player season progress
CREATE TABLE IF NOT EXISTS `player_seasons` (
    `player_id` VARCHAR(36) NOT NULL,
    `season_id` INT UNSIGNED NOT NULL,
    `tier_reached` TINYINT UNSIGNED DEFAULT 0,
    `is_paid_pass` BOOLEAN DEFAULT FALSE,
    `paid_claimed_tiers` JSON,
    `free_claimed_tiers` JSON,
    `crowns_earned` INT UNSIGNED DEFAULT 0,
    `last_crown_at` TIMESTAMP NULL,
    `updated_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    
    PRIMARY KEY (`player_id`, `season_id`),
    FOREIGN KEY (`player_id`) REFERENCES `players`(`id`) ON DELETE CASCADE,
    FOREIGN KEY (`season_id`) REFERENCES `seasons`(`season_id`) ON DELETE CASCADE,
    INDEX `idx_season_tier` (`season_id`, `tier_reached` DESC)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Season crown milestones (additional rewards for crown accumulation)
CREATE TABLE IF NOT EXISTS `season_crown_milestones` (
    `milestone_id` INT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    `season_id` INT UNSIGNED NOT NULL,
    `crowns_required` INT UNSIGNED NOT NULL,
    `rewards` JSON NOT NULL,
    `is_paid_only` BOOLEAN DEFAULT FALSE,
    
    FOREIGN KEY (`season_id`) REFERENCES `seasons`(`season_id`) ON DELETE CASCADE,
    UNIQUE KEY `uk_season_crowns` (`season_id`, `crowns_required`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Quests
CREATE TABLE IF NOT EXISTS `quests` (
    `quest_id` INT UNSIGNED PRIMARY KEY,
    `name_key` VARCHAR(64),
    `description_key` VARCHAR(128),
    `quest_type` ENUM('daily','weekly','seasonal','achievement','tutorial') NOT NULL,
    `requirements` JSON NOT NULL,
    `rewards` JSON NOT NULL,
    `starts_at` TIMESTAMP NULL,
    `ends_at` TIMESTAMP NULL,
    `reset_cron` VARCHAR(64),
    `xp_reward` INT UNSIGNED DEFAULT 0,
    `is_enabled` BOOLEAN DEFAULT TRUE,
    `is_repeatable` BOOLEAN DEFAULT FALSE,
    `sort_order` INT DEFAULT 0,
    
    INDEX `idx_type_schedule` (`quest_type`, `starts_at`, `ends_at`),
    INDEX `idx_enabled` (`is_enabled`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Player quest progress
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
    FOREIGN KEY (`quest_id`) REFERENCES `quests`(`quest_id`) ON DELETE CASCADE,
    INDEX `idx_player_status` (`player_id`, `status`),
    INDEX `idx_quest_status` (`quest_id`, `status`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Quest progress auto-update triggers would be in application code, not DB

SET FOREIGN_KEY_CHECKS = 1;