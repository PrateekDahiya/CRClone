using System;
using System.Collections.Generic;
using UnityEngine;
using CRClone.Core;
using CRClone.Data;

namespace CRClone.Battle.Simulation
{
    public class SpellEffect : Entity
    {
        public CardData SpellData { get; private set; }
        public Vector2 CenterPosition { get; private set; }
        public float Radius { get; private set; }
        public float Duration { get; private set; }
        public float RemainingTime { get; private set; }
        public SpellType Type { get; private set; }
        public bool IsFinished => RemainingTime <= 0;

        // Spell-specific data
        private int _damage;
        private int _damagePerTick;
        private float _tickInterval;
        private float _tickTimer;
        private float _knockback;
        private float _slowPercent;
        private int _spawnCount;
        private int _spawnedCount;
        private CardData _spawnCardData;
        private bool _isInstant;
        private bool _hasAppliedInstant;
        private float _logTravelDistance;
        private Vector2 _logDirection;
        private int _lightningStrikesDone;
        private float _lightningStrikeTimer;

        public static SpellEffect CreateFromCard(uint id, int ownerPlayerId, CardData cardData, Vector2 position, int level)
        {
            var stats = cardData.GetStats(level);
            var spell = new SpellEffect(id, ownerPlayerId, cardData, position, stats);
            return spell;
        }

        private SpellEffect(uint id, int ownerPlayerId, CardData cardData, Vector2 position, CardLevelStats stats)
            : base(id, ownerPlayerId, EntityType.SpellEffect, position, 1)
        {
            SpellData = cardData;
            CenterPosition = position;
            Duration = GetSpellDuration(cardData);
            RemainingTime = Duration;
            Type = GetSpellType(cardData);
            _isInstant = IsInstantSpell(cardData);
            _hasAppliedInstant = false;
            _spawnedCount = 0;
            _lightningStrikesDone = 0;
            _lightningStrikeTimer = 0f;

            ParseSpellStats(cardData, stats);
        }

        private SpellType GetSpellType(CardData card)
        {
            return card.cardName switch
            {
                // Damage spells
                "Fireball" => SpellType.Damage,
                "Rocket" => SpellType.Damage,
                "Lightning" => SpellType.Damage,
                "Poison" => SpellType.DamageOverTime,
                "Earthquake" => SpellType.Damage,
                "Giant Snowball" => SpellType.Damage,
                "Zap" => SpellType.Damage,
                "Arrows" => SpellType.Damage,
                "The Log" => SpellType.Damage,
                "Royal Delivery" => SpellType.Damage | SpellType.Spawn,
                "Barbarian Barrel" => SpellType.Damage | SpellType.Spawn,
                "Firecracker" => SpellType.Damage, // Not a spell but projectile

                // Utility spells
                "Freeze" => SpellType.Utility,
                "Rage" => SpellType.Utility,
                "Clone" => SpellType.Utility,
                "Mirror" => SpellType.Utility,
                "Tornado" => SpellType.Utility,
                "Graveyard" => SpellType.Spawn,

                // Spawn spells
                "Goblin Barrel" => SpellType.Spawn,
                "Skeleton Barrel" => SpellType.Spawn,

                _ => SpellType.Damage
            };
        }

        private bool IsInstantSpell(CardData card)
        {
            return card.cardName == "Zap" || card.cardName == "Arrows" || 
                   card.cardName == "The Log" || card.cardName == "Freeze" ||
                   card.cardName == "Rage" || card.cardName == "Tornado" ||
                   card.cardName == "Earthquake" || card.cardName == "Giant Snowball" ||
                   card.cardName == "Lightning" || card.cardName == "Clone" ||
                   card.cardName == "Goblin Barrel" || card.cardName == "Skeleton Barrel" ||
                   card.cardName == "Royal Delivery" || card.cardName == "Barbarian Barrel";
        }

