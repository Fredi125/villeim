namespace VillageLife.NPC
{
    /// <summary>One thing a coin-shop vendor offers: a prefab, its price, and the stack size sold.</summary>
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
    /// How a vendor does business.
    ///   • <see cref="CoinShop"/> — sells goods for coins through Valheim's own trade window.
    ///   • <see cref="Barter"/>   — gives a single product for a fixed resource (e.g. 40 Stone →
    ///     30 Wood) via a small no-UI interaction. No coins, no custom window.
    /// </summary>
    public enum VendorKind
    {
        CoinShop,
        Barter
    }

    /// <summary>A kind of villager: a display title plus what it offers.</summary>
    public class VendorType
    {
        public string Id;
        public string Title;
        public VendorKind Kind;

        // CoinShop: the goods sold for coins.
        public VendorGood[] Goods;

        // Barter: "give CostAmount × CostPrefab, receive GiveAmount × GivePrefab".
        public string CostPrefab;
        public int CostAmount;
        public string GivePrefab;
        public int GiveAmount;
    }

    /// <summary>
    /// The catalogue of villager types. This is the single, obvious place to add a new kind of
    /// vendor — append an entry and it joins the rotation automatically. Prefab names are resolved
    /// at runtime and any that don't exist are skipped, so a typo offers fewer goods (or a barter
    /// that politely refuses) rather than crashing.
    ///
    /// Planned: this catalogue will move to a JSON config so types/goods/prices are editable
    /// without rebuilding (see README roadmap).
    /// </summary>
    public static class VendorCatalog
    {
        public static readonly VendorType[] All =
        {
            new VendorType
            {
                Id = "general", Title = "General Store", Kind = VendorKind.CoinShop,
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
                Id = "forager", Title = "Forager", Kind = VendorKind.CoinShop,
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
                Id = "huntsman", Title = "Huntsman", Kind = VendorKind.CoinShop,
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
                Id = "stonemason", Title = "Stonemason", Kind = VendorKind.Barter,
                CostPrefab = "Stone", CostAmount = 40,
                GivePrefab = "Wood",  GiveAmount = 30,
            },
        };

        /// <summary>Look up a type by id, falling back to the first type if unknown.</summary>
        public static VendorType ById(string id)
        {
            if (!string.IsNullOrEmpty(id))
                foreach (var v in All)
                    if (v.Id == id)
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
