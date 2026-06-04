using System.Collections.Generic;
using Jotunn.Managers;
using UnityEngine;
using VillageLife.Plugin;
using VillageLife.Util;

namespace VillageLife.NPC
{
    /// <summary>
    /// Registers the villager prefabs — friendly, persistent clones of an NPC model (Hildir by
    /// default, Haldor as the reliable fallback). Three kinds per model:
    ///   • <see cref="Constants.MerchantPrefabName"/> KEEPS the model's Trader (vanilla shop window);
    ///   • <see cref="Constants.BartererPrefabName"/> has the Trader removed and a <see cref="VillageBarterer"/> added;
    ///   • <see cref="Constants.GuardPrefabName"/> likewise, with a <see cref="VillageGuard"/>.
    ///
    /// The model can be set globally (the Experimental <c>VillagerBasePrefab</c> config) or per villager
    /// type (<see cref="VendorType.Model"/>). The default model keeps the plain prefab names, so
    /// villagers placed by older versions still resolve; every other model gets suffixed variants
    /// (e.g. <c>VL_Barterer__Haldor</c>), and <see cref="NpcSpawner"/> picks the right one.
    ///
    /// A creature model (like Hildir) is a real <c>Character</c>, so its wander/combat/loot AI is
    /// stripped on clone (<see cref="Neutralize"/>) to leave a stationary, friendly NPC; that strip is
    /// a no-op for Haldor, who has none of it. If a model can't be cloned the clone falls back to
    /// Haldor, so a bad name only changes the look, never breaks the villager.
    /// </summary>
    public static class NpcPrefab
    {
        public static void Register()
        {
            string def = DefaultModel();
            RegisterVariant(def, "");
            foreach (string model in OverrideModels(def))
                RegisterVariant(model, Suffix(model));
        }

        /// <summary>The default villager model: the Experimental config override, or Hildir.</summary>
        public static string DefaultModel()
        {
            var cfg = VillageLifePlugin.VillagerBasePrefab;
            string name = cfg != null ? cfg.Value : null;
            return string.IsNullOrWhiteSpace(name) ? Constants.NpcBasePrefab : name.Trim();
        }

        /// <summary>The registered prefab name for a kind + model: the plain name for the default
        /// model, or a suffixed variant otherwise. Used by <see cref="NpcSpawner"/>.</summary>
        public static string VariantPrefab(string baseKind, string model)
        {
            if (string.IsNullOrWhiteSpace(model) || model.Trim() == DefaultModel())
                return baseKind;
            return baseKind + Suffix(model.Trim());
        }

        private static string Suffix(string model) => "__" + model;

        /// <summary>Distinct non-default models any vendor type asks for (so we register their variants).</summary>
        private static IEnumerable<string> OverrideModels(string def)
        {
            var set = new HashSet<string>();
            foreach (VendorType v in VendorCatalog.All)
                if (v != null && !string.IsNullOrWhiteSpace(v.Model) && v.Model.Trim() != def)
                    set.Add(v.Model.Trim());
            return set;
        }

        private static void RegisterVariant(string model, string suffix)
        {
            RegisterMerchant(Constants.MerchantPrefabName + suffix, model);
            RegisterBarterer(Constants.BartererPrefabName + suffix, model);
            RegisterGuard(Constants.GuardPrefabName + suffix, model);
        }

        private static void RegisterMerchant(string prefabName, string model)
        {
            GameObject prefab = Clone(prefabName, model);
            if (prefab == null)
                return;

            // Keep the model's Trader (vanilla shop UI + Use interaction). Companion sets name + stock.
            if (prefab.GetComponent<VillageMerchant>() == null)
                prefab.AddComponent<VillageMerchant>();

            PrefabManager.Instance.AddPrefab(prefab);
            Jotunn.Logger.LogInfo($"[VillageLife] Merchant prefab '{prefabName}' ({model}) registered.");
        }

