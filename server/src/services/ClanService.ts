import { Database, db } from '../persistence/Database';
import { logger } from '../utils/logger';
import { v4 as uuidv4 } from 'uuid';
import { ClanRepository, ClanRow, ClanMemberRow, ClanChatRow, ClanDonationRow, ClanDonationFillRow, ClanInviteRow } from '../persistence/ClanRepository';
import { PlayerService } from './PlayerService';

export interface Clan {
  clanId: number;
  name: string;
  tag: string;
  description: string;
  type: 'open' | 'invite_only' | 'closed';
  requiredTrophies: number;
  requiredLevel: number;
  warFrequency: 'always' | 'weekly' | 'never';
  trophies: number;
  membersCount: number;
  maxMembers: number;
  warWins: number;
  warLosses: number;
  capitalLevel: number;
  badgeId: number;
  createdAt: Date;
}

export interface ClanMember {
  clanId: number;
  playerId: string;
  username: string;
  role: 'leader' | 'co_leader' | 'elder' | 'member';
  trophies: number;
  donations: number;
  donationsReceived: number;
  weeklyDonations: number;
  warOptIn: boolean;
  warAttacksUsed: number;
  warBestAttack: number;
  joinedAt: Date;
}

export interface ClanChatMessage {
  messageId: number;
  clanId: number;
  playerId: string;
  username: string;
  messageType: 'text' | 'replay' | 'donation_request' | 'donation' | 'system';
  content: string;
  createdAt: Date;
}

export interface DonationRequest {
  requestId: number;
  clanId: number;
  playerId: string;
  username: string;
  cardId: number;
  cardName: string;
  countRequested: number;
  countFilled: number;
  status: 'open' | 'filled' | 'expired' | 'cancelled';
  createdAt: Date;
  expiresAt: Date;
  fills: DonationFill[];
}

export interface DonationFill {
  fillId: number;
  requestId: number;
  donorId: string;
  donorName: string;
  countFilled: number;
  filledAt: Date;
}

export interface ClanInvite {
  inviteId: number;
  clanId: number;
  clanName: string;
  clanTag: string;
  inviterId: string;
  inviterName: string;
  inviteeId: string;
  status: 'pending' | 'accepted' | 'declined' | 'expired';
  createdAt: Date;
  expiresAt: Date;
}

export class ClanService {
  private db: Database;
  private clanRepo: ClanRepository;
  private playerService: PlayerService;

  constructor(database: Database = db, playerService?: PlayerService) {
    this.db = database;
    this.clanRepo = new ClanRepository(database);
    this.playerService = playerService || new PlayerService(database);
  }

  private mapClanRow(row: ClanRow): Clan {
    return {
      clanId: row.clan_id,
      name: row.name,
      tag: row.tag,
      description: row.description || '',
      type: row.type as 'open' | 'invite_only' | 'closed',
      requiredTrophies: row.required_trophies,
      requiredLevel: row.required_level,
      warFrequency: row.war_frequency as 'always' | 'weekly' | 'never',
      trophies: row.trophies,
      membersCount: row.members_count,
      maxMembers: row.max_members,
      warWins: row.war_wins,
      warLosses: row.war_losses,
      capitalLevel: row.capital_level,
      badgeId: row.badge_id,
      createdAt: row.created_at
    };
  }

  async createClan(playerId: string, name: string, tag: string, description: string, type: 'open' | 'invite_only' | 'closed' = 'invite_only', requiredTrophies: number = 0): Promise<Clan> {
    const playerClan = await this.clanRepo.getPlayerClan(playerId);
    if (playerClan) throw new Error('PLAYER_ALREADY_IN_CLAN');

    const existingTag = await this.clanRepo.getClanByTag(tag);
    if (existingTag) throw new Error('CLAN_TAG_TAKEN');

    const existingName = await this.clanRepo.searchClans(name);
    if (existingName.some(c => c.name.toLowerCase() === name.toLowerCase())) {
      throw new Error('CLAN_NAME_TAKEN');
    }

    const clanId = await this.clanRepo.createClan({
      name,
      tag,
      description,
      description_key: description.replace(/\s+/g, '_').toLowerCase(),
      type,
      required_trophies: requiredTrophies,
      required_level: 1,
      war_frequency: 'weekly',
      trophies: 0,
      members_count: 1,
      max_members: 50,
      war_wins: 0,
      war_losses: 0,
      capital_level: 1,
      badge_id: 1
    });

    await this.clanRepo.joinClan(clanId, playerId, 'leader');

    const clan = await this.getClan(clanId);
    if (!clan) throw new Error('CLAN_CREATION_FAILED');
    return clan;
  }

