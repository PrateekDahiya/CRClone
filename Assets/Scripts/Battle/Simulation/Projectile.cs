using System;
using System.Collections.Generic;
using UnityEngine;
using CRClone.Core;
using CRClone.Data;

namespace CRClone.Battle.Simulation
{
    public class Projectile : Entity
    {
        public Entity Source { get; private set; }
        public Entity Target { get; private set; }
        public int Damage { get; private set; }
        public float Speed { get; private set; }
        public bool IsHoming { get; private set; }
        public bool IsBeam { get; private set; }
        public bool IsMortarShot { get; set; }
        public bool Is360Splash { get; set; } // Valkyrie, Dark Prince
        public float SplashRadius { get; set; }
        public int PierceCount { get; private set; }
        public int ChainCount { get; private set; }
        public float ChainRange { get; private set; }
        public DamageType DamageType { get; private set; }
        public new bool IsDead { get; private set; }

        private Vector2 _startPosition;
        private float _travelTime = 0f;
        private HashSet<uint> _hitEntities = new HashSet<uint>();

        public Projectile(uint id, int ownerPlayerId, Entity source, Entity target, int damage, float speed, string mechanics)
            : base(id, ownerPlayerId, EntityType.Projectile, source.Position, 1)
        {
            Source = source;
            Target = target;
            Damage = damage;
            Speed = speed;
            _startPosition = source.Position;
            Position = source.Position;
            DamageType = DamageType.Physical;

            ParseMechanics(mechanics);
        }

        private void ParseMechanics(string mechanics)
        {
            if (string.IsNullOrEmpty(mechanics)) return;

            // Parse JSON for projectile properties
            // Simplified for now
            if (mechanics.Contains("pierce")) PierceCount = 999; // Infinite pierce (Magic Archer)
            if (mechanics.Contains("chain"))
            {
                ChainCount = 2; // Electro Wizard, Electro Dragon
                ChainRange = 4f;
            }
            if (mechanics.Contains("splash"))
            {
                SplashRadius = 1.5f;
            }
        }

        public override void Tick(float dt, BattleSimulation sim)
        {
            if (IsDead) return;

            if (IsBeam || Speed <= 0)
            {
                // Instant hit or beam
                if (Target != null && !Target.IsDead)
                {
                    HitTarget(sim);
                }
                IsDead = true;
                return;
            }

            if (Target == null || Target.IsDead)
            {
                // Target lost - continue to last known position or die
                IsDead = true;
                return;
            }

            // Move toward target
            Vector2 dir = (Target.Position - Position).normalized;
            float moveDist = Speed * dt;

            // For mortar shots, use arc trajectory
            if (IsMortarShot)
            {
                _travelTime += dt;
                float totalTravelTime = Vector2.Distance(_startPosition, Target.Position) / Speed;
                float progress = _travelTime / totalTravelTime;

                if (progress >= 1f)
                {
                    Position = Target.Position;
                    HitTarget(sim);
                    IsDead = true;
                    return;
                }

                // Parabolic arc
                Position = Vector2.Lerp(_startPosition, Target.Position, progress);
                float arcHeight = 4f * progress * (1f - progress) * 2f; // Max height 2 tiles
                // Visual arc handled in presentation
            }
            else
            {
                // Direct movement
                float distToTarget = Vector2.Distance(Position, Target.Position);
                if (distToTarget <= moveDist + Target.CollisionRadius)
                {
                    Position = Target.Position;
                    HitTarget(sim);
                    IsDead = true;
                }
                else
                {
                    Position += dir * moveDist;
                }
            }
        }

        private void HitTarget(BattleSimulation sim)
        {
            if (Target == null || Target.IsDead) return;

            // Apply damage
            int finalDamage = Damage;

            // 360 splash (Valkyrie, Dark Prince) - splash from source position
            if (Is360Splash && SplashRadius > 0)
            {
                foreach (var entity in sim.GetPotentialTargets(this))
                {
                    if (Vector2.Distance(entity.Position, Source.Position) <= SplashRadius)
                    {
                        entity.TakeDamage(finalDamage, DamageType, Source.Id);
                    }
                }
            }
            // Normal splash - splash from impact point
            else if (SplashRadius > 0)
            {
                // Main target takes full damage
                Target.TakeDamage(finalDamage, DamageType, Source.Id);
                _hitEntities.Add(Target.Id);

                // Splash damage to nearby entities
                foreach (var entity in sim.GetPotentialTargets(this))
                {
                    if (_hitEntities.Contains(entity.Id)) continue;
                    if (Vector2.Distance(entity.Position, Target.Position) <= SplashRadius)
                    {
                        entity.TakeDamage((int)(finalDamage * 0.5f), DamageType.Area, Source.Id);
                        _hitEntities.Add(entity.Id);
                    }
                }
            }
            else
            {
                Target.TakeDamage(finalDamage, DamageType, Source.Id);
                _hitEntities.Add(Target.Id);
            }

            // Handle pierce (Magic Archer) - continue in same direction
            if (PierceCount > 0)
            {
                // Find next target in line
                Vector2 direction = (Target.Position - Source.Position).normalized;
                Entity nextTarget = FindPierceTarget(sim, direction);
                
                if (nextTarget != null)
                {
                    Target = nextTarget;
                    IsDead = false; // Continue
                    return;
                }
            }

            // Handle chain (Electro Wizard, Electro Dragon, Electro Spirit)
            if (ChainCount > 0)
            {
                ChainToNearby(sim);
            }

            IsDead = true;
        }

        private Entity FindPierceTarget(BattleSimulation sim, Vector2 direction)
        {
            Entity bestTarget = null;
            float bestDist = float.MaxValue;

            foreach (var entity in sim.GetPotentialTargets(this))
            {
                if (_hitEntities.Contains(entity.Id)) continue;
                if (entity.IsDead) continue;

                // Check if entity is in the projectile's path (within 0.5 tiles of line)
                Vector2 toEntity = entity.Position - Source.Position;
                float projDist = Vector2.Dot(toEntity, direction);
                if (projDist <= 0) continue; // Behind source

                Vector2 closestPoint = Source.Position + direction * projDist;
                float perpDist = Vector2.Distance(entity.Position, closestPoint);
                
                if (perpDist <= 0.5f && projDist < bestDist)
                {
                    bestDist = projDist;
                    bestTarget = entity;
                }
            }

            return bestTarget;
        }

        private void ChainToNearby(BattleSimulation sim)
        {
            int chainsLeft = ChainCount;
            Entity currentTarget = Target;
            var hitEntities = new HashSet<uint>(_hitEntities);

            while (chainsLeft > 0)
            {
                Entity nextTarget = null;
                float bestDist = float.MaxValue;

                foreach (var entity in sim.GetPotentialTargets(this))
                {
                    if (hitEntities.Contains(entity.Id)) continue;
                    if (entity.IsDead) continue;

                    float dist = Vector2.Distance(currentTarget.Position, entity.Position);
                    if (dist <= ChainRange && dist < bestDist)
                    {
                        bestDist = dist;
                        nextTarget = entity;
                    }
                }

                if (nextTarget == null) break;

                // Chain hit!
                nextTarget.TakeDamage(Damage, DamageType, Source.Id);
                hitEntities.Add(nextTarget.Id);
                currentTarget = nextTarget;
                chainsLeft--;

                // Visual: chain lightning effect between currentTarget and nextTarget
            }
        }
    }
}