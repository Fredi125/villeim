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
    /// Registers all VillageLife buildable stations. Every station is a workbench clone (the proven,
    /// reliable path) placed in the Hammer's built-in "Misc" tab; interacting summons a villager in
    /// front of it. The only thing that varies between stations is which vendor they summon:
    ///   • the <b>Village Hall</b> cycles through all vendor types (general sampler), while
    ///   • each <b>biome station</b> summons that biome's specific vendor.
    /// </summary>
    public static class VillageStations
    {
        private struct StationDef
        {
            public string PrefabName;
            public string DisplayName;
            public string Description;
            public string VendorId;     // null = use the rotation (Village Hall); else a specific vendor id.
            public RequirementConfig[] Requirements;
        }

        private static readonly StationDef[] Stations =
        {
            new StationDef
            {
                PrefabName = Constants.VillageHallPrefabName,
                DisplayName = "Village Hall",
                Description = "Press [Use] to summon a villager (cycles through all types).",
                VendorId = null,
                Requirements = new[]
                {
                    new RequirementConfig { Item = "Wood", Amount = 20, Recover = true },
                    new RequirementConfig { Item = "Stone", Amount = 10, Recover = true },
                }
            },
            BiomeStation("VL_Station_Meadows",     "Meadows Trading Post",      "meadows",
                         "Wood", 20, "Stone", 10),
            BiomeStation("VL_Station_BlackForest",  "Black Forest Trading Post", "blackforest",
                         "FineWood", 20, "Stone", 10),
            BiomeStation("VL_Station_Swamp",        "Swamp Trading Post",        "swamp",
                         "FineWood", 20, "Bronze", 2),
            BiomeStation("VL_Station_Mountain",     "Mountain Trading Post",     "mountain",
                         "Stone", 20, "Iron", 2),
            BiomeStation("VL_Station_Plains",       "Plains Trading Post",       "plains",
                         "FineWood", 20, "BlackMetal", 2),
        };

        private static StationDef BiomeStation(string prefab, string name, string vendorId,
            string item1, int amt1, string item2, int amt2)
        {
            return new StationDef
            {
                PrefabName = prefab,
                DisplayName = name,
                Description = $"Press [Use] to summon the {name} merchant.",
                VendorId = vendorId,
                Requirements = new[]
                {
                    new RequirementConfig { Item = item1, Amount = amt1, Recover = true },
                    new RequirementConfig { Item = item2, Amount = amt2, Recover = true },
                }
            };
        }

        public static void Register()
        {
            foreach (var def in Stations)
                RegisterOne(def);
        }

        private static void RegisterOne(StationDef def)
        {
            var config = new PieceConfig
            {
                Name = def.DisplayName,
                Description = def.Description,
                PieceTable = "Hammer",
                Category = "Misc",
                Requirements = def.Requirements
            };

            var piece = new CustomPiece(def.PrefabName, Constants.HallBasePrefab, config);
            GameObject prefab = piece.PiecePrefab;
            if (prefab != null)
            {
                // Remove the workbench's crafting-station behaviour so our interaction wins the Use
                // key. WearNTear/Piece/ZNetView are left intact so it builds, renders and saves like
                // any normal structure.
                var station = prefab.GetComponent<CraftingStation>();
                if (station != null)
                    Object.DestroyImmediate(station);

                var interaction = prefab.GetComponent<StationInteraction>();
                if (interaction == null)
                    interaction = prefab.AddComponent<StationInteraction>();
                interaction.Configure(def.DisplayName, def.VendorId);
            }

            PieceManager.Instance.AddPiece(piece);
            Jotunn.Logger.LogInfo($"[VillageLife] Station '{def.DisplayName}' registered.");
        }
    }

    /// <summary>
    /// Hover text + Use handler on a placed station. Summons a villager in front of it — a specific
    /// vendor if <see cref="_vendorId"/> is set, otherwise the next type in the rotation.
    /// </summary>
    public class StationInteraction : MonoBehaviour, Hoverable, Interactable
    {
        // Set on the prefab at registration; serialized by Unity so placed instances keep it.
        [SerializeField] private string _displayName = "Village Hall";
        [SerializeField] private string _vendorId = "";

        public void Configure(string displayName, string vendorId)
        {
            _displayName = displayName;
            _vendorId = vendorId ?? "";
        }

        public string GetHoverName() => _displayName;

        public string GetHoverText()
        {
            return Localization.instance.Localize(
                $"{_displayName}\n[<color=yellow><b>$KEY_Use</b></color>] Summon a merchant");
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

            // Face the new merchant back toward the station.
            Quaternion rotation = Quaternion.LookRotation(-transform.forward);

            NpcRequest request = string.IsNullOrEmpty(_vendorId)
                ? NpcSpawner.DefaultRequest(player)
                : NpcSpawner.RequestFor(player, _vendorId);

            NpcSpawner.Result result = NpcSpawner.Spawn(position, rotation, request);
            player.Message(MessageHud.MessageType.Center,
                result.Success
                    ? $"{result.Name} the {result.Title} has joined your village!"
                    : "Could not summon a villager.");

            return true;
        }

        public bool UseItem(Humanoid user, ItemDrop.ItemData item) => false;
    }
}
