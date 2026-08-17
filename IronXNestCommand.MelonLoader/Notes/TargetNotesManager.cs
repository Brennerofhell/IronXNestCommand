using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using MelonLoader;
using IronXNestCommand.Ammo;

namespace IronXNestCommand.Notes
{
    public static class TargetNotesManager
    {
        private static readonly string NotesFilePath = GetNotesPath();

        private static string GetNotesPath()
        {
            // MelonEnvironment is available in MelonLoader ≥ 0.6; fall back to manual path for older builds.
            try
            {
                var envType = Type.GetType("MelonLoader.MelonEnvironment, MelonLoader");
                if (envType != null)
                {
                    var prop = envType.GetProperty("UserDataDirectory");
                    if (prop != null)
                        return Path.Combine((string)prop.GetValue(null), "IronXNestCommand", "notes.json");
                }
            }
            catch { }

            // Fallback: <GameDir>/UserData/IronXNestCommand/notes.json
            string gameDir = AppDomain.CurrentDomain.BaseDirectory;
            return Path.Combine(gameDir, "UserData", "IronXNestCommand", "notes.json");
        }

        private static Dictionary<string, string> _notes = new();

        public static void Initialize()
        {
            LoadNotes();
        }

        public static string GetNote(TargetCategory category)
        {
            string key = category.ToString();
            return _notes.TryGetValue(key, out var note) ? note : GetDefaultNote(category);
        }

        public static void SetNote(TargetCategory category, string note)
        {
            string key = category.ToString();
            _notes[key] = note ?? "";
            SaveNotes();
        }

        private static void LoadNotes()
        {
            try
            {
                if (File.Exists(NotesFilePath))
                {
                    string json = File.ReadAllText(NotesFilePath);
                    var loaded = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                    if (loaded != null)
                    {
                        _notes = loaded;
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"[TargetNotesManager] Fehler beim Laden von notes.json: {ex.Message}");
            }

            // Standard-Notizen initialisieren
            _notes = new Dictionary<string, string>
            {
                { TargetCategory.InfantrySquad.ToString(), "Streuung beachten. Bei Deckung 1-2 Strich vorhalten." },
                { TargetCategory.LightVehicle.ToString(), "Hohe Geschwindigkeit. Schnelle Zielerfassung nötig." },
                { TargetCategory.MediumArmor.ToString(), "Winkel >45° vermeiden. Auf Ketten oder Turmkranz zielen." },
                { TargetCategory.HeavyBunker.ToString(), "Maximale Ladung verwenden. Mehrere Treffer erforderlich." },
                { TargetCategory.CounterBatteryArtillery.ToString(), "Sofortige Priorität! Flugbahn kalkulieren." },
                { TargetCategory.ElectronicCommandCenter.ToString(), "EMP-Shell verwenden um Radar zu blenden." }
            };
            SaveNotes();
        }

        private static void SaveNotes()
        {
            try
            {
                string dir = Path.GetDirectoryName(NotesFilePath);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                string json = JsonSerializer.Serialize(_notes, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(NotesFilePath, json);
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[TargetNotesManager] Fehler beim Speichern von notes.json: {ex.Message}");
            }
        }

        private static string GetDefaultNote(TargetCategory category)
        {
            return "Keine speziellen Notizen hinterlegt.";
        }
    }
}
