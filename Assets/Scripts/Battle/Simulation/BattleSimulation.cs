using System;
using System.Collections.Generic;
using UnityEngine;
using CRClone.Core;
using FixedMath = CRClone.Core.Math;
using CRClone.Data;
using CRClone.Network;
// AGENT6-COMPAT: CRClone.Core and CRClone.Network both declare BattleStatus,
// EntityType, CardType and CardRarity with DIFFERENT values. The simulation
// logic below was written against the Core (gameplay) values, so pin the
// Core meanings explicitly. Network message types (PlayerInput, InputType,
// GameStateMessage) resolve via the CRClone.Network using above.
// TODO(Agent 1/2): de-duplicate the Core/Network enums properly.
using BattleStatus = CRClone.Core.BattleStatus;
using EntityType = CRClone.Core.EntityType;
using CardType = CRClone.Core.CardType;
using CardRarity = CRClone.Core.CardRarity;

namespace CRClone.Battle.Simulation
{
    public class BattleSimulation : IDisposable
    {
        public const int TICK_RATE = 60;
        public const float FIXED_DT = 1f / TICK_RATE;
        public const int MAX_ENTITIES = 500;

        // Game state
        internal GameConfig _config;
        internal ulong _seed;
        internal FixedMath.DeterministicRNG _rng;
        private uint _currentTick = 0;
        private uint _serverTick = 0;
        private BattleStatus _status = BattleStatus.Waiting;

        // Players
        internal PlayerState _player1;
        internal PlayerState _player2;

        // Entities
        internal readonly Dictionary<uint, Entity> _entities = new();
        internal readonly List<Unit> _units = new();
        internal readonly List<Building> _buildings = new();
        internal readonly List<Projectile> _projectiles = new();
        internal readonly List<SpellEffect> _activeSpells = new();
        internal readonly List<Tower> _towers = new();

        // Entity ID allocation
        internal uint _nextEntityId = 1000; // Start after tower IDs

        // Input queues
        // AGENT6-COMPAT: NetworkClient has no nested PlayerInput type; the
        // message type is CRClone.Network.PlayerInput (MessageTypes.cs).
        private readonly Queue<PlayerInput> _p1Inputs = new();
        private readonly Queue<PlayerInput> _p2Inputs = new();

        // Events for replay
        private readonly List<BattleEvent> _eventLog = new();
        private readonly List<ReplayEvent> _replayLog = new();

        // Pathfinding
        internal Pathfinding _pathfinding;

        // Collision grid dirty flag
        private bool _collisionGridDirty = true;

        public BattleStatus Status => _status;
        public uint CurrentTick => _currentTick;
        public PlayerState Player1 => _player1;
        public PlayerState Player2 => _player2;
        public IReadOnlyList<Unit> Units => _units;
        public IReadOnlyList<Building> Buildings => _buildings;
        public IReadOnlyList<Projectile> Projectiles => _projectiles;
        public IReadOnlyList<SpellEffect> ActiveSpells => _activeSpells;
        public IReadOnlyList<Tower> Towers => _towers;
        public IReadOnlyList<BattleEvent> EventLog => _eventLog;

        public uint AllocateEntityId() { return _nextEntityId++; }
        public void RegisterUnit(Unit unit) { _units.Add(unit); _entities[unit.Id] = unit; }
        public void RegisterBuilding(Building building) { _buildings.Add(building); _entities[building.Id] = building; _collisionGridDirty = true; }
        public double NextRandomDouble() { return _rng.NextDouble(); }
        internal void LogBattleEvent(BattleEvent evt) { _eventLog.Add(evt); }

        public void Initialize(GameConfig config, ulong seed, int[] p1Deck, int[] p2Deck)
        {
            _config = config;
            _seed = seed;
            _rng = new FixedMath.DeterministicRNG(seed);
            _currentTick = 0;
            _serverTick = 0;
            _status = BattleStatus.Playing;

            // Initialize players
            _player1 = new PlayerState(1, p1Deck, _config);
            _player2 = new PlayerState(2, p2Deck, _config);

            // Initialize pathfinding
            _pathfinding = new Pathfinding();
            _pathfinding.Initialize();

            // Create towers
            CreateTowers();

            // Initial elixir
            _player1.Elixir = _config.startingElixir;
            _player2.Elixir = _config.startingElixir;

            LogEvent(new BattleEvent { tick = 0, type = EventType.BattleStart });
            LogReplayEvent(new ReplayEvent { tick = 0, type = ReplayEventType.BattleStart });
        }

