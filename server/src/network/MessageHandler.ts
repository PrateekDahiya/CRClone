import { logger } from '../utils/logger';
import { NetworkClient } from './NetworkClient';
import { NetworkMessage, AuthMessage, MatchmakingRequest, InputMessage, SaveDeckRequest, HeartbeatMessage, BattleType } from '../types';
import { Matchmaker } from '../matchmaking/Matchmaker';
import { BattleServer } from '../battle/BattleServer';
import { PlayerService } from '../services/PlayerService';
import { ClanService } from '../services/ClanService';
import { ShopService } from '../services/ShopService';
import { QuestService } from '../services/QuestService';
import { SeasonService } from '../services/SeasonService';
import { TournamentService } from '../services/TournamentService';
import { ReplayService } from '../services/ReplayService';

export class MessageHandler {
  constructor(
    private connectionManager: any,
    private matchmaker: Matchmaker,
    private battles: Map<string, BattleServer>,
    private playerService: PlayerService,
    private clanService: ClanService,
    private shopService: ShopService,
    private questService: QuestService,
    private seasonService: SeasonService,
    private tournamentService: TournamentService,
    private replayService: ReplayService
  ) {}

  handle(client: NetworkClient, message: NetworkMessage): void {
    try {
      if (!this.validateMessage(message)) {
        logger.warn('Invalid message structure', { clientId: client.id, type: message.type });
        client.sendError('INVALID_MESSAGE_FORMAT');
        return;
      }

      switch (message.type) {
        case 'auth':
          this.handleAuth(client, message as AuthMessage);
          break;
        case 'matchmaking':
          this.handleMatchmaking(client, message as MatchmakingRequest);
          break;
        case 'input':
          this.handleInput(client, message as InputMessage);
          break;
        case 'input_ack':
          this.handleInputAck(client, message as any);
          break;
        case 'save_deck':
          this.handleSaveDeck(client, message as SaveDeckRequest);
          break;
        case 'heartbeat':
          this.handleHeartbeat(client);
          break;
        case 'clan':
          this.handleClanMessage(client, message as any);
          break;
        case 'shop':
          this.handleShopMessage(client, message as any);
          break;
        case 'quest':
          this.handleQuestMessage(client, message as any);
          break;
        case 'season':
          this.handleSeasonMessage(client, message as any);
          break;
        case 'tournament':
          this.handleTournamentMessage(client, message as any);
          break;
        case 'replay':
          this.handleReplayMessage(client, message as any);
          break;
        case 'player':
          this.handlePlayerMessage(client, message as any);
          break;
        default:
          logger.warn('Unknown message type', { type: message.type, clientId: client.id });
      }
    } catch (error) {
      logger.error('Error handling message', { 
        clientId: client.id, 
        type: message.type, 
        error: error instanceof Error ? error.message : String(error) 
      });
      client.sendError('MESSAGE_HANDLING_ERROR');
    }
  }

  private validateMessage(message: NetworkMessage): boolean {
    if (!message || typeof message !== 'object') return false;
    if (!message.type || typeof message.type !== 'string') return false;
    return true;
  }

  private async handleAuth(client: NetworkClient, message: AuthMessage): Promise<void> {
    try {
      const player = await this.playerService.authenticate(message.token);
      if (player) {
        client.authenticate(player);
        client.send({ type: 'auth_response', success: true, playerId: player.id });
        logger.info('Player authenticated', { playerId: player.id, username: player.username, clientId: client.id });
      } else {
        client.send({ type: 'auth_response', success: false, error: 'INVALID_TOKEN' });
      }
    } catch (error) {
      logger.error('Auth error', { error: error instanceof Error ? error.message : String(error) });
      client.send({ type: 'auth_response', success: false, error: 'AUTH_FAILED' });
    }
  }

