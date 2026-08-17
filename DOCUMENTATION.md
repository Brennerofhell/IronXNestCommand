# 📚 IronXNestCommand — Entwicklerdokumentation & System-Handbuch

Dieses Handbuch dokumentiert die vollständige Systemarchitektur, die **Non-Standalone Dual-Loader Architektur (MelonLoader & BepInEx)** zur Vermeidung von Datei-Chaos, alle Co-op Bugfixes, das Enemy-Despawn-Schutzsystem, das Lochkarten-Sync-Modell, das Audio-Feedback-System (Bleeps & Klick-Sounds) sowie das 1:1 umgesetzte GUI-Design der **IronXNestCommand** Mod für *Iron Nest: Heavy Turret Simulator*.

---

## 1. Design-Entscheidung: Warum "Non-Standalone"? (Anti-Datei-Chaos)

### 1.1 Das Problem von Standalone-Paketen (Datei-Chaos)
Klassische Standalone-Installer bringen oft eigene Modloader-Runtimes, doppelte Bootstrapper (`winhttp.dll`, `doorstop_config.ini`), zusätzliche `.NET 6`-DLLs und verschachtelte `.zip`-Archive mit. 

In der Praxis führt dies zu erheblichem **Datei-Chaos**:
1. **DLL- und Runtime-Konflikte:** Wenn das Spiel bereits gemoddet ist (z. B. mit `IronNestCoop.Core.dll`), überschreiben Standalone-Pakete bestehende Konfigurationen oder erzeugen inkompatible Doppel-Laufzeiten.
2. **Hook-Kollisionen:** Unkontrollierte Doppel-Injektionen führen zu Crashes und Desyncs im Co-op.
3. **Unübersichtliche Verzeichnisse:** Temporäre `.zip`-Archive, Installer-Skripte und `dist/`-Ordner müllen das Spielverzeichnis zu.
4. **Schwierige Deinstallation:** Das Entfernen eines Standalone-Pakets hinterlässt oft verwaiste Bootstrapper.

### 1.2 Die Lösung: Saubere, modulare Non-Standalone Mod
**IronXNestCommand** wird als **reine Non-Standalone Mod** bereitgestellt:
- **MelonLoader-Nutzer:** Installieren einfach die Mod-DLL direkt in `<GameDir>\Mods\IronXNestCommand.dll`.
- **BepInEx-Nutzer:** Installieren die Mod-DLLs direkt in `<GameDir>\BepInEx\plugins\IronXNestCommand.dll` und `IronXNestCommand.Core.dll`.
- **Keine doppelten Runtimes:** Nutzt den Modloader, der bereits im Spiel eingerichtet ist.
- **Kein Datei-Chaos:** Keine Installer-Archive, keine doppelten Doorstop-DLLs.
- **100% rückstandsfreie Deinstallation:** Einfaches Löschen der Mod-DLL aus `Mods/` bzw. `BepInEx/plugins/`.

---

## 2. Systemarchitektur & Dual-Loader Unterstützung

