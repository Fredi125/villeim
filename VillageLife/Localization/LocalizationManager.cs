using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VillageLife.Util;

namespace VillageLife.Localization
{
    /// <summary>
    /// Localization system that loads translated strings from JSON files.
    /// Supports English (en) and French (fr). Falls back to English for missing keys.
    /// Integrates with Valheim's built-in Localization system.
    /// </summary>
    public static class LocalizationManager
    {
        private static Dictionary<string, Dictionary<string, string>> _translations =
            new Dictionary<string, Dictionary<string, string>>();

        private static string _currentLanguage = "en";

        public static void Initialize()
        {
            _currentLanguage = Plugin.VillageLifePlugin.Language.Value;

            // Register built-in translations
            RegisterEnglish();
            RegisterFrench();

            // Load any custom translation files
            LoadCustomTranslations();

            // Register with Valheim's localization
            RegisterWithValheim();
        }

        /// <summary>
        /// Get a translated string by key.
        /// Falls back to English, then returns the key itself if not found.
        /// </summary>
        public static string Get(string key)
        {
            // Try current language
            if (_translations.TryGetValue(_currentLanguage, out var langDict))
            {
                if (langDict.TryGetValue(key, out var value))
                    return value;
            }

            // Fall back to English
            if (_currentLanguage != "en" && _translations.TryGetValue("en", out var enDict))
            {
                if (enDict.TryGetValue(key, out var value))
                    return value;
            }

            return key;
        }

        private static void RegisterWithValheim()
        {
            // Add all translations to Valheim's localization system
            if (!_translations.TryGetValue("en", out var enDict)) return;

            foreach (var kvp in enDict)
            {
                // Valheim uses $ prefix for localization keys
                string key = kvp.Key.StartsWith("$") ? kvp.Key.Substring(1) : kvp.Key;
                Jotunn.Managers.LocalizationManager.Instance.AddTranslation(
                    new Jotunn.Configs.LocalizationConfig("English")
                    {
                        Translations = { { key, kvp.Value } }
                    });
            }

            if (_translations.TryGetValue("fr", out var frDict))
            {
                foreach (var kvp in frDict)
                {
                    string key = kvp.Key.StartsWith("$") ? kvp.Key.Substring(1) : kvp.Key;
                    Jotunn.Managers.LocalizationManager.Instance.AddTranslation(
                        new Jotunn.Configs.LocalizationConfig("French")
                        {
                            Translations = { { key, kvp.Value } }
                        });
                }
            }
        }

        private static void LoadCustomTranslations()
        {
            string locPath = Path.Combine(BepInEx.Paths.ConfigPath, Constants.ConfigFolderName, Constants.LocalizationFolder);
            if (!Directory.Exists(locPath)) return;

            foreach (var file in Directory.GetFiles(locPath, "*.json"))
            {
                try
                {
                    string langCode = Path.GetFileNameWithoutExtension(file);
                    string json = File.ReadAllText(file);
                    var dict = ParseSimpleJson(json);

                    if (!_translations.ContainsKey(langCode))
                        _translations[langCode] = new Dictionary<string, string>();

                    foreach (var kvp in dict)
                        _translations[langCode][kvp.Key] = kvp.Value;
                }
                catch (Exception e)
                {
                    Debug.LogError($"[VillageLife] Failed to load translation {file}: {e.Message}");
                }
            }
        }

