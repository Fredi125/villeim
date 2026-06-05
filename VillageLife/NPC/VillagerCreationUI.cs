using System;
using System.Collections.Generic;
using Jotunn.Managers;
using UnityEngine;
using UnityEngine.UI;

namespace VillageLife.NPC
{
    /// <summary>
    /// A small spawn panel: pick a villager name (re-roll) and a type from a given list, or dismiss a
    /// nearby villager. The Village Hall opens it with the "general" roles; a biome spawner opens it
    /// with that biome's villagers. Built with Jötunn's GUIManager and rebuilt on each open so the
    /// button list can differ per spawner.
    ///
    /// Defensive on purpose (Jötunn GUI signatures can't be verified offline): <see cref="Open"/>
    /// returns false rather than throwing, so a station can fall back to an instant summon; only the
    /// simplest helpers (CreateWoodpanel / CreateButton) are used.
    /// </summary>
    public static class VillagerCreationUI
    {
        private const float DismissRadius = 4f;

        private static GameObject _panel;
        private static Text _nameLabel;
        private static string _name = "Villager";
        private static Vector3 _pos;
        private static Quaternion _rot;

        /// <summary>Open the panel offering the given vendor types. Returns false if it can't be shown.</summary>
        public static bool Open(Vector3 pos, Quaternion rot, List<string> vendorIds)
        {
            try
            {
                if (GUIManager.Instance == null || GUIManager.CustomGUIFront == null)
                    return false;
                if (vendorIds == null || vendorIds.Count == 0)
                    return false;

                _pos = pos;
                _rot = rot;
                _name = NpcSpawner.RandomName();

                // Hide any villager already standing here (recall it by dismissing); split the rest into
                // what's offered now vs. what's shown locked behind a reputation gate.
                var available = new List<string>();
                var locked = new List<string>();
                Partition(vendorIds, pos, available, locked);

                Teardown();           // rebuild fresh so the list matches this spawner + current rep
                Build(available, locked);
                if (_panel == null)
                    return false;

                _panel.SetActive(true);
                GUIManager.BlockInput(true);
                return true;
            }
            catch (Exception e)
            {
                Jotunn.Logger.LogWarning($"[VillageLife] Creation UI couldn't open: {e.Message}");
                Close();
                return false;
            }
        }

        /// <summary>Village Hall entry point: the general roles.</summary>
        public static bool Open(Vector3 pos, Quaternion rot) => Open(pos, rot, GeneralRoleIds());

        /// <summary>Split the menu's vendor ids three ways: a villager already standing within the
        /// dismiss radius is dropped (recall it by dismissing); of the rest, one whose reputation gate
        /// isn't met yet goes to <paramref name="locked"/> (shown greyed), the others to
        /// <paramref name="available"/> (clickable).</summary>
        private static void Partition(List<string> ids, Vector3 pos, List<string> available, List<string> locked)
        {
            HashSet<string> present = NpcSpawner.VendorIdsNear(pos, DismissRadius);
            foreach (string id in ids)
            {
                if (present.Contains(id))
                    continue;
                VendorType v = VendorCatalog.ById(id);
                if (v != null && !v.ReputationMet)
                    locked.Add(id);
                else
                    available.Add(id);
            }
        }

        private static void Build(List<string> available, List<string> locked)
        {
            Transform parent = GUIManager.CustomGUIFront.transform;

            int offered = available.Count + locked.Count;
            bool none = offered == 0;
            const float rowH = 40f;
            // name + (each available + each locked, or a single "all summoned" notice) + dismiss + close
            int rows = (none ? 1 : offered) + 3;
            float height = rows * rowH + 60f;

            _panel = GUIManager.Instance.CreateWoodpanel(
                parent,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0f),
                460f, height, false);
            _panel.SetActive(false);

            float y = (rows - 1) * rowH * 0.5f;

