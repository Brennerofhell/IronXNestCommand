using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using IronXNestCommand.Core.Logging;
using IronXNestCommand.Host.BepInEx.Core;

namespace IronXNestCommand.Host.BepInEx.Ammo
{
    public class AmmoPunchcardInfo
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Subtitle { get; set; }
        public string CodeNumber { get; set; }
        public string Caliber { get; set; } = "150mm";
        public string Role { get; set; }
        public Color PunchcardColor { get; set; } = new Color(0.85f, 0.75f, 0.50f, 1f);
    }

    public static class AmmoRequisitionBridge
    {
        public static List<AmmoPunchcardInfo> AvailableCards { get; } = new()
        {
            new AmmoPunchcardInfo
            {
                Id = "HE",
                Title = "SPRENGGRANATE HE MK.IV",
                Subtitle = "High Explosive Fragmentation",
                CodeNumber = "CARD #01 // HE-150",
                Role = "Infanterie, leichte Fahrzeuge & Gräben",
                PunchcardColor = new Color(0.90f, 0.65f, 0.35f, 1f)
            },
            new AmmoPunchcardInfo
            {
                Id = "AP",
                Title = "PANZERBRECHEND AP MK.II",
                Subtitle = "Armor Piercing Solid Shot",
                CodeNumber = "CARD #02 // AP-150",
                Role = "Kampfpanzer, schwer gepanzerte Spähwagen",
                PunchcardColor = new Color(0.70f, 0.75f, 0.85f, 1f)
            },
            new AmmoPunchcardInfo
            {
                Id = "BUNKER_BUSTER",
                Title = "BETONBRECHER SAP-HE",
                Subtitle = "Semi-Armor Piercing Heavy Concrete Buster",
                CodeNumber = "CARD #03 // BB-150",
                Role = "Stahlbeton-Bunker & feindliche Artillerie-Nester",
                PunchcardColor = new Color(0.85f, 0.45f, 0.35f, 1f)
            },
            new AmmoPunchcardInfo
            {
                Id = "SMOKE",
                Title = "NEBELGRANATE WP-SMK",
                Subtitle = "White Phosphorus Tactical Screen",
                CodeNumber = "CARD #04 // SMK-150",
                Role = "Sichtschutz & Blendung gegnerischer Beobachter",
                PunchcardColor = new Color(0.75f, 0.80f, 0.75f, 1f)
            },
            new AmmoPunchcardInfo
            {
                Id = "ILLUM",
                Title = "LEUCHTGRANATE ILLUM",
                Subtitle = "Parachute Flare Illumination",
                CodeNumber = "CARD #05 // ILL-150",
                Role = "Nachtgefechte & Zielaufklärung bei Dunkelheit",
                PunchcardColor = new Color(0.95f, 0.90f, 0.50f, 1f)
            },
            new AmmoPunchcardInfo
            {
                Id = "POWDER",
                Title = "TREIBLADUNG (POWDER PACK)",
                Subtitle = "Standard Cordite Propellant Bag",
                CodeNumber = "CARD #06 // PWD-STD",
                Role = "Zusätzliche Treibladungen für Weitschuss-Distanzen",
                PunchcardColor = new Color(0.65f, 0.85f, 0.65f, 1f)
            }
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
                ModLogger.Info("[AmmoRequisitionBridge] Initialisiert.");
            }
            catch (Exception ex)
            {
                ModLogger.Warn($"[AmmoRequisitionBridge] Init Warnung: {ex.Message}");
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

                ModLogger.Info($"[AmmoRequisitionBridge] Lochkarte [{card.Id}] {card.Title} angefordert!");
                return true;
            }
            catch (Exception ex)
            {
                ModLogger.Warn($"[AmmoRequisitionBridge] Fehler bei Munitionsanforderung: {ex.Message}");
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
                    ModLogger.Info("[AmmoRequisitionBridge] ✔ Lochkarten- & Raum-Resync über IronNestCoop ausgelöst (StartResyncNow).");
                    return true;
                }
                else if (_onRequestStateMethod != null)
                {
                    _onRequestStateMethod.Invoke(null, null);
                    ModLogger.Info("[AmmoRequisitionBridge] ✔ Guest OnRequestState ausgelöst.");
                    return true;
                }
                else
                {
                    ModLogger.Info("[AmmoRequisitionBridge] IronNestCoop Resync-Methode nicht direkt im Speicher gebunden (Stand-Alone Modus).");
                    return true;
                }
            }
            catch (Exception ex)
            {
                ModLogger.Warn($"[AmmoRequisitionBridge] Fehler beim Co-op Resync: {ex.Message}");
                return false;
            }
        }
    }
}
