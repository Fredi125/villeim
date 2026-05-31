using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;
using VillageLife.Util;

namespace VillageLife.NPC
{
    /// <summary>
    /// Registers the villager creature: a friendly, stationary clone of Haldor.
    ///
    /// We clone through Jötunn's <see cref="CreatureManager"/>, which parents the clone to
    /// a disabled container during setup (so its Awake never fires mid-build) and registers
    /// it into ZNetScene so every client can resolve it by name. This is the supported path
    /// for custom creatures and avoids the prefab-lifecycle crashes that plagued earlier builds.
    /// </summary>
    public static class NpcPrefab
    {
        public static void Register()
        {
            // An empty config keeps us off Jötunn's optional/version-specific config fields;
            // we set everything we need directly on well-known vanilla components below.
            var custom = new CustomCreature(Constants.NpcPrefabName, Constants.NpcBasePrefab, new CreatureConfig());
            GameObject prefab = custom.Prefab;
            if (prefab == null)
            {
                Jotunn.Logger.LogError($"[VillageLife] Failed to clone '{Constants.NpcBasePrefab}' for the villager.");
                return;
            }

            // Drop Haldor's trade behaviour so our own interaction handles the Use key.
            Strip<Trader>(prefab);

            // Defensive: strip any AI so the villager holds its post (Haldor ships with none,
            // but a future base prefab might, and a wandering/fleeing NPC is out of scope here).
            Strip<MonsterAI>(prefab);
            Strip<AnimalAI>(prefab);
            Strip<BaseAI>(prefab);

            // Friendly faction: the player can't hit it and nothing treats it as prey.
            var humanoid = prefab.GetComponent<Humanoid>();
            if (humanoid != null)
                humanoid.m_faction = Character.Faction.Players;

            // Persist with the world like any other saved creature.
            var nview = prefab.GetComponent<ZNetView>();
            if (nview != null)
                nview.m_persistent = true;

            // Attach our controller: hover name + "talk" greeting + ZDO-backed name.
            if (prefab.GetComponent<VillageNpc>() == null)
                prefab.AddComponent<VillageNpc>();

            if (CreatureManager.Instance.AddCreature(custom))
                Jotunn.Logger.LogInfo("[VillageLife] Villager prefab registered.");
            else
                Jotunn.Logger.LogWarning("[VillageLife] Villager prefab was not added (already registered?).");
        }

        private static void Strip<T>(GameObject go) where T : Component
        {
            var component = go.GetComponent<T>();
            if (component != null)
                Object.DestroyImmediate(component);
        }
    }
}