```
+-----------------------------------------------------------------------------------+
|                        Iron Nest: Heavy Turret Simulator                          |
|                        Unity Engine (IL2CPP 64-Bit / Unity 6)                     |
+-----------------------------------------------------------------------------------+
                                          │
                   ┌──────────────────────┴──────────────────────┐
                   ▼                                             ▼
+------------------------------------+         +------------------------------------+
|            MelonLoader             |         |          BepInEx 6 IL2CPP          |
|    (Mods/IronXNestCommand.dll)     |         |  (plugins/IronXNestCommand.dll)    |
|   LavaGang.MelonLoader 0.7.3 net6  |         |   BepInEx.Unity.IL2CPP net6.0      |
+------------------------------------+         +------------------------------------+
                   │                                             │
                   └──────────────────────┬──────────────────────┘
                                          │
                                          ▼
+-----------------------------------------------------------------------------------+
|                               IronXNestCommand.Core                               |
|       - ModConfig & SaveManager (Persistenz)                                      |
|       - TurretTelemetry (Ballistik- & Vorhalt-Berechnung)                         |
|       - AudioFeedback (Synthetische Audio-Bleeps & Klicksounds)                   |
+-----------------------------------------------------------------------------------+
                                          │
                                          ▼
+-----------------------------------------------------------------------------------+
|                          Co-op & Multiplayer Engine                               |
|  1. SteamworksDetector   -> SteamNet & Steamworks.NET Hex-Lobby-Brücke             |
|  2. EnemyDespawnGuard    -> Culling-Bypass & HideVisualRoot Despawn-Schutz        |
|  3. CoopPunchcardFix     -> Lochkarten-Drucker-Sync für Gast-Spieler              |
|  4. AmmoRequisitionBridge-> Requisition & Instant Co-op Resync (StartResyncNow)  |
|  5. CommandOverlay       -> Pixel- und Token-getreues UI (520px Anthropic Design) |
+-----------------------------------------------------------------------------------+
```

---

## 3. Detaillierte Bugfixes & Problemlösungen

### 3.1 🛡️ Gegner-Despawn & Culling-Schutz (`EnemyDespawnGuard.cs`)

#### Problem-Analyse (Root Cause)
Im Mehrspieler-/Co-op-Modus verschwanden gegnerische Ziele oder Karteneinheiten unerwartet für den Host oder die Mitspieler. Die Ursachen lagen im Unity-Rendering- und Sichtbarkeits-Lifecycle:
1. **`MinimalVolumeCulling.CullTarget.ApplyCulled(true)`**: Wenn die Kamera des Spielers wegdrehte oder ein Raum gewechselt wurde, deaktivierte das Culling-System die Ziel-GameObjects.
2. **`EntityLocation.HideVisualRoot()`**: Wurde aufgerufen, sobald Scan-Fenster von Beobachtern abliefen oder Sichtbarkeitsprüfungen auf Gast-Rechnern fehlschlugen. Dies setzte `VisibilityGroup.alpha = 0` und blendete den `VisualRoot` aus.
3. **`EntityLocation.StartWithVisualRootHidden = true`**: Neu gespawnte Gegner wurden standardmäßig mit unsichtbarem VisualRoot instanziiert.

#### Die Lösung
- **`OnHideVisualRoot_Prefix`**: Fängt den Aufruf ab. Solange `entity.IsAlive == true` ist, wird das Verstecken übersprungen (`return false`) und `VisualRoot.SetActive(true)` sowie `VisibilityGroup.alpha = 1.0f` erzwungen.
- **`OnInit_Postfix`**: Setzt `StartWithVisualRootHidden = false` und aktiviert neu initialisierte Gegner sofort.
- **`OnApplyCulled_Prefix`**: Setzt `neverCull = true` und verhindert das Deaktivieren von lebenden Einheiten.
- **`UpdateWatchdog`**: Scannt alle 1,5 Sekunden alle `EntityLocation`-Instanzen und reaktiviert fälschlicherweise unsichtbar gemachte Feindeinheiten.
- **UI-Steuerung**: Über den Einstellungs-Tab (`🛡️ Gegner-Despawn Schutz`) jederzeit an- und abschaltbar.

---

### 3.2 🖨️ Lochkarten- und Druck-Synchronisation (`CoopPunchcardFix.cs` & `PunchcardSpawner.cs`)

#### Problem-Analyse
Auf Gast-Rechnern (Nicht-Host) wurden Einsatz-Lochkarten am Rechentisch nach einer Berechnung oft nicht sichtbar oder nicht gegriffen.

