using System;
using System.Reflection;
using HarmonyLib;
using IronXNestCommand.Core.Logging;
using IronXNestCommand.Host.BepInEx.Core;
using IronXNestCommand.Host.BepInEx.Economy;
using IronXNestCommand.Host.BepInEx.Overlay;
using IronXNestCommand.Host.BepInEx.Progression;

namespace IronXNestCommand.Host.BepInEx.Patches
{
    public static class GameEventsPatch
    {
        public static void InitializePatches(Harmony harmony)
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

        private static void TryPatchMethod(Harmony harmony, string typeName, string methodName, MethodInfo postfix = null, MethodInfo prefix = null)
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
                    ModLogger.Warn($"[GameEventsPatch] Typ nicht gefunden: {typeName}");
                    return;
                }

                var method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                if (method != null)
                {
                    harmony.Patch(method,
                        prefix: prefix != null ? new HarmonyMethod(prefix) : null,
                        postfix: postfix != null ? new HarmonyMethod(postfix) : null);
                    ModLogger.Info($"[GameEventsPatch] ✔ Hook aktiv: {type.Name}.{method.Name}");
                }
            }
            catch (Exception ex)
            {
                ModLogger.Warn($"[GameEventsPatch] Fehler beim Patch von {typeName}.{methodName}: {ex.Message}");
            }
        }

        // ── Event-Handler ────────────────────────────────────────────────────────

        public static void OnTriggerFire_Postfix()
        {
            try
            {
                ProgressionManager.Data.ShellsFired++;
                SaveManager.SaveJson(IronXNestCommand.Core.Paths.ModPaths.ProgressFile, ProgressionManager.Data);
            }
            catch { }
        }

        public static void OnShellLanded_Postfix()
        {
            try
            {
                ProgressionManager.Data.DirectHits++;
                ProgressionManager.AddXP(20, "Treffer auf Ziel");
                CurrencyManager.AddCurrency(CurrencyType.IntelPoints, 5);
                CommandOverlay.ShowNotification("🎯 Treffer bestätigt! +20 XP, +5 Intel");
            }
            catch { }
        }

        public static void OnDamageEntity_Postfix()
        {
            try
            {
                ProgressionManager.Data.CounterBatteryKills++;
                ProgressionManager.AddXP(50, "Ziel neutralisiert");
                CurrencyManager.AddCurrency(CurrencyType.LogisticsTokens, 2);
                CommandOverlay.ShowNotification("💥 Ziel zerstört! +50 XP, +2 Tokens");
            }
            catch { }
        }

        public static void OnMissionCompleted_Postfix(bool __result)
        {
            try
            {
                if (__result)
                {
                    ProgressionManager.RecordMissionFinished(true, 1, 1, 0);
                    CommandOverlay.ShowNotification("🏆 MISSION ERFOLGREICH ABGESCHLOSSEN! +250 XP, +10 Tokens");
                    ModLogger.Info("[GameEventsPatch] Missionssieg verbucht!");
                }
            }
            catch { }
        }

        public static void OnMissionFailed_Postfix(bool __result)
        {
            try
            {
                if (__result)
                {
                    ProgressionManager.RecordMissionFinished(false, 1, 0, 0);
                    CommandOverlay.ShowNotification("⚠ Mission gescheitert. +50 Trost-XP");
                    ModLogger.Info("[GameEventsPatch] Missionsniederlage verbucht.");
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
                    IronXNestCommand.Host.BepInEx.Notes.TeleprinterLog.AddDispatch(sourceId, lineList);
                    CommandOverlay.ShowNotification("📡 Neues HQ-Telegramm empfangen!");
                    ModLogger.Info($"[GameEventsPatch] Teleprinter-Telegramm von {sourceId} abgefangen ({lineList.Count} Zeilen).");
                }
            }
            catch { }
        }
    }
}