  async getClan(clanId: number): Promise<Clan | null> {
    const clanRow = await this.clanRepo.getClan(clanId);
    if (!clanRow) return null;
    return this.mapClanRow(clanRow);
  }

  async getClanByTag(tag: string): Promise<Clan | null> {
    const clanRow = await this.clanRepo.getClanByTag(tag);
    if (!clanRow) return null;
    return this.mapClanRow(clanRow);
  }

  async updateClanSettings(clanId: number, playerId: string, settings: {
    description?: string;
    type?: 'open' | 'invite_only' | 'closed';
    requiredTrophies?: number;
    requiredLevel?: number;
    warFrequency?: 'always' | 'weekly' | 'never';
    badgeId?: number;
  }): Promise<void> {
    const member = await this.clanRepo.getClanMember(clanId, playerId);
    if (!member || !['leader', 'co_leader'].includes(member.role)) {
      throw new Error('INSUFFICIENT_PERMISSIONS');
    }
    await this.clanRepo.updateClan(clanId, settings as any);
  }

  async disbandClan(clanId: number, playerId: string): Promise<void> {
    const member = await this.clanRepo.getClanMember(clanId, playerId);
    if (!member || member.role !== 'leader') throw new Error('ONLY_LEADER_CAN_DISBAND');
    await this.clanRepo.deleteClan(clanId);
  }

  async searchClans(query: string, type?: 'open' | 'invite_only' | 'closed', limit: number = 20): Promise<Clan[]> {
    const clans = await this.clanRepo.searchClans(query, type, limit);
    return clans.map(c => this.mapClanRow(c));
  }

  async getTopClans(limit: number = 100): Promise<Clan[]> {
    const clans = await this.clanRepo.getTopClans(limit);
    return clans.map(c => this.mapClanRow(c));
  }

  async getClanMembers(clanId: number): Promise<ClanMember[]> {
    const members = await this.clanRepo.getClanMembers(clanId);
    const result: ClanMember[] = [];
    for (const member of members) {
      const player = await this.playerService.getPlayerById(member.player_id);
      if (player) {
        result.push({
          clanId: member.clan_id,
          playerId: member.player_id,
          username: player.username,
          role: member.role as 'leader' | 'co_leader' | 'elder' | 'member',
          trophies: player.trophies,
          donations: member.donations,
          donationsReceived: member.donations_received,
          weeklyDonations: member.weekly_donations,
          warOptIn: member.war_opt_in,
          warAttacksUsed: member.war_attacks_used,
          warBestAttack: member.war_best_attack,
          joinedAt: member.joined_at
        });
      }
    }
    return result;
  }

  async getClanMember(clanId: number, playerId: string): Promise<ClanMember | null> {
    const member = await this.clanRepo.getClanMember(clanId, playerId);
    if (!member) return null;
    const player = await this.playerService.getPlayerById(member.player_id);
    if (!player) return null;
    return {
      clanId: member.clan_id,
      playerId: member.player_id,
      username: player.username,
      role: member.role as 'leader' | 'co_leader' | 'elder' | 'member',
      trophies: player.trophies,
      donations: member.donations,
      donationsReceived: member.donations_received,
      weeklyDonations: member.weekly_donations,
      warOptIn: member.war_opt_in,
      warAttacksUsed: member.war_attacks_used,
      warBestAttack: member.war_best_attack,
      joinedAt: member.joined_at
    };
  }