  private handleMatchmaking(client: NetworkClient, message: MatchmakingRequest): void {
    if (!client.player) {
      client.sendError('NOT_AUTHENTICATED');
      return;
    }

    const battleType = message.battleType || BattleType.Ladder;
    const deck = client.player.activeDeck?.cardIds || [];

    if (deck.length === 0) {
      client.sendError('NO_DECK_SELECTED');
      return;
    }

    this.matchmaker.addToQueue({
      playerId: client.player.id,
      username: client.player.username,
      trophies: client.player.trophies,
      deck,
      battleType,
      joinedAt: Date.now(),
    });

    client.send({ type: 'matchmaking_started', battleType });
    logger.info('Player joined matchmaking', { playerId: client.player.id, battleType, trophies: client.player.trophies });
  }

  private handleInput(client: NetworkClient, message: InputMessage): void {
    // Reject unauthenticated senders before touching any battle state.
    if (!client.authenticated || !client.player) {
      logger.warn('Input rejected: not authenticated', { clientId: client.id });
      client.sendError('NOT_AUTHENTICATED');
      return;
    }

    if (!message.input || typeof message.input.type !== 'string') {
      logger.warn('Input rejected: malformed payload', { clientId: client.id });
      client.sendError('INVALID_INPUT');
      return;
    }

    if (!client.battleId) {
      logger.warn('Input received but client not in battle', { clientId: client.id });
      client.sendError('NOT_IN_BATTLE');
      return;
    }

    const battle = this.battles.get(client.battleId);
    if (!battle) {
      logger.warn('Battle not found for input', { battleId: client.battleId, clientId: client.id });
      client.sendError('BATTLE_NOT_FOUND');
      return;
    }

    // Reject forged battleIds: the sender must be a participant of the battle.
    if (!battle.hasPlayer(client.player.id)) {
      logger.warn('Input rejected: sender not in battle', {
        clientId: client.id,
        playerId: client.player.id,
        battleId: client.battleId,
      });
      client.sendError('NOT_IN_BATTLE');
      return;
    }

    const input = {
      type: message.input.type,
      cardId: message.input.cardId,
      spellId: message.input.spellId,
      position: message.input.position,
      targetPosition: message.input.targetPosition,
      clientTick: message.input.clientTick,
    };

    // Track the received input for the per-tick retransmit pass
    // (mirrors the index.ts queueInput path).
    if (typeof (client as any).queueInput === 'function') {
      client.queueInput(input);
    }

    const rejection = battle.handleInput(client.player.id, input);
    if (typeof rejection === 'string' && rejection.length > 0) {
      client.sendError(rejection);
    }
  }

  private handleInputAck(client: NetworkClient, message: any): void {
    if (!client.authenticated || !client.player) {
      client.sendError('NOT_AUTHENTICATED');
      return;
    }

    if (!client.battleId) {
      client.sendError('NOT_IN_BATTLE');
      return;
    }

    const battle = this.battles.get(client.battleId);
    if (!battle) {
      client.sendError('BATTLE_NOT_FOUND');
      return;
    }

    if (!battle.hasPlayer(client.player.id)) {
      logger.warn('Ack rejected: sender not in battle', {
        clientId: client.id,
        playerId: client.player.id,
        battleId: client.battleId,
      });
      client.sendError('NOT_IN_BATTLE');
      return;
    }

    const ackTick = message.ackTick ?? message.ack_tick;
    if (typeof ackTick !== 'number' || !Number.isFinite(ackTick)) {
      client.sendError('INVALID_INPUT');
      return;
    }

    battle.handleInputAck(client.player.id, ackTick);
  }

  private async handleSaveDeck(client: NetworkClient, message: SaveDeckRequest): Promise<void> {
    if (!client.player) {
      client.sendError('NOT_AUTHENTICATED');
      return;
    }

    if (!message.cardIds || message.cardIds.length !== 8) {
      client.sendError('INVALID_DECK_SIZE');
      return;
    }

    try {
      await this.playerService.saveDeck(client.player.id, message.cardIds);
      client.send({ type: 'deck_saved', success: true });
      logger.info('Deck saved', { playerId: client.player.id, cardCount: message.cardIds.length });
    } catch (error) {
      logger.error('Failed to save deck', { error: error instanceof Error ? error.message : String(error) });
      client.sendError('DECK_SAVE_FAILED');
    }
  }

