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
    /// future creation UI go through <see cref="Spawn"/>. The vendor type's <see cref="VendorKind"/>
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
            public bool Success;
        }

        /// <summary>Build a default request: random name + next vendor type in rotation.</summary>
        public static NpcRequest DefaultRequest(Player creator)
        {
            VendorType type = VendorCatalog.ByIndex(_rotation++);
            return new NpcRequest
            {
                Name = NamePool[Random.Range(0, NamePool.Length)],
                VendorTypeId = type.Id,
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
            var result = new Result { Name = request.Name, Title = type.Title, Success = false };

            if (ZNetScene.instance == null)
                return result;

            string prefabName = type.Kind == VendorKind.Barter
                ? Constants.BartererPrefabName
                : Constants.MerchantPrefabName;

            GameObject prefab = ZNetScene.instance.GetPrefab(prefabName);
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

            result.Success = true;
            return result;
        }
    }
}