        private void CreateTowers()
        {
            // Player 1 towers (bottom)
            var p1King = new Tower(_nextEntityId++, 1, EntityType.Tower, TowerType.King, GameConstants.P1_KING_POS, _config.kingTowerHP, _config.towerDamage, _config.towerHitSpeed, _config.towerRange);
            var p1PrincessL = new Tower(_nextEntityId++, 1, EntityType.Tower, TowerType.PrincessLeft, GameConstants.P1_PRINCESS_LEFT_POS, _config.princessTowerHP, _config.towerDamage, _config.towerHitSpeed, _config.towerRange);
            var p1PrincessR = new Tower(_nextEntityId++, 1, EntityType.Tower, TowerType.PrincessRight, GameConstants.P1_PRINCESS_RIGHT_POS, _config.princessTowerHP, _config.towerDamage, _config.towerHitSpeed, _config.towerRange);

            // Player 2 towers (top)
            var p2King = new Tower(_nextEntityId++, 2, EntityType.Tower, TowerType.King, GameConstants.P2_KING_POS, _config.kingTowerHP, _config.towerDamage, _config.towerHitSpeed, _config.towerRange);
            var p2PrincessL = new Tower(_nextEntityId++, 2, EntityType.Tower, TowerType.PrincessLeft, GameConstants.P2_PRINCESS_LEFT_POS, _config.princessTowerHP, _config.towerDamage, _config.towerHitSpeed, _config.towerRange);
            var p2PrincessR = new Tower(_nextEntityId++, 2, EntityType.Tower, TowerType.PrincessRight, GameConstants.P2_PRINCESS_RIGHT_POS, _config.princessTowerHP, _config.towerDamage, _config.towerHitSpeed, _config.towerRange);

            _towers.Add(p1King); _towers.Add(p1PrincessL); _towers.Add(p1PrincessR);
            _towers.Add(p2King); _towers.Add(p2PrincessL); _towers.Add(p2PrincessR);

            foreach (var t in _towers) _entities[t.Id] = t;
        }

        public void Tick(float dt = FIXED_DT)
        {
            if (_status != BattleStatus.Playing) return;

            _currentTick++;

            // 1. Process inputs
            ProcessInputs();

            // 2. Elixir generation
            UpdateElixir(dt);

            // 3. Update active spells
            UpdateSpells(dt);

            // 4. Update projectiles
            UpdateProjectiles(dt);

            // 5. Update units (movement, targeting, attacks)
            UpdateUnits(dt);

            // 6. Update buildings
            UpdateBuildings(dt);

            // 7. Update pathfinding collision grid (when buildings change)
            if (_collisionGridDirty)
            {
                _pathfinding.UpdateBuildingCollision(_buildings);
                _collisionGridDirty = false;
            }

            // 8. Update towers
            UpdateTowers(dt);

            // 9. Collision resolution
            ResolveCollisions();

            // 10. Process deaths
            ProcessDeaths();

            // 11. Check win conditions
            CheckWinCondition();

            // 12. Record events for replay
            RecordTickEvents();
        }

        private void ProcessInputs()
        {
            // Process Player 1 inputs
            while (_p1Inputs.Count > 0)
            {
                var input = _p1Inputs.Dequeue();
                ApplyInput(1, input);
            }

            // Process Player 2 inputs
            while (_p2Inputs.Count > 0)
            {
                var input = _p2Inputs.Dequeue();
                ApplyInput(2, input);
            }
        }

        public void QueueInput(int playerId, PlayerInput input)
        {
            if (playerId == 1) _p1Inputs.Enqueue(input);
            else if (playerId == 2) _p2Inputs.Enqueue(input);
        }

        private void ApplyInput(int playerId, PlayerInput input)
        {
            var player = playerId == 1 ? _player1 : _player2;
            var opponent = playerId == 1 ? _player2 : _player1;

            switch (input.type)
            {
                case InputType.PlayCard:
                    PlayCard(player, opponent, (int)input.cardId, input.position);
                    break;
                case InputType.CastSpell:
                    CastSpell(player, opponent, (int)input.spellId, input.position);
                    break;
                // AGENT6-COMPAT: enum value is ChampionAbility (MessageTypes.cs), not UseChampionAbility.
                case InputType.ChampionAbility:
                    UseChampionAbility(player, opponent, input.position);
                    break;
            }
        }

