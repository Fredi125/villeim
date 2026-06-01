using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;
using VillageLife.Util;

namespace VillageLife.NPC
{
    /// <summary>
    /// Registers the villager creature: a friendly, persistent clone of Haldor.
    ///
    /// We clone Haldor through <see cref="PrefabManager"/> (the same path the Village Hall
    /// uses to clone the workbench) and then register the result with
    /// <see cref="CreatureManager"/>. We deliberately do NOT go through the
    /// <c>CustomCreature(name, basePrefabName, …)</c> string constructor: that resolves the
    /// base via <c>CreatureManager.GetCreaturePrefab("Haldor")</c>, which returns null because
    /// Haldor is a location-placed trader, not a spawn-system creature — that null was the
    /// "Failed to clone 'Haldor'" error.
    ///
    /// Design rule learned the hard way: keep the clone as close to working vanilla as
    /// possible. Vanilla Haldor stands still, is friendly, and never trips the game's
    /// per-frame character scans, so we leave his components intact and only remove the
    /// Trader (so our own E-interaction is the unambiguous one).
    /// </summary>
    public static class NpcPrefab
    {
        public static void Register()
        {
            // Clone Haldor via PrefabManager → GetPrefab (ZNetScene + cache), where he exists.
            GameObject prefab = PrefabManager.Instance.CreateClonedPrefab(
                Constants.NpcPrefabName, Constants.NpcBasePrefab);
            if (prefab == null)
            {
                Jotunn.Logger.LogError(
                    $"[VillageLife] Could not clone '{Constants.NpcBasePrefab}' for the villager.");
                return;
            }

            // Drop Haldor's trade behaviour so our VillageNpc.Interact handles the Use key.
            var trader = prefab.GetComponent<Trader>();
            if (trader != null)
                Object.DestroyImmediate(trader);

            // Friendly faction: the player can't hit it and nothing treats it as prey.
            var humanoid = prefab.GetComponent<Humanoid>();
            if (humanoid != null)
                humanoid.m_faction = Character.Faction.Players;

            // Persist with the world like any other saved object.
            var nview = prefab.GetComponent<ZNetView>();
            if (nview != null)
                nview.m_persistent = true;

            // Attach our controller: hover name + "talk" greeting + ZDO-backed name.
            if (prefab.GetComponent<VillageNpc>() == null)
                prefab.AddComponent<VillageNpc>();

            // Register the already-cloned GameObject as a creature. fixReference is false
            // because every reference on an in-game clone is already real (no asset-bundle mocks).
            var custom = new CustomCreature(prefab, fixReference: false, new CreatureConfig());

            if (CreatureManager.Instance.AddCreature(custom))
                Jotunn.Logger.LogInfo("[VillageLife] Villager prefab registered.");
            else
                Jotunn.Logger.LogWarning("[VillageLife] Villager prefab was not added (already registered?).");
        }
    }
}