  async joinClan(playerId: string, clanId: number): Promise<void> {
    const clan = await this.getClan(clanId);
    if (!clan) throw new Error('CLAN_NOT_FOUND');
    const playerClan = await this.clanRepo.getPlayerClan(playerId);
    if (playerClan) throw new Error('PLAYER_ALREADY_IN_CLAN');
    const player = await this.playerService.getPlayerById(playerId);
    if (!player) throw new Error('PLAYER_NOT_FOUND');
    if (player.trophies < clan.requiredTrophies) throw new Error('INSUFFICIENT_TROPHIES');
    if (player.level < clan.requiredLevel) throw new Error('INSUFFICIENT_LEVEL');
    if (clan.membersCount >= clan.maxMembers) throw new Error('CLAN_FULL');
    if (clan.type === 'closed') throw new Error('CLAN_CLOSED');
    if (clan.type === 'invite_only') {
      const invites = await this.clanRepo.getPlayerInvites(playerId);
      const hasInvite = invites.some(i => i.clan_id === clanId && i.status === 'pending');
      if (!hasInvite) throw new Error('INVITE_REQUIRED');
    }
    await this.clanRepo.joinClan(clanId, playerId, 'member');
    await this.clanRepo.sendClanMessage(clanId, playerId, 'system', `${player.username} joined the clan!`);
  }

  async leaveClan(playerId: string): Promise<void> {
    const playerClan = await this.clanRepo.getPlayerClan(playerId);
    if (!playerClan) throw new Error('NOT_IN_CLAN');
    const player = await this.playerService.getPlayerById(playerId);
    if (!player) throw new Error('PLAYER_NOT_FOUND');
    if (playerClan.member.role === 'leader') {
      const members = await this.clanRepo.getClanMembers(playerClan.clan.clan_id);
      const otherMembers = members.filter(m => m.player_id !== playerId);
      if (otherMembers.length > 0) {
        const newLeader = otherMembers.find(m => m.role === 'co_leader') || otherMembers.find(m => m.role === 'elder') || otherMembers[0];
        await this.promoteMember(playerClan.clan.clan_id, newLeader.player_id, playerId);
      } else {
        await this.disbandClan(playerClan.clan.clan_id, playerId);
        return;
      }
    }
    await this.clanRepo.leaveClan(playerClan.clan.clan_id, playerId);
    await this.clanRepo.sendClanMessage(playerClan.clan.clan_id, playerId, 'system', `${player.username} left the clan.`);
  }

  async promoteMember(clanId: number, targetPlayerId: string, requesterId: string): Promise<void> {
    const requester = await this.clanRepo.getClanMember(clanId, requesterId);
    if (!requester || !['leader', 'co_leader'].includes(requester.role)) throw new Error('INSUFFICIENT_PERMISSIONS');
    const target = await this.clanRepo.getClanMember(clanId, targetPlayerId);
    if (!target) throw new Error('MEMBER_NOT_FOUND');
    const roles = ['member', 'elder', 'co_leader', 'leader'];
    const requesterRank = roles.indexOf(requester.role);
    const targetRank = roles.indexOf(target.role);
    if (targetRank >= requesterRank) throw new Error('CANNOT_PROMOTE_SAME_OR_HIGHER_RANK');
    await this.clanRepo.updateMemberRole(clanId, targetPlayerId, roles[targetRank + 1]);
  }

  async demoteMember(clanId: number, targetPlayerId: string, requesterId: string): Promise<void> {
    const requester = await this.clanRepo.getClanMember(clanId, requesterId);
    if (!requester || !['leader', 'co_leader'].includes(requester.role)) throw new Error('INSUFFICIENT_PERMISSIONS');
    const target = await this.clanRepo.getClanMember(clanId, targetPlayerId);
    if (!target) throw new Error('MEMBER_NOT_FOUND');
    if (target.role === 'leader') throw new Error('CANNOT_DEMOTE_LEADER');
    const roles = ['member', 'elder', 'co_leader', 'leader'];
    const requesterRank = roles.indexOf(requester.role);
    const targetRank = roles.indexOf(target.role);
    if (targetRank >= requesterRank) throw new Error('CANNOT_DEMOTE_SAME_OR_HIGHER_RANK');
    await this.clanRepo.updateMemberRole(clanId, targetPlayerId, roles[targetRank - 1]);
  }

