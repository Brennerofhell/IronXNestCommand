using System;
using System.Collections.Generic;
using MelonLoader;
using IronXNestCommand.Core;

namespace IronXNestCommand.Ammo
{
    public static class LoadoutManager
    {
        public static LoadoutStore Store { get; private set; }

        public static void Initialize()
        {
            MelonLogger.Msg("[LoadoutManager] Initialisiere Loadout-System...");
            Store = SaveManager.LoadLoadouts();

            if (Store.Presets.Count == 0)
            {
                CreateDefaultPresets();
                SaveManager.SaveLoadouts(Store);
            }

            MelonLogger.Msg($"[LoadoutManager] {Store.Presets.Count} Loadout-Presets geladen.");
        }

        private static void CreateDefaultPresets()
        {
            var defaultPreset = new LoadoutPreset
            {
                Id = "preset_balanced",
                Name = "Allrounder-Paket",
                Description = "Ausgewogene Mischung aus Standard-HE und panzerbrechenden Shells.",
                Items = new List<LoadoutItem>
                {
                    new LoadoutItem { ShellId = "standard_he", Quantity = 20, DefaultPowderCharges = 2 },
                    new LoadoutItem { ShellId = "shell_ap_hv", Quantity = 10, DefaultPowderCharges = 3 }
                }
            };

            var siegePreset = new LoadoutPreset
            {
                Id = "preset_anti_armor",
                Name = "Bunkerbrecher / Anti-Armor",
                Description = "Maximale Durchschlagskraft und EMP gegen befestigte Stellungen.",
                Items = new List<LoadoutItem>
                {
                    new LoadoutItem { ShellId = "shell_ap_hv", Quantity = 25, DefaultPowderCharges = 3 },
                    new LoadoutItem { ShellId = "shell_emp_mk1", Quantity = 5, DefaultPowderCharges = 2 }
                }
            };

            Store.Presets.Add(defaultPreset);
            Store.Presets.Add(siegePreset);
            Store.ActivePresetId = defaultPreset.Id;
        }

        public static void AddPreset(LoadoutPreset preset)
        {
            Store.Presets.Add(preset);
            SaveManager.SaveLoadouts(Store);
            MelonLogger.Msg($"[LoadoutManager] Neues Preset '{preset.Name}' gespeichert.");
        }

        public static void SetActivePreset(string presetId)
        {
            var p = Store.Presets.Find(x => x.Id == presetId);
            if (p != null)
            {
                Store.ActivePresetId = presetId;
                SaveManager.SaveLoadouts(Store);
                MelonLogger.Msg($"[LoadoutManager] Aktives Preset geändert zu: {p.Name}");
            }
        }

        public static LoadoutPreset GetActivePreset()
        {
            return Store.Presets.Find(x => x.Id == Store.ActivePresetId) ?? (Store.Presets.Count > 0 ? Store.Presets[0] : null);
        }
    }
}
