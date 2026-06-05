using System;

namespace VillageLife.NPC
{
    /// <summary>One thing a coin-shop vendor offers: a prefab, its price, and the stack size sold.
    /// A "rare" item is simply a good with a high price — no separate type needed.</summary>
    [Serializable]
    public struct VendorGood
    {
        public string Prefab;
        public int Price;
        public int Stack;
        public int Tier;        // reputation level required before this good appears (0 = always sold).

        public VendorGood(string prefab, int price, int stack) : this(prefab, price, stack, 0) { }

        public VendorGood(string prefab, int price, int stack, int tier)
        {
            Prefab = prefab;
            Price = price;
            Stack = stack;
            Tier = tier;
        }
    }

    /// <summary>One required item in a quest turn-in: a prefab and how many. Its own small
    /// serializable type (rather than reusing <see cref="VendorGood"/>) so quests read clearly in
    /// <c>vendors.json</c>.</summary>
    [Serializable]
    public struct QuestItem
    {
        public string Prefab;
        public int Amount;

        public QuestItem(string prefab, int amount)
        {
            Prefab = prefab;
            Amount = amount;
        }
    }

    /// <summary>One recipe in a quest: the items to hand in and the reward for them. A quest holds a
    /// pool of these and rotates to the next one after each turn-in.</summary>
    [Serializable]
    public struct QuestRecipe
    {
        public QuestItem[] Items;
        public string RewardPrefab;
        public int RewardAmount;
    }

    /// <summary>
    /// A kind of villager: a display title plus what it offers. Serializable so the whole catalogue
    /// can live in <c>vendors.json</c>. <see cref="Kind"/> is a string ("coin" or "barter") rather
    /// than an enum so the config file stays human-readable.
    /// </summary>
    [Serializable]
    public class VendorType
    {
        public string Id;
        public string Title;
        public string Kind = "coin";   // "coin" = vanilla shop window; "barter" = fixed swap.
        public string Biome = "";      // Optional tag for biome-themed vendors (forward-looking).
        public string Model = "";      // Optional NPC prefab to clone for this villager's look (empty = default).
        public float Tint = -1f;       // Optional fixed colour hue (0..1) for this villager (< 0 = random per-villager).

        // Coin shop: which trader's reputation gates this shop's higher-tier goods. Empty = this
        // vendor's own Id (the usual case). Set it to share another trader's reputation track — e.g. a
        // biome's secondary shop reading the biome trader's reputation, since only that biome trader
        // has a bounty advancing it. Resolved by RepVendorId.
        public string ReputationId = "";

        // Coin shop: the goods sold for coins (include a high-priced "rare" entry if desired).
        public VendorGood[] Goods;

        // Barter: "give CostAmount × CostPrefab, receive GiveAmount × GivePrefab".
        public string CostPrefab;
        public int CostAmount;
        public string GivePrefab;
        public int GiveAmount;

        // If set, completing this barter (a bounty) raises world reputation with that trader id,
        // unlocking the trader's higher-tier goods. Empty = the barter grants no reputation.
        public string UnlocksVendorId = "";

        // Quest: a multi-item turn-in (deeper than a single-item barter). A quest holds a POOL of
        // recipes and rotates to the next one after each turn-in; non-empty = this villager is a
        // quest-giver. Spawned via the barter path (Kind="barter"); any UnlocksVendorId still grants
        // reputation on completion. The scaling fields tune how it grows as it is repeated.
        public QuestRecipe[] QuestRecipes;

        // Per-completion growth of the required items and the reward, as a fraction of the base
        // (0.5 = +50% per prior completion). Rewards usually grow faster than costs so repeats pay
        // off. QuestMaxScaling caps how far rotation + scaling climb before they plateau (0 = default).
        public float QuestCostGrowth;
        public float QuestRewardGrowth;
        public int QuestMaxScaling;

        /// <summary>True when this vendor trades by barter rather than the coin shop.</summary>
        public bool IsBarter => string.Equals(Kind, "barter", StringComparison.OrdinalIgnoreCase);

