# IronXNestCommand – Konzept & Umsetzungsplan

**Mod-Name:** IronXNestCommand  
**Spiel:** Iron Nest: Heavy Turret Simulator  
**Stand:** 17. August 2026  
**Ziel:** Eigene Assistenz-Mod mit erweitertem Munitions-, Währungs- und Rank-System

---

## 1. Ziel der Mod

Eine Operator-Assistenz-Mod, die den Kern des Spiels (haptische Bedienung der Turret) erhält und gleichzeitig spürbaren Komfort sowie Progression hinzufügt.

### Hauptbereiche
- Intelligentes Munitions-System (Advisor + Loadouts)
- Erweitertes Währungssystem
- Rank- / Progression-System
- Volle Steamworks-Kompatibilität
- Sauberer Umgang mit Steam Cloud
- Primär MelonLoader, später optional BepInEx

---

## 2. Feature-Übersicht

### A. Munitions-System
- **Ammo Advisor**: Analysiert Zieltyp und schlägt passende Shell + Powder Charge vor
- **Loadout Presets**: Speichern und laden von Munitions-Zusammenstellungen
- **Live-Inventar**: Übersicht über alle Shell-Typen + Verbrauchsprognose
- **Auto-Kauf-Regeln** (optional): Automatisches Nachbestellen bei niedrigem Bestand
- Vergleichsfunktion und persönliche Notizen pro Shell-Typ

### B. Währungssystem
| Währung              | Beschreibung                          | Verwendung                          |
|----------------------|---------------------------------------|-------------------------------------|
| Requisition Credits  | Bestehende Spiel-Währung              | Standard-Munition, Basis-Käufe      |
| Intel Points         | Aufklärung & Spotting                 | Bessere Karten-Tools, Ziel-Priorisierung |
| Logistics Tokens     | Effizienz & Logistik                  | Loadouts, größere Vorräte, Schnell-Nachschub |
| Command Favor        | Hohe Genauigkeit & saubere Missionen  | Seltene / experimentelle Shells     |

- Dynamic Pricing möglich
- Effizienz-Bonus bei sparsamer Munitionsnutzung
- Eigene Währungen nur im Singleplayer voll aktiv

### C. Rank- / Progression-System
**Beispiel-Ränge:**
- Recruit Operator
- Junior Gunner
- Qualified Operator
- Senior Operator
- Master Gunner
- Nest Commander
- High Command Liaison

**XP-Quellen:**
- Missionsabschluss
- Genauigkeit (First-Hit-Bonus)
- Effizienz (Munitionsverbrauch)
- Counter-Battery-Erfolge
- Spezielle Objectives

**Freischaltungen pro Rank:**
- Mehr Loadout-Slots
- Bessere Advisor-Funktionen
- Erweiterte Karten-Tools
- Passive Boni (dezente Komfort-Verbesserungen)
- Limitierte aktive Fähigkeiten pro Mission

### D. Steamworks-Kompatibilität
- Multiplayer-Erkennung (Co-op-Mods / Steam Lobby)
- Im Multiplayer: Rank/XP weiter zählen, Gameplay-Boni abschalten
- Keine Manipulation von Steam Stats, Achievements oder Leaderboards
- Klare Warnung bei Leaderboard-Nutzung
- Config-Option: `DisableInMultiplayer` (Standard: true)

### E. Steam Cloud & Saves
- Alle eigenen Daten unter `UserData/IronXNestCommand/` speichern
- **Nicht** in die Steam-Cloud-Pfade des Spiels schreiben
- Fortschritt bleibt vorerst lokal (pro PC)
- Später optional eigene Cloud-Sync möglich

---

## 3. Loader-Strategie

| Priorität | Loader                  | Status     | Begründung                                      |
|-----------|-------------------------|------------|-------------------------------------------------|
| 1         | MelonLoader (IL2CPP)    | Primär     | Beste Open-Source-Beispiele (FCS), große Community |
| 2         | BepInEx 6 (IL2CPP)      | Optional   | Spätere maximale Kompatibilität                 |

**Empfehlung:**  
Zuerst komplett auf MelonLoader entwickeln. Architektur so halten, dass die eigentliche Logik loader-unabhängig bleibt → erleichtert spätere Portierung auf BepInEx.

---

## 4. Architektur

```
IronXNestCommand/
├── IronXNestCommand.Host          → MelonMod Entry Point (später auch BepInEx möglich)
├── IronXNestCommand.Core          → Config, Save-System, Interfaces
├── IronXNestCommand.Ammo          → Advisor, Loadouts, Inventar
├── IronXNestCommand.Economy       → Währungen + Requisition-Handling
├── IronXNestCommand.Progression   → Rank, XP, Freischaltungen
├── IronXNestCommand.Steam         → Multiplayer-Erkennung, Steamworks-Checks
└── IronXNestCommand.UI            → Overlay / Panel
```

- Host enthält den loader-spezifischen Code
- Rest der Logik möglichst loader-unabhängig halten

---

## 5. Speicherorte

```
UserData/IronXNestCommand/
├── config.json               → Einstellungen
├── player_progress.json      → Rank, XP, Währungen
├── loadouts.json             → Gespeicherte Presets
└── notes.json                → Persönliche Shell-Notizen (optional)
```

---

## 6. Entwicklungsreihenfolge

| Phase | Inhalt                                      | Ziel                              |
|-------|---------------------------------------------|-----------------------------------|
| 0     | MelonLoader + minimales Overlay             | Grundgerüst läuft                 |
| 1     | Spielsysteme finden (Munition, Requisition, Ziele) | Zugriff auf relevante Daten |
| 2     | Ammo Advisor + Loadouts                     | Erster spürbarer Nutzen           |
| 3     | Eigene Währungen + lokales Save             | Economy steht                     |
| 4     | Rank + XP + Freischaltungen                 | Progression steht                 |
| 5     | Multiplayer-Erkennung + Feature-Abschaltung | Steamworks-sicher                 |
| 6     | Feinschliff + optionale BepInEx-Variante    | Langfristige Kompatibilität       |

---

## 7. Design-Prinzipien

- Haptik und Immersion des Spiels erhalten
- Alles modular und abschaltbar
- Keine Leaderboard-Vorteile im Multiplayer
- Eigene Daten nur unter `UserData/IronXNestCommand/`
- Loader-spezifischen Code isolieren
- Klare Config-Optionen
- Dieselpunk-Ästhetik bei UI anstreben (Teleprinter / militärisches Logbuch)

---

## 8. Wichtige Referenzen

- Bestehende Mods: IronNestFCS / FCS-Smart (Munition kaufen, laden, zielen)
- Loader: MelonLoader (IL2CPP)
- Alternativ: BepInEx 6 IL2CPP
- Co-op-Mods beachten (Synchrony, Open Nest Co-op) wegen Sync von Requisition etc.

---

## 9. Offene Punkte / Nächste Entscheidungen

- [x] Endgültiger Mod-Name → **IronXNestCommand**
- [ ] Genaue Rank-Tabelle mit XP-Anforderungen und Freischaltungen
- [ ] Welche Boni genau pro Rank freigeschaltet werden
- [ ] UI-Stil (einfaches IMGUI zuerst oder direkt aufwändiger)
- [ ] Ob später Steam-Cloud-Sync für eigene Daten gewünscht ist

---

*Dieses Dokument fasst den bisherigen Konzeptionsstand zusammen und dient als Grundlage für die Umsetzung.*
