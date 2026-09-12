-- Down migration 004: Season Pass / Battle Pass
-- Reverts 004_season_pass.sql. Tables dropped in reverse dependency order
-- (player progress -> milestones/quests -> seasons).

SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS = 0;

DROP TABLE IF EXISTS `player_quests`;
DROP TABLE IF EXISTS `season_crown_milestones`;
DROP TABLE IF EXISTS `player_seasons`;
DROP TABLE IF EXISTS `quests`;
DROP TABLE IF EXISTS `seasons`;

SET FOREIGN_KEY_CHECKS = 1;
