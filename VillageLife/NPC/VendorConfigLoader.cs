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
            public int configVersion;
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

                // Value/goods upgrade: if the file predates the current built-in defaults, replace
                // it with the new defaults (saving the old one to .bak) so price/goods changes here
                // take effect without the player deleting the file. Player edits at the current
                // version are preserved by the merge path below.
                int fileVersion = parsed?.configVersion ?? 0;
                if (fileVersion != VendorCatalog.ConfigVersion)
                {
                    string backup = path + ".bak";
                    try { File.Copy(path, backup, overwrite: true); }
                    catch { /* best-effort backup; regenerate regardless */ }

                    File.WriteAllText(path, Serialize(VendorCatalog.DefaultVendors));
                    VendorCatalog.Initialize(VendorCatalog.DefaultVendors);
                    Jotunn.Logger.LogInfo(
                        $"[VillageLife] vendors.json (v{fileVersion}) is older than the built-in " +
                        $"defaults (v{VendorCatalog.ConfigVersion}); regenerated from defaults " +
                        $"(previous saved to {Path.GetFileName(backup)}).");
                    return;
                }

                if (parsed?.vendors == null || parsed.vendors.Length == 0)
                {
                    VendorCatalog.Initialize(null); // fall back to defaults
                    Jotunn.Logger.LogWarning(
                        $"[VillageLife] {path} had no vendors; using built-in defaults.");
                    return;
                }

                // Upgrade path: append any built-in vendor whose id the file doesn't already have,
                // so new defaults (e.g. the biome vendors) appear after a mod update without
                // discarding the player's edits. If anything was added, rewrite the file.
                VendorType[] merged = MergeMissingDefaults(parsed.vendors, out bool added);
                VendorCatalog.Initialize(merged);
                if (added)
                    File.WriteAllText(path, Serialize(merged));

                Jotunn.Logger.LogInfo(
                    $"[VillageLife] Loaded {merged.Length} vendor type(s) from {path}" +
                    (added ? " (added new built-in vendors)." : "."));
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
            return JsonUtility.ToJson(
                new Wrapper { configVersion = VendorCatalog.ConfigVersion, vendors = vendors },
                prettyPrint: true);
        }

        /// <summary>
        /// Return <paramref name="existing"/> plus any built-in default vendor whose id isn't
        /// already present. Existing entries are preserved exactly (player edits win); only
        /// genuinely missing ids are appended. <paramref name="added"/> reports whether anything
        /// was appended so the caller can persist the upgraded file.
        /// </summary>
        private static VendorType[] MergeMissingDefaults(VendorType[] existing, out bool added)
        {
            added = false;
            var result = new System.Collections.Generic.List<VendorType>(existing);

            foreach (VendorType def in VendorCatalog.DefaultVendors)
            {
                bool present = false;
                foreach (VendorType cur in existing)
                {
                    if (cur != null && cur.Id == def.Id)
                    {
                        present = true;
                        break;
                    }
                }
                if (!present)
                {
                    result.Add(def);
                    added = true;
                }
            }

            return result.ToArray();
        }
    }
}
