using System;
using System.Collections.Generic;

namespace VillageLife.Quest
{
    /// <summary>
    /// Static definition of a quest loaded from config.
    /// Uses fields (not properties) so Unity's JsonUtility can serialize them.
    /// </summary>
    [Serializable]
    public class QuestDefinition
    {
        public string QuestId;
        public string Title;
        public string Description;
        public string Biome;
        public List<ObjectiveDefinition> Objectives = new List<ObjectiveDefinition>();
        public List<QuestReward> Rewards = new List<QuestReward>();
        public int CooldownHours = 24;
        public QuestDialog Dialog = new QuestDialog();
    }

    [Serializable]
    public class ObjectiveDefinition
    {
        public string Type;   // "kill", "gather", "deliver", "explore", "build"
        public string Target;  // creature name, item name, location name, piece name
        public int Count = 1;
    }

    [Serializable]
    public class QuestReward
    {
        public string Type = "item";  // "item"
        public string ItemName;
        public int Amount = 1;
    }

    [Serializable]
    public class QuestDialog
    {
        public string Offer = "I have a task for you.";
        public string Progress = "How's that task coming along?";
        public string Complete = "Well done! Here's your reward.";
    }
}
