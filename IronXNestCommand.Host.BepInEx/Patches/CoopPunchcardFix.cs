using System;
using System.Reflection;
using System.Text.RegularExpressions;
using HarmonyLib;
using UnityEngine;
using IronXNestCommand.Core.Logging;
using IronXNestCommand.Host.BepInEx.Core;
using IronXNestCommand.Host.BepInEx.Overlay;
using IronXNestCommand.Host.BepInEx.Steam;

namespace IronXNestCommand.Host.BepInEx.Patches
{
    public static class CoopPunchcardFix
    {
        private static float _watchdogTimer = 0f;
        private const float WatchdogInterval = 2.0f;
        private static bool _patchesApplied = false;

        public static void InitializePatches(Harmony harmony)
        {
            if (harmony == null || _patchesApplied) return;

            // 1. Hook: FireMissionCardPrinter.HandleCalculationSuccess (wenn eine Karte berechnet/gedruckt wird)
            TryPatchMethod(harmony, "FireMissionCardPrinter, Assembly-CSharp", "HandleCalculationSuccess",
                postfix: typeof(CoopPunchcardFix).GetMethod(nameof(OnPrinterCalculate_Postfix), BindingFlags.Public | BindingFlags.Static));

            // 2. Hook: FireMissionCard.Apply (wenn Daten auf eine Lochkarte übertragen werden)
            TryPatchMethod(harmony, "FireMissionCard, Assembly-CSharp", "Apply",
                postfix: typeof(CoopPunchcardFix).GetMethod(nameof(OnCardApply_Postfix), BindingFlags.Public | BindingFlags.Static));

            // 3. Hook: Teleprinter.SubmitLines (Fängt Zieldaten für die Lochkarte ab)
            TryPatchMethod(harmony, "Teleprinter, Assembly-CSharp", "SubmitLines",
                prefix: typeof(CoopPunchcardFix).GetMethod(nameof(OnTeleprinterSubmitLines_Prefix), BindingFlags.Public | BindingFlags.Static));

            _patchesApplied = true;
            ModLogger.Info("[CoopPunchcardFix] ✔ Lochkarten Co-op Patches erfolgreich registriert.");
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
                    ModLogger.Warn($"[CoopPunchcardFix] Typ nicht gefunden: {typeName}");
                    return;
                }

                var method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                if (method != null)
                {
                    harmony.Patch(method,
                        prefix: prefix != null ? new HarmonyMethod(prefix) : null,
                        postfix: postfix != null ? new HarmonyMethod(postfix) : null);
                    ModLogger.Info($"[CoopPunchcardFix] ✔ Hook aktiv: {type.Name}.{method.Name}");
                }
            }
            catch (Exception ex)
            {
                ModLogger.Warn($"[CoopPunchcardFix] Fehler beim Patch von {typeName}.{methodName}: {ex.Message}");
            }
        }

        /// <summary>
        /// Periodischer Watchdog (aufgerufen via MonoBehaviour Update in CommandOverlay oder Plugin).
        /// </summary>
        public static void UpdateWatchdog(float deltaTime)
        {
            _watchdogTimer += deltaTime;
            if (_watchdogTimer >= WatchdogInterval)
            {
                _watchdogTimer = 0f;

                // Wenn im Co-op (insbesondere als Gast): Stelle sicher, dass Lochkarten aktiv sind
                if (SteamworksDetector.IsInLobby)
                {
                    if (PunchcardSpawner.CurrentMission.HasActiveMission)
                    {
                        PunchcardSpawner.EnsureGuestFireMissionCard();
                    }
                    PunchcardSpawner.EnsureGuestAmmoPunchcards();
                }
            }
        }

        // ── Event-Handler ────────────────────────────────────────────────────────

        public static void OnPrinterCalculate_Postfix(object __instance, float elevationDegrees = 0f, float clampedRange = 0f, int powderCharge = 0, bool wasClamped = false)
        {
            try
            {
                ModLogger.Info($"[CoopPunchcardFix] FireMissionCardPrinter hat Berechnung abgeschlossen (Elev: {elevationDegrees:F1}°, Dist: {clampedRange:F0}m, Powder: {powderCharge}). Stelle Gast-Lochkarte sicher...");
                PunchcardSpawner.EnsureGuestFireMissionCard();
                CommandOverlay.ShowNotification("🖨️ Neue Einsatz-Lochkarte gedruckt!");
            }
            catch { }
        }

        public static void OnCardApply_Postfix(object __instance, string distanceToTarget = "", string bearingToTarget = "", string gunElevation = "", string powderCharge = "", string shellType = "", string gunSelection = "")
        {
            try
            {
                ModLogger.Info($"[CoopPunchcardFix] FireMissionCard Apply (Dist: {distanceToTarget}, Az: {bearingToTarget}, Elev: {gunElevation}, Powder: {powderCharge}, Shell: {shellType}).");
                PunchcardSpawner.EnsureGuestFireMissionCard();
            }
            catch { }
        }

        public static void OnTeleprinterSubmitLines_Prefix(string sourceId, object lines)
        {
            try
            {
                if (lines is System.Collections.IEnumerable enumerable)
                {
                    string fullText = "";
                    foreach (var item in enumerable)
                    {
                        if (item != null) fullText += item.ToString() + " ";
                    }

                    // Versuche Distanz und Azimuth aus dem Funkspruch zu parsen. Ohne erkannte Distanz
                    // gibt es kein echtes Ziel — vorher wurde hier stillschweigend auf 1200m/0° zurückgefallen
                    // und eine Feuerleitlösung für ein Phantom-Ziel berechnet und an den Gast verteilt.
                    var distMatch = Regex.Match(fullText, @"(?:Dist|Entf|Range|Distance)[:\s]+(\d+)", RegexOptions.IgnoreCase);
                    if (!distMatch.Success || !float.TryParse(distMatch.Groups[1].Value, out float dist))
                    {
                        ModLogger.Warn($"[CoopPunchcardFix] Konnte keine Distanz aus Funkspruch parsen, überspringe: \"{fullText.Trim()}\"");
                        return;
                    }

                    float az = 0f;
                    var azMatch = Regex.Match(fullText, @"(?:Azimuth|Bearing|Peilung|Heading)[:\s]+(\d+(?:\.\d+)?)", RegexOptions.IgnoreCase);
                    if (azMatch.Success && float.TryParse(azMatch.Groups[1].Value, out float parsedAz))
                        az = parsedAz;

                    var (charges, elev, eta) = TurretTelemetry.CalculateFiringSolution(dist);

                    PunchcardSpawner.SetActiveMissionData(
                        missionId: $"HQ-{DateTime.Now:mmssf}",
                        targetName: fullText.Length > 30 ? fullText.Substring(0, 30) + "..." : fullText,
                        dist: dist,
                        azimuth: az,
                        elev: elev,
                        charges: charges,
                        shell: "HE-150 / AP-150"
                    );
                }
            }
            catch { }
        }
    }
}
