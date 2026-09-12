import { DeterministicRNG, generateBattleSeed } from '../../src/utils/rng';

describe('DeterministicRNG', () => {
  describe('Determinism', () => {
    test('Same seed produces identical sequence', () => {
      const rng1 = new DeterministicRNG(12345n);
      const rng2 = new DeterministicRNG(12345n);
      for (let i = 0; i < 200; i++) {
        expect(rng1.nextUInt()).toBe(rng2.nextUInt());
      }
    });

    test('Different seeds produce different sequences', () => {
      const rng1 = new DeterministicRNG(12345n);
      const rng2 = new DeterministicRNG(54321n);
      let allSame = true;
      for (let i = 0; i < 50; i++) {
        if (rng1.nextUInt() !== rng2.nextUInt()) { allSame = false; break; }
      }
      expect(allSame).toBe(false);
    });

    test('State can be saved and restored', () => {
      const rng = new DeterministicRNG(999n);
      rng.nextUInt(); rng.nextUInt();
      const state = rng.getState();
      const third = rng.nextUInt();
      const fourth = rng.nextUInt();
      rng.setState(state);
      expect(rng.nextUInt()).toBe(third);
      expect(rng.nextUInt()).toBe(fourth);
    });
  });

  describe('nextUInt', () => {
    test('Returns 32-bit unsigned integers', () => {
      const rng = new DeterministicRNG(12345n);
      for (let i = 0; i < 500; i++) {
        const val = rng.nextUInt();
        expect(val).toBeGreaterThanOrEqual(0);
        expect(val).toBeLessThanOrEqual(0xFFFFFFFF);
      }
    });
  });

  describe('nextInt', () => {
    test('Returns values in range [min, max)', () => {
      const rng = new DeterministicRNG(12345n);
      for (let i = 0; i < 500; i++) {
        const val = rng.nextInt(5, 15);
        expect(val).toBeGreaterThanOrEqual(5);
        expect(val).toBeLessThan(15);
      }
    });
  });

  describe('nextFloat', () => {
    test('Returns values in [0, 1)', () => {
      const rng = new DeterministicRNG(12345n);
      for (let i = 0; i < 500; i++) {
        const val = rng.nextFloat();
        expect(val).toBeGreaterThanOrEqual(0);
        expect(val).toBeLessThan(1);
      }
    });
  });

  describe('nextBool', () => {
    test('Returns true with given probability', () => {
      const rng = new DeterministicRNG(12345n);
      let trueCount = 0;
      const trials = 5000;
      for (let i = 0; i < trials; i++) if (rng.nextBool(0.3)) trueCount++;
      const ratio = trueCount / trials;
      expect(ratio).toBeGreaterThan(0.26);
      expect(ratio).toBeLessThan(0.34);
    });

    test('Always true for probability 1, false for 0', () => {
      const rng = new DeterministicRNG(7n);
      for (let i = 0; i < 20; i++) expect(rng.nextBool(1.0)).toBe(true);
      for (let i = 0; i < 20; i++) expect(rng.nextBool(0.0)).toBe(false);
    });
  });

  describe('shuffle / sample', () => {
    test('Shuffle preserves elements', () => {
      const rng = new DeterministicRNG(42n);
      const arr = [1, 2, 3, 4, 5, 6, 7, 8];
      const out = rng.shuffle([...arr]);
      expect(out.sort((a, b) => a - b)).toEqual(arr);
    });

    test('Sample returns requested count', () => {
      const rng = new DeterministicRNG(42n);
      const out = rng.sample([1, 2, 3, 4, 5], 3);
      expect(out).toHaveLength(3);
    });
  });

  describe('generateBattleSeed', () => {
    test('Deterministic for same inputs, different for different inputs', () => {
      const s1 = generateBattleSeed('b1', 'p1', 'p2', 1000);
      const s2 = generateBattleSeed('b1', 'p1', 'p2', 1000);
      const s3 = generateBattleSeed('b2', 'p1', 'p2', 1000);
      expect(s1).toBe(s2);
      expect(s1).not.toBe(s3);
      expect(s1).not.toBe(0n);
    });
  });
});