        private void PlayCard(PlayerState player, PlayerState opponent, int cardId, Vector2 position)
        {
            var cardData = Services.Get<DataManager>().GetCard(cardId);
            if (cardData == null) return;

            // Validate elixir
            var stats = cardData.GetStats(1); // Base level, actual level from collection
            int cost = cardData.elixirCost;
            if (player.Elixir < cost) return;

            // Validate position
            if (!IsValidDeployPosition(player.PlayerId, position, cardData)) return;

            // Deduct elixir
            int prevElixir = player.Elixir;
            player.Elixir -= cost;

            // Emit ElixirChanged event for card play
            EventBus.Raise(new EventBus.ElixirChangedEvent
            {
                playerId = player.PlayerId,
                currentElixir = player.Elixir,
                previousElixir = prevElixir,
                reason = EventBus.ElixirChangeReason.CardPlayed
            });

            // Track last non-Mirror card for Mirror spell
            if (cardData.type != CardType.Spell || cardData.cardName != "Mirror")
            {
                player.LastPlayedCardId = cardId;
            }

            // Spawn entity based on card type
            switch (cardData.type)
            {
                case CardType.Troop:
                case CardType.Champion:
                    SpawnUnit(cardData, player.PlayerId, position, stats.level);
                    break;
                case CardType.Building:
                    SpawnBuilding(cardData, player.PlayerId, position, stats.level);
                    break;
                case CardType.Spell:
                    CastSpell(player, opponent, cardId, position);
                    break;
            }

            // Draw next card
            player.DrawCard();

            LogEvent(new BattleEvent
            {
                tick = _currentTick,
                type = EventType.CardPlayed,
                playerId = player.PlayerId,
                cardId = cardId,
                position = position
            });

            LogReplayEvent(new ReplayEvent
            {
                tick = _currentTick,
                type = ReplayEventType.CardPlayed,
                playerId = player.PlayerId,
                cardId = cardId,
                position = new FixedMath.FixedVector2(position),
                elixir = player.Elixir
            });

            // Emit EventBus event
            EventBus.Raise(new EventBus.CardPlayedEvent
            {
                playerId = player.PlayerId,
                cardId = cardId,
                position = position,
                elixirCost = cost,
                tick = _currentTick
            });
        }

        private bool IsValidDeployPosition(int playerId, Vector2 position, CardData card)
        {
            // Check deploy zone expansion when princess tower destroyed
            bool princessLeftDead = false, princessRightDead = false;
            foreach (var tower in _towers)
            {
                if (tower.OwnerPlayerId == playerId)
                {
                    if (tower.Type == TowerType.PrincessLeft && tower.IsDead) princessLeftDead = true;
                    if (tower.Type == TowerType.PrincessRight && tower.IsDead) princessRightDead = true;
                }
            }
            bool expandedDeploy = princessLeftDead || princessRightDead;

            float deployZoneMaxY = playerId == 1 
                ? (expandedDeploy ? GameConstants.DEPLOY_ZONE_Y_P1_MAX - 4f : GameConstants.DEPLOY_ZONE_Y_P1_MAX)
                : (expandedDeploy ? GameConstants.DEPLOY_ZONE_Y_P2_MIN + 4f : GameConstants.DEPLOY_ZONE_Y_P2_MIN);

            // Check if in deploy zone
            if (playerId == 1 && position.y > deployZoneMaxY) return false;
            if (playerId == 2 && position.y < deployZoneMaxY) return false;

            // Spells can be placed anywhere
            if (card.type == CardType.Spell) return true;

            // Buildings must be on own side of river
            if (card.type == CardType.Building)
            {
                if (playerId == 1 && position.y > GameConstants.RIVER_Y_MIN) return false;
                if (playerId == 2 && position.y < GameConstants.RIVER_Y_MAX) return false;
            }

            // Ground units cannot be placed across river
            if (card.type == CardType.Troop || card.type == CardType.Champion)
            {
                // Flying units can be placed anywhere in deploy zone
                bool isFlying = card.mechanicsJson?.Contains("flying") == true ||
                                card.cardName.Contains("Minion") || card.cardName.Contains("Bat") ||
                                card.cardName.Contains("Dragon") || card.cardName.Contains("Balloon") ||
                                card.cardName.Contains("Phoenix") || card.cardName.Contains("Lava Hound") ||
                                card.cardName.Contains("Skeleton Dragon") || card.cardName.Contains("Mega Minion");
                
                if (!isFlying)
                {
                    // Ground units must be on own side
                    if (playerId == 1 && position.y > GameConstants.RIVER_Y_MIN) return false;
                    if (playerId == 2 && position.y < GameConstants.RIVER_Y_MAX) return false;
                }
            }

            // Footprint-overlap rejection (MECHANICS.md deploy rules, GAP-1.4).
            // Spells stay exempt (returned earlier). Towers/buildings/units block.
            float newRadius = card.type == CardType.Building ? GetBuildingFootprintRadius(card.cardName) : 0.5f;
            foreach (var b in _buildings)
            {
                if (b.IsDead) continue;
                if (Vector2.Distance(position, b.Position) < newRadius + b.CollisionRadius) return false;
            }
            foreach (var t in _towers)
            {
                if (t.IsDead) continue;
                if (Vector2.Distance(position, t.Position) < newRadius + t.CollisionRadius) return false;
            }
            foreach (var u in _units)
            {
                if (u.IsDead) continue;
                if (Vector2.Distance(position, u.Position) < newRadius + u.CollisionRadius) return false;
            }

            return true;
        }

