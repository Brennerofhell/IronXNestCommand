using System;
using System.IO;
using System.Text.Json;
using IronXNestCommand.Core.Config;
using IronXNestCommand.Core.Logging;
using IronXNestCommand.Core.Paths;

namespace IronXNestCommand.Host.BepInEx.Core
{
    public static class SaveManager
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        public static void Initialize()
        {
            ModPaths.EnsureDirectories();
            ModLogger.Info($"[SaveManager] Speicherpfad: {ModPaths.DataRoot}");
        }

        public static ModConfig LoadConfig() => ConfigStore.LoadOrCreate();

        public static void SaveConfig(ModConfig config) => ConfigStore.Save(config);

        public static T LoadJson<T>(string filePath, T defaultValue) where T : class, new()
        {
            try
            {
                if (!File.Exists(filePath))
                    return defaultValue ?? new T();

                string json = File.ReadAllText(filePath);
                return JsonSerializer.Deserialize<T>(json, JsonOptions) ?? defaultValue ?? new T();
            }
            catch (Exception ex)
            {
                ModLogger.Warn($"[SaveManager] Fehler beim Laden von {Path.GetFileName(filePath)}: {ex.Message}");
                return defaultValue ?? new T();
            }
        }

        public static void SaveJson<T>(string filePath, T data)
        {
            try
            {
                string dir = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                string json = JsonSerializer.Serialize(data, JsonOptions);
                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                ModLogger.Error($"[SaveManager] Fehler beim Speichern von {Path.GetFileName(filePath)}: {ex.Message}");
            }
        }
    }
}
