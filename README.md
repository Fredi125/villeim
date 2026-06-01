# VillageLife

A Valheim mod that lets you build a **Village Hall** and summon friendly, named
**merchants** to your settlement.

This is a deliberately small, reliable foundation rebuilt from scratch. Earlier versions
tried to ship quests, merchants, guards, ambient dialog, localization and a custom
multiplayer layer all at once, and never reached a dependable state. The plan now is to
grow the mod one tested feature at a time on top of this core.

## What it does today

- Adds a **Village Hall** buildable to the Hammer's *Misc* tab (20 Wood, 10 Stone).
- Pressing *Use* on the hall **summons a merchant** with a random Norse name in front of it.
- Merchants open **Valheim's real trade window** — buy a starter stock of common resources
  for coins, using the game's own shop UI (no custom panel).
- Merchants are **non-hostile, stationary, persistent**, and work in **multiplayer** and on
  **dedicated servers** (they're ordinary networked objects, like Haldor).

## Project layout

```
VillageLife/
  Plugin/VillageLifePlugin.cs   BepInEx entry point; binds config; registers content
  Util/Constants.cs             Prefab names and hashed ZDO keys
  NPC/NpcPrefab.cs              Registers the merchant (Haldor clone) via PrefabManager
  NPC/VillageMerchant.cs       Per-merchant companion: persists name, sets shop stock
  NPC/MerchantStock.cs         Builds the goods list from ObjectDB (extend the economy here)
  NPC/NpcSpawner.cs            Single spawn entry point (NpcRequest) — UI plugs in here later
  Building/VillageHall.cs       Buildable piece (workbench clone) that summons a merchant
  lib/                          BepInEx / Jötunn / Harmony reference DLLs (committed)
```

### Design principles (why it should stay reliable)

- **Clone vanilla, don't hand-build.** The merchant is a Haldor clone registered through
  Jötunn's `PrefabManager`; the hall is a Jötunn `CustomPiece` cloned from the workbench.
  No manual prefab construction. We keep Haldor's own `Trader` so the shop UI is vanilla.
- **Use built-ins.** The hall lives in the vanilla *Misc* build tab (custom tabs caused
  bugs before).
- **No incidental complexity.** No Harmony patches, no custom RPC, no per-frame managers.
- **One spawn seam.** Everything that creates a merchant goes through `NpcSpawner.Spawn`,
  so a future creation UI only has to fill in an `NpcRequest`.

## Building

The build needs a local Valheim install for the game assemblies (they are **not** committed).
The BepInEx, Jötunn and Harmony reference DLLs *are* committed under `VillageLife/lib/`, so you
don't need to pre-install any mods to compile.

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

If the assemblies still can't be found, the build stops with a clear message telling you which
path it checked — that's expected, just set `VALHEIM_INSTALL` to the right folder.

### Build

**Visual Studio:** open `VillageLife.sln`, set the configuration to **Release**, then
**Build → Build Solution** (`Ctrl+Shift+B`).

**CLI:**
```powershell
dotnet build VillageLife.sln -c Release
```

**Output:** `VillageLife/bin/<Config>/net472/VillageLife.dll`. If BepInEx is found under your
Valheim folder, the DLL is also copied to `BepInEx/plugins/VillageLife/` automatically.

### Testing through r2modman (auto-copy)

r2modman launches its **own** BepInEx inside the active profile, so the copy above (into the raw
Valheim folder) is ignored. Point the build at your profile once and every rebuild lands where
r2modman will load it:

```powershell
# one time — use YOUR profile name, then reopen Visual Studio so it sees the variable
setx R2_PROFILE "C:\Users\<you>\AppData\Roaming\r2modmanPlus-local\Valheim\profiles\<Profile>"
```

With `R2_PROFILE` set, the build copies `VillageLife.dll` into
`<profile>\BepInEx\plugins\VillageLife\`. Then the loop is just **Rebuild → Start modded**. The
step is inert if `R2_PROFILE` is unset, so it never affects other machines. Make sure that
profile has **BepInExPack_Valheim** and **Jotunn** installed.

## Packaging for Thunderstore

`Thunderstore/` holds the package metadata (`manifest.json`, `README.md`, `CHANGELOG.md`)
and `VillageLife/thunderstore/` holds the icon. Drop a freshly built `VillageLife.dll`
alongside them when zipping a release. Keep both `manifest.json` files on the same version.

## Roadmap

1. ~~Merchant with simple buy/sell.~~ ✅ Done (3.1.0) — reuses Valheim's trade window.
2. Config-driven / per-merchant shop stock (extend `MerchantStock`).
3. Small creation UI (name + appearance) — plugs into `NpcSpawner`.
4. Visual variety so merchants aren't all Haldor look-alikes.
5. Further roles (quests, guards) and ambient behaviour.
