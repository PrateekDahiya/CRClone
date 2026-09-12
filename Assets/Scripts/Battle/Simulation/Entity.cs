using System;
using System.Collections.Generic;
using UnityEngine;
using CRClone.Core;
using CRClone.Data;

namespace CRClone.Battle.Simulation
{
    public abstract class Entity
    {
        public uint Id { get; protected set; }
        public int OwnerPlayerId { get; protected set; }
        public EntityType Type { get; protected set; }
        public Vector2 Position { get; set; }
        public Vector2 Velocity { get; set; }
        public float Rotation { get; set; }
        public int CurrentHP { get; protected set; }
        public int MaxHP { get; protected set; }
        public float CollisionRadius { get; protected set; } = 0.5f;
        public bool IsDead => CurrentHP <= 0;
        public uint TargetId { get; set; }
        public List<StatusEffect> StatusEffects { get; } = new();

        protected Entity(uint id, int ownerPlayerId, EntityType type, Vector2 position, int maxHP)
        {
            Id = id;
            OwnerPlayerId = ownerPlayerId;
            Type = type;
            Position = position;
            MaxHP = maxHP;
            CurrentHP = maxHP;
        }

        public virtual void Tick(float dt, BattleSimulation sim) { }

        public virtual void TakeDamage(int amount, DamageType damageType, uint sourceId)
        {
            if (IsDead) return;

            // Apply damage modifiers
            int finalDamage = amount;

            // Shield absorption
            var shield = GetStatusEffect(StatusEffectType.Shield);
            if (shield != null)
            {
                int absorbed = Math.Min(shield.RemainingAmount, finalDamage);
                shield.RemainingAmount -= absorbed;
                finalDamage -= absorbed;
                if (shield.RemainingAmount <= 0) RemoveStatusEffect(StatusEffectType.Shield);
            }

            CurrentHP -= finalDamage;
            if (CurrentHP < 0) CurrentHP = 0;

            // Log damage event
            sim.LogEvent(new BattleEvent
            {
                tick = sim.CurrentTick,
                type = EventType.TowerDamaged, // Generic damage event
                playerId = OwnerPlayerId,
                position = Position
            });
        }

        public virtual void Die(DeathCause cause = DeathCause.Damage)
        {
            CurrentHP = 0;
        }

        public virtual void OnDeath(BattleSimulation sim) { }

        public void AddStatusEffect(StatusEffect effect)
        {
            // Check for existing effect of same type
            var existing = GetStatusEffect(effect.Type);
            if (existing != null)
            {
                existing.Refresh(effect.Duration);
                return;
            }
            StatusEffects.Add(effect);
            OnStatusEffectAdded(effect);
        }

        public void RemoveStatusEffect(StatusEffectType type)
        {
            var effect = GetStatusEffect(type);
            if (effect != null)
            {
                StatusEffects.RemoveAll(e => e.Type == type);
                OnStatusEffectRemoved(effect);
            }
        }

        protected virtual void OnStatusEffectAdded(StatusEffect effect) { }
        protected virtual void OnStatusEffectRemoved(StatusEffect effect) { }

        public StatusEffect GetStatusEffect(StatusEffectType type)
        {
            return StatusEffects.Find(e => e.Type == type);
        }

        public bool HasStatusEffect(StatusEffectType type)
        {
            return GetStatusEffect(type) != null;
        }

        public bool IsStunned => HasStatusEffect(StatusEffectType.Stun);
        public bool IsFrozen => HasStatusEffect(StatusEffectType.Freeze);
        public bool IsSlowed => HasStatusEffect(StatusEffectType.Slow);
        public bool IsInvisible => HasStatusEffect(StatusEffectType.Invisible);
        public bool IsInvulnerable => HasStatusEffect(StatusEffectType.Invulnerable);

        public void UpdateStatusEffects(float dt)
        {
            for (int i = StatusEffects.Count - 1; i >= 0; i--)
            {
                var effect = StatusEffects[i];
                effect.RemainingTime -= dt;
                if (effect.RemainingTime <= 0)
                {
                    StatusEffects.RemoveAt(i);
                }
            }
        }
    }

    public enum DamageType
    {
        Physical,
        Spell,
        Area,
        Beam,
        Siege,
        True
    }

    public enum DeathCause
    {
        Damage,
        Spell,
        LifetimeExpired,
        Suicide,
        Sacrifice
    }

    public class StatusEffect
    {
        public StatusEffectType Type { get; }
        public float RemainingTime { get; set; }
        public float Duration { get; }
        public int RemainingAmount { get; set; } // For shields
        public float Value { get; } // Slow %, damage per tick, etc.
        public uint SourceId { get; }

        public StatusEffect(StatusEffectType type, float duration, float value = 0, int amount = 0, uint sourceId = 0)
        {
            Type = type;
            Duration = duration;
            RemainingTime = duration;
            Value = value;
            RemainingAmount = amount;
            SourceId = sourceId;
        }

        public void Refresh(float newDuration)
        {
            RemainingTime = Math.Max(RemainingTime, newDuration);
        }
    }

    public enum StatusEffectType
    {
        None = 0,
        Stun = 1,           // Cannot move/attack
        Freeze = 2,         // Complete stop
        Slow = 3,           // 35% move/attack speed reduction
        Poison = 4,         // Damage over time
        Burn = 5,           // Damage over time (fire)
        Invisible = 6,      // Untargetable
        Invulnerable = 7,   // No damage taken
        Shield = 8,         // Absorbs damage
        Charge = 9,         // Charging (double damage, fast, invuln)
        Rage = 10,          // +50% atk speed, +30% move speed
        Snare = 11,         // Cannot move (can attack)
        Silence = 12        // Cannot use abilities
    }
}