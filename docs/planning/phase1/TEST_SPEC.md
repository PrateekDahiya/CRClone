# Clash Royale Clone - Test Specification

## 1. TEST STRATEGY OVERVIEW

### 1.1 Test Pyramid
```
                    ┌─────────────┐
                    │   E2E/UI    │  ← 10% (Critical paths)
                    │  (Manual)   │
                   ┌┴─────────────┴┐
                  │  Integration  │  ← 20% (API, Network, Battle)
                  │   Tests       │
                 ┌┴────────────────┴┐
                │    Unit Tests     │  ← 70% (Simulation, Logic, Utils)
                │   (Automated)     │
               ┌┴────────────────────┴┐
              │   Static Analysis    │  ← Lint, TypeCheck, Security
              └──────────────────────┘
```

### 1.2 Test Categories

| Category | Tool | Coverage Target | Run Frequency |
|----------|------|-----------------|---------------|
| Unit Tests | NUnit/Jest | 80%+ | Every commit |
| Integration | Custom | Core flows | Daily CI |
| E2E | Playwright/Cypress | Critical paths | Pre-release |
| Performance | Custom | Benchmarks | Weekly |
| Load | k6/Artillery | Server capacity | Pre-launch |
| Security | OWASP ZAP | Vulnerabilities | Monthly |

---

## 2. UNIT TEST SPECIFICATIONS

### 2.1 Battle Simulation Tests

#### 2.1.1 Core Mechanics
```csharp
[TestFixture]
public class ElixirSystemTests
{
    [Test]
    public void Elixir_Generates_At_Correct_Rate_Normal()
    {
        var sim = CreateSimulation();
        sim.StartBattle();
        
        sim.Step(2.8f); // 1 elixir cycle
        
        Assert.AreEqual(6, sim.Player1.Elixir); // Started with 5
    }
    
    [Test]
    public void Elixir_Caps_At_10()
    {
        var sim = CreateSimulation();
        sim.Player1.Elixir = 10;
        
        sim.Step(10f);
        
        Assert.AreEqual(10, sim.Player1.Elixir);
    }
    
    [Test]
    public void Double_Elixir_Generates_Twice_As_Fast()
    {
        var sim = CreateSimulation();
        sim.StartBattle();
        sim.SetDoubleElixir(true);
        
        sim.Step(1.4f); // Double elixir cycle
        
        Assert.AreEqual(6, sim.Player1.Elixir);
    }
    
    [Test]
    public void Elixir_Spent_On_Card_Play()
    {
        var sim = CreateSimulation();
        sim.Player1.Elixir = 5;
        
        sim.PlayCard(Player.P1, CardId.Knight, new Vector2(9, 8));
        
        Assert.AreEqual(2, sim.Player1.Elixir); // Knight costs 3
    }
    
    [Test]
    public void Cannot_Play_Card_Without_Elixir()
    {
        var sim = CreateSimulation();
        sim.Player1.Elixir = 2;
        
        var result = sim.PlayCard(Player.P1, CardId.Knight, new Vector2(9, 8));
        
        Assert.IsFalse(result.Success);
        Assert.AreEqual(2, sim.Player1.Elixir);
    }
}
```

```csharp
[TestFixture]
public class CardCycleTests
{
    [Test]
    public void Initial_Hand_Has_4_Cards()
    {
        var sim = CreateSimulation();
        sim.StartBattle();
        
        Assert.AreEqual(4, sim.Player1.Hand.Count);
    }
    
    [Test]
    public void Playing_Card_Draws_Next_In_Cycle()
    {
        var sim = CreateSimulation();
        sim.StartBattle();
        var initialHand = sim.Player1.Hand.ToList();
        var fifthCard = sim.Player1.Deck[4]; // 5th card in deck
        
        sim.PlayCard(Player.P1, initialHand[0], new Vector2(9, 8));
        
        Assert.AreEqual(fifthCard, sim.Player1.Hand[3]); // New card at position 4
    }
    
    [Test]
    public void Cycle_Wraps_Around_After_8_Cards()
    {
        var sim = CreateSimulation();
        sim.StartBattle();
        
        // Play all 8 cards
        for (int i = 0; i < 8; i++)
        {
            sim.Player1.Elixir = 10;
            sim.PlayCard(Player.P1, sim.Player1.Hand[0], new Vector2(9, 8));
            sim.Step(0.1f); // Small step to process
        }
        
        // Hand should contain first 4 cards again
        for (int i = 0; i < 4; i++)
        {
            Assert.AreEqual(sim.Player1.Deck[i], sim.Player1.Hand[i]);
        }
    }
}
```

