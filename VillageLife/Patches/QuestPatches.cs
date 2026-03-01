using HarmonyLib;
using UnityEngine;
using VillageLife.Quest;

namespace VillageLife.Patches
{
    /// <summary>
    /// Harmony patches that hook into Valheim's game events for quest objective tracking.
    /// </summary>
    public static class QuestPatches
    {
        /// <summary>
        /// Patch Character.OnDeath to detect creature kills for Kill objectives.
        /// </summary>
        [HarmonyPatch(typeof(Character), "OnDeath")]
        public static class CharacterOnDeathPatch
        {
            public static void Postfix(Character __instance)
            {
                if (!Plugin.VillageLifePlugin.EnableQuestSystem.Value)
                    return;

                // Only process on the server/host
                if (!ZNet.instance.IsServer() && !ZNet.instance.IsDedicated())
                    return;

                // Get the creature name (prefab name without clone suffix)
                string creatureName = Utils.GetPrefabName(__instance.gameObject);

                // Find who killed it by checking the last hit data
                var lastHit = __instance.m_lastHit;
                if (lastHit == null) return;

                // Check if the attacker is a player
                var attacker = lastHit.GetAttacker();
                if (attacker == null) return;

                var player = attacker as Player;
                if (player == null)
                {
                    // Check if a player's creature (tamed) killed it
                    player = Player.GetClosestPlayer(__instance.transform.position, 50f);
                }

                if (player != null)
                {
                    QuestEngine.OnCreatureKilled(creatureName, __instance.transform.position, player);
                }
            }
        }

        /// <summary>
        /// Patch Inventory.AddItem to detect item gathering for Gather objectives.
        /// </summary>
        [HarmonyPatch(typeof(Inventory), nameof(Inventory.AddItem), typeof(ItemDrop.ItemData))]
        public static class InventoryAddItemPatch
        {
            public static void Postfix(Inventory __instance, ItemDrop.ItemData item, bool __result)
            {
                if (!Plugin.VillageLifePlugin.EnableQuestSystem.Value)
                    return;

                if (!__result || item == null) return;

                // Check if this inventory belongs to a player
                var player = Player.m_localPlayer;
                if (player == null || player.GetInventory() != __instance)
                    return;

                string itemName = item.m_shared.m_name;
                QuestEngine.OnItemGathered(itemName, item.m_stack, player);
            }
        }

        /// <summary>
        /// Patch Piece placement to detect building for Build objectives.
        /// </summary>
        [HarmonyPatch(typeof(Player), nameof(Player.PlacePiece))]
        public static class PlayerPlacePiecePatch
        {
            public static void Postfix(Player __instance, Piece piece, bool __result)
            {
                if (!Plugin.VillageLifePlugin.EnableQuestSystem.Value)
                    return;

                if (!__result || piece == null) return;

                if (__instance != Player.m_localPlayer)
                    return;

                string pieceName = piece.m_name;
                QuestEngine.OnStructureBuilt(pieceName, __instance);
            }
        }
    }

    /// <summary>
    /// Patches for NPC event reactions (raids, combat).
    /// </summary>
    public static class EventPatches
    {
        /// <summary>
        /// Patch RandEventSystem to detect raid events and trigger NPC flee behavior.
        /// </summary>
        [HarmonyPatch(typeof(RandEventSystem), "SetActiveEvent")]
        public static class RaidEventPatch
        {
            public static void Postfix(RandEventSystem __instance, RandomEvent ev)
            {
                if (ev == null) return;

                // Get all NPCs near the event and trigger flee
                var nearbyNPCs = NPC.NPCManager.GetNPCsInRange(ev.m_pos, 50f);
                foreach (var npc in nearbyNPCs)
                {
                    npc.BehaviorFSM?.TriggerFlee();
                }
            }
        }
    }

    /// <summary>
    /// Patch to detect location discovery for exploration quests.
    /// </summary>
    [HarmonyPatch(typeof(MessageHud), nameof(MessageHud.ShowBiomeFoundMsg))]
    public static class LocationDiscoveryPatch
    {
        public static void Postfix(string text)
        {
            if (!Plugin.VillageLifePlugin.EnableQuestSystem.Value)
                return;

            var player = Player.m_localPlayer;
            if (player == null) return;

            QuestEngine.OnLocationDiscovered(text, player);
        }
    }
}
