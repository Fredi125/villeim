using System.Collections.Generic;
using UnityEngine;

namespace VillageLife.NPC
{
    /// <summary>
    /// Builds the goods a VillageLife merchant offers for sale. Item prefabs are resolved from
    /// ObjectDB at runtime; any entry that can't be resolved is skipped. If the whole list comes
    /// back empty (e.g. ObjectDB not ready yet), the caller keeps the merchant's existing
    /// (vanilla Haldor) stock, so a merchant is never left with an empty store.
    ///
    /// This table is the obvious, low-risk place to extend the economy later — or to drive from
    /// a config file once we want per-merchant shops.
    /// </summary>
    public static class MerchantStock
    {
        // (prefab name, price in coins, stack size). Deliberately common, always-present
        // resources so resolution is reliable across game versions.
        private static readonly (string prefab, int price, int stack)[] Goods =
        {
            ("Wood",          1, 20),
            ("Stone",         1, 20),
            ("Coal",          2, 10),
            ("Flint",         3, 10),
            ("LeatherScraps", 3, 10),
            ("Resin",         2, 10),
            ("Feathers",      3, 10),
            ("Thistle",       5,  5),
        };

        /// <summary>
        /// Resolve <see cref="Goods"/> into trade items. Returns an empty list (never null) if
        /// ObjectDB isn't available, so callers can detect "couldn't build" and keep vanilla stock.
        /// </summary>
        public static List<Trader.TradeItem> Build()
        {
            var list = new List<Trader.TradeItem>();

            ObjectDB odb = ObjectDB.instance;
            if (odb == null)
                return list;

            foreach (var g in Goods)
            {
                GameObject go = odb.GetItemPrefab(g.prefab);
                if (go == null)
                    continue;

                ItemDrop drop = go.GetComponent<ItemDrop>();
                if (drop == null)
                    continue;

                list.Add(new Trader.TradeItem
                {
                    m_prefab = drop,
                    m_price = g.price,
                    m_stack = g.stack,
                    m_requiredGlobalKey = ""
                });
            }

            return list;
        }
    }
}
