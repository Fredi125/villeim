using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;
using VillageLife.Util;

namespace VillageLife.NPC
{
    /// <summary>
    /// The Village Hall crafting station — a custom piece that opens the NPC creation UI
    /// when players interact with it. NPCs are "crafted" here by selecting role, name,
    /// and appearance before being placed via the build hammer.
    /// </summary>
    public static class VillageHallStation
    {
        private static GameObject _prefab;

        public static void RegisterPrefab()
        {
            // Base the Village Hall on an existing crafting station for the mesh
            var basePrefab = PrefabManager.Instance.GetPrefab("piece_workbench");
            if (basePrefab == null)
            {
                Debug.LogError("[VillageLife] Could not find workbench prefab for Village Hall!");
                return;
            }

            _prefab = Object.Instantiate(basePrefab);
            _prefab.name = Constants.VillageHallPrefabName;

            // Replace the CraftingStation behavior with our own
            var existingStation = _prefab.GetComponent<CraftingStation>();
            if (existingStation != null)
            {
                existingStation.m_name = "$piece_vl_villagehall";
                existingStation.m_rangeBuild = 20f;
            }

            // Add our custom interaction component
            _prefab.AddComponent<VillageHallInteraction>();

            // Ensure ZNetView persistence
            var zNetView = _prefab.GetComponent<ZNetView>();
            if (zNetView != null)
                zNetView.m_persistent = true;

            _prefab.SetActive(false);

            PrefabManager.Instance.AddPrefab(new CustomPrefab(_prefab, false));
        }

        public static void RegisterPiece()
        {
            if (_prefab == null) return;

            var pieceConfig = new PieceConfig
            {
                Name = "$piece_vl_villagehall",
                Description = "$piece_vl_villagehall_desc",
                PieceTable = "Hammer",
                Category = Constants.PieceCategory,
                Requirements = new[]
                {
                    new RequirementConfig { Item = "Wood", Amount = 20, Recover = true },
                    new RequirementConfig { Item = "Stone", Amount = 10, Recover = true },
                    new RequirementConfig { Item = "Resin", Amount = 5, Recover = true }
                }
            };

            PieceManager.Instance.AddPiece(new CustomPiece(_prefab, true, pieceConfig));
        }
    }

    /// <summary>
    /// Interaction handler for the Village Hall.
    /// Opens the NPC creation UI when the player presses E.
    /// </summary>
    public class VillageHallInteraction : MonoBehaviour, Hoverable, Interactable
    {
        public string GetHoverText()
        {
            return Localization.instance.Localize(
                "$piece_vl_villagehall\n" +
                "[<color=yellow><b>$KEY_Use</b></color>] Open NPC Workshop");
        }

        public string GetHoverName()
        {
            return "$piece_vl_villagehall";
        }

        public bool Interact(Humanoid user, bool hold, bool alt)
        {
            if (hold) return false;

            var player = user as Player;
            if (player == null) return false;

            // Open NPC creation UI
            UI.NPCCreationPanel.Show(this);
            return true;
        }

        public bool UseItem(Humanoid user, ItemDrop.ItemData item)
        {
            return false;
        }
    }
}
