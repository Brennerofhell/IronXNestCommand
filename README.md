# IronXNestCommand

**IronXNestCommand** ist eine in Entwicklung befindliche Assistenz-Mod für das Spiel *Iron Nest: Heavy Turret Simulator*. Die Mod basiert auf dem **MelonLoader** (IL2CPP) Framework.

Das Ziel dieser Mod ist es, das Operator-Erlebnis durch intelligente Assistenzsysteme (Munitions-Management, eigene Loadouts) zu verbessern, ein persistentes Rang- und Währungssystem einzuführen und dabei stets fair im Co-op Multiplayer zu bleiben.

---

## 🏗️ Aktueller Entwicklungsstand (Architektur-Grundgerüst)

Die Mod befindet sich in der **Aufbauphase**. Das grundlegende C#-Projekt (`IronXNestCommand.MelonLoader`) steht und ist in folgende logische Module unterteilt:

### 1. Kernsysteme (`Core/`)
- **`SaveManager.cs`**: Verwaltet das Speichern und Laden von Mod-Daten (wie Custom Loadouts oder Währungen). Alle Daten werden strikt isoliert im Ordner `<Spielverzeichnis>/UserData/IronXNestCommand/` abgelegt, um Konflikte mit Steam Cloud zu vermeiden.
- **`FairnessGuard.cs`**: Das Sicherheitsherz der Mod. Deaktiviert automatisch alle QoL- und Gameplay-Boni, sobald eine Multiplayer-Sitzung erkannt wird, um cheaten zu verhindern.

### 2. Munition & Custom Shells (`Ammo/`)
- **`ShellDefinition.cs`**: Ein modulares Datenmodell zur Definition eigener Munitionsarten (Schaden, Penetration, Kosten, Explosionsradius).
- **`CustomShellManager.cs`**: Ein Registry-System, in dem neue, eigene Munitionstypen (z.B. EMP, High-Velocity AP) registriert und verwaltet werden.

### 3. Steam & Multiplayer (`Steam/`, `Patches/`)
- **`ModCompatibility.cs`**: Scannt beim Start nach anderen bekannten Co-op Mods (z.B. "Open Nest"), um rechtzeitig Konflikte zu erkennen und P2P-Routen anzupassen.
- **`MultiplayerPatches.cs` & `Main.cs`**: Sorgt dafür, dass der Eintritt in eine Multiplayer-Lobby oder das Laden einer Co-op Map zuverlässig erkannt wird, woraufhin der `FairnessGuard` ausgelöst wird.

---

## 🛠️ Kompilieren & Setup

### Voraussetzungen
- **.NET 6.0 SDK**
- Eine aktuelle Installation von **MelonLoader** für *Iron Nest*.

### Abhängigkeiten
Bevor die Mod kompiliert werden kann, müssen in der `IronXNestCommand.csproj` die Pfade zu den Game-Assemblies angepasst werden. Die Mod benötigt Zugriff auf:
- `MelonLoader.dll`
- `0Harmony.dll`
- `Il2CppInterop.Runtime.dll`

### Build-Prozess
Öffne die Solution (`.sln`) oder das Projektverzeichnis in einer IDE (Visual Studio / Rider) oder nutze die .NET CLI:
```bash
dotnet build IronXNestCommand.MelonLoader/IronXNestCommand.csproj -c Release
```

---

## 🚀 Geplante Features (TODOs)

Die folgenden Systeme sind konzipiert, aber noch nicht (oder nicht vollständig) implementiert:

- [ ] **Economy System**: Einführung von *Intel Points*, *Logistics Tokens* und *Command Favor* zur Freischaltung von Mod-Funktionen.
- [ ] **Rank & Progression**: Ein XP-System, durch das der Operator im Rang aufsteigt (Recruit bis High Command Liaison).
- [ ] **Game Assembly Injection**: Das Anflanschen der bisherigen Template-Patches (z.B. `AmmoInjectionPatch.cs`) an die tatsächlichen Spielklassen von *Iron Nest*.
- [ ] **In-Game UI / Overlay**: Ein im Dieselpunk-Stil gehaltenes Interface zur Konfiguration und zur Nutzung des Ammo Advisors.

---
*Dokumentation generiert am 17. August 2026*
