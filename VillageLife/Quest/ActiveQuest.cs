using System.Collections.Generic;
using System.Linq;

namespace VillageLife.Quest
{
    /// <summary>
    /// An active, in-progress quest instance tied to a specific player.
    /// </summary>
    public class ActiveQuest
    {
        public QuestDefinition Definition { get; }
        public ZDOID GiverNPCId { get; }
        public List<QuestObjective> Objectives { get; } = new List<QuestObjective>();
        public long AcceptedTimestamp { get; set; }

        public ActiveQuest(QuestDefinition definition, ZDOID npcId)
        {
            Definition = definition;
            GiverNPCId = npcId;
            AcceptedTimestamp = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            // Create objective instances from definitions
            foreach (var objDef in definition.Objectives)
            {
                Objectives.Add(CreateObjective(objDef));
            }
        }

        /// <summary>
        /// Check if all objectives are complete.
        /// </summary>
        public bool IsReadyToComplete()
        {
            return Objectives.All(o => o.IsComplete());
        }

        /// <summary>
        /// Get overall progress as a 0-1 fraction.
        /// </summary>
        public float GetOverallProgress()
        {
            if (Objectives.Count == 0) return 1f;
            return Objectives.Average(o => o.GetProgress());
        }

        /// <summary>
        /// Update all objectives (check player inventory for gather/deliver quests, etc.).
        /// </summary>
        public void UpdateObjectives(Player player)
        {
            foreach (var obj in Objectives)
            {
                if (obj is GatherObjective gatherObj)
                {
                    // Sync gather progress with actual inventory count
                    int count = player.GetInventory().CountItems(gatherObj.TargetItem);
                    gatherObj.SyncWithInventory(count);
                }
            }
        }

        private QuestObjective CreateObjective(ObjectiveDefinition def)
        {
            return def.Type.ToLower() switch
            {
                "kill" => new KillObjective(def.Target, def.Count),
                "gather" => new GatherObjective(def.Target, def.Count),
                "deliver" => new DeliverObjective(def.Target, def.Count),
                "explore" => new ExploreObjective(def.Target),
                "build" => new BuildObjective(def.Target, def.Count),
                _ => new KillObjective(def.Target, def.Count)
            };
        }

        /// <summary>
        /// Serialize quest state for persistence.
        /// </summary>
        public string Serialize()
        {
            var objStates = Objectives.Select(o => $"{o.GetType().Name}|{o.SerializeProgress()}");
            return $"{Definition.QuestId},{GiverNPCId.UserID},{GiverNPCId.ID},{AcceptedTimestamp}," +
                   string.Join(";", objStates);
        }
    }
}
