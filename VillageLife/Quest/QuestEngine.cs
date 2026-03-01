using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VillageLife.Config;

namespace VillageLife.Quest
{
    /// <summary>
    /// Central quest management system.
    /// Handles quest state machine, objective tracking, and per-player persistence.
    /// </summary>
    public static class QuestEngine
    {
        private static readonly Dictionary<long, PlayerQuestData> _playerQuests =
            new Dictionary<long, PlayerQuestData>();

        private static List<QuestDefinition> _allQuests = new List<QuestDefinition>();
        private static float _tickTimer;
        private const float TickInterval = 2f;

        public static void Initialize()
        {
            _playerQuests.Clear();
            _allQuests = ConfigManager.GetAllQuestDefinitions();
        }

        public static void Tick()
        {
            if (!Plugin.VillageLifePlugin.EnableQuestSystem.Value)
                return;

            _tickTimer += Time.deltaTime;
            if (_tickTimer < TickInterval) return;
            _tickTimer = 0f;

            // Update active quests for local player
            var player = Player.m_localPlayer;
            if (player == null) return;

            var data = GetOrCreatePlayerData(player);
            foreach (var quest in data.ActiveQuests)
            {
                quest.UpdateObjectives(player);
            }
        }

        public static PlayerQuestData GetOrCreatePlayerData(Player player)
        {
            long playerId = player.GetPlayerID();
            if (!_playerQuests.TryGetValue(playerId, out var data))
            {
                data = new PlayerQuestData(playerId);
                data.LoadFromPlayer(player);
                _playerQuests[playerId] = data;
            }
            return data;
        }

        /// <summary>
        /// Get quests available to a player from a specific quest pool (biome).
        /// </summary>
        public static List<QuestDefinition> GetAvailableQuests(Player player, string questPool)
        {
            var data = GetOrCreatePlayerData(player);

            return _allQuests
                .Where(q => q.Biome.Equals(questPool, StringComparison.OrdinalIgnoreCase) &&
                           !data.IsQuestActive(q.QuestId) &&
                           !data.IsQuestOnCooldown(q.QuestId))
                .ToList();
        }

        /// <summary>
        /// Get active quests that were accepted from a specific NPC.
        /// </summary>
        public static List<ActiveQuest> GetActiveQuestsForNPC(Player player, ZDOID npcId)
        {
            var data = GetOrCreatePlayerData(player);
            return data.ActiveQuests
                .Where(q => q.GiverNPCId == npcId)
                .ToList();
        }

        /// <summary>
        /// Player accepts a quest from an NPC.
        /// </summary>
        public static ActiveQuest AcceptQuest(Player player, QuestDefinition definition, ZDOID npcId)
        {
            var data = GetOrCreatePlayerData(player);

            if (data.IsQuestActive(definition.QuestId))
                return null;

            var activeQuest = new ActiveQuest(definition, npcId);
            data.ActiveQuests.Add(activeQuest);
            data.SaveToPlayer(player);

            return activeQuest;
        }

        /// <summary>
        /// Player completes a quest and receives rewards.
        /// </summary>
        public static bool CompleteQuest(Player player, ActiveQuest quest)
        {
            if (!quest.IsReadyToComplete()) return false;

            var data = GetOrCreatePlayerData(player);

            // Grant rewards
            foreach (var reward in quest.Definition.Rewards)
            {
                GrantReward(player, reward);
            }

            // Move to completed + set cooldown
            data.ActiveQuests.Remove(quest);
            data.CompletedQuests[quest.Definition.QuestId] = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            data.SaveToPlayer(player);

            return true;
        }

        private static void GrantReward(Player player, QuestReward reward)
        {
            switch (reward.Type)
            {
                case "item":
                    var prefab = ObjectDB.instance?.GetItemPrefab(reward.ItemName);
                    if (prefab != null)
                        player.GetInventory().AddItem(prefab, reward.Amount);
                    break;
            }
        }

        /// <summary>
        /// Notify the quest engine that a creature was killed.
        /// Called from Harmony patches.
        /// </summary>
        public static void OnCreatureKilled(string creatureName, Vector3 position, Player attacker)
        {
            if (attacker == null) return;

            var data = GetOrCreatePlayerData(attacker);
            foreach (var quest in data.ActiveQuests)
            {
                foreach (var obj in quest.Objectives)
                {
                    if (obj is KillObjective killObj && killObj.TargetCreature == creatureName)
                    {
                        killObj.IncrementProgress(1);
                    }
                }
            }
            data.SaveToPlayer(attacker);
        }

        /// <summary>
        /// Notify the quest engine that an item was gathered.
        /// </summary>
        public static void OnItemGathered(string itemName, int amount, Player player)
        {
            if (player == null) return;

            var data = GetOrCreatePlayerData(player);
            foreach (var quest in data.ActiveQuests)
            {
                foreach (var obj in quest.Objectives)
                {
                    if (obj is GatherObjective gatherObj && gatherObj.TargetItem == itemName)
                    {
                        gatherObj.IncrementProgress(amount);
                    }
                }
            }
            data.SaveToPlayer(player);
        }

        /// <summary>
        /// Notify the quest engine that a location was discovered.
        /// </summary>
        public static void OnLocationDiscovered(string locationName, Player player)
        {
            if (player == null) return;

            var data = GetOrCreatePlayerData(player);
            foreach (var quest in data.ActiveQuests)
            {
                foreach (var obj in quest.Objectives)
                {
                    if (obj is ExploreObjective exploreObj && exploreObj.TargetLocation == locationName)
                    {
                        exploreObj.MarkComplete();
                    }
                }
            }
            data.SaveToPlayer(player);
        }

        /// <summary>
        /// Notify the quest engine that a structure was built.
        /// </summary>
        public static void OnStructureBuilt(string pieceName, Player player)
        {
            if (player == null) return;

            var data = GetOrCreatePlayerData(player);
            foreach (var quest in data.ActiveQuests)
            {
                foreach (var obj in quest.Objectives)
                {
                    if (obj is BuildObjective buildObj && buildObj.TargetPiece == pieceName)
                    {
                        buildObj.IncrementProgress(1);
                    }
                }
            }
            data.SaveToPlayer(player);
        }

        /// <summary>
        /// Get all active quests for the local player (for HUD display).
        /// </summary>
        public static List<ActiveQuest> GetAllActiveQuests(Player player)
        {
            var data = GetOrCreatePlayerData(player);
            return data.ActiveQuests.ToList();
        }
    }
}
