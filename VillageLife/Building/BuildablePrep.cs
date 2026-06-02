using System;
using Jotunn.Managers;
using UnityEngine;
using VillageLife.Util;

namespace VillageLife.Building
{
    /// <summary>
    /// Shared helper for turning a cloned vanilla world prefab into a placeable build piece: strips
    /// spawner / dungeon / AI components, ensures a persistent ZNetView and a Piece, and borrows a
    /// vanilla icon so it passes Jötunn's validation. Used both for standalone world structures and
    /// for building-based stations (a post that looks like a real house instead of a workbench).
    /// </summary>
    public static class BuildablePrep
    {
        // Component type names (matched by reflection, so no compile dependency on them) that must
        // be removed from a cloned world prefab: spawners/dungeon generators (would spawn creatures),
        // and MusicLocation (a location-only component that throws a NullReferenceException every
        // frame once the piece is detached from a real world Location).
        private static readonly string[] Dangerous =
        {
            "CreatureSpawner", "SpawnArea", "SpawnSystem", "DungeonGenerator",
            "FishSpawner", "MonsterAI", "AnimalAI", "BaseAI", "Tameable", "Character",
            "MusicLocation", "LocationProxy"
        };

        private static Sprite _placeholderIcon;

        /// <summary>Strip dangerous components and ensure the clone has the bits a buildable needs.</summary>
        public static void Prepare(GameObject go)
        {
            if (go == null)
                return;

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

        /// <summary>
        /// A stand-in build-menu icon borrowed from a vanilla piece (the workbench), so prefabs that
        /// ship without one still pass Jötunn's "must have an icon" check. Vanilla-sourced; real
        /// per-prefab icons can be rendered later via Jötunn's RenderManager.
        /// </summary>
        public static Sprite PlaceholderIcon()
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
