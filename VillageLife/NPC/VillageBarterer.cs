using UnityEngine;
using VillageLife.Util;

namespace VillageLife.NPC
{
    /// <summary>
    /// A barter villager: give a fixed resource, receive a fixed product (e.g. 40 Stone → 30 Wood).
    /// No coins and no shop window — this is the sole Hoverable/Interactable on the barter prefab
    /// (its Trader component was removed at registration), so there's no interaction ambiguity.
    ///
    /// The exchange is deliberately simple and safe:
    ///   1. count the required input,
    ///   2. only if enough is present, remove exactly that input and add the output,
    /// so a failed/declined trade never removes anything. Names are resolved via <see cref="ItemNames"/>
    /// because CountItems/RemoveItem take the shared name while AddItem takes the prefab name.
    /// </summary>
    public class VillageBarterer : MonoBehaviour, Hoverable, Interactable
    {
        private ZNetView _nview;

        public string MerchantName { get; private set; } = "Trader";
        public string VendorTypeId { get; private set; } = "stonemason";

        private void Awake()
        {
            _nview = GetComponent<ZNetView>();
        }

        private void Start()
        {
            var zdo = _nview != null ? _nview.GetZDO() : null;
            if (zdo != null)
            {
                MerchantName = zdo.GetString(Constants.KeyName, MerchantName);
                VendorTypeId = zdo.GetString(Constants.KeyVendorType, VendorTypeId);
            }

            VillagerAppearance.Apply(gameObject, zdo);
        }

        /// <summary>Called by <see cref="NpcSpawner"/> on the owning client right after spawn.</summary>
        public void Initialize(string name, string vendorTypeId, long creatorId)
        {
            if (!string.IsNullOrEmpty(name))
                MerchantName = name;
            if (!string.IsNullOrEmpty(vendorTypeId))
                VendorTypeId = vendorTypeId;

            var zdo = _nview != null ? _nview.GetZDO() : null;
            if (zdo != null)
            {
                zdo.Set(Constants.KeyName, MerchantName);
                zdo.Set(Constants.KeyVendorType, VendorTypeId);
                zdo.Set(Constants.KeyCreator, creatorId);
            }
        }

        #region Hoverable

        public string GetHoverName() => MerchantName;

        public string GetHoverText()
        {
            VendorType t = VendorCatalog.ById(VendorTypeId);
            string offer = $"{t.CostAmount} {ItemNames.Display(t.CostPrefab)} → " +
                           $"{t.GiveAmount} {ItemNames.Display(t.GivePrefab)}";
            return Localization.instance.Localize(
                $"<color=yellow><b>{MerchantName}</b></color> ({t.Title})\n" +
                $"{offer}\n[<color=yellow><b>$KEY_Use</b></color>] Trade");
        }

        #endregion

        #region Interactable

        public bool Interact(Humanoid user, bool hold, bool alt)
        {
            if (hold)
                return false;

            var player = user as Player;
            if (player == null)
                return false;

            VendorType t = VendorCatalog.ById(VendorTypeId);
            TryBarter(player, t);
            return true;
        }

        public bool UseItem(Humanoid user, ItemDrop.ItemData item) => false;

        #endregion

        /// <summary>Perform the swap if the player has the input; otherwise tell them what's needed.</summary>
        private void TryBarter(Player player, VendorType t)
        {
            Inventory inv = player.GetInventory();
            if (inv == null)
                return;

            // Resolve names up front; if either item can't be resolved, refuse without touching anything.
            string costShared = ItemNames.SharedName(t.CostPrefab);
            if (string.IsNullOrEmpty(costShared) || !ItemNames.Exists(t.GivePrefab))
            {
                player.Message(MessageHud.MessageType.Center, "This trader cannot trade right now.");
                Jotunn.Logger.LogWarning(
                    $"[VillageLife] Barter '{t.Id}' could not resolve items " +
                    $"(cost '{t.CostPrefab}', give '{t.GivePrefab}').");
                return;
            }

            int have = inv.CountItems(costShared);
            if (have < t.CostAmount)
            {
                player.Message(MessageHud.MessageType.Center,
                    $"Need {t.CostAmount} {ItemNames.Display(t.CostPrefab)} " +
                    $"(you have {have}).");
                return;
            }

            // Make sure there's a free slot for the reward before taking payment, so we never take
            // the input and then fail to give the output. (Conservative: requires an empty slot
            // even if an existing stack could absorb it — safe, and uses only a well-known API.)
            if (!inv.HaveEmptySlot())
            {
                player.Message(MessageHud.MessageType.Center, "Your inventory is full.");
                return;
            }

            // Take payment, then give the reward. AddItem's full overload (name, stack, quality,
            // variant, crafterID, crafterName) is the one we can rely on; quality 1, variant 0,
            // and an empty crafter mirror how plain resources are created.
            inv.RemoveItem(costShared, t.CostAmount);
            inv.AddItem(t.GivePrefab, t.GiveAmount, 1, 0, 0L, "");

            player.Message(MessageHud.MessageType.Center,
                $"Traded {t.CostAmount} {ItemNames.Display(t.CostPrefab)} for " +
                $"{t.GiveAmount} {ItemNames.Display(t.GivePrefab)}.");

            // A bounty turn-in also builds reputation with the trader it's tied to, which can unlock
            // higher-tier goods in that trader's shop.
            GrantReputation(player, t);
        }

        /// <summary>If this barter is tied to a trader, advance that trader's world reputation and
        /// refresh any of its merchants already standing in the world.</summary>
        private static void GrantReputation(Player player, VendorType t)
        {
            if (t == null || string.IsNullOrEmpty(t.UnlocksVendorId))
                return;

            if (!TraderReputation.TryAdvance(t.UnlocksVendorId, out int level, out int max))
                return;

            VendorType trader = VendorCatalog.ById(t.UnlocksVendorId);
            player.Message(MessageHud.MessageType.Center,
                level >= max
                    ? $"{trader.Title}: reputation {level}/{max} — all goods unlocked!"
                    : $"{trader.Title}: reputation {level}/{max} — new goods unlocked!");

            foreach (VillageMerchant m in Object.FindObjectsOfType<VillageMerchant>())
                if (m != null && m.VendorTypeId == t.UnlocksVendorId)
                    m.RefreshStock();
        }
    }
}
