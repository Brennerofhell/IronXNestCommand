using HarmonyLib;
using MelonLoader;
using IronXNestCommand.Ammo;

namespace IronXNestCommand.Patches
{
    /// <summary>
    /// Diese Klasse enthält Harmony-Patches, um unsere Custom Shells in das Spiel zu injizieren.
    /// Da die internen Klassennamen noch unbekannt sind, sind dies Platzhalter (Templates).
    /// </summary>
    public static class AmmoInjectionPatch
    {
        public static void InitializePatches(HarmonyLib.Harmony harmony)
        {
            // Ammo injection hooks
        }

        // TODO: Ersetze 'ItemDatabase' durch den echten Klassennamen des Spiels, der Items/Munition lädt.
        // [HarmonyPatch(typeof(ItemDatabase), "Initialize")] 
        public static class ItemDatabase_Initialize_Patch
        {
            public static void Postfix()
            {
                MelonLogger.Msg("[AmmoInjectionPatch] Spiel-Datenbank geladen. Injiziere Custom Shells...");
                
                foreach (var shell in CustomShellManager.GetAllCustomShells())
                {
                    // TODO: Hier den Code einfügen, um die Shell (shell) in die Spieldatenbank zu laden.
                    // Beispiel: 
                    // var gameShell = new GameShellData(shell.Name, shell.KineticDamage, ...);
                    // ItemDatabase.Add(shell.Id, gameShell);
                    
                    MelonLogger.Msg($"[AmmoInjectionPatch] Shell injiziert: {shell.Name}");
                }
            }
        }
    }
}
