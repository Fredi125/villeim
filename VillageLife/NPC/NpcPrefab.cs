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
            // The default model can back a vendor of any kind, so register all three kinds for it.
            RegisterVariant(def, "");

            // Every other (creature) model is registered only for the KINDS a vendor actually uses it
            // for — almost always just the barterer. Without this we'd clone and AI-strip a merchant +
            // barterer + guard apiece for a dozen creatures that only ever appear as one, a startup
            // cost that ballooned with the v3.40.0 variety pass (Goblins, Dvergr, Wraith, Troll, …).
            foreach (var pair in OverrideModelKinds(def))
                RegisterVariantKinds(pair.Key, Suffix(pair.Key), pair.Value);
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

        /// <summary>The base villager prefab name for a vendor's kind (guard / barter / coin merchant).
        /// Mirrors the selection in <see cref="NpcSpawner.Spawn"/> so registration covers exactly the
        /// kinds that can be summoned — no more, no fewer.</summary>
        public static string BaseKindFor(VendorType v)
        {
            if (v != null && string.Equals(v.Kind, "guard", System.StringComparison.OrdinalIgnoreCase))
                return Constants.GuardPrefabName;
            if (v != null && v.IsBarter)
                return Constants.BartererPrefabName;
            return Constants.MerchantPrefabName;
        }

        private static string Suffix(string model) => "__" + model;

        /// <summary>Non-default models any vendor asks for, each mapped to the set of base kinds that
        /// use it — so we register a creature variant only for the kinds actually summoned.</summary>
        private static Dictionary<string, HashSet<string>> OverrideModelKinds(string def)
        {
            var map = new Dictionary<string, HashSet<string>>();
            foreach (VendorType v in VendorCatalog.All)
            {
                if (v == null || string.IsNullOrWhiteSpace(v.Model))
                    continue;
                string model = v.Model.Trim();
                if (model == def)
                    continue;
                if (!map.TryGetValue(model, out HashSet<string> kinds))
                    map[model] = kinds = new HashSet<string>();
                kinds.Add(BaseKindFor(v));
            }
            return map;
        }

        private static void RegisterVariant(string model, string suffix)
        {
            RegisterMerchant(Constants.MerchantPrefabName + suffix, model);
            RegisterBarterer(Constants.BartererPrefabName + suffix, model);
            RegisterGuard(Constants.GuardPrefabName + suffix, model);
        }

        /// <summary>Register only the requested kinds of a model's variant — used for creature models,
        /// which are typically only ever barterers, so we skip cloning the merchant/guard forms.</summary>
        private static void RegisterVariantKinds(string model, string suffix, HashSet<string> kinds)
        {
            if (kinds.Contains(Constants.MerchantPrefabName))
                RegisterMerchant(Constants.MerchantPrefabName + suffix, model);
            if (kinds.Contains(Constants.BartererPrefabName))
                RegisterBarterer(Constants.BartererPrefabName + suffix, model);
            if (kinds.Contains(Constants.GuardPrefabName))
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
                { "MonsterAI", "AnimalAI", "BaseAI", "NpcTalk", "Tameable", "CharacterDrop", "Growup", "Procreation" })
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
