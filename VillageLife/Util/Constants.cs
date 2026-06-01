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
        public const string PluginVersion = "3.13.0";

        // Prefab names for the content we register (cloned from the vanilla prefabs below).
        // Two villager prefabs: one keeps Haldor's Trader (coin shop), one has it removed and
        // uses our own barter interaction. The vendor type decides which is spawned.
        public const string MerchantPrefabName = "VL_Merchant";
        public const string BartererPrefabName = "VL_Barterer";
        public const string GuardPrefabName = "VL_Guard";
        public const string VillageHallPrefabName = "VL_VillageHall";

        // Vanilla prefabs we clone. Haldor is a friendly, stationary, non-combat trader with
        // no Character/AI component — so we keep his Trader (real shop UI) for our merchant.
        // The workbench is a known-good buildable piece.
        public const string NpcBasePrefab = "Haldor";
        public const string HallBasePrefab = "piece_workbench";

        // ZDO keys, namespaced so they never collide with the game or other mods.
        // Hashed once at startup (GetStableHashCode is pure and deterministic).
        public static readonly int KeyName = "villagelife_name".GetStableHashCode();
        public static readonly int KeyCreator = "villagelife_creator".GetStableHashCode();
        public static readonly int KeyVendorType = "villagelife_vendortype".GetStableHashCode();
        public static readonly int KeyScale = "villagelife_scale".GetStableHashCode();
    }
}