  async kickMember(clanId: number, targetPlayerId: string, requesterId: string): Promise<void> {
    const requester = await this.clanRepo.getClanMember(clanId, requesterId);
    if (!requester || !['leader', 'co_leader', 'elder'].includes(requester.role)) throw new Error('INSUFFICIENT_PERMISSIONS');
    const target = await this.clanRepo.getClanMember(clanId, targetPlayerId);
    if (!target) throw new Error('MEMBER_NOT_FOUND');
    if (target.role === 'leader') throw new Error('CANNOT_KICK_LEADER');
    const roles = ['member', 'elder', 'co_leader', 'leader'];
    const requesterRank = roles.indexOf(requester.role);
    const targetRank = roles.indexOf(target.role);
    if (targetRank >= requesterRank) throw new Error('CANNOT_KICK_SAME_OR_HIGHER_RANK');
    await this.clanRepo.kickMember(clanId, targetPlayerId);
  }

  async setWarOptIn(playerId: string, optIn: boolean): Promise<void> {
    const playerClan = await this.clanRepo.getPlayerClan(playerId);
    if (!playerClan) throw new Error('NOT_IN_CLAN');
    await this.clanRepo.updateWarOptIn(playerClan.clan.clan_id, playerId, optIn);
  }

  async requestDonation(playerId: string, cardId: number, count: number): Promise<DonationRequest> {
    const playerClan = await this.clanRepo.getPlayerClan(playerId);
    if (!playerClan) throw new Error('NOT_IN_CLAN');
    const limits = await this.getDonationLimits(cardId);
    if (count > limits.maxPerRequest) throw new Error(`MAX_${limits.maxPerRequest}_PER_REQUEST`);
    const expiresAt = new Date();
    expiresAt.setHours(expiresAt.getHours() + 24);
    const requestId = await this.clanRepo.createDonationRequest({
      clan_id: playerClan.clan.clan_id,
      player_id: playerId,
      card_id: cardId,
      count_requested: count,
      expires_at: expiresAt
    });
    const player = await this.playerService.getPlayerById(playerId);
    await this.clanRepo.sendClanMessage(playerClan.clan.clan_id, playerId, 'donation_request', JSON.stringify({ cardId, count, requester: player?.username }));
    return this.getDonationRequest(requestId) as Promise<DonationRequest>;
  }

  async getDonationRequest(requestId: number): Promise<DonationRequest | null> {
    const request = await this.clanRepo.getDonationRequest(requestId);
    if (!request) return null;
    const player = await this.playerService.getPlayerById(request.player_id);
    const fills = await this.clanRepo.getDonationFills(requestId);
    return {
      requestId: request.request_id,
      clanId: request.clan_id,
      playerId: request.player_id,
      username: player?.username || 'Unknown',
      cardId: request.card_id,
      cardName: '',
      countRequested: request.count_requested,
      countFilled: request.count_filled,
      status: request.status as 'open' | 'filled' | 'expired' | 'cancelled',
      createdAt: request.created_at,
      expiresAt: request.expires_at,
      fills: fills.map(f => ({
        fillId: f.fill_id,
        requestId: f.request_id,
        donorId: f.donor_id,
        donorName: '',
        countFilled: f.count_filled,
        filledAt: f.filled_at
      }))
    };
  }

  async getOpenDonations(clanId: number): Promise<DonationRequest[]> {
    const requests = await this.clanRepo.getOpenDonations(clanId);
    const result: DonationRequest[] = [];
    for (const request of requests) {
      const player = await this.playerService.getPlayerById(request.player_id);
      const fills = await this.clanRepo.getDonationFills(request.request_id);
      result.push({
        requestId: request.request_id,
        clanId: request.clan_id,
        playerId: request.player_id,
        username: player?.username || 'Unknown',
        cardId: request.card_id,
        cardName: '',
        countRequested: request.count_requested,
        countFilled: request.count_filled,
        status: request.status as 'open' | 'filled' | 'expired' | 'cancelled',
        createdAt: request.created_at,
        expiresAt: request.expires_at,
        fills: fills.map(f => ({
          fillId: f.fill_id,
          requestId: f.request_id,
          donorId: f.donor_id,
          donorName: '',
          countFilled: f.count_filled,
          filledAt: f.filled_at
        }))
      });
    }
    return result;
  }

