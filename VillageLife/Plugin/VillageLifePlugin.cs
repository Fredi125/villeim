using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Jotunn;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;
using UnityEngine;

namespace VillageLife.Plugin
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    public class VillageLifePlugin : BaseUnityPlugin
    {
        public const string PluginGUID = "com.villagelife.mod";
        public const string PluginName = "VillageLife";
        public const string PluginVersion = "1.0.0";

        public static VillageLifePlugin Instance { get; private set; }

        private Harmony _harmony;

        // Configuration
        public static ConfigEntry<int> MaxNPCsPerPlayer;
        public static ConfigEntry<float> NPCWanderRadius;
        public static ConfigEntry<int> MerchantRestockMinutes;
        public static ConfigEntry<bool> EnableQuestSystem;
        public static ConfigEntry<bool> EnableAmbientDialog;
        public static ConfigEntry<string> Language;

        private void Awake()
        {
            Instance = this;

            InitConfig();

            _harmony = new Harmony(PluginGUID);
            _harmony.PatchAll(typeof(VillageLifePlugin).Assembly);

            // Initialize core systems
            VillageLife.Config.ConfigManager.Initialize(BepInEx.Paths.ConfigPath);
            VillageLife.Localization.LocalizationManager.Initialize();
            NPC.NPCManager.Initialize();
            Quest.QuestEngine.Initialize();
            Dialog.DialogSystem.Initialize();
            Multiplayer.RPCManager.Initialize();

            // Initialize UI
            UI.UIManager.Initialize(gameObject);

            // Register Jötunn events
            PrefabManager.OnVanillaPrefabsAvailable += OnVanillaPrefabsAvailable;

            Logger.LogInfo($"{PluginName} v{PluginVersion} loaded.");
        }

        private void InitConfig()
        {
            MaxNPCsPerPlayer = base.Config.Bind("General", "MaxNPCsPerPlayer", 20,
                "Maximum number of NPCs a single player can place.");

            NPCWanderRadius = base.Config.Bind("Behavior", "NPCWanderRadius", 12f,
                "Default wander radius for NPCs around their home point.");

            MerchantRestockMinutes = base.Config.Bind("Merchant", "RestockMinutes", 60,
                "Minutes between merchant inventory restocks.");

            EnableQuestSystem = base.Config.Bind("Quests", "EnableQuestSystem", true,
                "Enable the quest system. Disable to use NPCs as merchants/villagers only.");

            EnableAmbientDialog = base.Config.Bind("Dialog", "EnableAmbientDialog", true,
                "Enable NPCs speaking ambient dialog lines above their heads.");

            Language = base.Config.Bind("Localization", "Language", "en",
                "Language code for UI text and dialog (en, fr).");
        }

        private void OnVanillaPrefabsAvailable()
        {
            // Unsubscribe immediately — this event can fire multiple times (e.g. returning
            // to main menu) and re-registering prefabs causes "already exists" errors.
            PrefabManager.OnVanillaPrefabsAvailable -= OnVanillaPrefabsAvailable;

            NPC.NPCPrefabFactory.RegisterPrefabs();
            NPC.VillageHallStation.RegisterPrefab();

            // Register pieces immediately after prefabs, while piece tables are still being built.
            // OnPiecesRegistered fires AFTER tables are finalized, which is too late.
            NPC.NPCPrefabFactory.RegisterPieces();
            NPC.VillageHallStation.RegisterPiece();
        }

        private void Update()
        {
            NPC.NPCManager.Tick();
            Quest.QuestEngine.Tick();
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
            NPC.NPCManager.Cleanup();
        }
    }
}
