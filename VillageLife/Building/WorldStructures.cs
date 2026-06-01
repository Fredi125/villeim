using System;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace VillageLife.Building
{
    /// <summary>
    /// Registers a curated set of vanilla WORLD structures (abandoned houses, ruined towers) as
    /// buildable Hammer pieces — so you can place real buildings, not just crafting-table clones.
    ///
    /// Deliberately experimental and fully guarded, because the exact prefab names and whether a
    /// given structure clones into a clean piece can't be verified offline:
    ///   • each prefab is cloned in its own try/catch — a name that doesn't resolve, or a structure
    ///     that won't clone, is logged and skipped, never fatal;
    ///   • spawner / dungeon / AI components are stripped so a placed structure is inert scenery
    ///     rather than a monster nest or a dungeon entrance;
    ///   • a summary line reports how many registered, so the BepInEx log is the ground truth for
    ///     which structure names are valid on this game version. Edit <see cref="Structures"/> from
    ///     what the log shows.
    /// </summary>
    public static class WorldStructures
    {
        private struct Def
        {
            public string Prefab;
            public string DisplayName;
            public string Description;
            public RequirementConfig[] Requirements;
        }

        // Component type names (matched by reflection, so no compile dependency on them) that would
        // make a placed "building" spawn creatures or generate a dungeon. Stripped on registration.
        private static readonly string[] Dangerous =
        {
            "CreatureSpawner", "SpawnArea", "SpawnSystem", "DungeonGenerator",
            "FishSpawner", "MonsterAI", "AnimalAI", "BaseAI", "Tameable", "Character"
        };

        private static Def Wood(string prefab, string name) => new Def
        {
            Prefab = prefab,
            DisplayName = name,
            Description = "A reclaimed wooden structure.",
            Requirements = new[] { new RequirementConfig { Item = "Wood", Amount = 20, Recover = true } }
        };

        private static Def Stone(string prefab, string name) => new Def
        {
            Prefab = prefab,
            DisplayName = name,
            Description = "A reclaimed stone structure.",
            Requirements = new[] { new RequirementConfig { Item = "Stone", Amount = 20, Recover = true } }
        };

        // First pass: names confirmed to exist as Meadows/Mountain world prefabs. The log will tell
        // us which actually clone into placeable pieces; we prune/extend from there.
        private static Def[] Structures => new[]
        {
            Wood("WoodHouse1", "Old Wooden House I"),
            Wood("WoodHouse2", "Old Wooden House II"),
            Wood("WoodHouse3", "Old Wooden House III"),
            Wood("WoodHouse4", "Old Wooden House IV"),
            Wood("WoodHouse5", "Old Wooden House V"),
            Wood("WoodHouse6", "Old Wooden House VI"),
            Stone("StoneTowerRuins04", "Ruined Stone Tower I"),
            Stone("StoneTowerRuins05", "Ruined Stone Tower II"),
        };

        public static void Register()
        {
            int ok = 0, skipped = 0;
            foreach (Def d in Structures)
            {
                try
                {
                    var config = new PieceConfig
                    {
                        Name = d.DisplayName,
                        Description = d.Description,
                        PieceTable = "Hammer",
                        Category = "Misc",
                        Requirements = d.Requirements
                    };

                    var piece = new CustomPiece(d.Prefab + "_VLBuild", d.Prefab, config);
                    GameObject prefab = piece.PiecePrefab;
                    if (prefab == null)
                    {
                        skipped++;
                        Jotunn.Logger.LogWarning($"[VillageLife] Structure '{d.Prefab}' didn't resolve; skipped.");
                        continue;
                    }

                    Neutralize(prefab);
                    EnsureBuildable(prefab);
                    PieceManager.Instance.AddPiece(piece);
                    ok++;
                    Jotunn.Logger.LogInfo($"[VillageLife] Structure '{d.DisplayName}' ({d.Prefab}) registered.");
                }
                catch (Exception e)
                {
                    skipped++;
                    Jotunn.Logger.LogWarning($"[VillageLife] Structure '{d.Prefab}' failed: {e.Message}");
                }
            }

            Jotunn.Logger.LogInfo(
                $"[VillageLife] World structures: {ok} buildable, {skipped} skipped (see warnings for the names).");
        }

        /// <summary>Remove components that would make a placed structure spawn enemies or a dungeon.</summary>
        private static void Neutralize(GameObject go)
        {
            foreach (Component comp in go.GetComponentsInChildren<Component>(true))
            {
                if (comp == null)
                    continue;
                if (Array.IndexOf(Dangerous, comp.GetType().Name) >= 0)
                    UnityEngine.Object.DestroyImmediate(comp);
            }
        }

        /// <summary>Best-effort: ensure the clone has a Piece so the Hammer can place it.</summary>
        private static void EnsureBuildable(GameObject go)
        {
            if (go.GetComponent<Piece>() == null)
                go.AddComponent<Piece>();
        }
    }
}