#### Die Lösung
1. **Drucker-Hook (`FireMissionCardPrinter.HandleCalculationSuccess`)**: Sobald eine Einsatzberechnung erfolgreich abgeschlossen wird, sichert der Postfix-Hook die Zieldaten (Entfernung, Azimut, Treibladung, Überhöhung) und benachrichtigt den Spawner.
2. **Karten-Hook (`FireMissionCard.Apply`)**: Überträgt berechnete Werte direkt in die Instanz.
3. **Teleprinter-Hook (`Teleprinter.SubmitLines`)**: Extrahiert automatisch Zielkoordinaten aus eingehenden HQ-Funksprüchen via Regex und berechnet mit `TurretTelemetry` eine sofortige Feuerleitlösung.
4. **Resync-Bridge (`AmmoRequisitionBridge.TriggerCoopResync`)**: Ruft `IronNestCoop.Core.Sync.MissionSync.StartResyncNow()` und `OnRequestState()` auf, leert die Objekt-Caches und erzwingt das Aktivieren aller Lochkarten-Slots am Cockpittisch des Gastes.

---

### 3.3 🌐 Steam-Lobby & Hex-Code Beitritt (`SteamworksDetector.cs`)

#### Problem-Analyse
- Beim Eingeben von Hex-Lobby-Codes kam es zu Typkonvertierungs-Fehlern, wenn Methoden eine `ulong` 64-Bit Steam-ID erwarteten.
- Der Wechsel zwischen `IronNestCoop` und generischem `Steamworks.NET` führte zu NullReferenceExceptions.

#### Die Lösung
- **Dynamische Typauflösung**: Scannt `SteamNet` via Reflection und bindet `SteamNet.ResolveLobbyId(string)` ein. Hex-Codes (z. B. `4A2F-9C1B`) werden sauber in echte 64-Bit Steam-Lobby-IDs aufgelöst.
- **Lobby-Management**:
  - `TryCreateLobby(int maxPlayers)`: Erstellt eine Co-op Lobby für bis zu 4 Spieler.
  - `TryJoinLobby(string codeOrId)`: Akzeptiert sowohl formatierte Hex-Strings als auch unformatierte 64-Bit IDs.
  - `TryLeaveLobby()`: Verlässt die aktuelle Lobby sauber und setzt alle Mitspieler-Listen zurück.
  - `TryOpenInviteOverlay()`: Öffnet das native Steam-Freundeseinladungs-Overlay (`steam://friends`).

---

### 3.4 🔊 Audio-Feedback & Sound-Synthese (`AudioFeedback.cs`)
- **Prozedurale Sinuswellen-Generierung:** Keine externen `.wav`- oder `.mp3`-Dateien nötig (100% autark).
- **Audio-Effekte:**
  - **Click/Bleep (`PlayClick`):** Kurzer 800 Hz Impuls (25 ms) mit exponentiellem Decay für Button-Klicks.
  - **Switch/Bleep (`PlayTargetSwitch`):** 1200 Hz Bestätigungston für Tab- und Zielwechsel.
  - **Fanfare/Bleep (`PlayLevelUp`):** 4-stufige C-Dur Fanfare (C5, E5, G5, C6) für Rang-Aufstiege und Lobby-Erstellung.
- **Einstellbar:** Über das Menü (`Audio-Rückmeldung bei Klicks & Aktionen`) per Checkbox umschaltbar.

---

### 3.5 🖱️ Overlay nicht klickbar (Cursor-Sperre durch Turret-Zielsteuerung)

#### Problem-Analyse (Root Cause)
Als Heavy-Turret-Simulator sperrt und versteckt das Spiel den System-Cursor während des Zielens (`Cursor.lockState = CursorLockMode.Locked`, `Cursor.visible = false`). Beim Öffnen des Overlays (`[F8]`) blieb diese Sperre unangetastet — der Mauszeiger konnte sich nicht bewegen, Klicks auf Buttons kamen nie an.

