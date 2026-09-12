import * as fs from 'fs';
import * as path from 'path';
import {
  loadProtocol,
  getProtocolRoot,
  MessageType,
  TYPE_TO_PROTO,
  getAllMessageTypes,
  verifyMessageType,
  encodeMessage,
  decodeMessage,
  ProtoMessage,
} from '../../src/network/Protocol';

const CS_PATH = path.resolve(__dirname, '..', '..', '..', 'Assets', 'Scripts', 'Network', 'MessageTypes.cs');
const PROTO_PATH = path.resolve(__dirname, '..', '..', 'src', 'network', 'protocol.proto');

function readFile(p: string): string {
  return fs.readFileSync(p, 'utf-8');
}

function extractCsConstants(cs: string): Map<string, string> {
  // Matches: public const string Auth = "auth";
  const re = /public const string (\w+)\s*=\s*"([^"]+)"\s*;/g;
  const out = new Map<string, string>();
  let m: RegExpExecArray | null;
  while ((m = re.exec(cs)) !== null) {
    out.set(m[1], m[2]);
  }
  return out;
}

function extractCsClasses(cs: string): string[] {
  // Matches: public class ClanMessage : NetworkMessage
  const re = /public class (\w+)\s*:\s*NetworkMessage/g;
  const out: string[] = [];
  let m: RegExpExecArray | null;
  while ((m = re.exec(cs)) !== null) {
    out.push(m[1]);
  }
  return out;
}

function extractProtoMessages(proto: string): Map<string, string> {
  // Matches: message ClanMessage { ... } (non-nested; file has no nesting)
  const re = /message\s+(\w+)\s*\{([\s\S]*?)\n\}/g;
  const out = new Map<string, string>();
  let m: RegExpExecArray | null;
  while ((m = re.exec(proto)) !== null) {
    out.set(m[1], m[2]);
  }
  return out;
}

