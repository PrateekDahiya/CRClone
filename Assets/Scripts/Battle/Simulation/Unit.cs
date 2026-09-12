using System;
using System.Collections.Generic;
using UnityEngine;
using CRClone.Core;
using CRClone.Data;

namespace CRClone.Battle.Simulation
{
    public class Unit : Entity
    {
        public CardData CardData { get; private set; }
        public CardLevelStats Stats { get; private set; }
        public int Level { get; private set; }
        public UnitState State { get; private set; } = UnitState.Idle;
        public Entity Target { get; internal set; }
        public float AttackCooldown { get; private set; }
        public float MoveSpeed { get; private set; }
        public float BaseMoveSpeed { get; private set; }
        public float AttackRange { get; private set; }
        public bool IsFlying { get; private set; }
        public bool IsCharging { get; private set; }
        public float ChargeTimeRemaining { get; private set; }
        public Vector2 ChargeTarget { get; private set; }
        public float LastAttackTime { get; private set; }
        public float ChargeDamageMultiplier { get; private set; } = 1f;
        public bool CanTargetAir { get; private set; }
        public bool CanTargetGround { get; private set; } = true;

        // Champion ability
        public float AbilityCooldown { get; private set; }
        public bool AbilityReady => AbilityCooldown <= 0 && CardData.rarity == CardRarity.Champion;

        // Pathfinding
        private List<Vector2> _path;
        private int _pathIndex;
        private float _pathRequestTimer = 0f;
        private Vector2 _lastTargetPosition;
        private const float PATH_REQUEST_INTERVAL = 0.5f; // Request new path every 0.5s if target moves

        // Ramping damage (Inferno Dragon)
        private int _infernoRampStage = 0;
        private float _infernoTimeAtStage = 0f;
        private uint _infernoTargetId = 0;

        // Status effect modifiers
        private float _damageMultiplier = 1f;

        public Unit(uint id, int ownerPlayerId, CardData cardData, CardLevelStats stats, Vector2 position, int level)
            : base(id, ownerPlayerId, EntityType.Unit, position, stats.hitpoints)
        {
            CardData = cardData;
            Stats = stats;
            Level = level;
            AttackRange = stats.range;
            BaseMoveSpeed = GetSpeedValue(cardData.speed);
            MoveSpeed = BaseMoveSpeed * (IsFlying ? 1.2f : 1f);
            AttackCooldown = stats.hitSpeed;
            LastAttackTime = -stats.hitSpeed; // Can attack immediately

            // Determine flying and targeting
            DetermineUnitProperties(cardData);

            // Collision radius based on unit size
            CollisionRadius = cardData.count > 1 ? 0.4f : 0.5f;

            // Champion ability cooldown
            if (cardData.rarity == CardRarity.Champion)
            {
                AbilityCooldown = 0f; // Ready initially
            }

            // Initialize shields for specific units
            InitializeShields();
        }

        private void DetermineUnitProperties(CardData cardData)
        {
            string name = cardData.cardName;
            
            // Flying units
            IsFlying = cardData.mechanicsJson?.Contains("flying") == true ||
                       name.Contains("Minion") || name.Contains("Bat") ||
                       name.Contains("Dragon") || name.Contains("Balloon") ||
                       name.Contains("Phoenix") || name.Contains("Lava Hound") ||
                       name.Contains("Skeleton Dragon") || name.Contains("Mega Minion");

            // Targeting capabilities
            CanTargetAir = cardData.targetType == TargetType.Air || cardData.targetType == TargetType.Both;
            CanTargetGround = cardData.targetType == TargetType.Ground || cardData.targetType == TargetType.Both;

            // Charge units
            if (name == "Prince" || name == "Dark Prince" || name == "Ram Rider" || 
                name == "Battle Ram" || name == "Little Prince" || name == "Goblin Drill")
            {
                // Charge logic handled in StartCharge
            }
        }

