using UnityEngine;
using VillageLife.Dialog;
using VillageLife.Util;

namespace VillageLife.NPC.Behaviors
{
    /// <summary>
    /// Periodically surfaces a context-aware ambient line for the NPC (time of day,
    /// weather, nearby combat). Shown in the message HUD when the local player is close.
    /// Attached automatically by <see cref="VillageNPC"/>.
    /// </summary>
    public class AmbientDialogBehavior : MonoBehaviour
    {
        private VillageNPC _npc;
        private float _nextDialogTime;

        private void Awake()
        {
            _npc = GetComponent<VillageNPC>();
            ResetTimer();
        }

        private void Update()
        {
            if (!Plugin.VillageLifePlugin.EnableAmbientDialog.Value)
                return;
            if (_npc == null)
                return;

            _nextDialogTime -= Time.deltaTime;
            if (_nextDialogTime <= 0f)
            {
                ShowAmbientDialog();
                ResetTimer();
            }
        }

        private void ShowAmbientDialog()
        {
            // Only chatter when the local player is nearby enough to read it.
            var player = Player.m_localPlayer;
            if (player == null)
                return;
            if (Vector3.Distance(transform.position, player.transform.position) > 15f)
                return;

            DialogContext context = DialogSystem.IsInCombatArea(transform.position)
                ? DialogContext.Combat
                : DialogSystem.GetCurrentContext();

            string line = DialogSystem.GetRandomLine(_npc.RoleId, context);
            if (string.IsNullOrEmpty(line))
                return;

            MessageHud.instance?.ShowMessage(MessageHud.MessageType.TopLeft,
                $"{_npc.NPCName}: \"{line}\"");
        }

        private void ResetTimer()
        {
            _nextDialogTime = Random.Range(
                Constants.AmbientDialogIntervalMin,
                Constants.AmbientDialogIntervalMax);
        }
    }
}
