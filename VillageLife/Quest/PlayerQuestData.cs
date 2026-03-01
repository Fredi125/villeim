using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VillageLife.Config;

namespace VillageLife.Quest
{
    /// <summary>
    /// Per-player quest state. Persisted in Player.m_customData.
    /// </summary>
    public class PlayerQuestData
    {
        private const string SaveKey = "villagelife_quests";
        private const string CompletedKey = "villagelife_completed";

        public long PlayerId { get; }
        public List<ActiveQuest> ActiveQuests { get; } = new List<ActiveQuest>();
        public Dictionary<string, long> CompletedQuests { get; } = new Dictionary<string, long>();

        public PlayerQuestData(long playerId)
        {
            PlayerId = playerId;
        }

        public bool IsQuestActive(string questId)
        {
            return ActiveQuests.Any(q => q.Definition.QuestId == questId);
        }

        public bool IsQuestOnCooldown(string questId)
        {
            if (!CompletedQuests.TryGetValue(questId, out long completedTime))
                return false;

            var questDef = ConfigManager.GetQuestDefinition(questId);
            if (questDef == null) return false;

            long cooldownSeconds = questDef.CooldownHours * 3600;
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            return (now - completedTime) < cooldownSeconds;
        }

        public void SaveToPlayer(Player player)
        {
            if (player == null) return;

            // Serialize active quests
            var activeData = new StringBuilder();
            foreach (var quest in ActiveQuests)
            {
                if (activeData.Length > 0) activeData.Append("||");
                activeData.Append(quest.Serialize());
            }
            player.m_customData[SaveKey] = activeData.ToString();

            // Serialize completed quests
            var completedData = new StringBuilder();
            foreach (var kvp in CompletedQuests)
            {
                if (completedData.Length > 0) completedData.Append("||");
                completedData.Append($"{kvp.Key},{kvp.Value}");
            }
            player.m_customData[CompletedKey] = completedData.ToString();
        }

        public void LoadFromPlayer(Player player)
        {
            if (player == null) return;

            ActiveQuests.Clear();
            CompletedQuests.Clear();

            // Load active quests
            if (player.m_customData.TryGetValue(SaveKey, out string activeData) &&
                !string.IsNullOrEmpty(activeData))
            {
                var parts = activeData.Split(new[] { "||" }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var part in parts)
                {
                    var quest = DeserializeActiveQuest(part);
                    if (quest != null)
                        ActiveQuests.Add(quest);
                }
            }

            // Load completed quests
            if (player.m_customData.TryGetValue(CompletedKey, out string completedData) &&
                !string.IsNullOrEmpty(completedData))
            {
                var parts = completedData.Split(new[] { "||" }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var part in parts)
                {
                    var kv = part.Split(',');
                    if (kv.Length >= 2 && long.TryParse(kv[1], out long timestamp))
                    {
                        CompletedQuests[kv[0]] = timestamp;
                    }
                }
            }
        }

        private ActiveQuest DeserializeActiveQuest(string data)
        {
            try
            {
                var parts = data.Split(',');
                if (parts.Length < 4) return null;

                string questId = parts[0];
                var definition = ConfigManager.GetQuestDefinition(questId);
                if (definition == null) return null;

                long npcUserId = long.Parse(parts[1]);
                uint npcId = uint.Parse(parts[2]);
                var npcZDOID = new ZDOID(npcUserId, npcId);

                var quest = new ActiveQuest(definition, npcZDOID);
                quest.AcceptedTimestamp = long.Parse(parts[3]);

                // Restore objective progress if available
                if (parts.Length > 4)
                {
                    string objectiveData = string.Join(",", parts.Skip(4));
                    RestoreObjectiveProgress(quest, objectiveData);
                }

                return quest;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private void RestoreObjectiveProgress(ActiveQuest quest, string data)
        {
            var objParts = data.Split(';');
            for (int i = 0; i < objParts.Length && i < quest.Objectives.Count; i++)
            {
                var fields = objParts[i].Split('|');
                if (fields.Length < 2) continue;

                string progressData = fields[1];
                var progressFields = progressData.Split(',');

                var obj = quest.Objectives[i];
                if (obj is KillObjective killObj && progressFields.Length >= 3)
                {
                    if (int.TryParse(progressFields[2], out int count))
                        killObj.IncrementProgress(count);
                }
                else if (obj is GatherObjective gatherObj && progressFields.Length >= 3)
                {
                    if (int.TryParse(progressFields[2], out int count))
                        gatherObj.IncrementProgress(count);
                }
                else if (obj is ExploreObjective exploreObj && progressFields.Length >= 2)
                {
                    if (progressFields[1] == "1")
                        exploreObj.MarkComplete();
                }
                else if (obj is BuildObjective buildObj && progressFields.Length >= 3)
                {
                    if (int.TryParse(progressFields[2], out int count))
                        buildObj.IncrementProgress(count);
                }
            }
        }
    }
}
