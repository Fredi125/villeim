using System.Collections.Generic;
using UnityEngine;
using VillageLife.Util;

namespace VillageLife.NPC.Roles
{
    /// <summary>
    /// Guard role — a posted watchman.
    ///
    /// 2.0: Guards are stationary, like all NPCs. The old manual chase/attack code
    /// (which slid the transform around without animation or net-sync) is gone.
    /// Instead the guard periodically scans for nearby hostiles purely to flavour its
    /// hover text and dialog, while its friendly-faction Humanoid lets the vanilla
    /// combat system handle anything that attacks it.
    /// </summary>
    public class GuardRole : INPCRole
    {
        public string RoleId => Constants.RoleGuard;

        private const float DetectionRadius = 20f;
        private const float ScanInterval = 2f;

        private float _scanTimer;
        private bool _threatNearby;

        public void OnAssigned(VillageNPC npc)
        {
            var humanoid = npc.GetComponent<Humanoid>();
            if (humanoid != null)
                humanoid.m_faction = Character.Faction.Players;
        }

        public void OnRemoved(VillageNPC npc) { }

        public void OnInteract(VillageNPC npc, Player player)
        {
            string status = _threatNearby
                ? $"{npc.NPCName}: \"Stay alert — danger is close!\""
                : $"{npc.NPCName}: \"All quiet. The village is safe.\"";

            MessageHud.instance?.ShowMessage(MessageHud.MessageType.Center, status);
        }

        public string GetHoverText(VillageNPC npc)
        {
            return _threatNearby
                ? "<color=red>[On Guard]</color>"
                : "<color=green>[Watching]</color>";
        }

        public void OnUpdate(VillageNPC npc, float deltaTime)
        {
            _scanTimer -= deltaTime;
            if (_scanTimer > 0f)
                return;
            _scanTimer = ScanInterval;

            var characters = new List<Character>();
            Character.GetCharactersInRange(npc.transform.position, DetectionRadius, characters);

            _threatNearby = characters.Exists(c =>
                c != null && !c.IsDead() &&
                c.GetFaction() != Character.Faction.Players &&
                c.GetComponent<Player>() == null);
        }
    }
}
