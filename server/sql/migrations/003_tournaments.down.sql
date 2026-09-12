-- Down migration 003: Tournaments & Challenges
-- Reverts 003_tournaments.sql. Tables dropped in reverse dependency order
-- (queue -> globals/rewards -> entries -> tournaments).

SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS = 0;

DROP TABLE IF EXISTS `tournament_queue`;
DROP TABLE IF EXISTS `global_tournaments`;
DROP TABLE IF EXISTS `tournament_rewards`;
DROP TABLE IF EXISTS `tournament_entries`;
DROP TABLE IF EXISTS `tournaments`;

SET FOREIGN_KEY_CHECKS = 1;
