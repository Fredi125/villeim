using System.Collections.Generic;
using UnityEngine;
using VillageLife.Plugin;
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

        // Chatter: greet the moment the player walks up (the rising edge of "in range"), the way the
        // vanilla Trader greets — so a barterer/bounty shows its bubble on approach, not only on a
        // slow random timer that often left it silent as you arrived.
        private const float ChatterRange = 10f;
        private bool _playerNear;

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
            InvokeRepeating(nameof(ChatterTick), 2f, 2f);
        }

        private void OnDestroy() => CancelInvoke();

        /// <summary>Show a flavour line over the barterer: a greeting the moment the player comes within
        /// range (so the bubble appears on approach, like the vanilla Trader), then only an occasional
        /// idle line while they linger. Purely visual (a local chat bubble), so it runs on every client
        /// and needs no ownership check.</summary>
        private void ChatterTick()
        {
            if (VillageLifePlugin.VillagerChatter != null && !VillageLifePlugin.VillagerChatter.Value)
                return;
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

            Say();
        }

        /// <summary>Pick the flavour lines that match this villager's role and show one over its head.</summary>
        private void Say()
        {
            VendorType t = VendorCatalog.ById(VendorTypeId);
            string[] lines;
            if (t != null && t.IsQuest)
                lines = VillagerChatter.QuestTalk;
            else if (t != null && !string.IsNullOrEmpty(t.UnlocksVendorId))
                lines = VillagerChatter.BountyTalk;
            else
                lines = VillagerChatter.BartererTalk;
            VillagerChatter.Say(gameObject, lines);
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

            // For bounties/quests tied to a trader, note the reputation gain.
            string favor = "";
            if (!string.IsNullOrEmpty(t.UnlocksVendorId) && TraderReputation.MaxTierForTrack(t.UnlocksVendorId) > 0)
                favor = $"\n<color=#aab4ff>Earns favor with the {VendorCatalog.ById(t.UnlocksVendorId).Title}</color>";

            string body;
            string action;
            if (t.IsQuest)
            {
                int done = QuestProgress.Completed(t.Id, t.QuestCap);
                QuestRecipe r = t.ActiveQuestRecipe(done);
                string list = "";
                if (r.Items != null)
                    foreach (QuestItem q in r.Items)
                        list += (list.Length > 0 ? ", " : "") +
                                $"{QuestProgress.Scale(q.Amount, done, t.QuestCostGrowth)} {ItemNames.Display(q.Prefab)}";
                int reward = QuestProgress.Scale(r.RewardAmount, done, t.QuestRewardGrowth);
                string repeat = done > 0 ? $" <color=#aab4ff>(completed {done}×)</color>" : "";
                body = $"Quest{repeat}: {list}\nReward: {reward} {ItemNames.Display(r.RewardPrefab)}";
                action = "Hand in";
            }
            else
            {
                body = $"{t.CostAmount} {ItemNames.Display(t.CostPrefab)} → " +
                       $"{t.GiveAmount} {ItemNames.Display(t.GivePrefab)}";
                action = "Trade";
            }

            return Localization.instance.Localize(
                $"<color=yellow><b>{MerchantName}</b></color> ({t.Title})\n" +
                $"{body}{favor}\n[<color=yellow><b>$KEY_Use</b></color>] {action}");
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
            if (t.IsQuest)
                TryQuest(player, t);
            else
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

        /// <summary>Multi-item quest turn-in: require ALL listed items at once, then take them and give
        /// the reward. Like the barter path, nothing is removed unless the whole turn-in succeeds.</summary>
        private void TryQuest(Player player, VendorType t)
        {
            Inventory inv = player.GetInventory();
            if (inv == null)
                return;

            // Completion count drives both which recipe is active (rotation) and its size (scaling).
            int done = QuestProgress.Completed(t.Id, t.QuestCap);
            QuestRecipe r = t.ActiveQuestRecipe(done);

            if (r.Items == null || r.Items.Length == 0 || !ItemNames.Exists(r.RewardPrefab))
            {
                player.Message(MessageHud.MessageType.Center, "This quest can't be completed right now.");
                Jotunn.Logger.LogWarning($"[VillageLife] Quest '{t.Id}' recipe unresolved (reward '{r.RewardPrefab}').");
                return;
            }

            // First pass: resolve and SUM the scaled requirement per resolved item name, removing
            // nothing. Summing means a recipe that lists the same prefab twice is counted once against
            // the player's stock instead of being verified twice and then removed twice (item loss).
            var need = new Dictionary<string, int>();
            var display = new Dictionary<string, string>();
            foreach (QuestItem q in r.Items)
            {
                string shared = ItemNames.SharedName(q.Prefab);
                if (string.IsNullOrEmpty(shared))
                {
                    player.Message(MessageHud.MessageType.Center, "This quest can't be completed right now.");
                    Jotunn.Logger.LogWarning($"[VillageLife] Quest '{t.Id}' item '{q.Prefab}' didn't resolve.");
                    return;
                }
                need[shared] = (need.TryGetValue(shared, out int prior) ? prior : 0)
                               + QuestProgress.Scale(q.Amount, done, t.QuestCostGrowth);
                display[shared] = ItemNames.Display(q.Prefab);
            }

            foreach (KeyValuePair<string, int> req in need)
            {
                int have = inv.CountItems(req.Key);
                if (have < req.Value)
                {
                    player.Message(MessageHud.MessageType.Center,
                        $"Quest needs {req.Value} {display[req.Key]} (you have {have}).");
                    return;
                }
            }

            if (!inv.HaveEmptySlot())
            {
                player.Message(MessageHud.MessageType.Center, "Your inventory is full.");
                return;
            }

            // Requirements met — take the summed amounts, then give the scaled reward.
            foreach (KeyValuePair<string, int> req in need)
                inv.RemoveItem(req.Key, req.Value);
            int reward = QuestProgress.Scale(r.RewardAmount, done, t.QuestRewardGrowth);
            inv.AddItem(r.RewardPrefab, reward, 1, 0, 0L, "");

            QuestProgress.RecordCompletion(t.Id, t.QuestCap);
            string more = done < t.QuestCap ? " The next request will change." : "";
            player.Message(MessageHud.MessageType.Center,
                $"Quest complete! Received {reward} {ItemNames.Display(r.RewardPrefab)}.{more}");

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

            foreach (VillageMerchant m in Object.FindObjectsByType<VillageMerchant>(FindObjectsSortMode.None))
            {
                if (m == null)
                    continue;
                // Refresh every shop that reads this trader's reputation — its own merchants and any
                // secondary shop sharing the track (e.g. the Mountain Miner reading Mountain rep).
                if (VendorCatalog.ById(m.VendorTypeId).RepVendorId == t.UnlocksVendorId)
                    m.RefreshStock();
            }
        }
    }
}
