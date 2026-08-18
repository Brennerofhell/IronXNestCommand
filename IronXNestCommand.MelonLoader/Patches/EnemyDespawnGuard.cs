using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using MelonLoader;
using IronXNestCommand.Core;
using IronXNestCommand.UI;

namespace IronXNestCommand.Patches
{
    /// <summary>
    /// Verhindert das fehlerhafte 3D-Volumen-Culling im Spiel,
    /// ohne das reguläre Nebel-des-Krieges- und Aufklärungssystem (EntityLocation) zu manipulieren.
    /// </summary>
    public static class EnemyDespawnGuard
    {
        private static bool _patchesApplied = false;

        private static Type _cullTargetType;
        private static PropertyInfo _neverCullProp;

        public static void InitializePatches(HarmonyLib.Harmony harmony)
        {
            if (harmony == null || _patchesApplied) return;

            try
            {
                // Hook: MinimalVolumeCulling.CullTarget.ApplyCulled (3D Volume Culling)
                TryPatchMethod(harmony, "MinimalVolumeCulling.CullTarget, Assembly-CSharp", "ApplyCulled",
                    prefix: typeof(EnemyDespawnGuard).GetMethod(nameof(OnApplyCulled_Prefix), BindingFlags.Public | BindingFlags.Static));

                _patchesApplied = true;
                MelonLogger.Msg("[EnemyDespawnGuard] ✔ 3D-Culling-Guard erfolgreich aktiviert.");
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"[EnemyDespawnGuard] Init Fehler: {ex.Message}");
            }
        }

        private static void TryPatchMethod(HarmonyLib.Harmony harmony, string typeName, string methodName, MethodInfo prefix = null, MethodInfo postfix = null)
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

                if (type.Name == "CullTarget")
                {
                    _cullTargetType = type;
                    _neverCullProp = type.GetProperty("neverCull");
                }

                var method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                if (method != null)
                {
                    harmony.Patch(method,
                        prefix: prefix != null ? new HarmonyMethod(prefix) : null,
                        postfix: postfix != null ? new HarmonyMethod(postfix) : null);
                    MelonLogger.Msg($"[EnemyDespawnGuard] ✔ Hook aktiv: {type.Name}.{method.Name}");
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"[EnemyDespawnGuard] Fehler beim Patch von {typeName}.{methodName}: {ex.Message}");
            }
        }

        public static bool OnApplyCulled_Prefix(object __instance, ref bool culled)
        {
            if (__instance == null) return true;
            if (!CommandOverlay.Config.PreventEnemyDespawn) return true;

            try
            {
                if (culled && _neverCullProp != null)
                {
                    bool neverCull = (bool)(_neverCullProp.GetValue(__instance) ?? false);
                    if (neverCull)
                    {
                        culled = false;
                        return false;
                    }
                }
            }
            catch { }
            return true;
        }

        public static void UpdateWatchdog(float deltaTime)
        {
            // Watchdog manipuliert keine EntityLocation-Objekte mehr,
            // um das Spiel-eigene Nebel-des-Krieges- und Aufklärungssystem nicht zu brechen.
        }
    }
}