  private handleHeartbeat(client: NetworkClient): void {
    client.lastHeartbeat = Date.now();
    client.send({ type: 'pong' });
  }

  // Clan messages
  private async handleClanMessage(client: NetworkClient, message: any): Promise<void> {
    if (!client.player) {
      client.sendError('NOT_AUTHENTICATED');
      return;
    }

    const action = message.action;
    const data = message.data || {};

    try {
      switch (action) {
        case 'create':
          const clan = await this.clanService.createClan(
            client.player.id,
            data.name,
            data.tag,
            data.description,
            data.type,
            data.requiredTrophies
          );
          client.send({ type: 'clan_response', action: 'create', success: true, clan });
          break;

        case 'join':
          await this.clanService.joinClan(client.player.id, data.clanId);
          client.send({ type: 'clan_response', action: 'join', success: true });
          break;

        case 'leave':
          await this.clanService.leaveClan(client.player.id);
          client.send({ type: 'clan_response', action: 'leave', success: true });
          break;

        case 'get_info':
          const clanInfo = await this.clanService.getClan(data.clanId);
          client.send({ type: 'clan_response', action: 'get_info', clan: clanInfo });
          break;

        case 'search':
          const clans = await this.clanService.searchClans(data.query, data.type, data.limit);
          client.send({ type: 'clan_response', action: 'search', clans });
          break;

        case 'get_members':
          const members = await this.clanService.getClanMembers(data.clanId);
          client.send({ type: 'clan_response', action: 'get_members', members });
          break;

        case 'promote':
          await this.clanService.promoteMember(data.clanId, data.targetPlayerId, client.player.id);
          client.send({ type: 'clan_response', action: 'promote', success: true });
          break;

        case 'demote':
          await this.clanService.demoteMember(data.clanId, data.targetPlayerId, client.player.id);
          client.send({ type: 'clan_response', action: 'demote', success: true });
          break;

        case 'kick':
          await this.clanService.kickMember(data.clanId, data.targetPlayerId, client.player.id);
          client.send({ type: 'clan_response', action: 'kick', success: true });
          break;

        case 'war_opt_in':
          await this.clanService.setWarOptIn(client.player.id, data.optIn);
          client.send({ type: 'clan_response', action: 'war_opt_in', success: true });
          break;

        case 'donate_request':
          const donation = await this.clanService.requestDonation(client.player.id, data.cardId, data.count);
          client.send({ type: 'clan_response', action: 'donate_request', donation });
          break;

        case 'get_donations':
          const donations = await this.clanService.getOpenDonations(data.clanId);
          client.send({ type: 'clan_response', action: 'get_donations', donations });
          break;

        case 'fulfill_donation':
          await this.clanService.fulfillDonation(data.requestId, client.player.id, data.count);
          client.send({ type: 'clan_response', action: 'fulfill_donation', success: true });
          break;

        case 'get_chat':
          const chat = await this.clanService.getClanChat(data.clanId, data.limit, data.beforeMessageId);
          client.send({ type: 'clan_response', action: 'get_chat', messages: chat });
          break;

        case 'send_chat':
          await this.clanService.sendClanMessage(data.clanId, client.player.id, data.messageType, data.content);
          client.send({ type: 'clan_response', action: 'send_chat', success: true });
          break;

        case 'invite':
          const invite = await this.clanService.invitePlayer(data.clanId, client.player.id, data.inviteeId);
          client.send({ type: 'clan_response', action: 'invite', invite });
          break;

        case 'get_invites':
          const invites = await this.clanService.getPlayerInvites(client.player.id);
          client.send({ type: 'clan_response', action: 'get_invites', invites });
          break;

        case 'accept_invite':
          await this.clanService.acceptInvite(data.inviteId, client.player.id);
          client.send({ type: 'clan_response', action: 'accept_invite', success: true });
          break;

        case 'decline_invite':
          await this.clanService.declineInvite(data.inviteId, client.player.id);
          client.send({ type: 'clan_response', action: 'decline_invite', success: true });
          break;

        case 'update_settings':
          await this.clanService.updateClanSettings(data.clanId, client.player.id, data.settings);
          client.send({ type: 'clan_response', action: 'update_settings', success: true });
          break;

        default:
          client.sendError('UNKNOWN_CLAN_ACTION');
      }
    } catch (error) {
      logger.error('Clan action error', { action, error: error instanceof Error ? error.message : String(error) });
      client.sendError(error instanceof Error ? error.message : 'CLAN_ACTION_FAILED');
    }
  }

