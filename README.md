# VillageLife

A Valheim mod that lets you build a small **trading village**: place biome-themed stations and
summon friendly, named **villagers** — coin merchants, barterers, bounty-givers and guards — to
populate and defend your settlement.

The mod is a deliberately small, reliable core grown one tested feature at a time. Earlier versions
tried to ship quests, merchants, guards, ambient dialog, localization and a custom multiplayer layer
all at once and never reached a dependable state. Everything here is built by **cloning vanilla
prefabs** (no hand-built prefabs, no Harmony patches, no per-frame managers).

## What it does today

All buildables live in a single **"VillageLife" tab** in the build Hammer (Jötunn creates the tab),
so they're grouped together instead of scattered through *Misc*.

**Stations you can build**

- **Village Hall** — press *Use* to open the creation panel and summon a villager, sampling across
  all the general roles.
- **Biome spawners** (Meadows, Black Forest, Swamp, Mountain, Plains) — each is a distinct small
  crafting station (cauldron, artisan table, stonecutter, forge, spinning wheel). *Use* opens a menu
  to **choose one of that biome's villagers**: the biome trader, two themed coin shops, the biome
  bounty-giver, and barterers (the Swamp, Mountain and Plains also add quest-givers and extra
  creature-model locals). The same panel has a **Dismiss nearby villager** button — and a villager
  you've already summoned here is **hidden from the menu until you dismiss it**, so you fill out a
  one-of-each village without stacking duplicates.
- **Bounty Board** — posts (or dismisses) every bounty-giver in a row at once (the five biome
  trophy bounties plus a Resin bounty).
- **Quest Board** — posts multi-item **quest-givers**: hand in several different items at once for a
  reward. Repeatable quests **rotate** through a pool of recipes and **scale up** each turn-in (the
  reward grows faster than the cost), tracked per world like reputation.
- **Guard Post** — posts (or dismisses) a guard that wards off nearby monsters.
- **Model Sampler** — a preview tool: [Use] spawns one of every humanoid NPC model in a row (AI
  stripped, non-persistent), each named after its prefab, so you can scout which models to use for
  villager looks. [Use] again clears them.
- **Decorative world structures** — a curated set of vanilla buildings (abandoned houses, ruins)
  registered as plain buildable scenery.

**Villager kinds**

- **Coin merchants** open **Valheim's real trade window** — buy biome goods for coins using the
  game's own shop UI (no custom panel).
- **Barterers** make one fixed swap (e.g. 5 Deer Hide → 10 Leather Scraps), via a small interaction.
- **Bounty-givers** are barterers that buy monster trophies for coins *and* raise your **reputation**
  with that biome's trader.
- **Guards** periodically damage nearby hostile creatures (owner-only, never players or tamed).

**Economy & progression**

- **Per-tier pricing** — a good's coin cost scales with its biome tier: ~**1 gold/unit** in the
  Meadows, +1 per tier up to ~**5 gold/unit** in the Plains.
