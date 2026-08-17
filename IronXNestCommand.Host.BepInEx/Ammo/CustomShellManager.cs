using System.Collections.Generic;
using IronXNestCommand.Core.Logging;

namespace IronXNestCommand.Host.BepInEx.Ammo
{
    public static class CustomShellManager
    {
        private static readonly Dictionary<string, ShellDefinition> CustomShells = new();

        public static void Initialize()
        {
            RegisterDefaultCustomShells();
            ModLogger.Info($"[CustomShellManager] {CustomShells.Count} Spezial-Geschosse im Arsenal geladen.");
        }

        private static void RegisterDefaultCustomShells()
        {
            RegisterShell(new ShellDefinition("shell_ap_hv", "HV-AP Armor Piercing")
            {
                Description = "High-Velocity Vollkaliber-Geschoss mit Wolframkern zur Zerstörung schwerer Panzerung.",
                KineticDamage = 350f,
                ExplosiveDamage = 30f,
                ArmorPenetration = 220f,
                BlastRadius = 3.5f,
                RequisitionCost = 25,
                CommandFavorCost = 0
            });

            RegisterShell(new ShellDefinition("shell_emp_mk1", "EMP Disruptor Mk I")
            {
                Description = "Erzeugt einen elektro-magnetischen Puls, der Elektronik und Richtantriebe lahmlegt.",
                KineticDamage = 50f,
                ExplosiveDamage = 80f,
                ArmorPenetration = 40f,
                BlastRadius = 25f,
                RequisitionCost = 45,
                CommandFavorCost = 1
            });
        }

        public static void RegisterShell(ShellDefinition shell)
        {
            if (shell != null && !string.IsNullOrEmpty(shell.Id))
            {
                CustomShells[shell.Id] = shell;
            }
        }

        public static ShellDefinition GetShell(string id)
        {
            if (CustomShells.TryGetValue(id, out var shell))
                return shell;

            return null;
        }

        public static IEnumerable<ShellDefinition> GetAllCustomShells() => CustomShells.Values;
    }
}
