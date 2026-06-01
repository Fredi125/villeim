using Jotunn.Managers;
using UnityEngine;
using VillageLife.Util;

namespace VillageLife.NPC
{
    /// <summary>
    /// Registers the two villager prefabs, both friendly, persistent clones of Haldor added as
    /// PLAIN custom prefabs (not Jötunn "creatures" — the game log proved Haldor has no Character/
    /// AI/Rigidbody, so CreatureManager rejects him; PrefabManager.AddPrefab injects the clone into
    /// ZNetScene on every world load instead).
    ///
    ///   • <see cref="Constants.MerchantPrefabName"/> — KEEPS Haldor's Trader, so the vanilla shop
    ///     window, hover and Use interaction all come for free (coin shops).
    ///   • <see cref="Constants.BartererPrefabName"/> — Trader REMOVED and a <see cref="VillageBarterer"/>
    ///     added, which becomes the sole Hoverable/Interactable (fixed resource-for-product swap,
    ///     no coins, no shop window). Keeping these as separate prefabs avoids two interactables on
    ///     one object — the kind of ambiguity that bit earlier builds.
    ///
    /// A no-Character villager also can't enter the global character list, so it cannot trigger the
    /// per-frame EnemyHud / GetCharactersInRange crashes seen with the old Player-based NPCs.
    /// </summary>
    public static class NpcPrefab
    {
        public static void Register()
        {
            RegisterMerchant();
            RegisterBarterer();
        }

        private static void RegisterMerchant()
        {
            GameObject prefab = Clone(Constants.MerchantPrefabName);
            if (prefab == null)
                return;

            // Keep Haldor's Trader (vanilla shop UI + Use interaction). Companion sets name + stock.
            if (prefab.GetComponent<VillageMerchant>() == null)
                prefab.AddComponent<VillageMerchant>();

            PrefabManager.Instance.AddPrefab(prefab);
            Jotunn.Logger.LogInfo("[VillageLife] Merchant prefab registered.");
        }

        private static void RegisterBarterer()
        {
            GameObject prefab = Clone(Constants.BartererPrefabName);
            if (prefab == null)
                return;

            // Remove Haldor's Trader so our VillageBarterer is the only interactable.
            var trader = prefab.GetComponent<Trader>();
            if (trader != null)
                Object.DestroyImmediate(trader);

            if (prefab.GetComponent<VillageBarterer>() == null)
                prefab.AddComponent<VillageBarterer>();

            PrefabManager.Instance.AddPrefab(prefab);
            Jotunn.Logger.LogInfo("[VillageLife] Barterer prefab registered.");
        }

        /// <summary>Clone Haldor (resolvable via PrefabManager) and make it persist with the world.</summary>
        private static GameObject Clone(string name)
        {
            GameObject prefab = PrefabManager.Instance.CreateClonedPrefab(name, Constants.NpcBasePrefab);
            if (prefab == null)
            {
                Jotunn.Logger.LogError(
                    $"[VillageLife] Could not clone '{Constants.NpcBasePrefab}' for '{name}'.");
                return null;
            }

            var nview = prefab.GetComponent<ZNetView>();
            if (nview != null)
                nview.m_persistent = true;

            return prefab;
        }
    }
}