        /// <summary>The trader id whose reputation gates this vendor's goods: <see cref="ReputationId"/>
        /// when set, otherwise this vendor's own <see cref="Id"/>.</summary>
        public string RepVendorId => string.IsNullOrEmpty(ReputationId) ? Id : ReputationId;

        /// <summary>True when this villager is a quest-giver (has at least one recipe).</summary>
        public bool IsQuest => QuestRecipes != null && QuestRecipes.Length > 0;

        /// <summary>This quest's scaling/rotation cap (completions before it plateaus).</summary>
        public int QuestCap => QuestMaxScaling > 0 ? QuestMaxScaling : QuestProgress.DefaultMaxScaling;

        /// <summary>The recipe active at a given completion count — the pool cycles one per turn-in.</summary>
        public QuestRecipe ActiveQuestRecipe(int completed)
        {
            int n = QuestRecipes.Length;
            return QuestRecipes[((completed % n) + n) % n];
        }
    }

    /// <summary>
    /// The in-memory catalogue of villager types. Populated from <c>vendors.json</c> at startup
    /// (see <see cref="VendorConfigLoader"/>); if that file is missing or invalid we fall back to
    /// <see cref="DefaultVendors"/>, so a bad edit can never leave the mod with no vendors.
    ///
    /// To add a vendor: edit vendors.json (no rebuild), or add to <see cref="DefaultVendors"/> here.
    /// Prefab names are resolved at runtime and any that don't exist are skipped, so a typo offers
    /// fewer goods (or a barter that politely refuses) rather than crashing.
    /// </summary>
    public static class VendorCatalog
    {
        /// <summary>
        /// Bumped whenever the built-in defaults change (prices, goods, new vendors). The loader
        /// regenerates an out-of-date vendors.json from these defaults (keeping a .bak), so value
        /// tweaks here reach an existing install without a manual file delete.
        /// </summary>
        public const int ConfigVersion = 17;

