using System;
using System.Reflection;
using UnityEngine;
using MelonLoader;

namespace IronXNestCommand.Core
{
    public class GunTelemetryStatus
    {
        public int GunIndex { get; set; }
        public float CurrentElevation { get; set; }
        public float CurrentBearing { get; set; }
        public string LoadedShell { get; set; } = "Leer";
        public bool IsLoaded { get; set; }
        public bool IsReadyToFire { get; set; }
        public bool IsReloading { get; set; }
    }

    /// <summary>
    /// Telemetrie- und Ballistik-Berechnung für das Hauptgeschütz.
    /// </summary>
    public static class TurretTelemetry
    {
        public static GunTelemetryStatus Gun1 { get; } = new() { GunIndex = 1 };
        public static GunTelemetryStatus Gun2 { get; } = new() { GunIndex = 2 };

        private static Type _gunControllerType;
        private static PropertyInfo _gun1Prop;
        private static PropertyInfo _gun2Prop;
        private static PropertyInfo _gunElevationProp;
        private static PropertyInfo _gunBearingProp;
        private static PropertyInfo _gunLoadedShellProp;
        private static PropertyInfo _gunIsReadyProp;
        private static PropertyInfo _gunIsReloadingProp;

        private static bool _initialized = false;

        public static void Initialize()
        {
            if (_initialized) return;

            try
            {
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    _gunControllerType ??= asm.GetType("GunController") ?? asm.GetType("SleepyNodes.GunController");
                }

                if (_gunControllerType != null)
                {
                    _gun1Prop = _gunControllerType.GetProperty("Gun1", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                    _gun2Prop = _gunControllerType.GetProperty("Gun2", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);

                    var gunType = _gun1Prop?.PropertyType;
                    if (gunType != null)
                    {
                        _gunElevationProp = gunType.GetProperty("Elevation") ?? gunType.GetProperty("CurrentElevation");
                        _gunBearingProp = gunType.GetProperty("Bearing") ?? gunType.GetProperty("CurrentBearing");
                        _gunLoadedShellProp = gunType.GetProperty("LoadedShell") ?? gunType.GetProperty("CurrentShell");
                        _gunIsReadyProp = gunType.GetProperty("IsReady") ?? gunType.GetProperty("CanFire");
                        _gunIsReloadingProp = gunType.GetProperty("IsReloading");
                    }
                }

                _initialized = true;
                MelonLogger.Msg("[TurretTelemetry] Initialisiert.");
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"[TurretTelemetry] Init Warnung: {ex.Message}");
            }
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