        private float GetSpellDuration(CardData card)
        {
            return card.cardName switch
            {
                "Poison" => 8f,
                "Freeze" => 4f,
                "Rage" => 6f,
                "Tornado" => 1.5f,
                "Graveyard" => 8f, // 3s spawn + skeleton lifetime
                "Goblin Barrel" => 1.2f, // Travel time
                "Skeleton Barrel" => 3f,
                "Royal Delivery" => 1.5f,
                "Barbarian Barrel" => 1f,
                "Fireball" => 1f, // Travel time
                "Rocket" => 1.5f, // Travel time
                _ => 0f // Instant
            };
        }

        private void ParseSpellStats(CardData card, CardLevelStats stats)
        {
            Radius = GetSpellRadius(card);
            _damage = stats.damage;
            _knockback = GetKnockback(card);
            _slowPercent = 0.35f; // Standard slow

            switch (card.cardName)
            {
                case "Fireball":
                    _damage = stats.damage;
                    _knockback = 0.5f;
                    break;
                case "Rocket":
                    _damage = stats.damage;
                    break;
                case "Lightning":
                    _damage = stats.damage; // Per strike
                    break;
                case "Poison":
                    _damagePerTick = 65; // Per 0.5s at tournament standard
                    _tickInterval = 0.5f;
                    break;
                case "Freeze":
                    // No damage, just freeze
                    break;
                case "Rage":
                    // Buff, no damage
                    break;
                case "Tornado":
                    // Pull effect, no damage
                    break;
                case "Graveyard":
                    _spawnCount = 15;
                    _spawnCardData = Services.Get<DataManager>().GetCardByName("Skeleton");
                    break;
                case "Zap":
                    _damage = stats.damage;
                    break;
                case "Arrows":
                    _damage = stats.damage;
                    break;
                case "The Log":
                    _damage = stats.damage;
                    _knockback = 0.5f;
                    _logTravelDistance = 11.5f;
                    break;
                case "Giant Snowball":
                    _damage = stats.damage;
                    _knockback = 0.5f;
                    break;
                case "Earthquake":
                    _damage = stats.damage;
                    break;
                case "Royal Delivery":
                    _damage = stats.damage;
                    _spawnCardData = Services.Get<DataManager>().GetCardByName("Royal Recruit");
                    break;
                case "Barbarian Barrel":
                    _damage = stats.damage;
                    _spawnCardData = Services.Get<DataManager>().GetCardByName("Barbarian");
                    break;
                case "Clone":
                    Radius = 3f;
                    break;
                case "Mirror":
                    // Handled elsewhere - mirrors last played card
                    break;
            }
        }

        private float GetSpellRadius(CardData card)
        {
            return card.cardName switch
            {
                "Fireball" => 2.5f,
                "Rocket" => 2f,
                "Lightning" => 3.5f,
                "Poison" => 3.5f,
                "Freeze" => 3f,
                "Rage" => 3.5f,
                "Tornado" => 5.5f,
                "Graveyard" => 4f,
                "Zap" => 2.5f,
                "Arrows" => 4f,
                "The Log" => 2.5f, // Width of the log
                "Giant Snowball" => 2.5f,
                "Earthquake" => 3.5f,
                "Royal Delivery" => 2.5f,
                "Barbarian Barrel" => 2.5f,
                "Clone" => 3f,
                _ => 2f
            };
        }

        private float GetKnockback(CardData card)
        {
            return card.cardName switch
            {
                "Fireball" => 0.5f,
                "The Log" => 0.5f,
                "Giant Snowball" => 0.5f,
                "Rocket" => 0f,
                _ => 0f
            };
        }