#### 2.1.2 Unit Combat Tests
```csharp
[TestFixture]
public class UnitCombatTests
{
    [Test]
    public void Knight_Defeats_Single_Goblin()
    {
        var sim = CreateSimulation();
        var knight = sim.SpawnUnit(CardId.Knight, Player.P1, new Vector2(9, 8));
        var goblin = sim.SpawnUnit(CardId.Goblin, Player.P2, new Vector2(9, 9));
        
        sim.StepUntil(() => knight.IsDead || goblin.IsDead, maxTime: 10f);
        
        Assert.IsTrue(goblin.IsDead);
        Assert.IsFalse(knight.IsDead);
        Assert.Greater(knight.CurrentHP, knight.MaxHP * 0.5f);
    }
    
    [Test]
    public void Three_Goblins_Defeat_Knight()
    {
        var sim = CreateSimulation();
        var knight = sim.SpawnUnit(CardId.Knight, Player.P1, new Vector2(9, 8));
        var goblins = new List<Unit>();
        for (int i = 0; i < 3; i++)
            goblins.Add(sim.SpawnUnit(CardId.Goblin, Player.P2, new Vector2(9 + i * 0.5f, 9)));
        
        sim.StepUntil(() => knight.IsDead || goblins.All(g => g.IsDead), maxTime: 10f);
        
        Assert.IsTrue(knight.IsDead);
    }
    
    [Test]
    public void Ranged_Unit_Attacks_From_Distance()
    {
        var sim = CreateSimulation();
        var musketeer = sim.SpawnUnit(CardId.Musketeer, Player.P1, new Vector2(9, 5));
        var knight = sim.SpawnUnit(CardId.Knight, Player.P2, new Vector2(9, 12));
        
        sim.Step(1f);
        
        // Musketeer range = 6 tiles, distance = 7, should not attack yet
        Assert.AreEqual(musketeer.MaxHP, musketeer.CurrentHP);
        Assert.AreEqual(knight.MaxHP, knight.CurrentHP);
        
        sim.Step(2f); // Knight walks closer
        
        // Now in range, musketeer should have attacked
        Assert.Less(knight.CurrentHP, knight.MaxHP);
    }
    
    [Test]
    public void Splash_Damage_Hits_Multiple_Units()
    {
        var sim = CreateSimulation();
        var wizard = sim.SpawnUnit(CardId.Wizard, Player.P1, new Vector2(9, 5));
        var goblins = new List<Unit>();
        for (int i = 0; i < 3; i++)
            goblins.Add(sim.SpawnUnit(CardId.Goblin, Player.P2, new Vector2(9 + i * 0.8f, 8)));
        
        sim.StepUntil(() => wizard.AttackCooldown <= 0, maxTime: 5f);
        sim.Step(0.1f); // Process attack
        
        // All 3 goblins should take splash damage
        foreach (var g in goblins)
            Assert.Less(g.CurrentHP, g.MaxHP);
    }
}
```

#### 2.1.3 Building Tests
```csharp
[TestFixture]
public class BuildingTests
{
    [Test]
    public void Cannon_Attacks_Ground_Units_Only()
    {
        var sim = CreateSimulation();
        var cannon = sim.SpawnBuilding(CardId.Cannon, Player.P1, new Vector2(9, 8));
        var knight = sim.SpawnUnit(CardId.Knight, Player.P2, new Vector2(9, 10));
        var minion = sim.SpawnUnit(CardId.Minion, Player.P2, new Vector2(9, 10));
        
        sim.Step(3f);
        
        Assert.Less(knight.CurrentHP, knight.MaxHP);
        Assert.AreEqual(minion.MaxHP, minion.CurrentHP); // Minion is air
    }
    
    [Test]
    public void Tesla_Retracts_When_No_Targets()
    {
        var sim = CreateSimulation();
        var tesla = sim.SpawnBuilding(CardId.Tesla, Player.P1, new Vector2(9, 8));
        
        sim.Step(5f);
        
        Assert.IsTrue(tesla.IsRetracted);
        Assert.IsTrue(tesla.IsInvulnerable);
    }
    
    [Test]
    public void Tesla_Pops_Up_When_Target_In_Range()
    {
        var sim = CreateSimulation();
        var tesla = sim.SpawnBuilding(CardId.Tesla, Player.P1, new Vector2(9, 8));
        
        sim.Step(2f); // Tesla retracts
        Assert.IsTrue(tesla.IsRetracted);
        
        var knight = sim.SpawnUnit(CardId.Knight, Player.P2, new Vector2(9, 10));
        sim.Step(1f);
        
        Assert.IsFalse(tesla.IsRetracted);
        Assert.IsFalse(tesla.IsInvulnerable);
    }
    
    [Test]
    public void Spawner_Building_Spawns_Units_Over_Time()
    {
        var sim = CreateSimulation();
        var goblinHut = sim.SpawnBuilding(CardId.GoblinHut, Player.P1, new Vector2(9, 8));
        
        sim.Step(30f); // Full lifetime
        
        // Should spawn ~6 spear goblins (every 4.9s, max 6)
        var spawned = sim.GetEntitiesOfType(CardId.SpearGoblin, Player.P1);
        Assert.AreEqual(6, spawned.Count);
    }
    
    [Test]
    public void Building_Lifetime_Expires()
    {
        var sim = CreateSimulation();
        var cannon = sim.SpawnBuilding(CardId.Cannon, Player.P1, new Vector2(9, 8));
        
        sim.Step(31f); // Cannon lifetime = 30s
        
        Assert.IsTrue(cannon.IsDead);
    }
}
```

