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
                    new VendorGood("Raspberries", 2, 20),
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
