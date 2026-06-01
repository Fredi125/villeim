using System;

namespace VillageLife.NPC
{
    /// <summary>One thing a coin-shop vendor offers: a prefab, its price, and the stack size sold.
    /// A "rare" item is simply a good with a high price — no separate type needed.</summary>
    [Serializable]
    public struct VendorGood
    {
        public string Prefab;
        public int Price;
        public int Stack;

        public VendorGood(string prefab, int price, int stack)
        {
            Prefab = prefab;
            Price = price;
            Stack = stack;
        }
    }

    /// <summary>
    /// A kind of villager: a display title plus what it offers. Serializable so the whole catalogue
    /// can live in <c>vendors.json</c>. <see cref="Kind"/> is a string ("coin" or "barter") rather
    /// than an enum so the config file stays human-readable.
    /// </summary>
    [Serializable]
    public class VendorType
    {
        public string Id;
        public string Title;
        public string Kind = "coin";   // "coin" = vanilla shop window; "barter" = fixed swap.
        public string Biome = "";      // Optional tag for biome-themed vendors (forward-looking).

        // Coin shop: the goods sold for coins (include a high-priced "rare" entry if desired).
        public VendorGood[] Goods;

        // Barter: "give CostAmount × CostPrefab, receive GiveAmount × GivePrefab".
        public string CostPrefab;
        public int CostAmount;
        public string GivePrefab;
        public int GiveAmount;

