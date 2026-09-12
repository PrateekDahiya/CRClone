using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using CRClone.Battle.Simulation;

namespace CRClone.Network
{
    // Client-side reconciliation helpers (Agent2 deliverable 2.11).
    //
    // The entity hash mirrors the server ReplayRecorder.computeEntitiesHash
    // algorithm exactly (id, round(x*1000), round(y*1000), hp combined with
    // h = h*31 + v in int32 arithmetic, hex-encoded like JS Number.toString(16)):
    //   hash = 0
    //   for entity (id-sorted): hash = hash*31 + id; hash = hash*31 + round(x*1000);
    //                           hash = hash*31 + round(y*1000); hash = hash*31 + hp
    // Both sides sort by entity id so the comparison is order-independent.
    // NOTE: C# Math.Round uses banker's rounding by default while JS Math.round
    // rounds half up; AwayFromZero is used to match JS for all values except
    // exact negative x.5 ties (vanishingly rare for world positions).
    public static class Reconciler
    {
        public static string ComputeEntityHash(IEnumerable<EntityState> entities)
        {
            int hash = 0;
            unchecked
            {
                foreach (var e in entities.OrderBy(e => e.id))
                {
                    hash = hash * 31 + (int)e.id;
                    hash = hash * 31 + (int)Math.Round(e.position.x * 1000f, MidpointRounding.AwayFromZero);
                    hash = hash * 31 + (int)Math.Round(e.position.y * 1000f, MidpointRounding.AwayFromZero);
                    hash = hash * 31 + e.hp;
                }
            }
            return ToHex(hash);
        }

        private static string ToHex(int hash)
        {
            // Match JS Number.toString(16): negatives render as "-<hex of magnitude>".
            if (hash < 0) return "-" + (-(long)hash).ToString("x");
            return hash.ToString("x");
        }

        // Snapshot of a live simulation entity for hashing/comparison.
        public static EntityState ToEntityState(Entity entity)
        {
            return new EntityState
            {
                id = entity.Id,
                position = entity.Position,
                velocity = entity.Velocity,
                hp = entity.CurrentHP,
                isDead = entity.IsDead,
                targetId = entity.TargetId,
            };
        }

        // Applies an authoritative GameState snapshot: entity positions, HP,
        // elixir and tick bookkeeping. Returns the number of entities applied.
        // Entities unknown locally (e.g. already-removed dead entities) are skipped.
        public static int ApplySnapshot(CRClone.Battle.Simulation.BattleSimulation sim, GameStateMessage state)
        {
            if (sim == null || state == null) return 0;

            // Record the authoritative tick via the existing simulation API.
            sim.Reconcile(state);

            int applied = 0;
            if (state.entities != null)
                applied += ApplyEntities(sim, state.entities);
            if (state.projectiles != null)
                applied += ApplyEntities(sim, state.projectiles);

            if (state.player1 != null && sim.Player1 != null)
                sim.Player1.Elixir = state.player1.elixir;
            if (state.player2 != null && sim.Player2 != null)
                sim.Player2.Elixir = state.player2.elixir;

            return applied;
        }

        // Full-resync variant for ReconcileMessage (entities + tick only).
        public static int ApplySnapshot(CRClone.Battle.Simulation.BattleSimulation sim, ReconcileMessage msg)
        {
            if (sim == null || msg == null) return 0;
            if (msg.entities == null) return 0;
            return ApplyEntities(sim, msg.entities);
        }

        private static int ApplyEntities(CRClone.Battle.Simulation.BattleSimulation sim, EntityState[] states)
        {
            int applied = 0;
            foreach (var s in states)
            {
                var local = sim.GetEntity(s.id);
                if (local == null) continue;

                local.Position = s.position;
                local.Velocity = s.velocity;
                local.TargetId = s.targetId;
                if (s.isDead && !local.IsDead)
                {
                    local.Die();
                }
                else if (!s.isDead)
                {
                    local.CurrentHP = s.hp;
                }
                applied++;
            }
            return applied;
        }
    }
}
