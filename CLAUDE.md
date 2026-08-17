# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

**IronXNestCommand** is a mod for the Unity/IL2CPP game *Iron Nest: Heavy Turret Simulator*. It started as an operator-assistance mod (ammo advisor, custom shells, currency/economy, rank/XP progression) and has since grown a co-op-focused layer on top:
- A pixel-accurate "Anthropic/Dieselpunk"-styled lobby & crew-management overlay (`[F8]`, hex-code lobby join/create, Steam friend invite, crew roster with ping/role).
- `EnemyDespawnGuard.cs` — stops enemies from despawning/going invisible for guests due to Unity culling/visibility bugs in co-op.
- `CoopPunchcardFix.cs` / `PunchcardSpawner.cs` — fixes fire-mission punchcards not appearing/being grabbable on non-host clients, and auto-extracts target coordinates from incoming teleprinter radio messages.
- `AmmoRequisitionBridge.cs` — forces an immediate resync (`MissionSync.StartResyncNow()`/`OnRequestState()`) of a guest's cockpit state.

Documentation is in German. `README.md` is the current-feature quick-start (co-op/lobby framing); `DOCUMENTATION.md` is the fuller manual with root-cause writeups for each co-op bugfix and the GUI's exact design tokens; `IronNest-Mod-Plan.md` is the original design/roadmap doc for the older ammo/economy/progression scope.

**Scope decision (project owner, 2026-08-18): the mod's sole purpose going forward is bringing multiplayer/co-op to the game.** The older ammo-advisor/economy/progression/custom-shell systems (`Ammo/`, `Economy/`, `Progression/` in both hosts) are explicitly **out of scope** — they stay in the tree as dormant legacy code, but are not to be actively developed or completed. Interop with that older feature set, if ever needed, happens either via compatibility rules with *other* mods, or in a separate future version — not by building it out here. Concretely: **do not implement the `// TODO` stubs in `Patches/AmmoInjectionPatch.cs`** (they reference a fictional `ItemDatabase` type; the game's real types are `ShellBlueprint`/`ShellDefinition`/`ShellSlotPool`/`RequisitionConsoleManager`, found via `all_types.txt`, but wiring custom shells into them is out of scope, not merely unresearched). Default to co-op/lobby work unless told otherwise.

## Two parallel host projects — know which one you're editing

The repo contains **two independent mod-loader hosts for the same feature set**, each with its own near-duplicate `Ammo/`, `Economy/`, `Progression/`, `Steam/`, `Core/`, `Patches/` folders (overlay UI is `UI/CommandOverlay.cs` in MelonLoader, `Overlay/CommandOverlay.cs` in BepInEx). They are not layered — features added to one do not automatically exist in the other; both currently have `EnemyDespawnGuard`/`CoopPunchcardFix`/`AmmoRequisitionBridge`/`PunchcardSpawner`, so treat that as a "must update both" set when touching co-op fixes, unless told to target only one host.

| | `IronXNestCommand.Host.BepInEx/` | `IronXNestCommand.MelonLoader/` |
|---|---|---|
| Loader | BepInEx 6 IL2CPP | MelonLoader 0.7.3 (`net6.0`) |
| Entry point | `Plugin.cs` (`BasePlugin.Load()`) | `Main.cs` (`MelonMod.OnInitializeMelon()`) |
| Status | **Primary.** README, DOCUMENTATION.md, and the double-clickable `Build-And-Deploy.bat`/`Install-Mod.bat`/`Deinstall-Mod.bat` target this. | Actively supported in parallel (dual-loader install, both mods run side by side) — build via `tools/Deploy-Melon.ps1`. Needs MelonLoader itself installed in the game folder (separate from this mod) for its `Il2CppAssemblies` to exist at all. |
| Deploys to | `<game>/BepInEx/plugins/IronXNestCommand.dll` (+ `IronXNestCommand.Core.dll`) | `<game>/Mods/IronXNestCommand.dll` |

`tools/ModManagerGUI.ps1` (via `Uninstall-GUI.bat`) is loader-agnostic — it scans and can remove entries from both `BepInEx/plugins` and `Mods`.

## `IronXNestCommand.Core/` — a namespace landmine, not a full shared layer

`IronXNestCommand.Core` (`Config/ConfigStore.cs`, `Config/ModConfig.cs`, `Logging/ModLogger.cs`, `Paths/ModPaths.cs`, `ModInfo.cs`) is a small loader-independent project, all under namespace `IronXNestCommand.Core` / `IronXNestCommand.Core.Config` / etc. Both host `.csproj`s now `ProjectReference` it.

However, **each host also keeps its own local `Core/` folder** (`ModConfig.cs`, `SaveManager.cs`, `FairnessGuard.cs`, `TurretTelemetry.cs`, `AudioFeedback.cs`, `PunchcardSpawner.cs`) whose types are declared directly in namespace `IronXNestCommand.Core` — the *same* namespace as the shared project, not a sub-namespace. `Main.cs`'s `using IronXNestCommand.Core;` resolves to these **local** duplicates, not the shared project's `Config.ModConfig`/`Paths.ModPaths`. The shared project effectively only contributes `ModInfo` and whatever isn't shadowed locally. Telemetry/Audio/Save logic is **not** actually shared between hosts despite living in a project both reference — don't assume editing `IronXNestCommand.Core` propagates to gameplay behavior in either host, and watch for accidental type collisions if you ever add a class name to the shared project that a host's local `Core/` folder also defines.