        public override void Tick(float dt, BattleSimulation sim)
        {
            base.Tick(dt, sim);

            RemainingTime -= dt;

            // Instant spells apply immediately on first tick
            if (_isInstant && !_hasAppliedInstant)
            {
                ApplyInstantEffect(sim);
                _hasAppliedInstant = true;
                if (Duration <= 0) RemainingTime = 0;
            }

            // Damage over time spells
            if (Type == SpellType.DamageOverTime)
            {
                _tickTimer += dt;
                if (_tickTimer >= _tickInterval)
                {
                    _tickTimer = 0f;
                    ApplyDamageOverTime(sim);
                }
            }

            // Spawn over time (Graveyard)
            if (SpellData.cardName == "Graveyard")
            {
                // Spawn skeletons over 3 seconds
                float spawnRate = _spawnCount / 3f; // 5 per second
                int toSpawn = (int)(spawnRate * dt);
                for (int i = 0; i < toSpawn && _spawnedCount < _spawnCount; i++)
                {
                    SpawnSkeleton(sim);
                    _spawnedCount++;
                }
            }

            // Tornado pull effect
            if (SpellData.cardName == "Tornado")
            {
                ApplyTornadoPull(sim);
            }

            // Lightning strikes over time
            if (SpellData.cardName == "Lightning")
            {
                _lightningStrikeTimer += dt;
                if (_lightningStrikeTimer >= 0.4f && _lightningStrikesDone < 3)
                {
                    _lightningStrikeTimer = 0f;
                    ApplyLightningStrike(sim);
                    _lightningStrikesDone++;
                }
            }

            // Log travel
            if (SpellData.cardName == "The Log" || SpellData.cardName == "Barbarian Barrel" || SpellData.cardName == "Royal Delivery")
            {
                UpdateTravelingSpell(sim, dt);
            }

            // Royal Delivery / Barbarian Barrel spawn at end
            if ((SpellData.cardName == "Royal Delivery" || SpellData.cardName == "Barbarian Barrel") && RemainingTime <= 0 && _spawnCardData != null)
            {
                SpawnAtEnd(sim);
            }
        }

        private void ApplyInstantEffect(BattleSimulation sim)
        {
            var targets = GetAffectedTargets(sim);

            foreach (var target in targets)
            {
                if (target.IsDead) continue;

                // Damage
                if (_damage > 0)
                {
                    target.TakeDamage(_damage, DamageType.Spell, Id);
                }

                // Knockback
                if (_knockback > 0)
                {
                    ApplyKnockback(target);
                }

                // Stun (Zap, Lightning)
                if (SpellData.cardName == "Zap" || SpellData.cardName == "Lightning")
                {
                    target.AddStatusEffect(new StatusEffect(StatusEffectType.Stun, 0.5f, 0, 0, Id));
                }

                // Freeze
                if (SpellData.cardName == "Freeze")
                {
                    target.AddStatusEffect(new StatusEffect(StatusEffectType.Freeze, Duration, 0, 0, Id));
                }

                // Slow (Poison, Giant Snowball)
                if (SpellData.cardName == "Poison" || SpellData.cardName == "Giant Snowball")
                {
                    target.AddStatusEffect(new StatusEffect(StatusEffectType.Slow, Duration, _slowPercent, 0, Id));
                }

                // Rage buff (friendly)
                if (SpellData.cardName == "Rage" && target.OwnerPlayerId == OwnerPlayerId)
                {
                    target.AddStatusEffect(new StatusEffect(StatusEffectType.Rage, Duration, 0.5f, 0, Id));
                }

                // Clone
                if (SpellData.cardName == "Clone" && target.OwnerPlayerId == OwnerPlayerId && target.Type == EntityType.Unit)
                {
                    CloneUnit(target, sim);
                }

                // Earthquake: extra damage to buildings, stun
                if (SpellData.cardName == "Earthquake")
                {
                    int damage = _damage;
                    if (target.Type == EntityType.Building)
                    {
                        damage *= 2;
                    }
                    target.TakeDamage(damage, DamageType.Spell, Id);
                    target.AddStatusEffect(new StatusEffect(StatusEffectType.Stun, 1f, 0, 0, Id));
                }
            }

            // Special spells
            switch (SpellData.cardName)
            {
                case "The Log":
                    // Log pushes all ground units in path
                    ApplyLogPush(sim);
                    break;
                case "Lightning":
                    // First strike applied above, rest in tick
                    break;
                case "Tornado":
                    // Pull applied over duration
                    break;
                case "Earthquake":
                    // Already applied above
                    break;
                case "Royal Delivery":
                    // Spawn recruit after delay - handled in UpdateTravelingSpell
                    break;
                case "Barbarian Barrel":
                    // Spawn barbarian at end - handled in UpdateTravelingSpell
                    break;
            }
        }

