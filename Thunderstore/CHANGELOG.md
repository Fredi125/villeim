# Changelog

## 3.19.1 — Lift inside-spawned villagers

### Fixed
- Villagers spawned inside a building were sinking into the floor (spawned at the structure's
  foundation pivot). They're now lifted ~1 m so they stand on the floor.

## 3.19.0 — More posts become buildings

### Changed
- Re-skinned three more biome posts onto real structures (all confirmed-working vanilla prefabs):
  **Black Forest → old wooden house**, **Mountain → ruined stone tower**, **Plains → ruined stone
  tower**. The merchant spawns inside each. The Swamp post stays a workbench until a fitting swamp
  structure is confirmed.

## 3.18.0 — Reputation is visible

### Added
- Bounty-givers now say **"Earns favor with the &lt;Trader&gt;"** in their hover text, so it's clear
  that completing a bounty raises that biome trader's reputation (and unlocks its rare good).

## 3.17.1 — Villagers can stand inside

### Added
- Stations can spawn their villager **inside the structure** (new per-station `SpawnInside` option),
  enabled for the Meadows house so the merchant stands in the house rather than out front. The
  summon/dismiss toggle is centred there too.

## 3.17.0 — The Meadows post is a house now

### Changed
- The **Meadows Trading Post** is now built as an **Old Wooden House V**, not a workbench — your
  first real "building, not a crafting table" station. Build the house, press [Use] on it to summon
  the Meadows merchant; the summon/dismiss toggle works exactly as before.
- Stations can now clone any vanilla building as their model (new `BasePrefab` option), via a shared
  `BuildablePrep` helper that the world-structures feature now uses too. Other posts still use the
  workbench for now — say the word and they become houses as well.

## 3.16.3 — World structures: icon fix

### Fixed
- World structures registered but were rejected with **"has no icon"** (build pieces need one). Each
  now gets a stand-in icon borrowed from a vanilla piece, so they pass validation and appear in the
  **VillageLife** tab. All vanilla-sourced — no external art. (Real per-building icons via Jötunn's
  RenderManager are a planned polish once placement is confirmed.)

## 3.16.2 — Build hardening

### Fixed
- Use the positional `CustomPiece(GameObject, bool, PieceConfig)` form the Jötunn DLL documents,
  removing a named-argument dependency. (Folds into the 3.16.1 world-structures fix.)

## 3.16.1 — World structures actually build now

### Fixed
- World structures registered but were **rejected as invalid** ("no Piece component") and never
  appeared. The log confirmed the `WoodHouse#` / `StoneTowerRuins##` prefabs resolve but ship with
  no `Piece`. Fixed by cloning the prefab ourselves and adding a `Piece` + persistent `ZNetView`
  **before** wrapping it as a CustomPiece (using Jötunn's `CustomPiece(GameObject, …)` form), and
  stripping spawner/dungeon/AI components so the placed structure is inert scenery. They now land in
  the **VillageLife** Hammer tab.
- Removed the early prefab-discovery scan that always no-op'd (ZNetScene isn't ready at that point).
- Self-heal: a `vendors.json` that loads with no vendors is now **rewritten** from defaults instead
  of just warning every launch.

## 3.16.0 — One build tab + structure discovery

