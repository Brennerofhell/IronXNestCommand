using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Il2CppInterop.Runtime;
using MelonLoader;
using IronXNestCommand.Core;
using IronXNestCommand.UI;

namespace IronXNestCommand.Patches
{
    /// <summary>
    /// Verhindert das vorzeitige oder fehlerhafte Despawnen/Unsichtbarwerden von Gegner- und Zieleinheiten im Co-op.
    /// Schützt Einheiten vor Culling-Entfernung, Nebel-Timeout und fehlerhaften HideVisualRoot-Aufrufen.
    /// </summary>
    public static class EnemyDespawnGuard
    {
        private static float _watchdogTimer = 0f;
        private const float WatchdogInterval = 1.5f;
        private static bool _patchesApplied = false;

        public static void InitializePatches(HarmonyLib.Harmony harmony)
        {
            if (harmony == null || _patchesApplied) return;

            try
            {
                // 1. Hook: EntityLocation.HideVisualRoot
                TryPatchMethod(harmony, "EntityLocation, Assembly-CSharp", "HideVisualRoot",
                    prefix: typeof(EnemyDespawnGuard).GetMethod(nameof(OnHideVisualRoot_Prefix), BindingFlags.Public | BindingFlags.Static));

                // 2. Hook: EntityLocation.Init
                TryPatchMethod(harmony, "EntityLocation, Assembly-CSharp", "Init",
                    postfix: typeof(EnemyDespawnGuard).GetMethod(nameof(OnInit_Postfix), BindingFlags.Public | BindingFlags.Static));

                // 3. Hook: MinimalVolumeCulling.CullTarget.ApplyCulled
                TryPatchMethod(harmony, "MinimalVolumeCulling.CullTarget, Assembly-CSharp", "ApplyCulled",
                    prefix: typeof(EnemyDespawnGuard).GetMethod(nameof(OnApplyCulled_Prefix), BindingFlags.Public | BindingFlags.Static));

                _patchesApplied = true;
                MelonLogger.Msg("[EnemyDespawnGuard] ✔ Gegner-Despawn Schutz & Culling-Guard erfolgreich aktiviert.");
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

                var method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                if (method != null)
                {
                    harmony.Patch(method,
                        prefix: prefix != null ? new HarmonyMethod(prefix) : null,
                        postfix: postfix != null ? new HarmonyMethod(postfix) : null);
                    MelonLogger.Msg($"[EnemyDespawnGuard] ✔ Hook aktiv: {type.Name}.{method.Name}");
                }
            }
            catch { }
        }

        /// <summary>
        /// Verhindert, dass aktive, lebendige Gegner durch HideVisualRoot unsichtbar gemacht werden.
        /// </summary>
        public static bool OnHideVisualRoot_Prefix(object __instance)
        {
            if (__instance == null) return true;
            if (!CommandOverlay.Config.PreventEnemyDespawn) return true;

            try
            {
                if (__instance is global::EntityLocation el)
                {
                    if (el.Entity != null && el.Entity.IsAlive)
                    {
                        if (el.VisualRoot != null && !el.VisualRoot.activeSelf)
                        {
                            el.VisualRoot.SetActive(true);
                        }
                        if (el.VisibilityGroup != null && el.VisibilityGroup.alpha < 0.9f)
                        {
                            el.VisibilityGroup.alpha = 1.0f;
                        }
                        return false; // Verhindert das Ausblenden / Despawnen
                    }
                }
            }
            catch { }
            return true;
        }

        /// <summary>
        /// Stellt sicher, dass neu initialisierte Gegner nicht im versteckten Zustand verbleiben.
        /// </summary>
        public static void OnInit_Postfix(object __instance, object entity)
        {
            if (__instance == null) return;
            if (!CommandOverlay.Config.PreventEnemyDespawn) return;

            try
            {
                if (__instance is global::EntityLocation el)
                {
                    el.StartWithVisualRootHidden = false;
                    if (el.VisualRoot != null && !el.VisualRoot.activeSelf)
                    {
                        el.VisualRoot.SetActive(true);
                    }
                    if (el.VisibilityGroup != null && el.VisibilityGroup.alpha < 0.9f)
                    {
                        el.VisibilityGroup.alpha = 1.0f;
                    }
                }
            }
            catch { }
        }

        /// <summary>
        /// Verhindert, dass das Culling-System Ziele oder Karteneinheiten deaktiviert.
        /// </summary>
        public static bool OnApplyCulled_Prefix(object __instance, ref bool culled)
        {
            if (__instance == null) return true;
            if (!CommandOverlay.Config.PreventEnemyDespawn) return true;

            try
            {
                if (culled && __instance is global::MinimalVolumeCulling.CullTarget ct)
                {
                    if (ct.neverCull)
                    {
                        culled = false;
                        return false;
                    }

                    var el = ct.GetComponent<global::EntityLocation>();
                    if (el != null && el.Entity != null && el.Entity.IsAlive)
                    {
                        ct.neverCull = true;
                        culled = false;
                        return false;
                    }
                }
            }
            catch { }
            return true;
        }

        /// <summary>
        /// Periodischer Watchdog: Scannt und reaktiviert fälschlicherweise deaktivierte oder unsichtbare Gegner.
        /// </summary>
        public static void UpdateWatchdog(float deltaTime)
        {
            if (!CommandOverlay.Config.PreventEnemyDespawn) return;

            _watchdogTimer += deltaTime;
            if (_watchdogTimer < WatchdogInterval) return;
            _watchdogTimer = 0f;

            try
            {
                var il2cppType = Il2CppType.Of<global::EntityLocation>();
                if (il2cppType == null) return;

                var locations = UnityEngine.Object.FindObjectsOfType(il2cppType);
                if (locations == null || locations.Length == 0) return;

                int protectedCount = 0;
                for (int i = 0; i < locations.Length; i++)
                {
                    var obj = locations[i];
                    if (obj == null) continue;

                    var el = obj.TryCast<global::EntityLocation>();
                    if (el != null && el.Entity != null && el.Entity.IsAlive)
                    {
                        bool wasHidden = false;

                        if (el.VisualRoot != null && !el.VisualRoot.activeSelf)
                        {
                            el.VisualRoot.SetActive(true);
                            wasHidden = true;
                        }

                        if (el.VisibilityGroup != null && el.VisibilityGroup.alpha < 0.8f)
                        {
                            el.VisibilityGroup.alpha = 1.0f;
                            wasHidden = true;
                        }

                        if (!el.gameObject.activeSelf)
                        {
                            el.gameObject.SetActive(true);
                            wasHidden = true;
                        }

                        var cull = el.GetComponent<global::MinimalVolumeCulling.CullTarget>();
                        if (cull != null && !cull.neverCull)
                        {
                            cull.neverCull = true;
                        }

                        if (wasHidden)
                        {
                            protectedCount++;
                        }
                    }
                }

                if (protectedCount > 0)
                {
                    MelonLogger.Msg($"[EnemyDespawnGuard] 🛡️ {protectedCount} Feindeinheit(en) vor dem Despawnen gerettet und sichtbar gehalten.");
                }
            }
            catch { }
        }
    }
}
