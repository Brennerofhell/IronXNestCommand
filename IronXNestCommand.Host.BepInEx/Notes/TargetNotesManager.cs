using System.Collections.Generic;
using IronXNestCommand.Core.Logging;
using IronXNestCommand.Core.Paths;
using IronXNestCommand.Host.BepInEx.Ammo;
using IronXNestCommand.Host.BepInEx.Core;

namespace IronXNestCommand.Host.BepInEx.Notes
{
    public static class TargetNotesManager
    {
        private static Dictionary<string, string> _notes = new();

        public static void Initialize()
        {
            _notes = SaveManager.LoadJson(ModPaths.NotesFile, GetDefaultNotes());
            if (_notes.Count == 0)
            {
                _notes = GetDefaultNotes();
                SaveManager.SaveJson(ModPaths.NotesFile, _notes);
            }
            ModLogger.Info($"[TargetNotesManager] {_notes.Count} Ziel-Notizen geladen.");
        }

        public static string GetNote(TargetCategory category)
        {
            string key = category.ToString();
            if (_notes.TryGetValue(key, out var note))
                return note;

            return "";
        }

        public static void SetNote(TargetCategory category, string note)
        {
            string key = category.ToString();
            _notes[key] = note ?? "";
            SaveManager.SaveJson(ModPaths.NotesFile, _notes);
        }

        private static Dictionary<string, string> GetDefaultNotes()
        {
            return new Dictionary<string, string>
            {
                { TargetCategory.InfantrySquad.ToString(), "Splitterwirkung optimal bei 1x Treibladung auf mittlere Distanz." },
                { TargetCategory.LightVehicle.ToString(), "2x Ladung einsetzen, leicht vorhalten bei Fahrbewegungen." },
                { TargetCategory.MediumArmor.ToString(), "HV-AP Shell verwenden! Flacher Richtwinkel für maximalen Durchschlag." },
                { TargetCategory.HeavyBunker.ToString(), "4x Ladung, direkter Treffer auf Scharte erforderlich." },
                { TargetCategory.CounterBatteryArtillery.ToString(), "EMP-Erstschlag gefolgt von Schnellfeuer mit Sprenggranaten." },
                { TargetCategory.ElectronicCommandCenter.ToString(), "EMP-Shell legt das Suchradar sofort lahm." }
            };
        }
    }
}
