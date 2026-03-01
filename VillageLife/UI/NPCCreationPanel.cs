using UnityEngine;
using VillageLife.NPC;
using VillageLife.NPC.Roles;
using VillageLife.Util;

namespace VillageLife.UI
{
    /// <summary>
    /// UI panel for creating new NPCs at the Village Hall.
    /// Allows selecting name, role, gender, and basic appearance options.
    /// </summary>
    public static class NPCCreationPanel
    {
        private static bool _visible;
        private static VillageHallInteraction _station;
        private static Rect _windowRect = new Rect(Screen.width / 2 - 200, Screen.height / 2 - 250, 400, 500);

        // Form state
        private static string _npcName = "Villager";
        private static int _selectedRole;
        private static bool _isMale = true;
        private static int _selectedHair;
        private static int _selectedBeard;
        private static string _selectedShopType = "general_store";

        private static readonly string[] _roleNames = { "Merchant", "Quest Giver", "Guard", "Villager" };
        private static readonly string[] _roleIds = { Constants.RoleMerchant, Constants.RoleQuestGiver, Constants.RoleGuard, Constants.RoleVillager };
        private static readonly string[] _hairStyles = { "Hair1", "Hair2", "Hair3", "Hair4", "Hair5", "Hair6" };
        private static readonly string[] _beardStyles = { "None", "Beard1", "Beard2", "Beard3", "Beard4" };
        private static readonly string[] _shopTypes = { "general_store", "weaponsmith", "armorer", "food_vendor" };
        private static readonly string[] _shopTypeNames = { "General Store", "Weaponsmith", "Armorer", "Food Vendor" };
        private static readonly string[] _questPools = { "meadows", "blackforest", "swamp" };
        private static readonly string[] _questPoolNames = { "Meadows", "Black Forest", "Swamp" };
        private static int _selectedQuestPool;

        private static Vector2 _scrollPos;

        public static bool IsVisible => _visible;

        public static void Show(VillageHallInteraction station)
        {
            _station = station;
            _visible = true;
            _npcName = "Villager";
            _selectedRole = 3; // Default to Villager
            _isMale = true;
            _selectedHair = 0;
            _selectedBeard = 0;
            _selectedShopType = "general_store";
            _selectedQuestPool = 0;

            // Lock player input
            GUIManager.BlockInput(true);
        }

        public static void Hide()
        {
            _visible = false;
            _station = null;
            GUIManager.BlockInput(false);
        }

        public static void OnGUI()
        {
            if (!_visible) return;

            GUI.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.95f);
            _windowRect = GUI.Window(9001, _windowRect, DrawWindow, "NPC Workshop");
        }

        private static void DrawWindow(int windowId)
        {
            GUILayout.Space(10);

            _scrollPos = GUILayout.BeginScrollView(_scrollPos);

            // Name input
            GUILayout.Label("<b>Name:</b>", CreateLabelStyle());
            _npcName = GUILayout.TextField(_npcName, 30);
            GUILayout.Space(10);

            // Role selection
            GUILayout.Label("<b>Role:</b>", CreateLabelStyle());
            _selectedRole = GUILayout.SelectionGrid(_selectedRole, _roleNames, 2);
            GUILayout.Space(10);

            // Role-specific options
            if (_roleIds[_selectedRole] == Constants.RoleMerchant)
            {
                GUILayout.Label("<b>Shop Type:</b>", CreateLabelStyle());
                int shopIdx = System.Array.IndexOf(_shopTypes, _selectedShopType);
                if (shopIdx < 0) shopIdx = 0;
                shopIdx = GUILayout.SelectionGrid(shopIdx, _shopTypeNames, 2);
                _selectedShopType = _shopTypes[shopIdx];
                GUILayout.Space(5);
            }
            else if (_roleIds[_selectedRole] == Constants.RoleQuestGiver)
            {
                GUILayout.Label("<b>Quest Region:</b>", CreateLabelStyle());
                _selectedQuestPool = GUILayout.SelectionGrid(_selectedQuestPool, _questPoolNames, 3);
                GUILayout.Space(5);
            }

            // Gender
            GUILayout.Label("<b>Gender:</b>", CreateLabelStyle());
            GUILayout.BeginHorizontal();
            if (GUILayout.Toggle(_isMale, "Male")) _isMale = true;
            if (GUILayout.Toggle(!_isMale, "Female")) _isMale = false;
            GUILayout.EndHorizontal();
            GUILayout.Space(10);

            // Hair style
            GUILayout.Label("<b>Hair Style:</b>", CreateLabelStyle());
            _selectedHair = GUILayout.SelectionGrid(_selectedHair, _hairStyles, 3);
            GUILayout.Space(10);

            // Beard (only for male NPCs)
            if (_isMale)
            {
                GUILayout.Label("<b>Beard Style:</b>", CreateLabelStyle());
                _selectedBeard = GUILayout.SelectionGrid(_selectedBeard, _beardStyles, 3);
                GUILayout.Space(10);
            }

            GUILayout.EndScrollView();

            GUILayout.Space(10);

            // Buttons
            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Create NPC", GUILayout.Height(40)))
            {
                CreateNPC();
            }

            if (GUILayout.Button("Cancel", GUILayout.Height(40)))
            {
                Hide();
            }

            GUILayout.EndHorizontal();

            GUI.DragWindow();
        }