        private float GetBuildingFootprintRadius(string cardName)
        {
            return cardName switch
            {
                "X-Bow" => 2f,      // 4x4
                "Mortar" => 2f,     // 4x4
                "Elixir Collector" => 1.5f, // 3x3
                _ => 1f             // 2x2 default
            };
        }

        public void SpawnUnit(CardData card, int playerId, Vector2 position, int level)
        {
            var stats = card.GetStats(level);
            var unit = new Unit(_nextEntityId++, playerId, card, stats, position, level);
            _units.Add(unit);
            _entities[unit.Id] = unit;

            // Find initial target
            unit.AcquireTarget(GetPotentialTargets(unit), this);

            LogEvent(new BattleEvent
            {
                tick = _currentTick,
                type = EventType.UnitSpawned,
                playerId = playerId,
                cardId = card.cardId,
                position = position
            });

            LogReplayEvent(new ReplayEvent
            {
                tick = _currentTick,
                type = ReplayEventType.UnitSpawned,
                playerId = playerId,
                cardId = card.cardId,
                entityId = unit.Id,
                position = new FixedMath.FixedVector2(position),
                hpRemaining = unit.CurrentHP
            });

            // Emit EventBus event
            EventBus.Raise(new EventBus.UnitSpawnedEvent
            {
                entityId = unit.Id,
                playerId = playerId,
                cardId = card.cardId,
                position = position,
                level = level
            });
        }

        public void SpawnBuilding(CardData card, int playerId, Vector2 position, int level)
        {
            var stats = card.GetStats(level);
            var building = new Building(_nextEntityId++, playerId, card, stats, position, level);
            _buildings.Add(building);
            _entities[building.Id] = building;
            _collisionGridDirty = true;

            LogEvent(new BattleEvent
            {
                tick = _currentTick,
                type = EventType.BuildingPlaced,
                playerId = playerId,
                cardId = card.cardId,
                position = position
            });

            LogReplayEvent(new ReplayEvent
            {
                tick = _currentTick,
                type = ReplayEventType.BuildingPlaced,
                playerId = playerId,
                cardId = card.cardId,
                entityId = building.Id,
                position = new FixedMath.FixedVector2(position),
                hpRemaining = building.CurrentHP
            });

            // Emit EventBus event
            EventBus.Raise(new EventBus.BuildingPlacedEvent
            {
                entityId = building.Id,
                playerId = playerId,
                cardId = card.cardId,
                position = position,
                lifetime = building.MaxLifetime
            });
        }

        private void CastSpell(PlayerState player, PlayerState opponent, int spellId, Vector2 position)
        {
            var cardData = Services.Get<DataManager>().GetCard(spellId);
            if (cardData == null || cardData.type != CardType.Spell) return;

            // Validate elixir (already done in ApplyInput)

            // Handle Mirror spell - mirrors last played card at +1 level
            if (cardData.cardName == "Mirror")
            {
                if (player.LastPlayedCardId <= 0) return;
                
                var lastCard = Services.Get<DataManager>().GetCard(player.LastPlayedCardId);
                if (lastCard == null) return;
                
                // Mirror costs last card elixir + 1
                int mirrorCost = lastCard.elixirCost + 1;
                if (player.Elixir < mirrorCost) return;
                
                player.Elixir -= mirrorCost;
                
                // Play the mirrored card at +1 level
                if (lastCard.type == CardType.Troop || lastCard.type == CardType.Champion)
                {
                    SpawnUnit(lastCard, player.PlayerId, position, Math.Min(lastCard.GetStats(1).level + 1, 14));
                }
                else if (lastCard.type == CardType.Building)
                {
                    SpawnBuilding(lastCard, player.PlayerId, position, Math.Min(lastCard.GetStats(1).level + 1, 14));
                }
                else if (lastCard.type == CardType.Spell)
                {
                    CastSpell(player, opponent, lastCard.cardId, position);
                }
                
                player.DrawCard();
                
                LogEvent(new BattleEvent
                {
                    tick = _currentTick,
                    type = EventType.CardPlayed,
                    playerId = player.PlayerId,
                    cardId = spellId,
                    position = position
                });
                
                LogReplayEvent(new ReplayEvent
                {
                    tick = _currentTick,
                    type = ReplayEventType.CardPlayed,
                    playerId = player.PlayerId,
                    cardId = spellId,
                    position = new FixedMath.FixedVector2(position),
                    elixir = player.Elixir
                });
                
                return;
            }

            var spellEffect = SpellEffect.CreateFromCard(_nextEntityId++, player.PlayerId, cardData, position, 1);
            if (spellEffect != null)
            {
                _activeSpells.Add(spellEffect);
                _entities[spellEffect.Id] = spellEffect;

                LogEvent(new BattleEvent
                {
                    tick = _currentTick,
                    type = EventType.SpellCast,
                    playerId = player.PlayerId,
                    cardId = spellId,
                    position = position
                });

                LogReplayEvent(new ReplayEvent
                {
                    tick = _currentTick,
                    type = ReplayEventType.SpellCast,
                    playerId = player.PlayerId,
                    cardId = spellId,
                    entityId = spellEffect.Id,
                    position = new FixedMath.FixedVector2(position)
                });

                // Emit EventBus event
                EventBus.Raise(new EventBus.SpellCastEvent
                {
                    playerId = player.PlayerId,
                    spellId = spellId,
                    position = position,
                    targetPosition = position,
                    tick = _currentTick
                });
            }
        }

