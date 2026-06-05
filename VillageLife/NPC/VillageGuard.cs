using System;
using System.Collections.Generic;
using UnityEngine;
using VillageLife.Util;

namespace VillageLife.NPC
{
    /// <summary>
    /// A stationary guard villager. It is a friendly Haldor clone — so, like every other villager in
    /// this mod, it never gets a crash-prone <c>Character</c>/AI of its own — that periodically harms
    /// nearby hostile creatures, like a warded watchpost.
    ///
    /// Safety, on purpose:
    ///   • only the ZDO <b>owner</b> applies damage, so a creature isn't hit once per connected client;
    ///   • the whole tick is wrapped — any surprise from the combat API disables the tick and logs
    ///     once, rather than throwing every few seconds;
    ///   • players and tamed creatures are never targeted.
    /// </summary>
    public class VillageGuard : MonoBehaviour, Hoverable, Interactable
    {
        private const float Radius = 12f;
        private const float TickSeconds = 3f;
        private const float DamagePerTick = 12f;
        private const int MaxTargetsPerTick = 3;
        private const float ChatterRange = 10f;

        private ZNetView _nview;
        private bool _tickFailed;
        private bool _playerNear;
        private static readonly List<Character> _buffer = new List<Character>();

        public string GuardName { get; private set; } = "Guard";
        public string VendorTypeId { get; private set; } = "guard";

        private void Awake()
        {
            _nview = GetComponent<ZNetView>();
        }

        private void Start()
        {
            ZDO zdo = _nview != null ? _nview.GetZDO() : null;
            if (zdo != null)
            {
                GuardName = zdo.GetString(Constants.KeyName, GuardName);
                VendorTypeId = zdo.GetString(Constants.KeyVendorType, VendorTypeId);
            }

            VillagerAppearance.Apply(gameObject, zdo);
            InvokeRepeating(nameof(GuardTick), TickSeconds, TickSeconds);
            InvokeRepeating(nameof(ChatterTick), 2f, 2f);
        }

        private void OnDestroy()
        {
            CancelInvoke();
        }

        /// <summary>Occasionally show a watch-flavour line over the guard when a player is nearby.
        /// Purely visual (a local chat bubble), so it runs on every client.</summary>
        private void ChatterTick()
        {
            Player p = Player.m_localPlayer;
            bool near = p != null && Vector3.Distance(p.transform.position, transform.position) <= ChatterRange;
            if (!near)
            {
                _playerNear = false;
                return;
            }

            // Greet on the rising edge (just walked up); afterwards, only an occasional idle line.
            if (!_playerNear)
                _playerNear = true;
            else if (UnityEngine.Random.value > 0.12f)
                return;

            VillagerChatter.Say(gameObject, VillagerChatter.GuardTalk);
        }

        /// <summary>Called by <see cref="NpcSpawner"/> on the owning client right after spawn.</summary>
        public void Initialize(string name, string vendorTypeId, long creatorId)
        {
            if (!string.IsNullOrEmpty(name))
                GuardName = name;
            if (!string.IsNullOrEmpty(vendorTypeId))
                VendorTypeId = vendorTypeId;

            ZDO zdo = _nview != null ? _nview.GetZDO() : null;
            if (zdo != null)
            {
                zdo.Set(Constants.KeyName, GuardName);
                zdo.Set(Constants.KeyVendorType, VendorTypeId);
                zdo.Set(Constants.KeyCreator, creatorId);
            }
        }

        private void GuardTick()
        {
            // Only the owner deals damage, and only when the world object is live.
            if (_nview == null || !_nview.IsValid() || !_nview.IsOwner())
                return;

            try
            {
                _buffer.Clear();
                Character.GetCharactersInRange(transform.position, Radius, _buffer);

                int hits = 0;
                foreach (Character c in _buffer)
                {
                    if (hits >= MaxTargetsPerTick)
                        break;
                    if (c == null || c.IsDead() || c.IsPlayer() || c.IsTamed())
                        continue;

                    var hit = new HitData();
                    hit.m_damage.m_blunt = DamagePerTick;
                    hit.m_point = c.GetCenterPoint();
                    hit.m_dir = (c.transform.position - transform.position).normalized;
                    c.Damage(hit);
                    hits++;
                }
            }
            catch (Exception e)
            {
                // Never let an unexpected combat-API result throw on a repeating timer.
                _tickFailed = true;
                CancelInvoke(nameof(GuardTick));
                Jotunn.Logger.LogWarning($"[VillageLife] Guard tick disabled after error: {e.Message}");
            }
        }

        public string GetHoverName() => GuardName;

        public string GetHoverText()
        {
            string status = _tickFailed ? "Resting." : "Keeps watch over your village.";
            return Localization.instance.Localize(
                $"<color=yellow><b>{GuardName}</b></color> (Village Guard)\n{status}");
        }

        public bool Interact(Humanoid user, bool hold, bool alt)
        {
            if (hold)
                return false;

            var player = user as Player;
            if (player != null)
                player.Message(MessageHud.MessageType.Center,
                    $"{GuardName}: \"Stay safe — I'll handle the rabble.\"");
            return true;
        }

        public bool UseItem(Humanoid user, ItemDrop.ItemData item) => false;
    }
}
