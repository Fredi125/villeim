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
            public string BasePrefab;   // prefab to clone for the model; null/empty = the workbench.
            public string DisplayName;
            public string Description;
            public string VendorId;     // null = use the rotation (Village Hall); else a specific vendor id.
            public string[] VendorIds;  // when set, the station posts several specific vendors at once (Bounty Board).
            public string[] MenuVendorIds; // when set, [Use] opens the spawn panel to pick from these (biome spawners).
            public bool SpawnInside;    // spawn the villager inside the structure (for building-based posts).
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
                DisplayName = "Meadows Spawner",
                Description = "Press [Use] to choose a Meadows villager.",
                MenuVendorIds = new[] { "meadows", "bounty_meadows" },
                Requirements = Req(
                    ("Wood", 30), ("Stone", 15), ("Resin", 10),
                    ("LeatherScraps", 10), ("Feathers", 10), ("Dandelion", 5)),
            },
            new StationDef
            {
                PrefabName = "VL_Station_BlackForest",
                DisplayName = "Black Forest Spawner",
                Description = "Press [Use] to choose a Black Forest villager.",
                MenuVendorIds = new[] { "blackforest", "bounty_forest" },
                Requirements = Req(
                    ("FineWood", 30), ("RoundLog", 20), ("Coal", 15),
                    ("Copper", 10), ("Tin", 10), ("GreydwarfEye", 10)),
            },
            new StationDef
            {
                PrefabName = "VL_Station_Swamp",
                DisplayName = "Swamp Spawner",
                Description = "Press [Use] to choose a Swamp villager.",
                MenuVendorIds = new[] { "swamp", "bounty_swamp" },
                Requirements = Req(
                    ("ElderBark", 30), ("Iron", 5), ("Guck", 10),
                    ("WitheredBone", 5), ("Bloodbag", 5), ("Entrails", 5)),
            },
            new StationDef
            {
                PrefabName = "VL_Station_Mountain",
                DisplayName = "Mountain Spawner",
                Description = "Press [Use] to choose a Mountain villager.",
                MenuVendorIds = new[] { "mountain", "bounty_mountain" },
                Requirements = Req(
                    ("Stone", 30), ("Obsidian", 10), ("Silver", 5),
                    ("WolfPelt", 5), ("FreezeGland", 5), ("Crystal", 5)),
            },
            new StationDef
            {
                PrefabName = "VL_Station_Plains",
                DisplayName = "Plains Spawner",
                Description = "Press [Use] to choose a Plains villager.",
                MenuVendorIds = new[] { "plains", "bounty_plains" },
                Requirements = Req(
                    ("FineWood", 30), ("BlackMetal", 5), ("Flax", 10),
                    ("Barley", 10), ("Tar", 15), ("LoxPelt", 5)),
            },
            new StationDef
            {
                PrefabName = "VL_Station_GuardPost",
                DisplayName = "Guard Post",
                Description = "Press [Use] to post (or dismiss) a guard who wards off nearby monsters.",
                VendorId = "guard",
                Requirements = Req(
                    ("Wood", 20), ("Stone", 10), ("Bronze", 2)),
            },

            // The Bounty Board posts every bounty-giver at once in a row out front (and its Use toggle
            // clears the whole row), so the turn-in bounties are reachable directly instead of by
            // cycling the Village Hall. Built cheap and early — bounties are how you earn the coins the
            // pricier posts assume you already have.
            new StationDef
            {
                PrefabName = "VL_Station_BountyBoard",
                DisplayName = "Bounty Board",
                Description = "Press [Use] to post (or dismiss) the bounty-givers.",
                VendorId = null,
                VendorIds = new[]
                {
                    "bounty_meadows", "bounty_forest", "bounty_swamp",
                    "bounty_mountain", "bounty_plains",
                },
                Requirements = Req(
                    ("Wood", 20), ("FineWood", 10), ("Coal", 5)),
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
                Category = Constants.BuildCategory,
                Requirements = def.Requirements
            };

            CustomPiece piece;
            if (string.IsNullOrEmpty(def.BasePrefab) || def.BasePrefab == Constants.HallBasePrefab)
            {
                // Workbench-based station (proven path): the base already has Piece, ZNetView and icon.
                piece = new CustomPiece(def.PrefabName, Constants.HallBasePrefab, config);
            }
            else
            {
                // Building-based station: clone a world building and make it placeable first, so the
                // post itself looks like a real structure rather than a workbench.
                GameObject clone = PrefabManager.Instance.CreateClonedPrefab(def.PrefabName, def.BasePrefab);
                if (clone == null)
                {
                    Jotunn.Logger.LogWarning(
                        $"[VillageLife] Station '{def.DisplayName}' base '{def.BasePrefab}' didn't resolve; skipped.");
                    return;
                }
                BuildablePrep.Prepare(clone);
                config.Icon = BuildablePrep.PlaceholderIcon();
                piece = new CustomPiece(clone, false, config);
            }

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
                interaction.Configure(def.DisplayName, def.VendorId, def.VendorIds, def.SpawnInside, def.MenuVendorIds);
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
        // How close a villager counts as "this post's" for the summon/dismiss toggle. Big enough to
        // catch the one we spawn just in front (and any old duplicate pile stacked there), small
        // enough not to grab a neighbouring post's merchant in a tightly-packed trading hub.
        private const float MerchantRadius = 3f;

        // Lift for inside-spawned villagers so they stand on the floor instead of sinking into it
        // (the structure's pivot sits at the foundation, a bit below the floor surface).
        private const float InsideLift = 1f;

        // Set on the prefab at registration; serialized by Unity so placed instances keep them.
        [SerializeField] private string _displayName = "Village Hall";
        [SerializeField] private string _vendorId = "";
        [SerializeField] private string _vendorIds = "";   // CSV; non-empty = a board that posts several at once.
        [SerializeField] private bool _spawnInside;        // spawn the villager inside (building-based posts).
        [SerializeField] private string _menuVendorIds = ""; // CSV; non-empty = [Use] opens the spawn panel for these.

        public void Configure(string displayName, string vendorId, string[] vendorIds = null,
            bool spawnInside = false, string[] menuVendorIds = null)
        {
            _displayName = displayName;
            _vendorId = vendorId ?? "";
            _vendorIds = (vendorIds != null && vendorIds.Length > 0) ? string.Join(",", vendorIds) : "";
            _spawnInside = spawnInside;
            _menuVendorIds = (menuVendorIds != null && menuVendorIds.Length > 0) ? string.Join(",", menuVendorIds) : "";
        }

        public string GetHoverName() => _displayName;

        public string GetHoverText()
        {
            string action;
            if (!string.IsNullOrEmpty(_menuVendorIds))
                action = "Choose a villager";
            else if (!string.IsNullOrEmpty(_vendorIds))
                action = "Post / dismiss bounties";
            else
                action = "Summon / dismiss villager";
            return Localization.instance.Localize(
                $"{_displayName}\n[<color=yellow><b>$KEY_Use</b></color>] {action}");
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

            Vector3 frontCenter;
            if (_spawnInside)
            {
                // Building-based posts (e.g. the Meadows house) put the villager inside, lifted onto
                // the floor rather than sunk into it.
                frontCenter = transform.position + Vector3.up * InsideLift;
            }
            else
            {
                frontCenter = transform.position + transform.forward * distance;
                frontCenter.y = GroundHeight(frontCenter);
            }

            string[] menuIds = SplitIds(_menuVendorIds);
            if (menuIds.Length > 0)
                return OpenMenu(player, frontCenter, menuIds);

            string[] boardIds = SplitIds(_vendorIds);
            return boardIds.Length > 0
                ? ToggleBoard(player, frontCenter, boardIds)
                : ToggleSingle(player, frontCenter);
        }

        /// <summary>Biome spawner: open the panel to choose which villager to summon.</summary>
        private bool OpenMenu(Player player, Vector3 frontCenter, string[] ids)
        {
            Quaternion rotation = Quaternion.LookRotation(-transform.forward);
            if (VillagerCreationUI.Open(frontCenter, rotation, new List<string>(ids)))
                return true;

            // Fallback if the panel can't be shown: summon the first option directly.
            NpcSpawner.Result result = NpcSpawner.Spawn(frontCenter, rotation, NpcSpawner.RequestFor(player, ids[0]));
            player.Message(MessageHud.MessageType.Center,
                result.Success
                    ? $"{result.Name} the {result.Title} joined your village — \"{result.Greeting}\""
                    : "Could not summon a villager.");
            return true;
        }

        /// <summary>Ordinary post: one merchant, summoned on the first Use and dismissed on the next.</summary>
        private bool ToggleSingle(Player player, Vector3 frontCenter)
        {
            int dismissed = NpcSpawner.RemoveNear(frontCenter, MerchantRadius);
            if (dismissed > 0)
            {
                player.Message(MessageHud.MessageType.Center,
                    dismissed == 1
                        ? "The merchant has left your village."
                        : $"Dismissed {dismissed} merchants.");
                return true;
            }

            Quaternion rotation = Quaternion.LookRotation(-transform.forward);

            // The Village Hall (no specific vendor) opens the creation panel to pick name + type; a
            // specific post summons its vendor directly. If the panel can't open (older Jötunn, GUI
            // not ready), fall through to the old instant rotation summon so the hall always works.
            if (string.IsNullOrEmpty(_vendorId) && VillagerCreationUI.Open(frontCenter, rotation))
                return true;

            NpcRequest request = string.IsNullOrEmpty(_vendorId)
                ? NpcSpawner.DefaultRequest(player)
                : NpcSpawner.RequestFor(player, _vendorId);

            NpcSpawner.Result result = NpcSpawner.Spawn(frontCenter, rotation, request);
            player.Message(MessageHud.MessageType.Center,
                result.Success
                    ? $"{result.Name} the {result.Title} joined your village — \"{result.Greeting}\""
                    : "Could not summon a villager.");
            return true;
        }

        /// <summary>Board: posts each listed vendor in a row out front, or clears the whole row.</summary>
        private bool ToggleBoard(Player player, Vector3 frontCenter, string[] ids)
        {
            const float spacing = 1.2f;
            float halfSpan = (ids.Length - 1) * spacing * 0.5f;

            // One sweep clears the entire row (the radius spans it), so a board can't pile up either.
            int dismissed = NpcSpawner.RemoveNear(frontCenter, halfSpan + spacing);
            if (dismissed > 0)
            {
                player.Message(MessageHud.MessageType.Center,
                    $"The bounty board is empty again ({dismissed} dismissed).");
                return true;
            }

            Quaternion rotation = Quaternion.LookRotation(-transform.forward);
            int posted = 0;
            for (int i = 0; i < ids.Length; i++)
            {
                Vector3 pos = frontCenter + transform.right * (i * spacing - halfSpan);
                pos.y = GroundHeight(pos);
                if (NpcSpawner.Spawn(pos, rotation, NpcSpawner.RequestFor(player, ids[i])).Success)
                    posted++;
            }

            player.Message(MessageHud.MessageType.Center,
                posted > 0
                    ? $"{posted} bounty-givers have taken up posts at the board!"
                    : "Could not staff the bounty board.");
            return true;
        }

        private static float GroundHeight(Vector3 p)
            => ZoneSystem.instance != null ? ZoneSystem.instance.GetGroundHeight(p) : p.y;

        private static string[] SplitIds(string csv)
            => string.IsNullOrEmpty(csv) ? new string[0] : csv.Split(',');

        public bool UseItem(Humanoid user, ItemDrop.ItemData item) => false;
    }
}
