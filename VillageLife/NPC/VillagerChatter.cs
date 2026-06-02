using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace VillageLife.NPC
{
    /// <summary>
    /// Gives coin merchants village-flavoured chatter by replacing the vanilla <see cref="Trader"/>'s
    /// built-in greeting/idle text lists. The Trader already drives these bubbles itself (its own
    /// Update periodically calls <c>Chat.SetNpcText</c> and greets a nearby player), so we add no API
    /// of our own — we only swap the words for ones that suit a village shopkeeper instead of Haldor.
    ///
    /// The lists are assigned by reflection and any field that isn't a <c>List&lt;string&gt;</c> on the
    /// running game build is skipped. So a future Trader rename can never break the build or throw at
    /// runtime — that one list simply keeps Haldor's default. Purely cosmetic, like <see cref="Greetings"/>.
    /// </summary>
    public static class VillagerChatter
    {
        // Shown when a player comes within greeting range of the shop.
        private static readonly string[] Greets =
        {
            "Welcome, friend!",
            "Ah, a customer — come closer.",
            "Good to see a friendly face.",
            "Looking for something today?",
            "Step right up!",
        };

        // Shown when a player wanders back out of range.
        private static readonly string[] Goodbye =
        {
            "Safe travels!",
            "Come back soon.",
            "Mind the wolves out there.",
            "Until next time, friend.",
        };

        // Shown when the trade window opens.
        private static readonly string[] StartTrade =
        {
            "Let's see what you need.",
            "Take your time browsing.",
            "Good wares, fair prices.",
        };

        // Shown after a purchase (and, where the build uses it, a sale).
        private static readonly string[] Buy =
        {
            "A fine choice!",
            "Pleasure doing business.",
            "May it serve you well.",
        };

        // Idle one-liners said on the Trader's own timer; generic lines plus a little biome flavour.
        private static readonly string[] TalkGeneral =
        {
            "A fine day for trade.",
            "The village grows livelier every day.",
            "Coin keeps the hearth warm.",
            "Hard work builds a home.",
        };

        /// <summary>Replace the merchant's greeting/idle lines with village-flavoured ones.</summary>
        public static void Apply(Trader trader, VendorType type)
        {
            if (trader == null)
                return;

            SetList(trader, "m_randomGreets", Greets);
            SetList(trader, "m_randomGoodbye", Goodbye);
            SetList(trader, "m_randomStartTrade", StartTrade);
            SetList(trader, "m_randomBuy", Buy);
            SetList(trader, "m_randomSell", Buy);
            SetList(trader, "m_randomTalk", Talk(type));
        }

        // --- Ambient bubbles for villagers WITHOUT a Trader (barterers, guards). The Trader drives
        // merchant chatter itself; here we trigger Chat.SetNpcText ourselves, resolved by reflection
        // so a signature change just means no bubble rather than a crash. ---

        public static readonly string[] BartererTalk =
        {
            "Fair trades, always.",
            "Got something to swap?",
            "One villager's scrap is another's treasure.",
            "No coin needed here — just goods.",
            "Bring me what you've got.",
        };

        public static readonly string[] BountyTalk =
        {
            "Trophies for coin — that's the deal.",
            "The wilds won't cull themselves.",
            "Dangerous work, good pay.",
            "Bring me proof of the kill.",
            "Another beast for the bounty?",
        };

        public static readonly string[] GuardTalk =
        {
            "All quiet on the watch.",
            "Nothing gets past me.",
            "Stay sharp out there.",
            "Eyes on the perimeter.",
            "Let them come.",
        };

        private static MethodInfo _setNpcText;
        private static bool _setNpcTextLookedUp;

        private static MethodInfo NpcTextMethod()
        {
            if (!_setNpcTextLookedUp)
            {
                _setNpcTextLookedUp = true;
                try { _setNpcText = typeof(Chat).GetMethod("SetNpcText", BindingFlags.Public | BindingFlags.Instance); }
                catch { _setNpcText = null; }
            }
            return _setNpcText;
        }

        /// <summary>Show a random line as a chat bubble over a villager that has no Trader of its own.
        /// Uses the vanilla bubble call via reflection; any mismatch simply shows nothing.</summary>
        public static void Say(GameObject npc, string[] lines)
        {
            if (npc == null || lines == null || lines.Length == 0)
                return;

            Chat chat = Chat.instance;
            MethodInfo method = NpcTextMethod();
            if (chat == null || method == null)
                return;

            try
            {
                string text = lines[UnityEngine.Random.Range(0, lines.Length)];
                method.Invoke(chat, BuildNpcTextArgs(method.GetParameters(), npc, text));
            }
            catch
            {
                // Cosmetic only — a chatter bubble must never throw.
            }
        }

        /// <summary>Build an argument list for Chat.SetNpcText by matching parameter types, so it works
        /// across signature variants: the talker GameObject, an upward offset, ranges, and the text
        /// (assumed to be the last string parameter; an earlier string is the optional topic).</summary>
        private static object[] BuildNpcTextArgs(ParameterInfo[] pars, GameObject npc, string text)
        {
            int lastString = -1;
            for (int i = 0; i < pars.Length; i++)
                if (pars[i].ParameterType == typeof(string))
                    lastString = i;

            var args = new object[pars.Length];
            int floatsSeen = 0;
            for (int i = 0; i < pars.Length; i++)
            {
                Type pt = pars[i].ParameterType;
                if (pt == typeof(GameObject)) args[i] = npc;
                else if (pt == typeof(Vector3)) args[i] = Vector3.up * 1.6f;
                else if (pt == typeof(string)) args[i] = (i == lastString) ? text : "";
                else if (pt == typeof(float)) args[i] = (floatsSeen++ == 0) ? 24f : 8f;
                else if (pt == typeof(bool)) args[i] = false;
                else if (pt == typeof(int)) args[i] = 0;
                else args[i] = pt.IsValueType ? Activator.CreateInstance(pt) : null;
            }
            return args;
        }

        /// <summary>Generic idle lines plus any that suit the vendor's biome.</summary>
        private static List<string> Talk(VendorType type)
        {
            var lines = new List<string>(TalkGeneral);
            switch (type?.Biome)
            {
                case "Meadows":
                    lines.Add("Nothing beats a calm meadow morning.");
                    lines.Add("The bees are busy today.");
                    break;
                case "BlackForest":
                    lines.Add("Keep your axe handy in these woods.");
                    lines.Add("The greydwarves are restless lately.");
                    break;
                case "Swamp":
                    lines.Add("Mind the leeches by the water.");
                    lines.Add("This damp gets into your bones.");
                    break;
                case "Mountain":
                    lines.Add("Bundle up — the peaks are bitter.");
                    lines.Add("Watch the skies for drakes.");
                    break;
                case "Plains":
                    lines.Add("The fields stretch on forever out here.");
                    lines.Add("Listen for the lox at dusk.");
                    break;
            }
            return lines;
        }

        /// <summary>
        /// Assign a <c>List&lt;string&gt;</c> field on the Trader by name, if it exists and has that
        /// type. Any mismatch (renamed/removed field) is skipped — chatter is cosmetic, never critical.
        /// </summary>
        private static void SetList(Trader trader, string fieldName, IEnumerable<string> values)
        {
            try
            {
                FieldInfo fi = typeof(Trader).GetField(fieldName, BindingFlags.Public | BindingFlags.Instance);
                if (fi != null && fi.FieldType == typeof(List<string>))
                    fi.SetValue(trader, new List<string>(values));
            }
            catch
            {
                // A cosmetic text swap must never throw; leave Haldor's default for this list.
            }
        }
    }
}
