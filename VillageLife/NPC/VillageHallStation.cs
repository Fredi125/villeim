using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;
using VillageLife.Util;

namespace VillageLife.NPC
{
    /// <summary>
    /// The Village Hall — a buildable piece that opens the NPC Workshop when used.
    ///
    /// 2.0: Built with Jötunn's <c>CustomPiece(name, basePrefab, config)</c> constructor,
    /// which clones <c>piece_workbench</c> through the disabled prefab container (no Awake
    /// crashes) and registers the piece exactly once (no "already exists" double-register).
    /// We only remove the CraftingStation component so our interaction handler wins the
    /// E key, and leave WearNTear/Piece/ZNetView intact so it behaves like a normal build.
    /// </summary>
    public static class VillageHallStation
    {
        private const string BasePrefab = "piece_workbench";

        public static void Register()
        {
            // Borrow the workbench's icon so the hammer entry is never blank.
            Sprite icon = null;
            var workbench = PrefabManager.Instance.GetPrefab(BasePrefab);
            if (workbench != null)
            {
                var wbPiece = workbench.GetComponent<Piece>();
                if (wbPiece != null)
                    icon = wbPiece.m_icon;
            }

            var config = new PieceConfig
            {
                Name = "$piece_vl_villagehall",
                Description = "$piece_vl_villagehall_desc",
                PieceTable = "Hammer",
                Category = Constants.PieceCategory,
                Icon = icon,
                Requirements = new[]
                {
                    new RequirementConfig { Item = "Wood", Amount = 20, Recover = true },
                    new RequirementConfig { Item = "Stone", Amount = 10, Recover = true },
                    new RequirementConfig { Item = "Resin", Amount = 5, Recover = true }
                }
            };

            // Jötunn clones piece_workbench into a fresh prefab named VL_VillageHall.
            var customPiece = new CustomPiece(Constants.VillageHallPrefabName, BasePrefab, config);
            var prefab = customPiece.PiecePrefab;

            if (prefab != null)
            {
                // Remove the crafting-station behaviour so our E interaction is used instead.
                var craftingStation = prefab.GetComponent<CraftingStation>();
                if (craftingStation != null)
                    Object.DestroyImmediate(craftingStation);

                // Our interaction handler opens the NPC Workshop UI.
                prefab.AddComponent<VillageHallInteraction>();
            }

            PieceManager.Instance.AddPiece(customPiece);

            Jotunn.Logger.LogInfo("[VillageLife] Village Hall piece registered.");
        }
    }

    /// <summary>
    /// Hover text + E-key handler for a placed Village Hall.
    /// Opens the NPC Workshop where players design and place villagers.
    /// </summary>
    public class VillageHallInteraction : MonoBehaviour, Hoverable, Interactable
    {
        public string GetHoverText()
        {
            return global::Localization.instance.Localize(
                "$piece_vl_villagehall\n" +
                "[<color=yellow><b>$KEY_Use</b></color>] Open NPC Workshop");
        }

        public string GetHoverName()
        {
            return global::Localization.instance.Localize("$piece_vl_villagehall");
        }

        public bool Interact(Humanoid user, bool hold, bool alt)
        {
            if (hold) return false;
            if (!(user is Player)) return false;

            UI.NPCCreationPanel.Show(this);
            return true;
        }

        public bool UseItem(Humanoid user, ItemDrop.ItemData item)
        {
            return false;
        }
    }
}
