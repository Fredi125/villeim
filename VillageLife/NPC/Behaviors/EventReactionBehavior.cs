using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VillageLife.Dialog;
using VillageLife.Util;

namespace VillageLife.NPC.Behaviors
{
    /// <summary>
    /// Monitors the area around the NPC for combat events, raids, and boss spawns.
    /// Triggers appropriate reactions: flee from danger, cheer after combat, etc.
    /// </summary>
    public class EventReactionBehavior : MonoBehaviour
    {
        private VillageNPC _npc;
        private float _checkTimer;
        private const float CheckInterval = 3f;
        private bool _inCombatMode;

        private void Awake()
        {
            _npc = GetComponent<VillageNPC>();
        }

        private void Update()
        {
            if (_npc == null || _npc.BehaviorFSM == null) return;

            // Don't react if sleeping or already interacting
            var state = _npc.BehaviorFSM.CurrentState;
            if (state == BehaviorState.Sleeping || state == BehaviorState.Interacting)
                return;

            _checkTimer += Time.deltaTime;
            if (_checkTimer < CheckInterval) return;
            _checkTimer = 0f;

            CheckForThreats();
        }

        private void CheckForThreats()
        {
            // Skip guards — they handle combat themselves
            if (_npc.RoleId == Constants.RoleGuard) return;

            float dangerRadius = 20f;
            var enemies = new List<Character>();
            Character.GetCharactersInRange(transform.position, dangerRadius, enemies);

            var hostiles = enemies
                .Where(c => c != null && !c.IsDead() &&
                           c.GetFaction() != Character.Faction.Players &&
                           c.GetComponent<Player>() == null)
                .ToList();

            if (hostiles.Count > 0)
            {
                if (!_inCombatMode)
                {
                    _inCombatMode = true;

                    // Non-guard NPCs flee from combat
                    _npc.BehaviorFSM.TriggerFlee();

                    // Show combat dialog
                    string line = DialogSystem.GetRandomLine(_npc.RoleId, DialogContext.Combat);
                    if (!string.IsNullOrEmpty(line) && Chat.instance != null)
                    {
                        Chat.instance.AddInworldText(gameObject, 0L,
                            transform.position + Vector3.up * 2.2f,
                            Talker.Type.Shout, UserInfo.GetLocalUser(), line);
                    }
                }
            }
            else if (_inCombatMode)
            {
                _inCombatMode = false;
                // Danger passed — return to normal behavior
            }
        }
    }
}
