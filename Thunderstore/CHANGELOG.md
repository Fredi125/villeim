# Changelog

## 3.0.0 — Reliable foundation (rebuild)

A deliberate, from-scratch rebuild. Earlier versions tried to ship a large feature set
(quests, merchants, guards, ambient dialog, localization, custom multiplayer RPC) on an
unstable base and never reached a dependable state. 3.0 strips the mod back to a small core
that works, so the rest can be added one tested feature at a time.

### What it does now
- Buildable **Village Hall** (Hammer → *Misc* tab).
- Press *Use* on the hall to **summon a friendly, named villager** in front of it.
- Villagers are non-hostile, stationary, persistent, and greet you when talked to.
- Standard networked creatures — multiplayer and dedicated-server friendly.

### How it's built (for reliability)
- Villagers are registered through Jötunn's **CreatureManager** as a clone of **Haldor**
  (a friendly, idle, AI-less humanoid), replacing the hand-rolled prefab creation that
  caused the invisible/crashing NPCs in earlier builds.
- The Village Hall is a Jötunn **CustomPiece** cloned from the workbench, placed in the
  built-in *Misc* tab (custom build tabs were a recurring source of bugs).
- No Harmony patches, no custom RPC layer, no per-frame managers — far less to go wrong.
- Spawning goes through a single `NpcSpawner` entry point so a creation UI can be added
  later without changing the spawn/persistence logic.

### Removed (returning later as tested increments)
- Quest system, merchant trading, guard AI, day/night routines, ambient dialog packs,
  localization framework, and the custom multiplayer RPC layer.

---

## 2.0.0
Rebuild of the prefab/behaviour layer (NPCs cloned from Haldor). Improved over 1.x but
still carried the full, unreliable feature set; superseded by the 3.0 foundation.

## 1.0.0
Initial release: NPC roles (merchant, quest giver, guard, villager), a 15-quest system,
ambient dialog, guard AI, localization, and multiplayer RPC. Ambitious but unreliable.
