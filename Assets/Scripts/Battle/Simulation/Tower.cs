using System;
using System.Collections.Generic;
using UnityEngine;
using CRClone.Core;

namespace CRClone.Battle.Simulation
{
    public class Tower : Entity
    {
        public TowerType Type { get; private set; }
        public bool IsActivated { get; private set; } // King Tower only
        public float AttackCooldown { get; private set; }
        public int Damage { get; private set; }
        public float HitSpeed { get; private set; }
        public float Range { get; private set; }
        public Entity Target { get; private set; }
        private Vector2 _lastTargetPosition;
        private float _retargetTimer = 0f;

        public Tower(uint id, int ownerPlayerId, EntityType type, TowerType towerType, Vector2 position, int hp, int damage, float hitSpeed, float range)
            : base(id, ownerPlayerId, type, position, hp)
        {
            Type = towerType;
            Damage = damage;
            HitSpeed = hitSpeed;
            Range = range;
            AttackCooldown = hitSpeed;
            CollisionRadius = 1f;
        }

        public override void Tick(float dt, BattleSimulation sim)
        {
            base.Tick(dt, sim);

            if (IsDead) return;

            // King Tower activation check
            if (Type == TowerType.King && !IsActivated)
            {
                CheckKingActivation(sim);
            }

            // Find target
            if (Target == null || Target.IsDead || !IsInRange(Target) || !HasLineOfSight(Target))
            {
                Target = FindTarget(sim);
            }

            // Attack
            if (Target != null)
            {
                _lastTargetPosition = Target.Position;
                AttackCooldown -= dt;
                if (AttackCooldown <= 0)
                {
                    PerformAttack(sim);
                    AttackCooldown = HitSpeed;
                }
            }
        }

        private void CheckKingActivation(BattleSimulation sim)
        {
            // King Tower activates when:
            // 1. Takes any damage
            // 2. Princess Tower destroyed
            // 3. Tornado pulls unit to King Tower (handled externally via ActivateKingTower)
            // 4. Fisherman hooks unit to King Tower (handled externally)

            if (CurrentHP < MaxHP)
            {
                ActivateKingTower(KingTowerActivationCause.Damaged);
                return;
            }

            // Check if either princess tower is destroyed
            foreach (var tower in sim._towers)
            {
                if (tower.OwnerPlayerId == OwnerPlayerId && 
                    (tower.Type == TowerType.PrincessLeft || tower.Type == TowerType.PrincessRight) && 
                    tower.IsDead)
                {
                    ActivateKingTower(KingTowerActivationCause.PrincessTowerDestroyed);
                    return;
                }
            }
        }

        public void ActivateKingTower(KingTowerActivationCause cause)
        {
            if (IsActivated) return;

            IsActivated = true;
            // Spawn 2 Guards at King Tower
            var guardData = Services.Get<DataManager>().GetCardByName("Guards");
            if (guardData != null)
            {
                var stats = guardData.GetStats(1); // Level based on king tower
                for (int i = 0; i < 2; i++)
                {
                    var offset = new Vector2(UnityEngine.Random.Range(-0.5f, 0.5f), UnityEngine.Random.Range(-0.5f, 0.5f));
                    var guard = new Unit(sim._nextEntityId++, OwnerPlayerId, guardData, stats, Position + offset, 1);
                    sim._units.Add(guard);
                    sim._entities[guard.Id] = guard;
                }
            }

            sim.LogEvent(new BattleEvent
            {
                tick = sim.CurrentTick,
                type = EventType.KingActivated,
                playerId = OwnerPlayerId,
                position = Position
            });
        }

        private Entity FindTarget(BattleSimulation sim)
        {
            var candidates = sim.GetPotentialTargets(this);
            Entity bestTarget = null;
            int bestPathDist = int.MaxValue;
            int bestPriority = int.MaxValue;

            foreach (var candidate in candidates)
            {
                if (!IsValidTarget(candidate)) continue;

                // Calculate path distance for more accurate targeting
                int pathDist = sim._pathfinding.GetPathDistance(Position, candidate.Position, false);
                
                // Target priority: troops > buildings > towers
                int priority = GetTargetPriority(candidate);

                if (pathDist < bestPathDist || (pathDist == bestPathDist && priority < bestPriority))
                {
                    bestPathDist = pathDist;
                    bestPriority = priority;
                    bestTarget = candidate;
                }
            }

            return bestTarget;
        }

        private int GetTargetPriority(Entity target)
        {
            // Priority: troops > buildings > towers
            if (target.Type == EntityType.Unit) return 0;
            if (target.Type == EntityType.Building) return 1;
            if (target.Type == EntityType.Tower) return 2;
            return 100;
        }

        private bool IsValidTarget(Entity target)
        {
            // Towers target both air and ground
            // Don't target invisible units
            if (target.IsInvisible) return false;
            
            // Don't target invulnerable units (Tesla retracted, etc.)
            if (target.IsInvulnerable) return false;

            return true;
        }

        private bool HasLineOfSight(Entity target)
        {
            // Simplified: towers have LOS to everything in range
            // In a full implementation, this would check for obstacles
            return true;
        }

        private bool IsInRange(Entity target)
        {
            return Vector2.Distance(Position, target.Position) <= Range + target.CollisionRadius;
        }

        private void PerformAttack(BattleSimulation sim)
        {
            if (Target == null) return;

            // Create projectile
            var projectile = new Projectile(
                sim._nextEntityId++,
                OwnerPlayerId,
                this,
                Target,
                Damage,
                600f, // Tower projectile speed
                ""
            );
            sim.AddProjectile(projectile);

            // Visual: tower muzzle flash
        }
    }

    public enum TowerType
    {
        King = 0,
        PrincessLeft = 1,
        PrincessRight = 2
    }
}