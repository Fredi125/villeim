using BepInEx;
using BepInEx.Configuration;
using Jotunn.Managers;
using Jotunn.Utils;
using VillageLife.Building;
using VillageLife.NPC;
using VillageLife.Util;

namespace VillageLife.Plugin
{
    /// <summary>
    /// BepInEx entry point. Binds a single config value and registers our content with Jötunn.
    /// No Harmony patches, no per-frame update loop, no background managers — the foundation
    /// does exactly one thing and does it reliably.
    /// </summary>
    [BepInPlugin(Constants.PluginGuid, Constants.PluginName, Constants.PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    public class VillageLifePlugin : BaseUnityPlugin
    {
        /// <summary>How far in front of the player (metres) a summoned villager appears (0 = at feet).</summary>
        public static ConfigEntry<float> SpawnDistance;

        /// <summary>EXPERIMENTAL: an alternate NPC prefab to clone villagers from (empty = Haldor).</summary>
        public static ConfigEntry<string> VillagerBasePrefab;

        private void Awake()
        {
            VillagerBasePrefab = Config.Bind(
                "Experimental", "VillagerBasePrefab", "",
                "EXPERIMENTAL — clone villagers from this NPC prefab instead of Haldor, for a " +
                "different look (try \"Hildir\"). Empty = Haldor (the stable default). A non-Haldor " +
                "model keeps a Character/AI that this mod normally avoids, so it may be attackable, " +
                "wander, or be unstable; the mod strips obvious AI defensively and falls back to " +
                "Haldor if the prefab can't be cloned. Restart to apply.");

            SpawnDistance = Config.Bind(
                "General", "SpawnDistance", 0f,
                "How far in front of the player (in metres) a summoned villager appears. 0 = right at " +
                "your feet, so you can place a few around a spawner by summoning from different spots.");

            // Load the vendor catalogue from BepInEx/config/VillageLife/vendors.json (writes
            // defaults on first run; falls back to built-in defaults if the file is bad). Done
            // here in Awake so the catalogue is ready before any villager is summoned.
            VendorConfigLoader.Load(BepInEx.Paths.ConfigPath);

            // Both the villager and the Village Hall are clones of vanilla PREFABS (Haldor and
            // piece_workbench), so both register on the PrefabManager event — the recommended
            // time to access/clone vanilla prefabs. The villager is a plain prefab, not a
            // creature (Haldor has no Character component; see NpcPrefab). Jötunn re-injects
            // registered content on every world load, so we add once and unsubscribe.
            PrefabManager.OnVanillaPrefabsAvailable += OnPrefabsAvailable;

            Jotunn.Logger.LogInfo($"{Constants.PluginName} v{Constants.PluginVersion} loaded.");

            // Once a world is loaded, log the building-like prefabs so we can pick decorative
            // structures per biome from a real list (ZNetScene isn't populated until in-world).
            StartCoroutine(DiscoverBuildingsWhenReady());
        }

        private void OnPrefabsAvailable()
        {
            PrefabManager.OnVanillaPrefabsAvailable -= OnPrefabsAvailable;
            NpcPrefab.Register();
            VillageStations.Register();
            WorldStructures.Register();

            // Now that vanilla prefabs (and ObjectDB items) are available, verify every item name
            // the mod references resolves, logging any that don't. This is the same moment Jötunn
            // resolves the station requirements above, so the audit sees exactly what the game will.
            ItemNameAudit.Run();
        }

        /// <summary>Wait until a world is loaded (ZNetScene populated), then log building prefabs once.</summary>
        private System.Collections.IEnumerator DiscoverBuildingsWhenReady()
        {
            while (ZNetScene.instance == null ||
                   ZNetScene.instance.m_prefabs == null ||
                   ZNetScene.instance.m_prefabs.Count == 0)
            {
                yield return new UnityEngine.WaitForSeconds(1f);
            }
            WorldStructures.LogBuildingCandidates();
        }
    }
}