        private void InitializeShields()
        {
            string name = CardData.cardName;
            if (name == "Guards")
            {
                // Guards have 3 shields of 120 HP each (simplified as single 360 HP shield)
                AddStatusEffect(new StatusEffect(StatusEffectType.Shield, float.MaxValue, 0, 360, Id));
            }
            else if (name == "Dark Prince")
            {
                // Dark Prince has 300 HP shield
                AddStatusEffect(new StatusEffect(StatusEffectType.Shield, float.MaxValue, 0, 300, Id));
            }
        }

        private float GetSpeedValue(SpeedType speed)
        {
            return speed switch
            {
                SpeedType.VerySlow => 30f,
                SpeedType.Slow => 45f,
                SpeedType.Medium => 60f,
                SpeedType.Fast => 90f,
                SpeedType.VeryFast => 120f,
                _ => 60f
            };
        }

        public override void Tick(float dt, BattleSimulation sim)
        {
            base.Tick(dt, sim);
            UpdateStatusEffects(dt);

            if (IsDead) return;

            // Update ability cooldown
            if (AbilityCooldown > 0)
                AbilityCooldown -= dt;

            // Handle special states - completely stop
            if (IsStunned || IsFrozen)
            {
                Velocity = Vector2.zero;
                // Don't reduce attack cooldown while stunned/frozen
                return;
            }

            // Handle charge state
            if (IsCharging)
            {
                UpdateCharge(dt, sim);
                return;
            }

            // Update inferno ramp if applicable
            if (CardData.cardName == "Inferno Dragon")
            {
                UpdateInfernoRamp(dt, sim);
            }

            // Check target validity
            if (Target != null && (Target.IsDead || !IsValidTarget(Target)))
            {
                Target = null;
                _infernoRampStage = 0;
                _infernoTimeAtStage = 0f;
                _infernoTargetId = 0;
            }

            // Acquire target if needed
            if (Target == null)
            {
                Target = AcquireTarget(sim.GetPotentialTargets(this), sim);
            }

            // Attack or move
            if (Target != null)
            {
                float dist = Vector2.Distance(Position, Target.Position);
                float effectiveRange = AttackRange + Target.CollisionRadius + CollisionRadius;

                if (dist <= effectiveRange)
                {
                    // In range - attack
                    TryAttack(dt, sim);
                }
                else
                {
                    // Move toward target
                    MoveTowardTarget(dt, sim);
                }
            }
            else
            {
                // No target - idle or move toward enemy side
                State = UnitState.Idle;
                Velocity = Vector2.zero;
                
                // Auto-move toward enemy side if no target (for building targeters)
                if (CardData.targetType == TargetType.Buildings)
                {
                    MoveTowardEnemySide(dt, sim);
                }
            }
        }

        private bool IsValidTarget(Entity target)
        {
            // Check air/ground targeting
            if (target.Type == EntityType.Unit)
            {
                var targetUnit = target as Unit;
                if (targetUnit != null)
                {
                    if (targetUnit.IsFlying && !CanTargetAir) return false;
                    if (!targetUnit.IsFlying && !CanTargetGround) return false;
                }
            }

            // Building targeting
            if (CardData.targetType == TargetType.Buildings)
            {
                if (target.Type != EntityType.Building && target.Type != EntityType.Tower)
                    return false;
            }

            // Invisible units cannot be targeted (unless revealed by splash/true sight)
            if (target.IsInvisible) return false;

            return true;
        }

        internal Entity AcquireTarget(List<Entity> candidates, BattleSimulation sim)
        {
            Entity bestTarget = null;
            int bestPathDist = int.MaxValue;
            int bestPriority = int.MaxValue;

            foreach (var candidate in candidates)
            {
                if (!IsValidTarget(candidate)) continue;

                // Calculate path distance
                int pathDist = sim._pathfinding.GetPathDistance(Position, candidate.Position, IsFlying);
                
                // Target priority
                int priority = GetTargetPriority(candidate);

                // Prefer closer by path distance, then by priority
                if (pathDist < bestPathDist || (pathDist == bestPathDist && priority < bestPriority))
                {
                    bestPathDist = pathDist;
                    bestPriority = priority;
                    bestTarget = candidate;
                }
            }

            if (bestTarget != null)
            {
                State = UnitState.Attacking;
                RequestPath(bestTarget.Position, sim);
                _lastTargetPosition = bestTarget.Position;
            }

            return bestTarget;
        }

