using UnityEngine;

namespace VillageLife.UI
{
    /// <summary>
    /// Central UI manager that renders all VillageLife GUI panels.
    /// Attached to the plugin GameObject to receive OnGUI calls.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        private static UIManager _instance;

        public static void Initialize(GameObject parent)
        {
            if (_instance == null)
            {
                _instance = parent.AddComponent<UIManager>();
            }
        }

        private void OnGUI()
        {
            // Render active panels
            NPCCreationPanel.OnGUI();
            TradePanel.OnGUI();
            QuestPanel.OnGUI();
            QuestHUD.OnGUI();
        }

        private void Update()
        {
            // Close any open panel with Escape
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (NPCCreationPanel.IsVisible)
                    NPCCreationPanel.Hide();
                else if (TradePanel.IsVisible)
                    TradePanel.Hide();
                else if (QuestPanel.IsVisible)
                    QuestPanel.Hide();
            }
        }
    }
}
