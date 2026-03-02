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
            // Clone the workbench for its visual mesh only.
            var basePrefab = PrefabManager.Instance.GetPrefab("piece_workbench");
            if (basePrefab == null)
            {
                Debug.LogError("[VillageLife] Could not find workbench prefab for Village Hall!");
                return;
            }

            // Deactivate source before cloning to prevent Awake() from firing on the clone
            bool wasActive = basePrefab.activeSelf;
            basePrefab.SetActive(false);
            _prefab = Object.Instantiate(basePrefab);
            basePrefab.SetActive(wasActive);
            _prefab.name = Constants.VillageHallPrefabName;

            // Strip ALL MonoBehaviour scripts from root and children. This removes every
            // gameplay component (WearNTear, CraftingStation, ZNetView, Piece, EffectArea,
            // child effect scripts, etc.) while preserving visual/physics components
            // (MeshFilter, MeshRenderer, Transform, Collider) which are NOT MonoBehaviours.
            foreach (var mb in _prefab.GetComponentsInChildren<MonoBehaviour>(true))
                Object.DestroyImmediate(mb);

            // Now safe to activate — only built-in visual/physics components remain.
            // The prefab MUST be active because Valheim's Instantiate preserves active
            // state, and Player.PlacePiece never calls SetActive on placed instances.
            _prefab.SetActive(true);

            // Add required components on the now-active prefab.
            // At OnVanillaPrefabsAvailable time, ZNet.instance is null (no world loaded),
            // so ZNetView.Awake() early-returns without registering a phantom ZDO.
            var zNetView = _prefab.AddComponent<ZNetView>();
            zNetView.m_persistent = true;

            _prefab.AddComponent<Piece>();
            _prefab.AddComponent<VillageHallInteraction>();
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

            // Use a vanilla piece icon so Jötunn accepts the piece as valid
            var workbenchPrefab = PrefabManager.Instance.GetPrefab("piece_workbench");
            if (workbenchPrefab != null)
            {
                var wbPiece = workbenchPrefab.GetComponent<Piece>();
                if (wbPiece != null && wbPiece.m_icon != null)
                    pieceConfig.Icon = wbPiece.m_icon;
            }

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
            return global::Localization.instance.Localize(
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