describe('protocol wire contract (Agent2 deliverable 2.3)', () => {
  const cs = readFile(CS_PATH);
  const proto = readFile(PROTO_PATH);
  const csConstants = extractCsConstants(cs);
  const csClasses = extractCsClasses(cs);
  const protoMessages = extractProtoMessages(proto);
  const tsValues = new Set<string>(Object.values(MessageType));

  test('C# MessageTypes constants file exists and is non-empty', () => {
    expect(cs.length).toBeGreaterThan(0);
    expect(csConstants.size).toBeGreaterThanOrEqual(30);
  });

  test('every C# MessageTypes constant has a matching TS MessageType value', () => {
    const missing: string[] = [];
    for (const [, value] of csConstants) {
      if (!tsValues.has(value)) missing.push(value);
    }
    expect(missing).toEqual([]);
  });

  test('every TS MessageType value has a matching C# MessageTypes constant', () => {
    const csValues = new Set(csConstants.values());
    const missing: string[] = [];
    for (const v of getAllMessageTypes()) {
      if (!csValues.has(v)) missing.push(v);
    }
    expect(missing).toEqual([]);
  });

  test('every TS MessageType has a proto message via TYPE_TO_PROTO', () => {
    const missing: string[] = [];
    for (const v of getAllMessageTypes()) {
      const protoName = (TYPE_TO_PROTO as Record<string, string>)[v];
      if (!protoName || !protoMessages.has(protoName)) missing.push(`${v} -> ${protoName}`);
    }
    expect(missing).toEqual([]);
  });

  test('required deliverable 2.3 types are all present on all three sides', () => {
    const required = [
      'clan', 'clan_response',
      'shop', 'shop_response',
      'quest', 'quest_response',
      'season', 'season_response',
      'tournament', 'tournament_response',
      'replay', 'replay_response',
      'player', 'player_response',
      'battle_start', 'save_deck', 'deck_saved',
    ];
    const csValues = new Set(csConstants.values());
    for (const t of required) {
      expect(csValues.has(t)).toBe(true);
      expect(tsValues.has(t)).toBe(true);
      const protoName = (TYPE_TO_PROTO as Record<string, string>)[t];
      expect(protoName).toBeDefined();
      expect(protoMessages.has(protoName)).toBe(true);
    }
  });

  test('C# domain message classes exist for every required pair', () => {
    const requiredClasses = [
      'ClanMessage', 'ClanResponseMessage',
      'ShopMessage', 'ShopResponseMessage',
      'QuestMessage', 'QuestResponseMessage',
      'SeasonMessage', 'SeasonResponseMessage',
      'TournamentMessage', 'TournamentResponseMessage',
      'ReplayMessage', 'ReplayResponseMessage',
      'PlayerMessage', 'PlayerResponseMessage',
      'BattleStartMessage', 'SaveDeckRequest', 'DeckSavedMessage',
    ];
    for (const c of requiredClasses) {
      expect(csClasses).toContain(c);
    }
  });

  test('C# classes set the correct wire type string in their ctor', () => {
    const pairs: Array<[string, string]> = [
      ['ClanMessage', 'MessageTypes.Clan'],
      ['ClanResponseMessage', 'MessageTypes.ClanResponse'],
      ['ShopMessage', 'MessageTypes.Shop'],
      ['ShopResponseMessage', 'MessageTypes.ShopResponse'],
      ['QuestMessage', 'MessageTypes.Quest'],
      ['QuestResponseMessage', 'MessageTypes.QuestResponse'],
      ['SeasonMessage', 'MessageTypes.Season'],
      ['SeasonResponseMessage', 'MessageTypes.SeasonResponse'],
      ['TournamentMessage', 'MessageTypes.Tournament'],
      ['TournamentResponseMessage', 'MessageTypes.TournamentResponse'],
      ['ReplayMessage', 'MessageTypes.Replay'],
      ['ReplayResponseMessage', 'MessageTypes.ReplayResponse'],
      ['PlayerMessage', 'MessageTypes.Player'],
      ['PlayerResponseMessage', 'MessageTypes.PlayerResponse'],
      ['BattleStartMessage', '"battle_start"'],
      ['SaveDeckRequest', '"save_deck"'],
      ['DeckSavedMessage', '"deck_saved"'],
    ];
    for (const [cls, typeRef] of pairs) {
      const clsIdx = cs.indexOf(`class ${cls} : NetworkMessage`);
      expect(clsIdx).toBeGreaterThan(-1);
      const ctorWindow = cs.slice(clsIdx, clsIdx + 600);
      expect(ctorWindow).toContain(`type = ${typeRef}`);
    }
  });

  test('proto field numbers match C# [ProtoMember(N)] numbers', () => {
    const checks: Array<[string, string[]]> = [
      // [proto message, expected field lines]
      ['ClanMessage', ['string action = 200', 'bytes data = 201']],
      ['ClanResponseMessage', ['string action = 200', 'bool success = 201', 'bytes data = 202']],
      ['ShopMessage', ['string action = 300', 'bytes data = 301']],
      ['ShopResponseMessage', ['string action = 300', 'bytes data = 301']],
      ['QuestMessage', ['string action = 400', 'bytes data = 401']],
      ['QuestResponseMessage', ['string action = 400', 'bytes data = 401']],
      ['SeasonMessage', ['string action = 500', 'bytes data = 501']],
      ['SeasonResponseMessage', ['string action = 500', 'bytes data = 501']],
      ['TournamentMessage', ['string action = 600', 'bytes data = 601']],
      ['TournamentResponseMessage', ['string action = 600', 'bytes data = 601']],
      ['ReplayMessage', ['string action = 700', 'bytes data = 701']],
      ['ReplayResponseMessage', ['string action = 700', 'bytes data = 701']],
      ['PlayerMessage', ['string action = 800', 'bytes data = 801']],
      ['PlayerResponseMessage', ['string action = 800', 'bytes data = 801']],
      ['BattleStartMessage', ['string battle_id = 100', 'uint32 tick = 101']],
      ['SaveDeckRequest', ['repeated uint32 card_ids = 90']],
      ['DeckSavedMessage', ['bool success = 90']],
      ['AuthMessage', ['string token = 10']],
    ];
    for (const [msg, fields] of checks) {
      const body = protoMessages.get(msg);
      expect(body).toBeDefined();
      for (const f of fields) {
        expect(body).toContain(f);
      }
      // every wire message carries the base envelope prefix
      expect(body).toContain('string type = 1');
      expect(body).toContain('uint32 request_id = 2');
      expect(body).toContain('uint64 timestamp = 3');
    }

    // C# side spot-checks for the same numbers
    expect(cs).toContain('[ProtoMember(200)] public string action');
    expect(cs).toContain('[ProtoMember(300)] public string action');
    expect(cs).toContain('[ProtoMember(400)] public string action');
    expect(cs).toContain('[ProtoMember(500)] public string action');
    expect(cs).toContain('[ProtoMember(600)] public string action');
    expect(cs).toContain('[ProtoMember(700)] public string action');
    expect(cs).toContain('[ProtoMember(800)] public string action');
    expect(cs).toContain('[ProtoMember(100)] public string battleId');
    expect(cs).toContain('[ProtoMember(90)] public uint[] cardIds');
  });

  describe('protobuf round-trip', () => {
    beforeAll(async () => {
      await loadProtocol();
      expect(getProtocolRoot()).not.toBeNull();
    });

    test('verifyMessageType passes for every TS type', () => {
      for (const v of getAllMessageTypes()) {
        expect(verifyMessageType(v)).toBe(true);
      }
      expect(verifyMessageType('definitely_not_a_message')).toBe(false);
    });

    test('auth message round-trips', () => {
      const msg: ProtoMessage = { type: 'auth', token: 'tok123', timestamp: 1 };
      const bytes = encodeMessage(msg);
      expect(bytes.length).toBeGreaterThan(0);
      const back = decodeMessage(bytes);
      expect(back.type).toBe('auth');
      expect((back as any).token).toBe('tok123');
    });

    test('heartbeat round-trips', () => {
      const bytes = encodeMessage({ type: 'heartbeat', timestamp: 1 });
      const back = decodeMessage(bytes);
      expect(back.type).toBe('heartbeat');
    });

    test('clan message round-trips action/data', () => {
      const data = Uint8Array.from([1, 2, 3, 4]);
      const bytes = encodeMessage({ type: 'clan', action: 'get_info', data, timestamp: 1 });
      const back = decodeMessage(bytes) as any;
      expect(back.type).toBe('clan');
      expect(back.action).toBe('get_info');
      expect(Buffer.from(back.data as Uint8Array).equals(Buffer.from(data))).toBe(true);
    });

    test('save_deck round-trips card ids', () => {
      const bytes = encodeMessage({ type: 'save_deck', cardIds: [1, 2, 3], timestamp: 1 });
      const back = decodeMessage(bytes) as any;
      expect(back.type).toBe('save_deck');
      expect(Array.from(back.cardIds as number[])).toEqual([1, 2, 3]);
    });

    test('battle_start round-trips', () => {
      const bytes = encodeMessage({
        type: 'battle_start',
        battleId: 'b1',
        tick: 7,
        timestamp: 1,
      });
      const back = decodeMessage(bytes) as any;
      expect(back.type).toBe('battle_start');
      expect(back.battleId).toBe('b1');
      expect(Number(back.tick)).toBe(7);
    });
  });
});
