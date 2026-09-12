namespace CRClone.Tests.TestFixtures
{
    /// <summary>
    /// Card IDs match Assets/Resources/Data/Cards/*.asset (cardId 1-122).
    /// Knight=89 Archers=90 Giant=53 Musketeer=54 Fireball=57 Cannon=94
    /// Skeletons=92 Minions=93 Zap=59 Log=1 Poison=34 Rocket=32 Arrows=58
    /// Freeze=35 Tornado=38 Tesla=95 InfernoTower=31 GoblinHut=30 Furnace=100
    /// BombTower=96 ElixirCollector=99 ArcherQueen=51 SkeletonKing=50 MightyMiner=52
    /// </summary>
    public static class TestDecks
    {
        public static readonly int[] BalancedDeck = new int[]
        {
            89, // Knight
            90, // Archers
            53, // Giant
            54, // Musketeer
            57, // Fireball
            94, // Cannon
            92, // Skeletons
            93  // Minions
        };

        public static readonly int[] SpellHeavyDeck = new int[]
        {
            57, // Fireball
            59, // Zap
            1,  // The Log
            34, // Poison
            32, // Rocket
            58, // Arrows
            35, // Freeze
            38  // Tornado
        };

        public static readonly int[] TankDeck = new int[]
        {
            53, // Giant
            25, // P.E.K.K.A
            8,  // Mega Knight
            7,  // Lava Hound
            54, // Musketeer
            21, // Wizard
            57, // Fireball
            59  // Zap
        };

        public static readonly int[] CycleDeck = new int[]
        {
            92, // Skeletons
            10, // Ice Spirit
            89, // Knight
            59, // Zap
            1,  // The Log
            90, // Archers
            94, // Cannon
            91  // Goblins
        };

        public static readonly int[] ChampionDeck = new int[]
        {
            51, // Archer Queen
            89, // Knight
            90, // Archers
            57, // Fireball
            94, // Cannon
            92, // Skeletons
            93, // Minions
            59  // Zap
        };

        public static readonly int[] SkeletonKingDeck = new int[]
        {
            50, // Skeleton King
            89, // Knight
            90, // Archers
            57, // Fireball
            94, // Cannon
            92, // Skeletons
            93, // Minions
            59  // Zap
        };

        public static readonly int[] MightyMinerDeck = new int[]
        {
            52, // Mighty Miner
            89, // Knight
            90, // Archers
            57, // Fireball
            94, // Cannon
            92, // Skeletons
            93, // Minions
            59  // Zap
        };

        public static readonly int[] BuildingDeck = new int[]
        {
            94, // Cannon
            95, // Tesla
            31, // Inferno Tower
            30, // Goblin Hut
            100, // Furnace
            96, // Bomb Tower
            99, // Elixir Collector
            57  // Fireball
        };
    }
}
