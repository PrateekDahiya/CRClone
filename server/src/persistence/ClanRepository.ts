import { BaseRepository } from './BaseRepository';
import { Database, db } from './Database';
import { logger } from '../utils/logger';

export interface ClanRow {
  clan_id: number;
  name: string;
  tag: string;
  description: string | null;
  description_key: string | null;
  type: string;
  required_trophies: number;
  required_level: number;
  war_frequency: string;
  trophies: number;
  members_count: number;
  max_members: number;
  war_wins: number;
  war_losses: number;
  capital_level: number;
  badge_id: number;
  created_at: Date;
  updated_at: Date;
}

export interface ClanMemberRow {
  clan_id: number;
  player_id: string;
  role: string;
  donations: number;
  donations_received: number;
  weekly_donations: number;
  weekly_donations_reset: Date;
  war_opt_in: boolean;
  war_attacks_used: number;
  war_best_attack: number;
  joined_at: Date;
}

export interface ClanChatRow {
  message_id: number;
  clan_id: number;
  player_id: string;
  message_type: string;
  content: string;
  created_at: Date;
}

export interface ClanDonationRow {
  request_id: number;
  clan_id: number;
  player_id: string;
  card_id: number;
  count_requested: number;
  count_filled: number;
  status: string;
  created_at: Date;
  expires_at: Date;
  filled_at: Date | null;
}

export interface ClanDonationFillRow {
  fill_id: number;
  request_id: number;
  donor_id: string;
  count_filled: number;
  filled_at: Date;
}

export interface ClanInviteRow {
  invite_id: number;
  clan_id: number;
  inviter_id: string;
  invitee_id: string;
  status: string;
  created_at: Date;
  expires_at: Date;
}

export interface ClanWarParticipationRow {
  war_id: number;
  clan_id: number;
  season_id: number;
  war_type: string;
  status: string;
  started_at: Date | null;
  ended_at: Date | null;
  rank: number | null;
  fame_earned: number;
  repair_points: number;
  created_at: Date;
}

export interface ClanWarAttackRow {
  attack_id: number;
  war_id: number;
  attacker_id: string;
  defender_id: string | null;
  battle_id: number | null;
  damage_dealt: number;
  crowns_earned: number;
  fame_earned: number;
  is_replay: boolean;
  created_at: Date;
}

export class ClanRepository extends BaseRepository {
  constructor(database: Database = db) {
    super(database);
  }

  // Clan CRUD
  async createClan(clan: Omit<ClanRow, 'clan_id' | 'created_at' | 'updated_at'>): Promise<number> {
    const result = await this.execute(
      `INSERT INTO clans (name, tag, description, description_key, type, required_trophies, required_level, war_frequency, badge_id)
       VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)`,
      [
        clan.name,
        clan.tag,
        clan.description,
        clan.description_key,
        clan.type,
        clan.required_trophies,
        clan.required_level,
        clan.war_frequency,
        clan.badge_id || 1
      ]
    );
    return result.insertId;
  }

  async getClan(clanId: number): Promise<ClanRow | null> {
    const rows = await this.query<ClanRow>('SELECT * FROM clans WHERE clan_id = ?', [clanId]);
    return rows[0] || null;
  }

  async getClanByTag(tag: string): Promise<ClanRow | null> {
    const rows = await this.query<ClanRow>('SELECT * FROM clans WHERE tag = ?', [tag]);
    return rows[0] || null;
  }

  async updateClan(clanId: number, data: Partial<ClanRow>): Promise<void> {
    const { set, params } = this.buildUpdateClause(data);
    if (!set) return;
    params.push(clanId);
    await this.execute(`UPDATE clans SET ${set}, updated_at = NOW() WHERE clan_id = ?`, params);
  }

  async deleteClan(clanId: number): Promise<void> {
    await this.execute('DELETE FROM clans WHERE clan_id = ?', [clanId]);
  }

  async searchClans(query: string, type?: string, limit: number = 20): Promise<ClanRow[]> {
    let sql = 'SELECT * FROM clans WHERE (name LIKE ? OR tag LIKE ?)';
    const params: any[] = [`%${query}%`, `%${query}%`];

    if (type) {
      sql += ' AND type = ?';
      params.push(type);
    }

    sql += ' ORDER BY trophies DESC LIMIT ?';
    params.push(limit);

    return this.query<ClanRow>(sql, params);
  }

