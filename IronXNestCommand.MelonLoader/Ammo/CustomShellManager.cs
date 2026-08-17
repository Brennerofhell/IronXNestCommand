using System.Collections.Generic;
using MelonLoader;

namespace IronXNestCommand.Ammo
{
    /// <summary>
    /// Verwaltet alle benutzerdefinierten Shells und kümmert sich um die spätere Injizierung ins Spiel.
    /// </summary>
    public static class CustomShellManager
    {
        private static Dictionary<string, ShellDefinition> _customShells = new Dictionary<string, ShellDefinition>();

        public static void Initialize()
        {
            MelonLogger.Msg("[CustomShellManager] Initialisiere Custom Shells...");
            
            // Beispiel: Eine neue EMP-Shell registrieren
            RegisterShell(new ShellDefinition("shell_emp_mk1", "EMP Shell Mk I")
            {
                Description = "Stört elektronische Systeme in einem kleinen Radius. Verursacht keinen physischen Schaden.",
                KineticDamage = 5f,
                ExplosiveDamage = 0f,
                ArmorPenetration = 2f,
                BlastRadius = 15f, // EMP Wirkungsradius
                RequisitionCost = 250,
                CommandFavorCost = 1
            });

            // Beispiel: High-Velocity AP Shell
            RegisterShell(new ShellDefinition("shell_ap_hv", "HV-AP Shell")
            {
                Description = "High-Velocity Armor Piercing. Durchschlägt dicke Panzerung mit Leichtigkeit.",
                KineticDamage = 450f,
                ExplosiveDamage = 10f,
                ArmorPenetration = 150f,
                BlastRadius = 2f,
                RequisitionCost = 400,
                CommandFavorCost = 0
            });
            
            MelonLogger.Msg($"[CustomShellManager] {_customShells.Count} Custom Shell(s) registriert.");
        }

        public static void RegisterShell(ShellDefinition shell)
        {
            if (_customShells.ContainsKey(shell.Id))
            {
                MelonLogger.Warning($"[CustomShellManager] Shell mit der ID {shell.Id} ist bereits registriert!");
                return;
            }
            
            _customShells.Add(shell.Id, shell);
            MelonLogger.Msg($"[CustomShellManager] Shell registriert: {shell.Name}");
        }

        public static IEnumerable<ShellDefinition> GetAllCustomShells()
        {
            return _customShells.Values;
        }
    }
}
