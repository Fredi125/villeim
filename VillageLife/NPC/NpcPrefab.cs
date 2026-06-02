using Jotunn.Managers;
using UnityEngine;
using VillageLife.Plugin;
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
            RegisterGuard();
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

        private static void RegisterGuard()
        {
            GameObject prefab = Clone(Constants.GuardPrefabName);
            if (prefab == null)
                return;

            // A guard has no shop, so (like the barterer) Haldor's Trader is removed and our
            // VillageGuard becomes the only interactable.
            var trader = prefab.GetComponent<Trader>();
            if (trader != null)
                Object.DestroyImmediate(trader);

            if (prefab.GetComponent<VillageGuard>() == null)
                prefab.AddComponent<VillageGuard>();

            PrefabManager.Instance.AddPrefab(prefab);
            Jotunn.Logger.LogInfo("[VillageLife] Guard prefab registered.");
        }

        /// <summary>Clone the villager base (Haldor by default) and make it persist with the world.
        /// An experimental config can point this at another NPC prefab for a different look; that path
        /// strips obvious AI and falls back to Haldor if the prefab can't be cloned.</summary>
        private static GameObject Clone(string name)
        {
            string baseName = ExperimentalBase();

            GameObject prefab = PrefabManager.Instance.CreateClonedPrefab(name, baseName);
            if (prefab == null && baseName != Constants.NpcBasePrefab)
            {
                Jotunn.Logger.LogWarning(
                    $"[VillageLife] Experimental villager base '{baseName}' didn't resolve; using Haldor.");
                baseName = Constants.NpcBasePrefab;
                prefab = PrefabManager.Instance.CreateClonedPrefab(name, baseName);
            }
            if (prefab == null)
            {
                Jotunn.Logger.LogError($"[VillageLife] Could not clone '{baseName}' for '{name}'.");
                return null;
            }

            // A non-Haldor base is a real creature: strip its movement/combat/loot behaviour so it
            // stands still and friendly, keeping the model, animator, Trader and ZNetView. Done by
            // type name so it compiles on any build and silently no-ops for components it lacks.
            if (baseName != Constants.NpcBasePrefab)
                Neutralize(prefab);

            var nview = prefab.GetComponent<ZNetView>();
            if (nview != null)
                nview.m_persistent = true;

            return prefab;
        }

        /// <summary>The configured experimental base prefab, or Haldor when unset.</summary>
        private static string ExperimentalBase()
        {
            var cfg = VillageLifePlugin.VillagerBasePrefab;
            string name = cfg != null ? cfg.Value : null;
            return string.IsNullOrWhiteSpace(name) ? Constants.NpcBasePrefab : name.Trim();
        }

        /// <summary>Strip the crash- and wander-prone behaviour from a non-Haldor NPC base.</summary>
        private static void Neutralize(GameObject prefab)
        {
            foreach (string comp in new[]
                { "MonsterAI", "AnimalAI", "BaseAI", "Tameable", "CharacterDrop", "Growup", "Procreation" })
            {
                Component c = prefab.GetComponent(comp);
                if (c != null)
                    Object.DestroyImmediate(c);
            }
        }
    }
}
