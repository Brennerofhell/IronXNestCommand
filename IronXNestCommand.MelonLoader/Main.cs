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
            // Wenn eine andere Co-op Mod installiert ist, gehen wir auf Nummer sicher und aktivieren den Multiplayer-Sicherheitsmodus standardmäßig.
            if (ModCompatibility.OtherCoopModDetected)
            {
                MelonLogger.Msg("=== Co-op Mod erkannt! Modus: Multiplayer (Sicher) ===");
                FairnessGuard.SetMultiplayerState(true);
            }
            else
            {
                FairnessGuard.SetMultiplayerState(false);
            }

            // 4. Custom Shells initialisieren
            IronXNestCommand.Ammo.CustomShellManager.Initialize();

            // 5. Währungssystem initialisieren
            IronXNestCommand.Economy.CurrencyManager.Initialize();

            MelonLogger.Msg("=== IronXNestCommand erfolgreich geladen ===");
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            // Fallback: Erkennen, ob eine Multiplayer-Szene geladen wurde.
            // TODO: Passe "CoopMap" an den echten Szenennamen an, falls das Spiel spezielle Maps dafür nutzt.
            if (sceneName.ToLower().Contains("coop") || sceneName.ToLower().Contains("multiplayer"))
            {
                MelonLogger.Msg($"[Main] Multiplayer-Szene ({sceneName}) geladen. Aktiviere FairnessGuard.");
                FairnessGuard.SetMultiplayerState(true);
            }
            else if (sceneName.ToLower().Contains("mainmenu"))
            {
                // Zurück im Hauptmenü, wir setzen alles zurück auf Singleplayer (sicherer Zustand)
                FairnessGuard.SetMultiplayerState(false);
            }
        }
    }
}
