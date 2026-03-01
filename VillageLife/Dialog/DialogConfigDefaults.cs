using VillageLife.Config;
using System.Collections.Generic;

namespace VillageLife.Dialog
{
    /// <summary>
    /// Default dialog lines for all roles and contexts.
    /// Over 60 lines covering greetings, time-of-day, weather, and idle chatter.
    /// </summary>
    public static class DialogConfigDefaults
    {
        public static DialogConfig Create()
        {
            return new DialogConfig
            {
                Lines = new List<DialogLineConfig>
                {
                    // ===== GREETINGS (all roles) =====
                    new DialogLineConfig { Role = "any", Context = "greeting", Line = "Good to see you!", Weight = 1f },
                    new DialogLineConfig { Role = "any", Context = "greeting", Line = "Welcome back, friend.", Weight = 1f },
                    new DialogLineConfig { Role = "any", Context = "greeting", Line = "Hail, warrior!", Weight = 1f },
                    new DialogLineConfig { Role = "any", Context = "greeting", Line = "The gods smile upon your arrival.", Weight = 0.5f },

                    // Merchant greetings
                    new DialogLineConfig { Role = "merchant", Context = "greeting", Line = "Looking to trade? I've got fine wares!", Weight = 1.5f },
                    new DialogLineConfig { Role = "merchant", Context = "greeting", Line = "Step right up! Best prices in the village.", Weight = 1.5f },
                    new DialogLineConfig { Role = "merchant", Context = "greeting", Line = "Ah, a customer! What can I get you?", Weight = 1.5f },

                    // Quest giver greetings
                    new DialogLineConfig { Role = "quest_giver", Context = "greeting", Line = "I might have work for someone like you...", Weight = 1.5f },
                    new DialogLineConfig { Role = "quest_giver", Context = "greeting", Line = "You look capable. Interested in a task?", Weight = 1.5f },

                    // Guard greetings
                    new DialogLineConfig { Role = "guard", Context = "greeting", Line = "All quiet on my watch.", Weight = 1f },
                    new DialogLineConfig { Role = "guard", Context = "greeting", Line = "Stay safe out there.", Weight = 1f },

                    // ===== MORNING =====
                    new DialogLineConfig { Role = "any", Context = "morning", Line = "Another beautiful dawn!", Weight = 1f },
                    new DialogLineConfig { Role = "any", Context = "morning", Line = "The morning air is crisp and clean.", Weight = 1f },
                    new DialogLineConfig { Role = "any", Context = "morning", Line = "A new day, a new adventure.", Weight = 1f },
                    new DialogLineConfig { Role = "merchant", Context = "morning", Line = "Just opened shop! Fresh stock today.", Weight = 1.5f },
                    new DialogLineConfig { Role = "guard", Context = "morning", Line = "Morning patrol. All clear so far.", Weight = 1f },
                    new DialogLineConfig { Role = "villager", Context = "morning", Line = "*yawns* Good morning...", Weight = 1f },
                    new DialogLineConfig { Role = "villager", Context = "morning", Line = "Slept like a log. Ready for the day!", Weight = 1f },

                    // ===== EVENING =====
                    new DialogLineConfig { Role = "any", Context = "evening", Line = "The sun's going down. Time to head inside.", Weight = 1f },
                    new DialogLineConfig { Role = "any", Context = "evening", Line = "Beautiful sunset tonight.", Weight = 1f },
                    new DialogLineConfig { Role = "merchant", Context = "evening", Line = "Closing up shop soon. Last chance for deals!", Weight = 1.5f },
                    new DialogLineConfig { Role = "guard", Context = "evening", Line = "Night watch coming up. I'll keep my eyes peeled.", Weight = 1f },
                    new DialogLineConfig { Role = "guard", Context = "evening", Line = "The dark things come out at night. Stay close.", Weight = 1f },
                    new DialogLineConfig { Role = "villager", Context = "evening", Line = "Time for supper and a warm fire.", Weight = 1f },
                    new DialogLineConfig { Role = "villager", Context = "evening", Line = "Another day survived in the tenth world.", Weight = 0.8f },

                    // ===== NIGHT =====
                    new DialogLineConfig { Role = "any", Context = "night", Line = "...zzz...", Weight = 0.5f },
                    new DialogLineConfig { Role = "guard", Context = "night", Line = "Who goes there? Oh, it's you.", Weight = 1f },
                    new DialogLineConfig { Role = "guard", Context = "night", Line = "Can't sleep. The shadows move.", Weight = 0.8f },

                    // ===== RAIN =====
                    new DialogLineConfig { Role = "any", Context = "rain", Line = "This rain won't let up...", Weight = 1f },
                    new DialogLineConfig { Role = "any", Context = "rain", Line = "A bit of rain never hurt anyone.", Weight = 1f },
                    new DialogLineConfig { Role = "any", Context = "rain", Line = "I should've built a bigger roof.", Weight = 0.8f },
                    new DialogLineConfig { Role = "merchant", Context = "rain", Line = "Rain or shine, the shop's always open!", Weight = 1.5f },
                    new DialogLineConfig { Role = "villager", Context = "rain", Line = "Good weather for the crops, at least.", Weight = 1f },
                    new DialogLineConfig { Role = "guard", Context = "rain", Line = "Rain makes it hard to see. Stay alert.", Weight = 1f },

                    // ===== STORM =====
                    new DialogLineConfig { Role = "any", Context = "storm", Line = "Thor is angry tonight!", Weight = 1f },
                    new DialogLineConfig { Role = "any", Context = "storm", Line = "Best stay indoors in this weather.", Weight = 1f },
                    new DialogLineConfig { Role = "any", Context = "storm", Line = "The winds howl like wolves!", Weight = 0.8f },
                    new DialogLineConfig { Role = "guard", Context = "storm", Line = "Can barely see my own hand. Stay close!", Weight = 1f },

                    // ===== COMBAT =====
                    new DialogLineConfig { Role = "any", Context = "combat", Line = "Enemies! Watch out!", Weight = 1f },
                    new DialogLineConfig { Role = "any", Context = "combat", Line = "We're under attack!", Weight = 1f },
                    new DialogLineConfig { Role = "guard", Context = "combat", Line = "To arms! I'll hold them off!", Weight = 1.5f },
                    new DialogLineConfig { Role = "guard", Context = "combat", Line = "You'll not pass while I draw breath!", Weight = 1f },
                    new DialogLineConfig { Role = "merchant", Context = "combat", Line = "Not the shop! Someone protect the shop!", Weight = 1f },
                    new DialogLineConfig { Role = "villager", Context = "combat", Line = "Help! Someone help!", Weight = 1f },
                    new DialogLineConfig { Role = "villager", Context = "combat", Line = "I'm getting inside! Good luck!", Weight = 1f },
                    new DialogLineConfig { Role = "quest_giver", Context = "combat", Line = "Defend the village! I'll offer double rewards!", Weight = 1f },

                    // ===== IDLE =====
                    new DialogLineConfig { Role = "any", Context = "idle", Line = "What a day...", Weight = 1f },
                    new DialogLineConfig { Role = "any", Context = "idle", Line = "Odin watches over us.", Weight = 0.5f },
                    new DialogLineConfig { Role = "any", Context = "idle", Line = "I wonder what lies beyond the mist...", Weight = 0.5f },
                    new DialogLineConfig { Role = "any", Context = "idle", Line = "This is a fine settlement.", Weight = 1f },
                    new DialogLineConfig { Role = "merchant", Context = "idle", Line = "*counting coins*", Weight = 1f },
                    new DialogLineConfig { Role = "merchant", Context = "idle", Line = "I need to restock soon...", Weight = 0.8f },
                    new DialogLineConfig { Role = "merchant", Context = "idle", Line = "Business could be better, but can't complain.", Weight = 1f },
                    new DialogLineConfig { Role = "guard", Context = "idle", Line = "All quiet. Just how I like it.", Weight = 1f },
                    new DialogLineConfig { Role = "guard", Context = "idle", Line = "*sharpens weapon*", Weight = 0.8f },
                    new DialogLineConfig { Role = "guard", Context = "idle", Line = "I once fought a troll. True story.", Weight = 0.5f },
                    new DialogLineConfig { Role = "villager", Context = "idle", Line = "I should go fishing later.", Weight = 1f },
                    new DialogLineConfig { Role = "villager", Context = "idle", Line = "Have you tried the mead? It's excellent.", Weight = 1f },
                    new DialogLineConfig { Role = "villager", Context = "idle", Line = "Life in the village suits me fine.", Weight = 1f },
                    new DialogLineConfig { Role = "villager", Context = "idle", Line = "I heard strange sounds from the forest last night...", Weight = 0.6f },
                    new DialogLineConfig { Role = "quest_giver", Context = "idle", Line = "Always something that needs doing around here.", Weight = 1f },
                    new DialogLineConfig { Role = "quest_giver", Context = "idle", Line = "*studies a map*", Weight = 0.8f },
                    new DialogLineConfig { Role = "quest_giver", Context = "idle", Line = "The world is full of dangers... and rewards.", Weight = 0.7f }
                }
            };
        }
    }
}
