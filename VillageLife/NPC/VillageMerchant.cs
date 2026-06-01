using UnityEngine;
using VillageLife.Util;

namespace VillageLife.NPC
{
    /// <summary>
    /// Turns a Haldor clone into a VillageLife merchant.
    ///
    /// The vanilla <see cref="Trader"/> component (kept on the clone) already provides the hover
    /// text, the Use interaction, and the real shop window — so we deliberately do NOT add our own
    /// Hoverable/Interactable. That keeps exactly one thing handling the Use key (the ambiguity of
    /// two interactables on one object was the kind of fragile assumption that bit earlier builds).
    ///
    /// This component only customises the merchant: a per-merchant name (persisted in the ZDO so it
    /// survives saves and syncs to other clients) and the goods list. If our stock can't be built,
    /// the Trader keeps whatever stock it already had, so the shop is never empty.
    /// </summary>
    public class VillageMerchant : MonoBehaviour
    {
        private ZNetView _nview;
        private Trader _trader;

        public string MerchantName { get; private set; } = "Merchant";

        private void Awake()
        {
            _nview = GetComponent<ZNetView>();
            _trader = GetComponent<Trader>();
        }

        private void Start()
        {
            // Learn our name from the ZDO (this is how clients that received us over the network,
            // and reloaded merchants, pick up the name set when we were first summoned).
            var zdo = _nview != null ? _nview.GetZDO() : null;
            if (zdo != null)
                MerchantName = zdo.GetString(Constants.KeyName, MerchantName);

            if (_trader == null)
                return;

            _trader.m_name = MerchantName;

            // Replace Haldor's default stock with ours — but only if ours actually built,
            // otherwise leave the existing stock so the store still has something to sell.
            var stock = MerchantStock.Build();
            if (stock.Count > 0)
                _trader.m_items = stock;
        }

        /// <summary>
        /// Called by <see cref="NpcSpawner"/> on the owning client right after instantiation,
        /// while this client still holds the fresh ZDO.
        /// </summary>
        public void Initialize(string name, long creatorId)
        {
            if (!string.IsNullOrEmpty(name))
                MerchantName = name;

            var zdo = _nview != null ? _nview.GetZDO() : null;
            if (zdo != null)
            {
                zdo.Set(Constants.KeyName, MerchantName);
                zdo.Set(Constants.KeyCreator, creatorId);
            }

            if (_trader != null)
                _trader.m_name = MerchantName;
        }
    }
}
