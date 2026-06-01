using UnityEngine;
using VillageLife.Util;

namespace VillageLife.NPC
{
    /// <summary>
    /// Describes the merchant to create. Today the Village Hall fills this in with a random name
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
    /// The single entry point for summoning merchants. Both the current "instant summon" and any
    /// future creation UI go through <see cref="Spawn"/>, so all placement, ownership and ZDO-setup
    /// logic lives in one tested place.
    /// </summary>
    public static class NpcSpawner
    {
        private static readonly string[] NamePool =
        {
            "Bjorn", "Sigrid", "Olaf", "Astrid", "Leif", "Frida",
            "Gunnar", "Helga", "Ragnar", "Ingrid", "Sven", "Thora"
        };

        // Cycles the vendor types so each summon is a different shop (handy for testing).
        // A creation UI would replace this with an explicit player choice.
        private static int _rotation;

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
        /// Spawn a merchant at a world position. Returns the new <see cref="VillageMerchant"/>,
        /// or null if the prefab isn't available yet. Safe to call on a client — the new ZDO is
        /// created locally and replicated by the game like any other networked object.
        /// </summary>
        public static VillageMerchant Spawn(Vector3 position, Quaternion rotation, NpcRequest request)
        {
            if (ZNetScene.instance == null)
                return null;

            GameObject prefab = ZNetScene.instance.GetPrefab(Constants.NpcPrefabName);
            if (prefab == null)
            {
                Jotunn.Logger.LogError(
                    $"[VillageLife] Merchant prefab '{Constants.NpcPrefabName}' not found in ZNetScene.");
                return null;
            }

            GameObject go = Object.Instantiate(prefab, position, rotation);
            var merchant = go.GetComponent<VillageMerchant>();
            if (merchant != null)
                merchant.Initialize(request.Name, request.VendorTypeId, request.CreatorId);

            return merchant;
        }
    }
}
