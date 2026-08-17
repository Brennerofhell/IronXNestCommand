using System;
using System.Reflection;
using UnityEngine;
using IronXNestCommand.Core.Logging;

namespace IronXNestCommand.Host.BepInEx.Core
{
    public class GunStatus
    {
        public string Name { get; set; } = "Gun";
        public float Elevation { get; set; } = 0f;
        public int PowderCharges { get; set; } = 0;
        public float FlightTimeEta { get; set; } = 0f;
        public bool CanFire { get; set; } = false;
        public bool IsReloading { get; set; } = false;
        public string LoadedShellName { get; set; } = "Standard HE";
    }

    public static class TurretTelemetry
    {
        public static float CurrentCompassHeading { get; private set; } = 0f;
        public static float CurrentElevation { get; private set; } = 0f;
        public static bool IsTurretAvailable { get; private set; } = false;
        public static GunStatus LeftGun { get; } = new() { Name = "Geschütz L" };
        public static GunStatus RightGun { get; } = new() { Name = "Geschütz R" };

        private static Type _turretControllerType;
        private static PropertyInfo _instanceProp;
        private static PropertyInfo _compassProp;
        private static PropertyInfo _elevationProp;
        private static PropertyInfo _gunsProp;

        // GunController reflection
        private static PropertyInfo _gunElevationProp;
        private static PropertyInfo _gunChargesProp;
        private static PropertyInfo _gunEtaProp;
        private static PropertyInfo _gunCanFireProp;
        private static PropertyInfo _gunIsReloadingProp;

        // Performance Caching
        private static object _cachedTurretInstance;
        private static object _cachedLeftGun;
        private static object _cachedRightGun;
        private static float _throttleTimer = 0f;
        private const float ThrottleInterval = 0.05f; // 20 Hz Abfrage (spart 85% Reflection-Overhead)

        private static bool _initialized = false;

        public static void Initialize()
        {
            if (_initialized) return;

            try
            {
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    _turretControllerType ??= asm.GetType("TurretController, Assembly-CSharp")
                                           ?? asm.GetType("TurretController");
                    if (_turretControllerType != null) break;
                }

                if (_turretControllerType != null)
                {
                    _instanceProp = _turretControllerType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
                    _compassProp = _turretControllerType.GetProperty("CurrentAngleCompass", BindingFlags.Public | BindingFlags.Instance);
                    _elevationProp = _turretControllerType.GetProperty("CurrentElevation", BindingFlags.Public | BindingFlags.Instance);
                    _gunsProp = _turretControllerType.GetProperty("guns", BindingFlags.Public | BindingFlags.Instance);

                    Type gunType = null;
                    foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        gunType = asm.GetType("GunController, Assembly-CSharp") ?? asm.GetType("GunController");
                        if (gunType != null) break;
                    }

                    if (gunType != null)
                    {
                        _gunElevationProp = gunType.GetProperty("CurrentElevation", BindingFlags.Public | BindingFlags.Instance);
                        _gunChargesProp = gunType.GetProperty("PowderCharges", BindingFlags.Public | BindingFlags.Instance);
                        _gunEtaProp = gunType.GetProperty("PredictedImpactTime", BindingFlags.Public | BindingFlags.Instance);
                        _gunCanFireProp = gunType.GetProperty("CanFire", BindingFlags.Public | BindingFlags.Instance);
                        _gunIsReloadingProp = gunType.GetProperty("IsReloading", BindingFlags.Public | BindingFlags.Instance);
                    }

                    _initialized = true;
                    ModLogger.Info("[TurretTelemetry] Turret-Telemetrie erfolgreich angebunden (High-Performance 20Hz Mode).");
                }
            }
            catch (Exception ex)
            {
                ModLogger.Warn($"[TurretTelemetry] Init Fehler: {ex.Message}");
            }
        }

        public static void Update()
        {
            if (!_initialized || _instanceProp == null) return;

            _throttleTimer += Time.unscaledDeltaTime;
            if (_throttleTimer < ThrottleInterval) return;
            _throttleTimer = 0f;

            try
            {
                if (_cachedTurretInstance == null)
                    _cachedTurretInstance = _instanceProp.GetValue(null);

                if (_cachedTurretInstance == null)
                {
                    IsTurretAvailable = false;
                    _cachedLeftGun = null;
                    _cachedRightGun = null;
                    return;
                }

                IsTurretAvailable = true;

                if (_compassProp != null)
                    CurrentCompassHeading = (float)_compassProp.GetValue(_cachedTurretInstance);

                if (_elevationProp != null)
                    CurrentElevation = (float)_elevationProp.GetValue(_cachedTurretInstance);

                if (_cachedLeftGun == null && _gunsProp != null)
                {
                    var gunsList = _gunsProp.GetValue(_cachedTurretInstance) as System.Collections.IList;
                    if (gunsList != null)
                    {
                        if (gunsList.Count > 0) _cachedLeftGun = gunsList[0];
                        if (gunsList.Count > 1) _cachedRightGun = gunsList[1];
                    }
                }

                if (_cachedLeftGun != null) ReadGun(_cachedLeftGun, LeftGun);
                if (_cachedRightGun != null) ReadGun(_cachedRightGun, RightGun);
            }
            catch
            {
                _cachedTurretInstance = null;
                _cachedLeftGun = null;
                _cachedRightGun = null;
            }
        }

        private static void ReadGun(object gunObj, GunStatus status)
        {
            try
            {
                if (_gunElevationProp != null)
                    status.Elevation = (float)_gunElevationProp.GetValue(gunObj);

                if (_gunChargesProp != null)
                    status.PowderCharges = (int)_gunChargesProp.GetValue(gunObj);

                if (_gunEtaProp != null)
                    status.FlightTimeEta = (float)_gunEtaProp.GetValue(gunObj);

                if (_gunCanFireProp != null)
                    status.CanFire = (bool)_gunCanFireProp.GetValue(gunObj);

                if (_gunIsReloadingProp != null)
                    status.IsReloading = (bool)_gunIsReloadingProp.GetValue(gunObj);
            }
            catch { }
        }

        public static (int charges, float neededElevation, float etaSeconds) CalculateFiringSolution(float distanceMeters)
        {
            int charges = 1;
            if (distanceMeters > 2000f) charges = 4;
            else if (distanceMeters > 1300f) charges = 3;
            else if (distanceMeters > 600f) charges = 2;

            float v0 = 250f * charges;
            float g = 9.81f;
            float sin2theta = Mathf.Clamp((distanceMeters * g) / (v0 * v0), 0f, 1f);
            float elevation = (float)((Math.Asin(sin2theta) * (180.0 / Math.PI)) / 2.0);

            float cosVal = (float)Math.Cos(elevation * (Math.PI / 180.0));
            float eta = cosVal > 0.01f ? (distanceMeters / (v0 * cosVal)) * 1.15f : 5.0f;

            return (charges, Mathf.Clamp(elevation, 5f, 75f), Mathf.Max(1f, eta));
        }
    }
}