        private void UseChampionAbility(PlayerState player, PlayerState opponent, Vector2 position)
        {
            // Find champion on field
            foreach (var unit in _units)
            {
                if (unit.OwnerPlayerId == player.PlayerId && unit.CardData.rarity == CardRarity.Champion)
                {
                    if (unit.TryUseAbility(position, this))
                    {
                        int abilityCost = GetChampionAbilityCost(unit.CardData.cardId);
                        player.Elixir -= abilityCost;

                        LogEvent(new BattleEvent
                        {
                            tick = _currentTick,
                            type = EventType.ChampionAbility,
                            playerId = player.PlayerId,
                            cardId = unit.CardData.cardId,
                            position = position
                        });

                        LogReplayEvent(new ReplayEvent
                        {
                            tick = _currentTick,
                            type = ReplayEventType.ChampionAbility,
                            playerId = player.PlayerId,
                            cardId = unit.CardData.cardId,
                            entityId = unit.Id,
                            position = new FixedMath.FixedVector2(position),
                            elixir = player.Elixir
                        });
                    }
                    break;
                }
            }
        }

        private int GetChampionAbilityCost(int championCardId)
        {
            return championCardId switch
            {
                // Archer Queen, Skeleton King, Mighty Miner
                27000000 => 3, // Archer Queen
                27000001 => 2, // Skeleton King
                27000002 => 2, // Mighty Miner
                _ => 2
            };
        }

        // AGENT6-FIX (review: Agent 1): fractional elixir accumulators. The old
        // code added one tick's fraction to the integer Elixir and floored it,
        // discarding the fraction every tick, so elixir NEVER regenerated.
        private FixedMath.Fixed _p1ElixirFrac = FixedMath.Fixed.Zero;
        private FixedMath.Fixed _p2ElixirFrac = FixedMath.Fixed.Zero;

        private void UpdateElixir(float dt)
        {
            FixedMath.Fixed rate = GetElixirRate();
            FixedMath.Fixed elixirPerTick = FixedMath.Fixed.FromFloat(FIXED_DT) / rate;

            int prevElixir1 = _player1.Elixir;
            int prevElixir2 = _player2.Elixir;

            AccumulateElixir(_player1, ref _p1ElixirFrac, elixirPerTick);
            AccumulateElixir(_player2, ref _p2ElixirFrac, elixirPerTick);

            // Emit ElixirChanged events
            if (_player1.Elixir != prevElixir1)
            {
                EventBus.Raise(new EventBus.ElixirChangedEvent
                {
                    playerId = 1,
                    currentElixir = _player1.Elixir,
                    previousElixir = prevElixir1,
                    reason = EventBus.ElixirChangeReason.Generation
                });
            }
            if (_player2.Elixir != prevElixir2)
            {
                EventBus.Raise(new EventBus.ElixirChangedEvent
                {
                    playerId = 2,
                    currentElixir = _player2.Elixir,
                    previousElixir = prevElixir2,
                    reason = EventBus.ElixirChangeReason.Generation
                });
            }

            // Elixir collector production handled in building update
        }

        private void AccumulateElixir(PlayerState player, ref FixedMath.Fixed frac, FixedMath.Fixed perTick)
        {
            if (player.Elixir >= _config.maxElixir)
            {
                frac = FixedMath.Fixed.Zero; // Bar full: no phantom accumulation
                return;
            }
            frac += perTick;
            int whole = frac.ToInt();
            if (whole > 0)
            {
                player.Elixir = Math.Min(_config.maxElixir, player.Elixir + whole);
                frac -= FixedMath.Fixed.FromInt(whole);
            }
        }