- **Reputation** — completing a biome's bounty raises a world-global reputation level for that
  trader, which **unlocks higher-tier goods** (for example, the Black Forest's fine wood and each
  biome's premium "rare" item). Reputation is shown in the shop title and the bounty hover text.
- **Named & persistent** — every villager's name, type, size, colour and reputation are persisted in
  the ZDO, so they survive save/reload and sync in co-op. Villagers are non-hostile, stationary
  networked NPCs.
- **Varied looks** — villagers use the **Hildir** model by default (Haldor is the fallback), each with
  a gentle random size and colour. A vendor type can request its **own model** (the later biomes field
  the full Dvergr line, Goblins, a Wraith, a Troll, a Draugr archer and a turncoat Fenring Cultist) and
  an optional **signature tint**; the global model is configurable. A creature model's wander/combat AI
  is stripped on clone so it stays put and friendly.

Villagers are defined in `BepInEx/config/VillageLife/vendors.json` (written with defaults on first
run). Edit it to change types, goods, prices, or barter rates — no rebuild needed. The file carries a
version; when the built-in defaults change, an out-of-date file is **regenerated** automatically
(keeping a `.bak`).

## Project layout

```
VillageLife/
  Plugin/VillageLifePlugin.cs   BepInEx entry point; binds config; registers content on the PrefabManager event
  Util/Constants.cs             Prefab names, the "VillageLife" build-tab name, hashed ZDO keys, version
  NPC/
    NpcPrefab.cs                Registers villager prefabs (Hildir/Haldor/etc. clones) per model: merchant, barterer, guard
    NpcSpawner.cs               Single spawn seam; routes coin/barter/guard by vendor kind; RemoveNear() dismisses
    VendorCatalog.cs            Vendor data model + built-in defaults + ConfigVersion; the in-memory catalogue
    VendorConfigLoader.cs       Loads/writes vendors.json; regenerates on a version change; falls back to defaults
    VillageMerchant.cs          Coin-shop companion: persists name+type, sets the vanilla shop stock by reputation
    VillageBarterer.cs          Barter villager: sole interactable; fixed resource→product swap; bounties grant rep
    VillageGuard.cs             Guard villager: periodic owner-only tick that harms nearby monsters
    MerchantStock.cs            Resolves a coin vendor's goods into trade items (reputation-filtered) via ObjectDB
    TraderReputation.cs         World-global reputation levels (global keys) that gate higher-tier goods
    VillagerCreationUI.cs       The spawn panel: choose a villager, reroll the name, dismiss a nearby one
    VillagerAppearance.cs       Cosmetic per-villager size + colour-tint variation, persisted in the ZDO
    Greetings.cs                Flavour one-liners shown when a villager is summoned
    ItemNames.cs                Resolves prefab vs. shared item names (safe inventory moves)
    ItemNameAudit.cs            Startup audit: logs any referenced item/requirement name that won't resolve
  Building/
    VillageHall.cs              Buildable stations: Village Hall, 5 biome spawners, Guard Post, Bounty Board, Quest Board
    BuildablePrep.cs            Turns a cloned vanilla prefab into a safe placeable piece (strips crash-prone parts)
    WorldStructures.cs          Registers decorative vanilla world buildings (abandoned houses, ruins) as pieces
  lib/                          BepInEx / Jötunn / Harmony reference DLLs (committed)
```

### Design principles (why it should stay reliable)

- **Clone vanilla, don't hand-build.** Villagers are NPC clones (Hildir by default, Haldor as a
  fallback) registered through Jötunn's `PrefabManager`; stations are Jötunn `CustomPiece`s cloned
  from vanilla build pieces. No manual prefab construction. Coin merchants keep the model's own
  `Trader`, so the shop UI is vanilla; a creature model's AI is stripped on clone so it stays put.
- **One build tab.** Every piece is placed in a single Jötunn-created **"VillageLife"** Hammer tab so
  the content groups together. (An earlier rebuild used the vanilla *Misc* tab as a precaution; the
  custom tab now works reliably through Jötunn.)
- **No incidental complexity.** No Harmony patches and no custom RPC. The only timed code is the
  guard's low-frequency tick and a one-shot startup scan — both owner/once-guarded and wrapped so a
  surprise disables that behaviour and logs once, never a per-frame crash.
- **One spawn seam.** Everything that creates a villager goes through `NpcSpawner.Spawn`, so the
  creation UI and every station just fill in an `NpcRequest`.
- **Fail safe.** Unresolved item names are skipped and surfaced by the startup audit; a bad
  `vendors.json` falls back to the built-in defaults; cloned structures are stripped of crash-prone
  components before they're ever placed.

## Building

The build needs a local Valheim install for the game assemblies (they are **not** committed). The
BepInEx, Jötunn and Harmony reference DLLs *are* committed under `VillageLife/lib/`, so you don't
need to pre-install any mods to compile.

### Find your Valheim folder

It's the folder that contains `valheim.exe` and a `valheim_Data\Managed\` subfolder. In Steam:
right-click **Valheim → Manage → Browse local files**. Common paths:

- `C:\Program Files (x86)\Steam\steamapps\common\Valheim`
- `D:\SteamLibrary\steamapps\common\Valheim`

