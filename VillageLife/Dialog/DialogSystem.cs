using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VillageLife.Config;
using VillageLife.Util;

namespace VillageLife.Dialog
{
    public enum DialogContext
    {
        Greeting,
        Morning,
        Evening,
        Rain,
        Storm,
        Combat,
        Idle,
        Night
    }

    /// <summary>
    /// Manages ambient dialog — context-sensitive spoken lines that appear above NPC heads.
    /// Lines are filtered by role, time of day, weather, and randomly weighted.
    /// </summary>
    public static class DialogSystem
    {
        private static DialogConfig _config;
        private static readonly System.Random _rng = new System.Random();

        public static void Initialize()
        {
            _config = ConfigManager.GetDialogConfig();
        }

        /// <summary>
        /// Get a random dialog line matching the given role and context.
        /// Falls back to "any" role if no role-specific line is found.
        /// </summary>
        public static string GetRandomLine(string role, DialogContext context)
        {
            if (_config == null || _config.Lines == null || _config.Lines.Count == 0)
                return null;

            string contextStr = context.ToString().ToLower();

            // Find matching lines (role-specific first, then "any")
            var candidates = _config.Lines
                .Where(l => (l.Role == role || l.Role == "any") && l.Context == contextStr)
                .ToList();

            if (candidates.Count == 0)
            {
                // Fall back to idle lines
                candidates = _config.Lines
                    .Where(l => (l.Role == role || l.Role == "any") && l.Context == "idle")
                    .ToList();
            }

            if (candidates.Count == 0)
                return null;

            // Weighted random selection
            float totalWeight = candidates.Sum(c => c.Weight);
            float roll = (float)(_rng.NextDouble() * totalWeight);
            float cumulative = 0f;

            foreach (var candidate in candidates)
            {
                cumulative += candidate.Weight;
                if (roll <= cumulative)
                    return candidate.Line;
            }

            return candidates.Last().Line;
        }

        /// <summary>
        /// Determine the current dialog context based on game state.
        /// </summary>
        public static DialogContext GetCurrentContext()
        {
            if (EnvMan.instance == null)
                return DialogContext.Idle;

            float dayFraction = EnvMan.instance.GetDayFraction();

            // Check weather
            string currentEnv = EnvMan.instance.GetCurrentEnvironment()?.m_name ?? "";
            if (currentEnv.Contains("Storm") || currentEnv.Contains("Thunder"))
                return DialogContext.Storm;
            if (currentEnv.Contains("Rain"))
                return DialogContext.Rain;

            // Check time of day
            if (dayFraction < Constants.DawnFraction || dayFraction >= Constants.SleepFraction)
                return DialogContext.Night;
            if (dayFraction < 0.35f)
                return DialogContext.Morning;
            if (dayFraction >= Constants.DuskFraction)
                return DialogContext.Evening;

            return DialogContext.Idle;
        }

        /// <summary>
        /// Check if there are enemies nearby (for combat dialog).
        /// </summary>
        public static bool IsInCombatArea(Vector3 position, float radius = 30f)
        {
            var characters = new List<Character>();
            Character.GetCharactersInRange(position, radius, characters);

            return characters.Any(c => c != null && !c.IsDead() &&
                                       c.GetFaction() != Character.Faction.Players &&
                                       c.GetComponent<Player>() == null);
        }
    }
}
