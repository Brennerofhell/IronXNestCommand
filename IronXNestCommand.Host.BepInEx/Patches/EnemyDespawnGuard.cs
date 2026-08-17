using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using IronXNestCommand.Core.Logging;
using IronXNestCommand.Host.BepInEx.Overlay;

namespace IronXNestCommand.Host.BepInEx.Patches
{
    public static class EnemyDespawnGuard
    {
        private static float _watchdogTimer = 0f;
        private const float WatchdogInterval = 1.5f;
        private static bool _patchesApplied = false;

        private static Type _entityLocationType;
        private static Type _cullTargetType;
        private static PropertyInfo _entityProp;
        private static PropertyInfo _isAliveProp;
        private static PropertyInfo _visualRootProp;
        private static PropertyInfo _visibilityGroupProp;
        private static PropertyInfo _alphaProp;
        private static PropertyInfo _startHiddenProp;
        private static PropertyInfo _neverCullProp;

        public static void InitializePatches(Harmony harmony)
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
                ModLogger.Info("[EnemyDespawnGuard] ✔ Gegner-Despawn Schutz & Culling-Guard erfolgreich aktiviert.");
            }
            catch (Exception ex)
            {
                ModLogger.Warn($"[EnemyDespawnGuard] Init Fehler: {ex.Message}");
            }
        }

        private static void TryPatchMethod(Harmony harmony, string typeName, string methodName, MethodInfo prefix = null, MethodInfo postfix = null)
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

                if (type.Name == "EntityLocation")
                {
                    _entityLocationType = type;
                    _entityProp = type.GetProperty("Entity");
                    _visualRootProp = type.GetProperty("VisualRoot");
                    _visibilityGroupProp = type.GetProperty("VisibilityGroup");
                    _startHiddenProp = type.GetProperty("StartWithVisualRootHidden");

                    var vgType = _visibilityGroupProp?.PropertyType;
                    _alphaProp = vgType?.GetProperty("alpha");

                    var entityType = _entityProp?.PropertyType;
                    _isAliveProp = entityType?.GetProperty("IsAlive");
                }
                else if (type.Name == "CullTarget")
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
                    ModLogger.Info($"[EnemyDespawnGuard] ✔ Hook aktiv: {type.Name}.{method.Name}");
                }
            }
            catch (Exception ex)
            {
                ModLogger.Warn($"[EnemyDespawnGuard] Fehler beim Patch von {typeName}.{methodName}: {ex.Message}");
            }
        }

        public static bool OnHideVisualRoot_Prefix(object __instance)
        {
            if (__instance == null) return true;
            if (!CommandOverlay.Config.PreventEnemyDespawn) return true;

            try
            {
                if (_entityProp != null)
                {
                    var entity = _entityProp.GetValue(__instance);
                    if (entity != null)
                    {
                        bool isAlive = _isAliveProp != null ? (bool)(_isAliveProp.GetValue(entity) ?? true) : true;
                        if (isAlive)
                        {
                            var visualRoot = _visualRootProp?.GetValue(__instance) as GameObject;
                            if (visualRoot != null && !visualRoot.activeSelf)
                            {
                                visualRoot.SetActive(true);
                            }

                            var vg = _visibilityGroupProp?.GetValue(__instance);
                            if (vg != null && _alphaProp != null)
                            {
                                _alphaProp.SetValue(vg, 1.0f);
                            }

                            return false; // Skip HideVisualRoot!
                        }
                    }
                }
            }
            catch { }
            return true;
        }

        public static void OnInit_Postfix(object __instance, object entity)
        {
            if (__instance == null) return;
            if (!CommandOverlay.Config.PreventEnemyDespawn) return;

            try
            {
                if (_startHiddenProp != null)
                {
                    _startHiddenProp.SetValue(__instance, false);
                }

                var visualRoot = _visualRootProp?.GetValue(__instance) as GameObject;
                if (visualRoot != null && !visualRoot.activeSelf)
                {
                    visualRoot.SetActive(true);
                }

                var vg = _visibilityGroupProp?.GetValue(__instance);
                if (vg != null && _alphaProp != null)
                {
                    _alphaProp.SetValue(vg, 1.0f);
                }
            }
            catch { }
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

                    if (__instance is Component comp)
                    {
                        if (_entityLocationType != null)
                        {
                            var el = comp.GetComponent(Il2CppInterop.Runtime.Il2CppType.From(_entityLocationType));
                            if (el != null)
                            {
                                _neverCullProp.SetValue(__instance, true);
                                culled = false;
                                return false;
                            }
                        }
                    }
                }
            }
            catch { }
            return true;
        }

        public static void UpdateWatchdog(float deltaTime)
        {
            if (!CommandOverlay.Config.PreventEnemyDespawn) return;

            _watchdogTimer += deltaTime;
            if (_watchdogTimer < WatchdogInterval) return;
            _watchdogTimer = 0f;

            try
            {
                if (_entityLocationType == null) return;
                var il2cppType = Il2CppInterop.Runtime.Il2CppType.From(_entityLocationType);
                if (il2cppType == null) return;

                var locations = UnityEngine.Object.FindObjectsOfType(il2cppType);
                if (locations == null || locations.Length == 0) return;

                int protectedCount = 0;
                for (int i = 0; i < locations.Length; i++)
                {
                    var el = locations[i];
                    if (el == null) continue;

                    var entity = _entityProp?.GetValue(el);
                    if (entity != null)
                    {
                        bool isAlive = _isAliveProp != null ? (bool)(_isAliveProp.GetValue(entity) ?? true) : true;
                        if (isAlive)
                        {
                            bool wasHidden = false;
                            var visualRoot = _visualRootProp?.GetValue(el) as GameObject;
                            if (visualRoot != null && !visualRoot.activeSelf)
                            {
                                visualRoot.SetActive(true);
                                wasHidden = true;
                            }

                            var vg = _visibilityGroupProp?.GetValue(el);
                            if (vg != null && _alphaProp != null)
                            {
                                float a = (float)(_alphaProp.GetValue(vg) ?? 1f);
                                if (a < 0.8f)
                                {
                                    _alphaProp.SetValue(vg, 1f);
                                    wasHidden = true;
                                }
                            }

                            if (el is Component comp && comp.gameObject != null && !comp.gameObject.activeSelf)
                            {
                                comp.gameObject.SetActive(true);
                                wasHidden = true;
                            }

                            if (wasHidden)
                            {
                                protectedCount++;
                            }
                        }
                    }
                }

                if (protectedCount > 0)
                {
                    ModLogger.Info($"[EnemyDespawnGuard] 🛡️ {protectedCount} Feindeinheit(en) vor dem Despawnen gerettet und sichtbar gehalten.");
                }
            }
            catch { }
        }
    }
}
