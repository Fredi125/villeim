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
    /// Registers all VillageLife buildable stations, grouped in the Hammer's "VillageLife" tab.
    /// Each is a clone of a vanilla build piece (the proven, reliable path): the Village Hall is a
    /// maypole, the Guard Post an armor stand, the Bounty Board a workbench, and each biome spawner a
    /// different seat (a cauldron for the Meadows, chairs and thrones for the rest) — purely for a
    /// distinct look. Interacting summons a villager in front of it; the only behavioural difference
    /// is which vendor(s) a station offers:
    ///   • the <b>Village Hall</b> cycles through all vendor types (general sampler),
    ///   • each <b>biome spawner</b> opens a menu to pick from that biome's villagers, and
    ///   • the <b>Bounty Board</b> posts every bounty-giver at once.
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

        // Vanilla build pieces we can clone by name for a station's model: each already carries a
        // Piece, ZNetView and its own icon (like the workbench), so no BuildablePrep is needed — we
        // strip its crafting/seat interaction below so our own Use handler wins. Biome spawners each
        // pick a different seat purely for a distinct look (a cauldron for the Meadows, chairs and
        // thrones for the rest). Anything NOT in this set is a world structure (BuildablePrep path).
        private static readonly HashSet<string> NativeStationBases = new HashSet<string>
        {
            "piece_workbench", "piece_cauldron", "piece_maypole", "ArmorStand",
            "piece_chair", "piece_bone_throne", "piece_throne01", "piece_blackmarble_throne",
        };

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
                BasePrefab = "piece_maypole",
                DisplayName = "Village Hall",
                Description = "Press [Use] to summon a villager (cycles through all types).",
                VendorId = null,
                Requirements = Req(
                    ("Wood", 20), ("Stone", 10)),
            },
            new StationDef
            {
                PrefabName = "VL_Station_Meadows",
                BasePrefab = "piece_cauldron",
                DisplayName = "Meadows Spawner",
                Description = "Press [Use] to choose a Meadows villager.",
                MenuVendorIds = new[]
                {
                    "meadows", "meadows_forage", "meadows_hunt",
                    "bounty_meadows", "meadows_tanner", "meadows_beekeeper",
                },
                Requirements = Req(
                    ("Wood", 30), ("Stone", 15), ("Resin", 10),
                    ("LeatherScraps", 10), ("Feathers", 10), ("Dandelion", 5)),
            },
            new StationDef
            {
                PrefabName = "VL_Station_BlackForest",
                BasePrefab = "piece_chair",
                DisplayName = "Black Forest Spawner",
                Description = "Press [Use] to choose a Black Forest villager.",
                MenuVendorIds = new[]
                {
                    "blackforest", "blackforest_miner", "blackforest_carpenter",
                    "bounty_forest", "blackforest_charcoal", "blackforest_smelter",
                },
                Requirements = Req(
                    ("FineWood", 30), ("RoundLog", 20), ("Coal", 15),
                    ("Copper", 10), ("Tin", 10), ("GreydwarfEye", 10)),
            },
            new StationDef
            {
                PrefabName = "VL_Station_Swamp",
                BasePrefab = "piece_bone_throne",
                DisplayName = "Swamp Spawner",
                Description = "Press [Use] to choose a Swamp villager.",
                MenuVendorIds = new[]
                {
                    "swamp", "swamp_alchemist", "swamp_digger",
                    "bounty_swamp", "swamp_grinder", "swamp_renderer",
                },
                Requirements = Req(
                    ("ElderBark", 30), ("Iron", 5), ("Guck", 10),
                    ("WitheredBone", 5), ("Bloodbag", 5), ("Entrails", 5)),
            },
            new StationDef
            {
                PrefabName = "VL_Station_Mountain",
                BasePrefab = "piece_throne01",
                DisplayName = "Mountain Spawner",
                Description = "Press [Use] to choose a Mountain villager.",
                MenuVendorIds = new[]
                {
                    "mountain", "mountain_miner", "mountain_herbalist",
                    "bounty_mountain", "mountain_furrier", "mountain_jeweler",
                },
                Requirements = Req(
                    ("Stone", 30), ("Obsidian", 10), ("Silver", 5),
                    ("WolfPelt", 5), ("FreezeGland", 5), ("Crystal", 5)),
            },
            new StationDef
            {
                PrefabName = "VL_Station_Plains",
                BasePrefab = "piece_blackmarble_throne",
                DisplayName = "Plains Spawner",
                Description = "Press [Use] to choose a Plains villager.",
                MenuVendorIds = new[]
                {
                    "plains", "plains_farmer", "plains_smith",
                    "bounty_plains", "plains_weaver", "plains_rancher",
                },
                Requirements = Req(
                    ("FineWood", 30), ("BlackMetal", 5), ("Flax", 10),
                    ("Barley", 10), ("Tar", 15), ("LoxPelt", 5)),
            },
            new StationDef
            {
                PrefabName = "VL_Station_GuardPost",
                BasePrefab = "ArmorStand",
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
                    "bounty_resin", "bounty_meadows", "bounty_forest",
                    "bounty_swamp", "bounty_mountain", "bounty_plains",
                },
                Requirements = Req(
                    ("Wood", 20), ("FineWood", 10), ("Coal", 5)),
            },

            // Quest Board — posts the multi-item quest-givers (the proof of concept for deeper
            // quests). Like the Bounty Board, its Use toggles the whole row on/off.
            new StationDef
            {
                PrefabName = "VL_Station_QuestBoard",
                DisplayName = "Quest Board",
                Description = "Press [Use] to post (or dismiss) the quest-givers.",
                VendorId = null,
                VendorIds = new[] { "quest_provisions", "quest_smith" },
                Requirements = Req(
                    ("Wood", 20), ("FineWood", 10), ("BronzeNails", 5)),
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
            string baseName = string.IsNullOrEmpty(def.BasePrefab) ? Constants.HallBasePrefab : def.BasePrefab;
            if (NativeStationBases.Contains(baseName))
            {
                // Native build piece (workbench or a themed crafting station): clone by name so it
                // keeps its own model, icon, Piece and ZNetView. The CraftingStation is stripped below.
                // If a themed base unexpectedly doesn't resolve, fall back to the workbench so the
                // spawner still appears in the build menu rather than silently vanishing.
                if (baseName != Constants.HallBasePrefab && PrefabManager.Instance.GetPrefab(baseName) == null)
                {
                    Jotunn.Logger.LogWarning(
                        $"[VillageLife] Station '{def.DisplayName}' base '{baseName}' not found; using the workbench model.");
                    baseName = Constants.HallBasePrefab;
                }
                piece = new CustomPiece(def.PrefabName, baseName, config);
            }
            else
            {
                // Building-based station: clone a world building and make it placeable first, so the
                // post itself looks like a real structure rather than a workbench.
                GameObject clone = PrefabManager.Instance.CreateClonedPrefab(def.PrefabName, def.BasePrefab);
                if (clone == null)
                {
                    // Don't drop the piece — fall back to the workbench so the station still exists
                    // (and stays usable) rather than vanishing from the build menu.
                    Jotunn.Logger.LogWarning(
                        $"[VillageLife] Station '{def.DisplayName}' base '{def.BasePrefab}' didn't resolve; " +
                        "using the workbench model.");
                    piece = new CustomPiece(def.PrefabName, Constants.HallBasePrefab, config);
                }
                else
                {
                    BuildablePrep.Prepare(clone);
                    config.Icon = BuildablePrep.PlaceholderIcon();
                    piece = new CustomPiece(clone, false, config);
                }
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

                // The model base may also carry its own Hoverable/Interactable anywhere in its
                // hierarchy — a chair's "sit" prompt, an armor stand's equip slots — and sometimes on a
                // CHILD object (which is why the armor stand kept swallowing the Use key). Remove every
                // one except ours, so our StationInteraction alone answers hover and the Use key.
                foreach (var mb in prefab.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (mb == null || mb is StationInteraction)
                        continue;
                    if (mb is Hoverable || mb is Interactable)
                        Object.DestroyImmediate(mb);
                }

                // Drop any seasonal lock (e.g. the maypole's midsummer restriction) by type name.
                var seasonal = prefab.GetComponent("SeasonalItem");
                if (seasonal != null)
                    Object.DestroyImmediate(seasonal);

                // Build with just the Hammer (no nearby workbench/forge required), matching the
                // workbench-based stations — the themed model shouldn't change how it's placed.
                var pieceComp = prefab.GetComponent<Piece>();
                if (pieceComp != null)
                    pieceComp.m_craftingStation = null;

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
                action = "Post / dismiss villagers";
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

            Vector3 frontCenter;
            if (_spawnInside)
            {
                // Building-based posts put the villager inside, lifted onto the floor.
                frontCenter = transform.position + Vector3.up * InsideLift;
            }
            else
            {
                // Spawn where the player is standing — villagers appear at your feet, so you can walk
                // around the building and summon a few to arrange them. SpawnDistance optionally nudges
                // them forward (0 = exactly at your feet). You must be close enough to Use the station,
                // so they always end up near it.
                float offset = VillageLifePlugin.SpawnDistance != null ? VillageLifePlugin.SpawnDistance.Value : 0f;
                Vector3 fwd = player.transform.forward;
                fwd.y = 0f;
                frontCenter = player.transform.position;
                if (offset != 0f && fwd.sqrMagnitude > 0.0001f)
                    frontCenter += fwd.normalized * offset;
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