The project auto-detects these (and a few more). You only need the next step if your install is
somewhere else.

### Point the build at it (only if auto-detect fails)

Either set an environment variable (recommended — survives across rebuilds):

```powershell
# PowerShell, persists for your user; reopen the terminal/VS afterward
setx VALHEIM_INSTALL "D:\YourPath\steamapps\common\Valheim"
```

…or edit the `ValheimDir` line in `VillageLife/VillageLife.csproj`.

If the assemblies still can't be found, the build stops with a clear message telling you which path
it checked — that's expected, just set `VALHEIM_INSTALL` to the right folder.

### Build

**Visual Studio:** open `VillageLife.sln`, set the configuration to **Release**, then
**Build → Build Solution** (`Ctrl+Shift+B`).

**CLI:**
```powershell
dotnet build VillageLife.sln -c Release
```

**Output:** `VillageLife/bin/<Config>/net472/VillageLife.dll`. If BepInEx is found under your Valheim
folder, the DLL is also copied to `BepInEx/plugins/VillageLife/` automatically.

### Testing through r2modman (auto-copy)

r2modman launches its **own** BepInEx inside the active profile, so the copy above (into the raw
Valheim folder) is ignored. Point the build at your profile once and every rebuild lands where
r2modman will load it:

```powershell
# one time — use YOUR profile name, then reopen Visual Studio so it sees the variable
setx R2_PROFILE "C:\Users\<you>\AppData\Roaming\r2modmanPlus-local\Valheim\profiles\<Profile>"
```

With `R2_PROFILE` set, the build copies `VillageLife.dll` into
`<profile>\BepInEx\plugins\VillageLife\`. Then the loop is just **Rebuild → Start modded**. The step
is inert if `R2_PROFILE` is unset, so it never affects other machines. Make sure that profile has
**BepInExPack_Valheim** and **Jotunn** installed.

## Packaging for Thunderstore

`Thunderstore/` holds the complete package — `manifest.json`, `icon.png`, `README.md`, `CHANGELOG.md`
and `package.ps1`. After a **Release** build, run the script to produce an upload-ready zip:

```powershell
pwsh ./Thunderstore/package.ps1
```

It verifies the version is in sync across `manifest.json`, `Constants.PluginVersion` and the
`<Version>` in `VillageLife.csproj`, then writes `dist/VillageLife-<version>.zip` with the layout
Thunderstore expects (`manifest.json`, `icon.png`, `README.md`, `CHANGELOG.md`,
`plugins/VillageLife.dll`). Upload it at [thunderstore.io](https://thunderstore.io/) under your team.
Published version numbers are **permanent**, so bump the version (all three files above) for every
release.

## Roadmap

The original roadmap is complete:

1. ~~Coin merchant using Valheim's trade window.~~ ✅
2. ~~Multiple merchant types, each selling different goods.~~ ✅
3. ~~Barter vendors — a fixed product for a fixed resource.~~ ✅
4. ~~Config-driven vendor catalogue (`vendors.json`).~~ ✅
5. ~~Creation UI to pick a villager's name/type — plugs into `NpcSpawner`.~~ ✅
6. ~~Visual variety, plus later roles (guards, bounties + reputation, trade-skill shops).~~ ✅

Built on top of that core since: a **single VillageLife build tab**, **per-biome spawners** that open
a choice menu, a full **3 shops + 3 barterers per biome**, **per-tier coin pricing**, **reputation**
that unlocks higher-tier goods, distinct **per-tier station models** (a maypole Village Hall, an
armor-stand Guard Post, and a cauldron/chairs/thrones for the biome spawners), **ambient chatter**
for every villager, and a **Dismiss** button — see `Thunderstore/CHANGELOG.md` for the full history.

### Still to come

- **Deeper quests** — the turn-in bounties and the new multi-item **Quest Board** are the first
  steps; larger objectives (staged goals, varied rewards, kill/explore tasks) build on those rails.

Localization was considered and **deliberately dropped** — it adds churn and a visible failure mode
(`$token` strings) for little benefit in a single-language setup.
