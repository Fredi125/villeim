namespace VillageLife.Util
{
    /// <summary>
    /// Centralized ZDO key names and helpers for the VillageLife mod.
    /// All keys are namespaced with "villagelife:" to avoid conflicts.
    /// </summary>
    public static class ZDOHelper
    {
        // NPC identity
        public const string KeyNPCName = "villagelife:npc_name";
        public const string KeyNPCRole = "villagelife:npc_role";
        public const string KeyNPCCreatorId = "villagelife:creator_id";

        // Appearance
        public const string KeyHairStyle = "villagelife:hair_style";
        public const string KeyHairColor = "villagelife:hair_color";
        public const string KeyBeardStyle = "villagelife:beard_style";
        public const string KeySkinColor = "villagelife:skin_color";
        public const string KeyIsMale = "villagelife:is_male";

        // Merchant
        public const string KeyShopType = "villagelife:shop_type";
        public const string KeyLastRestock = "villagelife:last_restock";
        public const string KeyShopInventory = "villagelife:shop_inventory";

        // Quest giver
        public const string KeyQuestPool = "villagelife:quest_pool";
        public const string KeyActiveQuests = "villagelife:active_quests";

        // Behavior
        public const string KeyHomeX = "villagelife:home_x";
        public const string KeyHomeY = "villagelife:home_y";
        public const string KeyHomeZ = "villagelife:home_z";
        public const string KeyBehaviorState = "villagelife:behavior_state";
        public const string KeyWanderRadius = "villagelife:wander_radius";

        // Guard
        public const string KeyPatrolRadius = "villagelife:patrol_radius";
        public const string KeyGuardLevel = "villagelife:guard_level";

        /// <summary>
        /// Hashes a key string into an int for ZDO operations.
        /// Uses the same algorithm as Valheim's GetStableHashCode extension method.
        /// </summary>
        public static int Hash(string key)
        {
            unchecked
            {
                int hash1 = 5381;
                int hash2 = hash1;
                for (int i = 0; i < key.Length && key[i] != '\0'; i += 2)
                {
                    hash1 = ((hash1 << 5) + hash1) ^ key[i];
                    if (i == key.Length - 1 || key[i + 1] == '\0')
                        break;
                    hash2 = ((hash2 << 5) + hash2) ^ key[i + 1];
                }
                return hash1 + (hash2 * 1566083941);
            }
        }
    }
}
