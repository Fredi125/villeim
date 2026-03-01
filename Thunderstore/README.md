# Village Life — Valheim NPC Mod

**Craft, customize, and place friendly NPCs to bring your Valheim settlements to life!**

Village Life lets you create merchants, quest givers, guards, and villagers that populate your bases with trade, quests, ambient dialog, and day/night routines. Unlike mods that auto-generate villages, you're in full control — every NPC is built at a crafting station, assigned a role, given a name, and placed exactly where you want.

---

## Features

### NPC Roles

| Role | Description |
|------|-------------|
| **Merchant** | Buy and sell items. 4 shop types: General Store, Weaponsmith, Armorer, Food Vendor. Restocks on a configurable timer. |
| **Quest Giver** | Offers quests with objectives like Kill, Gather, Deliver, Explore, and Build. 15 starter quests across 3 biomes. |
| **Guard** | Patrols a radius around their home point and engages hostile creatures that get too close. |
| **Villager** | Ambient NPC that wanders, sleeps at night, and chats about the weather. Brings life to your settlement. |

### Quest System
- **15 Starter Quests** across Meadows, Black Forest, and Swamp biomes
- **5 Objective Types**: Kill creatures, Gather items, Deliver items, Explore locations, Build structures
- **Quest HUD**: Track active quests with a compact overlay
- **Per-Player Progress**: Each player has independent quest state in multiplayer
- **Cooldown System**: Quests can be repeated after a configurable cooldown

### NPC Behaviors
- **Day/Night Schedule**: NPCs wander during the day and return home to sleep at night
- **Ambient Dialog**: 60+ context-sensitive lines based on role, time of day, weather, and nearby events
- **Event Reactions**: NPCs flee during raids and react to nearby combat
- **Customization**: Choose name, gender, hair style, and beard for each NPC

### Multiplayer
- Full multiplayer and dedicated server support
- Server-authoritative trade validation (prevents cheating/duping)
- ZDO-synced NPC state across all clients
- Per-player quest persistence via character save data

---

## Installation

1. Install [BepInEx](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/) (5.4+)
2. Install [Jötunn (Valheim Library)](https://valheim.thunderstore.io/package/ValheimModding/Jotunn/) (2.20+)
3. Place `VillageLife.dll` in your `BepInEx/plugins/` folder
4. Launch Valheim!

**All players and the server must have the mod installed.**

---

## Getting Started

1. **Build a Village Hall**: Open the build hammer, go to the "Village" tab, and place a Village Hall (costs 20 Wood, 10 Stone, 5 Resin)
2. **Create an NPC**: Interact with the Village Hall to open the NPC Workshop. Choose a name, role, appearance, and create!
3. **Place Your NPC**: The NPC spawns near the Village Hall. Move them by deconstructing and recreating.
4. **Interact**: Press E on any NPC to trade, accept quests, or just chat!

---

## Configuration

All configuration files are generated in `BepInEx/config/VillageLife/` on first launch.

### BepInEx Config (`com.villagelife.mod.cfg`)

| Setting | Default | Description |
|---------|---------|-------------|
| MaxNPCsPerPlayer | 20 | Maximum NPCs per player |
| NPCWanderRadius | 12 | Wander radius in meters |
| RestockMinutes | 60 | Merchant restock interval |
| EnableQuestSystem | true | Toggle quest system |
| EnableAmbientDialog | true | Toggle ambient dialog |
| Language | en | Language (en, fr) |

### JSON Configs (fully moddable)

- `shops.json` — Define custom shop inventories, prices, and stock levels
- `quests.json` — Create custom quests with any combination of objectives and rewards
- `dialog.json` — Add custom ambient dialog lines per role and context

---

## Compatibility

- Designed for Valheim with BepInEx 5.4+ and Jötunn 2.20+
- Uses namespaced ZDO keys (`villagelife:`) to avoid conflicts with other mods
- Compatible with most other Valheim mods

---

## Credits

Built with [Jötunn — the Valheim Library](https://valheim-modding.github.io/Jotunn/) and [BepInEx](https://github.com/BepInEx/BepInEx).