#### 2.1.4 Spell Tests
```csharp
[TestFixture]
public class SpellTests
{
    [Test]
    public void Fireball_Damages_Units_In_Radius()
    {
        var sim = CreateSimulation();
        var units = new List<Unit>();
        for (int i = 0; i < 3; i++)
            units.Add(sim.SpawnUnit(CardId.Musketeer, Player.P2, new Vector2(9 + i, 8)));
        
        sim.CastSpell(Player.P1, CardId.Fireball, new Vector2(10, 8));
        sim.Step(1.5f); // Travel time
        
        foreach (var u in units)
            Assert.Less(u.CurrentHP, u.MaxHP);
    }
    
    [Test]
    public void Fireball_Knocks_Back_Units()
    {
        var sim = CreateSimulation();
        var knight = sim.SpawnUnit(CardId.Knight, Player.P2, new Vector2(10, 8));
        var originalPos = knight.Position;
        
        sim.CastSpell(Player.P1, CardId.Fireball, new Vector2(10, 8));
        sim.Step(1.5f);
        
        Assert.Greater(Vector2.Distance(knight.Position, originalPos), 0.3f);
    }
    
    [Test]
    public void Zap_Stuns_And_Resets_Inferno()
    {
        var sim = CreateSimulation();
        var inferno = sim.SpawnBuilding(CardId.InfernoTower, Player.P2, new Vector2(9, 8));
        var knight = sim.SpawnUnit(CardId.Knight, Player.P1, new Vector2(9, 10));
        
        // Let inferno ramp up
        sim.Step(3f);
        var highDamage = inferno.CurrentDamage;
        Assert.Greater(highDamage, 100);
        
        // Zap it
        sim.CastSpell(Player.P1, CardId.Zap, new Vector2(9, 8));
        sim.Step(0.1f);
        
        // Damage should reset
        Assert.AreEqual(50, inferno.CurrentDamage); // Base damage
        Assert.IsTrue(inferno.IsStunned);
    }
    
    [Test]
    public void Poison_Damages_Over_Time_And_Slows()
    {
        var sim = CreateSimulation();
        var knight = sim.SpawnUnit(CardId.Knight, Player.P2, new Vector2(9, 8));
        var originalSpeed = knight.MoveSpeed;
        
        sim.CastSpell(Player.P1, CardId.Poison, new Vector2(9, 8));
        sim.Step(0.1f);
        
        Assert.IsTrue(knight.IsSlowed);
        Assert.AreEqual(originalSpeed * 0.65f, knight.MoveSpeed, 0.01f);
        
        sim.Step(8f); // Full duration
        
        Assert.IsFalse(knight.IsSlowed);
        Assert.Less(knight.CurrentHP, knight.MaxHP - 400); // Significant damage
    }
    
    [Test]
    public void Freeze_Stops_Everything_In_Radius()
    {
        var sim = CreateSimulation();
        var knight = sim.SpawnUnit(CardId.Knight, Player.P2, new Vector2(9, 8));
        var cannon = sim.SpawnBuilding(CardId.Cannon, Player.P2, new Vector2(10, 8));
        
        sim.CastSpell(Player.P1, CardId.Freeze, new Vector2(9.5f, 8));
        sim.Step(0.1f);
        
        Assert.IsTrue(knight.IsFrozen);
        Assert.IsTrue(cannon.IsFrozen);
        
        sim.Step(2f);
        
        // Neither should have moved/attacked
        Assert.AreEqual(knight.MaxHP, knight.CurrentHP);
        Assert.AreEqual(cannon.MaxHP, cannon.CurrentHP);
    }
    
    [Test]
    public void Log_Only_Affects_Ground_Units()
    {
        var sim = CreateSimulation();
        var knight = sim.SpawnUnit(CardId.Knight, Player.P2, new Vector2(9, 8));
        var minion = sim.SpawnUnit(CardId.Minion, Player.P2, new Vector2(9, 8));
        
        sim.CastSpell(Player.P1, CardId.TheLog, new Vector2(5, 8)); // From left
        sim.Step(1f); // Travel across arena
        
        Assert.IsTrue(knight.IsDead || knight.CurrentHP < knight.MaxHP);
        Assert.AreEqual(minion.MaxHP, minion.CurrentHP); // Air unit unaffected
    }
    
    [Test]
    public void Tornado_Pulls_Units_To_Center()
    {
        var sim = CreateSimulation();
        var knight = sim.SpawnUnit(CardId.Knight, Player.P2, new Vector2(12, 8));
        var originalPos = knight.Position;
        
        sim.CastSpell(Player.P1, CardId.Tornado, new Vector2(9, 8));
        sim.Step(1.5f);
        
        Assert.Less(Vector2.Distance(knight.Position, new Vector2(9, 8)), 
                   Vector2.Distance(originalPos, new Vector2(9, 8)));
    }
    
    [Test]
    public void Graveyard_Spawns_Skeletons_Randomly()
    {
        var sim = CreateSimulation();
        
        sim.CastSpell(Player.P1, CardId.Graveyard, new Vector2(9, 8));
        sim.Step(3.5f); // Spawn duration + a bit
        
        var skeletons = sim.GetEntitiesOfType(CardId.Skeleton, Player.P1);
        Assert.AreEqual(15, skeletons.Count); // 15 skeletons total
        
        // All should be within 4 tile radius
        foreach (var s in skeletons)
        {
            Assert.Less(Vector2.Distance(s.Position, new Vector2(9, 8)), 4.5f);
        }
    }
}
```

