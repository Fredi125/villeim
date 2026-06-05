using System.Collections.Generic;
using UnityEngine;

namespace VillageLife.NPC
{
    /// <summary>
    /// A survey tool. Press Use to spawn one of every humanoid NPC model in a row in front of it, with
    /// their wander/combat AI stripped so they just stand there (and made non-persistent, so they
    /// don't save). Each is renamed to its prefab, so hovering one shows the name to drop into the
    /// <c>VillagerBasePrefab</c> config or a vendor's <c>Model</c>; the full left-to-right list is also
    /// written to the BepInEx log. Press Use again to clear the row.
    ///
    /// Defensive throughout: each spawn is wrapped (a model that won't instantiate is logged and
    /// skipped), and no `using System;` here on purpose, so bare <c>Object</c> stays UnityEngine's.
    /// </summary>
    public class ModelSampler : MonoBehaviour, Hoverable, Interactable
    {
        private const float Spacing = 1.5f;
        private const int MaxModels = 60;

        private static readonly string[] AiComponents =
            { "MonsterAI", "AnimalAI", "BaseAI", "Tameable", "CharacterDrop", "Growup", "Procreation" };

        private readonly List<GameObject> _spawned = new List<GameObject>();

        public string GetHoverName() => "Model Sampler";

        public string GetHoverText() => Localization.instance.Localize(
            "Model Sampler\n[<color=yellow><b>$KEY_Use</b></color>] Show / clear every NPC model");

        public bool Interact(Humanoid user, bool hold, bool alt)
        {
            if (hold)
                return false;
            var player = user as Player;
            if (player == null)
                return false;

            if (_spawned.Count > 0)
            {
                int cleared = Clear();
                player.Message(MessageHud.MessageType.Center, $"Cleared {cleared} models.");
                return true;
            }

            int n = Spawn();
            player.Message(MessageHud.MessageType.Center,
                n > 0
                    ? $"Spawned {n} models — walk the line and hover each to read its prefab name."
                    : "No models found to spawn.");
            return true;
        }

        public bool UseItem(Humanoid user, ItemDrop.ItemData item) => false;

        private void OnDestroy()
        {
            try { Clear(); } catch { /* world teardown — ignore */ }
        }

        private int Spawn()
        {
            ZNetScene zs = ZNetScene.instance;
            if (zs == null || zs.m_prefabs == null)
                return 0;

            List<string> names = ModelNames(zs);
            if (names.Count == 0)
                return 0;

            Vector3 right = transform.right;
            Vector3 fwd = transform.forward;
            Vector3 start = transform.position + fwd * 3f - right * (Spacing * (names.Count - 1) * 0.5f);
            Quaternion rot = Quaternion.LookRotation(-fwd);

            int spawned = 0;
            for (int i = 0; i < names.Count; i++)
            {
                GameObject prefab = zs.GetPrefab(names[i]);
                if (prefab == null)
                    continue;

                Vector3 pos = start + right * (i * Spacing);
                pos.y = GroundHeight(pos);
                try
                {
                    GameObject go = Object.Instantiate(prefab, pos, rot);
                    StripAi(go);                              // remove wander/combat so it stands still
                    var nview = go.GetComponent<ZNetView>();
                    if (nview != null)
                        nview.m_persistent = false;           // a survey, not something to save
                    var character = go.GetComponent<Character>();
                    if (character != null)
                        character.m_name = names[i];          // hover shows the prefab name
                    go.name = names[i];
                    _spawned.Add(go);
                    spawned++;
                }
                catch (System.Exception e)
                {
                    Jotunn.Logger.LogWarning($"[VillageLife] Model Sampler couldn't spawn '{names[i]}': {e.Message}");
                }
            }

            Jotunn.Logger.LogInfo("[VillageLife] Model Sampler line (left to right): " + string.Join(", ", names));
            return spawned;
        }

        private int Clear()
        {
            int cleared = 0;
            ZNetScene zs = ZNetScene.instance;
            foreach (GameObject go in _spawned)
            {
                if (go == null)
                    continue;
                var nview = go.GetComponent<ZNetView>();
                if (zs != null && nview != null && nview.IsValid())
                {
                    nview.ClaimOwnership();
                    zs.Destroy(go);
                }
                else
                {
                    Object.Destroy(go);
                }
                cleared++;
            }
            _spawned.Clear();
            return cleared;
        }

        /// <summary>Names of humanoid NPC prefabs worth previewing (skips ragdolls, effects, spawners).</summary>
        private static List<string> ModelNames(ZNetScene zs)
        {
            var names = new List<string>();
            foreach (GameObject p in zs.m_prefabs)
            {
                if (p == null || p.GetComponent("Humanoid") == null)
                    continue;
                string lower = p.name.ToLowerInvariant();
                if (lower.Contains("ragdoll") || lower.Contains("_attack") ||
                    lower.StartsWith("fx_") || lower.StartsWith("vfx_") || lower.StartsWith("sfx_") ||
                    lower.StartsWith("spawner_"))
                    continue;
                names.Add(p.name);
            }
            names.Sort();
            if (names.Count > MaxModels)
                names = names.GetRange(0, MaxModels);
            return names;
        }

        private static void StripAi(GameObject go)
        {
            foreach (string comp in AiComponents)
            {
                Component c = go.GetComponent(comp);
                if (c != null)
                    Object.DestroyImmediate(c);
            }
        }

        private static float GroundHeight(Vector3 p)
            => ZoneSystem.instance != null ? ZoneSystem.instance.GetGroundHeight(p) : p.y;
    }
}
