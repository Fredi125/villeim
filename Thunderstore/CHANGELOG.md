# Changelog

## 3.1.0 — Merchants

### Added
- **Summoned villagers are now working merchants.** Press *Use* on a merchant to open Valheim's
  real trade window and buy goods for coins.
- A starter stock of common resources (wood, stone, coal, flint, leather scraps, resin, feathers,
  thistle). `MerchantStock` is the single, obvious place to extend or later config-drive the shop.

### Changed
- The merchant reuses Haldor's **vanilla Trader** for hover, interaction and the shop UI instead
  of a custom panel — so there's exactly one thing handling the *Use* key (no dual-interactable
  ambiguity) and no fragile IMGUI trade window. This replaces the plain greeter from 3.0.x.
- Stock is applied defensively: if our item list can't be built (e.g. ObjectDB not ready), the
  merchant keeps its existing stock, so the store is never empty.

## 3.0.3 — Solidify the villager

### Fixed
- **Villagers no longer move when talked to.** The interaction used to rotate the villager to
  face the player; because Haldor's model is offset from its pivot (and direct transform edits
  fight the networked transform sync), that made them appear to teleport. Talking now leaves the
  transform completely untouched — villagers greet you in place.

### Changed
- **More greetings, no immediate repeats.** Expanded to 10 lines and the same line never plays
  twice in a row, so the variety is actually visible.

### Dev
- Logs Haldor's component list once at startup (diagnostic groundwork for future restyling).
- Optional `R2_PROFILE` build variable: when set, the build auto-copies the DLL straight into
  your r2modman profile (no more manual copy). Inert if unset. See README.

## 3.0.2 — Register the villager as a plain prefab

### Fixed
- **Summoning works.** The game log proved Haldor has no `Character`/`BaseAI`/`Rigidbody`/
  `ZSyncAnimation`/`CharacterAnimEvent` — he's a stationary, non-killable interactable NPC, not a
  spawn-system creature — so `CreatureManager.AddCreature` rejected the clone as "not valid".
  The villager is now cloned via `PrefabManager` and registered with `PrefabManager.AddPrefab`,
  which injects it into ZNetScene on every world load (so `GetPrefab("VL_Villager")` resolves and
  it persists). A villager with no `Character` component also can't enter the global character
  list, so it can't trigger the per-frame `EnemyHud` crash spam seen with the old NPCs.

## 3.0.1 — Fix villager summoning

### Fixed
- **Villagers can now be summoned.** Registration was using Jötunn's
  `CustomCreature(name, "Haldor", …)` string constructor, which resolves the base through
  `CreatureManager.GetCreaturePrefab("Haldor")` — that returns null because Haldor is a
  location-placed trader, not a spawn-system creature. The result was a "Failed to clone
  'Haldor'" error at startup and a "Could not summon a villager" message in game.
- Cloned the villager through `PrefabManager` instead of the `CustomCreature` string
  constructor. (Registration was still routed through `CreatureManager` at this point; 3.0.2
  finished the fix by switching to `PrefabManager.AddPrefab`.)
- Kept the Haldor clone faithful to vanilla — only the Trader component is removed (so our
  E-interaction is unambiguous). Avoids the over-stripping that produced malformed NPCs
  before.

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