        private int GetTargetPriority(Entity target)
        {
            if (CardData.targetType == TargetType.Buildings)
            {
                if (target.Type == EntityType.Building) return 0;
                if (target.Type == EntityType.Tower) return 1;
                return 1000;
            }

            // Normal troops: troops > buildings > towers
            if (target.Type == EntityType.Unit) return 0;
            if (target.Type == EntityType.Building) return 1;
            if (target.Type == EntityType.Tower) return 2;
            return 100;
        }

        private void MoveTowardTarget(float dt, BattleSimulation sim)
        {
            if (Target == null) return;
            // Re-request path if target moved significantly or timer elapsed
            _pathRequestTimer += dt;
            bool targetMoved = Vector2.Distance(Target.Position, _lastTargetPosition) > 1f;
            
            if (_path == null || _path.Count == 0 || _pathIndex >= _path.Count || 
                _pathRequestTimer >= PATH_REQUEST_INTERVAL || targetMoved)
            {
                RequestPath(Target.Position, sim);
                _pathRequestTimer = 0f;
                _lastTargetPosition = Target.Position;
                return;
            }

            Vector2 targetPos = _path[_pathIndex];
            Vector2 dir = (targetPos - Position).normalized;
            float moveDist = MoveSpeed * dt * GetSpeedMultiplier();

            if (Vector2.Distance(Position, targetPos) <= moveDist)
            {
                Position = targetPos;
                _pathIndex++;
            }
            else
            {
                Position += dir * moveDist;
            }

            State = UnitState.Moving;
            Velocity = dir * MoveSpeed * GetSpeedMultiplier();
        }

        private void MoveTowardEnemySide(float dt, BattleSimulation sim)
        {
            // Move toward enemy princess towers
            Vector2 targetPos = OwnerPlayerId == 1 
                ? new Vector2(9f, 20f) // Toward enemy side
                : new Vector2(9f, 12f); // Toward enemy side
            
            if (_path == null || _path.Count == 0 || _pathIndex >= _path.Count)
            {
                RequestPath(targetPos, sim);
                return;
            }

            Vector2 nextPos = _path[_pathIndex];
            Vector2 dir = (nextPos - Position).normalized;
            float moveDist = MoveSpeed * dt * GetSpeedMultiplier();

            if (Vector2.Distance(Position, nextPos) <= moveDist)
            {
                Position = nextPos;
                _pathIndex++;
            }
            else
            {
                Position += dir * moveDist;
            }

            State = UnitState.Moving;
            Velocity = dir * MoveSpeed * GetSpeedMultiplier();
        }

        private float GetSpeedMultiplier()
        {
            float mult = 1f;
            if (IsSlowed) mult *= 0.65f;
            if (HasStatusEffect(StatusEffectType.Rage)) mult *= 1.3f;
            return mult;
        }

        private float GetDamageMultiplier()
        {
            float mult = _damageMultiplier;
            if (HasStatusEffect(StatusEffectType.Rage)) mult *= 1f; // Rage only affects speed
            if (CardData.cardName == "Archer Queen" && IsInvisible) mult *= 2.5f; // Royal Cloak
            return mult;
        }

        private void RequestPath(Vector2 targetPos, BattleSimulation sim)
        {
            _path = sim._pathfinding.FindPath(Position, targetPos, IsFlying);
            _pathIndex = 0;
        }

        private void TryAttack(float dt, BattleSimulation sim)
        {
            Velocity = Vector2.zero;
            State = UnitState.Attacking;

            AttackCooldown -= dt * GetSpeedMultiplier();

            if (AttackCooldown <= 0)
            {
                PerformAttack(sim);
                AttackCooldown = Stats.hitSpeed / GetSpeedMultiplier();
            }
        }

