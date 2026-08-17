using System;
using System.IO;
using System.Text.Json;
using MelonLoader;
using MelonLoader.Utils;
using IronXNestCommand.Ammo;
using IronXNestCommand.Economy;
using IronXNestCommand.Progression;

namespace IronXNestCommand.Core
{
    public static class SaveManager
    {
        public static string ModDataDirectory { get; private set; } = string.Empty;

        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        public static void Initialize()
        {
            ModDataDirectory = Path.Combine(MelonEnvironment.UserDataDirectory, "IronXNestCommand");

            if (!Directory.Exists(ModDataDirectory))
            {
                Directory.CreateDirectory(ModDataDirectory);
                MelonLogger.Msg($"[SaveManager] Verzeichnis erstellt: {ModDataDirectory}");
            }
        }

        // ==================== CONFIG ====================
        public static void SaveConfig(ModConfig config)
        {
            SaveJson("config.json", config);
        }

        public static ModConfig LoadConfig()
        {
            return LoadJson<ModConfig>("config.json") ?? new ModConfig();
        }

        // ==================== ECONOMY ====================
        public static void SaveCurrencyData(CurrencyData data)
        {
            SaveJson("currency_data.json", data);
        }

        public static CurrencyData LoadCurrencyData()
        {
            return LoadJson<CurrencyData>("currency_data.json") ?? new CurrencyData();
        }

        // ==================== PROGRESSION ====================
        public static void SaveProgressionData(ProgressionData data)
        {
            SaveJson("player_progress.json", data);
        }

        public static ProgressionData LoadProgressionData()
        {
            return LoadJson<ProgressionData>("player_progress.json") ?? new ProgressionData();
        }

        // ==================== LOADOUTS ====================
        public static void SaveLoadouts(LoadoutStore store)
        {
            SaveJson("loadouts.json", store);
        }

        public static LoadoutStore LoadLoadouts()
        {
            return LoadJson<LoadoutStore>("loadouts.json") ?? new LoadoutStore();
        }

        // ==================== GENERIC HELPERS ====================
        private static void SaveJson<T>(string filename, T data)
        {
            try
            {
                string path = Path.Combine(ModDataDirectory, filename);
                string json = JsonSerializer.Serialize(data, JsonOpts);
                File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[SaveManager] Fehler beim Speichern von {filename}: {ex.Message}");
            }
        }

        private static T LoadJson<T>(string filename) where T : class
        {
            string path = Path.Combine(ModDataDirectory, filename);
            if (!File.Exists(path))
                return null;

            try
            {
                string json = File.ReadAllText(path);
                return JsonSerializer.Deserialize<T>(json, JsonOpts);
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[SaveManager] Fehler beim Laden von {filename}: {ex.Message}");
                return null;
            }
        }
    }
}