#### Die Lösung
- Neue zentrale `SetVisible(bool)`-Methode in `CommandOverlay` (beide Hosts): setzt beim Öffnen `Cursor.lockState = CursorLockMode.None` und `Cursor.visible = true`, beim Schließen wieder `Locked`/`false`.
- Ersetzt alle direkten `IsVisible = ...`-Zuweisungen (Hotkey-Toggle, `✕`-Button, Initial-Zustand beim Mod-Start).

---

### 3.6 💥 StackOverflowException beim Verlassen der Lobby (MelonLoader)

#### Problem-Analyse (Root Cause)
`MultiplayerPatches.LeaveLobby_Postfix` (Harmony-Postfix auf `Steamworks.SteamMatchmaking.LeaveLobby`) rief `SteamworksDetector.OnLobbyLeft()` auf. `OnLobbyLeft()` rief wiederum `TryLeaveLobby()` auf — welches im generischen Steamworks-Fallback-Pfad **erneut** die native `LeaveLobby`-Methode invokte. Das löste denselben Harmony-Postfix erneut aus → unendliche Rekursion → Absturz des Spielprozesses.

#### Die Lösung
- `OnLobbyLeft()` ist jetzt reiner Zustands-Reset (kein erneuter nativer Aufruf) — genau das, was der Postfix nach bereits erfolgtem nativen `LeaveLobby()` braucht.
- `TryLeaveLobby()` (für den nutzerausgelösten Fall, z. B. Klick auf „🚪 Verlassen") ruft die native/Coop-Leave-Methode auf und delegiert danach an `OnLobbyLeft()` für den Reset — spiegelt jetzt exakt das bereits korrekte Muster aus dem BepInEx-Host.

---

### 3.7 ⚖️ FairnessGuard sperrte keine Währungs-Belohnungen

#### Problem-Analyse (Root Cause)
`FairnessGuard.IsMultiplayerActive` wurde korrekt gesetzt, aber `CurrencyManager.AddCurrency(...)` (Intel Points, Logistics Tokens, Command Favor) prüfte es nirgends. Treffer- und Missionsbelohnungen wurden im Multiplayer 1:1 wie im Singleplayer gutgeschrieben — ein klarer Verstoß gegen das dokumentierte Fairness-Versprechen. Zusätzlich setzte `SteamworksDetector.CheckSteamState()` (MelonLoader) `FairnessGuard` beim Verlassen einer Coop-Lobby nie zurück auf `false`.

#### Die Lösung
- `CurrencyManager.AddCurrency(...)` (beide Hosts) prüft jetzt zentral `FairnessGuard.IsMultiplayerActive` und verweigert die Gutschrift im Multiplayer — an dieser einen Stelle, damit kein Aufrufer (aktuell oder künftig) das umgehen kann.
- Rang/XP (`ProgressionManager.AddXP`) bleiben bewusst **ungegated** — laut Mod-Plan zählt Progression im Multiplayer weiter, nur die Wirtschaft ist Singleplayer-exklusiv.
- `CheckSteamState()` (MelonLoader) setzt `FairnessGuard.SetMultiplayerState(false)`, sobald keine aktive Lobby mehr erkannt wird (spiegelt den BepInEx-Host).

---

### 3.8 🖱️×N Mehrfach-Feuern von GUI-Buttons pro Klick

#### Problem-Analyse (Root Cause)
`DrawButton()` erkannte Klicks über `Input.GetMouseButtonUp(0)` — dieser Zustand bleibt für das **gesamte physische Frame** wahr, während Unity IMGUI `OnGUI()` pro Frame mehrfach aufruft (Layout- und Repaint-Pass, ggf. weitere Event-Pässe). Ein einzelner Klick auf z. B. „🔄 Besatzung re-syncen" oder „➕ Lobby-ID generieren" konnte dadurch die Aktion 2–3× auslösen.

#### Die Lösung
- Klick-Erkennung nutzt jetzt `Event.current.type == EventType.MouseUp && Event.current.button == 0`, was pro echtem Maus-Event nur einmal zutrifft, plus `Event.current.Use()` zum Konsumieren des Events.

---

### 3.9 🖨️ Lokaler Drucker-Aufruf warf TargetParameterCountException

#### Problem-Analyse (Root Cause)
`PunchcardSpawner.EnsureGuestFireMissionCard()` (BepInEx) rief `_printMethod.Invoke(printer, null)` auf — also mit 0 Argumenten. Die real aufgelöste Methode `FireMissionCardPrinter.HandleCalculationSuccess` erwartet aber 4 Parameter (`float elevationDegrees, float clampedRange, int powderCharge, bool wasClamped`), bestätigt durch die bereits funktionierende Harmony-Postfix-Signatur in `CoopPunchcardFix.OnPrinterCalculate_Postfix`. Der Aufruf warf daher immer eine `TargetParameterCountException`, die vom umgebenden `catch` still verschluckt wurde — der lokale Drucker-Pfad hat dadurch nie tatsächlich eine Karte gedruckt.

#### Die Lösung
- Neue Hilfsmethode `BuildPrintMethodArgs(MethodInfo)` liefert für `HandleCalculationSuccess` die 4 echten Werte aus `PunchcardSpawner.CurrentMission` (Elevation, Distanz, Ladungen, `wasClamped = false`); für unbekannte Fallback-Methoden (`PrintCard`/`DispenseCard`) werden typgerechte Default-Werte je Parameter erzeugt statt einer garantiert falschen Argumentanzahl.

---

### 3.10 📡 Teleprinter-Parser rechnete Phantom-Ziel bei Regex-Fehltreffer

#### Problem-Analyse (Root Cause)
`OnTeleprinterSubmitLines_Prefix` (beide Hosts) parste Distanz/Azimut per Regex aus freiem Funkspruch-Text. Schlug die Distanz-Regex fehl (andere Formulierung, Lokalisierung, neuer Missionstyp), fiel der Code stillschweigend auf einen festen Default (1200 m, 0°) zurück und berechnete darauf eine „echte" Feuerleitlösung für ein nicht existierendes Ziel — ohne jede Fehlermeldung.

#### Die Lösung
- Ohne erkannte Distanz wird jetzt **kein** Missionsdatensatz mehr erzeugt; stattdessen loggt der Handler eine Warnung mit dem nicht-erkannten Funkspruch-Text und überspringt den Vorgang.

---

### 3.11 📡 TurretTelemetry lieferte stale Daten nach Missionsende (Unity „Fake Null")

#### Problem-Analyse (Root Cause)
`_cachedTurretInstance` war als `object` typisiert; `_cachedTurretInstance == null` prüfte daher reine Referenzgleichheit. IL2CPP/Unity-Objekte überladen `==` für den „Fake Null"-Zustand zerstörter Objekte (z. B. nach Missionsende oder Szenenwechsel) — dieser wurde durch die reine `object`-Referenzprüfung nicht erkannt. Property-Zugriffe auf ein bereits zerstörtes natives Objekt lieferten dadurch teils stale/falsche Werte statt einer Exception, wodurch der Cache nicht zuverlässig neu befüllt wurde.

#### Die Lösung
- Neue Hilfsmethode `IsUnityDestroyed(object)`: castet auf `UnityEngine.Object` und nutzt dessen überladenen `==`-Operator, sofern zutreffend, sonst normale Null-Prüfung. Ersetzt beide `_cachedTurretInstance == null`-Vergleiche in `Update()`.

---

### 3.12 🛡️ MelonLoader-Build brach mit CS0400 auf `EntityLocation`/`MinimalVolumeCulling`

#### Problem-Analyse (Root Cause)
`EnemyDespawnGuard.cs` referenzierte im MelonLoader-Host `EntityLocation`/`MinimalVolumeCulling.CullTarget` direkt als C#-Compile-Zeit-Typen (`__instance is global::EntityLocation el`). Die von MelonLoaders Il2CppAssemblyGenerator (Cpp2IL) erzeugten Stub-Assemblies ließen sich für diese konkreten Typen nicht zuverlässig direkt referenzieren — der Compiler brach mit `CS0400` ab, obwohl die Typen laut `all_types.txt` im Spiel existieren. Der BepInEx-Host hatte exakt dasselbe Problem nie, weil seine Version von `EnemyDespawnGuard.cs` von Anfang an ausschließlich über Reflection (Typname als String) arbeitet.

#### Die Lösung
- MelonLoaders `EnemyDespawnGuard.cs` wurde 1:1 auf denselben Reflection-Ansatz wie der BepInEx-Host umgestellt: `Type.GetType("EntityLocation, Assembly-CSharp")` statt direkter Typreferenz, Zugriff auf Felder/Properties (`Entity`, `IsAlive`, `VisualRoot`, `VisibilityGroup.alpha`, `StartWithVisualRootHidden`, `neverCull`) über gecachte `PropertyInfo`-Objekte statt Compile-Zeit-Member-Zugriff.
- Dadurch bauen jetzt **beide Hosts identisch robust**, unabhängig davon, ob der jeweilige Interop-Generator diese speziellen Typen für direkte Referenzierung sauber exponiert.

---

## 4. Offizielle GUI-Vorlage: 1:1 Unity IMGUI-Implementierung

Das Interface wurde pixelgenau nach der modernen Anthropic / Dieselpunk Design-Vorlage umgesetzt:

| Element | Farbwert / Token | Funktion |
| :--- | :--- | :--- |
| **Master Container** | `#18181B` (98% Opazität) | 520px breites Hauptfenster mit 1px `#27272A` Rand |
| **Terracotta Accent** | `#D97757` (Hover: `#CC785C`) | Primär-Aktionen (Kopieren, Erstellen, Speichern, Tabs) |
| **Card Surface** | `#1F1E1D` / `#27272A` | Hex-Code Box, Besatzungs-Karten, Avatar-Badges |
| **Dashed Empty Slot** | `#1C1C1F` mit Rahmen | Platzhalter `Freier Platz an Rohr X` |
| **Status Pill** | `#1F1E1D` mit `#10B981` Dot | Anzeige `Lobby offen` / `Keine Lobby` oben rechts |
| **Besatzungs-Karten** | Initialen-Badge + Rolle | z. B. `[HM] Du (Operator)` · `👑 Kommandant · 28 ms` |
| **Re-Sync Leiste** | Outline Button | `🔄 Besatzung re-syncen` + `Sync vor X s` |
| **Footer Status Bar** | `#141416` (34px) | Status-Dot, `Relay Steam P2P`, 2px Ladebalken, Timestamp |

---

## 5. Build- & Deployment-Anleitung

### 5.1 Dual-Loader Deployment (1-Klick)
Doppelklicke auf [`Build-And-Deploy.bat`](file:///c:/Users/07785/Documents/PROGRAMMIEREN/IronXNestCommand/Build-And-Deploy.bat).
Das Skript kompiliert alle Projekte und installiert die DLLs automatisch an beiden Zielorten:
- `Mods\IronXNestCommand.dll` (MelonLoader)
- `BepInEx\plugins\IronXNestCommand.dll` (BepInEx)

---

## 6. Tastenkombinationen & Steuerung

- **`F8`**: Öffnet / Schließt das IronXNestCommand Overlay (Hotkey im Menü frei belegbar: F7 bis F12).
- **`Kopieren`**: Kopiert die 12-stellige Hex-Lobby-ID direkt in die Zwischenablage.
- **`Einladen`**: Öffnet das native Steam-Overlay zur Freundeseinladung.
- **`🔄 Besatzung re-syncen`**: Erzwingt sofortigen Lochkarten- und Zieldatenabgleich auf allen Gast-Cockpits.
