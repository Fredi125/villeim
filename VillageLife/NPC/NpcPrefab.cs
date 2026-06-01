using Jotunn.Managers;
using UnityEngine;
using VillageLife.Util;

namespace VillageLife.NPC
{
    /// <summary>
    /// Registers the villager: a friendly, persistent clone of Haldor, added as a PLAIN
    /// custom prefab (not a Jötunn "creature").
    ///
    /// Why not CreatureManager: the game log proved Haldor has no Character/BaseAI/Rigidbody/
    /// ZSyncAnimation/CharacterAnimEvent — he is a special stationary, non-killable interactable
    /// NPC, not a spawn-system creature. CreatureManager enforces that full monster contract and
    /// rejected the clone ("not valid"). Valheim itself treats Haldor as a location-placed prefab,
    /// so we mirror that: clone via PrefabManager and register with AddPrefab, which injects it
    /// into ZNetScene (so ZNetScene.GetPrefab("VL_Villager") resolves and it reloads with the world).
    ///
    /// A no-Character villager also can't enter the global character list, so it cannot trigger
    /// the per-frame EnemyHud / GetCharactersInRange crashes seen with the old Player-based NPCs.
    /// </summary>
    public static class NpcPrefab
    {
        public static void Register()
        {
            // Clone Haldor via PrefabManager (ZNetScene + cache lookup, where Haldor resolves).
            GameObject prefab = PrefabManager.Instance.CreateClonedPrefab(
                Constants.NpcPrefabName, Constants.NpcBasePrefab);
            if (prefab == null)
            {
                Jotunn.Logger.LogError(
                    $"[VillageLife] Could not clone '{Constants.NpcBasePrefab}' for the villager.");
                return;
            }

            // Drop Haldor's trade behaviour so our VillageNpc.Interact owns the Use key.
            var trader = prefab.GetComponent<Trader>();
            if (trader != null)
                Object.DestroyImmediate(trader);

            // Persist with the world like any other saved networked object.
            var nview = prefab.GetComponent<ZNetView>();
            if (nview != null)
                nview.m_persistent = true;

            // Attach our controller: hover name + "talk" greeting + ZDO-backed name.
            if (prefab.GetComponent<VillageNpc>() == null)
                prefab.AddComponent<VillageNpc>();

            // Register as a plain prefab; Jötunn injects it into ZNetScene on every world load.
            if (PrefabManager.Instance.AddPrefab(prefab))
                Jotunn.Logger.LogInfo("[VillageLife] Villager prefab registered.");
            else
                Jotunn.Logger.LogWarning("[VillageLife] Villager prefab was not added (already registered?).");
        }
    }
}
