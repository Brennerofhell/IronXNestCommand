# IronXNestCommand

**IronXNestCommand** ist eine hochentwickelte Operator-Assistenz-Mod für das Spiel *Iron Nest: Heavy Turret Simulator*. Die Mod basiert auf dem **MelonLoader** (IL2CPP) Framework für .NET 6.

Das Ziel dieser Mod ist es, das Operator-Erlebnis durch intelligente Assistenzsysteme (Munitions-Management, Ballistik-Advisor, eigene Loadouts) zu erweitern, ein persistentes Rang- und Währungssystem einzuführen und dabei stets maximale Fairness und Kompatibilität im Co-op Multiplayer zu gewährleisten.

---

## ✨ Features & Module

### 1. In-Game GUI Overlay (`UI/CommandOverlay.cs`)
- Interaktives **Dieselpunk / Teleprinter** Overlay im militärischen Cockpit-Stil (Taste **`F8`** zum Umschalten).
- 5 spezialisierte Konsolen-Tabs:
  1. **STATUS**: System- und Multiplayer-Status, FairnessGuard-Indikator, Steam-Lobby & Mitspielerliste, Test-Simulator.
  2. **ADVISOR**: Interaktiver Zielrechner mit automatischer Shell- und Powder-Charge-Empfehlung sowie Shell-Datenbank.
  3. **ECONOMY**: Kontostände für *Intel Points*, *Logistics Tokens* und *Command Favor* sowie aktives Loadout-Preset.
  4. **RANKS**: Aktueller Operator-Rang, visuelle XP-Fortschrittsleiste, Perk-Freischaltungen und Lifetime-Statistiken.
  5. **CONFIG**: Hotkey-Einstellung, Fairness-Optionen und Mod-Toggles mit direkter Speicherung.

### 2. Rang- & Erfahrungssystem (`Progression/`)
- 7 detaillierte Dienstgrade:
  - **Recruit Operator** (0 XP)
  - **Junior Gunner** (500 XP)
  - **Qualified Operator** (1.500 XP)
  - **Senior Operator** (3.500 XP)
  - **Master Gunner** (7.000 XP)
  - **Nest Commander** (12.000 XP)
  - **High Command Liaison** (20.000 XP)
- XP-Verdienst durch Missionssiege, Trefferquote und Counter-Battery-Erfolge.
- Automatische Belohnungsausschüttung (Command Favor & Intel Points) bei Beförderungen.

### 3. Logistik & Währungssystem (`Economy/`)
- **Intel Points**: Verdient durch Aufklärung und Treffer, schaltet taktische Analysen frei.
- **Logistics Tokens**: Verwaltet Nachschub und Loadout-Preset-Käufe.
- **Command Favor**: Seltene Währung für experimentelle Shell-Typen.
- **Fairness-Lock**: Im Multiplayer automatisch gegen unfaire Vorteilsnahme geschützt.

### 4. Ballistischer Ammo Advisor & Loadouts (`Ammo/`)
- **`AmmoAdvisor.cs`**: Berechnet für jedes Ziel (Infanterie, Spähwagen, Kampfpanzer, Beton-Bunker, feindliche Artillerie, Radarstationen) die ideale Shell und Treibladung.
- **`CustomShellManager.cs`**: Registriert neue Munitionstypen wie *EMP Shell Mk I* und *High-Velocity AP (HV-AP)*.
- **`LoadoutManager.cs`**: Speichert und lädt Munitions-Presets in `loadouts.json`.

### 5. Steamworks & Multiplayer-Schutz (`Steam/`, `Core/`)
- **`SteamworksDetector.cs`**: Fragt Steam-Lobbys und Mitspieler direkt über Steamworks ab.
- **`FairnessGuard.cs`**: Deaktiviert automatisch alle Gameplay-Boni, sobald ein Mitspieler in der Sitzung ist.
- **`ModCompatibility.cs`**: Erkennt koexistierende Mods (wie *Open Nest*).
- **`SaveManager.cs`**: Schützt den Spielstand vor Steam Cloud-Konflikten durch Speicherung unter `<Spielverzeichnis>/UserData/IronXNestCommand/`.

---

## 🛠️ Kompilieren & Setup

### Voraussetzungen
- **.NET 6.0 SDK**
- Eine aktuelle Installation von **MelonLoader** für *Iron Nest*.

### Build-Prozess
Öffne die Solution (`.sln`) in deiner bevorzugten IDE (Visual Studio / Rider) oder nutze die .NET CLI:
```bash
dotnet build IronXNestCommand.MelonLoader/IronXNestCommand.csproj -c Release
```

Die erstellte `IronXNestCommand.dll` wird anschließend in den Ordner `Mods/` deines *Iron Nest*-Verzeichnisses kopiert.

---

## 🗂️ Datenstruktur unter `UserData/IronXNestCommand/`
- `config.json`: Benutzereinstellungen & Hotkeys
- `player_progress.json`: Ränge, XP und Statistiken
- `currency_data.json`: Intel Points, Logistics Tokens, Command Favor
- `loadouts.json`: Gespeicherte Munitionspakete

---
*Dokumentation aktualisiert am 17. August 2026*
