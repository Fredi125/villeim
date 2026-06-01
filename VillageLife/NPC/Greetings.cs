using UnityEngine;

namespace VillageLife.NPC
{
    /// <summary>
    /// Flavour one-liners shown when a villager is summoned, to give each type a little character.
    /// Purely cosmetic text, chosen at summon time and never stored, so it can't affect anything.
    /// </summary>
    public static class Greetings
    {
        private static readonly string[] Merchant =
        {
            "Welcome to my stall!",
            "Coin for goods — that's the honest trade.",
            "Finest wares this side of the meadows.",
            "Browse as long as you like, friend.",
            "A village needs a good shop, and here I am.",
            "Trade keeps the cold away.",
        };

        private static readonly string[] Bounty =
        {
            "Got trophies? I've got coin.",
            "The hunt pays well — if you're up to it.",
            "Bring me proof of the kill.",
            "Dangerous work, good pay.",
            "There's always a beast that needs culling.",
        };

        /// <summary>A random line themed to the vendor (bounties get hunt talk, others shop talk).</summary>
        public static string Line(VendorType type)
        {
            string[] pool = (type != null && !string.IsNullOrEmpty(type.UnlocksVendorId))
                ? Bounty
                : Merchant;
            return pool[Random.Range(0, pool.Length)];
        }
    }
}