  async getTopClans(limit: number = 100): Promise<ClanRow[]> {
    return this.query<ClanRow>('SELECT * FROM clans ORDER BY trophies DESC LIMIT ?', [limit]);
  }

  // Clan Members
  async getClanMembers(clanId: number): Promise<ClanMemberRow[]> {
    return this.query<ClanMemberRow>(
      'SELECT * FROM clan_members WHERE clan_id = ? ORDER BY FIELD(role, "leader", "co_leader", "elder", "member"), donations DESC',
      [clanId]
    );
  }

  async getClanMember(clanId: number, playerId: string): Promise<ClanMemberRow | null> {
    const rows = await this.query<ClanMemberRow>(
      'SELECT * FROM clan_members WHERE clan_id = ? AND player_id = ?',
      [clanId, playerId]
    );
    return rows[0] || null;
  }

  async getPlayerClan(playerId: string): Promise<{clan: ClanRow, member: ClanMemberRow} | null> {
    const rows = await this.query<any>(
      `SELECT c.*, cm.* FROM clans c
       JOIN clan_members cm ON c.clan_id = cm.clan_id
       WHERE cm.player_id = ?`,
      [playerId]
    );
    if (rows.length === 0) return null;

    const row = rows[0];
    return {
      clan: row,
      member: row
    };
  }

  async joinClan(clanId: number, playerId: string, role: string = 'member'): Promise<void> {
    await this.execute(
      `INSERT INTO clan_members (clan_id, player_id, role)
       VALUES (?, ?, ?)`,
      [clanId, playerId, role]
    );
  }

  async leaveClan(clanId: number, playerId: string): Promise<void> {
    await this.execute('DELETE FROM clan_members WHERE clan_id = ? AND player_id = ?', [clanId, playerId]);
  }

  async updateMemberRole(clanId: number, playerId: string, role: string): Promise<void> {
    await this.execute(
      'UPDATE clan_members SET role = ? WHERE clan_id = ? AND player_id = ?',
      [role, clanId, playerId]
    );
  }

  async promoteMember(clanId: number, playerId: string): Promise<void> {
    const member = await this.getClanMember(clanId, playerId);
    if (!member) return;

    const roles = ['member', 'elder', 'co_leader', 'leader'];
    const currentIndex = roles.indexOf(member.role);
    if (currentIndex < roles.length - 1) {
      await this.updateMemberRole(clanId, playerId, roles[currentIndex + 1]);
    }
  }

  async demoteMember(clanId: number, playerId: string): Promise<void> {
    const member = await this.getClanMember(clanId, playerId);
    if (!member) return;

    const roles = ['member', 'elder', 'co_leader', 'leader'];
    const currentIndex = roles.indexOf(member.role);
    if (currentIndex > 0) {
      await this.updateMemberRole(clanId, playerId, roles[currentIndex - 1]);
    }
  }

  async kickMember(clanId: number, playerId: string): Promise<void> {
    await this.leaveClan(clanId, playerId);
  }

  async updateWarOptIn(clanId: number, playerId: string, optIn: boolean): Promise<void> {
    await this.execute(
      'UPDATE clan_members SET war_opt_in = ? WHERE clan_id = ? AND player_id = ?',
      [optIn, clanId, playerId]
    );
  }

  async incrementDonations(clanId: number, playerId: string, count: number): Promise<void> {
    await this.execute(
      `UPDATE clan_members SET donations = donations + ?, weekly_donations = weekly_donations + ? WHERE clan_id = ? AND player_id = ?`,
      [count, count, clanId, playerId]
    );
  }

  async incrementDonationsReceived(clanId: number, playerId: string, count: number): Promise<void> {
    await this.execute(
      'UPDATE clan_members SET donations_received = donations_received + ? WHERE clan_id = ? AND player_id = ?',
      [count, clanId, playerId]
    );
  }

  async resetWeeklyDonations(): Promise<void> {
    await this.execute(
      'UPDATE clan_members SET weekly_donations = 0, weekly_donations_reset = NOW()'
    );
  }

