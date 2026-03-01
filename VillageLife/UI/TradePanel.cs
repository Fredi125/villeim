using System.Linq;
using UnityEngine;
using VillageLife.NPC;
using VillageLife.NPC.Roles;

namespace VillageLife.UI
{
    /// <summary>
    /// Trade UI panel for merchant NPC interactions.
    /// Two-column layout: NPC inventory (buy) on the left, player inventory (sell) on the right.
    /// Shows coin balance and handles buy/sell transactions.
    /// </summary>
    public static class TradePanel
    {
        private static bool _visible;
        private static VillageNPC _npc;
        private static MerchantRole _merchant;
        private static Player _player;
        private static Rect _windowRect = new Rect(Screen.width / 2 - 300, Screen.height / 2 - 250, 600, 500);

        private static Vector2 _shopScrollPos;
        private static Vector2 _playerScrollPos;
        private static int _buyQuantity = 1;
        private static int _sellQuantity = 1;

        public static bool IsVisible => _visible;

        public static void Show(VillageNPC npc, MerchantRole merchant, Player player)
        {
            _npc = npc;
            _merchant = merchant;
            _player = player;
            _visible = true;
            _buyQuantity = 1;
            _sellQuantity = 1;
            GUIManager.BlockInput(true);
        }

        public static void Hide()
        {
            _visible = false;
            _npc = null;
            _merchant = null;
            _player = null;
            GUIManager.BlockInput(false);
        }

        public static void OnGUI()
        {
            if (!_visible || _merchant == null || _player == null) return;

            GUI.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.95f);
            _windowRect = GUI.Window(9002, _windowRect, DrawWindow, $"Trading with {_npc?.NPCName ?? "Merchant"}");
        }

        private static void DrawWindow(int windowId)
        {
            var labelStyle = new GUIStyle(GUI.skin.label) { richText = true, fontSize = 13 };
            var headerStyle = new GUIStyle(GUI.skin.label) { richText = true, fontSize = 16, fontStyle = FontStyle.Bold };

            // Coin display
            int playerCoins = _player.GetInventory().CountItems(_merchant.ShopInventory.Currency);
            GUILayout.Label($"<color=yellow>Your Coins: {playerCoins}</color>", headerStyle);
            GUILayout.Space(5);

            GUILayout.BeginHorizontal();

            // LEFT COLUMN: Shop inventory (Buy)
            GUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(280));
            GUILayout.Label("<b><color=cyan>Buy from Merchant</color></b>", headerStyle);
            GUILayout.Space(5);

            _shopScrollPos = GUILayout.BeginScrollView(_shopScrollPos, GUILayout.Height(320));

            foreach (var entry in _merchant.ShopInventory.Entries.Values)
            {
                if (entry.BuyPrice <= 0) continue;

                GUILayout.BeginHorizontal(GUI.skin.box);
                GUILayout.Label($"<b>{entry.ItemName}</b>", labelStyle, GUILayout.Width(100));
                GUILayout.Label($"Price: {entry.BuyPrice}c", labelStyle, GUILayout.Width(70));
                GUILayout.Label($"Stock: {entry.Stock}", labelStyle, GUILayout.Width(60));

                bool canAfford = playerCoins >= entry.BuyPrice && entry.Stock > 0;
                GUI.enabled = canAfford;
                if (GUILayout.Button("Buy", GUILayout.Width(40)))
                {
                    if (_merchant.TryBuy(_npc, _player, entry.ItemName, 1))
                    {
                        MessageHud.instance?.ShowMessage(MessageHud.MessageType.TopLeft,
                            $"Bought {entry.ItemName}");
                    }
                }
                GUI.enabled = true;

                GUILayout.EndHorizontal();
            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            GUILayout.Space(10);

            // RIGHT COLUMN: Player sellable items
            GUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(280));
            GUILayout.Label("<b><color=green>Sell to Merchant</color></b>", headerStyle);
            GUILayout.Space(5);

            _playerScrollPos = GUILayout.BeginScrollView(_playerScrollPos, GUILayout.Height(320));

            // Show player items that the merchant will buy
            foreach (var entry in _merchant.ShopInventory.Entries.Values)
            {
                if (entry.SellPrice <= 0) continue;

                int playerCount = _player.GetInventory().CountItems(entry.ItemName);
                if (playerCount <= 0) continue;

                GUILayout.BeginHorizontal(GUI.skin.box);
                GUILayout.Label($"<b>{entry.ItemName}</b>", labelStyle, GUILayout.Width(100));
                GUILayout.Label($"Value: {entry.SellPrice}c", labelStyle, GUILayout.Width(70));
                GUILayout.Label($"Own: {playerCount}", labelStyle, GUILayout.Width(60));

                if (GUILayout.Button("Sell", GUILayout.Width(40)))
                {
                    if (_merchant.TrySell(_npc, _player, entry.ItemName, 1))
                    {
                        MessageHud.instance?.ShowMessage(MessageHud.MessageType.TopLeft,
                            $"Sold {entry.ItemName} for {entry.SellPrice} coins");
                    }
                }

                GUILayout.EndHorizontal();
            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            // Close button
            if (GUILayout.Button("Close", GUILayout.Height(35)))
            {
                Hide();
            }

            GUI.DragWindow();
        }
    }
}
