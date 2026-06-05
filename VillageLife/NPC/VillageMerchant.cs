using UnityEngine;
using VillageLife.Util;

namespace VillageLife.NPC
{
    /// <summary>
    /// Turns a Haldor clone into a VillageLife merchant of a particular <see cref="VendorType"/>.
    ///
    /// The vanilla <see cref="Trader"/> component (kept on the clone) provides the hover text, the
    /// Use interaction, and the real shop window — so we deliberately add no Hoverable/Interactable
    /// of our own. Exactly one thing handles the Use key (two interactables on one object was the
    /// kind of fragile assumption that bit earlier builds).
    ///
    /// This component personalises the merchant: a name and a vendor-type id, both persisted in the
    /// ZDO so they survive saves and sync to other clients. On load it rebuilds the right shop stock
    /// from the type. If the stock can't be built, the Trader keeps its existing stock, so the shop
    /// is never empty.
    /// </summary>
    public class VillageMerchant : MonoBehaviour
    {
        private ZNetView _nview;
        private Trader _trader;

        public string MerchantName { get; private set; } = "Merchant";
        public string VendorTypeId { get; private set; } = "general";

        private void Awake()
        {
            _nview = GetComponent<ZNetView>();
            _trader = GetComponent<Trader>();
        }

        private void OnDestroy() => CancelInvoke();

        private void Start()
        {
            // Learn name + type from the ZDO. This is how clients that received us over the network,
            // and merchants reloaded with the world, recover what they were configured as.
            var zdo = _nview != null ? _nview.GetZDO() : null;
            if (zdo != null)
            {
                MerchantName = zdo.GetString(Constants.KeyName, MerchantName);
                VendorTypeId = zdo.GetString(Constants.KeyVendorType, VendorTypeId);
            }

            VillagerAppearance.Apply(gameObject, zdo);
            ApplyVendorType();
        }

        /// <summary>
        /// Called by <see cref="NpcSpawner"/> on the owning client right after instantiation,
        /// while this client still holds the fresh ZDO.
        /// </summary>
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

            ApplyVendorType();
        }

        /// <summary>Recompute the shop title and stock — call after this trader's reputation changes.</summary>
        public void RefreshStock() => ApplyVendorType();

        /// <summary>Set the shop title and goods for this merchant's vendor type and reputation.</summary>
        private void ApplyVendorType()
        {
            if (_trader == null)
                return;

            VendorType type = VendorCatalog.ById(VendorTypeId);
            int level = TraderReputation.Level(type.RepVendorId);
            int max = TraderReputation.MaxTierForTrack(type.RepVendorId);

            // Show reputation in the shop title when this trader has anything to unlock.
            _trader.m_name = max > 0
                ? $"{MerchantName} ({type.Title}) · Rep {level}/{max}"
                : $"{MerchantName} ({type.Title})";

            // Give the merchant village-flavoured greetings and idle chatter in place of Haldor's
            // lines. The vanilla Trader shows these itself; this is cosmetic and safe to re-apply.
            VillagerChatter.Apply(_trader, type);

            // Replace Haldor's default stock with ours (filtered by reputation) — but only if ours
            // actually built, otherwise leave the existing stock so the store still has something.
            var stock = MerchantStock.Build(type, level);
            if (stock.Count > 0)
            {
                _trader.m_items = stock;
            }
            else if (ObjectDB.instance == null)
            {
                // ObjectDB isn't populated this early in load, so Build returned nothing. Retry shortly
                // instead of sitting on the model's default (Haldor) stock for the whole session.
                Invoke(nameof(ApplyVendorType), 1f);
            }
            else
            {
                Jotunn.Logger.LogWarning(
                    $"[VillageLife] Merchant '{MerchantName}' ({type.Id}) built no stock; " +
                    "keeping default. Its item names may be unresolved.");
            }
        }
    }
}
