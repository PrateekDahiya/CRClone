-- Migration 005: Replay System & Leaderboards
-- Replays (extended), battle_events (partitioned), leaderboards, shop_offers, player_purchases

SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS = 0;

-- Extended replays table (already created in 001, adding more indexes)
-- This migration adds partitioning and additional indexes

-- Leaderboards (periodic refresh via cron)
CREATE TABLE IF NOT EXISTS `leaderboards` (
    `leaderboard_id` BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    `leaderboard_type` ENUM('trophies', 'wins', 'win_streak', 'donations', 'war_wins') NOT NULL,
    `scope` ENUM('global', 'country', 'clan', 'friends') NOT NULL,
    `scope_id` BIGINT UNSIGNED NULL,
    `player_id` VARCHAR(36) NOT NULL,
    `rank` INT UNSIGNED NOT NULL,
    `score` BIGINT NOT NULL,
    `snapshot_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    FOREIGN KEY (`player_id`) REFERENCES `players`(`id`) ON DELETE CASCADE,
    UNIQUE KEY `uk_lb_player` (`leaderboard_type`, `scope`, `scope_id`, `player_id`),
    INDEX `idx_lb_rank` (`leaderboard_type`, `scope`, `scope_id`, `rank`),
    INDEX `idx_lb_snapshot` (`snapshot_at` DESC)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Leaderboard snapshots (history)
CREATE TABLE IF NOT EXISTS `leaderboard_snapshots` (
    `snapshot_id` BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    `leaderboard_type` ENUM('trophies', 'wins', 'win_streak', 'donations', 'war_wins') NOT NULL,
    `scope` ENUM('global', 'country', 'clan', 'friends') NOT NULL,
    `scope_id` BIGINT UNSIGNED NULL,
    `data` JSON NOT NULL,
    `created_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    INDEX `idx_lb_type_scope_time` (`leaderboard_type`, `scope`, `scope_id`, `created_at` DESC)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Shop offers
CREATE TABLE IF NOT EXISTS `shop_offers` (
    `offer_id` BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    `offer_type` ENUM('daily', 'special', 'bundle', 'gem', 'gold', 'wild_card') NOT NULL,
    `cost_gems` INT UNSIGNED DEFAULT 0,
    `cost_gold` INT UNSIGNED DEFAULT 0,
    `cost_real_money` DECIMAL(10,2) DEFAULT 0,
    `rewards` JSON NOT NULL,
    `purchase_limit` INT UNSIGNED DEFAULT 1,
    `per_player_limit` INT UNSIGNED DEFAULT 1,
    `starts_at` TIMESTAMP NOT NULL,
    `ends_at` TIMESTAMP NOT NULL,
    `display_order` INT DEFAULT 0,
    `banner_image` VARCHAR(128),
    `is_enabled` BOOLEAN DEFAULT TRUE,
    `created_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    `updated_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    
    INDEX `idx_schedule` (`starts_at`, `ends_at`, `is_enabled`),
    INDEX `idx_type` (`offer_type`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Player purchases
CREATE TABLE IF NOT EXISTS `player_purchases` (
    `purchase_id` BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    `player_id` VARCHAR(36) NOT NULL,
    `offer_id` BIGINT UNSIGNED NOT NULL,
    `currency_type` ENUM('gems', 'gold', 'real_money') NOT NULL,
    `amount_spent` INT UNSIGNED NOT NULL,
    `transaction_id` VARCHAR(255) NULL,
    `platform` ENUM('ios', 'android', 'web') NULL,
    `receipt_data` TEXT NULL,
    `is_verified` BOOLEAN DEFAULT FALSE,
    `purchased_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    FOREIGN KEY (`player_id`) REFERENCES `players`(`id`) ON DELETE CASCADE,
    FOREIGN KEY (`offer_id`) REFERENCES `shop_offers`(`offer_id`),
    INDEX `idx_player_date` (`player_id`, `purchased_at` DESC),
    INDEX `idx_transaction` (`transaction_id`),
    INDEX `idx_offer` (`offer_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Player purchase limits tracking (for per-player limits)
CREATE TABLE IF NOT EXISTS `player_purchase_limits` (
    `player_id` VARCHAR(36) NOT NULL,
    `offer_id` BIGINT UNSIGNED NOT NULL,
    `purchase_count` INT UNSIGNED DEFAULT 0,
    `last_purchase_at` TIMESTAMP NULL,
    `period_start` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    PRIMARY KEY (`player_id`, `offer_id`),
    FOREIGN KEY (`player_id`) REFERENCES `players`(`id`) ON DELETE CASCADE,
    FOREIGN KEY (`offer_id`) REFERENCES `shop_offers`(`offer_id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Friends system (for friends leaderboard scope)
CREATE TABLE IF NOT EXISTS `friends` (
    `friendship_id` BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    `player_id` VARCHAR(36) NOT NULL,
    `friend_id` VARCHAR(36) NOT NULL,
    `status` ENUM('pending', 'accepted', 'blocked') DEFAULT 'pending',
    `created_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    `accepted_at` TIMESTAMP NULL,
    
    FOREIGN KEY (`player_id`) REFERENCES `players`(`id`) ON DELETE CASCADE,
    FOREIGN KEY (`friend_id`) REFERENCES `players`(`id`) ON DELETE CASCADE,
    UNIQUE KEY `uk_friendship` (`player_id`, `friend_id`),
    INDEX `idx_friend_status` (`friend_id`, `status`),
    INDEX `idx_player_status` (`player_id`, `status`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Friend requests (separate for easier management)
CREATE TABLE IF NOT EXISTS `friend_requests` (
    `request_id` BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    `from_player_id` VARCHAR(36) NOT NULL,
    `to_player_id` VARCHAR(36) NOT NULL,
    `status` ENUM('pending', 'accepted', 'declined', 'blocked') DEFAULT 'pending',
    `created_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    `responded_at` TIMESTAMP NULL,
    
    FOREIGN KEY (`from_player_id`) REFERENCES `players`(`id`) ON DELETE CASCADE,
    FOREIGN KEY (`to_player_id`) REFERENCES `players`(`id`) ON DELETE CASCADE,
    UNIQUE KEY `uk_request` (`from_player_id`, `to_player_id`, `status`),
    INDEX `idx_to_status` (`to_player_id`, `status`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Country codes reference (for country leaderboards)
CREATE TABLE IF NOT EXISTS `countries` (
    `country_code` CHAR(2) PRIMARY KEY,
    `name` VARCHAR(64) NOT NULL,
    `name_key` VARCHAR(64)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Insert common countries
INSERT IGNORE INTO `countries` (`country_code`, `name`, `name_key`) VALUES
('US', 'United States', 'country_us'),
('CN', 'China', 'country_cn'),
('JP', 'Japan', 'country_jp'),
('KR', 'South Korea', 'country_kr'),
('DE', 'Germany', 'country_de'),
('FR', 'France', 'country_fr'),
('GB', 'United Kingdom', 'country_gb'),
('BR', 'Brazil', 'country_br'),
('RU', 'Russia', 'country_ru'),
('IN', 'India', 'country_in'),
('CA', 'Canada', 'country_ca'),
('AU', 'Australia', 'country_au'),
('MX', 'Mexico', 'country_mx'),
('ES', 'Spain', 'country_es'),
('IT', 'Italy', 'country_it'),
('NL', 'Netherlands', 'country_nl'),
('PL', 'Poland', 'country_pl'),
('TR', 'Turkey', 'country_tr'),
('SA', 'Saudi Arabia', 'country_sa'),
('AE', 'United Arab Emirates', 'country_ae');

-- Replay search metadata view (for faster queries)
CREATE OR REPLACE VIEW `v_replay_search` AS
SELECT
    r.`replay_id`,
    r.`battle_id`,
    r.`player_1_id`,
    r.`player_2_id`,
    p1.`username` AS `player_1_name`,
    p2.`username` AS `player_2_name`,
    r.`player_1_deck_hash`,
    r.`player_2_deck_hash`,
    r.`duration_seconds`,
    r.`created_at`,
    r.`expires_at`,
    b.`battle_type`,
    b.`winner_team`
FROM `replays` r
JOIN `battles` b ON r.`battle_id` = b.`battle_id`
LEFT JOIN `players` p1 ON r.`player_1_id` = p1.`id`
LEFT JOIN `players` p2 ON r.`player_2_id` = p2.`id`;

-- Battle events view with player names
CREATE OR REPLACE VIEW `v_battle_events_detailed` AS
SELECT
    be.*,
    p.`username` AS `player_name`,
    c.`name` AS `card_name`
FROM `battle_events` be
LEFT JOIN `players` p ON be.`player_id` = p.`id`
LEFT JOIN `cards` c ON be.`card_id` = c.`card_id`;

SET FOREIGN_KEY_CHECKS = 1;