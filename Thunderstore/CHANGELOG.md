# Changelog

## 2.0.0

A ground-up rebuild of the prefab and behaviour layer for reliability.

### Changed
- **NPCs are now cloned from Haldor** (the vanilla trader) instead of the Player prefab — a friendly, animated, non-combat humanoid base. This eliminates the prefab-creation crashes from 1.x.
- **Prefab creation uses Jötunn's lifecycle-safe cloning** (`CreateClonedPrefab` / `CustomPiece` clone constructor) instead of hand-rolled `Instantiate` + component stripping. Fixes the invisible-building and startup crash bugs.
- **NPCs are stationary.** Each villager holds its post (a merchant at the stall, a guard at the gate). The 1.x movement system manipulated the transform directly, which never animated or synced over the network.

### Fixed
- Village Hall now places and renders correctly.
- NPC and Village Hall prefabs register exactly once (no "already exists" errors).
- Config files (shops/quests/dialog) generate with full content instead of empty data.

## 1.0.0

### Added
- **NPC System**: Craft and place friendly NPCs using the Village Hall crafting station
- **4 NPC Roles**: Merchant, Quest Giver, Guard, and Villager
- **Merchant Trading**: Config-driven shop inventories with buy/sell UI, restocking timers, and 4 shop types (General Store, Weaponsmith, Armorer, Food Vendor)
- **Quest System**: 15 starter quests across Meadows, Black Forest, and Swamp biomes
- **Quest Types**: Kill, Gather, Deliver, Explore, and Build objectives
- **Quest UI**: Quest offer panel, progress tracking, completion dialog, and HUD overlay
- **NPC Behaviors**: Day/night schedule with wander, sleep, and flee states
- **Ambient Dialog**: Context-sensitive spoken lines based on time, weather, and events (60+ lines)
- **Guard AI**: Guards patrol and defend against hostile creatures
- **NPC Customization**: Name, gender, hair style, and beard options
- **Multiplayer Support**: Server-authoritative trade validation, per-player quest state, ZDO sync
- **Localization**: Full English and French translations
- **Config System**: JSON-driven shop inventories, quest definitions, and dialog lines — fully moddable
- **Dedicated Server Support**: Headless-compatible with server-side validation
