using System;
using System.Collections.Generic;
using UnityEngine;
using VillageLife.Config;
using VillageLife.UI;
using VillageLife.Util;

namespace VillageLife.NPC.Roles
{
    /// <summary>
    /// Merchant role — NPC buys and sells items using a config-driven inventory.
    /// Supports restocking on a timer and per-NPC shop type configuration.
    /// </summary>
    public class MerchantRole : INPCRole
    {
        public string RoleId => Constants.RoleMerchant;

        private ShopInventory _shopInventory;
        private float _restockTimer;

        public ShopInventory ShopInventory => _shopInventory;

        public void OnAssigned(VillageNPC npc)
        {
            // Load shop type from ZDO
            var zdo = npc.ZNetView?.GetZDO();
            string shopType = zdo?.GetString(VLData.Hash(VLData.KeyShopType), "general_store")
                              ?? "general_store";

            // Load inventory from config
            var shopConfig = ConfigManager.GetShopConfig(shopType);
            _shopInventory = new ShopInventory(shopConfig);

            // Check if we need to restore saved inventory state
            string savedInventory = zdo?.GetString(VLData.Hash(VLData.KeyShopInventory), "");
            if (!string.IsNullOrEmpty(savedInventory))
            {
                _shopInventory.DeserializeState(savedInventory);
            }

            // Initialize restock timer
            long lastRestock = zdo?.GetLong(VLData.Hash(VLData.KeyLastRestock), 0) ?? 0;
            float minutesSinceRestock = (float)(DateTimeOffset.UtcNow.ToUnixTimeSeconds() - lastRestock) / 60f;
            int restockMinutes = Plugin.VillageLifePlugin.MerchantRestockMinutes.Value;

            if (minutesSinceRestock >= restockMinutes)
            {
                _shopInventory.Restock();
                SaveRestockTime(npc);
            }

            _restockTimer = 0f;
        }

        public void OnRemoved(VillageNPC npc)
        {
            SaveInventoryState(npc);
        }

        public void OnInteract(VillageNPC npc, Player player)
        {
            TradePanel.Show(npc, this, player);
        }

        public string GetHoverText(VillageNPC npc)
        {
            return "[<color=yellow><b>$KEY_Use</b></color>] Trade";
        }

        public void OnUpdate(VillageNPC npc, float deltaTime)
        {
            _restockTimer += deltaTime;
            float restockSeconds = Plugin.VillageLifePlugin.MerchantRestockMinutes.Value * 60f;

            if (_restockTimer >= restockSeconds)
            {
                _restockTimer = 0f;
                _shopInventory?.Restock();
                SaveRestockTime(npc);
                SaveInventoryState(npc);
            }
        }

        private void SaveRestockTime(VillageNPC npc)
        {
            var zdo = npc.ZNetView?.GetZDO();
            zdo?.Set(VLData.Hash(VLData.KeyLastRestock), DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        }

        private void SaveInventoryState(VillageNPC npc)
        {
            var zdo = npc.ZNetView?.GetZDO();
            if (zdo == null || _shopInventory == null) return;
            zdo.Set(VLData.Hash(VLData.KeyShopInventory), _shopInventory.SerializeState());
        }

        #region Trade Operations

        /// <summary>
        /// Execute a buy operation. Returns true if successful.
        /// </summary>
        public bool TryBuy(VillageNPC npc, Player player, string itemName, int quantity)
        {
            if (_shopInventory == null) return false;

            var entry = _shopInventory.GetEntry(itemName);
            if (entry == null || entry.Stock < quantity) return false;

            int totalCost = entry.BuyPrice * quantity;
            string currency = _shopInventory.Currency;

            // Check player can afford it
            var inventory = player.GetInventory();
            int playerCoins = inventory.CountItems(currency);
            if (playerCoins < totalCost) return false;

            // Check player has inventory space
            var itemPrefab = ObjectDB.instance.GetItemPrefab(itemName);
            if (itemPrefab == null) return false;

            // Execute trade
            inventory.RemoveItem(currency, totalCost);
            inventory.AddItem(itemPrefab, quantity);
            _shopInventory.RemoveStock(itemName, quantity);
            SaveInventoryState(npc);

            return true;
        }

        /// <summary>
        /// Execute a sell operation. Returns true if successful.
        /// </summary>
        public bool TrySell(VillageNPC npc, Player player, string itemName, int quantity)
        {
            if (_shopInventory == null) return false;

            var entry = _shopInventory.GetEntry(itemName);
            if (entry == null || entry.SellPrice <= 0) return false;

            int totalValue = entry.SellPrice * quantity;
            string currency = _shopInventory.Currency;

            // Check player actually has the items
            var inventory = player.GetInventory();
            if (inventory.CountItems(itemName) < quantity) return false;

            // Execute trade
            inventory.RemoveItem(itemName, quantity);

            var coinPrefab = ObjectDB.instance.GetItemPrefab(currency);
            if (coinPrefab != null)
                inventory.AddItem(coinPrefab, totalValue);

            _shopInventory.AddStock(itemName, quantity);
            SaveInventoryState(npc);

            return true;
        }

        #endregion
    }

    /// <summary>
    /// Represents a merchant's current inventory with stock levels and prices.
    /// </summary>
    public class ShopInventory
    {
        private readonly ShopConfig _config;
        private readonly Dictionary<string, ShopEntry> _entries = new Dictionary<string, ShopEntry>();

        public string Currency => _config?.Currency ?? "Coins";
        public IReadOnlyDictionary<string, ShopEntry> Entries => _entries;

        public ShopInventory(ShopConfig config)
        {
            _config = config;
            if (config?.Items != null)
            {
                foreach (var item in config.Items)
                {
                    _entries[item.ItemName] = new ShopEntry
                    {
                        ItemName = item.ItemName,
                        BuyPrice = item.BuyPrice,
                        SellPrice = item.SellPrice,
                        Stock = item.MaxStock,
                        MaxStock = item.MaxStock
                    };
                }
            }
        }

        public ShopEntry GetEntry(string itemName)
        {
            _entries.TryGetValue(itemName, out var entry);
            return entry;
        }

        public void RemoveStock(string itemName, int amount)
        {
            if (_entries.TryGetValue(itemName, out var entry))
                entry.Stock = Mathf.Max(0, entry.Stock - amount);
        }

        public void AddStock(string itemName, int amount)
        {
            if (_entries.TryGetValue(itemName, out var entry))
                entry.Stock = Mathf.Min(entry.MaxStock * 2, entry.Stock + amount);
        }

        public void Restock()
        {
            foreach (var entry in _entries.Values)
                entry.Stock = entry.MaxStock;
        }

        public string SerializeState()
        {
            var parts = new List<string>();
            foreach (var kvp in _entries)
                parts.Add($"{kvp.Key}:{kvp.Value.Stock}");
            return string.Join(";", parts);
        }

        public void DeserializeState(string data)
        {
            if (string.IsNullOrEmpty(data)) return;

            var parts = data.Split(';');
            foreach (var part in parts)
            {
                var kv = part.Split(':');
                if (kv.Length == 2 && _entries.TryGetValue(kv[0], out var entry))
                {
                    if (int.TryParse(kv[1], out int stock))
                        entry.Stock = stock;
                }
            }
        }
    }

    public class ShopEntry
    {
        public string ItemName;
        public int BuyPrice;
        public int SellPrice;
        public int Stock;
        public int MaxStock;
    }
}
