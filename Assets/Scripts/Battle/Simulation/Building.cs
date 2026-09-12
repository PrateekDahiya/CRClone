using System;
using System.Collections.Generic;
using UnityEngine;
using CRClone.Core;
using CRClone.Data;

namespace CRClone.Battle.Simulation
{
    public class Building : Entity
    {
        public CardData CardData { get; private set; }
        public CardLevelStats Stats { get; private set; }
        public int Level { get; private set; }
        public float Lifetime { get; private set; }
        public float MaxLifetime { get; private set; }
        public bool IsExpired => Lifetime <= 0;
        public Entity Target { get; private set; }
        public float AttackCooldown { get; private set; }
        public float AttackRange { get; private set; }
        public bool IsRetracted { get; private set; } // Tesla
        public bool IsInvulnerableWhileRetracted { get; private set; }
        public int SpawnWave { get; private set; } // For spawners
        public float SpawnTimer { get; private set; }
        public bool CanTargetAir { get; private set; }
        public bool CanTargetGround { get; private set; } = true;

        // Inferno Tower ramping
        private int _infernoRampStage = 0;
        private float _infernoTimeAtStage = 0f;
        private uint _infernoTargetId = 0;

        // Cannon Cart transformation
        private bool _cannonCartTransformed = false;

        public Building(uint id, int ownerPlayerId, CardData cardData, CardLevelStats stats, Vector2 position, int level)
            : base(id, ownerPlayerId, EntityType.Building, position, stats.hitpoints)
        {
            CardData = cardData;
            Stats = stats;
            Level = level;
            MaxLifetime = GetLifetime(cardData);
            Lifetime = MaxLifetime;
            AttackRange = stats.range;
            AttackCooldown = stats.hitSpeed;
            CollisionRadius = GetCollisionRadius(cardData);

            // Determine targeting capabilities
            DetermineTargeting(cardData);

            // Special building properties
            IsRetracted = cardData.cardName == "Tesla";
            IsInvulnerableWhileRetracted = IsRetracted;
        }

        private void DetermineTargeting(CardData cardData)
        {
            string name = cardData.cardName;
            
            // Most buildings target both air and ground
            CanTargetAir = true;
            CanTargetGround = true;

            // Exceptions
            if (name == "Cannon" || name == "Bomb Tower" || name == "Mortar" || name == "X-Bow")
            {
                CanTargetAir = false;
            }
            if (name == "Cannon Cart" && !_cannonCartTransformed)
            {
                CanTargetAir = false; // Mobile form: ground only
            }
        }

        private float GetLifetime(CardData card)
        {
            // Building lifetimes in seconds
            return card.cardName switch
            {
                "Cannon" => 30f,
                "Tesla" => 25f,
                "Bomb Tower" => 30f,
                "Inferno Tower" => 25f,
                "Mortar" => 30f,
                "X-Bow" => 30f,
                "Elixir Collector" => 70f,
                "Goblin Hut" => 30f,
                "Furnace" => 40f,
                "Tombstone" => 20f,
                "Goblin Cage" => 30f,
                "Goblin Drill" => 30f,
                "Cannon Cart" => 30f, // Until wheels destroyed
                _ => 30f
            };
        }

        private float GetCollisionRadius(CardData card)
        {
            // Building sizes: 1x1, 2x2, 3x3, 4x4 tiles
            // Radius = half diagonal
            return card.cardName switch
            {
                "X-Bow" => 2f,      // 4x4
                "Mortar" => 2f,     // 4x4
                "Elixir Collector" => 1.5f, // 3x3
                _ => 1f             // 2x2 default
            };
        }

