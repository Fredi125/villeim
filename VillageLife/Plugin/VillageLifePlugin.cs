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
    /// BepInEx entry point. Binds a single config value and registers our content with
    /// Jötunn at the right moment. No Harmony patches, no per-frame update loop, no
    /// background managers — the foundation does exactly one thing and does it reliably.
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

            // Register each piece of content once the vanilla prefab it clones is available.
            // The creature clones Haldor (a creature) → CreatureManager event.
            // The piece clones the workbench (a prefab) → PrefabManager event.
            // Jötunn re-injects registered content on every world load, so we add it once
            // and unsubscribe to avoid duplicate-registration errors.
            CreatureManager.OnVanillaCreaturesAvailable += OnCreaturesAvailable;
            PrefabManager.OnVanillaPrefabsAvailable += OnPrefabsAvailable;

            Jotunn.Logger.LogInfo($"{Constants.PluginName} v{Constants.PluginVersion} loaded.");
        }

        private void OnCreaturesAvailable()
        {
            CreatureManager.OnVanillaCreaturesAvailable -= OnCreaturesAvailable;
            NpcPrefab.Register();
        }

        private void OnPrefabsAvailable()
        {
            PrefabManager.OnVanillaPrefabsAvailable -= OnPrefabsAvailable;
            VillageHall.Register();
        }
    }
}
