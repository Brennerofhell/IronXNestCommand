# Drittanbieter-Lizenzen

Ab Version 0.1.3 bündelt jede IronXNestCommand-Distribution (ZIP, Standalone-`.exe`, `Setup.exe`)
die Modloader-Runtimes selbst mit, damit keine separate Installation von BepInEx oder MelonLoader
mehr nötig ist. Beide Projekte werden unverändert redistribuiert; die vollständigen Originaltexte
liegen in [`Licenses/`](Licenses/).

## BepInEx 6 IL2CPP

- Projekt: [BepInEx/BepInEx](https://github.com/BepInEx/BepInEx)
- Copyright © BepInEx Team
- Lizenz: **GNU Lesser General Public License v2.1** — siehe [`Licenses/LICENSE-BepInEx.txt`](Licenses/LICENSE-BepInEx.txt)
- Gebündelte Version: Bleeding-Edge Build **785** (`6.0.0-be.785+6abdba4`, Win x64 IL2CPP, Stand 2026-06-28), bezogen von `builds.bepinex.dev`
- Die LGPL-2.1 erlaubt das Bundling kompilierter Binaries in einem Drittanbieter-Installer, solange die Library für den Endnutzer ersetzbar/relinkbar bleibt — hier trivial erfüllt, da BepInEx als lose DLL-Sammlung in `BepInEx/core/` ausgeliefert wird und nichts statisch dagegen gelinkt ist.

## MelonLoader

- Projekt: [LavaGang/MelonLoader](https://github.com/LavaGang/MelonLoader)
- Copyright © Lava Gang
- Lizenz: **Apache License 2.0** — siehe [`Licenses/LICENSE-MelonLoader.txt`](Licenses/LICENSE-MelonLoader.txt)
- Gebündelte Version: **v0.7.3** (Asset `MelonLoader.x64.zip`)

## Hinweis zum ersten Spielstart (MelonLoader)

MelonLoaders `Il2CppAssemblies` werden erst beim ersten tatsächlichen Spielstart nach der Installation
generiert (ein MelonLoader-interner Mechanismus, siehe `CLAUDE.md`). Das Bundling erspart nur den
separaten Download-/Installationsschritt für MelonLoader selbst — der erste Spielstart nach der
Installation dauert trotzdem spürbar länger, während MelonLoader die Interop-Assemblies generiert.
