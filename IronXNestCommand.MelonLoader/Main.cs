using MelonLoader;
using IronXNestCommand.Core;
using IronXNestCommand.Steam;

[assembly: MelonInfo(typeof(IronXNestCommand.Main), "IronXNestCommand", "1.0.0", "YourName")]
[assembly: MelonGame(null, null)] // Gilt für Iron Nest

namespace IronXNestCommand
{
    public class Main : MelonMod
    {
        public override void OnInitializeMelon()
        {
            MelonLogger.Msg("=== IronXNestCommand wird initialisiert ===");

            // 1. Sicheres Speicherverzeichnis vorbereiten
            SaveManager.Initialize();

            // 2. Mod-Kompatibilitätsprüfung
            ModCompatibility.CheckForOtherMods();

            // 3. Fairness-Status initialisieren
            FairnessGuard.SetMultiplayerState(false);

            // 4. Custom Shells initialisieren
            IronXNestCommand.Ammo.CustomShellManager.Initialize();

            MelonLogger.Msg("=== IronXNestCommand erfolgreich geladen ===");
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            // Hier später: Erkennen, ob eine Multiplayer-Lobby betreten wurde
        }
    }
}
