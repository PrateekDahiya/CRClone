-- Migration 003: Tournaments & Challenges
-- Tournaments, tournament_entries, tournament_rewards

SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS = 0;

-- Tournaments/Challenges
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
    `rewards` JSON NOT NULL,
    `is_enabled` BOOLEAN DEFAULT TRUE,
    `max_participants` INT UNSIGNED DEFAULT 0,
    `created_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    `updated_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    
    INDEX `idx_schedule` (`starts_at`, `ends_at`, `is_enabled`),
    INDEX `idx_type` (`tournament_type`)
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
    `draft_picks` JSON NULL,
    
    FOREIGN KEY (`tournament_id`) REFERENCES `tournaments`(`tournament_id`) ON DELETE CASCADE,
    FOREIGN KEY (`player_id`) REFERENCES `players`(`id`) ON DELETE CASCADE,
    FOREIGN KEY (`deck_id`) REFERENCES `player_decks`(`id`),
    UNIQUE KEY `uk_tournament_player` (`tournament_id`, `player_id`),
    INDEX `idx_status` (`tournament_id`, `status`),
    INDEX `idx_player` (`player_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Tournament reward tiers (by wins)
CREATE TABLE IF NOT EXISTS `tournament_rewards` (
    `reward_id` BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    `tournament_id` BIGINT UNSIGNED NOT NULL,
    `min_wins` TINYINT UNSIGNED NOT NULL,
    `max_wins` TINYINT UNSIGNED NOT NULL,
    `rewards` JSON NOT NULL,
    
    FOREIGN KEY (`tournament_id`) REFERENCES `tournaments`(`tournament_id`) ON DELETE CASCADE,
    UNIQUE KEY `uk_tournament_wins` (`tournament_id`, `min_wins`, `max_wins`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Global tournaments (always available, no schedule)
CREATE TABLE IF NOT EXISTS `global_tournaments` (
    `tournament_id` BIGINT UNSIGNED PRIMARY KEY,
    `is_active` BOOLEAN DEFAULT TRUE,
    `auto_reset` BOOLEAN DEFAULT FALSE,
    `reset_cron` VARCHAR(64) NULL,
    
    FOREIGN KEY (`tournament_id`) REFERENCES `tournaments`(`tournament_id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Tournament matchmaking queue (for active tournaments)
CREATE TABLE IF NOT EXISTS `tournament_queue` (
    `queue_id` BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    `tournament_id` BIGINT UNSIGNED NOT NULL,
    `player_id` VARCHAR(36) NOT NULL,
    `entry_id` BIGINT UNSIGNED NOT NULL,
    `trophies_at_entry` INT NOT NULL,
    `joined_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    FOREIGN KEY (`tournament_id`) REFERENCES `tournaments`(`tournament_id`) ON DELETE CASCADE,
    FOREIGN KEY (`player_id`) REFERENCES `players`(`id`) ON DELETE CASCADE,
    FOREIGN KEY (`entry_id`) REFERENCES `tournament_entries`(`entry_id`) ON DELETE CASCADE,
    UNIQUE KEY `uk_tournament_queue_player` (`tournament_id`, `player_id`),
    INDEX `idx_tournament_trophies` (`tournament_id`, `trophies_at_entry`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

SET FOREIGN_KEY_CHECKS = 1;