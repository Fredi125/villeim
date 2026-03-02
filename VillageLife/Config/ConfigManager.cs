using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using VillageLife.Quest;
using VillageLife.Util;

namespace VillageLife.Config
{
    /// <summary>
    /// Manages loading and caching of JSON configuration files for shops, quests, and dialog.
    /// Creates default configs if none exist.
    /// </summary>
    public static class ConfigManager
    {
        private static string _configPath;
        private static Dictionary<string, ShopConfig> _shopConfigs = new Dictionary<string, ShopConfig>();
        private static List<QuestDefinition> _questDefinitions = new List<QuestDefinition>();
        private static DialogConfig _dialogConfig;

        public static void Initialize(string basePath)
        {
            _configPath = Path.Combine(basePath, Constants.ConfigFolderName);
            Directory.CreateDirectory(_configPath);

            LoadOrCreateShopConfigs();
            LoadOrCreateQuestConfigs();
            LoadOrCreateDialogConfig();
        }

        #region Shop Configs

        public static ShopConfig GetShopConfig(string shopType)
        {
            _shopConfigs.TryGetValue(shopType, out var config);
            return config ?? GetDefaultShopConfig();
        }

        public static IReadOnlyDictionary<string, ShopConfig> GetAllShopConfigs() => _shopConfigs;

