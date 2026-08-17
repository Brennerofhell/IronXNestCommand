using System;
using System.Reflection;
using HarmonyLib;
using MelonLoader;
using IronXNestCommand.Ammo;
using IronXNestCommand.Core;
using IronXNestCommand.Economy;
using IronXNestCommand.Progression;

namespace IronXNestCommand.Patches
{
    /// <summary>
    /// Reale Harmony-Patches, die sich in die tatsächlichen Spielklassen von Iron Nest einklinken.
    /// Entdeckte Klassen: Event_OnMissionCompleted, Event_ShellLanded, State_AddRequisitionPoints, CylinderShellSelector.
    /// </summary>
    public static class GameEventsPatch
    {
        public static void InitializePatches(HarmonyLib.Harmony harmony)
        {
            try
            {
                PatchMissionCompleted(harmony);
                PatchShellLanded(harmony);
                PatchRequisition(harmony);
                MelonLogger.Msg("[GameEventsPatch] Reale Spiel-Hooks erfolgreich scharfgeschaltet!");
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"[GameEventsPatch] Hinweis bei Spiel-Hooks Initialisierung: {ex.Message}");
            }
        }

        private static void PatchMissionCompleted(HarmonyLib.Harmony harmony)
        {
            // Sucht nach der Spielklasse Event_OnMissionCompleted
            var targetType = AccessTools.TypeByName("Event_OnMissionCompleted")
                          ?? AccessTools.TypeByName("IronNest.Event_OnMissionCompleted");

            if (targetType != null)
            {
                var methods = targetType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                foreach (var method in methods)
                {
                    if (method.Name == "Execute" || method.Name == "Invoke" || method.Name == "OnMissionCompleted")
                    {
                        var postfix = typeof(GameEventsPatch).GetMethod(nameof(OnMissionCompletedPostfix), BindingFlags.Static | BindingFlags.NonPublic);
                        harmony.Patch(method, postfix: new HarmonyMethod(postfix));
                        MelonLogger.Msg($"[GameEventsPatch] Hook auf Mission-Completed ({targetType.Name}.{method.Name}) gesetzt.");
                        break;
                    }
                }
            }
        }

        private static void PatchShellLanded(HarmonyLib.Harmony harmony)
        {
            var targetType = AccessTools.TypeByName("Event_ShellLanded")
                          ?? AccessTools.TypeByName("IronNest.Event_ShellLanded");

            if (targetType != null)
            {
                var methods = targetType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                foreach (var method in methods)
                {
                    if (method.Name == "Execute" || method.Name == "Invoke" || method.Name == "OnShellLanded")
                    {
                        var postfix = typeof(GameEventsPatch).GetMethod(nameof(OnShellLandedPostfix), BindingFlags.Static | BindingFlags.NonPublic);
                        harmony.Patch(method, postfix: new HarmonyMethod(postfix));
                        MelonLogger.Msg($"[GameEventsPatch] Hook auf Shell-Landed ({targetType.Name}.{method.Name}) gesetzt.");
                        break;
                    }
                }
            }
        }

        private static void PatchRequisition(HarmonyLib.Harmony harmony)
        {
            var targetType = AccessTools.TypeByName("State_AddRequisitionPoints")
                          ?? AccessTools.TypeByName("IronNest.State_AddRequisitionPoints");

            if (targetType != null)
            {
                var methods = targetType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                foreach (var method in methods)
                {
                    if (method.Name == "Execute" || method.Name == "Apply")
                    {
                        var postfix = typeof(GameEventsPatch).GetMethod(nameof(OnAddRequisitionPostfix), BindingFlags.Static | BindingFlags.NonPublic);
                        harmony.Patch(method, postfix: new HarmonyMethod(postfix));
                        MelonLogger.Msg($"[GameEventsPatch] Hook auf Requisition ({targetType.Name}.{method.Name}) gesetzt.");
                        break;
                    }
                }
            }
        }

        // ==================== POSTFIX HANDLER ====================

        private static void OnMissionCompletedPostfix()
        {
            MelonLogger.Msg("[GameEvents] In-Game Missionsabschluss registriert! Belohne Operator...");
            IronXNestCommand.Progression.ProgressionManager.RecordMissionFinished(true, 10, 8, 1);
        }

        private static void OnShellLandedPostfix()
        {
            MelonLogger.Msg("[GameEvents] Shell-Einschlag registriert.");
            // Belohnt präzises Schießen
            IronXNestCommand.Progression.ProgressionManager.AddXP(25, "Shell Treffer");
            CurrencyManager.AddCurrency(CurrencyType.IntelPoints, 2);
        }

        private static void OnAddRequisitionPostfix()
        {
            MelonLogger.Msg("[GameEvents] Requisition Points im Spiel verdient.");
            CurrencyManager.AddCurrency(CurrencyType.LogisticsTokens, 1);
        }
    }
}
