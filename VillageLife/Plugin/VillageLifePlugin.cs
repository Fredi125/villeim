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

        /// <summary>Damage a Village Guard deals per tick to each nearby hostile (0 = guards do no damage).</summary>
        public static ConfigEntry<float> GuardDamage;

        /// <summary>Whether barterers/bounty-givers/quest-givers/guards show ambient chat bubbles.</summary>
        public static ConfigEntry<bool> VillagerChatter;

        /// <summary>EXPERIMENTAL: an alternate NPC prefab to clone villagers from (empty = Haldor).</summary>
        public static ConfigEntry<string> VillagerBasePrefab;

        private void Awake()
        {
            VillagerBasePrefab = Config.Bind(
                "Experimental", "VillagerBasePrefab", "",
                "Override the global villager model with this NPC prefab. Empty = the built-in default " +
                "(Hildir). The BepInEx log lists candidate prefab names on world load (search " +
                "\"Villager-model candidates\"). A creature model's wander/combat AI is stripped on " +
                "clone, and it falls back to Haldor if the prefab can't be cloned. Per-type looks can " +
                "also be set in vendors.json (a vendor's \"Model\"). Restart to apply.");

            SpawnDistance = Config.Bind(
                "General", "SpawnDistance", 0f,
                "How far in front of the player (in metres) a summoned villager appears. 0 = right at " +
                "your feet, so you can place a few around a spawner by summoning from different spots.");

            GuardDamage = Config.Bind(
                "General", "GuardDamage", 12f,
                "Blunt damage a Village Guard deals each tick (about every 3s) to each nearby hostile " +
                "creature, up to a few at once. Set to 0 to make guards purely decorative (no damage).");

            VillagerChatter = Config.Bind(
                "General", "VillagerChatter", true,
                "Show ambient chat bubbles over barterers, bounty-givers, quest-givers and guards (a " +
                "greeting when you approach, then the occasional idle line). Coin merchants use " +
                "Valheim's own trader chatter and are unaffected by this toggle.");

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
            NpcPrefab.LogModelCandidates();
        }
    }
}
