using System.Collections.Generic;
using CRClone.Core;
using CRClone.Battle.Simulation;
using CRClone.Data;

namespace CRClone.Tests.TestFixtures
{
    public static class TestDecks
    {
        public static readonly int[] BalancedDeck = new int[]
        {
            26000040, // Knight
            26000041, // Archers
            26000042, // Giant
            26000043, // Musketeer
            26000044, // Fireball
            26000045, // Cannon
            26000046, // Skeletons
            26000047  // Minions
        };

        public static readonly int[] SpellHeavyDeck = new int[]
        {
            26000044, // Fireball
            26000048, // Zap
            26000049, // The Log
            26000050, // Poison
            26000051, // Rocket
            26000052, // Arrows
            26000053, // Freeze
            26000054  // Tornado
        };

        public static readonly int[] TankDeck = new int[]
        {
            26000042, // Giant
            26000055, // Golem
            26000056, // Mega Knight
            26000057, // Lava Hound
            26000043, // Musketeer
            26000058, // Wizard
            26000044, // Fireball
            26000048  // Zap
        };

        public static readonly int[] CycleDeck = new int[]
        {
            26000046, // Skeletons
            26000059, // Ice Spirit
            26000040, // Knight
            26000048, // Zap
            26000049, // The Log
            26000041, // Archers
            26000060, // Cannon
            26000061  // Ice Golem
        };

        public static readonly int[] ChampionDeck = new int[]
        {
            27000000, // Archer Queen
            26000040, // Knight
            26000041, // Archers
            26000044, // Fireball
            26000045, // Cannon
            26000046, // Skeletons
            26000047, // Minions
            26000048  // Zap
        };

        public static readonly int[] BuildingDeck = new int[]
        {
            26000045, // Cannon
            26000062, // Tesla
            26000063, // Inferno Tower
            26000064, // Goblin Hut
            26000065, // Furnace
            26000066, // Bomb Tower
            26000067, // Elixir Collector
            26000044  // Fireball
        };
    }
}