#### 2.1.5 Champion Tests
```csharp
[TestFixture]
public class ChampionTests
{
    [Test]
    public void Archer_Queen_Ability_Grants_Invisibility_And_Damage_Boost()
    {
        var sim = CreateSimulation();
        var aq = sim.SpawnUnit(CardId.ArcherQueen, Player.P1, new Vector2(9, 8));
        
        sim.UseChampionAbility(Player.P1, new Vector2(12, 8));
        sim.Step(0.1f);
        
        Assert.IsTrue(aq.IsInvisible);
        Assert.AreEqual(aq.BaseDamage * 2.5f, aq.CurrentDamage, 1f);
        
        sim.Step(3f); // Duration
        
        Assert.IsFalse(aq.IsInvisible);
        Assert.AreEqual(aq.BaseDamage, aq.CurrentDamage);
    }
    
    [Test]
    public void Skeleton_King_Ability_Spawns_Skeletons()
    {
        var sim = CreateSimulation();
        var sk = sim.SpawnUnit(CardId.SkeletonKing, Player.P1, new Vector2(9, 8));
        
        sim.UseChampionAbility(Player.P1, new Vector2(9, 8));
        sim.Step(0.1f);
        
        var skeletons = sim.GetEntitiesOfType(CardId.Skeleton, Player.P1);
        Assert.AreEqual(5, skeletons.Count);
    }
    
    [Test]
    public void Mighty_Miner_Ability_Dashes_And_Stuns()
    {
        var sim = CreateSimulation();
        var mm = sim.SpawnUnit(CardId.MightyMiner, Player.P1, new Vector2(5, 8));
        var knight = sim.SpawnUnit(CardId.Knight, Player.P2, new Vector2(9, 8));
        
        sim.UseChampionAbility(Player.P1, new Vector2(10, 8)); // Dash right
        sim.Step(0.5f);
        
        Assert.Greater(mm.Position.x, 9f); // Dashed past knight
        Assert.IsTrue(knight.IsStunned);
    }
    
    [Test]
    public void Champion_Ability_Respects_Cooldown()
    {
        var sim = CreateSimulation();
        var aq = sim.SpawnUnit(CardId.ArcherQueen, Player.P1, new Vector2(9, 8));
        
        sim.UseChampionAbility(Player.P1, new Vector2(9, 8));
        Assert.IsTrue(aq.AbilityOnCooldown);
        
        sim.Step(19f); // Almost cooldown
        Assert.IsTrue(aq.AbilityOnCooldown);
        
        sim.Step(2f); // Full 20s cooldown
        Assert.IsFalse(aq.AbilityOnCooldown);
    }
}
```

#### 2.1.6 Targeting & Pathfinding Tests
```csharp
[TestFixture]
public class TargetingTests
{
    [Test]
    public void Units_Target_Closest_By_Path_Distance()
    {
        var sim = CreateSimulation();
        var knight = sim.SpawnUnit(CardId.Knight, Player.P1, new Vector2(9, 5));
        var goblin1 = sim.SpawnUnit(CardId.Goblin, Player.P2, new Vector2(9, 12)); // Straight line
        var goblin2 = sim.SpawnUnit(CardId.Goblin, Player.P2, new Vector2(12, 9)); // Around river
        
        sim.Step(1f);
        
        // Goblin1 is closer by path (river blocks direct path to goblin2)
        Assert.AreEqual(goblin1, knight.Target);
    }
    
    [Test]
    public void Building_Targeters_Ignore_Troops()
    {
        var sim = CreateSimulation();
        var giant = sim.SpawnUnit(CardId.Giant, Player.P1, new Vector2(9, 5));
        var knight = sim.SpawnUnit(CardId.Knight, Player.P2, new Vector2(9, 9));
        var cannon = sim.SpawnBuilding(CardId.Cannon, Player.P2, new Vector2(9, 12));
        
        sim.Step(2f);
        
        // Giant targets buildings only
        Assert.AreEqual(cannon, giant.Target);
    }
    
    [Test]
    public void Retarget_On_Target_Death()
    {
        var sim = CreateSimulation();
        var wizard = sim.SpawnUnit(CardId.Wizard, Player.P1, new Vector2(9, 5));
        var goblin1 = sim.SpawnUnit(CardId.Goblin, Player.P2, new Vector2(9, 8));
        var goblin2 = sim.SpawnUnit(CardId.Goblin, Player.P2, new Vector2(10, 8));
        
        sim.StepUntil(() => goblin1.IsDead, maxTime: 5f);
        
        Assert.AreEqual(goblin2, wizard.Target);
    }
}
```

---

## 3. INTEGRATION TEST SPECIFICATIONS

### 3.1 Battle Flow Tests
```typescript
// battle-flow.test.ts
describe('Battle Flow Integration', () => {
    let server: TestServer;
    let client1: TestClient;
    let client2: TestClient;
    
    beforeAll(async () => {
        server = await startTestServer();
    });
    
    afterAll(async () => {
        await server.stop();
    });
    
    beforeEach(async () => {
        client1 = await connectClient(server, 'player1', testDeck1);
        client2 = await connectClient(server, 'player2', testDeck2);
    });
    
    afterEach(async () => {
        await client1.disconnect();
        await client2.disconnect();
    });
    
    test('complete 1v1 battle from matchmaking to result', async () => {
        // Matchmake
        await client1.matchmake(BattleType.Ladder);
        await client2.matchmake(BattleType.Ladder);
        
        const battleInfo = await client1.waitForBattleFound(10000);
        expect(battleInfo).toBeDefined();
        expect(battleInfo.seed).toBeDefined();
        
        // Load battle scene
        await client1.loadBattle(battleInfo);
        await client2.loadBattle(battleInfo);
        
        // Wait for battle ready
        await client1.waitForBattleStart(5000);
        await client2.waitForBattleStart(5000);
        
        // Play a simple game: Knight vs Knight
        await client1.playCard(CardId.Knight, { x: 9, y: 8 });
        await wait(2000);
        await client2.playCard(CardId.Knight, { x: 9, 10 });
        await wait(5000);
        
        // Fireball the enemy knight
        await client1.playCard(CardId.Fireball, { x: 9, y: 10 });
        await wait(2000);
        
        // Wait for battle end (or timeout)
        const result = await client1.waitForBattleEnd(30000);
        
        expect(result.winner).toBeDefined();
        expect(result.crowns).toBeDefined();
        expect(result.trophyChange).toBeDefined();
        expect(result.replayId).toBeDefined();
    });
    
    test('overtime sudden death works', async () => {
        // Setup: Both players at 1-1 crowns, low king HP
        const battleInfo = await setupOvertimeScenario(client1, client2);
        
        await client1.loadBattle(battleInfo);
        await client2.loadBattle(battleInfo);
        
        // First to damage king tower wins
        await client1.playCard(CardId.HogRider, { x: 9, y: 12 });
        
        const result = await client1.waitForBattleEnd(60000);
        
        expect(result.wentOvertime).toBe(true);
        expect(result.duration).toBeGreaterThan(180);
    });
    
    test('draw when no towers destroyed in overtime', async () => {
        const battleInfo = await setupDrawScenario(client1, client2);
        
        await client1.loadBattle(battleInfo);
        await client2.loadBattle(battleInfo);
        
        // Neither player attacks
        const result = await client1.waitForBattleEnd(300000); // 5 min max
        
        expect(result.isDraw).toBe(true);
        expect(result.trophyChange).toBe(0);
    });
});
```

