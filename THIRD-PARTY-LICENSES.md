# Drittanbieter-Lizenzen

Ab Version 0.1.3 bündelt jede IronXNestCommand-Distribution (ZIP, Standalone-`.exe`, `Setup.exe`)
die BepInEx-Runtime selbst mit, damit keine separate BepInEx-Installation mehr nötig ist. Der
vollständige Originaltext liegt in [`Licenses/`](Licenses/).

**Update 2026-08-19:** MelonLoader wird ab dieser Version nicht mehr released (nur noch BepInEx 6
IL2CPP) — siehe `CLAUDE.md`. Der MelonLoader-Lizenzeintrag unten bleibt als historische Referenz
stehen, falls MelonLoader-Releases künftig wieder aufgenommen werden; aktuell shippen keine
MelonLoader-Dateien mehr.

## BepInEx 6 IL2CPP

- Projekt: [BepInEx/BepInEx](https://github.com/BepInEx/BepInEx)
- Copyright © BepInEx Team
- Lizenz: **GNU Lesser General Public License v2.1** — siehe [`Licenses/LICENSE-BepInEx.txt`](Licenses/LICENSE-BepInEx.txt)
- Gebündelte Version: Bleeding-Edge Build **785** (`6.0.0-be.785+6abdba4`, Win x64 IL2CPP, Stand 2026-06-28), bezogen von `builds.bepinex.dev`
- Die LGPL-2.1 erlaubt das Bundling kompilierter Binaries in einem Drittanbieter-Installer, solange die Library für den Endnutzer ersetzbar/relinkbar bleibt — hier trivial erfüllt, da BepInEx als lose DLL-Sammlung in `BepInEx/core/` ausgeliefert wird und nichts statisch dagegen gelinkt ist.

## ⚠️ IronNestCoop.Core.dll — ungeklärte Lizenzlage

`tools/extracted_coop/IronNestCoop.Core.dll` wird seit Kurzem ebenfalls in jeden Installer gebündelt
(`BepInEx/plugins/`), damit Lobby-/P2P-Funktionen ohne separate Installation der Co-op-Basis-Mod
funktionieren. **Anders als BepInEx**: Herkunft und Lizenz dieser Datei sind nicht verifiziert — es
liegt keine Lizenzdatei, kein Copyright-Hinweis und keine erklärte Weitergabe-Erlaubnis bei. Das ist
ein offenes rechtliches Risiko, keine geklärte Entscheidung. Mögliche Alternative für eine spätere,
sauber lizenzierte Lösung: [`OPEN_NEST_CO-OP`](https://github.com/1499501762/OPEN_NEST_CO-OP)
(AGPL-3.0, passt zum selben Stack) — Achtung, AGPL-3.0 ist Copyleft und würde bei Einbindung das
gesamte verteilte IronXNestCommand-Paket AGPL-Bedingungen unterwerfen, was mit der aktuellen
MIT-Lizenz kollidiert.

## MelonLoader (historisch, nicht mehr gebündelt)

- Projekt: [LavaGang/MelonLoader](https://github.com/LavaGang/MelonLoader)
- Copyright © Lava Gang
- Lizenz: **Apache License 2.0** — siehe [`Licenses/LICENSE-MelonLoader.txt`](Licenses/LICENSE-MelonLoader.txt)
- Zuletzt gebündelte Version: **v0.7.3** (Asset `MelonLoader.x64.zip`), bis Version 0.1.5
