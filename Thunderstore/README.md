# Village Life — Valheim NPC Mod

**Build a Village Hall and summon friendly, named villagers to your settlement.**

Village Life is being rebuilt as a small, reliable foundation that we grow one tested
feature at a time. This release does one thing well: it lets you place a Village Hall and
populate your base with friendly villagers you can greet.

---

## Features (v3.0)

- **Village Hall** — a buildable structure available in the Hammer's *Misc* tab
  (costs 20 Wood, 10 Stone).
- **Summon villagers** — press *Use* on the Village Hall to summon a friendly villager
  with a random Norse name, who appears just in front of the hall.
- **Friendly & persistent** — villagers are non-hostile, stand at their post, and are
  saved with your world.
- **Talk to them** — hover to see a villager's name; press *Use* for a greeting.
- **Multiplayer-ready** — villagers are standard networked creatures, so they sync to
  every client and persist on dedicated servers.

> **Heads-up:** villagers are passive for now — they don't wander, trade, fight, or give
> quests. Those return as separate, tested updates on top of this foundation.

---

## Installation

1. Install [BepInEx](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/) (5.4+)
2. Install [Jötunn (Valheim Library)](https://valheim.thunderstore.io/package/ValheimModding/Jotunn/) (2.20+)
3. Place `VillageLife.dll` in your `BepInEx/plugins/` folder
4. Launch Valheim!

**All players and the server must have the mod installed.**

---

## Getting Started

1. Open the build hammer, go to the **Misc** tab, and place a **Village Hall**.
2. Walk up to it and press **Use** (default **E**) to summon a villager.
3. Hover over the villager to see their name; press **Use** to say hello.

---

## Configuration

`BepInEx/config/com.villagelife.mod.cfg` is generated on first launch.

| Setting | Default | Description |
|---------|---------|-------------|
| SpawnDistance | 2.5 | How far in front of the Village Hall (metres) a villager spawns |

---

## Roadmap

This 3.0 rebuild is the reliable base. Planned increments, each added only once it works:

1. A small creation UI (choose a villager's name and look).
2. A merchant role with simple buy/sell.
3. Quests, then ambient behaviour.

---

## Credits

Built with [Jötunn — the Valheim Library](https://valheim-modding.github.io/Jotunn/) and
[BepInEx](https://github.com/BepInEx/BepInEx).
