using UnityEngine;

namespace VillageLife.NPC
{
    /// <summary>
    /// Helpers for the two different "names" Valheim items have, which is the easy thing to get
    /// wrong when moving items in/out of an inventory:
    ///   • the PREFAB name (e.g. "Stone")        — used by Inventory.AddItem and ObjectDB lookups,
    ///   • the SHARED name (e.g. "$item_stone")   — used by Inventory.CountItems / RemoveItem.
    /// Resolving both from the same ItemDrop keeps a barter exchange symmetric and safe.
    /// </summary>
    public static class ItemNames
    {
        /// <summary>The item's shared (localized-token) name, or null if it can't be resolved.</summary>
        public static string SharedName(string prefabName)
        {
            ItemDrop drop = ResolveDrop(prefabName);
            return drop != null ? drop.m_itemData.m_shared.m_name : null;
        }

        /// <summary>True if the prefab resolves to a real ItemDrop in ObjectDB.</summary>
        public static bool Exists(string prefabName) => ResolveDrop(prefabName) != null;

        /// <summary>The item prefab GameObject (for Inventory.AddItem), or null if unresolved.</summary>
        public static GameObject Prefab(string prefabName)
        {
            ItemDrop drop = ResolveDrop(prefabName);
            return drop != null ? drop.gameObject : null;
        }

        /// <summary>A friendly display label for messages (the localized shared name, else the prefab).</summary>
        public static string Display(string prefabName)
        {
            string shared = SharedName(prefabName);
            if (string.IsNullOrEmpty(shared))
                return prefabName;
            return Localization.instance != null ? Localization.instance.Localize(shared) : shared;
        }

        private static ItemDrop ResolveDrop(string prefabName)
        {
            ObjectDB odb = ObjectDB.instance;
            if (odb == null || string.IsNullOrEmpty(prefabName))
                return null;

            GameObject go = odb.GetItemPrefab(prefabName);
            return go != null ? go.GetComponent<ItemDrop>() : null;
        }
    }
}