        private static void RegisterBarterer(string prefabName, string model)
        {
            GameObject prefab = Clone(prefabName, model);
            if (prefab == null)
                return;

            // Remove the model's Trader so our VillageBarterer is the only interactable.
            var trader = prefab.GetComponent<Trader>();
            if (trader != null)
                Object.DestroyImmediate(trader);

            if (prefab.GetComponent<VillageBarterer>() == null)
                prefab.AddComponent<VillageBarterer>();

            PrefabManager.Instance.AddPrefab(prefab);
            Jotunn.Logger.LogInfo($"[VillageLife] Barterer prefab '{prefabName}' ({model}) registered.");
        }

        private static void RegisterGuard(string prefabName, string model)
        {
            GameObject prefab = Clone(prefabName, model);
            if (prefab == null)
                return;

            // A guard has no shop, so (like the barterer) the Trader is removed and our VillageGuard
            // becomes the only interactable.
            var trader = prefab.GetComponent<Trader>();
            if (trader != null)
                Object.DestroyImmediate(trader);

            if (prefab.GetComponent<VillageGuard>() == null)
                prefab.AddComponent<VillageGuard>();

            PrefabManager.Instance.AddPrefab(prefab);
            Jotunn.Logger.LogInfo($"[VillageLife] Guard prefab '{prefabName}' ({model}) registered.");
        }

        /// <summary>Clone an NPC model into a persistent villager prefab, stripping wander/combat AI
        /// (a no-op for Haldor) and falling back to Haldor if the model can't be cloned.</summary>
        private static GameObject Clone(string name, string model)
        {
            GameObject prefab = PrefabManager.Instance.CreateClonedPrefab(name, model);
            if (prefab == null && model != Constants.NpcFallbackPrefab)
            {
                Jotunn.Logger.LogWarning(
                    $"[VillageLife] Villager model '{model}' didn't resolve; using {Constants.NpcFallbackPrefab}.");
                prefab = PrefabManager.Instance.CreateClonedPrefab(name, Constants.NpcFallbackPrefab);
            }
            if (prefab == null)
            {
                Jotunn.Logger.LogError($"[VillageLife] Could not clone a villager model for '{name}'.");
                return null;
            }

            Neutralize(prefab);

            var nview = prefab.GetComponent<ZNetView>();
            if (nview != null)
                nview.m_persistent = true;

            return prefab;
        }

        /// <summary>Strip wander/combat/loot/breeding behaviour from a creature-based model so it
        /// stands still and friendly. By type name, so it no-ops for components a model lacks
        /// (Haldor has none of these).</summary>
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

        /// <summary>
        /// Log NPC prefabs that could serve as villager models — Trader-NPCs first (the safest,
        /// e.g. Haldor and Hildir), then other humanoids to experiment with via the config or a
        /// vendor's Model. Read-only; call once a world is loaded (ZNetScene populated).
        /// </summary>
        public static void LogModelCandidates()
        {
            try
            {
                ZNetScene zs = ZNetScene.instance;
                if (zs == null || zs.m_prefabs == null)
                    return;

                var traders = new List<string>();
                var humanoids = new List<string>();
                foreach (GameObject p in zs.m_prefabs)
                {
                    if (p == null || p.GetComponent("Humanoid") == null)
                        continue;
                    if (p.GetComponent("Trader") != null)
                        traders.Add(p.name);
                    else
                        humanoids.Add(p.name);
                }
                traders.Sort();
                humanoids.Sort();

                Jotunn.Logger.LogInfo(
                    $"[VillageLife] Villager-model candidates — Trader NPCs (safest): {string.Join(", ", traders)}");
                Jotunn.Logger.LogInfo(
                    $"[VillageLife] Villager-model candidates — other humanoids ({humanoids.Count}, experimental):");
                const int chunk = 20;
                for (int i = 0; i < humanoids.Count; i += chunk)
                    Jotunn.Logger.LogInfo("  " + string.Join(", ", humanoids.GetRange(i, System.Math.Min(chunk, humanoids.Count - i))));
            }
            catch (System.Exception e)
            {
                Jotunn.Logger.LogWarning($"[VillageLife] Model-candidate scan failed: {e.Message}");
            }
        }
    }
}
