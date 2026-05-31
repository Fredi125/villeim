using UnityEngine;
using VillageLife.Util;

namespace VillageLife.NPC
{
    /// <summary>
    /// The controller on every spawned villager. It is intentionally simple:
    ///   • stores a display name in the creature's ZDO (so it persists and syncs),
    ///   • shows that name on hover,
    ///   • says a short greeting when talked to.
    /// Villagers are stationary — they never move from where they were summoned.
    /// </summary>
    public class VillageNpc : MonoBehaviour, Hoverable, Interactable
    {
        private ZNetView _nview;

        public string NpcName { get; private set; } = "Villager";

        private static readonly string[] Greetings =
        {
            "Well met, traveller.",
            "A fine day in the meadows, is it not?",
            "Stay a while — the fire is warm.",
            "The gods watch over this place.",
            "Mind the forest after dark, friend."
        };

        private void Awake()
        {
            _nview = GetComponent<ZNetView>();
        }

        private void Start()
        {
            // Read the name written by whoever summoned this villager. On clients that
            // received the creature over the network, this is how they learn its name.
            var zdo = _nview != null ? _nview.GetZDO() : null;
            if (zdo != null)
                NpcName = zdo.GetString(Constants.KeyName, NpcName);
        }

        /// <summary>
        /// Called by <see cref="NpcSpawner"/> on the summoning client immediately after the
        /// villager is instantiated (while this client still owns the fresh ZDO).
        /// </summary>
        public void Initialize(string npcName, long creatorId)
        {
            if (!string.IsNullOrEmpty(npcName))
                NpcName = npcName;

            var zdo = _nview != null ? _nview.GetZDO() : null;
            if (zdo == null)
                return;

            zdo.Set(Constants.KeyName, NpcName);
            zdo.Set(Constants.KeyCreator, creatorId);
        }

        #region Hoverable

        public string GetHoverName() => NpcName;

        public string GetHoverText()
        {
            return Localization.instance.Localize(
                $"<color=yellow><b>{NpcName}</b></color>\n" +
                "[<color=yellow><b>$KEY_Use</b></color>] Talk");
        }

        #endregion

        #region Interactable

        public bool Interact(Humanoid user, bool hold, bool alt)
        {
            if (hold)
                return false;

            // Turn to face the player (rotation only — villagers never leave their post).
            Vector3 dir = user.transform.position - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.LookRotation(dir.normalized);

            string line = Greetings[Random.Range(0, Greetings.Length)];
            if (Chat.instance != null)
                Chat.instance.SetNpcText(gameObject, Vector3.up * 1.5f, 20f, 5f, "", line, false);

            return true;
        }

        public bool UseItem(Humanoid user, ItemDrop.ItemData item) => false;

        #endregion
    }
}
