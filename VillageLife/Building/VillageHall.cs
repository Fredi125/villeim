using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;
using VillageLife.NPC;
using VillageLife.Plugin;
using VillageLife.Util;

namespace VillageLife.Building
{
    /// <summary>
    /// The Village Hall — a buildable piece cloned from the workbench. Interacting with a
    /// placed hall summons a merchant just in front of it. It lives in the Hammer's built-in
    /// "Misc" tab, which is guaranteed to exist (custom build tabs were a source of past bugs).
    /// </summary>
    public static class VillageHall
    {
        public static void Register()
        {
            var config = new PieceConfig
            {
                Name = "Village Hall",
                Description = "Press [Use] to summon a merchant to your settlement.",
                PieceTable = "Hammer",
                Category = "Misc",
                Requirements = new[]
                {
                    new RequirementConfig { Item = "Wood", Amount = 20, Recover = true },
                    new RequirementConfig { Item = "Stone", Amount = 10, Recover = true }
                }
            };

            var piece = new CustomPiece(Constants.VillageHallPrefabName, Constants.HallBasePrefab, config);
            GameObject prefab = piece.PiecePrefab;
            if (prefab != null)
            {
                // Remove the workbench's crafting-station behaviour so our interaction wins the
                // Use key (and so it doesn't act as a crafting station). WearNTear/Piece/ZNetView
                // are left intact so it builds, renders and saves like any normal structure.
                var station = prefab.GetComponent<CraftingStation>();
                if (station != null)
                    Object.DestroyImmediate(station);

                if (prefab.GetComponent<VillageHallInteraction>() == null)
                    prefab.AddComponent<VillageHallInteraction>();
            }

            PieceManager.Instance.AddPiece(piece);
            Jotunn.Logger.LogInfo("[VillageLife] Village Hall piece registered.");
        }
    }

    /// <summary>Hover text + Use handler on a placed Village Hall: summons a merchant in front of it.</summary>
    public class VillageHallInteraction : MonoBehaviour, Hoverable, Interactable
    {
        public string GetHoverName() => "Village Hall";

        public string GetHoverText()
        {
            return Localization.instance.Localize(
                "Village Hall\n[<color=yellow><b>$KEY_Use</b></color>] Summon a merchant");
        }

        public bool Interact(Humanoid user, bool hold, bool alt)
        {
            if (hold)
                return false;

            var player = user as Player;
            if (player == null)
                return false;

            float distance = VillageLifePlugin.SpawnDistance != null
                ? VillageLifePlugin.SpawnDistance.Value
                : 2.5f;

            Vector3 position = transform.position + transform.forward * distance;
            if (ZoneSystem.instance != null)
                position.y = ZoneSystem.instance.GetGroundHeight(position);

            // Face the new merchant back toward the hall.
            Quaternion rotation = Quaternion.LookRotation(-transform.forward);

            VillageMerchant merchant = NpcSpawner.Spawn(position, rotation, NpcSpawner.DefaultRequest(player));
            player.Message(
                MessageHud.MessageType.Center,
                merchant != null ? $"{merchant.MerchantName} has joined your village!" : "Could not summon a merchant.");

            return true;
        }

        public bool UseItem(Humanoid user, ItemDrop.ItemData item) => false;
    }
}
