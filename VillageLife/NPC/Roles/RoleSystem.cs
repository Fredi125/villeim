using System.Collections.Generic;
using VillageLife.Util;

namespace VillageLife.NPC.Roles
{
    /// <summary>
    /// Factory and registry for NPC roles.
    /// Creates role instances based on role ID strings stored in ZDO.
    /// </summary>
    public static class RoleSystem
    {
        private static readonly Dictionary<string, string> RoleDisplayNames = new Dictionary<string, string>
        {
            { Constants.RoleMerchant, "$role_merchant" },
            { Constants.RoleQuestGiver, "$role_quest_giver" },
            { Constants.RoleGuard, "$role_guard" },
            { Constants.RoleVillager, "$role_villager" }
        };

        /// <summary>
        /// All available role IDs.
        /// </summary>
        public static readonly string[] AllRoleIds = new[]
        {
            Constants.RoleMerchant,
            Constants.RoleQuestGiver,
            Constants.RoleGuard,
            Constants.RoleVillager
        };

        /// <summary>
        /// Create a new role instance from a role ID.
        /// </summary>
        public static INPCRole CreateRole(string roleId)
        {
            return roleId switch
            {
                Constants.RoleMerchant => new MerchantRole(),
                Constants.RoleQuestGiver => new QuestGiverRole(),
                Constants.RoleGuard => new GuardRole(),
                Constants.RoleVillager => new VillagerRole(),
                _ => new VillagerRole()
            };
        }

        /// <summary>
        /// Get the localization key for a role's display name.
        /// </summary>
        public static string GetRoleDisplayName(string roleId)
        {
            return RoleDisplayNames.TryGetValue(roleId, out var name) ? name : "$role_villager";
        }
    }
}
