using UnityEngine;
using VillageLife.NPC.Behaviors;
using VillageLife.NPC.Roles;
using VillageLife.Util;

namespace VillageLife.NPC
{
    /// <summary>
    /// Core controller attached to every placed NPC.
    /// Implements Hoverable (name/role on hover) and Interactable (E to talk).
    /// Handles ZDO persistence and delegates behaviour to the assigned role.
    ///
    /// 2.0: NPCs are stationary. The old movement FSM (which set transform.position
    /// directly and never animated or net-synced) has been removed. Roles are ticked
    /// here on the owning client; ambient dialog is added as a lightweight companion.
    /// </summary>
    public class VillageNPC : MonoBehaviour, Hoverable, Interactable
    {
        private ZNetView _zNetView;
        private INPCRole _role;

        // Cached ZDO data
        public string NPCName { get; private set; } = "Villager";
        public string RoleId { get; private set; } = Constants.RoleVillager;
        public long CreatorId { get; private set; }
        public bool IsMale { get; private set; } = true;
        public string HairStyle { get; private set; } = "";
        public string BeardStyle { get; private set; } = "";

        public INPCRole Role => _role;
        public ZNetView ZNetView => _zNetView;

        private void Awake()
        {
            _zNetView = GetComponent<ZNetView>();
        }

        private void Start()
        {
            if (_zNetView == null || _zNetView.GetZDO() == null)
                return;

            LoadFromZDO();
            AssignRole(RoleId);
            EnsureAmbientDialog();
            NPCManager.Register(this);
        }

        private void OnDestroy()
        {
            NPCManager.Unregister(this);
            _role?.OnRemoved(this);
        }

        private void Update()
        {
            // Only the owning client drives role logic (restock timers, guard scans, etc.).
            if (_zNetView == null || !_zNetView.IsValid() || !_zNetView.IsOwner())
                return;

            _role?.OnUpdate(this, Time.deltaTime);
        }

        private void EnsureAmbientDialog()
        {
            if (GetComponent<AmbientDialogBehavior>() == null)
                gameObject.AddComponent<AmbientDialogBehavior>();
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

            zdo.Set(VLData.Hash(VLData.KeyNPCName), NPCName);
            zdo.Set(VLData.Hash(VLData.KeyNPCRole), RoleId);
            zdo.Set(VLData.Hash(VLData.KeyNPCCreatorId), CreatorId);
            zdo.Set(VLData.Hash(VLData.KeyIsMale), IsMale);
            zdo.Set(VLData.Hash(VLData.KeyHairStyle), HairStyle);
            zdo.Set(VLData.Hash(VLData.KeyBeardStyle), BeardStyle);
        }

        public void LoadFromZDO()
        {
            var zdo = _zNetView?.GetZDO();
            if (zdo == null) return;

            NPCName = zdo.GetString(VLData.Hash(VLData.KeyNPCName), "Villager");
            RoleId = zdo.GetString(VLData.Hash(VLData.KeyNPCRole), Constants.RoleVillager);
            CreatorId = zdo.GetLong(VLData.Hash(VLData.KeyNPCCreatorId), 0L);
            IsMale = zdo.GetBool(VLData.Hash(VLData.KeyIsMale), true);
            HairStyle = zdo.GetString(VLData.Hash(VLData.KeyHairStyle), "");
            BeardStyle = zdo.GetString(VLData.Hash(VLData.KeyBeardStyle), "");
        }

        #endregion

        #region Configuration

        /// <summary>
        /// Called when an NPC is first created at the Village Hall. Writes the initial ZDO
        /// data and assigns the role. The owning client must hold the ZDO.
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
            string roleDisplay = global::Localization.instance.Localize(
                RoleSystem.GetRoleDisplayName(RoleId));

            string hoverText = $"<color=yellow><b>{NPCName}</b></color>\n" +
                               $"<color=#AAAAAA>{roleDisplay}</color>\n" +
                               "[<color=yellow><b>$KEY_Use</b></color>] Talk";

            if (_role != null)
            {
                string extra = _role.GetHoverText(this);
                if (!string.IsNullOrEmpty(extra))
                    hoverText += "\n" + extra;
            }

            return global::Localization.instance.Localize(hoverText);
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

            // Turn to face the player (rotation only — NPCs never move from their post).
            Vector3 dir = (user.transform.position - transform.position);
            dir.y = 0;
            if (dir.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.LookRotation(dir.normalized);

            _role.OnInteract(this, user as Player);
            return true;
        }

        public bool UseItem(Humanoid user, ItemDrop.ItemData item)
        {
            return false;
        }

        #endregion
    }
}