        private void PerformAttack(BattleSimulation sim)
        {
            LastAttackTime = sim.CurrentTick * BattleSimulation.FIXED_DT;

            int damage = Mathf.RoundToInt(Stats.damage * GetDamageMultiplier());

            // Create projectile or instant hit
            if (AttackRange > 1.5f) // Ranged
            {
                var projectile = new Projectile(
                    sim._nextEntityId++,
                    OwnerPlayerId,
                    this,
                    Target,
                    damage,
                    GetProjectileSpeed(),
                    CardData.mechanicsJson
                );
                
                // Configure special projectile properties
                ConfigureProjectile(projectile);
                sim.AddProjectile(projectile);
            }
            else // Melee
            {
                DealDamage(Target, damage, DamageType.Physical, sim);
            }

            // Handle special attack mechanics
            HandleAttackEffects(sim);
        }

        private void ConfigureProjectile(Projectile projectile)
        {
            string name = CardData.cardName;
            string mechanics = CardData.mechanicsJson ?? "";

            // Pierce (Magic Archer)
            if (name == "Magic Archer" || mechanics.Contains("pierce"))
            {
                projectile.PierceCount = int.MaxValue; // Infinite pierce
            }

            // Chain (Electro Wizard, Electro Dragon, Electro Spirit)
            if (name == "Electro Wizard" || name == "Electro Dragon" || name == "Electro Spirit" || mechanics.Contains("chain"))
            {
                projectile.ChainCount = name == "Electro Wizard" ? 2 : 3; // E-Wiz 2, E-Dragon 3, E-Spirit 3
                projectile.ChainRange = 4f;
            }

            // Splash
            if (mechanics.Contains("splash") || name == "Wizard" || name == "Bombardier" || name == "Bomber")
            {
                projectile.SplashRadius = 1.5f;
            }

            // Mortar shot (Mortar)
            if (name == "Mortar")
            {
                projectile.IsMortarShot = true;
                projectile.SplashRadius = 1.5f;
            }

            // 360 splash (Valkyrie, Dark Prince)
            if (name == "Valkyrie" || name == "Dark Prince")
            {
                projectile.Is360Splash = true;
                projectile.SplashRadius = 2f;
            }
        }

        private float GetProjectileSpeed()
        {
            string name = CardData.cardName;
            if (name.Contains("Musketeer")) return 600f;
            if (name.Contains("Wizard") || name.Contains("Witch")) return 450f;
            if (name.Contains("Princess")) return 450f;
            if (name.Contains("Dart Goblin")) return 750f;
            if (name.Contains("Electro Wizard")) return 600f;
            if (name.Contains("Archers")) return 500f;
            if (name.Contains("Spear Goblin")) return 550f;
            return 500f;
        }

        private void DealDamage(Entity target, int damage, DamageType type, BattleSimulation sim)
        {
            if (target == null || target.IsDead) return;

            int finalDamage = damage;
            target.TakeDamage(finalDamage, type, Id);

            // Track inferno ramp
            if (CardData.cardName == "Inferno Dragon" && target.Id != _infernoTargetId)
            {
                _infernoTargetId = target.Id;
                _infernoRampStage = 0;
                _infernoTimeAtStage = 0f;
            }
        }

        private void HandleAttackEffects(BattleSimulation sim)
        {
            string name = CardData.cardName;
            string mechanics = CardData.mechanicsJson ?? "";

            // 360 splash (Valkyrie, Dark Prince)
            if (name == "Valkyrie" || name == "Dark Prince")
            {
                float radius = 2f;
                foreach (var entity in sim.GetPotentialTargets(this))
                {
                    if (entity.Id != Target?.Id && Vector2.Distance(entity.Position, Position) <= radius)
                    {
                        DealDamage(entity, Mathf.RoundToInt(Stats.damage * 0.5f * GetDamageMultiplier()), DamageType.Area, sim);
                    }
                }
            }
            // Splash damage (Wizard, Bomber, etc.)
            else if (mechanics.Contains("splash") || name == "Wizard" || name == "Bombardier" || name == "Bomber")
            {
                float radius = 1.5f;
                if (Target != null)
                {
                    foreach (var entity in sim.GetPotentialTargets(this))
                    {
                        if (entity.Id != Target.Id && Vector2.Distance(entity.Position, Target.Position) <= radius)
                        {
                            DealDamage(entity, Mathf.RoundToInt(Stats.damage * 0.5f * GetDamageMultiplier()), DamageType.Area, sim);
                        }
                    }
                }
            }
        }