        private static void CreateNPC()
        {
            var player = Player.m_localPlayer;
            if (player == null)
            {
                Hide();
                return;
            }

            // Check NPC limit
            if (!NPCManager.CanPlayerPlaceNPC(player.GetPlayerID()))
            {
                MessageHud.instance?.ShowMessage(MessageHud.MessageType.Center,
                    $"NPC limit reached ({Plugin.VillageLifePlugin.MaxNPCsPerPlayer.Value})!");
                return;
            }

            // Check build resources
            var inventory = player.GetInventory();
            if (inventory.CountItems("Wood") < 5 || inventory.CountItems("LeatherScraps") < 2)
            {
                MessageHud.instance?.ShowMessage(MessageHud.MessageType.Center,
                    "Not enough materials! Need 5 Wood and 2 Leather Scraps.");
                return;
            }

            // Consume resources
            inventory.RemoveItem("Wood", 5);
            inventory.RemoveItem("LeatherScraps", 2);

            // Place NPC in front of the player
            Vector3 spawnPos = player.transform.position + player.transform.forward * 2f;
            Quaternion spawnRot = Quaternion.LookRotation(-player.transform.forward);

            var prefab = ZNetScene.instance?.GetPrefab(Constants.NPCPrefabName);
            if (prefab == null)
            {
                MessageHud.instance?.ShowMessage(MessageHud.MessageType.Center,
                    "Error: NPC prefab not found!");
                Hide();
                return;
            }

            var npcObj = UnityEngine.Object.Instantiate(prefab, spawnPos, spawnRot);
            var npc = npcObj.GetComponent<VillageNPC>();

            if (npc != null)
            {
                string roleId = _roleIds[_selectedRole];
                string beardStyle = _isMale ? _beardStyles[_selectedBeard] : "";

                npc.Configure(
                    _npcName,
                    roleId,
                    player.GetPlayerID(),
                    _isMale,
                    _hairStyles[_selectedHair],
                    beardStyle
                );

                // Set role-specific ZDO data
                var zdo = npc.ZNetView?.GetZDO();
                if (zdo != null)
                {
                    if (roleId == Constants.RoleMerchant)
                        zdo.Set(VLData.Hash(VLData.KeyShopType), _selectedShopType);
                    else if (roleId == Constants.RoleQuestGiver)
                        zdo.Set(VLData.Hash(VLData.KeyQuestPool), _questPools[_selectedQuestPool]);
                }

                NPCPrefabFactory.ApplyAppearance(npc);

                MessageHud.instance?.ShowMessage(MessageHud.MessageType.Center,
                    $"{_npcName} the {_roleNames[_selectedRole]} has joined the village!");
            }

            Hide();
        }

        private static GUIStyle CreateLabelStyle()
        {
            var style = new GUIStyle(GUI.skin.label);
            style.richText = true;
            style.fontSize = 14;
            return style;
        }
    }

    /// <summary>
    /// Helper to manage GUI input blocking.
    /// </summary>
    public static class GUIManager
    {
        private static bool _inputBlocked;

        public static bool IsInputBlocked => _inputBlocked;

        public static void BlockInput(bool block)
        {
            _inputBlocked = block;
            // Toggle game input
            if (GameCamera.instance != null)
                GameCamera.instance.enabled = !block;
        }
    }
}
