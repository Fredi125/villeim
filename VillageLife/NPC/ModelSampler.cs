using System.Collections.Generic;
using UnityEngine;

namespace VillageLife.NPC
{
    /// <summary>
    /// A survey tool. Press Use to spawn a curated set of humanoid NPC models in a row in front of it,
    /// with their wander/combat AI stripped so they just stand there (and made non-persistent, so they
    /// don't save). Each floats its prefab name as a label (and is renamed, so hovering also shows it)
    /// to drop into the <c>VillagerBasePrefab</c> config or a vendor's <c>Model</c>; the full
    /// left-to-right list is logged too. Press Use again to clear the row.
    ///
    /// Defensive throughout: each spawn is wrapped (a model that won't instantiate is logged and
    /// skipped), and no `using System;` here on purpose, so bare <c>Object</c> stays UnityEngine's.
    /// </summary>
    public class ModelSampler : MonoBehaviour, Hoverable, Interactable
    {
        private const float Spacing = 1.5f;
        private const int MaxModels = 60;

        private static readonly string[] AiComponents =
            { "MonsterAI", "AnimalAI", "BaseAI", "NpcTalk", "Tameable", "CharacterDrop", "Growup", "Procreation" };

        private readonly List<GameObject> _spawned = new List<GameObject>();

        public string GetHoverName() => "Model Sampler";

        public string GetHoverText() => Localization.instance.Localize(
            "Model Sampler\n[<color=yellow><b>$KEY_Use</b></color>] Show / clear the named model lineup");

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
                    ? $"Spawned {n} models — walk the line; each shows its prefab name."
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
            if (spawned > 0)
                InvokeRepeating(nameof(RelabelTick), 0.5f, 5f);
            return spawned;
        }

        /// <summary>Re-show each nearby model's prefab name as a floating label, so the whole lineup is
        /// readable without hovering. Low-frequency, proximity-gated, and visual-only.</summary>
        private void RelabelTick()
        {
            Player p = Player.m_localPlayer;
            if (p == null)
                return;
            foreach (GameObject go in _spawned)
            {
                if (go == null || Vector3.Distance(p.transform.position, go.transform.position) > 30f)
                    continue;
                VillagerChatter.Announce(go, go.name);
            }
        }

        private int Clear()
        {
            CancelInvoke(nameof(RelabelTick));
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

        // A curated allowlist of bipedal, person-/creature-shaped NPCs that render cleanly as a
        // standing model and make plausible villagers. Spawning *every* humanoid also dragged in
        // bosses, serpents, blobs and Gjall — visually broken and the source of looping errors — so we
        // pick from a known-good set instead. Names that don't exist on the running build are simply
        // skipped, so this stays safe to extend; tell me which to add or drop.
        private static readonly string[] Candidates =
        {
            // Traders — the cleanest fits
            "Haldor", "Hildir",
            // Black Forest
            "Greyling", "Greydwarf", "Greydwarf_Elite", "Greydwarf_Shaman", "Skeleton", "Skeleton_Poison",
            // Swamp
            "Draugr", "Draugr_Elite", "Draugr_Ranged", "Wraith",
            // Mountain
            "Fenring", "Fenring_Cultist", "Ulv", "Cultist",
            // Plains
            "Goblin", "GoblinShaman", "GoblinBrute",
            // Mistlands
            "Dverger", "DvergerMage", "DvergerMageFire", "DvergerMageIce", "DvergerMageSupport",
            "Seeker", "SeekerBrute",
            // Ashlands
            "Charred_Melee", "Charred_Archer", "Charred_Mage", "Charred_Twitcher",
            // Misc
            "Troll",
        };

        /// <summary>The allowlisted models actually present on this build (humanoid, not a boss).</summary>
        private static List<string> ModelNames(ZNetScene zs)
        {
            var names = new List<string>();
            foreach (string name in Candidates)
            {
                GameObject p = zs.GetPrefab(name);
                if (p == null || p.GetComponent("Humanoid") == null)
                    continue;
                Character character = p.GetComponent<Character>();
                if (character != null && character.m_boss)
                    continue;
                names.Add(name);
            }
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
