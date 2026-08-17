using System;
using HarmonyLib;
using IronXNestCommand.Core.Logging;
using IronXNestCommand.Host.BepInEx.Core;

namespace IronXNestCommand.Host.BepInEx.Patches
{
    public static class AmmoInjectionPatch
    {
        public static void InitializePatches(Harmony harmony)
        {
            if (harmony == null) return;
            // Vorbereitet für Runtime-Custom-Shells Injection
            ModLogger.Info("[AmmoInjectionPatch] Munitions-Injektor bereit.");
        }
    }
}
