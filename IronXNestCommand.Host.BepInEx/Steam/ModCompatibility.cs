using IronXNestCommand.Core.Logging;

namespace IronXNestCommand.Host.BepInEx.Steam
{
    public static class ModCompatibility
    {
        public static bool OtherCoopModDetected { get; private set; } = false;
        public static string DetectedModName { get; private set; } = string.Empty;

        public static void CheckForOtherMods()
        {
            if (SteamworksDetector.IsIronNestCoopDetected)
            {
                OtherCoopModDetected = true;
                DetectedModName = "IronNestCoop v2.2.1";
                ModLogger.Info($"[Compatibility] Co-op Modus erkannt: {DetectedModName}. Integration scharfgeschaltet.");
            }
            else
            {
                ModLogger.Info("[Compatibility] Autonome IronX-Engine aktiv.");
            }
        }
    }
}