        private static void LoadOrCreateShopConfigs()
        {
            string filePath = Path.Combine(_configPath, Constants.ShopConfigFile);

            if (File.Exists(filePath))
            {
                try
                {
                    string json = File.ReadAllText(filePath);
                    var wrapper = JsonUtility.FromJson<ShopConfigWrapper>(json);
                    if (wrapper?.Merchants != null)
                    {
                        foreach (var merchant in wrapper.Merchants)
                            _shopConfigs[merchant.ShopType] = merchant;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[VillageLife] Failed to load shop config: {e.Message}");
                }
            }

            // Validate loaded data — regenerate if empty or corrupt
            bool shopsValid = _shopConfigs.Count > 0 &&
                              _shopConfigs.Values.Any(s => s.Items != null && s.Items.Count > 0);
            if (!shopsValid)
            {
                _shopConfigs.Clear();
                CreateDefaultShopConfigs();
                SaveShopConfigs(filePath);
            }
        }

        private static void CreateDefaultShopConfigs()
        {
            _shopConfigs["general_store"] = new ShopConfig
            {
                ShopType = "general_store",
                DisplayName = "General Merchant",
                Currency = "Coins",
                Items = new List<ShopItemConfig>
                {
                    new ShopItemConfig { ItemName = "Wood", BuyPrice = 1, SellPrice = 0, MaxStock = 50 },
                    new ShopItemConfig { ItemName = "Stone", BuyPrice = 1, SellPrice = 0, MaxStock = 50 },
                    new ShopItemConfig { ItemName = "ArrowWood", BuyPrice = 2, SellPrice = 1, MaxStock = 100 },
                    new ShopItemConfig { ItemName = "Torch", BuyPrice = 3, SellPrice = 1, MaxStock = 20 },
                    new ShopItemConfig { ItemName = "Resin", BuyPrice = 2, SellPrice = 1, MaxStock = 30 },
                    new ShopItemConfig { ItemName = "Flint", BuyPrice = 2, SellPrice = 1, MaxStock = 40 }
                }
            };

            _shopConfigs["weaponsmith"] = new ShopConfig
            {
                ShopType = "weaponsmith",
                DisplayName = "Weaponsmith",
                Currency = "Coins",
                Items = new List<ShopItemConfig>
                {
                    new ShopItemConfig { ItemName = "AxeFlint", BuyPrice = 15, SellPrice = 5, MaxStock = 5 },
                    new ShopItemConfig { ItemName = "KnifeFlint", BuyPrice = 12, SellPrice = 4, MaxStock = 5 },
                    new ShopItemConfig { ItemName = "SpearFlint", BuyPrice = 15, SellPrice = 5, MaxStock = 5 },
                    new ShopItemConfig { ItemName = "ShieldWood", BuyPrice = 20, SellPrice = 7, MaxStock = 3 },
                    new ShopItemConfig { ItemName = "Bow", BuyPrice = 25, SellPrice = 8, MaxStock = 3 },
                    new ShopItemConfig { ItemName = "ArrowFlint", BuyPrice = 3, SellPrice = 1, MaxStock = 100 }
                }
            };

            _shopConfigs["armorer"] = new ShopConfig
            {
                ShopType = "armorer",
                DisplayName = "Armorer",
                Currency = "Coins",
                Items = new List<ShopItemConfig>
                {
                    new ShopItemConfig { ItemName = "ArmorLeatherChest", BuyPrice = 30, SellPrice = 10, MaxStock = 3 },
                    new ShopItemConfig { ItemName = "ArmorLeatherLegs", BuyPrice = 25, SellPrice = 8, MaxStock = 3 },
                    new ShopItemConfig { ItemName = "HelmetLeather", BuyPrice = 20, SellPrice = 7, MaxStock = 3 },
                    new ShopItemConfig { ItemName = "CapeDeerHide", BuyPrice = 20, SellPrice = 7, MaxStock = 3 }
                }
            };

            _shopConfigs["food_vendor"] = new ShopConfig
            {
                ShopType = "food_vendor",
                DisplayName = "Food Vendor",
                Currency = "Coins",
                Items = new List<ShopItemConfig>
                {
                    new ShopItemConfig { ItemName = "CookedMeat", BuyPrice = 5, SellPrice = 2, MaxStock = 20 },
                    new ShopItemConfig { ItemName = "Raspberry", BuyPrice = 1, SellPrice = 0, MaxStock = 50 },
                    new ShopItemConfig { ItemName = "Blueberries", BuyPrice = 2, SellPrice = 1, MaxStock = 30 },
                    new ShopItemConfig { ItemName = "Honey", BuyPrice = 5, SellPrice = 2, MaxStock = 15 },
                    new ShopItemConfig { ItemName = "QueensJam", BuyPrice = 8, SellPrice = 3, MaxStock = 10 }
                }
            };
        }

        private static ShopConfig GetDefaultShopConfig()
        {
            return _shopConfigs.Values.FirstOrDefault() ?? new ShopConfig
            {
                ShopType = "default",
                DisplayName = "Merchant",
                Currency = "Coins",
                Items = new List<ShopItemConfig>()
            };
        }

        private static void SaveShopConfigs(string filePath)
        {
            try
            {
                var wrapper = new ShopConfigWrapper
                {
                    Merchants = _shopConfigs.Values.ToList()
                };
                string json = JsonUtility.ToJson(wrapper, true);
                File.WriteAllText(filePath, json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[VillageLife] Failed to save shop config: {e.Message}");
            }
        }

        #endregion

        #region Quest Configs

        public static List<QuestDefinition> GetAllQuestDefinitions() => _questDefinitions;

        public static QuestDefinition GetQuestDefinition(string questId)
        {
            return _questDefinitions.FirstOrDefault(q => q.QuestId == questId);
        }

        private static void LoadOrCreateQuestConfigs()
        {
            string filePath = Path.Combine(_configPath, Constants.QuestConfigFile);

            if (File.Exists(filePath))
            {
                try
                {
                    string json = File.ReadAllText(filePath);
                    var wrapper = JsonUtility.FromJson<QuestConfigWrapper>(json);
                    if (wrapper?.Quests != null)
                        _questDefinitions = wrapper.Quests;
                }
                catch (Exception e)
                {
                    Debug.LogError($"[VillageLife] Failed to load quest config: {e.Message}");
                }
            }

            // Validate loaded data — regenerate if empty or corrupt (e.g. from prior serialization bug)
            bool questsValid = _questDefinitions.Count > 0 &&
                               _questDefinitions.Any(q => !string.IsNullOrEmpty(q.QuestId));
            if (!questsValid)
            {
                _questDefinitions.Clear();
                CreateDefaultQuests();
                SaveQuestConfigs(filePath);
            }
        }

        private static void CreateDefaultQuests()
        {
            // Meadows quests (5)
            _questDefinitions.Add(new QuestDefinition
            {
                QuestId = "meadows_boar_hunt",
                Title = "Boar Trouble",
                Description = "The boars are getting too bold. Thin the herd.",
                Biome = "meadows",
                Objectives = new List<ObjectiveDefinition>
                {
                    new ObjectiveDefinition { Type = "kill", Target = "Boar", Count = 5 }
                },
                Rewards = new List<QuestReward>
                {
                    new QuestReward { Type = "item", ItemName = "Coins", Amount = 50 },
                    new QuestReward { Type = "item", ItemName = "LeatherScraps", Amount = 10 }
                },
                CooldownHours = 24,
                Dialog = new QuestDialog
                {
                    Offer = "Those boars trampled my garden again! Can you deal with them?",
                    Progress = "Still hearing oinking out there...",
                    Complete = "Peace and quiet at last! Here, you earned this."
                }
            });

            _questDefinitions.Add(new QuestDefinition
            {
                QuestId = "meadows_deer_hunt",
                Title = "Venison for the Village",
                Description = "We need meat for the village. Hunt some deer.",
                Biome = "meadows",
                Objectives = new List<ObjectiveDefinition>
                {
                    new ObjectiveDefinition { Type = "kill", Target = "Deer", Count = 3 }
                },
                Rewards = new List<QuestReward>
                {
                    new QuestReward { Type = "item", ItemName = "Coins", Amount = 40 },
                    new QuestReward { Type = "item", ItemName = "CookedMeat", Amount = 5 }
                },
                CooldownHours = 12,
                Dialog = new QuestDialog
                {
                    Offer = "The village pantry is looking bare. Could you bag a few deer?",
                    Progress = "Any luck with the hunt?",
                    Complete = "Excellent! That'll feed us for days. Here's your share."
                }
            });

            _questDefinitions.Add(new QuestDefinition
            {
                QuestId = "meadows_wood_gathering",
                Title = "Timber!",
                Description = "We need wood for repairs. Gather some logs.",
                Biome = "meadows",
                Objectives = new List<ObjectiveDefinition>
                {
                    new ObjectiveDefinition { Type = "gather", Target = "Wood", Count = 30 }
                },
                Rewards = new List<QuestReward>
                {
                    new QuestReward { Type = "item", ItemName = "Coins", Amount = 30 },
                    new QuestReward { Type = "item", ItemName = "Resin", Amount = 10 }
                },
                CooldownHours = 6,
                Dialog = new QuestDialog
                {
                    Offer = "The fences need mending and we're short on lumber. Mind gathering some wood?",
                    Progress = "Got enough wood yet?",
                    Complete = "Perfect! These logs will serve us well. Take this for your trouble."
                }
            });

            _questDefinitions.Add(new QuestDefinition
            {
                QuestId = "meadows_neck_menace",
                Title = "Neck Menace",
                Description = "Necks are terrorizing the shoreline. Drive them back.",
                Biome = "meadows",
                Objectives = new List<ObjectiveDefinition>
                {
                    new ObjectiveDefinition { Type = "kill", Target = "Neck", Count = 8 }
                },
                Rewards = new List<QuestReward>
                {
                    new QuestReward { Type = "item", ItemName = "Coins", Amount = 35 },
                    new QuestReward { Type = "item", ItemName = "NeckTail", Amount = 5 }
                },
                CooldownHours = 18,
                Dialog = new QuestDialog
                {
                    Offer = "Those little lizard things by the water are getting aggressive. Clear them out?",
                    Progress = "Still seeing them skulking about...",
                    Complete = "The shore is safe again! Thanks, warrior."
                }
            });

            _questDefinitions.Add(new QuestDefinition
            {
                QuestId = "meadows_build_shelter",
                Title = "A Roof Over Our Heads",
                Description = "Help establish the village by building a workbench.",
                Biome = "meadows",
                Objectives = new List<ObjectiveDefinition>
                {
                    new ObjectiveDefinition { Type = "build", Target = "piece_workbench", Count = 1 }
                },
                Rewards = new List<QuestReward>
                {
                    new QuestReward { Type = "item", ItemName = "Coins", Amount = 25 },
                    new QuestReward { Type = "item", ItemName = "Flint", Amount = 10 }
                },
                CooldownHours = 48,
                Dialog = new QuestDialog
                {
                    Offer = "We need a proper workbench to start building. Can you set one up?",
                    Progress = "Has the workbench been built?",
                    Complete = "Now we're in business! The village grows stronger."
                }
            });

            // Black Forest quests (5)
            _questDefinitions.Add(new QuestDefinition
            {
                QuestId = "blackforest_greydwarf_clear",
                Title = "Greydwarf Infestation",
                Description = "Greydwarves are encroaching on our territory. Push them back.",
                Biome = "blackforest",
                Objectives = new List<ObjectiveDefinition>
                {
                    new ObjectiveDefinition { Type = "kill", Target = "Greydwarf", Count = 10 }
                },
                Rewards = new List<QuestReward>
                {
                    new QuestReward { Type = "item", ItemName = "Coins", Amount = 80 },
                    new QuestReward { Type = "item", ItemName = "GreydwarfEye", Amount = 10 }
                },
                CooldownHours = 24,
                Dialog = new QuestDialog
                {
                    Offer = "The forest is crawling with greydwarves. We need someone to thin their numbers.",
                    Progress = "Be careful in those woods...",
                    Complete = "The forest is a bit safer now. Well fought!"
                }
            });

            _questDefinitions.Add(new QuestDefinition
            {
                QuestId = "blackforest_core_wood",
                Title = "Heart of the Forest",
                Description = "We need core wood for advanced building. Chop some pine trees.",
                Biome = "blackforest",
                Objectives = new List<ObjectiveDefinition>
                {
                    new ObjectiveDefinition { Type = "gather", Target = "RoundLog", Count = 20 }
                },
                Rewards = new List<QuestReward>
                {
                    new QuestReward { Type = "item", ItemName = "Coins", Amount = 60 },
                    new QuestReward { Type = "item", ItemName = "BronzeNails", Amount = 20 }
                },
                CooldownHours = 12,
                Dialog = new QuestDialog
                {
                    Offer = "Pine trees hold strong wood in their cores. We need some for the forge.",
                    Progress = "Those big pines aren't going to chop themselves!",
                    Complete = "Solid wood! This will make fine beams."
                }
            });

            _questDefinitions.Add(new QuestDefinition
            {
                QuestId = "blackforest_troll_slayer",
                Title = "Troll Trouble",
                Description = "A troll has been spotted near the village. Deal with it.",
                Biome = "blackforest",
                Objectives = new List<ObjectiveDefinition>
                {
                    new ObjectiveDefinition { Type = "kill", Target = "Troll", Count = 1 }
                },
                Rewards = new List<QuestReward>
                {
                    new QuestReward { Type = "item", ItemName = "Coins", Amount = 150 },
                    new QuestReward { Type = "item", ItemName = "TrollHide", Amount = 5 }
                },
                CooldownHours = 48,
                Dialog = new QuestDialog
                {
                    Offer = "There's a troll lurking nearby. Big, blue, and mean. Can you handle it?",
                    Progress = "That troll still out there? I can feel the ground shaking...",
                    Complete = "You actually did it! The village is safe. You're a legend!"
                }
            });

            _questDefinitions.Add(new QuestDefinition
            {
                QuestId = "blackforest_copper_delivery",
                Title = "Copper Shipment",
                Description = "The smith needs copper ore. Deliver some from the mines.",
                Biome = "blackforest",
                Objectives = new List<ObjectiveDefinition>
                {
                    new ObjectiveDefinition { Type = "deliver", Target = "CopperOre", Count = 10 }
                },
                Rewards = new List<QuestReward>
                {
                    new QuestReward { Type = "item", ItemName = "Coins", Amount = 100 },
                    new QuestReward { Type = "item", ItemName = "Bronze", Amount = 3 }
                },
                CooldownHours = 24,
                Dialog = new QuestDialog
                {
                    Offer = "The forge is hungry for copper. Bring me 10 ore and I'll make it worth your while.",
                    Progress = "Need that copper! The forge runs cold without it.",
                    Complete = "Now that's what I like to see! Proper ore. Here, take some refined bronze."
                }
            });

            _questDefinitions.Add(new QuestDefinition
            {
                QuestId = "blackforest_skeleton_dungeon",
                Title = "Bones of the Fallen",
                Description = "Clear the skeleton guards from a burial chamber.",
                Biome = "blackforest",
                Objectives = new List<ObjectiveDefinition>
                {
                    new ObjectiveDefinition { Type = "kill", Target = "Skeleton", Count = 8 }
                },
                Rewards = new List<QuestReward>
                {
                    new QuestReward { Type = "item", ItemName = "Coins", Amount = 90 },
                    new QuestReward { Type = "item", ItemName = "BoneFragments", Amount = 15 }
                },
                CooldownHours = 36,
                Dialog = new QuestDialog
                {
                    Offer = "The dead won't stay buried in these parts. Clear out a burial chamber for me?",
                    Progress = "Still rattling around in there, are they?",
                    Complete = "The spirits can rest now. You've done good work."
                }
            });

            // Swamp quests (5)
            _questDefinitions.Add(new QuestDefinition
            {
                QuestId = "swamp_draugr_patrol",
                Title = "Draugr Patrol",
                Description = "The swamp is infested with draugr. Reduce their numbers.",
                Biome = "swamp",
                Objectives = new List<ObjectiveDefinition>
                {
                    new ObjectiveDefinition { Type = "kill", Target = "Draugr", Count = 12 }
                },
                Rewards = new List<QuestReward>
                {
                    new QuestReward { Type = "item", ItemName = "Coins", Amount = 120 },
                    new QuestReward { Type = "item", ItemName = "Entrails", Amount = 10 }
                },
                CooldownHours = 24,
                Dialog = new QuestDialog
                {
                    Offer = "The draugr are everywhere in the swamp. We need someone to push them back.",
                    Progress = "The swamp still reeks of the undead...",
                    Complete = "Fewer draugr means safer roads. Here's your payment."
                }
            });

            _questDefinitions.Add(new QuestDefinition
            {
                QuestId = "swamp_iron_scrap",
                Title = "Scrap Metal",
                Description = "Salvage iron scraps from the muddy crypts.",
                Biome = "swamp",
                Objectives = new List<ObjectiveDefinition>
                {
                    new ObjectiveDefinition { Type = "gather", Target = "IronScrap", Count = 15 }
                },
                Rewards = new List<QuestReward>
                {
                    new QuestReward { Type = "item", ItemName = "Coins", Amount = 150 },
                    new QuestReward { Type = "item", ItemName = "Iron", Amount = 5 }
                },
                CooldownHours = 24,
                Dialog = new QuestDialog
                {
                    Offer = "The crypts hold iron from a forgotten age. Brave the dark and bring me scraps.",
                    Progress = "Those crypts won't explore themselves. Find any iron?",
                    Complete = "Fine scraps! I'll smelt these into something useful. Your cut's right here."
                }
            });

            _questDefinitions.Add(new QuestDefinition
            {
                QuestId = "swamp_leech_cleanup",
                Title = "Leech Cleanup",
                Description = "The waterways are infested with leeches. Kill them.",
                Biome = "swamp",
                Objectives = new List<ObjectiveDefinition>
                {
                    new ObjectiveDefinition { Type = "kill", Target = "Leech", Count = 6 }
                },
                Rewards = new List<QuestReward>
                {
                    new QuestReward { Type = "item", ItemName = "Coins", Amount = 75 },
                    new QuestReward { Type = "item", ItemName = "Bloodbag", Amount = 8 }
                },
                CooldownHours = 18,
                Dialog = new QuestDialog
                {
                    Offer = "I can't go near the water without those leeches trying to drain me dry!",
                    Progress = "Are those slimy things still out there?",
                    Complete = "Finally! I can walk the shores again. Take these blood bags — they're useful for mead."
                }
            });

            _questDefinitions.Add(new QuestDefinition
            {
                QuestId = "swamp_sausage_delivery",
                Title = "Sausage Supply",
                Description = "The village needs food. Deliver sausages to keep morale up.",
                Biome = "swamp",
                Objectives = new List<ObjectiveDefinition>
                {
                    new ObjectiveDefinition { Type = "deliver", Target = "Sausages", Count = 5 }
                },
                Rewards = new List<QuestReward>
                {
                    new QuestReward { Type = "item", ItemName = "Coins", Amount = 100 },
                    new QuestReward { Type = "item", ItemName = "MeadHealthMedium", Amount = 3 }
                },
                CooldownHours = 12,
                Dialog = new QuestDialog
                {
                    Offer = "We're hungry and the swamp isn't exactly a breadbasket. Got any sausages?",
                    Progress = "My stomach is growling louder than the draugr...",
                    Complete = "Sausages! Now that's a proper meal. Have some mead as thanks."
                }
            });

            _questDefinitions.Add(new QuestDefinition
            {
                QuestId = "swamp_blob_exterminate",
                Title = "Blob Extermination",
                Description = "The blobs are spreading toxic ooze everywhere. Eliminate them.",
                Biome = "swamp",
                Objectives = new List<ObjectiveDefinition>
                {
                    new ObjectiveDefinition { Type = "kill", Target = "Blob", Count = 8 }
                },
                Rewards = new List<QuestReward>
                {
                    new QuestReward { Type = "item", ItemName = "Coins", Amount = 90 },
                    new QuestReward { Type = "item", ItemName = "Ooze", Amount = 10 }
                },
                CooldownHours = 24,
                Dialog = new QuestDialog
                {
                    Offer = "Those green blobs are poisoning everything! Get rid of as many as you can.",
                    Progress = "Can still smell that poison in the air...",
                    Complete = "The air smells slightly less toxic. Good work! Here's some ooze — might be useful for something."
                }
            });
        }

        private static void SaveQuestConfigs(string filePath)
        {
            try
            {
                var wrapper = new QuestConfigWrapper { Quests = _questDefinitions };
                string json = JsonUtility.ToJson(wrapper, true);
                File.WriteAllText(filePath, json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[VillageLife] Failed to save quest config: {e.Message}");
            }
        }

        #endregion

        #region Dialog Config

        public static DialogConfig GetDialogConfig() => _dialogConfig;

        private static void LoadOrCreateDialogConfig()
        {
            string filePath = Path.Combine(_configPath, Constants.DialogConfigFile);

            if (File.Exists(filePath))
            {
                try
                {
                    string json = File.ReadAllText(filePath);
                    _dialogConfig = JsonUtility.FromJson<DialogConfig>(json);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[VillageLife] Failed to load dialog config: {e.Message}");
                }
            }

            // Validate loaded data — regenerate if empty or corrupt
            if (_dialogConfig == null || _dialogConfig.Lines == null || _dialogConfig.Lines.Count == 0)
            {
                _dialogConfig = CreateDefaultDialogConfig();
                try
                {
                    string json = JsonUtility.ToJson(_dialogConfig, true);
                    File.WriteAllText(filePath, json);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[VillageLife] Failed to save dialog config: {e.Message}");
                }
            }
        }

        private static DialogConfig CreateDefaultDialogConfig()
        {
            var config = new DialogConfig();

            // Greeting lines
            config.Lines.Add(new DialogLineConfig { Role = "any", Context = "greeting", Line = "Welcome to our village!", Weight = 1f });
            config.Lines.Add(new DialogLineConfig { Role = "any", Context = "greeting", Line = "Good to see you, traveler!", Weight = 1f });
            config.Lines.Add(new DialogLineConfig { Role = "merchant", Context = "greeting", Line = "Looking to trade? I've got the goods!", Weight = 1f });
            config.Lines.Add(new DialogLineConfig { Role = "guard", Context = "greeting", Line = "Stay safe out there. I've got watch.", Weight = 1f });
            config.Lines.Add(new DialogLineConfig { Role = "quest_giver", Context = "greeting", Line = "Ah, just the adventurer I was looking for!", Weight = 1f });

            // Idle lines
            config.Lines.Add(new DialogLineConfig { Role = "any", Context = "idle", Line = "Beautiful day, isn't it?", Weight = 1f });
            config.Lines.Add(new DialogLineConfig { Role = "any", Context = "idle", Line = "The village is coming along nicely.", Weight = 1f });
            config.Lines.Add(new DialogLineConfig { Role = "merchant", Context = "idle", Line = "I should restock soon...", Weight = 1f });
            config.Lines.Add(new DialogLineConfig { Role = "guard", Context = "idle", Line = "All quiet on the perimeter.", Weight = 1f });
            config.Lines.Add(new DialogLineConfig { Role = "villager", Context = "idle", Line = "Another day in the tenth world.", Weight = 1f });

            // Morning lines
            config.Lines.Add(new DialogLineConfig { Role = "any", Context = "morning", Line = "Good morning! Fresh start to the day.", Weight = 1f });
            config.Lines.Add(new DialogLineConfig { Role = "any", Context = "morning", Line = "The sun rises on our village once more.", Weight = 1f });

            // Evening lines
            config.Lines.Add(new DialogLineConfig { Role = "any", Context = "evening", Line = "Getting dark... best head inside soon.", Weight = 1f });
            config.Lines.Add(new DialogLineConfig { Role = "any", Context = "evening", Line = "Another day survived in these lands.", Weight = 1f });

            // Rain lines
            config.Lines.Add(new DialogLineConfig { Role = "any", Context = "rain", Line = "This rain won't let up, will it?", Weight = 1f });
            config.Lines.Add(new DialogLineConfig { Role = "any", Context = "rain", Line = "At least the crops will be happy...", Weight = 1f });

            // Combat lines
            config.Lines.Add(new DialogLineConfig { Role = "any", Context = "combat", Line = "Enemies! Run!", Weight = 1f });
            config.Lines.Add(new DialogLineConfig { Role = "guard", Context = "combat", Line = "To arms! Defend the village!", Weight = 1.5f });
            config.Lines.Add(new DialogLineConfig { Role = "villager", Context = "combat", Line = "Help! Someone protect us!", Weight = 1f });
            config.Lines.Add(new DialogLineConfig { Role = "merchant", Context = "combat", Line = "My wares! Someone stop them!", Weight = 1f });

            return config;
        }

        #endregion
    }

    #region Config Data Classes

    [Serializable]
    public class ShopConfigWrapper
    {
        public List<ShopConfig> Merchants = new List<ShopConfig>();
    }

    [Serializable]
    public class ShopConfig
    {
        public string ShopType;
        public string DisplayName;
        public string Currency = "Coins";
        public List<ShopItemConfig> Items = new List<ShopItemConfig>();
    }

    [Serializable]
    public class ShopItemConfig
    {
        public string ItemName;
        public int BuyPrice;
        public int SellPrice;
        public int MaxStock;
    }

    [Serializable]
    public class QuestConfigWrapper
    {
        public List<QuestDefinition> Quests = new List<QuestDefinition>();
    }

    [Serializable]
    public class DialogConfig
    {
        public List<DialogLineConfig> Lines = new List<DialogLineConfig>();
    }

    [Serializable]
    public class DialogLineConfig
    {
        public string Role;         // "merchant", "quest_giver", "guard", "villager", "any"
        public string Context;      // "greeting", "morning", "evening", "rain", "combat", "idle"
        public string Line;
        public float Weight = 1f;
    }

    #endregion
}