        public override void Tick(float dt, BattleSimulation sim)
        {
            base.Tick(dt, sim);

            if (IsDead) return;

            // Handle invulnerability while retracted
            if (IsRetracted && IsInvulnerableWhileRetracted)
            {
                // Tesla cannot be targeted while retracted
            }

            // Update lifetime
            Lifetime -= dt;
            if (IsExpired)
            {
                Die(DeathCause.LifetimeExpired);
                return;
            }

            // Handle retraction (Tesla)
            if (CardData.cardName == "Tesla")
            {
                UpdateTeslaRetraction(sim);
            }

            // Handle spawner buildings
            if (IsSpawner())
            {
                UpdateSpawner(dt, sim);
            }

            // Handle Elixir Collector
            if (CardData.cardName == "Elixir Collector")
            {
                UpdateElixirCollector(dt, sim);
            }

            // Handle Goblin Drill burrowing
            if (CardData.cardName == "Goblin Drill")
            {
                UpdateGoblinDrill(dt, sim);
            }

            // Handle Cannon Cart transformation
            if (CardData.cardName == "Cannon Cart")
            {
                UpdateCannonCart(sim);
            }

            // Normal attack logic
            if (!IsRetracted && !IsSpawner() && CardData.cardName != "Elixir Collector" && CardData.cardName != "Goblin Drill")
            {
                UpdateAttack(dt, sim);
            }
        }

        private void UpdateTeslaRetraction(BattleSimulation sim)
        {
            bool hasTarget = FindTarget(sim) != null;

            if (hasTarget && IsRetracted)
            {
                // Pop up
                IsRetracted = false;
                IsInvulnerableWhileRetracted = false;
            }
            else if (!hasTarget && !IsRetracted)
            {
                // Retract
                IsRetracted = true;
                IsInvulnerableWhileRetracted = true;
                Target = null;
            }
        }

        private bool IsSpawner()
        {
            return CardData.cardName == "Goblin Hut" || 
                   CardData.cardName == "Furnace" || 
                   CardData.cardName == "Tombstone" ||
                   CardData.cardName == "Goblin Cage";
        }

        private void UpdateSpawner(float dt, BattleSimulation sim)
        {
            SpawnTimer -= dt;

            if (CardData.cardName == "Goblin Hut")
            {
                if (SpawnTimer <= 0 && SpawnWave < 6)
                {
                    SpawnSpearGoblin(sim);
                    SpawnTimer = 4.9f;
                    SpawnWave++;
                }
            }
            else if (CardData.cardName == "Furnace")
            {
                if (SpawnTimer <= 0 && SpawnWave < 4)
                {
                    SpawnFireSpirits(sim);
                    SpawnTimer = 10f;
                    SpawnWave++;
                }
            }
            else if (CardData.cardName == "Tombstone")
            {
                if (SpawnTimer <= 0 && SpawnWave < 6)
                {
                    SpawnSkeleton(sim);
                    SpawnTimer = 2.9f;
                    SpawnWave++;
                }
            }
            else if (CardData.cardName == "Goblin Cage")
            {
                // Goblin Cage doesn't spawn periodically; spawns on death
                SpawnTimer = float.MaxValue;
            }
        }

        private void UpdateGoblinDrill(float dt, BattleSimulation sim)
        {
            // Goblin Drill: Burrows underground, invulnerable, then pops up at target location
            // Simplified: spawn 3 waves of 2 goblins every 10s
            if (SpawnTimer <= 0 && SpawnWave < 3)
            {
                SpawnGoblinsFromDrill(sim);
                SpawnTimer = 10f;
                SpawnWave++;
            }
            else
            {
                SpawnTimer -= dt;
            }
        }

        private void SpawnSpearGoblin(BattleSimulation sim)
        {
            var cardData = Services.Get<DataManager>().GetCardByName("Spear Goblins");
            if (cardData != null)
            {
                var stats = cardData.GetStats(Level);
                var offset = new Vector2(UnityEngine.Random.Range(-0.5f, 0.5f), UnityEngine.Random.Range(-0.5f, 0.5f));
                var unit = new Unit(sim._nextEntityId++, OwnerPlayerId, cardData, stats, Position + offset, Level);
                sim._units.Add(unit);
                sim._entities[unit.Id] = unit;
            }
        }