        /// <summary>
        /// Simple JSON key-value parser for flat translation files.
        /// </summary>
        private static Dictionary<string, string> ParseSimpleJson(string json)
        {
            var result = new Dictionary<string, string>();

            // Simple parser for {"key": "value", ...} format
            json = json.Trim().TrimStart('{').TrimEnd('}');
            var pairs = json.Split(new[] { "\",\"", "\", \"" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var pair in pairs)
            {
                var clean = pair.Trim().Trim(',').Trim();
                var colonIdx = clean.IndexOf(':');
                if (colonIdx <= 0) continue;

                string key = clean.Substring(0, colonIdx).Trim().Trim('"');
                string value = clean.Substring(colonIdx + 1).Trim().Trim('"');
                result[key] = value;
            }

            return result;
        }

        #region Built-in Translations

        private static void RegisterEnglish()
        {
            _translations["en"] = new Dictionary<string, string>
            {
                // Pieces
                { "$piece_vl_npc", "Village NPC" },
                { "$piece_vl_npc_desc", "A friendly villager for your settlement." },
                { "$piece_vl_villagehall", "Village Hall" },
                { "$piece_vl_villagehall_desc", "A workshop for creating and customizing village NPCs." },

                // Roles
                { "$role_merchant", "Merchant" },
                { "$role_quest_giver", "Quest Giver" },
                { "$role_guard", "Guard" },
                { "$role_villager", "Villager" },

                // NPC
                { "$npc_vl_villager", "Villager" },

                // UI - NPC Creation
                { "$ui_npc_workshop", "NPC Workshop" },
                { "$ui_name", "Name" },
                { "$ui_role", "Role" },
                { "$ui_gender", "Gender" },
                { "$ui_male", "Male" },
                { "$ui_female", "Female" },
                { "$ui_hair", "Hair Style" },
                { "$ui_beard", "Beard Style" },
                { "$ui_create", "Create NPC" },
                { "$ui_cancel", "Cancel" },
                { "$ui_close", "Close" },

                // UI - Trade
                { "$ui_trade", "Trade" },
                { "$ui_buy", "Buy" },
                { "$ui_sell", "Sell" },
                { "$ui_coins", "Coins" },
                { "$ui_price", "Price" },
                { "$ui_stock", "Stock" },
                { "$ui_own", "Own" },

                // UI - Quests
                { "$ui_quests", "Quests" },
                { "$ui_quest_available", "Available Quests" },
                { "$ui_quest_active", "Active Quests" },
                { "$ui_quest_complete", "Quest Complete!" },
                { "$ui_accept_quest", "Accept Quest" },
                { "$ui_collect_rewards", "Collect Rewards" },
                { "$ui_objectives", "Objectives" },
                { "$ui_rewards", "Rewards" },
                { "$ui_no_quests", "No quests available right now. Check back later." },

                // Messages
                { "$msg_npc_limit", "NPC limit reached!" },
                { "$msg_not_enough_materials", "Not enough materials!" },
                { "$msg_quest_accepted", "Quest accepted!" },
                { "$msg_quest_complete", "Quest complete!" },
                { "$msg_trade_failed", "Trade failed! Check your inventory and coins." },
                { "$msg_npc_joined", "has joined the village!" }
            };
        }

        private static void RegisterFrench()
        {
            _translations["fr"] = new Dictionary<string, string>
            {
                // Pieces
                { "$piece_vl_npc", "PNJ du Village" },
                { "$piece_vl_npc_desc", "Un villageois amical pour votre colonie." },
                { "$piece_vl_villagehall", "Hôtel de Ville" },
                { "$piece_vl_villagehall_desc", "Un atelier pour créer et personnaliser les PNJ du village." },

                // Roles
                { "$role_merchant", "Marchand" },
                { "$role_quest_giver", "Donneur de Quêtes" },
                { "$role_guard", "Garde" },
                { "$role_villager", "Villageois" },

                // NPC
                { "$npc_vl_villager", "Villageois" },

                // UI - NPC Creation
                { "$ui_npc_workshop", "Atelier PNJ" },
                { "$ui_name", "Nom" },
                { "$ui_role", "Rôle" },
                { "$ui_gender", "Genre" },
                { "$ui_male", "Homme" },
                { "$ui_female", "Femme" },
                { "$ui_hair", "Coiffure" },
                { "$ui_beard", "Barbe" },
                { "$ui_create", "Créer PNJ" },
                { "$ui_cancel", "Annuler" },
                { "$ui_close", "Fermer" },

                // UI - Trade
                { "$ui_trade", "Commerce" },
                { "$ui_buy", "Acheter" },
                { "$ui_sell", "Vendre" },
                { "$ui_coins", "Pièces" },
                { "$ui_price", "Prix" },
                { "$ui_stock", "Stock" },
                { "$ui_own", "Possédé" },

                // UI - Quests
                { "$ui_quests", "Quêtes" },
                { "$ui_quest_available", "Quêtes Disponibles" },
                { "$ui_quest_active", "Quêtes Actives" },
                { "$ui_quest_complete", "Quête Terminée !" },
                { "$ui_accept_quest", "Accepter la Quête" },
                { "$ui_collect_rewards", "Récupérer les Récompenses" },
                { "$ui_objectives", "Objectifs" },
                { "$ui_rewards", "Récompenses" },
                { "$ui_no_quests", "Aucune quête disponible pour le moment. Revenez plus tard." },

                // Messages
                { "$msg_npc_limit", "Limite de PNJ atteinte !" },
                { "$msg_not_enough_materials", "Pas assez de matériaux !" },
                { "$msg_quest_accepted", "Quête acceptée !" },
                { "$msg_quest_complete", "Quête terminée !" },
                { "$msg_trade_failed", "Échange échoué ! Vérifiez votre inventaire et vos pièces." },
                { "$msg_npc_joined", "a rejoint le village !" }
            };
        }

        #endregion
    }
}
