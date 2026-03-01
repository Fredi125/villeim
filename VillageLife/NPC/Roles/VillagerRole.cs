using VillageLife.Dialog;
using VillageLife.Util;

namespace VillageLife.NPC.Roles
{
    /// <summary>
    /// Villager role — purely ambient NPC with idle dialog and no functional interaction.
    /// Wanders near home, sleeps at night, comments on the weather and surroundings.
    /// </summary>
    public class VillagerRole : INPCRole
    {
        public string RoleId => Constants.RoleVillager;

        public void OnAssigned(VillageNPC npc) { }

        public void OnRemoved(VillageNPC npc) { }

        public void OnInteract(VillageNPC npc, Player player)
        {
            // Villagers say a random greeting when interacted with
            string line = DialogSystem.GetRandomLine(Constants.RoleVillager, DialogContext.Greeting);
            if (!string.IsNullOrEmpty(line))
            {
                string message = $"{npc.NPCName}: \"{line}\"";
                MessageHud.instance?.ShowMessage(MessageHud.MessageType.Center, message);
            }
            else
            {
                MessageHud.instance?.ShowMessage(MessageHud.MessageType.Center,
                    $"{npc.NPCName}: \"Nice day, isn't it?\"");
            }
        }

        public string GetHoverText(VillageNPC npc)
        {
            return "";
        }

        public void OnUpdate(VillageNPC npc, float deltaTime) { }
    }
}
