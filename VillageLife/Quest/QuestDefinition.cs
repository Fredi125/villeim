using System.Collections.Generic;

namespace VillageLife.Quest
{
    /// <summary>
    /// Static definition of a quest loaded from config.
    /// </summary>
    public class QuestDefinition
    {
        public string QuestId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Biome { get; set; }
        public List<ObjectiveDefinition> Objectives { get; set; } = new List<ObjectiveDefinition>();
        public List<QuestReward> Rewards { get; set; } = new List<QuestReward>();
        public int CooldownHours { get; set; } = 24;
        public QuestDialog Dialog { get; set; } = new QuestDialog();
    }

    public class ObjectiveDefinition
    {
        public string Type { get; set; }   // "kill", "gather", "deliver", "explore", "build"
        public string Target { get; set; }  // creature name, item name, location name, piece name
        public int Count { get; set; } = 1;
    }

    public class QuestReward
    {
        public string Type { get; set; } = "item";  // "item"
        public string ItemName { get; set; }
        public int Amount { get; set; } = 1;
    }

    public class QuestDialog
    {
        public string Offer { get; set; } = "I have a task for you.";
        public string Progress { get; set; } = "How's that task coming along?";
        public string Complete { get; set; } = "Well done! Here's your reward.";
    }
}
