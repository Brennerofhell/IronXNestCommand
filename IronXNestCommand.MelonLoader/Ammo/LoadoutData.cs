using System.Collections.Generic;

namespace IronXNestCommand.Ammo
{
    public class LoadoutItem
    {
        public string ShellId { get; set; } = string.Empty;
        public int Quantity { get; set; } = 10;
        public int DefaultPowderCharges { get; set; } = 2;
    }

    public class LoadoutPreset
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = "Standard Loadout";
        public string Description { get; set; } = "Ausgewogenes Munitionspaket";
        public List<LoadoutItem> Items { get; set; } = new List<LoadoutItem>();
    }

    public class LoadoutStore
    {
        public List<LoadoutPreset> Presets { get; set; } = new List<LoadoutPreset>();
        public string ActivePresetId { get; set; } = string.Empty;
    }
}
