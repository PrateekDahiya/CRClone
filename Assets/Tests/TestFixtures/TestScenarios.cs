using System.Collections.Generic;
using CRClone.Battle.Simulation;
using CRClone.Core;
using CRClone.Data;

namespace CRClone.Tests.TestFixtures
{
    public static class TestScenarios
    {
        public class BattleScenario
        {
            public string Name { get; set; }
            public int[] Player1Deck { get; set; }
            public int[] Player2Deck { get; set; }
            public int Player1Trophies { get; set; }
            public int Player2Trophies { get; set; }
            public ulong Seed { get; set; } = 12345;
            public bool TournamentRules { get; set; } = false;
        }

        public static readonly BattleScenario Standard1v1 = new BattleScenario
        {
            Name = "Standard 1v1",
            Player1Deck = TestDecks.BalancedDeck,
            Player2Deck = TestDecks.BalancedDeck,
            Player1Trophies = 4000,
            Player2Trophies = 4000
        };

        public static readonly BattleScenario TankVsSwarm = new BattleScenario
        {
            Name = "Tank vs Swarm",
            Player1Deck = TestDecks.TankDeck,
            Player2Deck = new int[]
            {
                26000068, // Goblin Gang
                26000046, // Skeletons
                26000069, // Bats
                26000070, // Minion Horde
                26000049, // The Log
                26000054, // Tornado
                26000040, // Knight
                26000071  // Ice Spirit
            },
            Player1Trophies = 4500,
            Player2Trophies = 4300
        };

        public static readonly BattleScenario AirVsGround = new BattleScenario
        {
            Name = "Air vs Ground",
            Player1Deck = new int[]
            {
                26000057, // Lava Hound
                26000072, // Balloon
                26000073, // Mega Minion
                26000074, // Inferno Dragon
                26000044, // Fireball
                26000048, // Zap
                26000049, // The Log
                26000075  // Minions
            },
            Player2Deck = TestDecks.BalancedDeck,
            Player1Trophies = 4200,
            Player2Trophies = 4200
        };

        public static readonly BattleScenario SpellBait = new BattleScenario
        {
            Name = "Spell Bait",
            Player1Deck = new int[]
            {
                26000076, // Goblin Barrel
                26000077, // Princess
                26000078, // Dart Goblin
                26000079, // Gang
                26000046, // Skeletons
                26000080, // Knight
                26000044, // Fireball
                26000048  // Zap
            },
            Player2Deck = TestDecks.SpellHeavyDeck,
            Player1Trophies = 4400,
            Player2Trophies = 4400
        };

        public static readonly BattleScenario OvertimeScenario = new BattleScenario
        {
            Name = "Overtime Test",
            Player1Deck = TestDecks.BalancedDeck,
            Player2Deck = TestDecks.BalancedDeck,
            Player1Trophies = 4000,
            Player2Trophies = 4000
        };

        public static readonly BattleScenario DrawScenario = new BattleScenario
        {
            Name = "Draw Test",
            Player1Deck = TestDecks.BuildingDeck,
            Player2Deck = TestDecks.BuildingDeck,
            Player1Trophies = 4000,
            Player2Trophies = 4000
        };

        public static List<BattleScenario> GetAllScenarios()
        {
            return new List<BattleScenario>
            {
                Standard1v1,
                TankVsSwarm,
                AirVsGround,
                SpellBait,
                OvertimeScenario,
                DrawScenario
            };
        }
    }
}