### 3.2 Network Tests
```typescript
describe('Network Layer', () => {
    test('reconnection after disconnect', async () => {
        const client = await connectClient(server, 'player1');
        await client.matchmake(BattleType.Practice);
        await client.waitForBattleStart();
        
        // Disconnect
        client.disconnect();
        await wait(2000);
        
        // Reconnect
        await client.reconnect();
        
        // Should receive current game state
        const state = await client.waitForGameState(5000);
        expect(state.tick).toBeGreaterThan(0);
        expect(state.entities.length).toBeGreaterThan(0);
    });
    
    test('input validation rejects invalid plays', async () => {
        const client = await connectClient(server, 'player1');
        await client.matchmake(BattleType.Practice);
        await client.waitForBattleStart();
        
        // Try to play card not in deck
        const result = await client.playCard(999, { x: 9, y: 8 });
        expect(result.success).toBe(false);
        expect(result.error).toBe('CARD_NOT_IN_DECK');
    });
    
    test('rate limiting prevents spam', async () => {
        const client = await connectClient(server, 'player1');
        await client.matchmake(BattleType.Practice);
        await client.waitForBattleStart();
        
        // Send 100 inputs in 1 second
        for (let i = 0; i < 100; i++) {
            client.sendRawInput({ type: 'emote', emoteId: 1 });
        }
        
        await wait(1000);
        
        // Should be disconnected or rate limited
        expect(client.isConnected).toBe(false);
    });
});
```

### 3.3 Database Tests
```csharp
[TestFixture]
public class DatabaseIntegrationTests
{
    private MySqlConnection _conn;
    private PlayerRepository _playerRepo;
    private CardRepository _cardRepo;
    
    [OneTimeSetUp]
    public void Setup()
    {
        _conn = new MySqlConnection(TestConfig.ConnectionString);
        _conn.Open();
        _playerRepo = new PlayerRepository(_conn);
        _cardRepo = new CardRepository(_conn);
        
        // Seed test data
        SeedTestData();
    }
    
    [Test]
    public void Player_Can_Upgrade_Card()
    {
        var player = CreateTestPlayer();
        var card = GetCard(CardId.Knight);
        
        // Give player enough cards and gold
        _cardRepo.AddCards(player.Id, card.Id, 100);
        _playerRepo.AddGold(player.Id, 100000);
        
        var result = _cardRepo.UpgradeCard(player.Id, card.Id);
        
        Assert.IsTrue(result.Success);
        Assert.AreEqual(2, result.NewLevel);
        Assert.Less(_playerRepo.GetGold(player.Id), 100000);
    }
    
    [Test]
    public void Deck_Validation_Enforces_Rules()
    {
        var player = CreateTestPlayer();
        var deck = new Deck
        {
            Cards = new[] { CardId.Knight, CardId.ArcherQueen, CardId.ArcherQueen, ... } // 2 champions
        };
        
        var result = _deckRepo.SaveDeck(player.Id, deck);
        
        Assert.IsFalse(result.Success);
        Assert.AreEqual("MAX_ONE_CHAMPION", result.ErrorCode);
    }
}
```

---

## 4. E2E TEST SPECIFICATIONS

### 4.1 Critical User Journeys
```typescript
// e2e/critical-journeys.spec.ts
test.describe('Critical User Journeys', () => {
    test('New player onboarding', async ({ page }) => {
        await page.goto('/');
        
        // Guest login
        await page.click('[data-testid=guest-login]');
        
        // Tutorial completion
        await page.click('[data-testid=tutorial-start]');
        await completeTutorial(page);
        
        // First battle
        await page.click('[data-testid=battle-1v1]');
        await page.waitForSelector('[data-testid=battle-hud]');
        
        // Play a card
        await page.click('[data-testid=card-0]');
        await page.click('[data-testid=arena-position-9-8]');
        
        // Verify deployment
        await expect(page.locator('[data-testid=unit-knight]')).toBeVisible();
    });
    
    test('Deck building and saving', async ({ page }) => {
        await login(page, 'testuser');
        await page.click('[data-testid=nav-cards]');
        
        // Add cards to deck
        await page.dragAndDrop('[data-testid=card-knight]', '[data-testid=deck-slot-0]');
        await page.dragAndDrop('[data-testid=card-archers]', '[data-testid=deck-slot-1]');
        // ... fill all 8 slots
        
        // Save
        await page.click('[data-testid=deck-save]');
        await expect(page.locator('[data-testid=toast-success]')).toBeVisible();
        
        // Verify in battle
        await page.click('[data-testid=battle-1v1]');
        await page.waitForSelector('[data-testid=hand-card-0]');
        await expect(page.locator('[data-testid=hand-card-0]')).toHaveAttribute('data-card', 'knight');
    });
    
    test('Chest unlock flow', async ({ page }) => {
        await login(page, 'testuser');
        
        // Win battle to get chest
        await playAndWinBattle(page);
        
        // Go to chest screen
        await page.click('[data-testid=chest-slot-0]');
        await page.click('[data-testid=unlock-chest]');
        
        // Wait for unlock animation
        await page.waitForSelector('[data-testid=chest-rewards]');
        
        // Verify rewards received
        const rewards = await page.locator('[data-testid=reward-item]').all();
        expect(rewards.length).toBeGreaterThan(0);
    });
});
```

