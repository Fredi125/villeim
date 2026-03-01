# VillageLife

Add living NPCs to your Valheim settlements! Create merchants, quest givers, guards, and villagers through the Village Hall crafting station.

## Features

- **4 NPC Roles**: Merchant, Quest Giver, Guard, and Villager
- **Trading System**: Config-driven shops with 4 shop types (General Store, Weaponsmith, Armorer, Food Vendor)
- **Quest System**: 15 quests across Meadows, Black Forest, and Swamp biomes
- **Guard AI**: Guards patrol and defend your village from hostile creatures
- **NPC Behaviors**: Day/night schedules, wandering, fleeing from raids, ambient dialog
- **Multiplayer Support**: Synced via ZDO and Jotunn RPCs
- **Fully Configurable**: JSON config files for shops, quests, and dialog lines
- **Localization**: English and French built-in, with custom translation file support

## How to Use

1. Build a **Village Hall** using the Hammer (costs 10 Wood, 5 Stone)
2. Interact with the Village Hall to open the **NPC Workshop**
3. Choose a name, role, gender, and appearance for your NPC
4. The NPC spawns near you and begins its role behavior

## NPC Roles

| Role | Description |
|------|-------------|
| **Merchant** | Buy and sell items. Restocks periodically. |
| **Quest Giver** | Offers quests based on the biome pool you select. |
| **Guard** | Patrols and attacks hostile creatures near your base. |
| **Villager** | Wanders around, provides ambient dialog. |

## Configuration

Config files are generated in `BepInEx/config/VillageLife/` on first run:
- `shops.json` — Merchant inventories and prices
- `quests.json` — Quest definitions, objectives, and rewards
- `dialog.json` — NPC dialog lines by role and context

## Requirements

- Valheim
- BepInEx 5.4+
- Jotunn (JVL) 2.20+
