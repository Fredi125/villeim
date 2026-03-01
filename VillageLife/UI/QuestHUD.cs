using System.Collections.Generic;
using UnityEngine;
using VillageLife.Quest;

namespace VillageLife.UI
{
    /// <summary>
    /// Small HUD overlay showing active quest objectives and progress.
    /// Displayed in the top-right corner of the screen.
    /// </summary>
    public static class QuestHUD
    {
        private static readonly Rect _hudArea = new Rect(Screen.width - 320, 10, 310, 400);

        public static void OnGUI()
        {
            if (!Plugin.VillageLifePlugin.EnableQuestSystem.Value)
                return;

            var player = Player.m_localPlayer;
            if (player == null) return;

            var activeQuests = QuestEngine.GetAllActiveQuests(player);
            if (activeQuests.Count == 0) return;

            var labelStyle = new GUIStyle(GUI.skin.label)
            {
                richText = true,
                fontSize = 12,
                wordWrap = true
            };

            var headerStyle = new GUIStyle(labelStyle)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold
            };

            GUILayout.BeginArea(_hudArea);

            GUILayout.Label("<color=#DDDDDD><b>Active Quests</b></color>", headerStyle);
            GUILayout.Space(3);

            int maxDisplay = 3;
            int displayed = 0;

            foreach (var quest in activeQuests)
            {
                if (displayed >= maxDisplay) break;

                // Quest title
                float progress = quest.GetOverallProgress();
                string color = progress >= 1f ? "green" : "yellow";
                GUILayout.Label($"<color={color}>■</color> <b>{quest.Definition.Title}</b>", labelStyle);

                // Show objectives
                foreach (var obj in quest.Objectives)
                {
                    string objColor = obj.IsComplete() ? "#88FF88" : "#CCCCCC";
                    string check = obj.IsComplete() ? "✓" : "○";
                    GUILayout.Label($"  <color={objColor}>{check} {obj.GetDescription()}</color>", labelStyle);
                }

                GUILayout.Space(5);
                displayed++;
            }

            if (activeQuests.Count > maxDisplay)
            {
                GUILayout.Label($"<color=#888888>+{activeQuests.Count - maxDisplay} more quest(s)</color>", labelStyle);
            }

            GUILayout.EndArea();
        }
    }
}
