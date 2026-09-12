-- Down migration 002: Clan System
-- Reverts 002_clan_system.sql. Drops the member-count triggers first, then
-- all clan tables in reverse dependency order.

SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS = 0;

DROP TRIGGER IF EXISTS `trg_clan_member_count_insert`;
DROP TRIGGER IF EXISTS `trg_clan_member_count_delete`;

DROP TABLE IF EXISTS `clan_war_attacks`;
DROP TABLE IF EXISTS `clan_war_participation`;
DROP TABLE IF EXISTS `clan_invites`;
DROP TABLE IF EXISTS `clan_donation_fills`;
DROP TABLE IF EXISTS `clan_donations`;
DROP TABLE IF EXISTS `clan_chat`;
DROP TABLE IF EXISTS `clan_members`;
DROP TABLE IF EXISTS `clans`;

SET FOREIGN_KEY_CHECKS = 1;