        private void UpdateInfernoRamp(float dt, BattleSimulation sim)
        {
            if (Target == null || Target.IsDead || Target.Id != _infernoTargetId)
            {
                _infernoRampStage = 0;
                _infernoTimeAtStage = 0f;
                _infernoTargetId = 0;
                return;
            }

            if (!IsInRange(Target))
            {
                _infernoRampStage = 0;
                _infernoTimeAtStage = 0f;
                return;
            }

            _infernoTimeAtStage += dt;
            if (_infernoTimeAtStage >= 0.4f)
            {
                _infernoTimeAtStage = 0f;
                _infernoRampStage = Math.Min(_infernoRampStage + 1, 5);
            }
        }

        private int GetInfernoDamage()
        {
            int[] damages = { 50, 100, 200, 400, 800, 1600 };
            return _infernoRampStage < damages.Length ? damages[_infernoRampStage] : damages[damages.Length - 1];
        }

        private bool IsInRange(Entity target)
        {
            return Vector2.Distance(Position, target.Position) <= AttackRange + target.CollisionRadius + CollisionRadius;
        }

        private void UpdateCharge(float dt, BattleSimulation sim)
        {
            ChargeTimeRemaining -= dt;
            State = UnitState.Charging;

            Vector2 dir = (ChargeTarget - Position).normalized;
            float chargeSpeed = BaseMoveSpeed * 2.5f; // 2.5x speed during charge
            Position += dir * chargeSpeed * dt;

            // Check collision during charge
            foreach (var entity in sim.GetPotentialTargets(this))
            {
                if (Vector2.Distance(Position, entity.Position) < CollisionRadius + entity.CollisionRadius)
                {
                    // Hit during charge!
                    int chargeDamage = Mathf.RoundToInt(Stats.damage * ChargeDamageMultiplier * GetDamageMultiplier());
                    DealDamage(entity, chargeDamage, DamageType.Physical, sim);
                    
                    // Apply knockback/stun for certain charge units
                    if (CardData.cardName == "Ram Rider")
                    {
                        entity.AddStatusEffect(new StatusEffect(StatusEffectType.Snare, 1.5f, 0, 0, Id));
                    }
                    else if (CardData.cardName == "Battle Ram")
                    {
                        entity.AddStatusEffect(new StatusEffect(StatusEffectType.Stun, 0.5f, 0, 0, Id));
                    }
                    
                    EndCharge();
                    break;
                }
            }

            if (ChargeTimeRemaining <= 0)
            {
                // Handle Mighty Miner super dash end damage
                if (CardData.cardName == "Mighty Miner" && ChargeDamageMultiplier == 1f)
                {
                    // Super dash: deal 220 damage to enemies at end position
                    foreach (var entity in sim.GetPotentialTargets(this))
                    {
                        if (entity.IsDead) continue;
                        if (Vector2.Distance(Position, entity.Position) <= 1.5f)
                        {
                            DealDamage(entity, 220, DamageType.Physical, sim);
                        }
                    }
                }
                EndCharge();
            }
        }

        public void StartCharge(Vector2 targetPos, float duration, float damageMultiplier = 2f)
        {
            IsCharging = true;
            ChargeTarget = targetPos;
            ChargeTimeRemaining = duration;
            ChargeDamageMultiplier = damageMultiplier;
            AddStatusEffect(new StatusEffect(StatusEffectType.Charge, duration, damageMultiplier, 0, Id));
            AddStatusEffect(new StatusEffect(StatusEffectType.Invulnerable, duration, 0, 0, Id));
        }

