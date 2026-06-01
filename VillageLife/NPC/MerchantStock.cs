using System.Collections.Generic;
using UnityEngine;

namespace VillageLife.NPC
{
    /// <summary>
    /// Resolves a vendor type's goods into vanilla <see cref="Trader.TradeItem"/> entries using
    /// ObjectDB. Goods above the trader's current reputation <paramref name="level"/> are hidden,
    /// and items that can't be resolved are skipped; the method returns an empty list (never null)
    /// if ObjectDB isn't ready, so the caller can keep the merchant's existing (vanilla) stock
    /// instead of showing an empty shop.
    /// </summary>
    public static class MerchantStock
    {
        public static List<Trader.TradeItem> Build(VendorType type, int level)
        {
            var list = new List<Trader.TradeItem>();
            if (type?.Goods == null)
                return list;

            ObjectDB odb = ObjectDB.instance;
            if (odb == null)
                return list;

            foreach (var g in type.Goods)
            {
                if (g.Tier > level)
                    continue;   // locked until the trader's reputation reaches this tier.

                GameObject go = odb.GetItemPrefab(g.Prefab);
                if (go == null)
                    continue;

                ItemDrop drop = go.GetComponent<ItemDrop>();
                if (drop == null)
                    continue;

                list.Add(new Trader.TradeItem
                {
                    m_prefab = drop,
                    m_price = g.Price,
                    m_stack = g.Stack,
                    m_requiredGlobalKey = ""
                });
            }

            return list;
        }
    }
}
