# ✦ IronXNestCommand // Co-op Lobby, Feind-Schutz & Lochkarten-Sync ✦

[![Installer (.exe)](https://img.shields.io/badge/Installer-.exe-D97757?style=for-the-badge&logo=windows&logoColor=white)](https://github.com/Brennerofhell/IronXNestCommand/releases/download/v0.1.5/IronXNestCommand_Setup_v0.1.5.exe)
[![Version & Downloads](https://img.shields.io/github/v/release/Brennerofhell/IronXNestCommand?style=for-the-badge&label=Release&logo=github&color=1F1E1D)](https://github.com/Brennerofhell/IronXNestCommand/releases/latest)
[![Alle Downloads](https://img.shields.io/github/downloads/Brennerofhell/IronXNestCommand/total?style=for-the-badge&label=Downloads&color=1F1E1D)](https://github.com/Brennerofhell/IronXNestCommand/releases)

[![Target Game](https://img.shields.io/badge/Game-Iron_Nest:_Heavy_Turret_Simulator-blue?style=for-the-badge)](https://store.steampowered.com)
[![Modloader](https://img.shields.io/badge/ModLoader-BepInEx_6_IL2CPP-green?style=for-the-badge)](https://github.com/BepInEx/BepInEx)
[![Design](https://img.shields.io/badge/Design_System-Anthropic_Dieselpunk-E07A5F?style=for-the-badge)](https://anthropic.com)
[![License](https://img.shields.io/badge/License-MIT-purple?style=for-the-badge)](LICENSE)

> **[⬇ Installer .exe herunterladen](https://github.com/Brennerofhell/IronXNestCommand/releases/download/v0.1.5/IronXNestCommand_Setup_v0.1.5.exe)** — Doppelklick, Assistent führt durch die Installation. Alternativ ZIP oder Standalone-exe auf der [Releases-Seite](https://github.com/Brennerofhell/IronXNestCommand/releases/latest). Kein Kompilieren nötig.

**IronXNestCommand** ist eine elegante, hochperformante **Co-op Multiplayer- & Besatzungs-Suite** für *Iron Nest: Heavy Turret Simulator*. Die Modifikation läuft auf **BepInEx 6 IL2CPP** (`BepInEx/plugins/`) als saubere **Non-Standalone Mod** und integriert sich nahtlos in die offizielle Co-op Mod `IronNestCoop.Core.dll`.

Das Interface wurde 1:1 nach der offiziellen **Anthropic / Dieselpunk Design-Vorlage** umgesetzt (kompaktes 460px-Fenster im hellen „warmen Papier"-Theme, `#D95A33` Terrakotta-Akzente, Steam-Lobby Box mit Hex-Code Kopierfunktion, Besatzungs-Initialen-Badges und prozedurale Audio-Bleeps).

---

## 🎯 Kern-Features & Problemlösungen

### 1. 🛡️ 3D-Culling-Schutz & Nebel-des-Krieges-Integrität (`EnemyDespawnGuard.cs`)
Verhindert das unerwartete Verschwinden oder Despawnen von 3D-Zielen im Co-op durch Unitys Volumen-Culling:
- **Culling-Bypass:** Hält lebende Einheiten mit `neverCull` aktiv, auch wenn der Spieler in einen anderen Raum blickt.
- **Aufklärungs-Integrität:** Das originale Spielsystem für Aufklärung und den Nebel des Krieges bleibt vollständig erhalten — unaufgeklärte Verbündete und Feinde werden erst bei tatsächlicher Aufklärung/Spotter-Sichtung auf dem Kartentisch sichtbar.
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

## 🚀 Installation & Deployment (Non-Standalone, BepInEx 6 IL2CPP)

> **Hinweis:** MelonLoader wird ab dieser Version nicht mehr released — nur noch BepInEx 6 IL2CPP.

### Option A: Fertiges Paket (empfohlen, kein Kompilieren nötig)
1. **[⬇ Neueste Version von der Releases-Seite laden](https://github.com/Brennerofhell/IronXNestCommand/releases/latest)** — drei Formate zur Auswahl:
   - `IronXNestCommand_Setup_v0.1.5.exe` — echter Windows-Installer (Assistent, automatische Spielverzeichnis-Erkennung, sauberer Deinstallations-Eintrag in Windows). **Empfohlen.**
   - `IronXNestCommand-Installer.exe` — Single-File Standalone-Installer, keine Zusatz-Tools nötig.
   - `IronXNestCommand_v0.1.5.zip` — klassisches ZIP zum Entpacken.
2. Installer ausführen bzw. ZIP entpacken und `Install-Mod.bat` doppelklicken (Spielverzeichnis wird automatisch gefunden).
3. **Keine separate BepInEx-Installation nötig:** Alle drei Pakete bringen BepInEx 6 IL2CPP bereits mit (siehe [`THIRD-PARTY-LICENSES.md`](THIRD-PARTY-LICENSES.md)) — dadurch wachsen die Downloads auf ~35+ MB. Ist BepInEx in deinem Spielordner schon vorhanden, wird es nicht überschrieben.

> ⚠️ **Windows SmartScreen** ("Windows hat Ihren PC geschützt") kann bei den beiden `.exe`-Installern erscheinen — die Dateien sind (noch) nicht code-signiert und haben als frisch veröffentlichtes Release noch keine Download-Reputation bei Microsoft aufgebaut. Das ist **kein Virenfund**, nur eine Reputationswarnung für neue, unsignierte Programme. Auf **"Weitere Informationen"** → **"Trotzdem ausführen"** klicken, oder alternativ das ZIP-Paket verwenden (löst keine SmartScreen-Prüfung aus). Automatisches Code-Signing über [SignPath](https://signpath.io) ist per CI-Workflow vorbereitet (`.github/workflows/release.yml`, siehe DOCUMENTATION.md §3.23) und wird die Warnung für künftige Releases beseitigen, sobald die einmalige SignPath-Projekteinrichtung abgeschlossen ist.

### Option B: Selbst bauen & direkt deployen
Führe im Projektverzeichnis einfach [`Build-And-Deploy.bat`](file:///c:/Users/07785/Documents/PROGRAMMIEREN/IronXNestCommand/Build-And-Deploy.bat) aus.

Das Skript baut das Projekt und installiert die DLLs automatisch an beiden Orten:
- `Mods\IronXNestCommand.dll` (für MelonLoader)
- `BepInEx\plugins\IronXNestCommand.dll` (für BepInEx)

### Option C: Release-Paket oder Installer .exe bauen
Führe [`Package-Release.bat`](file:///c:/Users/07785/Documents/PROGRAMMIEREN/IronXNestCommand/Package-Release.bat) aus:
- Baut die Solution im Release-Modus.
- Erstellt ein fertiges, verteilbares Standalone-ZIP-Paket unter `dist/IronXNestCommand_v0.1.5.zip`.
- Wenn [Inno Setup 6](https://jrsoftware.org/isdl.php) installiert ist, wird über [`tools/Installer.iss`](file:///c:/Users/07785/Documents/PROGRAMMIEREN/IronXNestCommand/tools/Installer.iss) automatisch eine eigenständige `IronXNestCommand_Setup_v0.1.5.exe` generiert.

---

## ⌨️ Tastenbelegung & Steuerung

- **`F8`**: Öffnet / Schließt das IronXNestCommand Menü (im Einstellungs-Tab frei auf F7 bis F12 umstellbar).
- **`🏠`**: Springt aus jedem Tab sofort zurück zur Lobby-Übersicht (Header-Icon oder „🏠 ZU HOME"-Button im Einstellungen-Tab).
- **`Kopieren`**: Kopiert den Lobby-Hex-Code in die Zwischenablage.
- **`Einladen`**: Öffnet das native Steam-Einladungsfenster.
- **`🔄 Besatzung re-syncen`**: Synchronisiert alle Lochkarten und Zieldaten sofort.
