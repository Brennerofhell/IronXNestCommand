using System;
using System.Reflection;
using System.Text.RegularExpressions;
using HarmonyLib;
using UnityEngine;
using MelonLoader;
using IronXNestCommand.Core;
using IronXNestCommand.UI;

namespace IronXNestCommand.Patches
{
    public static class CoopPunchcardFix
    {
        private static float _watchdogTimer = 0f;
        private const float WatchdogInterval = 2.0f;
        private static bool _patchesApplied = false;

        public static void InitializePatches(HarmonyLib.Harmony harmony)
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
            MelonLogger.Msg("[CoopPunchcardFix] ✔ Lochkarten Co-op Patches erfolgreich registriert.");
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

                if (type == null) return;

                var method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                if (method != null)
                {
                    harmony.Patch(method,
                        prefix: prefix != null ? new HarmonyMethod(prefix) : null,
                        postfix: postfix != null ? new HarmonyMethod(postfix) : null);
                    MelonLogger.Msg($"[CoopPunchcardFix] ✔ Hook aktiv: {type.Name}.{method.Name}");
                }
            }
            catch { }
        }

        /// <summary>
        /// Periodischer Watchdog.
        /// </summary>
        public static void UpdateWatchdog(float deltaTime)
        {
            _watchdogTimer += deltaTime;
            if (_watchdogTimer >= WatchdogInterval)
            {
                _watchdogTimer = 0f;

                if (Steam.SteamworksDetector.IsInLobby)
                {
                    if (PunchcardSpawner.CurrentMission.HasActiveMission)
                    {
                        PunchcardSpawner.EnsureGuestFireMissionCard();
                    }
                    PunchcardSpawner.EnsureGuestAmmoPunchcards();
                }
            }
        }

        public static void OnPrinterCalculate_Postfix(object __instance, float elevationDegrees = 0f, float clampedRange = 0f, int powderCharge = 0, bool wasClamped = false)
        {
            try
            {
                PunchcardSpawner.EnsureGuestFireMissionCard();
                CommandOverlay.ShowNotification("🖨️ Neue Einsatz-Lochkarte gedruckt!");
            }
            catch { }
        }

        public static void OnCardApply_Postfix(object __instance, string distanceToTarget = "", string bearingToTarget = "", string gunElevation = "", string powderCharge = "", string shellType = "", string gunSelection = "")
        {
            try
            {
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

                    // Ohne erkannte Distanz gibt es kein echtes Ziel — vorher wurde hier stillschweigend
                    // auf 1200m/0° zurückgefallen und eine Feuerleitlösung für ein Phantom-Ziel berechnet.
                    var distMatch = Regex.Match(fullText, @"(?:Dist|Entf|Range|Distance)[:\s]+(\d+)", RegexOptions.IgnoreCase);
                    if (!distMatch.Success || !float.TryParse(distMatch.Groups[1].Value, out float dist))
                    {
                        MelonLogger.Warning($"[CoopPunchcardFix] Konnte keine Distanz aus Funkspruch parsen, überspringe: \"{fullText.Trim()}\"");
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
