using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;
using VillageLife.Util;

namespace VillageLife.NPC
{
    /// <summary>
    /// Creates and registers the NPC prefab based on Valheim's humanoid model.
    /// Strips unnecessary components (combat, player inventory) and attaches VillageNPC.
    /// </summary>
    public static class NPCPrefabFactory
    {
        private static GameObject _npcPrefab;

        public static void RegisterPrefabs()
        {
            _npcPrefab = CreateNPCPrefab();
            if (_npcPrefab != null)
            {
                PrefabManager.Instance.AddPrefab(new CustomPrefab(_npcPrefab, false));
            }
        }

        public static void RegisterPieces()
        {
            if (_npcPrefab == null) return;

            var pieceConfig = new PieceConfig
            {
                Name = "$piece_vl_npc",
                Description = "$piece_vl_npc_desc",
                PieceTable = "Hammer",
                Category = Constants.PieceCategory,
                AllowedInDungeons = false,
                Requirements = new[]
                {
                    new RequirementConfig { Item = "Wood", Amount = 5, Recover = true },
                    new RequirementConfig { Item = "LeatherScraps", Amount = 2, Recover = true }
                }
            };

            PieceManager.Instance.AddPiece(new CustomPiece(_npcPrefab, true, pieceConfig));
        }

        private static GameObject CreateNPCPrefab()
        {
            // Clone from a vanilla humanoid prefab (e.g., the player or a villager-type NPC)
            var basePrefab = PrefabManager.Instance.GetPrefab("Player");
            if (basePrefab == null)
            {
                Debug.LogError("[VillageLife] Could not find Player prefab to base NPC on!");
                return null;
            }

            var npcObj = Object.Instantiate(basePrefab);
            npcObj.name = Constants.NPCPrefabName;

            // Remove player-specific components we don't need
            RemoveComponent<Player>(npcObj);
            RemoveComponent<PlayerController>(npcObj);
            RemoveComponent<Talker>(npcObj);
            RemoveComponent<Skills>(npcObj);
            RemoveComponent<CraftingStation>(npcObj);

            // Clear default attack items on the humanoid to prevent NPC combat
            var existingHumanoid = npcObj.GetComponent<Humanoid>();
            if (existingHumanoid != null)
                existingHumanoid.m_defaultItems = System.Array.Empty<GameObject>();

            // Ensure we have a ZNetView
            var zNetView = npcObj.GetComponent<ZNetView>();
            if (zNetView == null)
                zNetView = npcObj.AddComponent<ZNetView>();

            zNetView.m_persistent = true;

            // Add the VillageNPC component
            npcObj.AddComponent<VillageNPC>();

            // Add a basic humanoid component for animations (if not already present)
            var humanoid = npcObj.GetComponent<Humanoid>();
            if (humanoid == null)
                humanoid = npcObj.AddComponent<Humanoid>();

            // Configure humanoid - make non-hostile and non-targetable by enemies
            humanoid.m_name = "$npc_vl_villager";
            humanoid.m_faction = Character.Faction.Players;
            humanoid.m_group = "villagelife";

            // Add a Piece component for building system
            var piece = npcObj.GetComponent<Piece>();
            if (piece == null)
                piece = npcObj.AddComponent<Piece>();

            // Setup collider for interaction
            var collider = npcObj.GetComponent<CapsuleCollider>();
            if (collider == null)
            {
                collider = npcObj.AddComponent<CapsuleCollider>();
                collider.center = new Vector3(0, 0.9f, 0);
                collider.radius = 0.3f;
                collider.height = 1.8f;
            }

            // Disable the NPC in prefab state (will be enabled when placed)
            npcObj.SetActive(false);

            return npcObj;
        }

        private static void RemoveComponent<T>(GameObject obj) where T : Component
        {
            var comp = obj.GetComponent<T>();
            if (comp != null)
                Object.DestroyImmediate(comp);
        }

        /// <summary>
        /// Apply appearance settings to a placed NPC.
        /// Called after the NPC is configured with its visual settings.
        /// </summary>
        public static void ApplyAppearance(VillageNPC npc)
        {
            if (npc == null) return;

            var visEquip = npc.GetComponent<VisEquipment>();
            if (visEquip == null) return;

            // Set hair and beard from NPC config
            if (!string.IsNullOrEmpty(npc.HairStyle))
                visEquip.SetHairItem(npc.HairStyle);

            if (!string.IsNullOrEmpty(npc.BeardStyle))
                visEquip.SetBeardItem(npc.BeardStyle);
        }
    }
}