## Build & deploy commands

All scripts assume the game is installed at `C:\Program Files (x86)\Steam\steamapps\common\Iron Nest Heavy Turret Simulator` (some, like `Install-Mod.bat`, fall back to scanning common `D:`/`E:`/`F:` SteamLibrary paths or prompting for a path if not found there).

```bash
# Build everything (Core + BepInEx + MelonLoader — all three are in the .sln)
dotnet build IronXNestCommand.sln -c Release

# BepInEx (primary), build + copy straight to <game>/BepInEx/plugins/
powershell -File tools/Deploy.ps1
# ...or the double-clickable equivalent:
Build-And-Deploy.bat

# MelonLoader (secondary), build + copy straight to <game>/Mods/
powershell -File tools/Deploy-Melon.ps1
```

`GameFolder`/`GameDir` MSBuild properties (see `Directory.Build.props` and the MelonLoader `.csproj`) override the hardcoded Steam path for `dotnet build` invocations.

End-user (not dev) install/uninstall, auto-detecting the game folder:
- `Install-Mod.bat` / `Deinstall-Mod.bat` — copy/remove the BepInEx payload and `UserData/IronXNestCommand`.
- `Uninstall-GUI.bat` → `tools/ModManagerGUI.ps1` — WinForms GUI over the same detection logic, covering both `BepInEx/plugins` and `Mods`.

There is no automated test suite — validate by building and, where feasible, running the game with the mod deployed (`F8` opens the main overlay; hotkey is reconfigurable to F7–F12 in-game).

## Architecture notes

- **Harmony patches** (`Patches/`) hook real game methods on IL2CPP-interop'd types — fragile across game updates. `all_types.txt` at the repo root is a dumped list of the game's IL2CPP type names; check it before guessing at a type/method name for a new patch. Known patch targets per `DOCUMENTATION.md`: `MinimalVolumeCulling.CullTarget.ApplyCulled`, `EntityLocation.HideVisualRoot`/`Init` (despawn guard), `FireMissionCardPrinter.HandleCalculationSuccess`, `FireMissionCard.Apply`, `Teleprinter.SubmitLines` (punchcard fix), plus the older `Event_OnMissionCompleted`/`Event_ShellLanded`/`State_AddRequisitionPoints` (mission/XP/requisition hooks).
- **Always reference game types via reflection (`Type.GetType("X, Assembly-CSharp")` + `PropertyInfo`), never via a direct `using`/compile-time type name.** MelonLoader's Cpp2IL-generated `Il2CppAssemblies` don't reliably expose every game type for direct C# referencing even when the type genuinely exists in the game (confirmed with `EntityLocation`/`MinimalVolumeCulling` — direct reference gave `CS0400` on a from-scratch MelonLoader interop generation, while the exact same types worked fine via reflection). Every existing patch file already follows this pattern (`GameEventsPatch.cs`, `CoopPunchcardFix.cs`, both hosts' `EnemyDespawnGuard.cs`) — keep new ones consistent with it, it's not optional stylistic preference.
- **MelonLoader needs MelonLoader itself installed separately from this mod.** `IronXNestCommand.MelonLoader` only compiles/runs once `<game>/MelonLoader/Il2CppAssemblies/` exists, which MelonLoader generates itself on its first successful game launch (`MelonLoader/Latest.log` should end with no errors and show `Assembly Generation Successful!`). If that folder or `MelonLoader/Latest.log` is missing, the loader isn't installed — get it from `https://github.com/LavaGang/MelonLoader/releases` (`MelonLoader.x64.zip`, matching the `LavaGang.MelonLoader` version pinned in the `.csproj`; extract directly into the game root) rather than assuming the mod build is broken.
- **FairnessGuard**: the multiplayer-fairness gate. When a co-op session is detected, gameplay-affecting bonuses (from the older economy/progression systems) are disabled while XP/rank tracking continues — no Steam achievement/leaderboard manipulation. Gate any new gameplay-affecting bonus behind it; cosmetic/co-op-quality-of-life fixes (despawn guard, punchcard sync) are not gated by it.
- **Steam lobby integration** (`Steam/SteamworksDetector.cs`) resolves human-readable hex lobby codes (e.g. `4A2F-9C1B`) to 64-bit Steam lobby IDs via reflection into the co-op plugin's `SteamNet` type (soft dependency on `IronNestCoop.Core.dll`, GUID `de.jager.ironnestcoop`), not a direct Steamworks.NET call — reflection was chosen specifically to avoid hard version coupling to that plugin.
- **Save data** lives under `<game>/UserData/IronXNestCommand/` (`config.json`, `player_progress.json`, `currency_data.json`, `loadouts.json`, `notes.json`) — deliberately outside the game's own save/Steam Cloud paths to avoid cloud-sync conflicts.
- **CommandOverlay** is a hand-rolled IL2CPP-safe Unity IMGUI renderer (not UGUI), built to pixel/token-match a specific design spec (520px width, `#18181B` background, `#D97757` terracotta accents — see `DOCUMENTATION.md` §3 for the full token table) — treat that table as the source of truth if asked to adjust overlay visuals.
- `tools/extracted_coop/` and `grok/` hold reference material (an extracted build of the co-op plugin, chat export notes) — not compiled into the mod.
