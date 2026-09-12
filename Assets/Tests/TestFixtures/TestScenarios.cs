using System.Collections.Generic;

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
            Player2Deck = new int[] { 76, 92, 82, 26, 1, 38, 89, 10 },
            Player1Trophies = 4500,
            Player2Trophies = 4300
        };

        public static readonly BattleScenario AirVsGround = new BattleScenario
        {
            Name = "Air vs Ground",
            Player1Deck = new int[] { 7, 24, 66, 6, 57, 59, 1, 93 },
            Player2Deck = TestDecks.BalancedDeck,
            Player1Trophies = 4200,
            Player2Trophies = 4200
        };

        public static readonly BattleScenario SpellBait = new BattleScenario
        {
            Name = "Spell Bait",
            Player1Deck = new int[] { 29, 2, 44, 76, 92, 89, 57, 59 },
            Player2Deck = TestDecks.SpellHeavyDeck,
            Player1Trophies = 4400,
            Player2Trophies = 4400
        };

        public static List<BattleScenario> GetAllScenarios()
        {
            return new List<BattleScenario>
            {
                Standard1v1,
                TankVsSwarm,
                AirVsGround,
                SpellBait
            };
        }
    }
}