        private FixedMath.Fixed GetElixirRate()
        {
            float elapsed = _currentTick * FIXED_DT;
            if (elapsed >= _config.battleDuration + _config.overtimeDuration) return FixedMath.Fixed.FromFloat(_config.tripleElixirRate);
            if (elapsed >= _config.battleDuration) return FixedMath.Fixed.FromFloat(_config.doubleElixirRate);
            return FixedMath.Fixed.FromFloat(_config.elixirGenerationRate);
        }

        private void UpdateSpells(float dt)
        {
            for (int i = _activeSpells.Count - 1; i >= 0; i--)
            {
                var spell = _activeSpells[i];
                spell.Tick(dt, this);
                if (spell.IsFinished)
                {
                    _activeSpells.RemoveAt(i);
                    _entities.Remove(spell.Id);
                }
            }
        }

        private void UpdateProjectiles(float dt)
        {
            for (int i = _projectiles.Count - 1; i >= 0; i--)
            {
                var proj = _projectiles[i];
                proj.Tick(dt, this);
                if (proj.IsDead)
                {
                    _projectiles.RemoveAt(i);
                    _entities.Remove(proj.Id);
                }
            }
        }

        private void UpdateUnits(float dt)
        {
            foreach (var unit in _units)
            {
                if (unit.IsDead) continue;
                unit.Tick(dt, this);
            }
        }

        private void UpdateBuildings(float dt)
        {
            for (int i = _buildings.Count - 1; i >= 0; i--)
            {
                var building = _buildings[i];
                if (building.IsDead) continue;
                building.Tick(dt, this);
                
                if (building.IsExpired)
                {
                    building.Die(DeathCause.LifetimeExpired);
                }
            }
        }

        private void UpdateTowers(float dt)
        {
            foreach (var tower in _towers)
            {
                if (tower.IsDead) continue;
                tower.Tick(dt, this);
            }
        }

        private void ResolveCollisions()
        {
            // Simple push-based collision for units
            for (int i = 0; i < _units.Count; i++)
            {
                var a = _units[i];
                if (a.IsDead) continue;

                for (int j = i + 1; j < _units.Count; j++)
                {
                    var b = _units[j];
                    if (b.IsDead) continue;

                    float dist = Vector2.Distance(a.Position, b.Position);
                    float minDist = a.CollisionRadius + b.CollisionRadius;

                    if (dist < minDist && dist > 0.01f)
                    {
                        Vector2 pushDir = (a.Position - b.Position).normalized;
                        float overlap = (minDist - dist) * 0.5f;
                        a.Position += pushDir * overlap;
                        b.Position -= pushDir * overlap;
                    }
                }
            }
        }

        private void ProcessDeaths()
        {
            // Process unit deaths
            for (int i = _units.Count - 1; i >= 0; i--)
            {
                if (_units[i].IsDead)
                {
                    var unit = _units[i];
                    unit.OnDeath(this);
                    _entities.Remove(unit.Id);
                    _units.RemoveAt(i);

                    LogReplayEvent(new ReplayEvent
                    {
                        tick = _currentTick,
                        type = ReplayEventType.UnitDied,
                        playerId = unit.OwnerPlayerId,
                        cardId = unit.CardData.cardId,
                        entityId = unit.Id,
                        position = new FixedMath.FixedVector2(unit.Position)
                    });

                    // Emit EventBus event
                    EventBus.Raise(new EventBus.UnitDiedEvent
                    {
                        entityId = unit.Id,
                        playerId = unit.OwnerPlayerId,
                        cardId = unit.CardData.cardId,
                        position = unit.Position,
                        cause = EventBus.DeathCause.Damage
                    });
                }
            }

            // Process building deaths
            for (int i = _buildings.Count - 1; i >= 0; i--)
            {
                if (_buildings[i].IsDead)
                {
                    var building = _buildings[i];
                    building.OnDeath(this);
                    _entities.Remove(building.Id);
                    _buildings.RemoveAt(i);
                    _collisionGridDirty = true;

                    LogReplayEvent(new ReplayEvent
                    {
                        tick = _currentTick,
                        type = ReplayEventType.BuildingDestroyed,
                        playerId = building.OwnerPlayerId,
                        cardId = building.CardData.cardId,
                        entityId = building.Id,
                        position = new FixedMath.FixedVector2(building.Position)
                    });

                    // Emit EventBus event
                    EventBus.Raise(new EventBus.BuildingDestroyedEvent
                    {
                        entityId = building.Id,
                        playerId = building.OwnerPlayerId,
                        cardId = building.CardData.cardId,
                        cause = EventBus.DeathCause.Damage
                    });
                }
            }
        }