### Changed
- **All VillageLife buildables now live in a single Hammer tab, "VillageLife"** — the Village Hall,
  biome trading posts, Guard Post, Bounty Board, and any world structures — instead of being mixed
  into vanilla *Misc*. (Uses Jötunn's custom-category support, the reliable path.)

### Fixed / diagnostics
- The first batch of world structures likely didn't appear because those prefab names don't resolve
  as standalone cloneable prefabs. Registration now **pre-checks the prefab cache** and logs a clear
  skip, and a new **discovery line** lists every building-like prefab actually present in ZNetScene
  (`Building-prefab candidates in ZNetScene (N): ...`). That log is what we'll use to pick names that
  really work.

## 3.15.0 — Buildable world structures (experimental)

### Added
- **Place real buildings**, not just crafting-table clones: a first set of vanilla world structures
  (abandoned wooden houses, ruined stone towers) is registered as **Hammer → Misc** pieces. Spawner,
  dungeon, and AI components are stripped so a placed structure is inert scenery.

### Notes
- **Experimental & self-diagnosing.** Exact prefab names / clone-ability can't be verified offline,
  so each registration is guarded: a name that doesn't resolve is skipped and logged. Check the
  BepInEx log for `World structures: N buildable, M skipped` and the per-name warnings — that tells
  us exactly which to keep, prune, or add. Large structures may not preview/snap perfectly yet.

## 3.14.0 — Villager creation panel

### Added
- The **Village Hall** now opens a **creation panel** instead of summoning a random villager: pick a
  name (re-roll button) and click the **role** you want (General Store, Forager, Huntsman, Blacksmith,
  Tavern Keeper, Farmer, Stonemason, or Guard), then it appears out front. Biome traders, bounties,
  and the bounty board still have their own dedicated stations.
- Safe fallback: if the panel can't be shown for any reason, the hall reverts to its previous instant
  rotation-summon, so it always does *something*.

## 3.13.0 — Guards

### Added
- A **Guard Post** station (Hammer → Misc) summons a **Village Guard** who wards off nearby monsters —
  a stationary, friendly Haldor clone (no crash-prone AI of its own) that periodically damages hostile
  creatures in range. Built with Wood, Stone, and Bronze. Same summon/dismiss toggle as other posts.
- Guards never harm players or tamed creatures; only the object's owner deals damage (so a monster
  isn't hit once per player), and the damage tick is fully guarded — any unexpected combat-API result
  disables the tick and logs once instead of throwing.

## 3.12.0 — Visual variety

### Added
- Villagers now spawn at a **slightly different size** (a gentle ±10%), stored per-villager and
  synced/saved, so a crowd looks like individuals rather than identical Haldor clones. Cosmetic and
  failure-safe — no value just means default size.

## 3.11.0 — More village roles

### Added
- Three new coin-shop villagers, each with a themed stock: **Blacksmith** (coal, nails, bronze),
  **Tavern Keeper** (food & honey), and **Farmer** (crops & seeds). They join the Village Hall's
  summon rotation and are fully editable in `vendors.json`.

## 3.10.0 — Villager greetings

### Added
- Summoned villagers now greet you with a **flavour line** themed to their trade (shop talk for
  merchants, hunt talk for bounty-givers) — a first dash of personality. Purely cosmetic.

## 3.9.0 — Upgradeable traders (reputation)

### Added
- **Traders now level up as you complete their quests.** Each biome trader has a **reputation**
  level; completing that biome's **bounty** raises it, and higher-tier goods unlock in the trader's
  shop. The five rare items (Queen Bee, Surtling Core, Chain, Dragon Egg, Lox Pelt) now start
  **locked** and become buyable once you've earned the trader's trust (Rep 1).
- Reputation is **world-global** (stored like boss-defeat flags), so it persists across saves and is
  shared in co-op. The shop title shows the current standing, e.g. `Sigrid (Mountain Trader) · Rep 1/1`.
- A trader already standing in the world **restocks immediately** when its reputation changes.

### Notes
- This is the first reputation tier (one unlock per trader). The system is data-driven — goods carry
  a `Tier` and bounties an `UnlocksVendorId` in `vendors.json` — so more tiers/quests are easy to add.
- `vendors.json` auto-updates to enable this (old file kept as `.bak`).

## 3.8.2 — Tier-wide price rebalance

### Changed
- **Every sold resource is now priced by biome tier**, so the economy is coherent instead of having
  cheap commons sitting next to expensive metals. Per-unit prices climb with progression, with each
  tier's refined metal as the ceiling:
  - Meadows / general: basics 0.2/unit (wood, stone), commons 0.5, herbs ~1
  - Black Forest: fine/core wood 0.2, coal 0.5, copper & tin 5
  - Swamp: guck/entrails/bloodbag 2, withered bone 3, iron 10
  - Mountain: obsidian/onion 3, freeze gland/crystal 4, wolf pelt 5, silver 16
  - Plains: barley/flax/cloudberry/needle/tar 4, black metal 20
- Metals, wood, and the novelty "rare" items keep their prior prices. Existing `vendors.json`
  auto-updates to the new curve (old file kept as `.bak`).

## 3.8.1 — Wood pricing

### Changed
- Black Forest trader: **Fine Wood** and **Core Wood (round logs)** now cost **6 per stack of 30**
  (up from 2). Existing `vendors.json` auto-updates to the new price.

## 3.8.0 — Bounty Board

### Added
- **Bounty Board** — a new buildable station (Hammer → Misc) that posts **all five bounty-givers at
  once** in a row out front, so the turn-in bounties are reachable directly instead of cycling the
  Village Hall. Its [Use] is the same summon/dismiss toggle: press once to staff the board, press
  again to clear the whole row. Built cheap and early (Wood, Fine Wood, Coal) since bounties are how
  you earn coins for the pricier posts.

### Changed
- Stations can now post a list of specific vendors at once (the mechanism behind the board), in
  addition to the single-vendor posts (biome traders) and the rotating Village Hall.

## 3.7.0 — Turn-in bounties

### Added
- **Bounty villagers** that buy monster trophies for Coins — the main early way to *earn* the coins
  the (pricey) minerals demand. Five span the biome progression, scaling with how dangerous the
  trophy is to collect:
  - Meadows Bounty — 2× Boar trophy → 10 coins
  - Forest Bounty — 3× Greydwarf trophy → 24 coins
  - Swamp Bounty — 2× Draugr trophy → 45 coins
  - Mountain Bounty — 2× Wolf trophy → 70 coins
  - Plains Bounty — 2× Fuling trophy → 90 coins
- These are barter villagers (one fixed turn-in each), summoned via the Village Hall's rotation.
  They're fully data-driven in `vendors.json`, so amounts and trophies are easy to tune.

## 3.6.0 — Removable merchants + economy pass

### Changed
- **Stations no longer breed endless merchants.** A station's [Use] is now a **summon / dismiss
  toggle**: press it with no merchant in front and one is summoned; press it again and the merchant
  (plus any leftover pile a previous build stacked there) is removed. This is also the cleanup path
  for worlds that already have a crowd of duplicate villagers — walk up to the post and press [Use].
- **Minerals are now a real money sink** (per the design goal — basics are cheap, but building via
  trade demands accumulated wealth). Prices are per unit: **Tin & Copper 5**, **Iron 10**,
  **Silver 16**, **Black Metal 20** coins. Silver (Mountain) and Black Metal (Plains) are now sold —
  previously they were only build-cost materials.

### Added
- **vendors.json auto-updates with the defaults.** The config now carries a version; when the
  built-in defaults change, an out-of-date file is regenerated from them (the previous file is kept
  as `vendors.json.bak`). Value tweaks now reach an existing install without deleting the file by
  hand. Edits made at the current version are still merged/preserved as before.

## 3.5.2 — Item-name correctness + startup audit

### Fixed
- The Forager sold **`Raspberries`**, which is not the prefab name — the item is **`Raspberry`**
  (singular). Valheim is inconsistent here: `Blueberries` is plural, `Cloudberry` is singular; both
  of those were already correct. New installs get the fix automatically; existing `vendors.json`
  files keep the old entry (player edits are never overwritten) — the new audit below will flag it.

### Added
- **Startup item-name audit.** On load, the mod now checks every item prefab it references — vendor
  goods, barter cost/give items, and station build requirements — against ObjectDB and logs a single
  consolidated warning naming any that don't resolve (and where each is used). Previously these
  failed silently: an unknown good was skipped and an unknown build requirement was dropped, with no
  hint as to why. The audit runs at the same moment Jötunn resolves requirements, so it reports
  exactly what the game will see.

## 3.5.1 — Biome-themed build costs

### Changed
- Each biome trading post now costs **a spread of ~6 materials from that biome** instead of two
  generic items — e.g. Black Forest needs fine wood, round logs, coal, copper, tin and greydwarf
  eyes; Mountain needs stone, obsidian, silver, wolf pelt, freeze glands and crystal. Makes each
  post a meaningful biome-progression goal.
- If a build-cost item name can't be resolved, Jötunn skips just that requirement (logged), so a
  typo makes a post cheaper rather than unbuildable.

## 3.5.0 — Biome trading posts

### Added
- **Five biome trading-post buildables** (Meadows, Black Forest, Swamp, Mountain, Plains), each in
  the Hammer's *Misc* tab and built from that biome's materials. Each summons its own merchant that
  sells **basic biome goods plus one rare item at a high coin price** (e.g. Mountain → Dragon Egg).
- The original **Village Hall** remains and now cycles through *all* vendor types (a sampler).
- All biome vendors are normal `vendors.json` entries (tagged with `biome`), so you can retune
  their goods, prices, and the rare item without rebuilding.

### Changed
- Stations share one generalized piece + interaction (`VillageStations` / `StationInteraction`):
  a station either summons a specific vendor (biome posts) or the rotation (Village Hall).
- **Config upgrade is non-destructive:** on load, any built-in vendor whose id is missing from your
  `vendors.json` (such as the new biome vendors) is appended and the file rewritten, preserving all
  your existing edits.

### Notes
- Biome sell-lists use vanilla item prefab names; any that don't resolve are skipped and logged, so
  a wrong name just means that item is absent — correct it in `vendors.json`.

## 3.4.0 — Vendors are config-driven (JSON)

### Added
- The whole vendor catalogue now lives in **`BepInEx/config/VillageLife/vendors.json`** — edit
  types, goods, prices, and barter rates without rebuilding the mod. Written with defaults on first
  run.
- Each vendor carries an optional **`biome`** tag and `kind` (`"coin"` / `"barter"`), so the planned
  per-biome vendors (basic biome goods + one high-priced rare item) are just config entries.

### Reliability
- If `vendors.json` is missing, empty, or malformed, the mod logs a warning and **falls back to the
  built-in defaults** — a bad edit can never leave you with no vendors or a failed load.
- Uses Unity's built-in `JsonUtility` (no new dependency).

## 3.3.0 — Barter vendors

### Added
- **Barter villagers** that give a single product for a fixed resource — no coins, no shop window.
  The starter one is the **Stonemason** (40 Stone → 30 Wood). Press *Use* to trade; the hover text
  shows the exchange.
- The exchange is safe by construction: it counts the required input first and only removes it if
  you have enough **and** there's a free inventory slot for the reward — a declined or impossible
  trade never takes anything.

### Changed
- Barter and coin vendors are now **separate prefabs** (`VL_Barterer` has Haldor's Trader removed
  and uses our own interaction; `VL_Merchant` keeps it for the vanilla shop). This guarantees one
  interactable per villager — no ambiguity over what the *Use* key does.
- `VendorType` gained barter fields (`CostPrefab`/`CostAmount` → `GivePrefab`/`GiveAmount`) and a
  `VendorKind` that routes spawning to the right prefab.

## 3.2.0 — Villager types

### Added
- **Multiple merchant types**, each with its own goods: **General Store**, **Forager**, and
  **Huntsman**. The Village Hall cycles through them, so each summon is a different shop.
- The summoned merchant's type is shown on summon and in the shop title, and is **persisted** in
  the ZDO (survives save/reload, syncs in multiplayer).
- `VendorCatalog` is the single place to add a new villager type — append an entry and it joins
  the rotation automatically.

### Foundation for barter
- Vendor types carry a `VendorKind` (`CoinShop` today; `Barter` planned). Barter vendors will give
  a single product for a fixed resource (e.g. 40 Stone → 30 Wood) via a small no-UI interaction,
  keeping the reliable coin shops on Valheim's vanilla trade window.

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