        /// <summary>Built-in safety net, also used to seed vendors.json on first run.</summary>
        public static VendorType[] DefaultVendors => new[]
        {
            new VendorType
            {
                Id = "general", Title = "General Store", Kind = "coin",
                Goods = new[]
                {
                    new VendorGood("Wood",  10, 50),    // 0.2/unit
                    new VendorGood("Stone", 10, 50),    // 0.2/unit
                    new VendorGood("Coal",  10, 20),    // 0.5/unit
                    new VendorGood("Flint", 10, 20),    // 0.5/unit
                    new VendorGood("Resin", 10, 20),    // 0.5/unit
                }
            },
            new VendorType
            {
                Id = "forager", Title = "Forager", Kind = "coin",
                Goods = new[]
                {
                    new VendorGood("Raspberry",   10, 20),  // 0.5/unit
                    new VendorGood("Mushroom",    10, 20),  // 0.5/unit
                    new VendorGood("Blueberries", 10, 20),  // 0.5/unit
                    new VendorGood("Honey",       10, 10),  // 1/unit
                    new VendorGood("Dandelion",   10, 10),  // 1/unit
                    new VendorGood("Thistle",     10, 10),  // 1/unit
                }
            },
            new VendorType
            {
                Id = "huntsman", Title = "Huntsman", Kind = "coin",
                Goods = new[]
                {
                    new VendorGood("LeatherScraps", 10, 20),  // 0.5/unit
                    new VendorGood("DeerHide",      10, 15),  // ~0.7/unit
                    new VendorGood("Feathers",      10, 20),  // 0.5/unit
                    new VendorGood("ArrowFlint",    10, 20),  // 0.5/unit
                    new VendorGood("Resin",         10, 20),  // 0.5/unit
                }
            },
            new VendorType
            {
                Id = "stonemason", Title = "Stonemason", Kind = "barter",
                CostPrefab = "Stone", CostAmount = 40,
                GivePrefab = "Wood",  GiveAmount = 30,
            },

            // Trade-skill shops — extra village roles, each with a themed line of goods.
            new VendorType
            {
                Id = "blacksmith", Title = "Blacksmith", Kind = "coin",
                Goods = new[]
                {
                    new VendorGood("Coal",        10, 20),  // 0.5/unit
                    new VendorGood("BronzeNails", 10, 20),  // 0.5/unit
                    new VendorGood("IronNails",   20, 20),  // 1/unit
                    new VendorGood("Bronze",     100, 10),  // 10/unit (alloy — no ore form)
                }
            },
            new VendorType
            {
                Id = "tavern", Title = "Tavern Keeper", Kind = "coin",
                Goods = new[]
                {
                    new VendorGood("Mushroom",    10, 20),  // 0.5/unit
                    new VendorGood("Raspberry",   10, 20),  // 0.5/unit
                    new VendorGood("Blueberries", 10, 20),  // 0.5/unit
                    new VendorGood("Honey",       10, 10),  // 1/unit
                    new VendorGood("CookedMeat",  15, 10),  // 1.5/unit
                }
            },
            new VendorType
            {
                Id = "farmer", Title = "Farmer", Kind = "coin",
                Goods = new[]
                {
                    new VendorGood("Carrot",      10, 20),  // 0.5/unit
                    new VendorGood("Turnip",      10, 20),  // 0.5/unit
                    new VendorGood("CarrotSeeds", 15, 20),  // 0.75/unit
                    new VendorGood("TurnipSeeds", 15, 20),  // 0.75/unit
                    new VendorGood("OnionSeeds",  15, 20),  // 0.75/unit
                }
            },

            // Guard — not a trader at all; summoned at a Guard Post to ward off nearby monsters.
            new VendorType { Id = "guard", Title = "Village Guard", Kind = "guard" },

            // Biome vendors — summoned by their matching biome station. Each sells basic biome
            // resources plus one high-priced "rare" item (just a Good with a big price). Prefab
            // names are resolved at runtime; any that don't exist are skipped and logged, so a
            // wrong name simply means that item is absent (fix it in vendors.json).
            new VendorType
            {
                Id = "meadows", Title = "Meadows Trader", Kind = "coin", Biome = "Meadows",
                Goods = new[]
                {
                    new VendorGood("Wood",      50, 50),   // 1/unit
                    new VendorGood("Stone",     50, 50),   // 1/unit
                    new VendorGood("Flint",     30, 30),   // 1/unit
                    new VendorGood("Resin",     20, 20),   // 1/unit
                    new VendorGood("Dandelion", 10, 10),   // 1/unit
                    new VendorGood("QueenBee", 800,  1, 1),  // rare — unlocks at Rep 1
                }
            },
            new VendorType
            {
                Id = "blackforest", Title = "Black Forest Trader", Kind = "coin", Biome = "BlackForest",
                Goods = new[]
                {
                    new VendorGood("RoundLog",      60, 30),     // 2/unit (core wood)
                    new VendorGood("Coal",          60, 30),     // 2/unit
                    new VendorGood("CopperOre",     80, 20),     // 4/unit (raw ore)
                    new VendorGood("TinOre",        80, 20),     // 4/unit (raw ore)
                    new VendorGood("FineWood",      60, 30, 1),  // 2/unit — fine wood unlocks at Rep 1
                    new VendorGood("SurtlingCore", 400,  1, 1),  // rare — unlocks at Rep 1
                }
            },
            new VendorType
            {
                Id = "swamp", Title = "Swamp Trader", Kind = "coin", Biome = "Swamp",
                Goods = new[]
                {
                    new VendorGood("Guck",          60, 20),  // 3/unit
                    new VendorGood("Entrails",      60, 20),  // 3/unit
                    new VendorGood("Bloodbag",      60, 20),  // 3/unit
                    new VendorGood("WitheredBone",  30, 10),  // 3/unit
                    new VendorGood("ElderBark",     60, 20),  // 3/unit (ancient wood)
                    new VendorGood("IronScrap",    120, 20),  // 6/unit (raw scrap)
                    new VendorGood("Chain",        400,  1, 1),  // rare — unlocks at Rep 1
                }
            },
            new VendorType
            {
                Id = "mountain", Title = "Mountain Trader", Kind = "coin", Biome = "Mountain",
                Goods = new[]
                {
                    new VendorGood("Obsidian",     160, 20),  // 8/unit
                    new VendorGood("FreezeGland",   80, 20),  // 4/unit
                    new VendorGood("WolfPelt",      80, 20),  // 4/unit
                    new VendorGood("Onion",         40, 10),  // 4/unit
                    new VendorGood("Crystal",      160, 20),  // 8/unit
                    new VendorGood("SilverOre",    160, 10),  // 16/unit (raw ore)
                    new VendorGood("DragonEgg",   9999,  1, 1),  // rare — unlocks at Rep 1
                }
            },
            new VendorType
            {
                Id = "plains", Title = "Plains Trader", Kind = "coin", Biome = "Plains",
                Goods = new[]
                {
                    new VendorGood("Barley",          100, 20),  // 5/unit
                    new VendorGood("Flax",            100, 20),  // 5/unit
                    new VendorGood("Cloudberry",      100, 20),  // 5/unit
                    new VendorGood("Needle",          100, 20),  // 5/unit
                    new VendorGood("Tar",             100, 20),  // 5/unit
                    new VendorGood("BlackMetalScrap", 200, 20),  // 10/unit (raw scrap)
                    new VendorGood("LoxPelt",         300,  1, 1),  // rare — unlocks at Rep 1
                }
            },

            // Turn-in bounties — barter villagers that buy monster trophies for Coins. They reuse the
            // barter mechanic (one fixed turn-in each) and are the main early way to EARN the coins
            // the pricey minerals above demand. A bounty scales with how dangerous its trophy is to
            // collect. Trophy prefab names are verified by the startup ItemNameAudit; a wrong one
            // just makes that bounty politely refuse rather than break anything.
            new VendorType
            {
                Id = "bounty_meadows", Title = "Meadows Bounty", Kind = "barter",
                CostPrefab = "TrophyBoar",      CostAmount = 2,
                GivePrefab = "Coins",           GiveAmount = 10,
                UnlocksVendorId = "meadows",
            },
            new VendorType
            {
                Id = "bounty_forest", Title = "Forest Bounty", Kind = "barter",
                CostPrefab = "TrophyGreydwarf", CostAmount = 3,
                GivePrefab = "Coins",           GiveAmount = 24,
                UnlocksVendorId = "blackforest",
            },
            new VendorType
            {
                Id = "bounty_swamp", Title = "Swamp Bounty", Kind = "barter",
                CostPrefab = "TrophyDraugr",    CostAmount = 2,
                GivePrefab = "Coins",           GiveAmount = 45,
                UnlocksVendorId = "swamp",
            },
            new VendorType
            {
                Id = "bounty_mountain", Title = "Mountain Bounty", Kind = "barter", Model = "DvergerMageSupport",
                CostPrefab = "TrophyWolf",      CostAmount = 2,
                GivePrefab = "Coins",           GiveAmount = 70,
                UnlocksVendorId = "mountain",
            },
            new VendorType
            {
                Id = "bounty_plains", Title = "Plains Bounty", Kind = "barter",
                CostPrefab = "TrophyGoblin",    CostAmount = 2,
                GivePrefab = "Coins",           GiveAmount = 90,
                UnlocksVendorId = "plains",
            },
            // A resource bounty rather than a trophy one: a standing order for resin, payable any
            // time. No reputation (resin spans biomes) — just a reliable early coin faucet.
            new VendorType
            {
                Id = "bounty_resin", Title = "Resin Bounty", Kind = "barter", Model = "Haldor",
                CostPrefab = "Resin", CostAmount = 5,
                GivePrefab = "Coins", GiveAmount = 10,
            },

            // --- Quests (proof of concept) — multi-item turn-ins, a step beyond the single-item
            // bounties: hand in everything listed at once for the reward. Spawned like barterers
            // (Kind="barter") and posted at the Quest Board. ---
            new VendorType
            {
                Id = "quest_provisions", Title = "Provisioner's Request", Kind = "barter",
                QuestCostGrowth = 0.5f, QuestRewardGrowth = 0.75f, QuestMaxScaling = 10,
                QuestRecipes = new[]
                {
                    new QuestRecipe
                    {
                        Items = new[] { new QuestItem("Wood", 20), new QuestItem("Resin", 10), new QuestItem("LeatherScraps", 5) },
                        RewardPrefab = "Coins", RewardAmount = 40,
                    },
                    new QuestRecipe
                    {
                        Items = new[] { new QuestItem("Stone", 20), new QuestItem("Flint", 10), new QuestItem("Feathers", 10) },
                        RewardPrefab = "Coins", RewardAmount = 45,
                    },
                },
            },
            new VendorType
            {
                Id = "quest_smith", Title = "Smith's Commission", Kind = "barter",
                QuestCostGrowth = 0.5f, QuestRewardGrowth = 0.75f, QuestMaxScaling = 10,
                QuestRecipes = new[]
                {
                    new QuestRecipe
                    {
                        Items = new[] { new QuestItem("Coal", 10), new QuestItem("Copper", 5), new QuestItem("Tin", 5) },
                        RewardPrefab = "Coins", RewardAmount = 60,
                    },
                    new QuestRecipe
                    {
                        Items = new[] { new QuestItem("Bronze", 5), new QuestItem("BronzeNails", 10), new QuestItem("Coal", 15) },
                        RewardPrefab = "Coins", RewardAmount = 75,
                    },
                },
            },

            // --- Meadows biome villagers (tier 1 ≈ 1 gold/unit) — the template for every biome's
            // spawner: 3 shops that sell for gold + 3 barterers that swap a resource for gold or goods. ---
            new VendorType
            {
                Id = "meadows_forage", Title = "Meadows Forager", Kind = "coin", Biome = "Meadows",
                Goods = new[]
                {
                    new VendorGood("Raspberry",   20, 20),  // 1/unit
                    new VendorGood("Mushroom",    20, 20),  // 1/unit
                    new VendorGood("Blueberries", 20, 20),  // 1/unit
                    new VendorGood("Dandelion",   20, 20),  // 1/unit
                    new VendorGood("Honey",       40, 20),  // 2/unit
                }
            },
            new VendorType
            {
                Id = "meadows_hunt", Title = "Meadows Hunter", Kind = "coin", Biome = "Meadows",
                Goods = new[]
                {
                    new VendorGood("LeatherScraps", 20, 20),  // 1/unit
                    new VendorGood("DeerHide",      20, 20),  // 1/unit
                    new VendorGood("ArrowFlint",    20, 20),  // 1/unit
                    new VendorGood("Feathers",      20, 20),  // 1/unit
                    new VendorGood("Resin",         20, 20),  // 1/unit
                }
            },
            new VendorType
            {
                Id = "meadows_tanner", Title = "Meadows Tanner", Kind = "barter", Biome = "Meadows",
                CostPrefab = "DeerHide",      CostAmount = 5,
                GivePrefab = "LeatherScraps", GiveAmount = 10,
            },
            new VendorType
            {
                Id = "meadows_beekeeper", Title = "Meadows Beekeeper", Kind = "barter", Biome = "Meadows",
                CostPrefab = "Honey", CostAmount = 5,
                GivePrefab = "Coins", GiveAmount = 10,
            },

            // --- Black Forest biome villagers (tier 2 ≈ 2 gold/unit): 2 shops + 2 barterers that,
            // with the trader and the Forest Bounty, make the spawner's 3 shops + 3 barterers. ---
            new VendorType
            {
                Id = "blackforest_miner", Title = "Black Forest Miner", Kind = "coin", Biome = "BlackForest",
                Goods = new[]
                {
                    new VendorGood("CopperOre", 80, 20),  // 4/unit (raw ore)
                    new VendorGood("TinOre",    80, 20),  // 4/unit (raw ore)
                    new VendorGood("Coal",      60, 30),  // 2/unit
                    new VendorGood("Stone",     40, 20),  // 2/unit
                    new VendorGood("Flint",     40, 20),  // 2/unit
                }
            },
            new VendorType
            {
                Id = "blackforest_carpenter", Title = "Black Forest Carpenter", Kind = "coin", Biome = "BlackForest",
                Goods = new[]
                {
                    new VendorGood("RoundLog",     60, 30),  // 2/unit
                    new VendorGood("Wood",         40, 20),  // 2/unit
                    new VendorGood("Resin",        40, 20),  // 2/unit
                    new VendorGood("GreydwarfEye", 40, 20),  // 2/unit
                    new VendorGood("BronzeNails",  40, 20),  // 2/unit
                }
            },
            new VendorType
            {
                Id = "blackforest_charcoal", Title = "Black Forest Charcoaler", Kind = "barter", Biome = "BlackForest", Model = "Greydwarf", Tint = 0.08f,
                CostPrefab = "Wood", CostAmount = 5,
                GivePrefab = "Coal", GiveAmount = 10,
            },
            new VendorType
            {
                Id = "blackforest_smelter", Title = "Black Forest Smelter", Kind = "barter", Biome = "BlackForest", Model = "DvergerMageFire",
                CostPrefab = "GreydwarfEye", CostAmount = 5,
                GivePrefab = "Coins",        GiveAmount = 20,
            },

            // --- Swamp biome villagers (tier 3 ≈ 3 gold/unit). ---
            new VendorType
            {
                Id = "swamp_alchemist", Title = "Swamp Alchemist", Kind = "coin", Biome = "Swamp",
                Goods = new[]
                {
                    new VendorGood("Guck",         60, 20),  // 3/unit
                    new VendorGood("Bloodbag",     60, 20),  // 3/unit
                    new VendorGood("Entrails",     60, 20),  // 3/unit
                    new VendorGood("Thistle",      60, 20),  // 3/unit
                    new VendorGood("WitheredBone", 30, 10),  // 3/unit
                }
            },
            new VendorType
            {
                Id = "swamp_digger", Title = "Swamp Digger", Kind = "coin", Biome = "Swamp",
                Goods = new[]
                {
                    new VendorGood("IronScrap",   120, 20),  // 6/unit (raw scrap)
                    new VendorGood("ElderBark",    60, 20),  // 3/unit
                    new VendorGood("Coal",         90, 30),  // 3/unit
                    new VendorGood("Stone",        60, 20),  // 3/unit
                    new VendorGood("WitheredBone", 30, 10),  // 3/unit
                }
            },
            new VendorType
            {
                Id = "swamp_grinder", Title = "Swamp Bonegrinder", Kind = "barter", Biome = "Swamp", Model = "DvergerMageIce",
                CostPrefab = "WitheredBone",  CostAmount = 5,
                GivePrefab = "BoneFragments", GiveAmount = 10,
            },
            new VendorType
            {
                Id = "swamp_renderer", Title = "Swamp Renderer", Kind = "barter", Biome = "Swamp",
                CostPrefab = "Bloodbag", CostAmount = 5,
                GivePrefab = "Coins",    GiveAmount = 30,
            },

            // --- Mountain biome villagers (tier 4 ≈ 4 gold/unit). ---
            new VendorType
            {
                Id = "mountain_miner", Title = "Mountain Miner", Kind = "coin", Biome = "Mountain",
                // Shares the Mountain Trader's reputation (raised by the Mountain Bounty): the cheap
                // bulk silver ore stays locked until you've proven yourself in the peaks.
                ReputationId = "mountain",
                Goods = new[]
                {
                    new VendorGood("SilverOre", 160, 10),     // 16/unit — small lots, always available
                    new VendorGood("SilverOre",  80, 20, 1),  // 4/unit — bulk lot, unlocks at Mountain Rep 1
                    new VendorGood("Obsidian",  160, 20),  // 8/unit
                    new VendorGood("Crystal",   160, 20),  // 8/unit
                    new VendorGood("Stone",      80, 20),  // 4/unit
                    new VendorGood("Coal",      120, 30),  // 4/unit
                }
            },
            new VendorType
            {
                Id = "mountain_herbalist", Title = "Mountain Herbalist", Kind = "coin", Biome = "Mountain",
                Goods = new[]
                {
                    new VendorGood("Onion",       40, 10),  // 4/unit
                    new VendorGood("OnionSeeds",  40, 10),  // 4/unit
                    new VendorGood("FreezeGland", 80, 20),  // 4/unit
                    new VendorGood("WolfPelt",    80, 20),  // 4/unit
                    new VendorGood("Crystal",     80, 20),  // 4/unit
                }
            },
            new VendorType
            {
                Id = "mountain_furrier", Title = "Mountain Furrier", Kind = "barter", Biome = "Mountain", Model = "Dverger",
                CostPrefab = "WolfPelt",      CostAmount = 5,
                GivePrefab = "LeatherScraps", GiveAmount = 10,
            },
            new VendorType
            {
                Id = "mountain_jeweler", Title = "Mountain Jeweler", Kind = "barter", Biome = "Mountain", Model = "DvergerMage",
                CostPrefab = "FreezeGland", CostAmount = 5,
                GivePrefab = "Coins",       GiveAmount = 40,
            },

            // --- Plains biome villagers (tier 5 ≈ 5 gold/unit). ---
            new VendorType
            {
                Id = "plains_farmer", Title = "Plains Farmer", Kind = "coin", Biome = "Plains",
                Goods = new[]
                {
                    new VendorGood("Barley",      100, 20),  // 5/unit
                    new VendorGood("Flax",        100, 20),  // 5/unit
                    new VendorGood("Cloudberry",  100, 20),  // 5/unit
                    new VendorGood("BarleyFlour", 100, 20),  // 5/unit
                    new VendorGood("Needle",      100, 20),  // 5/unit
                }
            },
            new VendorType
            {
                Id = "plains_smith", Title = "Plains Blacksmith", Kind = "coin", Biome = "Plains",
                Goods = new[]
                {
                    new VendorGood("BlackMetalScrap", 200, 20),  // 10/unit (raw scrap)
                    new VendorGood("Tar",             100, 20),  // 5/unit
                    new VendorGood("Needle",          100, 20),  // 5/unit
                    new VendorGood("Barley",          100, 20),  // 5/unit
                    new VendorGood("Flax",            100, 20),  // 5/unit
                }
            },
            new VendorType
            {
                Id = "plains_weaver", Title = "Plains Weaver", Kind = "barter", Biome = "Plains", Model = "Goblin",
                CostPrefab = "Flax",        CostAmount = 5,
                GivePrefab = "LinenThread", GiveAmount = 10,
            },
            new VendorType
            {
                Id = "plains_rancher", Title = "Plains Rancher", Kind = "barter", Biome = "Plains", Model = "GoblinShaman",
                CostPrefab = "Cloudberry", CostAmount = 5,
                GivePrefab = "Coins",      GiveAmount = 50,
            },

            // --- v3.40.0 variety pass — themed creature-model villagers that round out the later
            // biomes. Each clones a monster prefab (stripped of AI by NpcPrefab.Neutralize) so the
            // swamp/mountain/plains stations field recognisable locals instead of more Hildirs. ---

            // Swamp Wraith quest-giver: a ghost that pays well for the marsh's grisly leavings.
            new VendorType
            {
                Id = "quest_wraith", Title = "Wraith's Lament", Kind = "barter", Biome = "Swamp", Model = "Wraith",
                UnlocksVendorId = "swamp",
                QuestCostGrowth = 0.5f, QuestRewardGrowth = 0.75f, QuestMaxScaling = 10,
                QuestRecipes = new[]
                {
                    new QuestRecipe
                    {
                        Items = new[] { new QuestItem("WitheredBone", 3), new QuestItem("Bloodbag", 5), new QuestItem("Guck", 5) },
                        RewardPrefab = "Coins", RewardAmount = 80,
                    },
                    new QuestRecipe
                    {
                        Items = new[] { new QuestItem("Entrails", 5), new QuestItem("IronScrap", 10), new QuestItem("ElderBark", 10) },
                        RewardPrefab = "Coins", RewardAmount = 90,
                    },
                },
            },
            // Swamp Draugr archer: a barterer who buys feathers for the fletching trade.
            new VendorType
            {
                Id = "swamp_archer", Title = "Swamp Bowman", Kind = "barter", Biome = "Swamp", Model = "Draugr_Ranged",
                CostPrefab = "Feathers", CostAmount = 10,
                GivePrefab = "Coins",    GiveAmount = 30,
            },

            // Mountain Fenring Cultist quest-giver: a turncoat who pays bounties on his own kind.
            new VendorType
            {
                Id = "quest_fenring", Title = "Cultist's Vendetta", Kind = "barter", Biome = "Mountain", Model = "Fenring_Cultist",
                UnlocksVendorId = "mountain",
                QuestCostGrowth = 0.5f, QuestRewardGrowth = 0.8f, QuestMaxScaling = 10,
                QuestRecipes = new[]
                {
                    new QuestRecipe
                    {
                        Items = new[] { new QuestItem("TrophyFenring", 2) },
                        RewardPrefab = "Coins", RewardAmount = 120,
                    },
                    new QuestRecipe
                    {
                        Items = new[] { new QuestItem("TrophyFenring", 1), new QuestItem("TrophyUlv", 2) },
                        RewardPrefab = "Coins", RewardAmount = 140,
                    },
                },
            },
            // Mountain Troll barterer: a gentle giant who takes stone off your hands for coin.
            new VendorType
            {
                Id = "mountain_troll", Title = "Mountain Troll", Kind = "barter", Biome = "Mountain", Model = "Troll",
                CostPrefab = "Stone", CostAmount = 20,
                GivePrefab = "Coins", GiveAmount = 40,
            },

            // Plains Goblin Brute bounty: the heaviest trophy pays the heaviest purse.
            new VendorType
            {
                Id = "bounty_brute", Title = "Plains Brute Bounty", Kind = "barter", Biome = "Plains", Model = "GoblinBrute",
                CostPrefab = "TrophyGoblinBrute", CostAmount = 1,
                GivePrefab = "Coins",             GiveAmount = 120,
                UnlocksVendorId = "plains",
            },
        };

        private static VendorType[] _all;

        /// <summary>The active catalogue (defaults until <see cref="Initialize"/> is called).</summary>
        public static VendorType[] All => _all ?? (_all = DefaultVendors);

        /// <summary>Replace the catalogue. Null or empty input falls back to the built-in defaults.</summary>
        public static void Initialize(VendorType[] vendors)
        {
            _all = (vendors != null && vendors.Length > 0) ? vendors : DefaultVendors;
        }

        /// <summary>Look up a type by id, falling back to the first type if unknown.</summary>
        public static VendorType ById(string id)
        {
            if (!string.IsNullOrEmpty(id))
                foreach (var v in All)
                    if (v != null && v.Id == id)
                        return v;
            return All[0];
        }

        /// <summary>Pick a type by index, wrapping safely for any int (including negatives).</summary>
        public static VendorType ByIndex(int index)
        {
            int i = ((index % All.Length) + All.Length) % All.Length;
            return All[i];
        }
    }
}
