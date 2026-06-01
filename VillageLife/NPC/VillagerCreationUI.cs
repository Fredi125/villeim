using System;
using System.Collections.Generic;
using Jotunn.Managers;
using UnityEngine;
using UnityEngine.UI;

namespace VillageLife.NPC
{
    /// <summary>
    /// A small panel for the Village Hall: pick a villager's name (re-roll button) and type (one
    /// button per general role), then summon. Built with Jötunn's GUIManager.
    ///
    /// IMPORTANT — this is the highest-risk piece in the mod: it depends on Jötunn's GUI helper
    /// signatures, which couldn't be verified offline. It is therefore written defensively:
    ///   • <see cref="Open"/> returns false (rather than throwing) on any problem, so the Village
    ///     Hall can fall back to its old instant summon and always works;
    ///   • only the simplest helpers (CreateWoodpanel / CreateButton) are used — no input fields or
    ///     dropdowns — and the panel is built once, then reused.
    /// If anything here is wrong it affects only this feature; revert this one commit.
    /// </summary>
    public static class VillagerCreationUI
    {
        private static GameObject _panel;
        private static Text _nameLabel;
        private static string _name = "Villager";
        private static Vector3 _pos;
        private static Quaternion _rot;

        /// <summary>Open the panel for a summon at the given spot. Returns false if it can't be shown.</summary>
        public static bool Open(Vector3 pos, Quaternion rot)
        {
            try
            {
                if (GUIManager.Instance == null || GUIManager.CustomGUIFront == null)
                    return false;

                _pos = pos;
                _rot = rot;
                _name = NpcSpawner.RandomName();

                if (_panel == null)
                    Build();
                if (_panel == null)
                    return false;

                if (_nameLabel != null)
                    _nameLabel.text = $"Name: {_name}";

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

        private static void Build()
        {
            Transform parent = GUIManager.CustomGUIFront.transform;

            _panel = GUIManager.Instance.CreateWoodpanel(
                parent,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0f),
                440f, 540f, false);
            _panel.SetActive(false);

            // Re-rollable name button.
            GameObject nameBtn = GUIManager.Instance.CreateButton(
                $"Name: {_name}", _panel.transform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 215f), 380f, 38f);
            _nameLabel = nameBtn.GetComponentInChildren<Text>();
            nameBtn.GetComponent<Button>().onClick.AddListener(() =>
            {
                _name = NpcSpawner.RandomName();
                if (_nameLabel != null)
                    _nameLabel.text = $"Name: {_name}";
            });

            // One button per general role (biome traders, bounties and the like keep their stations).
            float y = 165f;
            foreach (VendorType type in GeneralRoles())
            {
                string id = type.Id;
                GameObject btn = GUIManager.Instance.CreateButton(
                    type.Title, _panel.transform,
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, y), 380f, 36f);
                btn.GetComponent<Button>().onClick.AddListener(() => Summon(id));
                y -= 40f;
            }

            GameObject close = GUIManager.Instance.CreateButton(
                "Close", _panel.transform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, y - 8f), 380f, 36f);
            close.GetComponent<Button>().onClick.AddListener(Close);
        }

        /// <summary>The roles offered by the hall: everything that isn't a biome trader or a bounty.</summary>
        private static IEnumerable<VendorType> GeneralRoles()
        {
            foreach (VendorType v in VendorCatalog.All)
                if (v != null && string.IsNullOrEmpty(v.Biome) && string.IsNullOrEmpty(v.UnlocksVendorId))
                    yield return v;
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

        public static void Close()
        {
            if (_panel != null)
                _panel.SetActive(false);
            try { GUIManager.BlockInput(false); } catch { /* never let closing throw */ }
        }
    }
}