        private void ApplyDamageOverTime(BattleSimulation sim)
        {
            var targets = GetAffectedTargets(sim);
            foreach (var target in targets)
            {
                if (target.IsDead) continue;
                target.TakeDamage(_damagePerTick, DamageType.Spell, Id);
                
                // Reapply slow
                if (SpellData.cardName == "Poison")
                {
                    target.AddStatusEffect(new StatusEffect(StatusEffectType.Slow, _tickInterval * 2, _slowPercent, 0, Id));
                }
            }
        }

        private void ApplyTornadoPull(BattleSimulation sim)
        {
            var targets = GetAffectedTargets(sim);
            foreach (var target in targets)
            {
                if (target.IsDead) continue;

                Vector2 dir = (CenterPosition - target.Position).normalized;
                float pullStrength = 4f * BattleSimulation.FIXED_DT; // Max 4 tiles over duration
                target.Position += dir * pullStrength;

                // If pulled to King Tower, activate it
                foreach (var tower in sim._towers)
                {
                    if (tower.OwnerPlayerId != OwnerPlayerId && tower.Type == TowerType.King)
                    {
                        if (Vector2.Distance(target.Position, tower.Position) < 1.5f)
                        {
                            tower.ActivateKingTower(KingTowerActivationCause.TornadoPull);
                        }
                    }
                }
            }
        }

        private void ApplyLogPush(BattleSimulation sim)
        {
            // Log travels horizontally across arena
            // Pushes ground units back 0.5 tiles
            var targets = GetAffectedTargets(sim);
            foreach (var target in targets)
            {
                if (target.IsDead) continue;
                
                // Only ground units
                if (target.Type == EntityType.Unit)
                {
                    var unit = target as Unit;
                    if (unit != null && unit.IsFlying) continue;
                }
                
                // Push perpendicular to log travel (vertical push)
                Vector2 pushDir = new Vector2(0, OwnerPlayerId == 1 ? 1 : -1);
                target.Position += pushDir * _knockback;
            }
        }

        private void ApplyLightningStrike(BattleSimulation sim)
        {
            var targets = GetAffectedTargets(sim);
            // Lightning hits 3 highest HP targets in radius
            targets.Sort((a, b) => b.CurrentHP.CompareTo(a.CurrentHP));
            
            int strikes = Math.Min(3 - _lightningStrikesDone, targets.Count);
            for (int i = 0; i < strikes; i++)
            {
                var target = targets[i];
                target.TakeDamage(_damage, DamageType.Spell, Id);
                target.AddStatusEffect(new StatusEffect(StatusEffectType.Stun, 0.5f, 0, 0, Id));
                
                // Visual: lightning strike effect
            }
        }

        private void UpdateTravelingSpell(BattleSimulation sim, float dt)
        {
            // Log, Barbarian Barrel, Royal Delivery travel across arena
            float travelSpeed = SpellData.cardName == "The Log" ? 12f : 8f; // tiles per second
            float travelDist = travelSpeed * dt;
            _logTravelDistance -= travelDist;
            
            if (_logTravelDistance <= 0)
            {
                RemainingTime = 0;
            }
        }

        private void SpawnAtEnd(BattleSimulation sim)
        {
            if (_spawnCardData == null) return;

            var stats = _spawnCardData.GetStats(Level);
            var unit = new Unit(sim._nextEntityId++, OwnerPlayerId, _spawnCardData, stats, CenterPosition, Level);
            sim._units.Add(unit);
            sim._entities[unit.Id] = unit;
        }

