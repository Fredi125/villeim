using UnityEngine;

namespace VillageLife.NPC
{
    /// <summary>
    /// World-global completion count per quest, stored as consecutive boolean global keys — the same
    /// mechanism Valheim uses for boss defeats and <see cref="TraderReputation"/> — so it survives
    /// saves and syncs in co-op.
    ///
    /// A repeatable quest scales up: each prior completion adds 50% of the base to both the required
    /// items and the reward (completion 0 = base ×1, then ×1.5, ×2, …). Scaling plateaus at
    /// <see cref="MaxScaling"/> completions — the quest stays repeatable at that size, and the cap
    /// keeps the per-quest global keys bounded.
    /// </summary>
    public static class QuestProgress
    {
        /// <summary>How many completions the difficulty climbs over before it plateaus.</summary>
        public const int MaxScaling = 10;

        private static string Key(string questId, int n) => $"villagelife_quest_{questId}_{n}";

        /// <summary>How many times this quest has been completed (clamped to 0..<see cref="MaxScaling"/>).</summary>
        public static int Completed(string questId)
        {
            ZoneSystem zs = ZoneSystem.instance;
            if (zs == null || string.IsNullOrEmpty(questId))
                return 0;

            int n = 0;
            while (n < MaxScaling && zs.GetGlobalKey(Key(questId, n + 1)))
                n++;
            return n;
        }

        /// <summary>The cost/reward multiplier at a completion count: +50% of the base per prior turn-in.</summary>
        public static float Multiplier(int completed) => 1f + 0.5f * completed;

        /// <summary>Scale a base amount by the completion multiplier (at least 1).</summary>
        public static int Scale(int baseAmount, int completed)
            => Mathf.Max(1, Mathf.RoundToInt(baseAmount * Multiplier(completed)));

        /// <summary>Record one completion (advancing the count) unless already at the scaling cap.</summary>
        public static void RecordCompletion(string questId)
        {
            ZoneSystem zs = ZoneSystem.instance;
            if (zs == null || string.IsNullOrEmpty(questId))
                return;

            int n = Completed(questId);
            if (n < MaxScaling)
                zs.SetGlobalKey(Key(questId, n + 1));
        }
    }
}
