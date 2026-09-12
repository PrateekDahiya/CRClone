import { QuestService } from '../../src/services/QuestService';

function makeMockDb() {
  return { query: jest.fn().mockResolvedValue([]), execute: jest.fn().mockResolvedValue({}) } as any;
}

describe('QuestService', () => {
  test('Constructs without DB connection', () => {
    expect(() => new QuestService(makeMockDb())).not.toThrow();
  });

  test('getActiveQuests returns array', async () => {
    const svc = new QuestService(makeMockDb());
    const { QuestRepository } = require('../../src/persistence/QuestRepository');
    const spy = jest.spyOn(QuestRepository.prototype, 'getActiveQuests').mockResolvedValue([]);
    expect(await svc.getActiveQuests()).toEqual([]);
    spy.mockRestore();
  });

  test('getQuest returns null when missing', async () => {
    const svc = new QuestService(makeMockDb());
    const { QuestRepository } = require('../../src/persistence/QuestRepository');
    const spy = jest.spyOn(QuestRepository.prototype, 'getQuest').mockResolvedValue(null);
    expect(await svc.getQuest(999999)).toBeNull();
    spy.mockRestore();
  });

  test('resetDailyQuests delegates to repo', async () => {
    const svc = new QuestService(makeMockDb());
    const { QuestRepository } = require('../../src/persistence/QuestRepository');
    const spy = jest.spyOn(QuestRepository.prototype, 'resetDailyQuests').mockResolvedValue(0);
    expect(await svc.resetDailyQuests()).toBe(0);
    spy.mockRestore();
  });
});
