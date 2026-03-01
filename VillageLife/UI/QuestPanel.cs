using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VillageLife.NPC;
using VillageLife.Quest;

namespace VillageLife.UI
{
    /// <summary>
    /// UI panel for quest interactions — offering quests, showing progress, and completing quests.
    /// </summary>
    public static class QuestPanel
    {
        private enum Mode { Offer, Progress, Completion, NoQuests }

        private static bool _visible;
        private static Mode _mode;
        private static VillageNPC _npc;
        private static Player _player;
        private static List<QuestDefinition> _availableQuests;
        private static ActiveQuest _activeQuest;
        private static int _selectedQuestIndex;
        private static Rect _windowRect = new Rect(Screen.width / 2 - 225, Screen.height / 2 - 200, 450, 400);
        private static Vector2 _scrollPos;

        public static bool IsVisible => _visible;

        public static void ShowOffer(VillageNPC npc, Player player, List<QuestDefinition> quests)
        {
            _npc = npc;
            _player = player;
            _availableQuests = quests;
            _activeQuest = null;
            _selectedQuestIndex = 0;
            _mode = Mode.Offer;
            _visible = true;
            GUIManager.BlockInput(true);
        }

        public static void ShowProgress(VillageNPC npc, Player player, ActiveQuest quest)
        {
            _npc = npc;
            _player = player;
            _activeQuest = quest;
            _availableQuests = null;
            _mode = Mode.Progress;
            _visible = true;
            GUIManager.BlockInput(true);
        }

        public static void ShowCompletion(VillageNPC npc, Player player, ActiveQuest quest)
        {
            _npc = npc;
            _player = player;
            _activeQuest = quest;
            _availableQuests = null;
            _mode = Mode.Completion;
            _visible = true;
            GUIManager.BlockInput(true);
        }

        public static void ShowNoQuests(VillageNPC npc, Player player)
        {
            _npc = npc;
            _player = player;
            _mode = Mode.NoQuests;
            _visible = true;
            GUIManager.BlockInput(true);
        }

        public static void Hide()
        {
            _visible = false;
            _npc = null;
            _player = null;
            _availableQuests = null;
            _activeQuest = null;
            GUIManager.BlockInput(false);
        }

        public static void OnGUI()
        {
            if (!_visible) return;

            GUI.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.95f);
            string title = _mode switch
            {
                Mode.Offer => $"{_npc?.NPCName ?? "Quest Giver"} — Available Quests",
                Mode.Progress => $"{_npc?.NPCName ?? "Quest Giver"} — Quest Progress",
                Mode.Completion => $"{_npc?.NPCName ?? "Quest Giver"} — Quest Complete!",
                Mode.NoQuests => $"{_npc?.NPCName ?? "Quest Giver"}",
                _ => "Quest"
            };

            _windowRect = GUI.Window(9003, _windowRect, DrawWindow, title);
        }

        private static void DrawWindow(int windowId)
        {
            var labelStyle = new GUIStyle(GUI.skin.label) { richText = true, fontSize = 13, wordWrap = true };
            var headerStyle = new GUIStyle(GUI.skin.label) { richText = true, fontSize = 15, fontStyle = FontStyle.Bold };

            switch (_mode)
            {
                case Mode.Offer:
                    DrawOfferMode(labelStyle, headerStyle);
                    break;
                case Mode.Progress:
                    DrawProgressMode(labelStyle, headerStyle);
                    break;
                case Mode.Completion:
                    DrawCompletionMode(labelStyle, headerStyle);
                    break;
                case Mode.NoQuests:
                    DrawNoQuestsMode(labelStyle);
                    break;
            }

            GUILayout.Space(10);

            if (GUILayout.Button("Close", GUILayout.Height(30)))
                Hide();

            GUI.DragWindow();
        }