---

## 5. PERFORMANCE TEST SPECIFICATIONS

### 5.1 Client Performance Benchmarks
```csharp
[TestFixture]
public class PerformanceBenchmarks
{
    [Test]
    public void BattleSimulation_60FPS_Under_Load()
    {
        var sim = CreateSimulation();
        sim.StartBattle();
        
        // Spawn max units (50 per side)
        for (int i = 0; i < 50; i++)
        {
            sim.SpawnUnit(CardId.Knight, Player.P1, RandomPosition());
            sim.SpawnUnit(CardId.Knight, Player.P2, RandomPosition());
        }
        
        var sw = Stopwatch.StartNew();
        for (int tick = 0; tick < 3600; tick++) // 60 seconds at 60Hz
        {
            sim.Tick(FIXED_DT);
        }
        sw.Stop();
        
        var avgMsPerTick = sw.ElapsedMilliseconds / 3600.0;
        Assert.Less(avgMsPerTick, 16.67); // Must be under 16.67ms for 60 FPS
    }
    
    [Test]
    public void Pathfinding_Performance()
    {
        var pf = new Pathfinding();
        pf.Initialize(TestArena());
        
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < 10000; i++)
        {
            pf.FindPath(RandomPosition(), RandomPosition(), EntityType.Ground);
        }
        sw.Stop();
        
        Assert.Less(sw.ElapsedMilliseconds, 100); // 10k paths in <100ms
    }
    
    [Test]
    public void Memory_Stable_Over_Long_Battle()
    {
        var sim = CreateSimulation();
        sim.StartBattle();
        
        var initialMemory = GC.GetTotalMemory(true);
        
        // Simulate 5 minute battle with heavy activity
        for (int tick = 0; tick < 18000; tick++)
        {
            if (tick % 60 == 0)
            {
                sim.SpawnUnit(RandomCard(), RandomPlayer(), RandomPosition());
                sim.CastSpell(RandomPlayer(), RandomSpell(), RandomPosition());
            }
            sim.Tick(FIXED_DT);
        }
        
        GC.Collect();
        var finalMemory = GC.GetTotalMemory(true);
        var growth = finalMemory - initialMemory;
        
        Assert.Less(growth, 50 * 1024 * 1024); // < 50MB growth
    }
}
```

### 5.2 Server Load Tests (k6)
```javascript
// load-test.js
import http from 'k6/http';
import ws from 'k6/ws';
import { check, sleep } from 'k6';

export const options = {
    stages: [
        { duration: '2m', target: 100 },   // Ramp up
        { duration: '5m', target: 1000 },  // Stay at 1000 users
        { duration: '2m', target: 5000 },  // Stress to 5000
        { duration: '5m', target: 5000 },  // Sustain
        { duration: '2m', target: 0 },     // Ramp down
    ],
    thresholds: {
        'ws_connect_duration': ['p(95)<500'],
        'ws_msg_duration': ['p(99)<100'],
        'checks': ['rate>0.99'],
    },
};

export default function() {
    const url = `ws://${__ENV.SERVER_HOST}:3001/battle`;
    
    ws.connect(url, {}, function(socket) {
        socket.on('open', () => {
            socket.send(JSON.stringify({
                type: 'auth',
                token: `test_token_${__VU}`
            }));
        });
        
        socket.on('message', (msg) => {
            const data = JSON.parse(msg);
            if (data.type === 'battle_found') {
                // Simulate playing
                setInterval(() => {
                    socket.send(JSON.stringify({
                        type: 'input',
                        cardPlayed: { cardId: 1, position: { x: 9, y: 8 } },
                        clientTick: Date.now()
                    }));
                }, 3000);
            }
        });
        
        socket.setTimeout(() => socket.close(), 300000); // 5 min max
    });
    
    sleep(1);
}
```

---

## 6. REGRESSION TEST SUITE

### 6.1 Balance Regression Tests
```csharp
[TestFixture]
public class BalanceRegressionTests
{
    // These tests capture known-good interactions
    // If they fail, balance has changed unintentionally
    
    [Test]
    public void Fireball_Kills_Musketeer_At_Tournament_Standard()
    {
        var sim = CreateTournamentSimulation();
        var musketeer = sim.SpawnUnit(CardId.Musketeer, Player.P2, new Vector2(9, 8));
        
        sim.CastSpell(Player.P1, CardId.Fireball, new Vector2(9, 8));
        sim.Step(1.5f);
        
        Assert.IsTrue(musketeer.IsDead);
    }
    
    [Test]
    public void Rocket_Kills_Wizard_At_Tournament_Standard()
    {
        var sim = CreateTournamentSimulation();
        var wizard = sim.SpawnUnit(CardId.Wizard, Player.P2, new Vector2(9, 8));
        
        sim.CastSpell(Player.P1, CardId.Rocket, new Vector2(9, 8));
        sim.Step(2f);
        
        Assert.IsTrue(wizard.IsDead);
    }
    