        private void CheckWinCondition()
        {
            bool p1KingDead = false, p2KingDead = false;
            int p1Crowns = 0, p2Crowns = 0;
            bool p1PrincessLeftDead = false, p1PrincessRightDead = false;
            bool p2PrincessLeftDead = false, p2PrincessRightDead = false;

            foreach (var tower in _towers)
            {
                // Crowns earned = enemy towers destroyed.
                if (tower.OwnerPlayerId == 1)
                {
                    if (tower.Type == TowerType.King && tower.IsDead) p1KingDead = true;
                    if (tower.Type == TowerType.PrincessLeft && tower.IsDead) { p1PrincessLeftDead = true; p2Crowns++; }
                    if (tower.Type == TowerType.PrincessRight && tower.IsDead) { p1PrincessRightDead = true; p2Crowns++; }
                }
                else
                {
                    if (tower.Type == TowerType.King && tower.IsDead) p2KingDead = true;
                    if (tower.Type == TowerType.PrincessLeft && tower.IsDead) { p2PrincessLeftDead = true; p1Crowns++; }
                    if (tower.Type == TowerType.PrincessRight && tower.IsDead) { p2PrincessRightDead = true; p1Crowns++; }
                }
            }

            bool isOvertime = _currentTick >= _config.battleDuration * TICK_RATE;
            float battleEndTime = (_config.battleDuration + _config.overtimeDuration) * TICK_RATE;
            bool pastOvertimeEnd = _currentTick >= battleEndTime;

            BattleStatus newStatus = _status;

            // King tower destroyed = instant 3-crown win
            if (p1KingDead || p2KingDead)
            {
                newStatus = p1KingDead ? BattleStatus.Player2Won : BattleStatus.Player1Won;
                p1Crowns = p1KingDead ? 0 : 3;
                p2Crowns = p2KingDead ? 0 : 3;
            }
            else if (!isOvertime)
            {
                // Regulation time still running: keep playing.
                newStatus = BattleStatus.Playing;
            }
            else if (!pastOvertimeEnd)
            {
                // Regulation-expiry crown compare + overtime sudden death (GAP-1.5):
                // the moment regulation ends with unequal crowns, the leader wins
                // immediately; any later first-tower-destroyed during overtime wins.
                if (p1Crowns != p2Crowns)
                {
                    newStatus = p1Crowns > p2Crowns ? BattleStatus.Player1Won : BattleStatus.Player2Won;
                }
            }
            else
            {
                // Overtime timeout: lowest-tower-HP tiebreak (GAP-1.5).
                // Sum surviving tower HP per side; higher total wins; exact equal = draw.
                if (p1Crowns != p2Crowns)
                {
                    newStatus = p1Crowns > p2Crowns ? BattleStatus.Player1Won : BattleStatus.Player2Won;
                }
                else
                {
                    float hp1 = 0f, hp2 = 0f;
                    foreach (var tower in _towers)
                    {
                        if (tower.IsDead) continue;
                        if (tower.OwnerPlayerId == 1) hp1 += tower.CurrentHP;
                        else hp2 += tower.CurrentHP;
                    }
                    if (hp1 > hp2) newStatus = BattleStatus.Player1Won;
                    else if (hp2 > hp1) newStatus = BattleStatus.Player2Won;
                    else newStatus = BattleStatus.Draw;
                }
            }

            if (newStatus != _status && newStatus != BattleStatus.Playing)
            {
                _status = newStatus;
                
                // Emit BattleEnded event
                EventBus.Raise(new EventBus.BattleEndedEvent
                {
                    result = _status,
                    player1Crowns = p1Crowns,
                    player2Crowns = p2Crowns,
                    player1TrophyChange = 0, // Calculated by server
                    player2TrophyChange = 0,
                    duration = _currentTick * FIXED_DT,
                    wentOvertime = isOvertime,
                    replayId = 0 // Will be set by server
                });

                LogReplayEvent(new ReplayEvent
                {
                    tick = _currentTick,
                    type = ReplayEventType.BattleEnd,
                    playerId = _status == BattleStatus.Player1Won ? 1 : 
                             _status == BattleStatus.Player2Won ? 2 : 0
                });
            }
        }

        private void RecordTickEvents()
        {
            // Record significant state changes for replay
            // Elixir changes
            LogReplayEvent(new ReplayEvent
            {
                tick = _currentTick,
                type = ReplayEventType.ElixirChanged,
                playerId = 1,
                elixir = _player1.Elixir
            });
            LogReplayEvent(new ReplayEvent
            {
                tick = _currentTick,
                type = ReplayEventType.ElixirChanged,
                playerId = 2,
                elixir = _player2.Elixir
            });
        }

        private void LogEvent(BattleEvent evt)
        {
            _eventLog.Add(evt);
        }