        private static void DrawOfferMode(GUIStyle labelStyle, GUIStyle headerStyle)
        {
            if (_availableQuests == null || _availableQuests.Count == 0) return;

            // Quest selection list
            _scrollPos = GUILayout.BeginScrollView(_scrollPos, GUILayout.Height(120));
            for (int i = 0; i < _availableQuests.Count; i++)
            {
                var quest = _availableQuests[i];
                bool selected = i == _selectedQuestIndex;
                string style = selected ? $"<color=yellow><b>► {quest.Title}</b></color>" : quest.Title;

                if (GUILayout.Button(style, labelStyle))
                    _selectedQuestIndex = i;
            }
            GUILayout.EndScrollView();

            GUILayout.Space(10);

            // Selected quest details
            if (_selectedQuestIndex < _availableQuests.Count)
            {
                var quest = _availableQuests[_selectedQuestIndex];

                GUILayout.Label($"<b>{quest.Title}</b>", headerStyle);

                // NPC dialog
                GUILayout.Label($"<i>\"{quest.Dialog.Offer}\"</i>", labelStyle);
                GUILayout.Space(5);

                GUILayout.Label(quest.Description, labelStyle);
                GUILayout.Space(5);

                // Objectives
                GUILayout.Label("<b>Objectives:</b>", labelStyle);
                foreach (var obj in quest.Objectives)
                {
                    GUILayout.Label($"  • {obj.Type}: {obj.Target} x{obj.Count}", labelStyle);
                }

                GUILayout.Space(5);

                // Rewards
                GUILayout.Label("<b>Rewards:</b>", labelStyle);
                foreach (var reward in quest.Rewards)
                {
                    GUILayout.Label($"  • {reward.ItemName} x{reward.Amount}", labelStyle);
                }

                GUILayout.Space(10);

                if (GUILayout.Button("Accept Quest", GUILayout.Height(35)))
                {
                    var activeQuest = QuestEngine.AcceptQuest(_player, quest, _npc.GetZDOID());
                    if (activeQuest != null)
                    {
                        MessageHud.instance?.ShowMessage(MessageHud.MessageType.Center,
                            $"Quest accepted: {quest.Title}");
                        Hide();
                    }
                }
            }
        }

        private static void DrawProgressMode(GUIStyle labelStyle, GUIStyle headerStyle)
        {
            if (_activeQuest == null) return;

            GUILayout.Label($"<b>{_activeQuest.Definition.Title}</b>", headerStyle);

            // NPC dialog
            GUILayout.Label($"<i>\"{_activeQuest.Definition.Dialog.Progress}\"</i>", labelStyle);
            GUILayout.Space(10);

            // Progress
            GUILayout.Label("<b>Progress:</b>", labelStyle);
            foreach (var obj in _activeQuest.Objectives)
            {
                float progress = obj.GetProgress();
                GUILayout.Label($"  {obj.GetDescription()}", labelStyle);

                // Progress bar
                var rect = GUILayoutUtility.GetRect(100, 20);
                GUI.Box(rect, "");
                var fillRect = new Rect(rect.x + 2, rect.y + 2, (rect.width - 4) * progress, rect.height - 4);
                GUI.DrawTexture(fillRect, Texture2D.whiteTexture);
            }

            GUILayout.Space(5);
            GUILayout.Label($"Overall: {(_activeQuest.GetOverallProgress() * 100):F0}%", labelStyle);
        }

        private static void DrawCompletionMode(GUIStyle labelStyle, GUIStyle headerStyle)
        {
            if (_activeQuest == null) return;

            GUILayout.Label($"<color=green><b>{_activeQuest.Definition.Title} — Complete!</b></color>", headerStyle);
            GUILayout.Space(5);

            // NPC dialog
            GUILayout.Label($"<i>\"{_activeQuest.Definition.Dialog.Complete}\"</i>", labelStyle);
            GUILayout.Space(10);

            // Rewards
            GUILayout.Label("<b>Rewards:</b>", labelStyle);
            foreach (var reward in _activeQuest.Definition.Rewards)
            {
                GUILayout.Label($"  <color=yellow>• {reward.ItemName} x{reward.Amount}</color>", labelStyle);
            }

            GUILayout.Space(15);

            if (GUILayout.Button("Collect Rewards", GUILayout.Height(40)))
            {
                if (QuestEngine.CompleteQuest(_player, _activeQuest))
                {
                    MessageHud.instance?.ShowMessage(MessageHud.MessageType.Center,
                        $"Quest complete: {_activeQuest.Definition.Title}!");
                    Hide();
                }
            }
        }

        private static void DrawNoQuestsMode(GUIStyle labelStyle)
        {
            GUILayout.Space(20);
            GUILayout.Label($"<i>\"{_npc?.NPCName ?? "Quest Giver"} shakes their head.\"</i>", labelStyle);
            GUILayout.Space(10);
            GUILayout.Label("No quests available right now. Check back later.", labelStyle);
        }
    }
}
