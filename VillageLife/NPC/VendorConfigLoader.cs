using System;
using System.IO;
using UnityEngine;

namespace VillageLife.NPC
{
    /// <summary>
    /// Loads the vendor catalogue from <c>BepInEx/config/VillageLife/vendors.json</c>.
    ///
    /// Behaviour is deliberately forgiving so a hand-edit can never break the mod:
    ///   • file missing      → write the defaults to it, use the defaults;
    ///   • file unreadable / malformed / empty → log a warning, use the built-in defaults;
    ///   • file valid        → use it.
    ///
    /// Uses Unity's built-in <see cref="JsonUtility"/> (no extra dependency). Because JsonUtility
    /// cannot (de)serialize a top-level array, the file is an object: <c>{ "vendors": [ ... ] }</c>.
    /// </summary>
    public static class VendorConfigLoader
    {
        [Serializable]
        private class Wrapper
        {
            public VendorType[] vendors;
        }

        public static void Load(string configRoot)
        {
            try
            {
                string dir = Path.Combine(configRoot, "VillageLife");
                string path = Path.Combine(dir, "vendors.json");

                if (!File.Exists(path))
                {
                    Directory.CreateDirectory(dir);
                    File.WriteAllText(path, Serialize(VendorCatalog.DefaultVendors));
                    VendorCatalog.Initialize(VendorCatalog.DefaultVendors);
                    Jotunn.Logger.LogInfo($"[VillageLife] Wrote default vendor config to {path}");
                    return;
                }

                string json = File.ReadAllText(path);
                Wrapper parsed = JsonUtility.FromJson<Wrapper>(json);

                if (parsed?.vendors == null || parsed.vendors.Length == 0)
                {
                    VendorCatalog.Initialize(null); // fall back to defaults
                    Jotunn.Logger.LogWarning(
                        $"[VillageLife] {path} had no vendors; using built-in defaults.");
                    return;
                }

                VendorCatalog.Initialize(parsed.vendors);
                Jotunn.Logger.LogInfo(
                    $"[VillageLife] Loaded {parsed.vendors.Length} vendor type(s) from {path}");
            }
            catch (Exception e)
            {
                // Any failure (bad JSON, IO error) must not stop the mod loading.
                VendorCatalog.Initialize(null);
                Jotunn.Logger.LogWarning(
                    $"[VillageLife] Failed to load vendors.json ({e.Message}); using built-in defaults.");
            }
        }

        private static string Serialize(VendorType[] vendors)
        {
            return JsonUtility.ToJson(new Wrapper { vendors = vendors }, prettyPrint: true);
        }
    }
}
