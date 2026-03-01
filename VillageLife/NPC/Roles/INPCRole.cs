namespace VillageLife.NPC.Roles
{
    /// <summary>
    /// Interface for NPC role behaviors.
    /// Each role defines how an NPC interacts with players and what special
    /// functionality it provides (trading, quests, guarding, etc.).
    /// </summary>
    public interface INPCRole
    {
        /// <summary>Role identifier string (e.g., "merchant", "quest_giver").</summary>
        string RoleId { get; }

        /// <summary>Called when this role is assigned to an NPC.</summary>
        void OnAssigned(VillageNPC npc);

        /// <summary>Called when this role is removed from an NPC (role change or destruction).</summary>
        void OnRemoved(VillageNPC npc);

        /// <summary>Called when a player interacts with this NPC (E key press).</summary>
        void OnInteract(VillageNPC npc, Player player);

        /// <summary>Additional hover text shown below the NPC name.</summary>
        string GetHoverText(VillageNPC npc);

        /// <summary>Called every frame while the NPC is active. Use for role-specific updates.</summary>
        void OnUpdate(VillageNPC npc, float deltaTime);
    }
}
