using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using IronXNestCommand.Core.Logging;

namespace IronXNestCommand.Host.BepInEx.Core
{
    public class ActiveMissionCardData
    {
        public string MissionId { get; set; } = "MIS-01";
        public string TargetName { get; set; } = "FEINDLICHE PANZER-KOLONNE";
        public float Distance { get; set; } = 1200f;
        public float Azimuth { get; set; } = 45f;
        public float Elevation { get; set; } = 18.5f;
        public int RecommendedCharges { get; set; } = 2;
        public string RecommendedShell { get; set; } = "AP-150";
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public bool HasActiveMission { get; set; } = false;
    }

    public static class PunchcardSpawner
    {
        public static ActiveMissionCardData CurrentMission { get; } = new();

        private static Type _printerType;
        private static Type _cardType;
        private static Type _reqSlotType;
        private static MethodInfo _printMethod;

        // Performance Object Cache
        private static UnityEngine.Object[] _cachedPrinters;
        private static UnityEngine.Object[] _cachedCards;
        private static UnityEngine.Object[] _cachedSlots;
        private static float _cacheTime = 0f;
        private const float CacheLifetime = 8.0f; // Cache 8 Sekunden gültig

        private static bool _initialized = false;

        public static void Initialize()
        {
            if (_initialized) return;

            try
            {
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (_printerType == null)
                        _printerType = asm.GetType("FireMissionCardPrinter, Assembly-CSharp") 
                                    ?? asm.GetType("FireMissionCardPrinter");

                    if (_cardType == null)
                        _cardType = asm.GetType("WFireMissionCard, Assembly-CSharp") 
                                 ?? asm.GetType("FireMissionCard, Assembly-CSharp")
                                 ?? asm.GetType("PunchcardRuntime");

                    if (_reqSlotType == null)
                        _reqSlotType = asm.GetType("WRequisitionSlot, Assembly-CSharp") 
                                    ?? asm.GetType("RequisitionSlot");
                }

                if (_printerType != null)
                {
                    _printMethod = _printerType.GetMethod("HandleCalculationSuccess", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                                ?? _printerType.GetMethod("PrintCard", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                                ?? _printerType.GetMethod("DispenseCard", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                }

                _initialized = true;
                ModLogger.Info($"[PunchcardSpawner] Initialisiert mit Objekt-Caching.");
            }
            catch (Exception ex)
            {
                ModLogger.Warn($"[PunchcardSpawner] Init Warnung: {ex.Message}");
            }
        }

        public static void InvalidateCache()
        {
            _cachedPrinters = null;
            _cachedCards = null;
            _cachedSlots = null;
            _cacheTime = 0f;
        }

        public static bool EnsureGuestFireMissionCard()
        {
            Initialize();

            try
            {
                RefreshCacheIfNeeded();

                // 1. Lokalen Drucker bedienen
                if (_cachedPrinters != null && _cachedPrinters.Length > 0 && _printMethod != null)
                {
                    foreach (var printer in _cachedPrinters)
                    {
                        if (printer != null)
                        {
                            _printMethod.Invoke(printer, BuildPrintMethodArgs(_printMethod));
                            ModLogger.Info("[PunchcardSpawner] ✔ Einsatz-Lochkarte über lokalen Drucker gedruckt.");
                            return true;
                        }
                    }
                }

                // 2. Versteckte Karten reaktivieren
                if (_cachedCards != null && _cachedCards.Length > 0)
                {
                    foreach (var card in _cachedCards)
                    {
                        if (card is Component comp && comp.gameObject != null)
                        {
                            if (!comp.gameObject.activeSelf)
                            {
                                comp.gameObject.SetActive(true);
                                ModLogger.Info($"[PunchcardSpawner] ✔ Versteckte Lochkarte [{comp.gameObject.name}] reaktiviert.");
                            }
                        }
                    }
                    return true;
                }

                return true;
            }
            catch (Exception ex)
            {
                ModLogger.Warn($"[PunchcardSpawner] Fehler beim Spawnen der Einsatzkarte: {ex.Message}");
                return false;
            }
        }

        public static bool EnsureGuestAmmoPunchcards()
        {
            Initialize();

            try
            {
                RefreshCacheIfNeeded();

                if (_cachedSlots != null && _cachedSlots.Length > 0)
                {
                    foreach (var slot in _cachedSlots)
                    {
                        if (slot is Component comp && comp.gameObject != null)
                        {
                            if (!comp.gameObject.activeSelf)
                                comp.gameObject.SetActive(true);
                        }
                    }
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        // Die echte Signatur von FireMissionCardPrinter.HandleCalculationSuccess ist
        // (float elevationDegrees, float clampedRange, int powderCharge, bool wasClamped) —
        // bestätigt durch den Harmony-Postfix in CoopPunchcardFix.OnPrinterCalculate_Postfix.
        // Ein Aufruf mit 0 Argumenten (vorher: Invoke(printer, null)) wirft eine
        // TargetParameterCountException, die vom umgebenden catch stillschweigend verschluckt
        // wurde — dieser Pfad hat dadurch bislang nie tatsächlich eine Karte gedruckt.
        private static object[] BuildPrintMethodArgs(MethodInfo method)
        {
            var parameters = method.GetParameters();
            if (parameters.Length == 0) return null;

            if (method.Name == "HandleCalculationSuccess" && parameters.Length == 4)
            {
                return new object[] { CurrentMission.Elevation, CurrentMission.Distance, CurrentMission.RecommendedCharges, false };
            }

            // Unbekannte Fallback-Methode (PrintCard/DispenseCard) — Default-Werte je Parametertyp,
            // besser als eine garantiert falsche Argumentanzahl.
            var args = new object[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                var pt = parameters[i].ParameterType;
                args[i] = pt.IsValueType ? Activator.CreateInstance(pt) : null;
            }
            return args;
        }

        private static object GetIl2CppType(Type managedType)
        {
            if (managedType == null) return null;
            try
            {
                var il2cppType = Il2CppInterop.Runtime.Il2CppType.From(managedType);
                if (il2cppType != null) return il2cppType;
            }
            catch { }

            return null;
        }

        private static void RefreshCacheIfNeeded()
        {
            if (Time.unscaledTime - _cacheTime < CacheLifetime && _cachedPrinters != null)
                return;

            _cacheTime = Time.unscaledTime;

            if (_printerType != null)
            {
                dynamic il2cppPrinterType = GetIl2CppType(_printerType);
                if (il2cppPrinterType != null)
                    _cachedPrinters = UnityEngine.Object.FindObjectsOfType(il2cppPrinterType);
            }

            if (_cardType != null)
            {
                dynamic il2cppCardType = GetIl2CppType(_cardType);
                if (il2cppCardType != null)
                    _cachedCards = Resources.FindObjectsOfTypeAll(il2cppCardType);
            }

            if (_reqSlotType != null)
            {
                dynamic il2cppReqType = GetIl2CppType(_reqSlotType);
                if (il2cppReqType != null)
                    _cachedSlots = UnityEngine.Object.FindObjectsOfType(il2cppReqType);
            }
        }

        public static void SetActiveMissionData(string missionId, string targetName, float dist, float azimuth, float elev, int charges, string shell)
        {
            CurrentMission.MissionId = missionId;
            CurrentMission.TargetName = targetName;
            CurrentMission.Distance = dist;
            CurrentMission.Azimuth = azimuth;
            CurrentMission.Elevation = elev;
            CurrentMission.RecommendedCharges = charges;
            CurrentMission.RecommendedShell = shell;
            CurrentMission.Timestamp = DateTime.Now;
            CurrentMission.HasActiveMission = true;

            InvalidateCache(); // Cache invalidieren für sofortigen Neuscan
            ModLogger.Info($"[PunchcardSpawner] Neue Einsatz-Lochkarte erfasst: {targetName} @ {dist:F0}m");
        }
    }
}