  async fulfillDonation(requestId: number, donorId: string, count: number): Promise<void> {
    const request = await this.clanRepo.getDonationRequest(requestId);
    if (!request) throw new Error('DONATION_REQUEST_NOT_FOUND');
    if (request.status !== 'open') throw new Error('REQUEST_NOT_OPEN');
    if (request.count_filled + count > request.count_requested) throw new Error('EXCEEDS_REQUESTED_COUNT');
    const donor = await this.playerService.getPlayerById(donorId);
    if (!donor) throw new Error('DONOR_NOT_FOUND');
    const donorCollection = await this.playerService.getCollection(donorId);
    const cardEntry = donorCollection.get(request.card_id);
    if (!cardEntry || cardEntry.count < count) throw new Error('INSUFFICIENT_CARDS');
    const donorClan = await this.clanRepo.getPlayerClan(donorId);
    if (!donorClan || donorClan.clan.clan_id !== request.clan_id) throw new Error('NOT_IN_SAME_CLAN');
    await this.clanRepo.fillDonation(requestId, donorId, count);
    await this.playerService.addCards(donorId, request.card_id, -count);
    const requester = await this.playerService.getPlayerById(request.player_id);
    if (requester) await this.playerService.addCards(request.player_id, request.card_id, count);
    await this.clanRepo.incrementDonations(request.clan_id, donorId, count);
    await this.clanRepo.incrementDonationsReceived(request.clan_id, request.player_id, count);
    await this.giveDonationRewards(donorId, request.card_id, count);
    await this.clanRepo.sendClanMessage(request.clan_id, donorId, 'donation', JSON.stringify({ cardId: request.card_id, count, recipient: request.player_id }));
  }

  async cancelDonationRequest(requestId: number, playerId: string): Promise<void> {
    await this.clanRepo.cancelDonation(requestId, playerId);
  }

  private async getDonationLimits(cardId: number): Promise<{maxPerRequest: number, maxPerWeek: number}> {
    return { maxPerRequest: 10, maxPerWeek: 100 };
  }

  private async giveDonationRewards(donorId: string, cardId: number, count: number): Promise<void> {
    const configRows = await this.db.query('SELECT config_value FROM game_config WHERE config_key = "donation_rewards"');
    if (!configRows[0]) return;
    const rewards = configRows[0].config_value;
    const rarity = this.getCardRarity(cardId);
    const reward = rewards[rarity];
    if (reward) {
      await this.playerService.addGold(donorId, reward.gold * count);
      await this.playerService.addExperience(donorId, reward.xp * count);
    }
  }

  private getCardRarity(cardId: number): string {
    if (cardId >= 27000000) return 'champion';
    if (cardId >= 26000100) return 'legendary';
    if (cardId >= 26000050) return 'epic';
    if (cardId >= 26000020) return 'rare';
    return 'common';
  }

  async getClanChat(clanId: number, limit: number = 50, beforeMessageId?: number): Promise<ClanChatMessage[]> {
    const messages = await this.clanRepo.getClanChat(clanId, limit, beforeMessageId);
    const result: ClanChatMessage[] = [];
    for (const msg of messages) {
      const player = await this.playerService.getPlayerById(msg.player_id);
      result.push({
        messageId: msg.message_id,
        clanId: msg.clan_id,
        playerId: msg.player_id,
        username: player?.username || 'Unknown',
        messageType: msg.message_type as 'text' | 'replay' | 'donation_request' | 'donation' | 'system',
        content: msg.content,
        createdAt: msg.created_at
      });
    }
    return result;
  }

  async sendClanMessage(clanId: number, playerId: string, messageType: 'text' | 'replay' | 'donation_request' | 'donation' | 'system', content: string): Promise<number> {
    const member = await this.clanRepo.getClanMember(clanId, playerId);
    if (!member && messageType !== 'system') throw new Error('NOT_IN_CLAN');
    return this.clanRepo.sendClanMessage(clanId, playerId, messageType, content);
  }

