using System.Collections.Generic;
using System.Text;
using VillageLife.Building;

namespace VillageLife.NPC
{
    /// <summary>
    /// One-time startup check that every item prefab the mod references actually resolves in
    /// ObjectDB: vendor goods, barter cost/give items, and station build requirements. Any name
    /// that doesn't resolve is reported in a single consolidated warning.
    ///
    /// Why this exists: the runtime paths fail <i>silently</i> on a bad name — an unresolved good is
    /// skipped (<see cref="MerchantStock"/>) and an unresolved build requirement is dropped by
    /// Jötunn — so a typo just makes a shop smaller or a station cheaper, with nothing to point at.
    /// This audit turns all of that into one actionable line in the BepInEx log.
    ///
    /// It runs at OnVanillaPrefabsAvailable, the same moment Jötunn resolves piece requirements, so
    /// what it reports is exactly what the game will see. It is purely diagnostic and changes no data.
    /// </summary>
    public static class ItemNameAudit
    {
        public static void Run()
        {
            ObjectDB odb = ObjectDB.instance;
            if (odb == null || odb.m_items == null || odb.m_items.Count == 0)
            {
                // ObjectDB isn't populated yet; the runtime resolve paths guard themselves, so we
                // simply skip rather than report every name as "missing".
                Jotunn.Logger.LogDebug("[VillageLife] Item-name audit skipped: ObjectDB not ready.");
                return;
            }

            // name -> the places that reference it (so the log says where to fix each one).
            var unresolved = new SortedDictionary<string, List<string>>();

            void Check(string name, string source)
            {
                if (string.IsNullOrEmpty(name) || ItemNames.Exists(name))
                    return;
                if (!unresolved.TryGetValue(name, out List<string> sources))
                    unresolved[name] = sources = new List<string>();
                if (!sources.Contains(source))
                    sources.Add(source);
            }

            foreach (VendorType v in VendorCatalog.All)
            {
                if (v == null)
                    continue;

                if (v.IsBarter)
                {
                    Check(v.CostPrefab, $"barter '{v.Id}' cost");
                    Check(v.GivePrefab, $"barter '{v.Id}' give");
                }
                else if (v.Goods != null)
                {
                    foreach (VendorGood g in v.Goods)
                        Check(g.Prefab, $"vendor '{v.Id}'");
                }

                if (v.QuestItems != null)
                    foreach (QuestItem q in v.QuestItems)
                        Check(q.Prefab, $"quest '{v.Id}'");
            }

            foreach ((string station, string item) in VillageStations.RequirementItems())
                Check(item, $"station '{station}'");

            if (unresolved.Count == 0)
            {
                Jotunn.Logger.LogInfo("[VillageLife] Item-name audit: all referenced item prefabs resolve.");
                return;
            }

            var sb = new StringBuilder();
            sb.Append($"[VillageLife] Item-name audit found {unresolved.Count} unresolved prefab name(s). ");
            sb.Append("These are silently skipped in-game (smaller shop / cheaper station); ");
            sb.Append("fix the name in vendors.json or the station definition:");
            foreach (KeyValuePair<string, List<string>> kv in unresolved)
                sb.Append($"\n  - '{kv.Key}'  (used by: {string.Join(", ", kv.Value)})");

            Jotunn.Logger.LogWarning(sb.ToString());
        }
    }
}
