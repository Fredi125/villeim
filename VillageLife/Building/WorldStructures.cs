using System;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;
using VillageLife.Util;

namespace VillageLife.Building
{
    /// <summary>
    /// Registers a curated set of vanilla WORLD structures (abandoned houses, ruined towers) as
    /// buildable Hammer pieces — real buildings, not crafting-table clones. These prefabs ship
    /// without a Piece/ZNetView/icon, so each is cloned and run through <see cref="BuildablePrep"/>
    /// before being wrapped as a CustomPiece. Every one is guarded; a name that won't clone is
    /// logged and skipped.
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

        // Names confirmed to clone into placeable pieces on 0.221.12 (per the in-game log).
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
                    GameObject clone = PrefabManager.Instance.CreateClonedPrefab(d.Prefab + "_VLBuild", d.Prefab);
                    if (clone == null)
                    {
                        skipped++;
                        Jotunn.Logger.LogWarning($"[VillageLife] Structure '{d.Prefab}' didn't resolve; skipped.");
                        continue;
                    }

                    BuildablePrep.Prepare(clone);

                    var config = new PieceConfig
                    {
                        Name = d.DisplayName,
                        Description = d.Description,
                        PieceTable = "Hammer",
                        Category = Constants.BuildCategory,
                        Icon = BuildablePrep.PlaceholderIcon(),
                        Requirements = d.Requirements
                    };

                    var piece = new CustomPiece(clone, false, config);
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

            Jotunn.Logger.LogInfo($"[VillageLife] World structures: {ok} buildable, {skipped} skipped.");
        }
    }
}
