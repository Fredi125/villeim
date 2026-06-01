using System.Collections.Generic;
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

        // Each biome station's build cost is a spread of materials from that biome (per the design:
        // "the building's resource requirements will have many resources from that biome"). Prefab
        // names are standard vanilla items; if one can't be resolved at runtime, Jötunn skips that
        // single requirement (logged) rather than failing the piece — fix the name if a station
        // ends up cheaper than intended.
        private static readonly StationDef[] Stations =
        {
            new StationDef
            {
                PrefabName = Constants.VillageHallPrefabName,
                DisplayName = "Village Hall",
                Description = "Press [Use] to summon a villager (cycles through all types).",
                VendorId = null,
                Requirements = Req(
                    ("Wood", 20), ("Stone", 10)),
            },
            new StationDef
            {
                PrefabName = "VL_Station_Meadows",
                DisplayName = "Meadows Trading Post",
                Description = "Press [Use] to summon the Meadows merchant.",
                VendorId = "meadows",
                Requirements = Req(
                    ("Wood", 30), ("Stone", 15), ("Resin", 10),
                    ("LeatherScraps", 10), ("Feathers", 10), ("Dandelion", 5)),
            },
            new StationDef
            {
                PrefabName = "VL_Station_BlackForest",
                DisplayName = "Black Forest Trading Post",
                Description = "Press [Use] to summon the Black Forest merchant.",
                VendorId = "blackforest",
                Requirements = Req(
                    ("FineWood", 30), ("RoundLog", 20), ("Coal", 15),
                    ("Copper", 10), ("Tin", 10), ("GreydwarfEye", 10)),
            },
            new StationDef
            {
                PrefabName = "VL_Station_Swamp",
                DisplayName = "Swamp Trading Post",
                Description = "Press [Use] to summon the Swamp merchant.",
                VendorId = "swamp",
                Requirements = Req(
                    ("ElderBark", 30), ("Iron", 5), ("Guck", 10),
                    ("WitheredBone", 5), ("Bloodbag", 5), ("Entrails", 5)),
            },
            new StationDef
            {
                PrefabName = "VL_Station_Mountain",
                DisplayName = "Mountain Trading Post",
                Description = "Press [Use] to summon the Mountain merchant.",
                VendorId = "mountain",
                Requirements = Req(
                    ("Stone", 30), ("Obsidian", 10), ("Silver", 5),
                    ("WolfPelt", 5), ("FreezeGland", 5), ("Crystal", 5)),
            },
            new StationDef
            {
                PrefabName = "VL_Station_Plains",
                DisplayName = "Plains Trading Post",
                Description = "Press [Use] to summon the Plains merchant.",
                VendorId = "plains",
                Requirements = Req(
                    ("FineWood", 30), ("BlackMetal", 5), ("Flax", 10),
                    ("Barley", 10), ("Tar", 15), ("LoxPelt", 5)),
            },
        };

        /// <summary>Concise builder for a recovery-on-deconstruct requirement list.</summary>
        private static RequirementConfig[] Req(params (string item, int amount)[] items)
        {
            var reqs = new RequirementConfig[items.Length];
            for (int i = 0; i < items.Length; i++)
                reqs[i] = new RequirementConfig { Item = items[i].item, Amount = items[i].amount, Recover = true };
            return reqs;
        }

        /// <summary>
        /// Every (station display name, requirement item) pair referenced by the build costs, for
        /// the startup <see cref="VillageLife.NPC.ItemNameAudit"/>. Lets the audit surface a mistyped
        /// requirement, which Jötunn would otherwise drop silently.
        /// </summary>
        public static IEnumerable<(string station, string item)> RequirementItems()
        {
            foreach (StationDef s in Stations)
            {
                if (s.Requirements == null)
                    continue;
                foreach (RequirementConfig r in s.Requirements)
                    yield return (s.DisplayName, r.Item);
            }
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
