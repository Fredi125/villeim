using Jotunn.Managers;
using UnityEngine;
using VillageLife.Util;

namespace VillageLife.NPC
{
    /// <summary>
    /// Creates and registers the VillageLife NPC prefab.
    ///
    /// 2.0: Instead of cloning the Player prefab and stripping dozens of components
    /// (which caused SEMan duplicate-key crashes), we clone Haldor — the vanilla
    /// travelling trader. Haldor is already a friendly, idle-animated, non-combat
    /// Humanoid with no AI, which is exactly what a stationary villager needs.
    ///
    /// Cloning is done through Jötunn's <see cref="PrefabManager.CreateClonedPrefab"/>,
    /// which parents the clone to a disabled container so its Awake never fires during
    /// setup — the root cause of every prefab crash in 1.x.
    /// </summary>
    public static class NPCPrefabFactory
    {
        /// <summary>The vanilla prefab we base our NPC on.</summary>
        private const string BasePrefab = "Haldor";

        private static GameObject _npcPrefab;

        /// <summary>Clone Haldor, turn it into our NPC, and register it in ZNetScene.</summary>
        public static void Register()
        {
            // CreateClonedPrefab parents the clone to a disabled container (no Awake)
            // and registers it with the PrefabManager so it ends up in ZNetScene.
            _npcPrefab = PrefabManager.Instance.CreateClonedPrefab(Constants.NPCPrefabName, BasePrefab);
            if (_npcPrefab == null)
            {
                Jotunn.Logger.LogError($"[VillageLife] Could not clone '{BasePrefab}' for the NPC prefab.");
                return;
            }

            // Drop the vanilla trade behaviour so our VillageNPC interaction takes over.
            var trader = _npcPrefab.GetComponent<Trader>();
            if (trader != null)
                Object.DestroyImmediate(trader);

            // Strip any AI so the NPC stands at its post (no wandering, fleeing, or
            // day/night despawn). GetComponent returns null when absent, so this is
            // safe whether or not Haldor ships with an AI component.
            var monsterAI = _npcPrefab.GetComponent<MonsterAI>();
            if (monsterAI != null)
                Object.DestroyImmediate(monsterAI);
            var animalAI = _npcPrefab.GetComponent<AnimalAI>();
            if (animalAI != null)
                Object.DestroyImmediate(animalAI);

            // Persist the NPC with the world like any other saved creature.
            var nview = _npcPrefab.GetComponent<ZNetView>();
            if (nview != null)
                nview.m_persistent = true;

            // Friendly faction so nothing in the world treats it as an enemy.
            var humanoid = _npcPrefab.GetComponent<Humanoid>();
            if (humanoid != null)
            {
                humanoid.m_faction = Character.Faction.Players;
                humanoid.m_group = "villagelife";
            }

            // Attach our controller (ZDO persistence, role delegation, hover/interact).
            _npcPrefab.AddComponent<VillageNPC>();

            Jotunn.Logger.LogInfo("[VillageLife] NPC prefab registered.");
        }

        /// <summary>
        /// Best-effort visual customisation applied after an NPC is spawned.
        /// Failures here are purely cosmetic and never throw.
        /// </summary>
        public static void ApplyAppearance(VillageNPC npc)
        {
            if (npc == null) return;

            var vis = npc.GetComponent<VisEquipment>();
            if (vis == null) return;

            // 0 = male body, 1 = female body
            vis.SetModel(npc.IsMale ? 0 : 1);

            if (!string.IsNullOrEmpty(npc.HairStyle))
                vis.SetHairItem(npc.HairStyle);

            if (!string.IsNullOrEmpty(npc.BeardStyle) && npc.BeardStyle != "None")
                vis.SetBeardItem(npc.BeardStyle);
        }
    }
}