  async invitePlayer(clanId: number, inviterId: string, inviteeId: string): Promise<ClanInvite> {
    const inviter = await this.clanRepo.getClanMember(clanId, inviterId);
    if (!inviter || !['leader', 'co_leader', 'elder'].includes(inviter.role)) throw new Error('INSUFFICIENT_PERMISSIONS');
    const invitee = await this.playerService.getPlayerById(inviteeId);
    if (!invitee) throw new Error('PLAYER_NOT_FOUND');
    const inviteeClan = await this.clanRepo.getPlayerClan(inviteeId);
    if (inviteeClan) throw new Error('PLAYER_ALREADY_IN_CLAN');
    const clan = await this.getClan(clanId);
    if (!clan) throw new Error('CLAN_NOT_FOUND');
    const expiresAt = new Date();
    expiresAt.setDate(expiresAt.getDate() + 7);
    const inviteId = await this.clanRepo.createInvite({
      clan_id: clanId,
      inviter_id: inviterId,
      invitee_id: inviteeId,
      status: 'pending',
      expires_at: expiresAt
    });
    return this.getInvite(inviteId) as Promise<ClanInvite>;
  }

  async getInvite(inviteId: number): Promise<ClanInvite | null> {
    const invite = await this.clanRepo.getInvite(inviteId);
    if (!invite) return null;
    const clan = await this.getClan(invite.clan_id);
    const inviter = await this.playerService.getPlayerById(invite.inviter_id);
    const invitee = await this.playerService.getPlayerById(invite.invitee_id);
    return {
      inviteId: invite.invite_id,
      clanId: invite.clan_id,
      clanName: clan?.name || '',
      clanTag: clan?.tag || '',
      inviterId: invite.inviter_id,
      inviterName: inviter?.username || '',
      inviteeId: invite.invitee_id,
      status: invite.status as 'pending' | 'accepted' | 'declined' | 'expired',
      createdAt: invite.created_at,
      expiresAt: invite.expires_at
    };
  }

  async getPlayerInvites(playerId: string): Promise<ClanInvite[]> {
    const invites = await this.clanRepo.getPlayerInvites(playerId);
    const result: ClanInvite[] = [];
    for (const invite of invites) {
      const clan = await this.getClan(invite.clan_id);
      const inviter = await this.playerService.getPlayerById(invite.inviter_id);
      result.push({
        inviteId: invite.invite_id,
        clanId: invite.clan_id,
        clanName: clan?.name || '',
        clanTag: clan?.tag || '',
        inviterId: invite.inviter_id,
        inviterName: inviter?.username || '',
        inviteeId: invite.invitee_id,
        status: invite.status as 'pending' | 'accepted' | 'declined' | 'expired',
        createdAt: invite.created_at,
        expiresAt: invite.expires_at
      });
    }
    return result;
  }

  async acceptInvite(inviteId: number, playerId: string): Promise<void> {
    const invite = await this.clanRepo.getInvite(inviteId);
    if (!invite) throw new Error('INVITE_NOT_FOUND');
    if (invite.invitee_id !== playerId) throw new Error('NOT_YOUR_INVITE');
    if (invite.status !== 'pending') throw new Error('INVITE_NOT_PENDING');
    if (invite.expires_at < new Date()) throw new Error('INVITE_EXPIRED');
    const clan = await this.getClan(invite.clan_id);
    if (!clan) throw new Error('CLAN_NOT_FOUND');
    if (clan.membersCount >= clan.maxMembers) throw new Error('CLAN_FULL');
    const player = await this.playerService.getPlayerById(playerId);
    if (!player) throw new Error('PLAYER_NOT_FOUND');
    if (player.trophies < clan.requiredTrophies) throw new Error('INSUFFICIENT_TROPHIES');
    await this.clanRepo.acceptInvite(inviteId);
    await this.joinClan(playerId, invite.clan_id);
  }

  async declineInvite(inviteId: number, playerId: string): Promise<void> {
    const invite = await this.clanRepo.getInvite(inviteId);
    if (!invite) throw new Error('INVITE_NOT_FOUND');
    if (invite.invitee_id !== playerId) throw new Error('NOT_YOUR_INVITE');
    await this.clanRepo.declineInvite(inviteId);
  }

  async getClanWarStatus(clanId: number): Promise<any> {
    return { status: 'not_implemented', message: 'Clan wars will be implemented in Phase 3' };
  }

  async expireDonations(): Promise<number> {
    return this.clanRepo.expireDonations();
  }

  async expireInvites(): Promise<number> {
    return this.clanRepo.expireInvites();
  }

  async resetWeeklyDonations(): Promise<void> {
    await this.clanRepo.resetWeeklyDonations();
  }
}