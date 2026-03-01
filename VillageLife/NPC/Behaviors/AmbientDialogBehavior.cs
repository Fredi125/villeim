using UnityEngine;
using VillageLife.Dialog;
using VillageLife.Util;

namespace VillageLife.NPC.Behaviors
{
    /// <summary>
    /// Periodically displays ambient dialog text above the NPC's head.
    /// Context-sensitive: considers time of day, weather, and nearby threats.
    /// </summary>
    public class AmbientDialogBehavior : MonoBehaviour
    {
        private VillageNPC _npc;
        private float _nextDialogTime;
        private float _displayTimer;
        private string _currentLine;
        private bool _showing;

        private void Awake()
        {
            _npc = GetComponent<VillageNPC>();
            ResetTimer();
        }

        private void Update()
        {
            if (!Plugin.VillageLifePlugin.EnableAmbientDialog.Value)
                return;

            if (_npc == null) return;

            // Don't talk while sleeping
            if (_npc.BehaviorFSM != null && _npc.BehaviorFSM.CurrentState == BehaviorState.Sleeping)
                return;

            _nextDialogTime -= Time.deltaTime;

            if (_nextDialogTime <= 0f)
            {
                ShowAmbientDialog();
                ResetTimer();
            }

            if (_showing)
            {
                _displayTimer -= Time.deltaTime;
                if (_displayTimer <= 0f)
                {
                    _showing = false;
                    _currentLine = null;
                }
            }
        }

        private void ShowAmbientDialog()
        {
            // Check for combat first
            DialogContext context;
            if (DialogSystem.IsInCombatArea(transform.position))
            {
                context = DialogContext.Combat;
            }
            else
            {
                context = DialogSystem.GetCurrentContext();
            }

            string line = DialogSystem.GetRandomLine(_npc.RoleId, context);
            if (string.IsNullOrEmpty(line)) return;

            _currentLine = line;
            _showing = true;
            _displayTimer = Constants.DialogDisplaySeconds;

            // Display ambient dialog as a message when player is nearby
            if (Player.m_localPlayer != null)
            {
                float dist = Vector3.Distance(transform.position, Player.m_localPlayer.transform.position);
                if (dist < 15f)
                {
                    MessageHud.instance?.ShowMessage(MessageHud.MessageType.TopLeft,
                        $"{_npc.NPCName}: \"{line}\"");
                }
            }
        }

        private void ResetTimer()
        {
            _nextDialogTime = Random.Range(
                Constants.AmbientDialogIntervalMin,
                Constants.AmbientDialogIntervalMax);
        }
    }
}