        public void EndCharge()
        {
            IsCharging = false;
            ChargeDamageMultiplier = 1f;
            RemoveStatusEffect(StatusEffectType.Charge);
            RemoveStatusEffect(StatusEffectType.Invulnerable);
        }

        public bool TryUseAbility(Vector2 targetPos, BattleSimulation sim)
        {
            if (!AbilityReady) return false;

            string name = CardData.cardName;
            
            if (name == "Archer Queen")
            {
                // Royal Cloak: Invisibility 3s, 2.5x damage, +20% speed, can target air
                StartInvisibility(3f);
                _damageMultiplier = 2.5f;
                MoveSpeed = BaseMoveSpeed * 1.2f;
                CanTargetAir = true;
                AbilityCooldown = 20f;
                
                // Emit EventBus event
                EventBus.Raise(new EventBus.ChampionAbilityUsedEvent
                {
                    playerId = OwnerPlayerId,
                    championCardId = CardData.cardId,
                    targetPosition = targetPos,
                    elixirCost = 3
                });
                
                return true;
            }
            else if (name == "Skeleton King")
            {
                // Summon Skeletons: Spawn 5 skeletons around self
                SummonSkeletons(sim);
                AbilityCooldown = 15f;
                
                // Emit EventBus event
                EventBus.Raise(new EventBus.ChampionAbilityUsedEvent
                {
                    playerId = OwnerPlayerId,
                    championCardId = CardData.cardId,
                    targetPosition = targetPos,
                    elixirCost = 2
                });
                
                return true;
            }
            else if (name == "Mighty Miner")
            {
                // Super Dash: Dash 5 tiles, stun enemies in path, 220 damage at end
                SuperDash(targetPos, sim);
                AbilityCooldown = 10f;
                
                // Emit EventBus event
                EventBus.Raise(new EventBus.ChampionAbilityUsedEvent
                {
                    playerId = OwnerPlayerId,
                    championCardId = CardData.cardId,
                    targetPosition = targetPos,
                    elixirCost = 2
                });
                
                return true;
            }
            return false;
        }

        private void SummonSkeletons(BattleSimulation sim)
        {
            var skeletonCard = Services.Get<DataManager>().GetCardByName("Skeleton");
            if (skeletonCard == null) return;

            var stats = skeletonCard.GetStats(Level);
            for (int i = 0; i < 5; i++)
            {
                float angle = (i / 5f) * 360f * Mathf.Deg2Rad;
                float radius = 1f;
                Vector2 spawnPos = Position + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                var skeleton = new Unit(sim._nextEntityId++, OwnerPlayerId, skeletonCard, stats, spawnPos, Level);
                sim._units.Add(skeleton);
                sim._entities[skeleton.Id] = skeleton;
            }
        }

        private void SuperDash(Vector2 targetPos, BattleSimulation sim)
        {
            Vector2 dir = (targetPos - Position).normalized;
            Vector2 dashTarget = Position + dir * 5f;
            
            // Stun enemies in path
            foreach (var entity in sim.GetPotentialTargets(this))
            {
                if (entity.IsDead) continue;
                
                // Check if entity is in the dash path (within 0.75 tiles of line)
                Vector2 toEntity = entity.Position - Position;
                float projDist = Vector2.Dot(toEntity, dir);
                if (projDist < 0 || projDist > 5f) continue; // Behind or past dash end
                
                Vector2 closestPoint = Position + dir * projDist;
                float perpDist = Vector2.Distance(entity.Position, closestPoint);
                
                if (perpDist <= 0.75f)
                {
                    entity.AddStatusEffect(new StatusEffect(StatusEffectType.Stun, 1f, 0, 0, Id));
                }
            }
            
            // Dash to target
            StartCharge(dashTarget, 0.5f, 1f);
            
            // Schedule damage at end of dash (will be applied when charge ends via collision)
            // We'll handle the end-dash damage in UpdateCharge when charge completes
        }

        private void StartInvisibility(float duration)
        {
            AddStatusEffect(new StatusEffect(StatusEffectType.Invisible, duration, 0, 0, Id));
        }

