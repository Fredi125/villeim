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
    /// buildable Hammer pieces — so you can place real buildings, not just crafting-table clones.
    ///
    /// These world prefabs ship without a <c>Piece</c> (or a usable <c>ZNetView</c>), and Jötunn
    /// validates the Piece while constructing the CustomPiece — so we clone the prefab ourselves,
    /// add those components, strip anything that would spawn creatures or a dungeon, and only then
    /// wrap it as a CustomPiece. Each one is guarded; a name that won't clone is logged and skipped.
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

        // Names confirmed to resolve on 0.221.12 (per the in-game log). Prune/extend from testing.
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
                    // Clone the world prefab ourselves so we can add the components a buildable needs
                    // BEFORE Jötunn validates the CustomPiece (these prefabs have no Piece).
                    GameObject clone = PrefabManager.Instance.CreateClonedPrefab(d.Prefab + "_VLBuild", d.Prefab);
                    if (clone == null)
                    {
                        skipped++;
                        Jotunn.Logger.LogWarning($"[VillageLife] Structure '{d.Prefab}' didn't resolve; skipped.");
                        continue;
                    }

                    PrepareClone(clone);

                    var config = new PieceConfig
                    {
                        Name = d.DisplayName,
                        Description = d.Description,
                        PieceTable = "Hammer",
                        Category = Constants.BuildCategory,
                        Icon = PlaceholderIcon(),
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

        /// <summary>
        /// Turn a cloned world structure into a clean, placeable static piece: strip spawner / dungeon /
        /// AI components, then ensure it has a persistent ZNetView and a Piece (these prefabs ship with
        /// neither, which is exactly why the first attempt was rejected as "no Piece component").
        /// </summary>
        private static void PrepareClone(GameObject go)
        {
            foreach (Component comp in go.GetComponentsInChildren<Component>(true))
            {
                if (comp != null && Array.IndexOf(Dangerous, comp.GetType().Name) >= 0)
                    UnityEngine.Object.DestroyImmediate(comp);
            }

            ZNetView nview = go.GetComponent<ZNetView>();
            if (nview == null)
                nview = go.AddComponent<ZNetView>();
            nview.m_persistent = true;

            Piece piece = go.GetComponent<Piece>();
            if (piece == null)
                piece = go.AddComponent<Piece>();
            if (piece.m_icon == null)
                piece.m_icon = PlaceholderIcon();
        }

        private static Sprite _placeholderIcon;

        /// <summary>
        /// A stand-in build-menu icon borrowed from a vanilla piece (the workbench), so the structures
        /// pass Jötunn's "must have an icon" validation and actually appear in the tab. It's only a
        /// placeholder — real per-building icons can be rendered from each prefab later via Jötunn's
        /// RenderManager. Still entirely vanilla-sourced; no external art.
        /// </summary>
        private static Sprite PlaceholderIcon()
        {
            if (_placeholderIcon != null)
                return _placeholderIcon;

            GameObject src = PrefabManager.Instance.GetPrefab(Constants.HallBasePrefab);
            Piece p = src != null ? src.GetComponent<Piece>() : null;
            _placeholderIcon = p != null ? p.m_icon : null;
            return _placeholderIcon;
        }
    }
}