        private void SpawnSkeleton(BattleSimulation sim)
        {
            if (_spawnCardData == null) return;

            var stats = _spawnCardData.GetStats(1); // Skeletons always level 1 equivalent
            float angle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float radius = UnityEngine.Random.Range(0f, Radius);
            Vector2 spawnPos = CenterPosition + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;

            var unit = new Unit(sim._nextEntityId++, OwnerPlayerId, _spawnCardData, stats, spawnPos, 1);
            sim._units.Add(unit);
            sim._entities[unit.Id] = unit;
        }

        private void CloneUnit(Entity original, BattleSimulation sim)
        {
            if (original.Type != EntityType.Unit) return;
            var unit = original as Unit;
            if (unit == null) return;

            // Cannot clone Champions
            if (unit.CardData.rarity == CardRarity.Champion) return;

            // Clone at -1 level (same HP%)
            var cardData = unit.CardData;
            int cloneLevel = Math.Max(1, unit.Level - 1);
            var cloneStats = cardData.GetStats(cloneLevel);

            Vector2 offset = new Vector2(UnityEngine.Random.Range(-0.5f, 0.5f), UnityEngine.Random.Range(-0.5f, 0.5f));
            var clone = new Unit(sim._nextEntityId++, OwnerPlayerId, cardData, cloneStats, unit.Position + offset, cloneLevel);
            
            // Set HP to same percentage
            float hpPercent = (float)unit.CurrentHP / unit.MaxHP;
            clone.CurrentHP = Mathf.RoundToInt(clone.MaxHP * hpPercent);

            clone.Target = unit.Target;
            sim._units.Add(clone);
            sim._entities[clone.Id] = clone;
        }

        private List<Entity> GetAffectedTargets(BattleSimulation sim)
        {
            var targets = new List<Entity>();

            if (SpellData.cardName == "The Log" || SpellData.cardName == "Barbarian Barrel")
            {
                // Log/Barbarian Barrel: line across arena, ground units only
                // Simplified: all ground units in radius along path
                foreach (var unit in sim._units)
                {
                    if (unit.IsFlying) continue; // Air units unaffected
                    
                    // Check if in log path (rectangle along travel direction)
                    float distToLine = Math.Abs(unit.Position.y - CenterPosition.y);
                    if (distToLine <= Radius && unit.Position.x >= 0 && unit.Position.x <= 18)
                    {
                        targets.Add(unit);
                    }
                }
                
                // Also affect buildings in path
                foreach (var building in sim._buildings)
                {
                    float distToLine = Math.Abs(building.Position.y - CenterPosition.y);
                    if (distToLine <= Radius && building.Position.x >= 0 && building.Position.x <= 18)
                    {
                        targets.Add(building);
                    }
                }
            }
            else if (SpellData.cardName == "Lightning")
            {
                // All units in radius (will pick top 3 HP)
                foreach (var entity in sim.GetAllEntitiesInRadius(CenterPosition, Radius))
                {
                    if (entity.OwnerPlayerId != OwnerPlayerId)
                        targets.Add(entity);
                }
            }
            else if (SpellData.cardName == "Royal Delivery")
            {
                // Damage in radius on landing, then spawn
                foreach (var entity in sim.GetAllEntitiesInRadius(CenterPosition, Radius))
                {
                    if (entity.OwnerPlayerId != OwnerPlayerId)
                        targets.Add(entity);
                }
            }
            else
            {
                // Standard radius
                foreach (var entity in sim.GetAllEntitiesInRadius(CenterPosition, Radius))
                {
                    if (entity.OwnerPlayerId != OwnerPlayerId)
                        targets.Add(entity);
                }
            }

            return targets;
        }

        private void ApplyKnockback(Entity target)
        {
            Vector2 dir = (target.Position - CenterPosition).normalized;
            if (dir == Vector2.zero) dir = new Vector2(UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-1f, 1f)).normalized;
            target.Position += dir * _knockback;
        }
    }

    public enum SpellType
    {
        Damage = 1,
        DamageOverTime = 2,
        Utility = 4,
        Spawn = 8
    }

    public enum KingTowerActivationCause
    {
        Damaged,
        PrincessTowerDestroyed,
        TornadoPull,
        FishermanHook
    }
}