  // Shop messages
  private async handleShopMessage(client: NetworkClient, message: any): Promise<void> {
    if (!client.player) {
      client.sendError('NOT_AUTHENTICATED');
      return;
    }

    const action = message.action;
    const data = message.data || {};

    try {
      switch (action) {
        case 'get_offers':
          const offers = await this.shopService.getAllOffers();
          client.send({ type: 'shop_response', action: 'get_offers', offers });
          break;

        case 'get_daily':
          const daily = await this.shopService.getDailyOffers();
          client.send({ type: 'shop_response', action: 'get_daily', offers: daily });
          break;

        case 'get_special':
          const special = await this.shopService.getSpecialOffers();
          client.send({ type: 'shop_response', action: 'get_special', offers: special });
          break;

        case 'purchase':
          const result = await this.shopService.purchaseOffer(client.player.id, data.offerId, data.currencyType);
          client.send({ type: 'shop_response', action: 'purchase', result });
          break;

        case 'purchase_iap':
          const iapResult = await this.shopService.purchaseWithIAP(
            client.player.id,
            data.offerId,
            data.platform,
            data.transactionId,
            data.receiptData
          );
          client.send({ type: 'shop_response', action: 'purchase_iap', result: iapResult });
          break;

        case 'get_history':
          const history = await this.shopService.getPlayerPurchaseHistory(client.player.id, data.limit);
          client.send({ type: 'shop_response', action: 'get_history', purchases: history });
          break;

        default:
          client.sendError('UNKNOWN_SHOP_ACTION');
      }
    } catch (error) {
      logger.error('Shop action error', { action, error: error instanceof Error ? error.message : String(error) });
      client.sendError(error instanceof Error ? error.message : 'SHOP_ACTION_FAILED');
    }
  }

  // Quest messages
  private async handleQuestMessage(client: NetworkClient, message: any): Promise<void> {
    if (!client.player) {
      client.sendError('NOT_AUTHENTICATED');
      return;
    }

    const action = message.action;
    const data = message.data || {};

    try {
      switch (action) {
        case 'get_active':
          const quests = await this.questService.getActiveQuests(data.type);
          client.send({ type: 'quest_response', action: 'get_active', quests });
          break;

        case 'get_progress':
          const progress = await this.questService.getPlayerQuests(client.player.id, data.status);
          client.send({ type: 'quest_response', action: 'get_progress', quests: progress });
          break;

        case 'claim':
          const rewards = await this.questService.claimQuestReward(client.player.id, data.questId);
          client.send({ type: 'quest_response', action: 'claim', rewards });
          break;

        default:
          client.sendError('UNKNOWN_QUEST_ACTION');
      }
    } catch (error) {
      logger.error('Quest action error', { action, error: error instanceof Error ? error.message : String(error) });
      client.sendError(error instanceof Error ? error.message : 'QUEST_ACTION_FAILED');
    }
  }