  // Clan Chat
  async getClanChat(clanId: number, limit: number = 50, beforeMessageId?: number): Promise<ClanChatRow[]> {
    let sql = 'SELECT * FROM clan_chat WHERE clan_id = ?';
    const params: any[] = [clanId];

    if (beforeMessageId) {
      sql += ' AND message_id < ?';
      params.push(beforeMessageId);
    }

    sql += ' ORDER BY created_at DESC LIMIT ?';
    params.push(limit);

    const rows = await this.query<ClanChatRow>(sql, params);
    return rows.reverse(); // Return chronological order
  }

  async sendClanMessage(clanId: number, playerId: string, messageType: string, content: string): Promise<number> {
    const result = await this.execute(
      'INSERT INTO clan_chat (clan_id, player_id, message_type, content) VALUES (?, ?, ?, ?)',
      [clanId, playerId, messageType, content]
    );
    return result.insertId;
  }

  async deleteClanMessage(messageId: number): Promise<void> {
    await this.execute('DELETE FROM clan_chat WHERE message_id = ?', [messageId]);
  }

  // Clan Donations
  async createDonationRequest(donation: Omit<ClanDonationRow, 'request_id' | 'count_filled' | 'status' | 'created_at' | 'filled_at'>): Promise<number> {
    const result = await this.execute(
      `INSERT INTO clan_donations (clan_id, player_id, card_id, count_requested, expires_at)
       VALUES (?, ?, ?, ?, ?)`,
      [donation.clan_id, donation.player_id, donation.card_id, donation.count_requested, donation.expires_at]
    );
    return result.insertId;
  }

  async getDonationRequest(requestId: number): Promise<ClanDonationRow | null> {
    const rows = await this.query<ClanDonationRow>('SELECT * FROM clan_donations WHERE request_id = ?', [requestId]);
    return rows[0] || null;
  }

  async getOpenDonations(clanId: number): Promise<ClanDonationRow[]> {
    return this.query<ClanDonationRow>(
      'SELECT * FROM clan_donations WHERE clan_id = ? AND status = "open" AND expires_at > NOW() ORDER BY created_at',
      [clanId]
    );
  }

  async getPlayerDonationRequests(playerId: string): Promise<ClanDonationRow[]> {
    return this.query<ClanDonationRow>(
      'SELECT * FROM clan_donations WHERE player_id = ? ORDER BY created_at DESC',
      [playerId]
    );
  }

  async fillDonation(requestId: number, donorId: string, count: number): Promise<void> {
    await this.transaction(async (conn) => {
      // Update request
      await conn.execute(
        `UPDATE clan_donations SET count_filled = count_filled + ?, 
         status = CASE WHEN count_filled + ? >= count_requested THEN 'filled' ELSE 'open' END,
         filled_at = CASE WHEN count_filled + ? >= count_requested THEN NOW() ELSE NULL END
         WHERE request_id = ?`,
        [count, count, count, requestId]
      );

      // Record fill
      await conn.execute(
        'INSERT INTO clan_donation_fills (request_id, donor_id, count_filled) VALUES (?, ?, ?)',
        [requestId, donorId, count]
      );
    });
  }

  async cancelDonation(requestId: number, playerId: string): Promise<void> {
    await this.execute(
      'UPDATE clan_donations SET status = "cancelled" WHERE request_id = ? AND player_id = ?',
      [requestId, playerId]
    );
  }

  async expireDonations(): Promise<number> {
    const result = await this.execute(
      'UPDATE clan_donations SET status = "expired" WHERE status = "open" AND expires_at < NOW()'
    );
    return result.affectedRows;
  }

  async getDonationFills(requestId: number): Promise<ClanDonationFillRow[]> {
    return this.query<ClanDonationFillRow>(
      'SELECT * FROM clan_donation_fills WHERE request_id = ? ORDER BY filled_at',
      [requestId]
    );
  }

