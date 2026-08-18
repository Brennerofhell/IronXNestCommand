using System;
using System.Reflection;
using HarmonyLib;
using MelonLoader;
using IronXNestCommand.Core;
using IronXNestCommand.Economy;
using IronXNestCommand.UI;
using ModProgression = IronXNestCommand.Progression.ProgressionManager;

namespace IronXNestCommand.Patches
{
    public static class GameEventsPatch
    {
        public static void InitializePatches(HarmonyLib.Harmony harmony)
        {
            if (harmony == null) return;

            // 1. Hook: Geschützfeuer (Zagreekie.Tools.ArmedFireRelayOneShot.TriggerFire)
            TryPatchMethod(harmony, "Zagreekie.Tools.ArmedFireRelayOneShot, Assembly-CSharp", "TriggerFire",
                postfix: typeof(GameEventsPatch).GetMethod(nameof(OnTriggerFire_Postfix), BindingFlags.Public | BindingFlags.Static));

            // 2. Hook: Einschlag & Treffer (SleepyNodes.Event_ShellLanded.Run)
            TryPatchMethod(harmony, "SleepyNodes.Event_ShellLanded, Assembly-CSharp", "Run",
                postfix: typeof(GameEventsPatch).GetMethod(nameof(OnShellLanded_Postfix), BindingFlags.Public | BindingFlags.Static));

            // 3. Hook: Schaden & Zerstörung (SleepyNodes.State_DamageEntity.OnEnter)
            TryPatchMethod(harmony, "SleepyNodes.State_DamageEntity, Assembly-CSharp", "OnEnter",
                postfix: typeof(GameEventsPatch).GetMethod(nameof(OnDamageEntity_Postfix), BindingFlags.Public | BindingFlags.Static));

            // 4. Hook: Missionssieg (SleepyNodes.Event_OnMissionCompleted.ShouldRun)
            TryPatchMethod(harmony, "SleepyNodes.Event_OnMissionCompleted, Assembly-CSharp", "ShouldRun",
                postfix: typeof(GameEventsPatch).GetMethod(nameof(OnMissionCompleted_Postfix), BindingFlags.Public | BindingFlags.Static));

            // 5. Hook: Missionsniederlage (SleepyNodes.Event_OnMissionFailed.ShouldRun)
            TryPatchMethod(harmony, "SleepyNodes.Event_OnMissionFailed, Assembly-CSharp", "ShouldRun",
                postfix: typeof(GameEventsPatch).GetMethod(nameof(OnMissionFailed_Postfix), BindingFlags.Public | BindingFlags.Static));

            // 6. Hook: Teleprinter HQ Telegramme (Teleprinter.SubmitLines)
            TryPatchMethod(harmony, "Teleprinter, Assembly-CSharp", "SubmitLines",
                prefix: typeof(GameEventsPatch).GetMethod(nameof(OnTeleprinterSubmitLines_Prefix), BindingFlags.Public | BindingFlags.Static));
        }

        private static void TryPatchMethod(HarmonyLib.Harmony harmony, string typeName, string methodName, MethodInfo postfix = null, MethodInfo prefix = null)
        {
            try
            {
                Type type = Type.GetType(typeName);
                if (type == null)
                {
                    foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        type = asm.GetType(typeName.Split(',')[0].Trim());
                        if (type != null) break;
                    }
                }

                if (type == null)
                {
                    MelonLogger.Warning($"[GameEventsPatch] Typ nicht gefunden: {typeName}");
                    return;
                }

                var method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                if (method != null)
                {
                    harmony.Patch(method,
                        prefix: prefix != null ? new HarmonyMethod(prefix) : null,
                        postfix: postfix != null ? new HarmonyMethod(postfix) : null);
                    MelonLogger.Msg($"[GameEventsPatch] ✔ Hook aktiv: {type.Name}.{method.Name}");
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"[GameEventsPatch] Fehler beim Patch von {typeName}.{methodName}: {ex.Message}");
            }
        }

        // ── Event-Handler ────────────────────────────────────────────────────────

        public static void OnTriggerFire_Postfix()
        {
            try
            {
                // Kein SaveManager-Aufruf hier — TriggerFire kann mehrfach pro Sekunde feuern
                // (Dauerfeuer), ein synchroner JSON-Write pro Schuss verursacht Frame-Hitches.
                // ShellsFired wird beim naechsten AddXP/RecordMissionFinished mitgespeichert.
                ModProgression.Data.ShellsFired++;
            }
            catch { }
        }

        public static void OnShellLanded_Postfix()
        {
            try
            {
                ModProgression.Data.DirectHits++;
                ModProgression.AddXP(20, "Treffer auf Ziel");
                CurrencyManager.AddCurrency(CurrencyType.IntelPoints, 5);
                CommandOverlay.ShowNotification("🎯 Treffer bestätigt! +20 XP, +5 Intel");
            }
            catch { }
        }

        public static void OnDamageEntity_Postfix()
        {
            try
            {
                ModProgression.Data.CounterBatteryKills++;
                ModProgression.AddXP(50, "Ziel neutralisiert");
                CurrencyManager.AddCurrency(CurrencyType.LogisticsTokens, 2);
                CommandOverlay.ShowNotification("💥 Ziel zerstört! +50 XP, +2 Tokens");
            }
            catch { }
        }

        // ShouldRun wird von Node-Graph-Zustaenden i.d.R. bei jeder Graph-Auswertung erneut
        // abgefragt (nicht nur einmalig wie das alte Execute) — ohne Flanken-Erkennung wuerde
        // eine einzelne Missions-Fertigstellung XP/Waehrung mehrfach vergeben, solange
        // ShouldRun weiterhin true liefert. Nur beim Uebergang false->true feuern.
        private static bool _missionCompletedFired = false;
        private static bool _missionFailedFired = false;

        public static void OnMissionCompleted_Postfix(bool __result)
        {
            try
            {
                if (__result && !_missionCompletedFired)
                {
                    _missionCompletedFired = true;
                    ModProgression.RecordMissionFinished(true, 1, 1, 0);
                    CommandOverlay.ShowNotification("🏆 MISSION ERFOLGREICH ABGESCHLOSSEN! +250 XP, +10 Tokens");
                    MelonLogger.Msg("[GameEventsPatch] Missionssieg verbucht!");
                }
                else if (!__result)
                {
                    _missionCompletedFired = false;
                }
            }
            catch { }
        }

        public static void OnMissionFailed_Postfix(bool __result)
        {
            try
            {
                if (__result && !_missionFailedFired)
                {
                    _missionFailedFired = true;
                    ModProgression.RecordMissionFinished(false, 1, 0, 0);
                    CommandOverlay.ShowNotification("⚠ Mission gescheitert. +50 Trost-XP");
                    MelonLogger.Msg("[GameEventsPatch] Missionsniederlage verbucht.");
                }
                else if (!__result)
                {
                    _missionFailedFired = false;
                }
            }
            catch { }
        }

        public static void OnTeleprinterSubmitLines_Prefix(string sourceId, object lines)
        {
            try
            {
                if (lines is System.Collections.IEnumerable enumerable)
                {
                    var lineList = new System.Collections.Generic.List<string>();
                    foreach (var item in enumerable)
                    {
                        if (item != null) lineList.Add(item.ToString());
                    }
                    CommandOverlay.ShowNotification("📡 Neues HQ-Telegramm empfangen!");
                    MelonLogger.Msg($"[GameEventsPatch] Teleprinter-Telegramm von {sourceId} abgefangen ({lineList.Count} Zeilen).");
                }
            }
            catch { }
        }
    }
}
