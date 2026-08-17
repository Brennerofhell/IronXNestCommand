using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using MelonLoader;
using IronXNestCommand.Core;

namespace IronXNestCommand.Ammo
{
    public class AmmoPunchcardInfo
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Subtitle { get; set; }
        public string CodeNumber { get; set; }
        public string Caliber { get; set; } = "150mm";
        public string ShellType { get; set; } = "HE";
        public int DefaultCharges { get; set; } = 3;
        public int RequisitionCost { get; set; } = 15;
    }

    /// <summary>
    /// Brücke zur In-Game Munitionsanforderung und zum Co-op Lochkarten-Resync.
    /// </summary>
    public static class AmmoRequisitionBridge
    {
        public static readonly List<AmmoPunchcardInfo> StandardCards = new()
        {
            new AmmoPunchcardInfo { Id = "HE_STD", Title = "HE STANDARD 150MM", Subtitle = "Hochexplosiv · Flächenwirkung", CodeNumber = "150-HE-01", ShellType = "HE", DefaultCharges = 3, RequisitionCost = 10 },
            new AmmoPunchcardInfo { Id = "AP_HV", Title = "AP PANZERBRECHEND", Subtitle = "Wuchtgeschoss · Hohe Durchschlagskraft", CodeNumber = "150-AP-02", ShellType = "AP", DefaultCharges = 4, RequisitionCost = 18 },
            new AmmoPunchcardInfo { Id = "SHRAPNEL", Title = "SCHRAPNELL-KASSETTE", Subtitle = "Luftdetonation · Infanterieabwehr", CodeNumber = "150-SH-03", ShellType = "SHRAPNEL", DefaultCharges = 2, RequisitionCost = 15 },
            new AmmoPunchcardInfo { Id = "SMOKE_SCREEN", Title = "NEBELWAND-KAPSEL", Subtitle = "Sichtschutz & Radarstreuung", CodeNumber = "150-SMK-04", ShellType = "SMOKE", DefaultCharges = 2, RequisitionCost = 8 },
            new AmmoPunchcardInfo { Id = "FLARE_ILLUM", Title = "LEUCHT-FALLSCHIRM", Subtitle = "Gefechtsfeld-Erleuchtung", CodeNumber = "150-FLR-05", ShellType = "FLARE", DefaultCharges = 1, RequisitionCost = 5 },
            new AmmoPunchcardInfo { Id = "INCENDIARY", Title = "BRAND-PHOSPHOR", Subtitle = "Thermische Zerstörung & Flächenbrand", CodeNumber = "150-INC-06", ShellType = "INCENDIARY", DefaultCharges = 3, RequisitionCost = 22 }
        };

        private static Type _reqManagerType;
        private static MethodInfo _attemptReqMethod;
        private static Type _missionSyncType;
        private static MethodInfo _startResyncNowMethod;
        private static MethodInfo _onRequestStateMethod;
        private static bool _initialized = false;

        public static void Initialize()
        {
            if (_initialized) return;

            try
            {
                // 1. Suche nach Spiel Requisition-Manager
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (_reqManagerType == null)
                    {
                        _reqManagerType = asm.GetType("RequisitionConsoleManager") 
                                       ?? asm.GetType("SleepyNodes.RequisitionConsoleManager");
                        if (_reqManagerType != null)
                        {
                            _attemptReqMethod = _reqManagerType.GetMethod("AttemptRequisition", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                        }
                    }

                    // 2. Suche nach IronNestCoop MissionSync für Lochkarten-Resync
                    if (_missionSyncType == null)
                    {
                        _missionSyncType = asm.GetType("IronNestCoop.Core.Sync.MissionSync");
                        if (_missionSyncType != null)
                        {
                            _startResyncNowMethod = _missionSyncType.GetMethod("StartResyncNow", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                            _onRequestStateMethod = _missionSyncType.GetMethod("OnRequestState", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                        }
                    }
                }

                _initialized = true;
                MelonLogger.Msg("[AmmoRequisitionBridge] Initialisiert.");
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"[AmmoRequisitionBridge] Init Warnung: {ex.Message}");
            }
        }

        /// <summary>
        /// Löst die Munitions-Bestellung via Lochkarte im Spiel aus.
        /// </summary>
        public static bool RequestAmmoPunchcard(AmmoPunchcardInfo card)
        {
            Initialize();

            try
            {
                if (_attemptReqMethod != null)
                {
                    if (_attemptReqMethod.IsStatic)
                    {
                        _attemptReqMethod.Invoke(null, null);
                    }
                    else
                    {
                        var il2cppType = Il2CppSystem.Type.GetType(_reqManagerType.FullName);
                        var obj = il2cppType != null ? UnityEngine.Object.FindObjectOfType(il2cppType) : null;
                        if (obj != null)
                        {
                            _attemptReqMethod.Invoke(obj, null);
                        }
                    }
                }

                MelonLogger.Msg($"[AmmoRequisitionBridge] Lochkarte [{card.Id}] {card.Title} angefordert!");
                return true;
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"[AmmoRequisitionBridge] Fehler bei Munitionsanforderung: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Löst einen sofortigen Co-op Lochkarten- und Raum-Resync aus (für Nicht-Hosts / Gäste).
        /// </summary>
        public static bool TriggerCoopResync()
        {
            Initialize();

            try
            {
                PunchcardSpawner.InvalidateCache();
                PunchcardSpawner.EnsureGuestFireMissionCard();
                PunchcardSpawner.EnsureGuestAmmoPunchcards();

                if (_startResyncNowMethod != null)
                {
                    _startResyncNowMethod.Invoke(null, null);
                    MelonLogger.Msg("[AmmoRequisitionBridge] ✔ Lochkarten- & Raum-Resync über IronNestCoop ausgelöst (StartResyncNow).");
                    return true;
                }
                else if (_onRequestStateMethod != null)
                {
                    _onRequestStateMethod.Invoke(null, null);
                    MelonLogger.Msg("[AmmoRequisitionBridge] ✔ Guest OnRequestState ausgelöst.");
                    return true;
                }
                else
                {
                    MelonLogger.Msg("[AmmoRequisitionBridge] IronNestCoop Resync-Methode nicht direkt im Speicher gebunden (Stand-Alone Modus).");
                    return true;
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"[AmmoRequisitionBridge] Fehler beim Co-op Resync: {ex.Message}");
                return false;
            }
        }
    }
}
