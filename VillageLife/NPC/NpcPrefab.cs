using Jotunn.Managers;
using UnityEngine;
using VillageLife.Util;

namespace VillageLife.NPC
{
    /// <summary>
    /// Registers the merchant: a friendly, persistent clone of Haldor, added as a PLAIN
    /// custom prefab (not a Jötunn "creature").
    ///
    /// Why not CreatureManager: the game log proved Haldor has no Character/BaseAI/Rigidbody/
    /// ZSyncAnimation/CharacterAnimEvent — he is a special stationary, non-killable interactable
    /// NPC, not a spawn-system creature. CreatureManager enforces that full monster contract and
    /// rejected the clone ("not valid"). Valheim itself treats Haldor as a location-placed prefab,
    /// so we mirror that: clone via PrefabManager and register with AddPrefab, which injects it
    /// into ZNetScene (so ZNetScene.GetPrefab(name) resolves and it reloads with the world).
    ///
    /// We KEEP Haldor's vanilla Trader component: it already provides the hover text, the Use
    /// interaction, and the real shop window, so the merchant needs no custom UI. A
    /// <see cref="VillageMerchant"/> companion just personalises the name and stock.
    ///
    /// A no-Character merchant also can't enter the global character list, so it cannot trigger
    /// the per-frame EnemyHud / GetCharactersInRange crashes seen with the old Player-based NPCs.
    /// </summary>
    public static class NpcPrefab
    {
        public static void Register()
        {
            // Clone Haldor via PrefabManager (ZNetScene + cache lookup, where Haldor resolves).
            GameObject prefab = PrefabManager.Instance.CreateClonedPrefab(
                Constants.NpcPrefabName, Constants.NpcBasePrefab);
            if (prefab == null)
            {
                Jotunn.Logger.LogError(
                    $"[VillageLife] Could not clone '{Constants.NpcBasePrefab}' for the merchant.");
                return;
            }

            // Keep Haldor's Trader (the vanilla shop UI + Use interaction). Persist with the world.
            var nview = prefab.GetComponent<ZNetView>();
            if (nview != null)
                nview.m_persistent = true;

            // Companion that personalises name + stock. The Trader stays the sole interactable.
            if (prefab.GetComponent<VillageMerchant>() == null)
                prefab.AddComponent<VillageMerchant>();

            // Register as a plain prefab; Jötunn injects it into ZNetScene on every world load.
            // AddPrefab(GameObject) returns void, so we log success after it returns without throwing.
            PrefabManager.Instance.AddPrefab(prefab);
            Jotunn.Logger.LogInfo("[VillageLife] Merchant prefab registered.");
        }
    }
}
