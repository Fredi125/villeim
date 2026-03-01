using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VillageLife.Util;

namespace VillageLife.NPC.Roles
{
    /// <summary>
    /// Guard role — NPC patrols and defends an area around its home point.
    /// Engages hostile creatures that enter the patrol radius.
    /// </summary>
    public class GuardRole : INPCRole
    {
        public string RoleId => Constants.RoleGuard;

        private float _patrolRadius = 15f;
        private float _detectionRadius = 20f;
        private float _attackCooldown;
        private Character _currentTarget;

        public void OnAssigned(VillageNPC npc)
        {
            var zdo = npc.ZNetView?.GetZDO();
            _patrolRadius = zdo?.GetFloat(VLData.Hash(VLData.KeyPatrolRadius), 15f) ?? 15f;

            // Equip the guard with a weapon if the humanoid component exists
            var humanoid = npc.GetComponent<Humanoid>();
            if (humanoid != null)
            {
                humanoid.m_faction = Character.Faction.Players;
            }
        }

        public void OnRemoved(VillageNPC npc) { }

        public void OnInteract(VillageNPC npc, Player player)
        {
            // Guards show a simple status dialog
            string status = _currentTarget != null
                ? $"{npc.NPCName}: \"Hostile spotted! Stay behind me!\""
                : $"{npc.NPCName}: \"All clear. The perimeter is secure.\"";

            MessageHud.instance?.ShowMessage(MessageHud.MessageType.Center, status);
        }

        public string GetHoverText(VillageNPC npc)
        {
            if (_currentTarget != null)
                return "<color=red>[In Combat]</color>";
            return "<color=green>[On Patrol]</color>";
        }

        public void OnUpdate(VillageNPC npc, float deltaTime)
        {
            _attackCooldown -= deltaTime;

            // Scan for enemies
            if (_currentTarget == null || _currentTarget.IsDead())
            {
                _currentTarget = FindNearestEnemy(npc);
            }

            if (_currentTarget != null && !_currentTarget.IsDead())
            {
                // Move toward target
                Vector3 direction = (_currentTarget.transform.position - npc.transform.position).normalized;
                float distance = Vector3.Distance(npc.transform.position, _currentTarget.transform.position);

                if (distance > 2f)
                {
                    // Walk toward enemy
                    npc.transform.position += direction * 3f * deltaTime;
                    npc.transform.rotation = Quaternion.LookRotation(direction);
                }
                else if (_attackCooldown <= 0f)
                {
                    // Attack
                    var humanoid = npc.GetComponent<Humanoid>();
                    if (humanoid != null)
                    {
                        var hitData = new HitData
                        {
                            m_damage = { m_damage = 20f },
                            m_point = _currentTarget.transform.position,
                            m_dir = direction
                        };
                        _currentTarget.Damage(hitData);
                        _attackCooldown = 2f;
                    }
                }
            }
            else
            {
                _currentTarget = null;

                // Return to patrol area if too far from home
                Vector3 home = npc.GetHomePosition();
                float distFromHome = Vector3.Distance(npc.transform.position, home);
                if (distFromHome > _patrolRadius)
                {
                    Vector3 toHome = (home - npc.transform.position).normalized;
                    npc.transform.position += toHome * 2f * deltaTime;
                    npc.transform.rotation = Quaternion.LookRotation(toHome);
                }
            }
        }

        private Character FindNearestEnemy(VillageNPC npc)
        {
            var characters = new List<Character>();
            Character.GetCharactersInRange(npc.transform.position, _detectionRadius, characters);

            return characters
                .Where(c => c != null && !c.IsDead() && !c.IsTamed() &&
                           c.GetFaction() != Character.Faction.Players &&
                           c.GetComponent<Player>() == null)
                .OrderBy(c => Vector3.Distance(npc.transform.position, c.transform.position))
                .FirstOrDefault();
        }
    }
}
