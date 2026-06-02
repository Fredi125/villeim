using UnityEngine;

namespace VillageLife.NPC
{
    /// <summary>
    /// World-global completion count per quest, stored as consecutive boolean global keys — the same
    /// mechanism Valheim uses for boss defeats and <see cref="TraderReputation"/> — so it survives
    /// saves and syncs in co-op.
    ///
    /// The count drives both of a quest's repeat behaviours:
    ///   • <b>rotation</b> — which recipe in the pool is active (count modulo pool size), and
    ///   • <b>scaling</b> — the size of the required items and the reward, each growing by its own
    ///     fraction of the base per prior completion (see <see cref="Scale"/>).
    /// Both plateau at the quest's cap, which also keeps the per-quest global keys bounded.
    /// </summary>
    public static class QuestProgress
    {
        /// <summary>Cap used when a quest doesn't specify its own (completions before it plateaus).</summary>
        public const int DefaultMaxScaling = 10;

        private static string Key(string questId, int n) => $"villagelife_quest_{questId}_{n}";

        /// <summary>How many times this quest has been completed (clamped to 0..<paramref name="cap"/>).</summary>
        public static int Completed(string questId, int cap)
        {
            ZoneSystem zs = ZoneSystem.instance;
            if (zs == null || string.IsNullOrEmpty(questId))
                return 0;
            if (cap <= 0)
                cap = DefaultMaxScaling;

            int n = 0;
            while (n < cap && zs.GetGlobalKey(Key(questId, n + 1)))
                n++;
            return n;
        }

        /// <summary>Scale a base amount by <paramref name="growth"/> per completion (at least 1):
        /// <c>round(base × (1 + growth × completed))</c>.</summary>
        public static int Scale(int baseAmount, int completed, float growth)
            => Mathf.Max(1, Mathf.RoundToInt(baseAmount * (1f + growth * completed)));

        /// <summary>Record one completion (advancing the count) unless already at the cap.</summary>
        public static void RecordCompletion(string questId, int cap)
        {
            ZoneSystem zs = ZoneSystem.instance;
            if (zs == null || string.IsNullOrEmpty(questId))
                return;
            if (cap <= 0)
                cap = DefaultMaxScaling;

            int n = Completed(questId, cap);
            if (n < cap)
                zs.SetGlobalKey(Key(questId, n + 1));
        }
    }
}
