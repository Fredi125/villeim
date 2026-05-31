# VillageLife

A Valheim mod that lets you build a **Village Hall** and summon friendly, named
**villagers** to your settlement.

This is a deliberately small, reliable foundation (v3.0) rebuilt from scratch. Earlier
versions tried to ship quests, merchants, guards, ambient dialog, localization and a
custom multiplayer layer all at once, and never reached a dependable state. The plan now
is to grow the mod one tested feature at a time on top of this core.

## What it does today

- Adds a **Village Hall** buildable to the Hammer's *Misc* tab (20 Wood, 10 Stone).
- Pressing *Use* on the hall **summons a friendly villager** with a random Norse name in
  front of it.
- Villagers are **non-hostile, stationary, persistent**, and **greet you** when talked to.
- Villagers are ordinary networked creatures, so they work in **multiplayer** and on
  **dedicated servers**.

Villagers are intentionally passive for now — no wandering, trading, combat or quests yet.

## Project layout

```
VillageLife/
  Plugin/VillageLifePlugin.cs   BepInEx entry point; binds config; registers content
  Util/Constants.cs             Prefab names and hashed ZDO keys
  NPC/NpcPrefab.cs              Registers the villager (Haldor clone) via CreatureManager
  NPC/VillageNpc.cs            Per-villager controller: hover name, talk greeting, ZDO name
  NPC/NpcSpawner.cs            Single spawn entry point (NpcRequest) — UI plugs in here later
  Building/VillageHall.cs       Buildable piece (workbench clone) that summons a villager
  lib/                          BepInEx / Jötunn / Harmony reference DLLs (committed)
```

### Design principles (why it should stay reliable)

- **Clone vanilla, don't hand-build.** The villager is registered through Jötunn's
  `CreatureManager` as a clone of Haldor; the hall is a Jötunn `CustomPiece` cloned from
  the workbench. No manual `Instantiate` + component-stripping during prefab setup.
- **Use built-ins.** The hall lives in the vanilla *Misc* build tab (custom tabs caused
  bugs before).
- **No incidental complexity.** No Harmony patches, no custom RPC, no per-frame managers.
- **One spawn seam.** Everything that creates a villager goes through `NpcSpawner.Spawn`,
  so a future creation UI only has to fill in an `NpcRequest`.

## Building

The build needs a local Valheim install for the game assemblies (they are **not** committed).

1. Point the build at your install, either by setting an environment variable:
   ```
   VALHEIM_INSTALL=C:\Program Files (x86)\Steam\steamapps\common\Valheim
   ```
   or by editing `ValheimDir` in `VillageLife/VillageLife.csproj`.
2. Build:
   ```
   dotnet build VillageLife.sln -c Release
   ```
   (or open `VillageLife.sln` in Visual Studio / Rider and build).
3. Output: `VillageLife/bin/Release/net472/VillageLife.dll`. If BepInEx is found under the
   Valheim folder, it's also copied to `BepInEx/plugins/VillageLife/` automatically.

The BepInEx, Jötunn and Harmony reference DLLs are committed under `VillageLife/lib/`, so no
mod pre-install is required to compile.

## Packaging for Thunderstore

`Thunderstore/` holds the package metadata (`manifest.json`, `README.md`, `CHANGELOG.md`)
and `VillageLife/thunderstore/` holds the icon. Drop a freshly built `VillageLife.dll`
alongside them when zipping a release. Keep both `manifest.json` files on the same version.

## Roadmap

1. Small creation UI (name + appearance) — plugs into `NpcSpawner`.
2. Merchant role with simple buy/sell.
3. Quests, then ambient behaviour.