  // Season messages
  private async handleSeasonMessage(client: NetworkClient, message: any): Promise<void> {
    if (!client.player) {
      client.sendError('NOT_AUTHENTICATED');
      return;
    }

    const action = message.action;
    const data = message.data || {};

    try {
      switch (action) {
        case 'get_current':
          const season = await this.seasonService.getCurrentSeason();
          client.send({ type: 'season_response', action: 'get_current', season });
          break;

        case 'get_progress':
          const progress = await this.seasonService.getPlayerSeason(client.player.id, data.seasonId);
          client.send({ type: 'season_response', action: 'get_progress', progress });
          break;

        case 'claim_free':
          const freeRewards = await this.seasonService.claimFreeReward(client.player.id, data.tier, data.seasonId);
          client.send({ type: 'season_response', action: 'claim_free', rewards: freeRewards });
          break;

        case 'claim_paid':
          const paidRewards = await this.seasonService.claimPaidReward(client.player.id, data.tier, data.seasonId);
          client.send({ type: 'season_response', action: 'claim_paid', rewards: paidRewards });
          break;

        case 'claim_milestone':
          const milestoneRewards = await this.seasonService.claimCrownMilestone(client.player.id, data.milestoneId, data.seasonId);
          client.send({ type: 'season_response', action: 'claim_milestone', rewards: milestoneRewards });
          break;

        case 'purchase_pass':
          const purchased = await this.seasonService.purchaseBattlePass(client.player.id, data.seasonId);
          client.send({ type: 'season_response', action: 'purchase_pass', success: purchased });
          break;

        case 'leaderboard':
          const leaderboard = await this.seasonService.getSeasonLeaderboard(data.seasonId, data.limit);
          client.send({ type: 'season_response', action: 'leaderboard', leaderboard });
          break;

        default:
          client.sendError('UNKNOWN_SEASON_ACTION');
      }
    } catch (error) {
      logger.error('Season action error', { action, error: error instanceof Error ? error.message : String(error) });
      client.sendError(error instanceof Error ? error.message : 'SEASON_ACTION_FAILED');
    }
  }

  // Tournament messages
  private async handleTournamentMessage(client: NetworkClient, message: any): Promise<void> {
    if (!client.player) {
      client.sendError('NOT_AUTHENTICATED');
      return;
    }

    const action = message.action;
    const data = message.data || {};

    try {
      switch (action) {
        case 'get_active':
          const tournaments = await this.tournamentService.getActiveTournaments();
          client.send({ type: 'tournament_response', action: 'get_active', tournaments });
          break;

        case 'get_upcoming':
          const upcoming = await this.tournamentService.getUpcomingTournaments(data.limit);
          client.send({ type: 'tournament_response', action: 'get_upcoming', tournaments: upcoming });
          break;

        case 'get_info':
          const tournament = await this.tournamentService.getTournament(data.tournamentId);
          client.send({ type: 'tournament_response', action: 'get_info', tournament });
          break;

        case 'enter':
          const entry = await this.tournamentService.enterTournament(client.player.id, data.tournamentId, data.deckId);
          client.send({ type: 'tournament_response', action: 'enter', entry });
          break;

        case 'get_entry':
          const playerEntry = await this.tournamentService.getPlayerEntry(data.tournamentId, client.player.id);
          client.send({ type: 'tournament_response', action: 'get_entry', entry: playerEntry });
          break;

        case 'draft_pick':
          await this.tournamentService.makeDraftPick(data.entryId, data.cardId);
          client.send({ type: 'tournament_response', action: 'draft_pick', success: true });
          break;

        case 'finalize_draft':
          const deckId = await this.tournamentService.finalizeDraft(data.entryId);
          client.send({ type: 'tournament_response', action: 'finalize_draft', deckId });
          break;

        case 'retire':
          await this.tournamentService.retireFromTournament(data.entryId);
          client.send({ type: 'tournament_response', action: 'retire', success: true });
          break;

        case 'history':
          const history = await this.tournamentService.getPlayerTournamentHistory(client.player.id);
          client.send({ type: 'tournament_response', action: 'history', tournaments: history });
          break;

        default:
          client.sendError('UNKNOWN_TOURNAMENT_ACTION');
      }
    } catch (error) {
      logger.error('Tournament action error', { action, error: error instanceof Error ? error.message : String(error) });
      client.sendError(error instanceof Error ? error.message : 'TOURNAMENT_ACTION_FAILED');
    }
  }