        /// <summary>True when this vendor trades by barter rather than the coin shop.</summary>
        public bool IsBarter => string.Equals(Kind, "barter", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// The in-memory catalogue of villager types. Populated from <c>vendors.json</c> at startup
    /// (see <see cref="VendorConfigLoader"/>); if that file is missing or invalid we fall back to
    /// <see cref="DefaultVendors"/>, so a bad edit can never leave the mod with no vendors.
    ///
    /// To add a vendor: edit vendors.json (no rebuild), or add to <see cref="DefaultVendors"/> here.
    /// Prefab names are resolved at runtime and any that don't exist are skipped, so a typo offers
    /// fewer goods (or a barter that politely refuses) rather than crashing.
    /// </summary>
    public static class VendorCatalog
    {
        /// <summary>
        /// Bumped whenever the built-in defaults change (prices, goods, new vendors). The loader
        /// regenerates an out-of-date vendors.json from these defaults (keeping a .bak), so value
        /// tweaks here reach an existing install without a manual file delete.
        /// </summary>
        public const int ConfigVersion = 4;

        /// <summary>Built-in safety net, also used to seed vendors.json on first run.</summary>
        public static VendorType[] DefaultVendors => new[]
        {
            new VendorType
            {
                Id = "general", Title = "General Store", Kind = "coin",
                Goods = new[]
                {
                    new VendorGood("Wood",   1, 50),
                    new VendorGood("Stone",  1, 50),
                    new VendorGood("Coal",   2, 20),
                    new VendorGood("Flint",  3, 20),
                    new VendorGood("Resin",  2, 20),
                }
            },
            new VendorType
            {
                Id = "forager", Title = "Forager", Kind = "coin",
                Goods = new[]
                {
                    new VendorGood("Raspberry",   2, 20),
                    new VendorGood("Mushroom",    2, 20),
                    new VendorGood("Blueberries", 3, 20),
                    new VendorGood("Honey",       5, 10),
                    new VendorGood("Dandelion",   3, 10),
                    new VendorGood("Thistle",     5, 10),
                }
            },
            new VendorType
            {
                Id = "huntsman", Title = "Huntsman", Kind = "coin",
                Goods = new[]
                {
                    new VendorGood("LeatherScraps", 3, 20),
                    new VendorGood("DeerHide",      4, 15),
                    new VendorGood("Feathers",      3, 20),
                    new VendorGood("ArrowFlint",    4, 20),
                    new VendorGood("Resin",         2, 20),
                }
            },
            new VendorType
            {
                Id = "stonemason", Title = "Stonemason", Kind = "barter",
                CostPrefab = "Stone", CostAmount = 40,
                GivePrefab = "Wood",  GiveAmount = 30,
            },

            // Biome vendors — summoned by their matching biome station. Each sells basic biome
            // resources plus one high-priced "rare" item (just a Good with a big price). Prefab
            // names are resolved at runtime; any that don't exist are skipped and logged, so a
            // wrong name simply means that item is absent (fix it in vendors.json).
            new VendorType
            {
                Id = "meadows", Title = "Meadows Trader", Kind = "coin", Biome = "Meadows",
                Goods = new[]
                {
                    new VendorGood("Wood",      1, 50),
                    new VendorGood("Stone",     1, 50),
                    new VendorGood("Flint",     2, 30),
                    new VendorGood("Resin",     2, 20),
                    new VendorGood("Dandelion", 3, 10),
                    new VendorGood("QueenBee", 800,  1),  // rare
                }
            },
            new VendorType
            {
                Id = "blackforest", Title = "Black Forest Trader", Kind = "coin", Biome = "BlackForest",
                Goods = new[]
                {
                    new VendorGood("FineWood",     6, 30),
                    new VendorGood("RoundLog",     6, 30),
                    new VendorGood("Coal",         2, 30),
                    new VendorGood("Copper",      50, 10),  // 5 coins/unit
                    new VendorGood("Tin",         50, 10),  // 5 coins/unit
                    new VendorGood("SurtlingCore", 200, 1), // rare
                }
            },
            new VendorType
            {
                Id = "swamp", Title = "Swamp Trader", Kind = "coin", Biome = "Swamp",
                Goods = new[]
                {
                    new VendorGood("Guck",         3, 20),
                    new VendorGood("Entrails",     3, 20),
                    new VendorGood("Bloodbag",     3, 20),
                    new VendorGood("WitheredBone", 6, 10),
                    new VendorGood("IronScrap",  100, 10),  // 10 coins/unit
                    new VendorGood("Chain",      400,  1),  // rare
                }
            },
            new VendorType
            {
                Id = "mountain", Title = "Mountain Trader", Kind = "coin", Biome = "Mountain",
                Goods = new[]
                {
                    new VendorGood("Obsidian",    4, 20),
                    new VendorGood("FreezeGland", 4, 20),
                    new VendorGood("WolfPelt",    6, 10),
                    new VendorGood("Onion",       3, 10),
                    new VendorGood("Crystal",     5, 20),
                    new VendorGood("Silver",     80,  5),   // 16 coins/unit
                    new VendorGood("DragonEgg", 9999, 1),   // rare
                }
            },
            new VendorType
            {
                Id = "plains", Title = "Plains Trader", Kind = "coin", Biome = "Plains",
                Goods = new[]
                {
                    new VendorGood("Barley",     4, 20),
                    new VendorGood("Flax",       4, 20),
                    new VendorGood("Cloudberry", 4, 20),
                    new VendorGood("Needle",     3, 20),
                    new VendorGood("Tar",        3, 30),
                    new VendorGood("BlackMetal", 100, 5),   // 20 coins/unit
                    new VendorGood("LoxPelt",  300,  1),    // rare
                }
            },

            // Turn-in bounties — barter villagers that buy monster trophies for Coins. They reuse the
            // barter mechanic (one fixed turn-in each) and are the main early way to EARN the coins
            // the pricey minerals above demand. A bounty scales with how dangerous its trophy is to
            // collect. Trophy prefab names are verified by the startup ItemNameAudit; a wrong one
            // just makes that bounty politely refuse rather than break anything.
            new VendorType
            {
                Id = "bounty_meadows", Title = "Meadows Bounty", Kind = "barter",
                CostPrefab = "TrophyBoar",      CostAmount = 2,
                GivePrefab = "Coins",           GiveAmount = 10,
            },
            new VendorType
            {
                Id = "bounty_forest", Title = "Forest Bounty", Kind = "barter",
                CostPrefab = "TrophyGreydwarf", CostAmount = 3,
                GivePrefab = "Coins",           GiveAmount = 24,
            },
            new VendorType
            {
                Id = "bounty_swamp", Title = "Swamp Bounty", Kind = "barter",
                CostPrefab = "TrophyDraugr",    CostAmount = 2,
                GivePrefab = "Coins",           GiveAmount = 45,
            },
            new VendorType
            {
                Id = "bounty_mountain", Title = "Mountain Bounty", Kind = "barter",
                CostPrefab = "TrophyWolf",      CostAmount = 2,
                GivePrefab = "Coins",           GiveAmount = 70,
            },
            new VendorType
            {
                Id = "bounty_plains", Title = "Plains Bounty", Kind = "barter",
                CostPrefab = "TrophyGoblin",    CostAmount = 2,
                GivePrefab = "Coins",           GiveAmount = 90,
            },
        };

        private static VendorType[] _all;

        /// <summary>The active catalogue (defaults until <see cref="Initialize"/> is called).</summary>
        public static VendorType[] All => _all ?? (_all = DefaultVendors);

        /// <summary>Replace the catalogue. Null or empty input falls back to the built-in defaults.</summary>
        public static void Initialize(VendorType[] vendors)
        {
            _all = (vendors != null && vendors.Length > 0) ? vendors : DefaultVendors;
        }

        /// <summary>Look up a type by id, falling back to the first type if unknown.</summary>
        public static VendorType ById(string id)
        {
            if (!string.IsNullOrEmpty(id))
                foreach (var v in All)
                    if (v != null && v.Id == id)
                        return v;
            return All[0];
        }

        /// <summary>Pick a type by index, wrapping safely for any int (including negatives).</summary>
        public static VendorType ByIndex(int index)
        {
            int i = ((index % All.Length) + All.Length) % All.Length;
            return All[i];
        }
    }
}
