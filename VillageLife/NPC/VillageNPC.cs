using System;
using UnityEngine;
using VillageLife.NPC.Behaviors;
using VillageLife.NPC.Roles;
using VillageLife.Util;

namespace VillageLife.NPC
{
    /// <summary>
    /// Core NPC component. Attached to every placed NPC.
    /// Implements Hoverable (show name on hover) and Interactable (E key interaction).
    /// Manages ZDO persistence and delegates to role-specific behavior.
    /// </summary>
    public class VillageNPC : MonoBehaviour, Hoverable, Interactable
    {
        private ZNetView _zNetView;
        private INPCRole _role;
        private NPCBehaviorFSM _behaviorFSM;

        // Cached ZDO data
        public string NPCName { get; private set; } = "Villager";
        public string RoleId { get; private set; } = Constants.RoleVillager;
        public long CreatorId { get; private set; }
        public bool IsMale { get; private set; } = true;
        public string HairStyle { get; private set; } = "";
        public string BeardStyle { get; private set; } = "";

        public INPCRole Role => _role;
        public NPCBehaviorFSM BehaviorFSM => _behaviorFSM;
        public ZNetView ZNetView => _zNetView;

        private void Awake()
        {
            _zNetView = GetComponent<ZNetView>();
            if (_zNetView == null)
            {
                Debug.LogError("[VillageLife] VillageNPC missing ZNetView!");
                return;
            }

            _behaviorFSM = gameObject.AddComponent<NPCBehaviorFSM>();
        }

        private void Start()
        {
            if (_zNetView == null || _zNetView.GetZDO() == null)
                return;

            LoadFromZDO();
            AssignRole(RoleId);
            NPCManager.Register(this);
        }

        private void OnDestroy()
        {
            NPCManager.Unregister(this);
            _role?.OnRemoved(this);
        }

        public ZDOID GetZDOID()
        {
            return _zNetView != null && _zNetView.GetZDO() != null
                ? _zNetView.GetZDO().m_uid
                : ZDOID.None;
        }

        #region ZDO Persistence

        public void SaveToZDO()
        {
            var zdo = _zNetView?.GetZDO();
            if (zdo == null) return;

            zdo.Set(ZDOHelper.Hash(ZDOHelper.KeyNPCName), NPCName);
            zdo.Set(ZDOHelper.Hash(ZDOHelper.KeyNPCRole), RoleId);
            zdo.Set(ZDOHelper.Hash(ZDOHelper.KeyNPCCreatorId), CreatorId);
            zdo.Set(ZDOHelper.Hash(ZDOHelper.KeyIsMale), IsMale);
            zdo.Set(ZDOHelper.Hash(ZDOHelper.KeyHairStyle), HairStyle);
            zdo.Set(ZDOHelper.Hash(ZDOHelper.KeyBeardStyle), BeardStyle);

            var pos = transform.position;
            zdo.Set(ZDOHelper.Hash(ZDOHelper.KeyHomeX), pos.x);
            zdo.Set(ZDOHelper.Hash(ZDOHelper.KeyHomeY), pos.y);
            zdo.Set(ZDOHelper.Hash(ZDOHelper.KeyHomeZ), pos.z);
        }

        public void LoadFromZDO()
        {
            var zdo = _zNetView?.GetZDO();
            if (zdo == null) return;

            NPCName = zdo.GetString(ZDOHelper.Hash(ZDOHelper.KeyNPCName), "Villager");
            RoleId = zdo.GetString(ZDOHelper.Hash(ZDOHelper.KeyNPCRole), Constants.RoleVillager);
            CreatorId = zdo.GetLong(ZDOHelper.Hash(ZDOHelper.KeyNPCCreatorId), 0L);
            IsMale = zdo.GetBool(ZDOHelper.Hash(ZDOHelper.KeyIsMale), true);
            HairStyle = zdo.GetString(ZDOHelper.Hash(ZDOHelper.KeyHairStyle), "");
            BeardStyle = zdo.GetString(ZDOHelper.Hash(ZDOHelper.KeyBeardStyle), "");
        }

        #endregion

        #region Configuration

        /// <summary>
        /// Called when the NPC is first created at the Village Hall.
        /// Sets up initial ZDO data.
        /// </summary>
        public void Configure(string npcName, string roleId, long creatorId, bool isMale,
            string hairStyle = "", string beardStyle = "")
        {
            NPCName = npcName;
            RoleId = roleId;
            CreatorId = creatorId;
            IsMale = isMale;
            HairStyle = hairStyle;
            BeardStyle = beardStyle;

            SaveToZDO();
            AssignRole(roleId);
        }

        public void AssignRole(string roleId)
        {
            _role?.OnRemoved(this);
            _role = RoleSystem.CreateRole(roleId);
            _role?.OnAssigned(this);
        }

        #endregion

        #region Hoverable

        public string GetHoverText()
        {
            string roleDisplay = RoleSystem.GetRoleDisplayName(RoleId);
            string hoverText = $"<color=yellow><b>{NPCName}</b></color>\n" +
                              $"<color=#AAAAAA>{roleDisplay}</color>\n" +
                              "[<color=yellow><b>$KEY_Use</b></color>] Talk";

            if (_role != null)
            {
                string extra = _role.GetHoverText(this);
                if (!string.IsNullOrEmpty(extra))
                    hoverText += "\n" + extra;
            }

            return Localization.instance.Localize(hoverText);
        }

        public string GetHoverName()
        {
            return NPCName;
        }

        #endregion

        #region Interactable

        public bool Interact(Humanoid user, bool hold, bool alt)
        {
            if (hold) return false;
            if (_role == null) return false;

            // Face the player
            Vector3 dir = (user.transform.position - transform.position).normalized;
            dir.y = 0;
            if (dir != Vector3.zero)
                transform.rotation = Quaternion.LookRotation(dir);

            // Transition to INTERACTING state
            _behaviorFSM?.SetState(BehaviorState.Interacting);

            // Delegate to role
            _role.OnInteract(this, user as Player);
            return true;
        }

        public bool UseItem(Humanoid user, ItemDrop.ItemData item)
        {
            return false;
        }

        #endregion

        /// <summary>
        /// Get the NPC's home position (where it was originally placed).
        /// </summary>
        public Vector3 GetHomePosition()
        {
            var zdo = _zNetView?.GetZDO();
            if (zdo == null) return transform.position;

            return new Vector3(
                zdo.GetFloat(ZDOHelper.Hash(ZDOHelper.KeyHomeX), transform.position.x),
                zdo.GetFloat(ZDOHelper.Hash(ZDOHelper.KeyHomeY), transform.position.y),
                zdo.GetFloat(ZDOHelper.Hash(ZDOHelper.KeyHomeZ), transform.position.z)
            );
        }
    }
}