    [Test]
    public void Lightning_Hits_3_Highest_HP_In_Radius()
    {
        var sim = CreateTournamentSimulation();
        var golem = sim.SpawnUnit(CardId.Golem, Player.P2, new Vector2(9, 8));
        var giant = sim.SpawnUnit(CardId.Giant, Player.P2, new Vector2(10, 8));
        var knight = sim.SpawnUnit(CardId.Knight, Player.P2, new Vector2(11, 8));
        var goblin = sim.SpawnUnit(CardId.Goblin, Player.P2, new Vector2(12, 8));
        
        sim.CastSpell(Player.P1, CardId.Lightning, new Vector2(10, 8));
        sim.Step(0.1f);
        
        // Should hit Golem, Giant, Knight (3 highest HP)
        Assert.IsTrue(golem.CurrentHP < golem.MaxHP);
        Assert.IsTrue(giant.CurrentHP < giant.MaxHP);
        Assert.IsTrue(knight.CurrentHP < knight.MaxHP);
        Assert.AreEqual(goblin.MaxHP, goblin.CurrentHP); // Goblin not hit
    }
    
    [Test]
    public void Log_Kills_Skeletons_At_Tournament_Standard()
    {
        var sim = CreateTournamentSimulation();
        var skeletons = new List<Unit>();
        for (int i = 0; i < 4; i++)
            skeletons.Add(sim.SpawnUnit(CardId.Skeleton, Player.P2, new Vector2(9 + i, 8)));
        
        sim.CastSpell(Player.P1, CardId.TheLog, new Vector2(5, 8));
        sim.Step(1f);
        
        foreach (var s in skeletons)
            Assert.IsTrue(s.IsDead);
    }
}
```

---

## 7. EDGE CASE TEST SCENARIOS

### 7.1 Simultaneous Events
```csharp
[TestFixture]
public class EdgeCaseTests
{
    [Test]
    public void Simultaneous_Tower_Destruction_Both_Players()
    {
        var sim = CreateSimulation();
        // Both towers at 1 HP
        sim.Player1TowerLeft.CurrentHP = 1;
        sim.Player2TowerLeft.CurrentHP = 1;
        
        // Rocket hits both simultaneously
        sim.CastSpell(Player.P1, CardId.Rocket, sim.Player2TowerLeft.Position);
        sim.CastSpell(Player.P2, CardId.Rocket, sim.Player1TowerLeft.Position);
        sim.Step(2f);
        
        // Both towers destroyed = Draw (or handle per rules)
        Assert.IsTrue(sim.Player1TowerLeft.IsDead);
        Assert.IsTrue(sim.Player2TowerLeft.IsDead);
    }
    
    [Test]
    public void King_Tower_Activates_On_Tornado_Pull()
    {
        var sim = CreateSimulation();
        var knight = sim.SpawnUnit(CardId.Knight, Player.P2, new Vector2(9, 4)); // Near king
        
        sim.CastSpell(Player.P1, CardId.Tornado, sim.KingTowerP2.Position);
        sim.Step(1.5f);
        
        Assert.IsTrue(sim.KingTowerP2.IsActivated);
        Assert.AreEqual(2, sim.KingTowerP2.GuardsSpawned.Count);
    }
    
    [Test]
    public void Charge_Cancelled_By_Zap_Mid_Dash()
    {
        var sim = CreateSimulation();
        var prince = sim.SpawnUnit(CardId.Prince, Player.P1, new Vector2(5, 8));
        
        // Let prince charge
        sim.Step(4f);
        Assert.IsTrue(prince.IsCharging);
        
        // Zap during charge
        sim.CastSpell(Player.P2, CardId.Zap, prince.Position);
        sim.Step(0.1f);
        
        Assert.IsFalse(prince.IsCharging);
        Assert.AreEqual(prince.BaseDamage, prince.CurrentDamage); // No charge bonus
    }
    
    [Test]
    public void Invisible_Unit_Revealed_By_Splash_Damage()
    {
        var sim = CreateSimulation();
        var ghost = sim.SpawnUnit(CardId.Ghost, Player.P2, new Vector2(9, 8));
        var wizard = sim.SpawnUnit(CardId.Wizard, Player.P1, new Vector2(9, 5));
        
        // Ghost is invisible
        Assert.IsTrue(ghost.IsInvisible);
        
        // Wizard attacks near ghost (splash)
        sim.StepUntil(() => wizard.AttackCooldown <= 0, maxTime: 5f);
        sim.Step(0.1f);
        
        // Ghost should be revealed by splash
        Assert.IsFalse(ghost.IsInvisible);
    }
    
    [Test]
    public void Clone_Creates_Units_At_Minus_1_Level()
    {
        var sim = CreateTournamentSimulation(); // Level 11
        var knight = sim.SpawnUnit(CardId.Knight, Player.P1, new Vector2(9, 8));
        var originalHP = knight.MaxHP;
        
        sim.CastSpell(Player.P1, CardId.Clone, new Vector2(9, 8));
        sim.Step(0.1f);
        
        var cloned = sim.GetEntitiesOfType(CardId.Knight, Player.P1).Last();
        // Cloned at level 10 (tournament - 1)
        Assert.AreEqual(GetCardLevelHP(CardId.Knight, 10), cloned.MaxHP);
        Assert.AreEqual(cloned.CurrentHP, cloned.MaxHP); // Full HP
    }
}
```

### 7.2 Network Edge Cases
```typescript
describe('Network Edge Cases', () => {
    test('late input arrival after battle end', async () => {
        const client = await connectClient(server, 'player1');
        await client.matchmake(BattleType.Practice);
        await client.waitForBattleEnd();
        
        // Send input after battle ended
        const result = await client.playCard(CardId.Knight, { x: 9, y: 8 });
        expect(result.success).toBe(false);
        expect(result.error).toBe('BATTLE_ENDED');
    });
    
    test('desync detection and recovery', async () => {
        const client = await connectClient(server, 'player1');
        await client.matchmake(BattleType.Practice);
        await client.waitForBattleStart();
        
        // Force desync by modifying local state
        client.simulation.player1.elixir = 100; // Cheat
        
        // Server should detect and reconcile
        await wait(5000);
        
        const state = await client.waitForGameState(2000);
        expect(state.player1.elixir).toBeLessThan(20); // Back to valid range
    });
});
```

---

## 8. TEST DATA MANAGEMENT

### 8.1 Test Fixtures
```csharp
public static class TestFixtures
{
    public static BattleSimulation CreateSimulation(ulong seed = 12345)
    {
        var sim = new BattleSimulation(seed);
        sim.Initialize(TestDecks.BalancedDeck, TestDecks.BalancedDeck);
        return sim;
    }
    
