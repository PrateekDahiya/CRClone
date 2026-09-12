-- Down migration 001: Initial Schema
-- Reverts 001_initial_schema.sql. Drops all domain tables created by 001 in
-- reverse dependency order. The `schema_migrations` bookkeeping table itself
-- is intentionally NOT dropped (MigrationRunner deletes the version row after
-- these statements succeed). Run only via MigrationRunner.rollback('001').

SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS = 0;

DROP TABLE IF EXISTS `player_settings`;
DROP TABLE IF EXISTS `chest_rewards`;
DROP TABLE IF EXISTS `player_chests`;
DROP TABLE IF EXISTS `chest_types`;
DROP TABLE IF EXISTS `replays`;
DROP TABLE IF EXISTS `battle_events`;
DROP TABLE IF EXISTS `battles`;
DROP TABLE IF EXISTS `player_decks`;
DROP TABLE IF EXISTS `player_cards`;
DROP TABLE IF EXISTS `card_level_stats`;
DROP TABLE IF EXISTS `game_config`;
DROP TABLE IF EXISTS `player_devices`;
DROP TABLE IF EXISTS `cards`;
DROP TABLE IF EXISTS `players`;

SET FOREIGN_KEY_CHECKS = 1;
