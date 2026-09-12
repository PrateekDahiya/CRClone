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
        public Entity Target { get; private set; }
        public float AttackCooldown { get; private set; }
        public float MoveSpeed { get; private set; }
        public float AttackRange { get; private set; }
        public bool IsFlying { get; private set; }
        public bool IsCharging { get; private set; }
        public float ChargeTimeRemaining { get; private set; }
        public Vector2 ChargeTarget { get; private set; }
        public float LastAttackTime { get; private set; }

        // Pathfinding
        private List<Vector2> _path;
        private int _pathIndex;
        private float _repateTime = 0f;

        public Unit(uint id, int ownerPlayerId, CardData cardData, CardLevelStats stats, Vector2 position, int level)
            : base(id, ownerPlayerId, EntityType.Unit, position, stats.hitpoints)
        {
            CardData = cardData;
            Stats = stats;
            Level = level;
            AttackRange = stats.range;
            MoveSpeed = GetSpeedValue(cardData.speed) * (IsFlying ? 1.2f : 1f);
            AttackCooldown = stats.hitSpeed;
            LastAttackTime = -stats.hitSpeed; // Can attack immediately

            // Determine flying
            IsFlying = cardData.mechanicsJson?.Contains("flying") == true || 
                       cardData.type == CardType.Troop && (cardData.cardName.Contains("Minion") || cardData.cardName.Contains("Bat") || 
                       cardData.cardName.Contains("Dragon") || cardData.cardName.Contains("Balloon") ||
                       cardData.cardName.Contains("Inferno Dragon") || cardData.cardName.Contains("Electro Dragon") ||
                       cardData.cardName.Contains("Skeleton Dragon") || cardData.cardName.Contains("Phoenix"));

            // Collision radius based on unit size
            CollisionRadius = cardData.count > 1 ? 0.4f : 0.5f;

            // Champion ability
            if (cardData.rarity == CardRarity.Champion)
            {
                // Ability cooldown handled separately
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

            // Handle special states
            if (IsStunned || IsFrozen)
            {
                Velocity = Vector2.zero;
                AttackCooldown = Math.Max(AttackCooldown, dt); // Pause cooldown
                return;
            }

            if (IsCharging)
            {
                UpdateCharge(dt, sim);
                return;
            }

            // Check target validity
            if (Target != null && (Target.IsDead || !IsValidTarget(Target)))
            {
                Target = null;
            }

            // Acquire target if needed
            if (Target == null)
            {
                Target = AcquireTarget(sim.GetPotentialTargets(this));
            }

            // Attack or move
            if (Target != null)
            {
                float dist = Vector2.Distance(Position, Target.Position);
                float effectiveRange = AttackRange + Target.CollisionRadius + CollisionRadius;

                if (dist <= effectiveRange)
                {
                    // In range - attack
                    TryAttack(sim);
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
            }
        }

        private bool IsValidTarget(Entity target)
        {
            // Check targeting rules
            if (CardData.targetType == TargetType.Ground && target.Type == EntityType.Unit)
            {
                // Check if target is flying
                // This would need a property on Entity
            }
            if (CardData.targetType == TargetType.Buildings && target.Type != EntityType.Building && target.Type != EntityType.Tower)
                return false;

            return true;
        }

        private Entity AcquireTarget(List<Entity> candidates)
        {
            Entity bestTarget = null;
            float bestScore = float.MaxValue;

            foreach (var candidate in candidates)
            {
                if (!IsValidTarget(candidate)) continue;

                // Calculate path distance (simplified to Euclidean for now)
                float dist = Vector2.Distance(Position, candidate.Position);
                
                // Target priority
                int priority = GetTargetPriority(candidate);
                float score = dist * 1000 + priority;

                if (score < bestScore)
                {
                    bestScore = score;
                    bestTarget = candidate;
                }
            }

            if (bestTarget != null)
            {
                State = UnitState.Attacking;
                RequestPath(bestTarget.Position);
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
            if (_path == null || _path.Count == 0 || _pathIndex >= _path.Count)
            {
                RequestPath(Target.Position);
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

        private float GetSpeedMultiplier()
        {
            if (IsSlowed) return 0.65f;
            if (HasStatusEffect(StatusEffectType.Rage)) return 1.3f;
            return 1f;
        }

        private void RequestPath(Vector2 targetPos)
        {
            // Simplified: direct path or use pathfinding
            _path = sim._pathfinding.FindPath(Position, targetPos, IsFlying ? EntityType.Unit : EntityType.Unit);
            _pathIndex = 0;
        }

        private void TryAttack(BattleSimulation sim)
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

            // Create projectile or instant hit
            if (AttackRange > 1.5f) // Ranged
            {
                var projectile = new Projectile(
                    sim._nextEntityId++,
                    OwnerPlayerId,
                    this,
                    Target,
                    Stats.damage,
                    GetProjectileSpeed(),
                    CardData.mechanicsJson
                );
                sim.AddProjectile(projectile);
            }
            else // Melee
            {
                DealDamage(Target, Stats.damage, DamageType.Physical, sim);
            }

            // Handle special attack mechanics
            HandleAttackEffects(sim);
        }

        private float GetProjectileSpeed()
        {
            // Default projectile speeds
            if (CardData.cardName.Contains("Musketeer")) return 600f;
            if (CardData.cardName.Contains("Wizard")) return 450f;
            if (CardData.cardName.Contains("Princess")) return 450f;
            return 500f;
        }

        private void DealDamage(Entity target, int damage, DamageType type, BattleSimulation sim)
        {
            if (target == null || target.IsDead) return;

            // Apply damage modifiers
            int finalDamage = damage;

            // Target armor/resistances would go here

            target.TakeDamage(finalDamage, type, Id);

            // Log hit
            sim.LogEvent(new BattleEvent
            {
                tick = sim.CurrentTick,
                type = EventType.CardPlayed, // Generic
                playerId = OwnerPlayerId,
                position = target.Position
            });
        }

        private void HandleAttackEffects(BattleSimulation sim)
        {
            // Splash damage
            if (CardData.mechanicsJson?.Contains("splash") == true)
            {
                float radius = 1.5f; // Parse from JSON
                foreach (var entity in sim.GetPotentialTargets(this))
                {
                    if (entity.Id != Target?.Id && Vector2.Distance(entity.Position, Target.Position) <= radius)
                    {
                        DealDamage(entity, (int)(Stats.damage * 0.5f), DamageType.Area, sim);
                    }
                }
            }

            // Chain lightning (Electro Wizard, Electro Dragon)
            if (CardData.mechanicsJson?.Contains("chain") == true)
            {
                // Chain to nearby targets
            }

            // Pierce (Magic Archer)
            if (CardData.mechanicsJson?.Contains("pierce") == true)
            {
                // Projectile continues through targets
            }
        }

        private void UpdateCharge(float dt, BattleSimulation sim)
        {
            ChargeTimeRemaining -= dt;
            State = UnitState.Charging;

            Vector2 dir = (ChargeTarget - Position).normalized;
            float chargeSpeed = MoveSpeed * 2f; // Double speed during charge
            Position += dir * chargeSpeed * dt;

            // Check collision during charge
            foreach (var entity in sim.GetPotentialTargets(this))
            {
                if (Vector2.Distance(Position, entity.Position) < CollisionRadius + entity.CollisionRadius)
                {
                    // Hit during charge!
                    DealDamage(entity, Stats.damage * 2, DamageType.Physical, sim); // Double damage
                    EndCharge();
                    break;
                }
            }

            if (ChargeTimeRemaining <= 0)
            {
                EndCharge();
            }
        }

        public void StartCharge(Vector2 targetPos, float duration)
        {
            IsCharging = true;
            ChargeTarget = targetPos;
            ChargeTimeRemaining = duration;
            AddStatusEffect(new StatusEffect(StatusEffectType.Charge, duration, 2f, 0, Id));
            AddStatusEffect(new StatusEffect(StatusEffectType.Invulnerable, duration, 0, 0, Id));
        }

        public void EndCharge()
        {
            IsCharging = false;
            RemoveStatusEffect(StatusEffectType.Charge);
            RemoveStatusEffect(StatusEffectType.Invulnerable);
        }

        public bool TryUseAbility(Vector2 targetPos)
        {
            // Champion ability logic
            if (CardData.cardName == "Archer Queen")
            {
                StartInvisibility(3f);
                // Damage boost handled in GetSpeedMultiplier/GetDamageMultiplier
                return true;
            }
            else if (CardData.cardName == "Skeleton King")
            {
                // Spawn skeletons
                return true;
            }
            else if (CardData.cardName == "Mighty Miner")
            {
                // Dash
                return true;
            }
            return false;
        }

        private void StartInvisibility(float duration)
        {
            AddStatusEffect(new StatusEffect(StatusEffectType.Invisible, duration, 0, 0, Id));
        }

        public override void OnDeath(BattleSimulation sim)
        {
            base.OnDeath(sim);

            // Death spawns (Witch, Night Witch, Lava Hound, etc.)
            if (CardData.mechanicsJson?.Contains("deathSpawn") == true)
            {
                // Parse and spawn
            }

            sim.LogEvent(new BattleEvent
            {
                tick = sim.CurrentTick,
                type = EventType.UnitDied,
                playerId = OwnerPlayerId,
                cardId = CardData.cardId,
                position = Position
            });
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