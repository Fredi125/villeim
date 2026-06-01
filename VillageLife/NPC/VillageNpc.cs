using UnityEngine;
using VillageLife.Util;

namespace VillageLife.NPC
{
    /// <summary>
    /// The controller on every spawned villager. Intentionally simple:
    ///   • stores a display name in the ZDO (persists + syncs),
    ///   • shows that name on hover,
    ///   • greets the player when talked to.
    /// Villagers are stationary and the transform is never modified after spawn, so they can't
    /// fight Valheim's networked transform sync (which made them appear to teleport when talked to).
    /// </summary>
    public class VillageNpc : MonoBehaviour, Hoverable, Interactable
    {
        private ZNetView _nview;
        private int _lastGreeting = -1;

        public string NpcName { get; private set; } = "Villager";

        private static readonly string[] Greetings =
        {
            "Well met, traveller.",
            "A fine day in the meadows, is it not?",
            "Stay a while — the fire is warm.",
            "The gods watch over this place.",
            "Mind the forest after dark, friend.",
            "Good to see a friendly face.",
            "I hold my post, but I'm glad of company.",
            "Odin's ravens flew over this morning — a good omen.",
            "Rest easy. No draugr get past me here.",
            "Trade's been slow, but the mead is plenty."
        };

        private void Awake()
        {
            _nview = GetComponent<ZNetView>();
        }

        private void Start()
        {
            // Read the name written by whoever summoned this villager. On clients that
            // received the villager over the network, this is how they learn its name.
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

            // Deliberately do NOT rotate or move the villager. They greet the player in place.
            Say(PickGreeting());
            return true;
        }

        public bool UseItem(Humanoid user, ItemDrop.ItemData item) => false;

        #endregion

        /// <summary>Pick a greeting at random, never the same one twice in a row.</summary>
        private string PickGreeting()
        {
            if (Greetings.Length <= 1)
                return Greetings[0];

            int index;
            do { index = Random.Range(0, Greetings.Length); }
            while (index == _lastGreeting);

            _lastGreeting = index;
            return Greetings[index];
        }

        /// <summary>Show a speech bubble above the villager's head.</summary>
        private void Say(string line)
        {
            if (Chat.instance != null)
                Chat.instance.SetNpcText(gameObject, Vector3.up * 1.5f, 20f, 5f, "", line, false);
        }
    }
}