        protected override void OnStatusEffectAdded(StatusEffect effect)
        {
            base.OnStatusEffectAdded(effect);
            
            // Handle status effect application
            if (effect.Type == StatusEffectType.Invisible)
            {
                _damageMultiplier = CardData.cardName == "Archer Queen" ? 2.5f : 1f;
            }
        }

        protected override void OnStatusEffectRemoved(StatusEffect effect)
        {
            base.OnStatusEffectRemoved(effect);
            
            // Handle status effect removal
            if (effect.Type == StatusEffectType.Invisible)
            {
                _damageMultiplier = 1f;
                CanTargetAir = CardData.targetType == TargetType.Air || CardData.targetType == TargetType.Both;
            }
            if (effect.Type == StatusEffectType.Charge)
            {
                EndCharge();
            }
        }

        public override void OnDeath(BattleSimulation sim)
        {
            base.OnDeath(sim);

            // Death spawns (Witch, Night Witch, Lava Hound, etc.)
            if (CardData.mechanicsJson?.Contains("deathSpawn") == true)
            {
                SpawnDeathUnits(sim);
            }

            // Lava Hound death spawns Lava Pups
            if (CardData.cardName == "Lava Hound")
            {
                SpawnLavaPups(sim);
            }

            // Night Witch death spawns Bats
            if (CardData.cardName == "Night Witch")
            {
                SpawnBats(sim);
            }
        }

        private void SpawnDeathUnits(BattleSimulation sim)
        {
            // Parse death spawn from mechanicsJson
            // Simplified: spawn based on card name
            string name = CardData.cardName;
            CardData spawnCard = null;
            int count = 0;

            if (name == "Witch") { spawnCard = Services.Get<DataManager>().GetCardByName("Skeleton"); count = 4; }
            else if (name == "Night Witch") { spawnCard = Services.Get<DataManager>().GetCardByName("Bat"); count = 4; }
            else if (name == "Lava Hound") { spawnCard = Services.Get<DataManager>().GetCardByName("Lava Pup"); count = 6; }

            if (spawnCard != null && count > 0)
            {
                var stats = spawnCard.GetStats(Level);
                for (int i = 0; i < count; i++)
                {
                    var offset = new Vector2((float)sim._rng.NextDouble() - 0.5f, (float)sim._rng.NextDouble() - 0.5f);
                    var unit = new Unit(sim._nextEntityId++, OwnerPlayerId, spawnCard, stats, Position + offset, Level);
                    sim._units.Add(unit);
                    sim._entities[unit.Id] = unit;
                }
            }
        }

        private void SpawnLavaPups(BattleSimulation sim)
        {
            var cardData = Services.Get<DataManager>().GetCardByName("Lava Pup");
            if (cardData != null)
            {
                var stats = cardData.GetStats(Level);
                for (int i = 0; i < 6; i++)
                {
                    var offset = new Vector2((float)sim._rng.NextDouble() - 0.5f, (float)sim._rng.NextDouble() - 0.5f);
                    var unit = new Unit(sim._nextEntityId++, OwnerPlayerId, cardData, stats, Position + offset, Level);
                    sim._units.Add(unit);
                    sim._entities[unit.Id] = unit;
                }
            }
        }

        private void SpawnBats(BattleSimulation sim)
        {
            var cardData = Services.Get<DataManager>().GetCardByName("Bat");
            if (cardData != null)
            {
                var stats = cardData.GetStats(Level);
                for (int i = 0; i < 4; i++)
                {
                    var offset = new Vector2((float)sim._rng.NextDouble() - 0.5f, (float)sim._rng.NextDouble() - 0.5f);
                    var unit = new Unit(sim._nextEntityId++, OwnerPlayerId, cardData, stats, Position + offset, Level);
                    sim._units.Add(unit);
                    sim._entities[unit.Id] = unit;
                }
            }
        }
    }

    public enum UnitState
    {
        Idle,
        Moving,
        Attacking,
        Charging,
        Stunned,
        Frozen,
        Dead
    }
}