  // Clan Invites
  async createInvite(invite: Omit<ClanInviteRow, 'invite_id' | 'created_at'>): Promise<number> {
    const result = await this.execute(
      'INSERT INTO clan_invites (clan_id, inviter_id, invitee_id, status, expires_at) VALUES (?, ?, ?, ?, ?)',
      [invite.clan_id, invite.inviter_id, invite.invitee_id, invite.status, invite.expires_at]
    );
    return result.insertId;
  }

  async getInvite(inviteId: number): Promise<ClanInviteRow | null> {
    const rows = await this.query<ClanInviteRow>('SELECT * FROM clan_invites WHERE invite_id = ?', [inviteId]);
    return rows[0] || null;
  }

  async getPlayerInvites(playerId: string): Promise<ClanInviteRow[]> {
    return this.query<ClanInviteRow>(
      'SELECT * FROM clan_invites WHERE invitee_id = ? AND status = "pending" AND expires_at > NOW() ORDER BY created_at DESC',
      [playerId]
    );
  }

  async acceptInvite(inviteId: number): Promise<void> {
    await this.execute(
      'UPDATE clan_invites SET status = "accepted" WHERE invite_id = ?',
      [inviteId]
    );
  }

  async declineInvite(inviteId: number): Promise<void> {
    await this.execute(
      'UPDATE clan_invites SET status = "declined" WHERE invite_id = ?',
      [inviteId]
    );
  }

  async expireInvites(): Promise<number> {
    const result = await this.execute(
      'UPDATE clan_invites SET status = "expired" WHERE status = "pending" AND expires_at < NOW()'
    );
    return result.affectedRows;
  }

  // Clan Wars
  async createWar(war: Omit<ClanWarParticipationRow, 'war_id' | 'created_at'>): Promise<number> {
    const result = await this.execute(
      `INSERT INTO clan_war_participation (clan_id, season_id, war_type, status)
       VALUES (?, ?, ?, ?)`,
      [war.clan_id, war.season_id, war.war_type, war.status]
    );
    return result.insertId;
  }

  async getClanWars(clanId: number, seasonId?: number): Promise<ClanWarParticipationRow[]> {
    let sql = 'SELECT * FROM clan_war_participation WHERE clan_id = ?';
    const params: any[] = [clanId];

    if (seasonId) {
      sql += ' AND season_id = ?';
      params.push(seasonId);
    }

    sql += ' ORDER BY created_at DESC';
    return this.query<ClanWarParticipationRow>(sql, params);
  }

  async recordWarAttack(attack: Omit<ClanWarAttackRow, 'attack_id' | 'created_at'>): Promise<number> {
    const result = await this.execute(
      `INSERT INTO clan_war_attacks (war_id, attacker_id, defender_id, battle_id, damage_dealt, crowns_earned, fame_earned, is_replay)
       VALUES (?, ?, ?, ?, ?, ?, ?, ?)`,
      [attack.war_id, attack.attacker_id, attack.defender_id, attack.battle_id, attack.damage_dealt, attack.crowns_earned, attack.fame_earned, attack.is_replay]
    );
    return result.insertId;
  }

  async getWarAttacks(warId: number): Promise<ClanWarAttackRow[]> {
    return this.query<ClanWarAttackRow>(
      'SELECT * FROM clan_war_attacks WHERE war_id = ? ORDER BY created_at',
      [warId]
    );
  }

  // Statistics
  async getClanStats(clanId: number): Promise<{
    totalMembers: number;
    totalDonations: number;
    avgTrophies: number;
    warWinRate: number;
  }> {
    const clan = await this.getClan(clanId);
    if (!clan) return { totalMembers: 0, totalDonations: 0, avgTrophies: 0, warWinRate: 0 };

    const members = await this.getClanMembers(clanId);
    const totalDonations = members.reduce((sum, m) => sum + m.donations, 0);
    const avgTrophies = members.length > 0 
      ? members.reduce((sum, m) => {
          // Would need to join with players table for trophies
          return sum;
        }, 0) / members.length 
      : 0;

    const wars = await this.getClanWars(clanId);
    const warWins = wars.filter(w => w.rank === 1).length;
    const warWinRate = wars.length > 0 ? warWins / wars.length : 0;

    return {
      totalMembers: clan.members_count,
      totalDonations,
      avgTrophies,
      warWinRate
    };
  }
}