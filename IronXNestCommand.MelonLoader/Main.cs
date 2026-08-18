using MelonLoader;
using IronXNestCommand.Ammo;
using IronXNestCommand.Core;
using IronXNestCommand.Economy;
using IronXNestCommand.Progression;
using IronXNestCommand.Steam;
using IronXNestCommand.UI;

[assembly: MelonInfo(typeof(IronXNestCommand.Main), "IronXNestCommand", "0.1.4", "Brennerofhell")]
[assembly: MelonGame(null, null)] // Gilt für Iron Nest

namespace IronXNestCommand
{
    public class Main : MelonMod
    {
        public static ModConfig Config { get; private set; }

        public override void OnInitializeMelon()
        {
            MelonLogger.Msg("==================================================");
            MelonLogger.Msg("      IRON X NEST COMMAND - INITIALISIERUNG       ");
            MelonLogger.Msg("==================================================");

            // 1. Sicheres Speicherverzeichnis & Config laden
            SaveManager.Initialize();
            Config = SaveManager.LoadConfig();

            // 2. Mod-Kompatibilitätsprüfung
            ModCompatibility.CheckForOtherMods();

            // 3. Fairness-Status initialisieren
            if (ModCompatibility.OtherCoopModDetected && Config.DisableInMultiplayer)
            {
                MelonLogger.Msg("=== Co-op Mod erkannt! Modus: Multiplayer (Sicher) ===");
                FairnessGuard.SetMultiplayerState(true);
            }
            else
            {
                FairnessGuard.SetMultiplayerState(false);
            }

            // 4. Munition & Custom Shells initialisieren
            CustomShellManager.Initialize();

            // 5. Währungssystem initialisieren
            CurrencyManager.Initialize();

            // 6. Rang- & Erfahrungssystem initialisieren
            IronXNestCommand.Progression.ProgressionManager.Initialize();

            // 7. Loadout-System & Notizen initialisieren
            LoadoutManager.Initialize();
            IronXNestCommand.Notes.TargetNotesManager.Initialize();
            TurretTelemetry.Initialize();

            // 8. Steamworks Multiplayer-Erkennung initialisieren
            SteamworksDetector.Initialize();
            Patches.MultiplayerPatches.TryApplyDynamicPatches(HarmonyInstance);
            Patches.CoopPunchcardFix.InitializePatches(HarmonyInstance);
            Patches.EnemyDespawnGuard.InitializePatches(HarmonyInstance);

            // 9. GUI Overlay initialisieren
            CommandOverlay.Initialize(Config);

            // 10. Reale Spiel-Hooks (Missionen, Shells, Requisition) scharfschalten
            IronXNestCommand.Patches.GameEventsPatch.InitializePatches(HarmonyInstance);
            IronXNestCommand.Patches.AmmoInjectionPatch.InitializePatches(HarmonyInstance);

            MelonLogger.Msg("==================================================");
            MelonLogger.Msg($"[IronXNestCommand] Erfolgreich geladen! Drücke [{Config.ToggleKey}] für das Overlay.");
            MelonLogger.Msg("==================================================");
        }

        public override void OnUpdate()
        {
            // Regelmäßige Prüfung der Steam-Lobby & Telemetrie
            SteamworksDetector.Update(0.1f);
            TurretTelemetry.Update();

            // Overlay-Hotkey abfangen
            CommandOverlay.Update();
        }

        public override void OnGUI()
        {
            // Rendert das interaktive Dieselpunk-Overlay
            CommandOverlay.OnGUI();
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            // Szenen-Erkennung für Co-op Maps
            if (sceneName.ToLower().Contains("coop") || sceneName.ToLower().Contains("multiplayer"))
            {
                MelonLogger.Msg($"[Main] Multiplayer-Szene ({sceneName}) geladen. Aktiviere FairnessGuard.");
                FairnessGuard.SetMultiplayerState(true);
            }
            else if (sceneName.ToLower().Contains("mainmenu"))
            {
                FairnessGuard.SetMultiplayerState(false);
            }
        }
    }
}
