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
        /// <summary>How far in front of the Village Hall (metres) a new villager appears.</summary>
        public static ConfigEntry<float> SpawnDistance;

        private void Awake()
        {
            SpawnDistance = Config.Bind(
                "General", "SpawnDistance", 2.5f,
                "How far in front of the Village Hall (in metres) a new villager spawns.");

            // Load the vendor catalogue from BepInEx/config/VillageLife/vendors.json (writes
            // defaults on first run; falls back to built-in defaults if the file is bad). Done
            // here in Awake so the catalogue is ready before any villager is summoned.
            VendorConfigLoader.Load(Paths.ConfigPath);

            // Both the villager and the Village Hall are clones of vanilla PREFABS (Haldor and
            // piece_workbench), so both register on the PrefabManager event — the recommended
            // time to access/clone vanilla prefabs. The villager is a plain prefab, not a
            // creature (Haldor has no Character component; see NpcPrefab). Jötunn re-injects
            // registered content on every world load, so we add once and unsubscribe.
            PrefabManager.OnVanillaPrefabsAvailable += OnPrefabsAvailable;

            Jotunn.Logger.LogInfo($"{Constants.PluginName} v{Constants.PluginVersion} loaded.");
        }

        private void OnPrefabsAvailable()
        {
            PrefabManager.OnVanillaPrefabsAvailable -= OnPrefabsAvailable;
            NpcPrefab.Register();
            VillageHall.Register();
        }
    }
}
