using System.IO;
using System.Text.Json;
using MelonLoader;
using MelonLoader.Utils;

namespace IronXNestCommand.Core
{
    public static class SaveManager
    {
        public static string ModDataDirectory { get; private set; } = string.Empty;

        public static void Initialize()
        {
            // Pfad: <Spielverzeichnis>/UserData/IronXNestCommand/
            ModDataDirectory = Path.Combine(MelonEnvironment.UserDataDirectory, "IronXNestCommand");

            if (!Directory.Exists(ModDataDirectory))
            {
                Directory.CreateDirectory(ModDataDirectory);
                MelonLogger.Msg($"[SaveManager] Verzeichnis erstellt: {ModDataDirectory}");
            }
        }

        public static void SaveCurrencyData(IronXNestCommand.Economy.CurrencyData data)
        {
            try
            {
                string path = Path.Combine(ModDataDirectory, "player_progress.json");
                string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(path, json);
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error($"[SaveManager] Fehler beim Speichern der Währung: {ex.Message}");
            }
        }

        public static IronXNestCommand.Economy.CurrencyData LoadCurrencyData()
        {
            string path = Path.Combine(ModDataDirectory, "player_progress.json");
            
            if (!File.Exists(path))
            {
                return new IronXNestCommand.Economy.CurrencyData(); // Neues, leeres Profil
            }

            try
            {
                string json = File.ReadAllText(path);
                var data = JsonSerializer.Deserialize<IronXNestCommand.Economy.CurrencyData>(json);
                return data ?? new IronXNestCommand.Economy.CurrencyData();
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error($"[SaveManager] Fehler beim Laden der Währung: {ex.Message}. Erstelle neues Profil.");
                return new IronXNestCommand.Economy.CurrencyData();
            }
        }
    }
}
