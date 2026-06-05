using UnityEngine;
using VillageLife.Util;

namespace VillageLife.NPC
{
    /// <summary>
    /// Cosmetic per-villager variation so a row of villagers isn't a row of identical Haldors:
    ///   • a subtle height/build difference, and
    ///   • a gentle clothing-colour tint.
    /// Both are chosen once at spawn and stored in the ZDO, so they persist across saves and sync to
    /// other clients. Entirely failure-safe: a missing value simply leaves that villager at the
    /// default, and the tint is applied with a MaterialPropertyBlock (no shared-material edits, no
    /// leaked instances) — if the model's shader has no colour property the tint is silently ignored.
    /// </summary>
    public static class VillagerAppearance
    {
        private const float Min = 0.9f;
        private const float Max = 1.12f;

        // How far the tint pulls the model toward a random hue (0 = none, 1 = full). Kept low so it
        // reads as different-coloured clothes rather than a garish, fully-recoloured body.
        private const float TintStrength = 0.4f;

        /// <summary>Pick a random size + tint, store them in the ZDO, and apply. Called once, at spawn.
        /// A <paramref name="tintSeed"/> in [0,1] fixes this villager's hue (so a vendor type can have a
        /// signature colour); anything outside that range keeps the per-villager random tint.</summary>
        public static void Assign(GameObject go, ZDO zdo, float tintSeed = -1f)
        {
            float scale = Random.Range(Min, Max);
            float tint = (tintSeed >= 0f && tintSeed <= 1f) ? tintSeed : Random.value;
            if (zdo != null)
            {
                zdo.Set(Constants.KeyScale, scale);
                zdo.Set(Constants.KeyTint, tint);
            }
            ApplyScale(go, scale);
            ApplyTint(go, tint);
        }

        /// <summary>Re-apply the stored size + tint (on world load and on remote clients).</summary>
        public static void Apply(GameObject go, ZDO zdo)
        {
            if (zdo == null)
                return;

            float scale = zdo.GetFloat(Constants.KeyScale, 0f);
            if (scale > 0f)
                ApplyScale(go, scale);

            // A stored hue of exactly 0 is valid (red), so use a negative sentinel for "never set"
            // (villagers spawned before tints existed) and leave those untinted.
            float tint = zdo.GetFloat(Constants.KeyTint, -1f);
            if (tint >= 0f)
                ApplyTint(go, tint);
        }

        private static void ApplyScale(GameObject go, float scale)
        {
            if (go != null)
                go.transform.localScale = new Vector3(scale, scale, scale);
        }

        private static void ApplyTint(GameObject go, float seed)
        {
            if (go == null)
                return;

            Color hue = Color.HSVToRGB(Mathf.Repeat(seed, 1f), 0.7f, 1f);
            Color tint = Color.Lerp(Color.white, hue, TintStrength);

            foreach (SkinnedMeshRenderer rend in go.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (rend == null)
                    continue;
                var block = new MaterialPropertyBlock();
                rend.GetPropertyBlock(block);
                block.SetColor("_Color", tint);
                rend.SetPropertyBlock(block);
            }
        }
    }
}
