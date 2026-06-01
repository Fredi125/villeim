namespace VillageLife.NPC
{
    /// <summary>
    /// World-global trader reputation, stored as boolean global keys — the same mechanism Valheim
    /// uses for boss defeats — so it survives saves and syncs in co-op.
    ///
    /// A trader's level is simply how many consecutive reputation keys are set
    /// (<c>villagelife_rep_&lt;id&gt;_1</c>, <c>_2</c>, …). Completing that trader's bounty advances
    /// it by one, capped at the highest tier the trader actually has goods for, so the keys can't
    /// grow without bound. Higher-tier goods become visible once the level reaches their tier
    /// (see <see cref="MerchantStock"/>).
    /// </summary>
    public static class TraderReputation
    {
        private static string Key(string traderId, int level) => $"villagelife_rep_{traderId}_{level}";

        /// <summary>Current reputation level for a trader (0 if none, or the world isn't ready yet).</summary>
        public static int Level(string traderId)
        {
            ZoneSystem zs = ZoneSystem.instance;
            if (zs == null || string.IsNullOrEmpty(traderId))
                return 0;

            int level = 0;
            while (zs.GetGlobalKey(Key(traderId, level + 1)))
                level++;
            return level;
        }

        /// <summary>The highest good tier a trader defines — its reputation cap.</summary>
        public static int MaxTier(VendorType trader)
        {
            int max = 0;
            if (trader?.Goods != null)
                foreach (VendorGood g in trader.Goods)
                    if (g.Tier > max)
                        max = g.Tier;
            return max;
        }

        /// <summary>
        /// Raise a trader's reputation by one level (setting the next global key) unless it's already
        /// at its cap. Returns true if it advanced. <paramref name="newLevel"/> and
        /// <paramref name="maxLevel"/> describe the state either way, so callers can message progress.
        /// </summary>
        public static bool TryAdvance(string traderId, out int newLevel, out int maxLevel)
        {
            VendorType trader = VendorCatalog.ById(traderId);
            maxLevel = MaxTier(trader);
            newLevel = Level(traderId);

            ZoneSystem zs = ZoneSystem.instance;
            if (zs == null || newLevel >= maxLevel)
                return false;

            zs.SetGlobalKey(Key(traderId, newLevel + 1));
            newLevel++;
            return true;
        }
    }
}
