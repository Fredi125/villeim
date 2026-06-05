# Village Life — Valheim NPC Mod

**Build biome-themed stations and summon friendly, named villagers — merchants, barterers,
bounty-givers and guards — to populate, supply and defend your settlement.**

Village Life is a small, reliable mod grown one tested feature at a time. Everything is built by
cloning vanilla prefabs — no Harmony patches, no per-frame managers — so it stays dependable as it
grows.

---

## Features

- **One build tab.** Every piece lives in a dedicated **"VillageLife"** tab in the build Hammer.
- **Village Hall** — press *Use* to open a panel and summon a villager (a sampler of the general
  roles).
- **Biome spawners** — Meadows, Black Forest, Swamp, Mountain and Plains stations, each a distinct
  small crafting station. *Use* opens a menu to **choose a villager** for that biome (a trader, two
  themed shops, a bounty-giver and barterers — the later biomes add quest-givers and more), plus a
  **Dismiss nearby villager** button. A villager you've already summoned here drops out of the menu
  until dismissed, so you fill out one of each without duplicates.
- **Recognisable locals** — many later-biome villagers wear monster looks (the full Dvergr line,
  Goblins, a Wraith, a Troll, a Draugr archer and a turncoat Fenring Cultist), each cloned from the
  creature and stripped of its AI so it stands still and friendly — with a signature colour where it
  helps it read as an NPC.
- **Coin merchants** — open **Valheim's own trade window** and sell that biome's goods for coins.
- **Barterers** — make one fixed swap, like 5 Deer Hide → 10 Leather Scraps.
- **Bounties & reputation** — turn monster trophies in for coins to raise your **reputation** with a
  biome's trader, which **unlocks its higher-tier goods** (premium materials and rare items).
- **Per-tier pricing** — goods cost roughly **1 gold/unit in the Meadows**, climbing **+1 per tier**
  up to the Plains, so prices stay sensible as you progress.
- **Bounty Board, Quest Board & Guard Post** — post every bounty-giver at once, take on multi-item
  quests for rewards, or station a guard who wards off nearby monsters.
- **Named, persistent, multiplayer-ready** — each villager keeps its name, type, size and reputation
  across saves, and syncs to every client and dedicated server.

---

## Installation

1. Install [BepInEx](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/) (5.4+)
2. Install [Jötunn (Valheim Library)](https://valheim.thunderstore.io/package/ValheimModding/Jotunn/) (2.27+)
3. Place `VillageLife.dll` in your `BepInEx/plugins/` folder
4. Launch Valheim!

**All players and the server must have the mod installed.**

---

## Getting Started

1. Open the build Hammer and go to the **VillageLife** tab.
2. Place a **Village Hall**, or a **biome spawner** (its build cost is a spread of that biome's
   materials).
3. Press **Use** (default **E**) on a spawner to open its menu and choose a villager to summon.
4. Buy from merchants in the vanilla trade window, trade with barterers, and turn trophies in at the
   bounty-givers to build reputation and unlock better goods.

---

## Configuration

`BepInEx/config/com.villagelife.mod.cfg` is generated on first launch.

| Setting | Default | Description |
|---------|---------|-------------|
| SpawnDistance | 0 | How far in front of the player (metres) a summoned villager appears (0 = at your feet) |

Villagers themselves are defined in `BepInEx/config/VillageLife/vendors.json`, written with defaults
on first run. Edit it to tweak types, goods, prices and barter rates with no rebuild — the file
regenerates automatically (keeping a `.bak`) when a new version changes the built-in defaults.

---

## Roadmap

The trading-village core is complete, villagers chatter ambiently, and **deeper quests** have a first
proof of concept (a Quest Board with multi-item turn-ins) — with larger objectives to follow.
(Localization was considered and deliberately dropped.)

---

## Credits

Built with [Jötunn — the Valheim Library](https://valheim-modding.github.io/Jotunn/) and
[BepInEx](https://github.com/BepInEx/BepInEx).
