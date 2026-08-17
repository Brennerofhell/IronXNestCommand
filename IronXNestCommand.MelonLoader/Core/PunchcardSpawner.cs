using System;
using System.Reflection;
using UnityEngine;
using MelonLoader;

namespace IronXNestCommand.Core
{
    public class MissionTargetData
    {
        public string MissionId { get; set; } = "HQ-01";
        public string TargetName { get; set; } = "HQ-FEINDSTELLUNG";
        public float Distance { get; set; } = 1200f;
        public float Azimuth { get; set; } = 45f;
        public float Elevation { get; set; } = 12.5f;
        public int RecommendedCharges { get; set; } = 3;
        public string RecommendedShell { get; set; } = "HE-150";
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public bool HasActiveMission { get; set; } = false;
    }

    /// <summary>
    /// Spawner & Synchronisationshelfer für Lochkarten auf dem Geschütztisch von Gast-Spielern.
    /// </summary>
    public static class PunchcardSpawner
    {
        public static MissionTargetData CurrentMission { get; } = new();

        private static Type _cardType;
        private static Type _printerType;
        private static Type _reqSlotType;
        private static MethodInfo _printerApplyTarget;
        private static MethodInfo _printerApplyPowder;
        private static MethodInfo _cardApplyMethod;

        private static bool _initialized = false;
        private static float _cacheTime = 0f;
        private const float CacheLifetime = 8.0f;
        private static UnityEngine.Object[] _cachedPrinters;
        private static UnityEngine.Object[] _cachedCards;
        private static UnityEngine.Object[] _cachedSlots;

        public static void Initialize()
        {
            if (_initialized) return;

            try
            {
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    _cardType ??= asm.GetType("FireMissionCard") ?? asm.GetType("PunchcardRuntime");
                    _printerType ??= asm.GetType("FireMissionCardPrinter");
                    _reqSlotType ??= asm.GetType("PunchcardSlot") ?? asm.GetType("SleepyNodes.RequisitionSlot");
                }

                if (_cardType != null)
                {
                    _cardApplyMethod = _cardType.GetMethod("Apply", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                }

                if (_printerType != null)
                {
                    _printerApplyTarget = _printerType.GetMethod("ApplyTargetTextureToCard", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    _printerApplyPowder = _printerType.GetMethod("ApplyPowderChargeTextureToCard", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                }

                _initialized = true;
                MelonLogger.Msg("[PunchcardSpawner] Initialisiert.");
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"[PunchcardSpawner] Init Warnung: {ex.Message}");
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

                if (_cachedCards != null && _cachedCards.Length > 0)
                {
                    foreach (var card in _cachedCards)
                    {
                        if (card is Component comp && comp.gameObject != null)
                        {
                            if (!comp.gameObject.activeSelf)
                            {
                                comp.gameObject.SetActive(true);
                            }
                        }
                    }
                    return true;
                }
                return false;
            }
            catch
            {
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
                        if (slot is Component comp && comp.gameObject != null && !comp.gameObject.activeSelf)
                        {
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

        private static Il2CppSystem.Type GetIl2CppType(Type managedType)
        {
            if (managedType == null) return null;
            try
            {
                var il2cppType = Il2CppInterop.Runtime.Il2CppType.From(managedType);
                if (il2cppType != null) return il2cppType;
            }
            catch { }

            try
            {
                return Il2CppSystem.Type.GetType(managedType.AssemblyQualifiedName)
                    ?? Il2CppSystem.Type.GetType($"{managedType.FullName}, Assembly-CSharp")
                    ?? Il2CppSystem.Type.GetType(managedType.FullName);
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
                var il2cppPrinterType = GetIl2CppType(_printerType);
                if (il2cppPrinterType != null)
                    _cachedPrinters = UnityEngine.Object.FindObjectsOfType(il2cppPrinterType);
            }

            if (_cardType != null)
            {
                var il2cppCardType = GetIl2CppType(_cardType);
                if (il2cppCardType != null)
                    _cachedCards = Resources.FindObjectsOfTypeAll(il2cppCardType);
            }

            if (_reqSlotType != null)
            {
                var il2cppReqType = GetIl2CppType(_reqSlotType);
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

            InvalidateCache();
            MelonLogger.Msg($"[PunchcardSpawner] Neue Einsatz-Lochkarte erfasst: {targetName} @ {dist:F0}m");
        }
    }
}
