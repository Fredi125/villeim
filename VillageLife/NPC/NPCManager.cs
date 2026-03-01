using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VillageLife.Util;

namespace VillageLife.NPC
{
    /// <summary>
    /// Central registry for all placed VillageLife NPCs.
    /// Manages NPC lifecycle, lookup, and per-player NPC limits.
    /// </summary>
    public static class NPCManager
    {
        private static readonly Dictionary<ZDOID, VillageNPC> _npcs = new Dictionary<ZDOID, VillageNPC>();
        private static float _tickTimer;
        private const float TickInterval = 1f;

        public static IReadOnlyDictionary<ZDOID, VillageNPC> AllNPCs => _npcs;

        public static void Initialize()
        {
            _npcs.Clear();
            _tickTimer = 0f;
        }

        public static void Register(VillageNPC npc)
        {
            if (npc == null || !npc.GetZDOID().IsNone() == false)
                return;

            var zdoId = npc.GetZDOID();
            if (!zdoId.IsNone())
            {
                _npcs[zdoId] = npc;
            }
        }

        public static void Unregister(VillageNPC npc)
        {
            if (npc == null)
                return;

            var zdoId = npc.GetZDOID();
            if (!zdoId.IsNone())
            {
                _npcs.Remove(zdoId);
            }
        }

        public static VillageNPC GetNPC(ZDOID zdoId)
        {
            _npcs.TryGetValue(zdoId, out var npc);
            return npc;
        }

        /// <summary>
        /// Find all NPCs within a given radius of a position.
        /// </summary>
        public static List<VillageNPC> GetNPCsInRange(Vector3 position, float radius)
        {
            float radiusSq = radius * radius;
            return _npcs.Values
                .Where(npc => npc != null && (npc.transform.position - position).sqrMagnitude <= radiusSq)
                .ToList();
        }

        /// <summary>
        /// Get all NPCs placed by a specific player.
        /// </summary>
        public static List<VillageNPC> GetNPCsByCreator(long playerId)
        {
            return _npcs.Values
                .Where(npc => npc != null && npc.CreatorId == playerId)
                .ToList();
        }

        /// <summary>
        /// Get all NPCs with a specific role.
        /// </summary>
        public static List<VillageNPC> GetNPCsByRole(string roleId)
        {
            return _npcs.Values
                .Where(npc => npc != null && npc.RoleId == roleId)
                .ToList();
        }

        /// <summary>
        /// Check if a player has reached the NPC limit.
        /// </summary>
        public static bool CanPlayerPlaceNPC(long playerId)
        {
            int count = GetNPCsByCreator(playerId).Count;
            return count < Plugin.VillageLifePlugin.MaxNPCsPerPlayer.Value;
        }

        /// <summary>
        /// Find the nearest NPC to a position, optionally filtered by role.
        /// </summary>
        public static VillageNPC GetNearestNPC(Vector3 position, string roleId = null)
        {
            VillageNPC nearest = null;
            float nearestDistSq = float.MaxValue;

            foreach (var npc in _npcs.Values)
            {
                if (npc == null) continue;
                if (roleId != null && npc.RoleId != roleId) continue;

                float distSq = (npc.transform.position - position).sqrMagnitude;
                if (distSq < nearestDistSq)
                {
                    nearestDistSq = distSq;
                    nearest = npc;
                }
            }

            return nearest;
        }

        public static void Tick()
        {
            _tickTimer += Time.deltaTime;
            if (_tickTimer < TickInterval)
                return;
            _tickTimer = 0f;

            // Prune destroyed NPCs
            var toRemove = _npcs.Where(kvp => kvp.Value == null).Select(kvp => kvp.Key).ToList();
            foreach (var key in toRemove)
            {
                _npcs.Remove(key);
            }
        }

        public static void Cleanup()
        {
            _npcs.Clear();
        }

        public static int GetNPCCount() => _npcs.Count;
    }
}
