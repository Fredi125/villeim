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
            // Clone the workbench just for its visual mesh. We strip ALL gameplay
            // components (CraftingStation, WearNTear, ZNetView, Piece, EffectArea)
            // because their Awake() methods reference objects/RPCs from the original
            // workbench that don't exist on the clone, causing silent crashes and
            // invisible buildings.
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

            // Strip gameplay components with complex Awake() logic that references
            // workbench-specific objects, RPCs, or effects. Keep Piece (data-only,
            // no risky Awake) — Jötunn requires it for CustomPiece validation.
            StripComponent<WearNTear>(_prefab);
            StripComponent<CraftingStation>(_prefab);

            // Remove stale ZNetView (has workbench RPCs/hash) and add a fresh one
            StripComponent<ZNetView>(_prefab);
            var zNetView = _prefab.AddComponent<ZNetView>();
            zNetView.m_persistent = true;

            // Strip EffectArea from children (workbench area-of-effect markers)
            foreach (var ea in _prefab.GetComponentsInChildren<EffectArea>(true))
                Object.DestroyImmediate(ea);

            // Add our custom interaction component
            _prefab.AddComponent<VillageHallInteraction>();

            _prefab.SetActive(false);
        }

        private static void StripComponent<T>(GameObject obj) where T : Component
        {
            var comp = obj.GetComponent<T>();
            if (comp != null)
                Object.DestroyImmediate(comp);
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
