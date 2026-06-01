using System.Collections.Generic;

namespace VillageLife.NPC
{
    /// <summary>One thing a vendor offers: a prefab, its price, and the stack size sold.</summary>
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
    ///   • <see cref="CoinShop"/> — sells goods for coins through Valheim's own trade window. (Built.)
    ///   • <see cref="Barter"/>   — gives a single product for a fixed resource (e.g. 40 Stone → 30
    ///     Wood). Planned: it steps off the vanilla shop and uses a small no-UI interact handler.
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
        public VendorGood[] Goods;   // CoinShop: the goods sold for coins.
    }

    /// <summary>
    /// The catalogue of villager types. This is the single, obvious place to add a new kind of
    /// merchant — append an entry and it joins the rotation automatically. Only coin shops exist
    /// today; barter vendors are the planned next type (see README roadmap).
    ///
    /// Prefab names are resolved at runtime and any that don't exist are skipped, so an entry with
    /// a typo simply offers fewer goods rather than crashing.
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