    public static BattleSimulation CreateTournamentSimulation()
    {
        var sim = CreateSimulation();
        sim.SetTournamentRules(true); // Level 11, no card level advantage
        return sim;
    }
    
    public static Deck BalancedDeck => new Deck
    {
        Cards = new[] {
            CardId.Knight, CardId.Archers, CardId.Giant, CardId.Musketeer,
            CardId.Fireball, CardId.Cannon, CardId.Skeletons, CardId.Minions
        }
    };
    
    public static Deck SpellHeavyDeck => new Deck
    {
        Cards = new[] {
            CardId.Fireball, CardId.Zap, CardId.Log, CardId.Poison,
            CardId.Rocket, CardId.Arrows, CardId.Freeze, CardId.Tornado
        }
    };
}
```

### 8.2 Test Data Generation
```python
# generate_test_data.py
import json
import random

def generate_battle_scenarios():
    scenarios = []
    
    # Scenario: Tank + Support vs Swarm
    scenarios.append({
        "name": "Tank vs Swarm",
        "player1_deck": ["Giant", "Musketeer", "Wizard", "Fireball", "Zap", "Cannon", "Skeletons", "Minions"],
        "player2_deck": ["Goblin Gang", "Skeleton Army", "Bats", "Minion Horde", "Log", "Tornado", "Knight", "Ice Spirit"],
        "expected_winner": "player1", # Tank beats swarm with splash
        "max_duration": 120
    })
    
    # ... more scenarios
    
    with open('test_scenarios.json', 'w') as f:
        json.dump(scenarios, f, indent=2)
```

---

## 9. TEST AUTOMATION PIPELINE

### 9.1 CI/CD Integration (GitHub Actions)
```yaml
# .github/workflows/test.yml
name: Test Suite

on: [push, pull_request]

jobs:
  unit-tests:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - name: Setup Unity
        uses: game-ci/unity-installer@v2
        with:
          unityVersion: 2022.3.20f1
      - name: Run Unit Tests
        run: |
          /opt/unity/Editor/Unity -batchmode -runTests \
            -projectPath . \
            -testResults results.xml \
            -logFile -
      - name: Upload Results
        uses: actions/upload-artifact@v4
        with:
          name: test-results
          path: results.xml
  
  integration-tests:
    runs-on: ubuntu-latest
    services:
      mysql:
        image: mysql:8.0
        env:
          MYSQL_DATABASE: crclone_test
          MYSQL_ROOT_PASSWORD: test
        ports: [3306:3306]
      redis:
        image: redis:7-alpine
        ports: [6379:6379]
    steps:
      - uses: actions/checkout@v4
      - name: Setup Node.js
        uses: actions/setup-node@v4
        with:
          node-version: '20'
      - name: Install Server Deps
        run: cd server && npm ci
      - name: Run Integration Tests
        run: cd server && npm run test:integration
  
  e2e-tests:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - name: Build Client
        run: ./build-client.sh
      - name: Start Test Environment
        run: docker-compose -f docker-compose.test.yml up -d
      - name: Run Playwright Tests
        run: npx playwright test
  
  performance-tests:
    runs-on: ubuntu-latest
    if: github.event_name == 'schedule' || github.event_name == 'workflow_dispatch'
    steps:
      - uses: actions/checkout@v4
      - name: Run k6 Load Test
        run: k6 run load-test.js
```

---

## 10. TEST REPORTING

### 10.1 Metrics Dashboard
- **Unit Test Coverage**: Codecov/CodeClimate
- **Integration Test Results**: JUnit XML → Jenkins/GitHub Actions
- **Performance Trends**: Grafana + InfluxDB
- **Flaky Test Detection**: Custom analysis of historical runs

### 10.2 Quality Gates
| Gate | Threshold |
|------|-----------|
| Unit Test Pass Rate | 100% |
| Integration Test Pass Rate | 100% |
| Code Coverage | > 80% |
| No Critical Bugs | 0 |
| Performance Regression | < 5% slower |
| Memory Leak | 0 bytes/frame growth |

---

## 11. TEST EXECUTION CHECKLIST

### Pre-Commit
- [ ] Unit tests pass locally
- [ ] Lint/TypeCheck pass
- [ ] No console errors in manual test

### CI Pipeline
- [ ] Unit tests (client + server)
- [ ] Integration tests (database, network)
- [ ] Static analysis
- [ ] Dependency vulnerability scan

### Pre-Release
- [ ] Full E2E suite
- [ ] Performance benchmarks
- [ ] Load test (staging)
- [ ] Security scan
- [ ] Replay determinism verification
- [ ] Balance regression suite

### Post-Release
- [ ] Monitor error rates
- [ ] Track desync incidents
- [ ] Verify telemetry matches expectations
- [ ] Schedule regression test run

---

*This test specification provides comprehensive coverage for all game systems. Each test should be implemented as automated verification where possible.*