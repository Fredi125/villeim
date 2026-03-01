namespace VillageLife.Util
{
    public static class Constants
    {
        // Prefab names
        public const string NPCPrefabName = "VL_NPC";
        public const string VillageHallPrefabName = "VL_VillageHall";

        // Piece categories
        public const string PieceCategory = "Village";

        // Role IDs
        public const string RoleMerchant = "merchant";
        public const string RoleQuestGiver = "quest_giver";
        public const string RoleGuard = "guard";
        public const string RoleVillager = "villager";

        // Day/night schedule (EnvMan day fractions)
        public const float DawnFraction = 0.25f;
        public const float DuskFraction = 0.7f;
        public const float SleepFraction = 0.8f;

        // NPC behavior
        public const float DefaultWanderRadius = 12f;
        public const float WanderIdleMinSeconds = 5f;
        public const float WanderIdleMaxSeconds = 15f;
        public const float InteractionDistance = 3f;
        public const float DialogDisplaySeconds = 4f;
        public const float AmbientDialogIntervalMin = 30f;
        public const float AmbientDialogIntervalMax = 120f;

        // Multiplayer
        public const string RPCTradeRequest = "VL_TradeRequest";
        public const string RPCTradeResponse = "VL_TradeResponse";
        public const string RPCQuestAccept = "VL_QuestAccept";
        public const string RPCQuestComplete = "VL_QuestComplete";
        public const string RPCQuestResponse = "VL_QuestResponse";
        public const string RPCNPCCreate = "VL_NPCCreate";

        // Config paths
        public const string ConfigFolderName = "VillageLife";
        public const string ShopConfigFile = "shops.json";
        public const string QuestConfigFile = "quests.json";
        public const string DialogConfigFile = "dialog.json";
        public const string LocalizationFolder = "localization";
    }
}
