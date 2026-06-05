using System.Collections.Generic;
using UnityEngine;
using VillageLife.Util;

namespace VillageLife.NPC
{
    /// <summary>
    /// Describes the villager to create. Today the Village Hall fills this in with a random name
    /// and the next vendor type in rotation; a creation UI can populate the same struct later
    /// without touching spawn logic.
    /// </summary>
    public struct NpcRequest
    {
        public string Name;
        public string VendorTypeId;
        public long CreatorId;
    }

    /// <summary>
    /// The single entry point for summoning villagers. Both the current "instant summon" and any
    /// future creation UI go through <see cref="Spawn"/>. The vendor type's kind (coin vs. barter)
    /// decides which prefab is used (coin merchant vs. barterer); the caller gets back the spawned
    /// name and title without needing to know which kind it was.
    /// </summary>
    public static class NpcSpawner
    {
        private static readonly string[] NamePool =
        {
            "Bjorn", "Sigrid", "Olaf", "Astrid", "Leif", "Frida",
            "Gunnar", "Helga", "Ragnar", "Ingrid", "Sven", "Thora"
        };

        // Cycles the vendor types so each summon is a different villager (handy for testing).
        // A creation UI would replace this with an explicit player choice.
        private static int _rotation;

        /// <summary>The result of a summon: what to tell the player (empty Title = failed).</summary>
        public struct Result
        {
            public string Name;
            public string Title;
            public string Greeting;
            public bool Success;
        }

        /// <summary>Build a default request: random name + next vendor type in rotation.</summary>
        public static NpcRequest DefaultRequest(Player creator)
        {
            VendorType type = VendorCatalog.ByIndex(_rotation++);
            return RequestFor(creator, type.Id);
        }

        /// <summary>A random Norse name from the pool (also used by the creation UI).</summary>
        public static string RandomName() => NamePool[Random.Range(0, NamePool.Length)];

        /// <summary>Build a request for a specific vendor id (used by the biome stations).</summary>
        public static NpcRequest RequestFor(Player creator, string vendorTypeId)
        {
            return new NpcRequest
            {
                Name = RandomName(),
                VendorTypeId = vendorTypeId,
                CreatorId = creator != null ? creator.GetPlayerID() : 0L
            };
        }

        /// <summary>
        /// Spawn a villager at a world position. Safe to call on a client — the new ZDO is created
        /// locally and replicated by the game like any other networked object.
        /// </summary>
        public static Result Spawn(Vector3 position, Quaternion rotation, NpcRequest request)
        {
            VendorType type = VendorCatalog.ById(request.VendorTypeId);
            var result = new Result
            {
                Name = request.Name,
                Title = type.Title,
                Greeting = Greetings.Line(type),
                Success = false
            };

            if (ZNetScene.instance == null)
                return result;

            string baseKind;
            if (string.Equals(type.Kind, "guard", System.StringComparison.OrdinalIgnoreCase))
                baseKind = Constants.GuardPrefabName;
            else if (type.IsBarter)
                baseKind = Constants.BartererPrefabName;
            else
                baseKind = Constants.MerchantPrefabName;

            // Per-type model: a vendor can request a specific look via its Model; otherwise the default.
            string prefabName = NpcPrefab.VariantPrefab(baseKind, type.Model);
            GameObject prefab = ZNetScene.instance.GetPrefab(prefabName);
            if (prefab == null && prefabName != baseKind)
                prefab = ZNetScene.instance.GetPrefab(baseKind);   // variant missing → fall back to default look
            if (prefab == null)
            {
                Jotunn.Logger.LogError($"[VillageLife] Prefab '{prefabName}' not found in ZNetScene.");
                return result;
            }

            GameObject go = Object.Instantiate(prefab, position, rotation);

            // Initialise the matching companion (only one of these exists on a given prefab).
            var merchant = go.GetComponent<VillageMerchant>();
            if (merchant != null)
                merchant.Initialize(request.Name, request.VendorTypeId, request.CreatorId);

            var barterer = go.GetComponent<VillageBarterer>();
            if (barterer != null)
                barterer.Initialize(request.Name, request.VendorTypeId, request.CreatorId);

            var guard = go.GetComponent<VillageGuard>();
            if (guard != null)
                guard.Initialize(request.Name, request.VendorTypeId, request.CreatorId);

            // Give each villager a slightly different build, persisted in its ZDO. A vendor type may
            // pin a signature hue via Tint (e.g. the NPC Greydwarf); otherwise the tint is random.
            var nview = go.GetComponent<ZNetView>();
            VillagerAppearance.Assign(go, nview != null ? nview.GetZDO() : null, type.Tint);

            result.Success = true;
            return result;
        }

