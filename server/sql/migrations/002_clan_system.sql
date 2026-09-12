-- Migration 002: Clan System
-- Clans, clan_members, clan_chat, clan_donations, clan_donation_fills

SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS = 0;

-- Clans
CREATE TABLE IF NOT EXISTS `clans` (
    `clan_id` BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    `name` VARCHAR(64) NOT NULL,
    `tag` VARCHAR(16) NOT NULL UNIQUE,
    `description` TEXT,
    `description_key` VARCHAR(128),
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
    INDEX `idx_type` (`type`),
    INDEX `idx_required_trophies` (`required_trophies`)
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
    INDEX `idx_role` (`clan_id`, `role`),
    INDEX `idx_player` (`player_id`)
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

-- Clan donations (requests)
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
    INDEX `idx_clan_status` (`clan_id`, `status`, `expires_at`),
    INDEX `idx_player` (`player_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Donation fills
CREATE TABLE IF NOT EXISTS `clan_donation_fills` (
    `fill_id` BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    `request_id` BIGINT UNSIGNED NOT NULL,
    `donor_id` VARCHAR(36) NOT NULL,
    `count_filled` TINYINT UNSIGNED NOT NULL,
    `filled_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    FOREIGN KEY (`request_id`) REFERENCES `clan_donations`(`request_id`) ON DELETE CASCADE,
    FOREIGN KEY (`donor_id`) REFERENCES `players`(`id`) ON DELETE CASCADE,
    INDEX `idx_request` (`request_id`),
    INDEX `idx_donor` (`donor_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Clan invites
CREATE TABLE IF NOT EXISTS `clan_invites` (
    `invite_id` BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    `clan_id` BIGINT UNSIGNED NOT NULL,
    `inviter_id` VARCHAR(36) NOT NULL,
    `invitee_id` VARCHAR(36) NOT NULL,
    `status` ENUM('pending','accepted','declined','expired') DEFAULT 'pending',
    `created_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    `expires_at` TIMESTAMP NOT NULL,
    
    FOREIGN KEY (`clan_id`) REFERENCES `clans`(`clan_id`) ON DELETE CASCADE,
    FOREIGN KEY (`inviter_id`) REFERENCES `players`(`id`) ON DELETE CASCADE,
    FOREIGN KEY (`invitee_id`) REFERENCES `players`(`id`) ON DELETE CASCADE,
    UNIQUE KEY `uk_clan_invitee` (`clan_id`, `invitee_id`, `status`),
    INDEX `idx_invitee_status` (`invitee_id`, `status`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Clan war participation (for River Race / Classic War)
CREATE TABLE IF NOT EXISTS `clan_war_participation` (
    `war_id` BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    `clan_id` BIGINT UNSIGNED NOT NULL,
    `season_id` INT UNSIGNED NOT NULL,
    `war_type` ENUM('river_race','classic','friendly') NOT NULL,
    `status` ENUM('preparation','in_progress','finished') DEFAULT 'preparation',
    `started_at` TIMESTAMP NULL,
    `ended_at` TIMESTAMP NULL,
    `rank` TINYINT UNSIGNED NULL,
    `fame_earned` INT UNSIGNED DEFAULT 0,
    `repair_points` INT UNSIGNED DEFAULT 0,
    `created_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    FOREIGN KEY (`clan_id`) REFERENCES `clans`(`clan_id`) ON DELETE CASCADE,
    INDEX `idx_clan_season` (`clan_id`, `season_id`),
    INDEX `idx_status` (`status`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Clan war attacks
CREATE TABLE IF NOT EXISTS `clan_war_attacks` (
    `attack_id` BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    `war_id` BIGINT UNSIGNED NOT NULL,
    `attacker_id` VARCHAR(36) NOT NULL,
    `defender_id` VARCHAR(36) NULL,
    `battle_id` BIGINT UNSIGNED NULL,
    `damage_dealt` INT UNSIGNED DEFAULT 0,
    `crowns_earned` TINYINT UNSIGNED DEFAULT 0,
    `fame_earned` INT UNSIGNED DEFAULT 0,
    `is_replay` BOOLEAN DEFAULT FALSE,
    `created_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    FOREIGN KEY (`war_id`) REFERENCES `clan_war_participation`(`war_id`) ON DELETE CASCADE,
    FOREIGN KEY (`attacker_id`) REFERENCES `players`(`id`) ON DELETE CASCADE,
    FOREIGN KEY (`defender_id`) REFERENCES `players`(`id`) ON DELETE SET NULL,
    FOREIGN KEY (`battle_id`) REFERENCES `battles`(`battle_id`) ON DELETE SET NULL,
    INDEX `idx_war_attacker` (`war_id`, `attacker_id`),
    INDEX `idx_battle` (`battle_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Triggers for clan member count
DELIMITER //

CREATE TRIGGER IF NOT EXISTS `trg_clan_member_count_insert`
AFTER INSERT ON `clan_members`
FOR EACH ROW
BEGIN
    UPDATE `clans` SET `members_count` = `members_count` + 1
    WHERE `clan_id` = NEW.`clan_id`;
END//

CREATE TRIGGER IF NOT EXISTS `trg_clan_member_count_delete`
AFTER DELETE ON `clan_members`
FOR EACH ROW
BEGIN
    UPDATE `clans` SET `members_count` = GREATEST(`members_count` - 1, 0)
    WHERE `clan_id` = OLD.`clan_id`;
END//

DELIMITER ;

SET FOREIGN_KEY_CHECKS = 1;