  // Replay messages
  private async handleReplayMessage(client: NetworkClient, message: any): Promise<void> {
    if (!client.player) {
      client.sendError('NOT_AUTHENTICATED');
      return;
    }

    const action = message.action;
    const data = message.data || {};

    try {
      switch (action) {
        case 'get_replay':
          const replay = await this.replayService.getReplayData(data.replayId);
          client.send({ type: 'replay_response', action: 'get_replay', replay });
          break;

        case 'get_metadata':
          const metadata = await this.replayService.getReplayMetadata(data.replayId);
          client.send({ type: 'replay_response', action: 'get_metadata', metadata });
          break;

        case 'get_history':
          const replays = await this.replayService.getPlayerReplays(client.player.id, data.limit, data.offset);
          client.send({ type: 'replay_response', action: 'get_history', replays });
          break;

        case 'search':
          const results = await this.replayService.searchReplays(data.filters);
          client.send({ type: 'replay_response', action: 'search', replays: results });
          break;

        default:
          client.sendError('UNKNOWN_REPLAY_ACTION');
      }
    } catch (error) {
      logger.error('Replay action error', { action, error: error instanceof Error ? error.message : String(error) });
      client.sendError(error instanceof Error ? error.message : 'REPLAY_ACTION_FAILED');
    }
  }

  // Player messages
  private async handlePlayerMessage(client: NetworkClient, message: any): Promise<void> {
    if (!client.player) {
      client.sendError('NOT_AUTHENTICATED');
      return;
    }

    const action = message.action;
    const data = message.data || {};

    try {
      switch (action) {
        case 'get_profile':
          const player = await this.playerService.getPlayerById(client.player.id);
          client.send({ type: 'player_response', action: 'get_profile', player });
          break;

        case 'update_profile':
          await this.playerService.updateProfile(client.player.id, data);
          client.send({ type: 'player_response', action: 'update_profile', success: true });
          break;

        case 'get_settings':
          const settings = await this.playerService.getSettings(client.player.id);
          client.send({ type: 'player_response', action: 'get_settings', settings });
          break;

        case 'update_settings':
          await this.playerService.updateSettings(client.player.id, data);
          client.send({ type: 'player_response', action: 'update_settings', success: true });
          break;

        case 'get_battle_history':
          const history = await this.playerService.getBattleHistory(client.player.id, data.limit);
          client.send({ type: 'player_response', action: 'get_battle_history', battles: history });
          break;

        case 'get_leaderboard':
          const leaderboard = await this.playerService.getLeaderboard(data.limit);
          client.send({ type: 'player_response', action: 'get_leaderboard', leaderboard });
          break;

        case 'get_rank':
          const rank = await this.playerService.getPlayerRank(client.player.id);
          client.send({ type: 'player_response', action: 'get_rank', rank });
          break;

        case 'search_players':
          const players = await this.playerService.searchPlayers(data.query, data.limit);
          client.send({ type: 'player_response', action: 'search_players', players });
          break;

        case 'get_decks':
          const decks = await this.playerService.getAllDecks(client.player.id);
          client.send({ type: 'player_response', action: 'get_decks', decks });
          break;

        case 'set_active_deck':
          await this.playerService.setActiveDeck(client.player.id, data.deckId);
          client.send({ type: 'player_response', action: 'set_active_deck', success: true });
          break;

        case 'delete_deck':
          await this.playerService.deleteDeck(client.player.id, data.deckId);
          client.send({ type: 'player_response', action: 'delete_deck', success: true });
          break;

        case 'get_collection':
          const collection = await this.playerService.getCollection(client.player.id);
          const collectionArray = Array.from(collection.entries()).map(([cid, v]) => ({ cardId: cid, count: v.count, level: v.level, upgradeProgress: v.upgradeProgress }));
          client.send({ type: 'player_response', action: 'get_collection', collection: collectionArray });
          break;

        default:
          client.sendError('UNKNOWN_PLAYER_ACTION');
      }
    } catch (error) {
      logger.error('Player action error', { action, error: error instanceof Error ? error.message : String(error) });
      client.sendError(error instanceof Error ? error.message : 'PLAYER_ACTION_FAILED');
    }
  }
}