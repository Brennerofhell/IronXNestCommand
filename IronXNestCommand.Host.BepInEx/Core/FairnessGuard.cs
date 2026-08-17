using System;
using IronXNestCommand.Core.Logging;

namespace IronXNestCommand.Host.BepInEx.Core
{
    public static class FairnessGuard
    {
        public static bool IsMultiplayerActive { get; private set; } = false;

        public static void SetMultiplayerState(bool active)
        {
            if (IsMultiplayerActive != active)
            {
                IsMultiplayerActive = active;
                if (active)
                {
                    ModLogger.Warn("[FairnessGuard] MULTIPLAYER ERKANNT. Cheat-Schutz scharfgeschaltet.");
                }
                else
                {
                    ModLogger.Info("[FairnessGuard] Singleplayer-Modus aktiv. Alle Boni freigeschaltet.");
                }
            }
        }

        public static bool AllowCustomShells() => !IsMultiplayerActive;
        public static bool AllowEconomicBypass() => !IsMultiplayerActive;
        public static bool AllowAdvisorOverlay() => true;
    }
}
