using UnityEngine;
using VillageLife.Util;

namespace VillageLife.NPC
{
    /// <summary>
    /// Cosmetic per-villager variation — currently a subtle height/build difference so a row of
    /// villagers isn't a row of identical Haldors. The chosen size is stored in the ZDO, so it
    /// persists across saves and syncs to other clients.
    ///
    /// Deliberately gentle (0.9–1.12×) to keep hitboxes and hover positions sensible, and entirely
    /// failure-safe: a missing or zero value simply leaves the villager at default size.
    /// </summary>
    public static class VillagerAppearance
    {
        private const float Min = 0.9f;
        private const float Max = 1.12f;

        /// <summary>Pick a random size, store it in the ZDO, and apply it. Called once, at spawn.</summary>
        public static void Assign(GameObject go, ZDO zdo)
        {
            float scale = Random.Range(Min, Max);
            zdo?.Set(Constants.KeyScale, scale);
            ApplyScale(go, scale);
        }

        /// <summary>Re-apply the stored size (on world load and on remote clients).</summary>
        public static void Apply(GameObject go, ZDO zdo)
        {
            if (zdo == null)
                return;
            float scale = zdo.GetFloat(Constants.KeyScale, 0f);
            if (scale > 0f)
                ApplyScale(go, scale);
        }

        private static void ApplyScale(GameObject go, float scale)
        {
            if (go != null)
                go.transform.localScale = new Vector3(scale, scale, scale);
        }
    }
}