        private void SpawnFireSpirits(BattleSimulation sim)
        {
            var cardData = Services.Get<DataManager>().GetCardByName("Fire Spirit");
            if (cardData != null)
            {
                var stats = cardData.GetStats(Level);
                for (int i = 0; i < 2; i++)
                {
                    var offset = new Vector2(UnityEngine.Random.Range(-0.5f, 0.5f), UnityEngine.Random.Range(-0.5f, 0.5f));
                    var unit = new Unit(sim._nextEntityId++, OwnerPlayerId, cardData, stats, Position + offset, Level);
                    sim._units.Add(unit);
                    sim._entities[unit.Id] = unit;
                }
            }
        }

        private void SpawnSkeleton(BattleSimulation sim)
        {
            var cardData = Services.Get<DataManager>().GetCardByName("Skeleton");
            if (cardData != null)
            {
                var stats = cardData.GetStats(Level);
                var offset = new Vector2(UnityEngine.Random.Range(-0.5f, 0.5f), UnityEngine.Random.Range(-0.5f, 0.5f));
                var unit = new Unit(sim._nextEntityId++, OwnerPlayerId, cardData, stats, Position + offset, Level);
                sim._units.Add(unit);
                sim._entities[unit.Id] = unit;
            }
        }

        private void SpawnGoblinsFromDrill(BattleSimulation sim)
        {
            var cardData = Services.Get<DataManager>().GetCardByName("Goblin");
            if (cardData != null)
            {
                var stats = cardData.GetStats(Level);
                for (int i = 0; i < 2; i++)
                {
                    var offset = new Vector2(UnityEngine.Random.Range(-0.5f, 0.5f), UnityEngine.Random.Range(-0.5f, 0.5f));
                    var unit = new Unit(sim._nextEntityId++, OwnerPlayerId, cardData, stats, Position + offset, Level);
                    sim._units.Add(unit);
                    sim._entities[unit.Id] = unit;
                }
            }
        }

        private void UpdateElixirCollector(float dt, BattleSimulation sim)
        {
            // Generate 1 elixir every 9.8 seconds (8 total over 70s)
            SpawnTimer -= dt;
            if (SpawnTimer <= 0)
            {
                var player = OwnerPlayerId == 1 ? sim._player1 : sim._player2;
                player.Elixir = Math.Min(sim._config.maxElixir, player.Elixir + 1);
                SpawnTimer = 9.8f;
            }
        }

        private void UpdateCannonCart(BattleSimulation sim)
        {
            // Cannon Cart has two forms: mobile (ground only) and stationary (air & ground)
            // When "wheels" HP depleted (at 50% HP), transforms
            if (!_cannonCartTransformed && CurrentHP <= MaxHP / 2)
            {
                _cannonCartTransformed = true;
                AttackRange = 5.5f; // Stationary range
                CanTargetAir = true; // Can now target air
                CanTargetGround = true;
                // Visual change handled by presentation
            }
        }

        private void UpdateAttack(float dt, BattleSimulation sim)
        {
            // Find target
            if (Target == null || Target.IsDead || !IsInRange(Target))
            {
                Target = FindTarget(sim);
            }

            if (Target != null)
            {
                AttackCooldown -= dt;
                if (AttackCooldown <= 0)
                {
                    PerformAttack(sim);
                    AttackCooldown = Stats.hitSpeed;
                }
            }
        }

        private Entity FindTarget(BattleSimulation sim)
        {
            var candidates = sim.GetPotentialTargets(this);
            Entity bestTarget = null;
            float bestDist = float.MaxValue;

            foreach (var candidate in candidates)
            {
                if (!IsValidTarget(candidate)) continue;

                float dist = Vector2.Distance(Position, candidate.Position);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestTarget = candidate;
                }
            }

            return bestTarget;
        }