        /// <summary>
        /// Destroy every VillageLife villager (coin merchant or barterer) within <paramref name="radius"/>
        /// of <paramref name="center"/> and return how many were removed. The station uses this to
        /// dismiss its merchant on a second Use — and, because it removes <i>all</i> nearby villagers,
        /// it also mops up the duplicate piles that earlier builds created by summoning without limit.
        /// Networked-safe: ownership is claimed before the ZDO is destroyed.
        /// </summary>
        public static int RemoveNear(Vector3 center, float radius)
        {
            if (ZNetScene.instance == null)
                return 0;

            float r2 = radius * radius;
            return DestroyNear(Object.FindObjectsByType<VillageMerchant>(FindObjectsSortMode.None), center, r2)
                 + DestroyNear(Object.FindObjectsByType<VillageBarterer>(FindObjectsSortMode.None), center, r2)
                 + DestroyNear(Object.FindObjectsByType<VillageGuard>(FindObjectsSortMode.None), center, r2);
        }

        /// <summary>
        /// The set of vendor-type ids whose villager currently stands within <paramref name="radius"/>
        /// of <paramref name="center"/>. The spawn menus use this to hide a villager that's already
        /// posted here until it's dismissed — swept with the same radius the menu's Dismiss button uses,
        /// so anything hidden right now is always recallable right now.
        /// </summary>
        public static HashSet<string> VendorIdsNear(Vector3 center, float radius)
        {
            var ids = new HashSet<string>();
            if (ZNetScene.instance == null)
                return ids;

            float r2 = radius * radius;
            CollectNear(Object.FindObjectsByType<VillageMerchant>(FindObjectsSortMode.None), center, r2, m => m.VendorTypeId, ids);
            CollectNear(Object.FindObjectsByType<VillageBarterer>(FindObjectsSortMode.None), center, r2, b => b.VendorTypeId, ids);
            CollectNear(Object.FindObjectsByType<VillageGuard>(FindObjectsSortMode.None), center, r2, g => g.VendorTypeId, ids);
            return ids;
        }

        private static void CollectNear<T>(T[] components, Vector3 center, float radiusSqr,
            System.Func<T, string> idOf, HashSet<string> into) where T : Component
        {
            foreach (T comp in components)
            {
                if (comp == null || (comp.transform.position - center).sqrMagnitude > radiusSqr)
                    continue;
                string id = idOf(comp);
                if (!string.IsNullOrEmpty(id))
                    into.Add(id);
            }
        }

        private static int DestroyNear<T>(T[] components, Vector3 center, float radiusSqr) where T : Component
        {
            int removed = 0;
            foreach (T comp in components)
            {
                if (comp == null || (comp.transform.position - center).sqrMagnitude > radiusSqr)
                    continue;

                ZNetView nview = comp.GetComponent<ZNetView>();
                if (nview != null && nview.IsValid())
                {
                    nview.ClaimOwnership();
                    ZNetScene.instance.Destroy(comp.gameObject);
                }
                else
                {
                    Object.Destroy(comp.gameObject);
                }
                removed++;
            }
            return removed;
        }
    }
}
