# IronXNestCommand – Vollständiger Chat-Export

**Datum:** 17. August 2026  
**Thema:** Konzeption und Vorbereitung einer eigenen Mod für *Iron Nest: Heavy Turret Simulator*

---

## 1. Ausgangspunkt

**User:** eigene iron nest turret simulator mod konzipieren

Es wurde eine eigene Mod für Iron Nest: Heavy Turret Simulator konzipiert.

### Erste Feature-Ideen
- Smart Fire Control System
- Enhanced Tactical Map & Spotting
- Munitions- & Logistik-Management
- Operator HUD & QoL
- Challenge- & Replay-Features

---

## 2. Erweiterungen durch den User

| User-Eingabe              | Bedeutung / Entscheidung                          |
|---------------------------|---------------------------------------------------|
| mehr muniton              | Fokus auf Munitions-System verstärken             |
| besseres währung und lvl system | Eigenes Währungs- + Rank-System              |
| wie umsetzen              | Technische Umsetzung (MelonLoader, Architektur)   |
| soll mit stemaworks kompatibele sein | Steamworks-Kompatibilität gefordert      |
| was ist das mit steamcloud | Erklärung von Steam Cloud und Save-Strategie         |
| bleepin                   | Nachfrage zu BepInEx                              |
| füge zu dem plan hinzu    | BepInEx in den Gesamtplan aufnehmen               |
| wieso melonloader         | Begründung für MelonLoader als primären Loader    |
| erstelle .md              | Markdown-Plan-Datei erstellen                     |
| name → IronXNestCommand   | Endgültiger Mod-Name festgelegt                   |
| passe md an               | Plan-Datei auf IronXNestCommand aktualisiert      |
| wei nutzze melonloader    | Installations- und Nutzungsanleitung              |
| multilpayer mod für melonloader | Übersicht der Co-op-Mods                     |
| dürfen wir die forken     | Lizenzprüfung der Co-op-Mods                      |
| ohen fragen               | Fork ohne Nachfrage beim Autor                    |
| neine meine mod soll ja multio player können | Mod soll selbst Multiplayer können |
| was ist agpl              | Erklärung der AGPL-Lizenz                         |
| ok gib instruktione für gorok build | Build-Anleitung                              |
| eine prompt               | Fertiger Prompt für die Weiterentwicklung         |
| sind auch fork anweisungne drin | Prompt um Fork-Anweisungen ergänzt           |

---

## 3. Endgültiger Mod-Name

**IronXNestCommand**

---

## 4. Kern-Features (finaler Stand)

### A. Munitions-System
- Ammo Advisor (Zieltyp → empfohlene Shell + Charge)
- Loadout Presets
- Live-Inventar + Verbrauchsprognose
- Auto-Kauf-Regeln (optional)

### B. Währungssystem
- Requisition Credits (Spiel-Währung)
- Intel Points
- Logistics Tokens
- Command Favor

### C. Rank- / Progression-System
- Operator-Ränge (Recruit → High Command Liaison)
- XP durch Genauigkeit, Effizienz, Counter-Battery, Missionen
- Freischaltungen und dezente Boni

### D. Multiplayer
- User will, dass die Mod **selbst Multiplayer kann**
- Zwei mögliche Wege:
  1. Eigene Lösung / starke Kompatibilität zu bestehenden Co-op-Mods
  2. Fork von **Open Nest Co-op** (AGPL-3.0) und Features integrieren

### E. Steamworks & Steam Cloud
- Multiplayer-Erkennung
- Im Multiplayer keine unfairen Boni
- Eigene Daten nur unter `UserData/IronXNestCommand/`
- Keine Manipulation von Leaderboards/Achievements

---

## 5. Technische Entscheidungen

| Punkt              | Entscheidung                              |
|--------------------|-------------------------------------------|
| Primärer Loader    | MelonLoader 0.7.3 (IL2CPP)                |
| Optionaler Loader  | BepInEx 6 (später)                        |
| Sprache            | C# / .NET 6                               |
| Patching           | Harmony                                   |
| Save-Pfad          | `UserData/IronXNestCommand/`              |

### Architektur
```
IronXNestCommand/
├── IronXNestCommand.Host
├── IronXNestCommand.Core
├── IronXNestCommand.Ammo
├── IronXNestCommand.Economy
├── IronXNestCommand.Progression
├── IronXNestCommand.Steam
└── IronXNestCommand.UI
```

---

## 6. Co-op-Mods Übersicht

| Mod                  | Loader              | Lizenz     | Fork möglich?     |
|----------------------|---------------------|------------|-------------------|
| Open Nest Co-op      | MelonLoader + BepInEx | AGPL-3.0 | Ja (Source offen) |
| Synchrony            | MelonLoader         | Privat     | Nein              |
| Iron Nest Co-op      | hauptsächlich BepInEx | Unklar   | Besser fragen     |

**Open Nest Co-op Repo:**  
https://github.com/1499501762/OPEN_NEST_CO-OP

---

## 7. Lizenz-Hinweis AGPL

- Code darf genommen und geändert werden
- Bei Veröffentlichung muss der **komplette Source-Code unter AGPL-3.0** freigegeben werden

---

## 8. Erstellte Dateien

- `IronNest-Mod-Plan.md` → später umbenannt/aktualisiert auf IronXNestCommand
- Speicherort: `/home/workdir/artifacts/IronNest-Mod-Plan.md`

---

## 9. Prompt (Version mit Fork-Unterstützung)

```
Du hilfst mir bei der Entwicklung der MelonLoader-Mod "IronXNestCommand" für "Iron Nest: Heavy Turret Simulator".

### Ziel
Eine Operator-Assistenz-Mod mit Munitions-System, erweitertem Währungssystem, Rank-System und Multiplayer-Unterstützung.

### Gewählter Weg: Fork von Open Nest Co-op
- Basis: Open Nest Co-op (AGPL-3.0)
- Repository: https://github.com/1499501762/OPEN_NEST_CO-OP
- Ich will die bestehende Multiplayer-Funktionalität behalten und meine Features (Rank, Economy, Ammo Advisor, Loadouts) integrieren.

### Wichtige Lizenz-Hinweis
Open Nest Co-op steht unter AGPL-3.0. Alle Änderungen und die veröffentlichte Version müssen ebenfalls unter AGPL-3.0 stehen und der Source-Code muss freigegeben werden.

### Technische Vorgaben
- MelonLoader 0.7.3 (IL2CPP)
- C# / .NET 6
- Harmony
- Eigene Module sauber in die bestehende Struktur einbauen
- Eigene Daten unter UserData/IronXNestCommand/ speichern

### Aktueller Stand
MelonLoader ist installiert. Noch kein eigener Code.

Bitte [HIER EINFÜGEN WAS DU JETZT WILLST]
```

---

## 10. Nächste offene Punkte

- [ ] Endgültige Entscheidung: Eigene Multiplayer-Lösung oder Fork von Open Nest Co-op
- [ ] Genaue Rank-Tabelle mit XP und Freischaltungen
- [ ] Konkrete Boni pro Rank
- [ ] UI-Stil (IMGUI zuerst oder aufwändiger)
- [ ] Projekt grundlegend aufsetzen (Skeleton)

---

*Ende des Chat-Exports – 17. August 2026*