        private bool IsValidTarget(Entity target)
        {
            string name = CardData.cardName;

            // Tesla: air & ground
            if (name == "Tesla") return true;
            
            // Cannon, Bomb Tower: ground only
            if (name == "Cannon" || name == "Bomb Tower")
            {
                if (target.Type == EntityType.Unit)
                {
                    var unit = target as Unit;
                    if (unit != null && unit.IsFlying) return false;
                }
                return true;
            }

            // Inferno Tower: air & ground
            if (name == "Inferno Tower") return true;

            // Mortar, X-Bow: ground only
            if (name == "Mortar" || name == "X-Bow")
            {
                if (target.Type == EntityType.Unit)
                {
                    var unit = target as Unit;
                    if (unit != null && unit.IsFlying) return false;
                }
                return true;
            }

            // Cannon Cart: ground only when mobile, air & ground when stationary
            if (name == "Cannon Cart")
            {
                if (!_cannonCartTransformed)
                {
                    if (target.Type == EntityType.Unit)
                    {
                        var unit = target as Unit;
                        if (unit != null && unit.IsFlying) return false;
                    }
                }
                return true;
            }

            return true;
        }

        private bool IsInRange(Entity target)
        {
            return Vector2.Distance(Position, target.Position) <= AttackRange + target.CollisionRadius;
        }

        private void PerformAttack(BattleSimulation sim)
        {
            if (Target == null) return;

            string name = CardData.cardName;

            // Mortar and X-Bow have special attack patterns
            if (name == "Mortar")
            {
                FireMortarShot(sim);
            }
            else if (name == "X-Bow")
            {
                FireXBowShot(sim);
            }
            else if (name == "Inferno Tower")
            {
                FireInfernoBeam(sim);
            }
            else
            {
                // Normal projectile
                var projectile = new Projectile(
                    sim._nextEntityId++,
                    OwnerPlayerId,
                    this,
                    Target,
                    Stats.damage,
                    500f, // Default speed
                    CardData.mechanicsJson
                );
                sim.AddProjectile(projectile);
            }
        }

        private void FireMortarShot(BattleSimulation sim)
        {
            // Mortar: dead zone 0-4 tiles, range 4-11.5
            // High arc, splash damage
            float dist = Vector2.Distance(Position, Target.Position);
            if (dist < 4f) return; // Dead zone

            var projectile = new Projectile(
                sim._nextEntityId++,
                OwnerPlayerId,
                this,
                Target,
                Stats.damage,
                300f, // Slow arc
                CardData.mechanicsJson
            );
            projectile.IsMortarShot = true;
            projectile.SplashRadius = 1.5f;
            sim.AddProjectile(projectile);
        }

        private void FireXBowShot(BattleSimulation sim)
        {
            // X-Bow: very fast, long range, ground only
            var projectile = new Projectile(
                sim._nextEntityId++,
                OwnerPlayerId,
                this,
                Target,
                Stats.damage,
                1200f, // Very fast
                CardData.mechanicsJson
            );
            sim.AddProjectile(projectile);
        }

        private void FireInfernoBeam(BattleSimulation sim)
        {
            // Inferno Tower: ramping damage beam
            // Damage doubles every 0.4s up to 1600
            var beam = new InfernoBeam(
                sim._nextEntityId++,
                OwnerPlayerId,
                this,
                Target,
                GetInfernoDamage(),
                CardData.mechanicsJson
            );
            sim.AddProjectile(beam);
        }

        private int GetInfernoDamage()
        {
            // Ramping: 50, 100, 200, 400, 800, 1600
            int[] damages = { 50, 100, 200, 400, 800, 1600 };
            return _infernoRampStage < damages.Length ? damages[_infernoRampStage] : damages[damages.Length - 1];
        }

        public override void TakeDamage(int amount, DamageType damageType, uint sourceId)
        {
            if (IsDead) return;
            if (IsRetracted && IsInvulnerableWhileRetracted) return; // Tesla retracted
            if (IsInvulnerable) return;

            base.TakeDamage(amount, damageType, sourceId);

            // Inferno Tower ramp reset on damage taken (if stunned)
            // Actually handled by stun status effect
        }

