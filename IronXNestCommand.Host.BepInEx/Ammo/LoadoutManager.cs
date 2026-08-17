using System.Collections.Generic;
using IronXNestCommand.Core.Logging;
using IronXNestCommand.Core.Paths;
using IronXNestCommand.Host.BepInEx.Core;

namespace IronXNestCommand.Host.BepInEx.Ammo
{
    public class LoadoutItem
    {
        public string ShellId { get; set; }
        public int Quantity { get; set; }
        public int DefaultPowderCharges { get; set; }
    }

    public class LoadoutPreset
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public List<LoadoutItem> Items { get; set; } = new();
    }

    public class LoadoutData
    {
        public string ActivePresetId { get; set; } = "preset_balanced";
        public List<LoadoutPreset> Presets { get; set; } = new();
    }

    public static class LoadoutManager
    {
        private static LoadoutData _data;

        public static void Initialize()
        {
            _data = SaveManager.LoadJson(ModPaths.LoadoutsFile, GetDefaultPresets());
            if (_data.Presets.Count == 0)
            {
                _data = GetDefaultPresets();
                SaveManager.SaveJson(ModPaths.LoadoutsFile, _data);
            }
            ModLogger.Info($"[LoadoutManager] {_data.Presets.Count} Munitions-Presets geladen.");
        }

        public static LoadoutPreset GetActivePreset()
        {
            if (_data == null || _data.Presets.Count == 0) return null;
            return _data.Presets.Find(p => p.Id == _data.ActivePresetId) ?? _data.Presets[0];
        }

        public static void SetActivePreset(string id)
        {
            if (_data != null && _data.Presets.Exists(p => p.Id == id))
            {
                _data.ActivePresetId = id;
                SaveManager.SaveJson(ModPaths.LoadoutsFile, _data);
            }
        }

        public static IEnumerable<LoadoutPreset> GetAllPresets() => _data?.Presets ?? new List<LoadoutPreset>();

        private static LoadoutData GetDefaultPresets()
        {
            return new LoadoutData
            {
                ActivePresetId = "preset_balanced",
                Presets = new List<LoadoutPreset>
                {
                    new LoadoutPreset
                    {
                        Id = "preset_balanced",
                        Name = "Standard Gefechts-Ausstattung",
                        Description = "Ausgewogene Mischung aus Spreng- und Panzerbrech-Granaten.",
                        Items = new List<LoadoutItem>
                        {
                            new LoadoutItem { ShellId = "standard_he", Quantity = 20, DefaultPowderCharges = 2 },
                            new LoadoutItem { ShellId = "shell_ap_hv", Quantity = 10, DefaultPowderCharges = 3 },
                            new LoadoutItem { ShellId = "shell_emp_mk1", Quantity = 2, DefaultPowderCharges = 2 }
                        }
                    },
                    new LoadoutPreset
                    {
                        Id = "preset_anti_armor",
                        Name = "Schwerer Panzerjäger",
                        Description = "Fokus auf bunkerbrechende und panzerbrechende Munition.",
                        Items = new List<LoadoutItem>
                        {
                            new LoadoutItem { ShellId = "shell_ap_hv", Quantity = 25, DefaultPowderCharges = 3 },
                            new LoadoutItem { ShellId = "standard_he", Quantity = 8, DefaultPowderCharges = 1 },
                            new LoadoutItem { ShellId = "shell_emp_mk1", Quantity = 4, DefaultPowderCharges = 2 }
                        }
                    }
                }
            };
        }
    }
}
