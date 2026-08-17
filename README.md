# ✦ IronXNestCommand // Co-op Lobby, Feind-Schutz & Lochkarten-Sync ✦

[![Target Game](https://img.shields.io/badge/Game-Iron_Nest:_Heavy_Turret_Simulator-blue?style=for-the-badge)](https://store.steampowered.com)
[![Modloader](https://img.shields.io/badge/ModLoader-MelonLoader_/_BepInEx_6-green?style=for-the-badge)](https://github.com/LavaGang/MelonLoader)
[![Design](https://img.shields.io/badge/Design_System-Anthropic_Dieselpunk-E07A5F?style=for-the-badge)](https://anthropic.com)
[![License](https://img.shields.io/badge/License-MIT-purple?style=for-the-badge)](LICENSE)

**IronXNestCommand** ist eine elegante, hochperformante **Co-op Multiplayer- & Besatzungs-Suite** für *Iron Nest: Heavy Turret Simulator*. Die Modifikation unterstützt **MelonLoader** (`Mods/`) sowie **BepInEx 6 IL2CPP** (`BepInEx/plugins/`) als saubere **Non-Standalone Mod** und integriert sich nahtlos in die offizielle Co-op Mod `IronNestCoop.Core.dll`.

Das Interface wurde 1:1 nach der offiziellen **Anthropic / Dieselpunk Design-Vorlage** umgesetzt (520px Breite, `#18181B` Dark-Graphite, `#D97757` Terrakotta-Akzente, Steam-Lobby Box mit Hex-Code Kopierfunktion, Besatzungs-Initialen-Badges, prozedurale Audio-Bleeps und synchronisierte Statusleiste).

---

## 🎯 Kern-Features & Problemlösungen

### 1. 🛡️ Gegner-Despawn & Culling-Schutz (`EnemyDespawnGuard.cs`)
Verhindert das unerwartete Verschwinden oder Despawnen von gegnerischen Einheiten und Zielen im Co-op:
- **Culling-Bypass:** Hält lebende Einheiten aktiv, auch wenn der Spieler in einen anderen Raum blickt.
- **Sichtbarkeits-Override:** Überschreibt fehlerhafte `HideVisualRoot`-Aufrufe und Nebel-Timeouts solange die Einheit lebt.
- **Aktiver Watchdog (1,5 s):** Überprüft kontinuierlich alle `EntityLocation`-Objekte und stellt die Sichtbarkeit sicher.
- **Einstellbar:** Über das In-Game-Menü (`[F8] -> Einstellungen`) jederzeit per Klick umschaltbar.

---

### 2. 🌐 Modernes Co-op Lobby- & Besatzungs-Management
- **Steam-Lobby Card:** Monospace Hex-Code-Anzeige (`4A2F-9C1B`) mit **1-Klick Kopieren** (`✔ Kopiert`) und direktem **Freunde-Einladen** ins Steam-Overlay.
- **Lobby Beitreten:** Direkte Eingabe oder Einfügen (Paste) von Hex-Codes oder 64-Bit Steam-IDs.
- **Besatzungs-Liste:** Übersicht aller Gunner an den Rohren mit Initialen-Badges (`[HM]`, `[TD]`), Ping-Anzeige und Rollen (`👑 Kommandant · 28 ms`, `🎯 Richtschütze · 34 ms`).
- **Freie Plätze:** Visuelle Anzeige freier Geschütz-Plätze (`Freier Platz an Rohr X`).

---

### 3. 🖨️ Lochkarten Co-op Fix (für Gäste / Nicht-Hosts)
- **Automatischer Drucker-Sync:** Synchronisiert berechnete Zieldaten automatisch auf den Rechentisch des Gastes.
- **Sofortiger Re-Sync:** `[ 🔄 Besatzung re-syncen ]` triggert den Raum- und Lochkarten-Sync via `MissionSync.StartResyncNow()` und `OnRequestState()`.

---

### 4. 🔊 Prozedurales Audio-Feedback (Bleeps & Clicks)
- Synthetische Sinuswellen-Töne direkt aus dem RAM für Klicks (800 Hz), Tabwechsel (1200 Hz) und Level-Up Fanfaren (keine externen Sounddateien).

---

## 🚀 Installation & Deployment (Non-Standalone Dual-Loader)

### 1-Klick Deployment
Führe im Projektverzeichnis einfach [`Build-And-Deploy.bat`](file:///c:/Users/07785/Documents/PROGRAMMIEREN/IronXNestCommand/Build-And-Deploy.bat) aus.

Das Skript baut das Projekt und installiert die DLLs automatisch an beiden Orten:
- `Mods\IronXNestCommand.dll` (für MelonLoader)
- `BepInEx\plugins\IronXNestCommand.dll` (für BepInEx)

---

## ⌨️ Tastenbelegung & Steuerung

- **`F8`**: Öffnet / Schließt das IronXNestCommand Menü (im Einstellungs-Tab frei auf F7 bis F12 umstellbar).
- **`Kopieren`**: Kopiert den Lobby-Hex-Code in die Zwischenablage.
- **`Einladen`**: Öffnet das native Steam-Einladungsfenster.
- **`🔄 Besatzung re-syncen`**: Synchronisiert alle Lochkarten und Zieldaten sofort.