        public override void OnDeath(BattleSimulation sim)
        {
            base.OnDeath(sim);

            // Death effects
            string name = CardData.cardName;
            
            if (name == "Bomb Tower")
            {
                // Death damage in radius
                foreach (var entity in sim.GetPotentialTargets(this))
                {
                    if (Vector2.Distance(entity.Position, Position) <= 1.5f)
                    {
                        entity.TakeDamage(Stats.damage, DamageType.Area, Id);
                    }
                }
            }
            else if (name == "Tombstone")
            {
                // Spawn 4 skeletons on death
                var cardData = Services.Get<DataManager>().GetCardByName("Skeleton");
                if (cardData != null)
                {
                    var stats = cardData.GetStats(Level);
                    for (int i = 0; i < 4; i++)
                    {
                        var offset = new Vector2(UnityEngine.Random.Range(-0.5f, 0.5f), UnityEngine.Random.Range(-0.5f, 0.5f));
                        var unit = new Unit(sim._nextEntityId++, OwnerPlayerId, cardData, stats, Position + offset, Level);
                        sim._units.Add(unit);
                        sim._entities[unit.Id] = unit;
                    }
                }
            }
            else if (name == "Goblin Cage")
            {
                // Spawn Goblin Brawler
                var cardData = Services.Get<DataManager>().GetCardByName("Goblin Brawler");
                if (cardData != null)
                {
                    var stats = cardData.GetStats(Level);
                    var unit = new Unit(sim._nextEntityId++, OwnerPlayerId, cardData, stats, Position, Level);
                    sim._units.Add(unit);
                    sim._entities[unit.Id] = unit;
                }
            }
            else if (name == "Goblin Drill")
            {
                // Final goblin spawn on death
                var cardData = Services.Get<DataManager>().GetCardByName("Goblin");
                if (cardData != null)
                {
                    var stats = cardData.GetStats(Level);
                    for (int i = 0; i < 2; i++)
                    {
                        var offset = new Vector2(UnityEngine.Random.Range(-0.5f, 0.5f), UnityEngine.Random.Range(-0.5f, 0.5f));
                        var unit = new Unit(sim._nextEntityId++, OwnerPlayerId, cardData, stats, Position + offset, Level);
                        sim._units.Add(unit);
                        sim._entities[unit.Id] = unit;
                    }
                }
            }
        }
    }

    // Special projectile types
    public class InfernoBeam : Projectile
    {
        private int _rampStage = 0;
        private float _timeAtStage = 0f;

        public InfernoBeam(uint id, int ownerPlayerId, Entity source, Entity target, int baseDamage, string mechanics)
            : base(id, ownerPlayerId, source, target, baseDamage, 0f, mechanics) // Instant hit
        {
            IsBeam = true;
        }

        public override void Tick(float dt, BattleSimulation sim)
        {
            if (Target == null || Target.IsDead || !IsInRange(Target))
            {
                // Target lost - reset ramp
                _rampStage = 0;
                _timeAtStage = 0f;
                base.Tick(dt, sim);
                return;
            }

            // Beam hits every 0.4s
            _timeAtStage += dt;
            if (_timeAtStage >= 0.4f)
            {
                _timeAtStage = 0f;
                _rampStage = Math.Min(_rampStage + 1, 5);

                int damage = GetRampDamage(_rampStage);
                Target.TakeDamage(damage, DamageType.Beam, SourceId);
            }

            // Beam doesn't travel - instant
            IsDead = false; // Beam persists while target valid
        }

        private int GetRampDamage(int stage)
        {
            int[] damages = { 50, 100, 200, 400, 800, 1600 };
            return stage < damages.Length ? damages[stage] : damages[damages.Length - 1];
        }

        private bool IsInRange(Entity target)
        {
            return Vector2.Distance(Source.Position, target.Position) <= 6f + target.CollisionRadius; // Inferno range = 6
        }
    }
}