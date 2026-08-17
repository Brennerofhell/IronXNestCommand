using System;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using IronXNestCommand.Core;
using IronXNestCommand.Core.Config;
using IronXNestCommand.Core.Logging;
using IronXNestCommand.Core.Paths;
using IronXNestCommand.Host.BepInEx.Ammo;
using IronXNestCommand.Host.BepInEx.Core;
using IronXNestCommand.Host.BepInEx.Economy;
using IronXNestCommand.Host.BepInEx.Notes;
using IronXNestCommand.Host.BepInEx.Overlay;
using IronXNestCommand.Host.BepInEx.Patches;
using IronXNestCommand.Host.BepInEx.Progression;
using IronXNestCommand.Host.BepInEx.Steam;

namespace IronXNestCommand.Host.BepInEx
{
    [BepInPlugin(ModInfo.Guid, ModInfo.Name, ModInfo.Version)]
    [BepInDependency(ModInfo.CoopPluginGuid, BepInDependency.DependencyFlags.SoftDependency)]
    public sealed class Plugin : BasePlugin
    {
        internal static Plugin Instance { get; private set; }
        internal Harmony Harmony { get; } = new(ModInfo.Guid);
        internal ModConfig ConfigData { get; private set; } = new();

        public override void Load()
        {
            Instance = this;

            // Logger-Weiterleitung auf BepInEx Logging
            ModLogger.OnLog = msg => Log.LogInfo(msg);
            ModLogger.OnWarning = msg => Log.LogWarning(msg);
            ModLogger.OnError = msg => Log.LogError(msg);

            Log.LogInfo("==================================================");
            Log.LogInfo("  IRON X NEST COMMAND (BepInEx 6 IL2CPP Standalone)");
            Log.LogInfo("==================================================");

            // 1. Pfade & Konfiguration initialisieren
            ModPaths.Initialize(global::BepInEx.Paths.GameRootPath);
            ModPaths.EnsureDirectories();
            SaveManager.Initialize();
            ConfigData = SaveManager.LoadConfig();

            // 2. Mod-Systeme initialisieren
            CustomShellManager.Initialize();
            CurrencyManager.Initialize();
            ProgressionManager.Initialize();
            LoadoutManager.Initialize();
            TargetNotesManager.Initialize();
            TurretTelemetry.Initialize();

            // 3. Steam & Co-op Integration
            SteamworksDetector.Initialize();
            ModCompatibility.CheckForOtherMods();

            // 4. Harmony Patches
            GameEventsPatch.InitializePatches(Harmony);
            MultiplayerPatches.TryApplyDynamicPatches(Harmony);
            AmmoInjectionPatch.InitializePatches(Harmony);
            CoopPunchcardFix.InitializePatches(Harmony);
            EnemyDespawnGuard.InitializePatches(Harmony);

            // 5. Cockpit Overlay MonoBehaviour hinzufügen
            AddComponent<CommandOverlay>();

            Log.LogInfo("==================================================");
            Log.LogInfo($"[IronXNestCommand] Erfolgreich geladen! Drücke [{ConfigData.ToggleKey}] für das Overlay.");
            Log.LogInfo("==================================================");
        }

        public override bool Unload()
        {
            Harmony.UnpatchSelf();
            Instance = null;
            return true;
        }
    }
}