        private void LogReplayEvent(ReplayEvent evt)
        {
            _replayLog.Add(evt);
        }

        public IReadOnlyList<ReplayEvent> GetReplayLog() => _replayLog;

        public List<Entity> GetPotentialTargets(Entity attacker)
        {
            var targets = new List<Entity>();
            int enemyPlayerId = attacker.OwnerPlayerId == 1 ? 2 : 1;

            // Add enemy units
            foreach (var u in _units)
            {
                if (u.OwnerPlayerId == enemyPlayerId && !u.IsDead)
                    targets.Add(u);
            }

            // Add enemy buildings
            foreach (var b in _buildings)
            {
                if (b.OwnerPlayerId == enemyPlayerId && !b.IsDead)
                    targets.Add(b);
            }

            // Add enemy towers
            foreach (var t in _towers)
            {
                if (t.OwnerPlayerId == enemyPlayerId && !t.IsDead)
                    targets.Add(t);
            }

            return targets;
        }

        public List<Entity> GetAllEntitiesInRadius(Vector2 center, float radius)
        {
            var entities = new List<Entity>();
            float radiusSq = radius * radius;

            foreach (var entity in _entities.Values)
            {
                if (entity.IsDead) continue;
                if (Vector2.Distance(center, entity.Position) <= radius + entity.CollisionRadius)
                {
                    entities.Add(entity);
                }
            }

            return entities;
        }

        public Entity GetEntity(uint id)
        {
            _entities.TryGetValue(id, out var entity);
            return entity;
        }

        public void AddProjectile(Projectile projectile)
        {
            _projectiles.Add(projectile);
            _entities[projectile.Id] = projectile;
        }

        // AGENT6-COMPAT: message type is CRClone.Network.GameStateMessage.
        public void Reconcile(GameStateMessage serverState)
        {
            _serverTick = serverState.tick;
            // TODO: Reconcile entity positions, HP, etc.
        }

        public void Dispose()
        {
            _entities.Clear();
            _units.Clear();
            _buildings.Clear();
            _projectiles.Clear();
            _activeSpells.Clear();
            _towers.Clear();
            _eventLog.Clear();
            _replayLog.Clear();
        }
    }

    // Supporting classes
    public class PlayerState
    {
        public int PlayerId { get; }
        public int Elixir { get; set; }
        public int[] Deck { get; }
        public int[] Hand { get; private set; }
        public int NextCardIndex { get; private set; }
        public bool KingTowerActivated { get; set; }
        public int LastPlayedCardId { get; set; } = -1; // For Mirror spell

        private readonly GameConfig _config;

        public PlayerState(int playerId, int[] deck, GameConfig config)
        {
            PlayerId = playerId;
            Deck = deck;
            _config = config;
            Hand = new int[_config.handSize];
            NextCardIndex = _config.handSize;
            DrawInitialHand();
        }

        private void DrawInitialHand()
        {
            for (int i = 0; i < _config.handSize; i++)
            {
                Hand[i] = Deck[i];
            }
            NextCardIndex = _config.handSize;
        }

        public void DrawCard()
        {
            if (NextCardIndex >= Deck.Length) NextCardIndex = 0;
            
            // Shift hand left
            for (int i = 0; i < _config.handSize - 1; i++)
            {
                Hand[i] = Hand[i + 1];
            }
            Hand[_config.handSize - 1] = Deck[NextCardIndex];
            NextCardIndex++;
        }
    }

    public struct BattleEvent
    {
        public uint tick;
        public EventType type;
        public int playerId;
        public int cardId;
        public Vector2 position;
    }

    public enum EventType
    {
        BattleStart,
        CardPlayed,
        UnitSpawned,
        UnitDied,
        BuildingPlaced,
        BuildingDestroyed,
        SpellCast,
        TowerDamaged,
        TowerDestroyed,
        KingActivated,
        ChampionAbility,
        BattleEnd
    }

    // Replay events for deterministic replay system
    public struct ReplayEvent
    {
        public uint tick;
        public ReplayEventType type;
        public int playerId;
        public int cardId;
        public int entityId;
        public FixedMath.FixedVector2 position;
        public int damage;
        public int hpRemaining;
        public int elixir;
    }

    public enum ReplayEventType
    {
        BattleStart,
        CardPlayed,
        UnitSpawned,
        UnitDied,
        BuildingPlaced,
        BuildingDestroyed,
        SpellCast,
        TowerDamaged,
        TowerDestroyed,
        KingActivated,
        ChampionAbility,
        BattleEnd,
        ElixirChanged,
        EntityStateSync
    }

    // Use FixedMath.DeterministicRNG from CRClone.Core.FixedMath