            // Re-rollable name button.
            GameObject nameBtn = Button($"Name: {_name}", y);
            _nameLabel = nameBtn.GetComponentInChildren<Text>();
            nameBtn.GetComponent<Button>().onClick.AddListener(() =>
            {
                _name = NpcSpawner.RandomName();
                if (_nameLabel != null)
                    _nameLabel.text = $"Name: {_name}";
            });
            y -= rowH;

            if (none)
            {
                // Everything this spawner offers is already standing nearby — a no-op notice, so the
                // panel still shows the Dismiss button below to recall them back into the list.
                Button("All summoned — dismiss to recall", y);
                y -= rowH;
            }
            else
            {
                foreach (string id in available)
                {
                    string vid = id;
                    string label = VendorCatalog.ById(vid).Title;
                    Button(label, y).GetComponent<Button>().onClick.AddListener(() => Summon(vid));
                    y -= rowH;
                }
                // Locked entries: visible but greyed and unclickable, so the player can see which
                // villagers reputation will unlock and how much is needed.
                foreach (string id in locked)
                {
                    VendorType v = VendorCatalog.ById(id);
                    LockedButton($"{v.Title}  —  Rep {v.MinReputation} required", y);
                    y -= rowH;
                }
            }

            Button("Dismiss nearby villager", y).GetComponent<Button>().onClick.AddListener(DismissNearby);
            y -= rowH;

            Button("Close", y).GetComponent<Button>().onClick.AddListener(Close);
        }

        /// <summary>A non-interactable, dimmed menu row — a villager whose reputation gate isn't met.</summary>
        private static void LockedButton(string text, float y)
        {
            GameObject go = Button(text, y);
            Button btn = go.GetComponent<Button>();
            if (btn != null)
                btn.interactable = false;
            Text label = go.GetComponentInChildren<Text>();
            if (label != null)
                label.color = new Color(0.6f, 0.6f, 0.6f, 0.9f);
        }

        private static GameObject Button(string text, float y) =>
            GUIManager.Instance.CreateButton(
                text, _panel.transform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, y), 400f, 36f);

        /// <summary>The roles offered by the hall: everything that isn't a biome trader or a bounty.</summary>
        private static List<string> GeneralRoleIds()
        {
            var ids = new List<string>();
            foreach (VendorType v in VendorCatalog.All)
                if (v != null && string.IsNullOrEmpty(v.Biome) && string.IsNullOrEmpty(v.UnlocksVendorId))
                    ids.Add(v.Id);
            return ids;
        }

        private static void Summon(string vendorTypeId)
        {
            try
            {
                var request = new NpcRequest
                {
                    Name = _name,
                    VendorTypeId = vendorTypeId,
                    CreatorId = Player.m_localPlayer != null ? Player.m_localPlayer.GetPlayerID() : 0L
                };
                NpcSpawner.Result r = NpcSpawner.Spawn(_pos, _rot, request);
                if (Player.m_localPlayer != null)
                    Player.m_localPlayer.Message(MessageHud.MessageType.Center,
                        r.Success
                            ? $"{r.Name} the {r.Title} joined your village — \"{r.Greeting}\""
                            : "Could not summon a villager.");
            }
            catch (Exception e)
            {
                Jotunn.Logger.LogWarning($"[VillageLife] Creation summon failed: {e.Message}");
            }
            Close();
        }

        private static void DismissNearby()
        {
            int removed = NpcSpawner.RemoveNear(_pos, DismissRadius);
            if (Player.m_localPlayer != null)
                Player.m_localPlayer.Message(MessageHud.MessageType.Center,
                    removed > 0 ? $"Dismissed {removed} villager(s)." : "No villager nearby to dismiss.");
            Close();
        }

        public static void Close()
        {
            if (_panel != null)
                _panel.SetActive(false);
            try { GUIManager.BlockInput(false); } catch { /* never let closing throw */ }
        }

        private static void Teardown()
        {
            if (_panel == null)
                return;
            try { GUIManager.BlockInput(false); } catch { }
            UnityEngine.Object.Destroy(_panel);
            _panel = null;
            _nameLabel = null;
        }
    }
}
