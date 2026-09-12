-- Down migration 005: Replay System & Leaderboards
-- Reverts 005_replay_system.sql. Drops the reporting views first, then all
-- tables in reverse dependency order.

SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS = 0;

DROP VIEW IF EXISTS `v_replay_search`;
DROP VIEW IF EXISTS `v_battle_events_detailed`;

DROP TABLE IF EXISTS `countries`;
DROP TABLE IF EXISTS `friend_requests`;
DROP TABLE IF EXISTS `friends`;
DROP TABLE IF EXISTS `player_purchase_limits`;
DROP TABLE IF EXISTS `player_purchases`;
DROP TABLE IF EXISTS `shop_offers`;
DROP TABLE IF EXISTS `leaderboard_snapshots`;
DROP TABLE IF EXISTS `leaderboards`;

SET FOREIGN_KEY_CHECKS = 1;
