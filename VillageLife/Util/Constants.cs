namespace VillageLife.Util
{
    /// <summary>
    /// Shared names and keys for the mod. Kept deliberately small — this is the
    /// reliable foundation we build the rest of VillageLife on top of.
    /// </summary>
    public static class Constants
    {
        // Plugin identity
        public const string PluginGuid = "com.villagelife.mod";
        public const string PluginName = "VillageLife";
        public const string PluginVersion = "3.38.0";

        // Prefab names for the content we register (cloned from the vanilla prefabs below).
        // Two villager prefabs: one keeps Haldor's Trader (coin shop), one has it removed and
        // uses our own barter interaction. The vendor type decides which is spawned.
        public const string MerchantPrefabName = "VL_Merchant";
        public const string BartererPrefabName = "VL_Barterer";
        public const string GuardPrefabName = "VL_Guard";
        public const string VillageHallPrefabName = "VL_VillageHall";

        // NPC prefabs we clone for villagers. Hildir is the default look; Haldor is the reliable
        // fallback (a friendly, stationary, no-Character/AI trader). The model can also be set globally
        // (the Experimental config) or per villager type (VendorType.Model); a creature model's wander/
        // combat AI is stripped on clone so it stands still and friendly. Workbench = known-good piece.
        public const string NpcBasePrefab = "Hildir";
        public const string NpcFallbackPrefab = "Haldor";
        public const string HallBasePrefab = "piece_workbench";

        // Single custom Hammer tab that groups every VillageLife buildable. Jötunn auto-creates the
        // tab from this category name, so all our pieces live together instead of scattered in "Misc".
        public const string BuildCategory = "VillageLife";

        // ZDO keys, namespaced so they never collide with the game or other mods.
        // Hashed once at startup (GetStableHashCode is pure and deterministic).
        public static readonly int KeyName = "villagelife_name".GetStableHashCode();
        public static readonly int KeyCreator = "villagelife_creator".GetStableHashCode();
        public static readonly int KeyVendorType = "villagelife_vendortype".GetStableHashCode();
        public static readonly int KeyScale = "villagelife_scale".GetStableHashCode();
        public static readonly int KeyTint = "villagelife_tint".GetStableHashCode();
    }
}
