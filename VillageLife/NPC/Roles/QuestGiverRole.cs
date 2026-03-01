using System.Collections.Generic;
using System.Linq;
using VillageLife.Quest;
using VillageLife.UI;
using VillageLife.Util;

namespace VillageLife.NPC.Roles
{
    /// <summary>
    /// Quest Giver role — NPC offers quests from a configured pool.
    /// Players can accept, track, and turn in quests through this NPC.
    /// </summary>
    public class QuestGiverRole : INPCRole
    {
        public string RoleId => Constants.RoleQuestGiver;

        private string _questPool = "meadows";

        public string QuestPool => _questPool;

        public void OnAssigned(VillageNPC npc)
        {
            var zdo = npc.ZNetView?.GetZDO();
            _questPool = zdo?.GetString(VLData.Hash(VLData.KeyQuestPool), "meadows") ?? "meadows";
        }

        public void OnRemoved(VillageNPC npc) { }

        public void OnInteract(VillageNPC npc, Player player)
        {
            // Get available quests for this NPC's pool
            var availableQuests = QuestEngine.GetAvailableQuests(player, _questPool);
            var activeQuests = QuestEngine.GetActiveQuestsForNPC(player, npc.GetZDOID());
            var completableQuests = activeQuests.Where(q => q.IsReadyToComplete()).ToList();

            // Priority: turn in completed quests first, then show available
            if (completableQuests.Count > 0)
            {
                QuestPanel.ShowCompletion(npc, player, completableQuests.First());
            }
            else if (activeQuests.Count > 0)
            {
                QuestPanel.ShowProgress(npc, player, activeQuests.First());
            }
            else if (availableQuests.Count > 0)
            {
                QuestPanel.ShowOffer(npc, player, availableQuests);
            }
            else
            {
                // No quests available — show idle dialog
                QuestPanel.ShowNoQuests(npc, player);
            }
        }

        public string GetHoverText(VillageNPC npc)
        {
            var player = Player.m_localPlayer;
            if (player == null) return "";

            var active = QuestEngine.GetActiveQuestsForNPC(player, npc.GetZDOID());
            var completable = active.Where(q => q.IsReadyToComplete()).ToList();

            if (completable.Count > 0)
                return "<color=green>[Quest Complete!]</color>";
            if (active.Count > 0)
                return "<color=yellow>[Quest In Progress]</color>";

            var available = QuestEngine.GetAvailableQuests(player, _questPool);
            if (available.Count > 0)
                return "<color=cyan>[New Quest Available]</color>";

            return "";
        }

        public void OnUpdate(VillageNPC npc, float deltaTime) { }
    }
}
