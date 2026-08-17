using MelonLoader;

namespace IronXNestCommand.Steam
{
    public static class ModCompatibility
    {
        public static bool OtherCoopModDetected { get; private set; } = false;
        public static string DetectedModName { get; private set; } = string.Empty;

        public static void CheckForOtherMods()
        {
            var registeredMods = MelonMod.RegisteredMelons;

            foreach (var mod in registeredMods)
            {
                string name = mod.Info.Name.ToLower();

                // Bekannte Co-op Mods prüfen
                if (name.Contains("open nest") || name.Contains("synchrony") || name.Contains("iron nest co-op"))
                {
                    OtherCoopModDetected = true;
                    DetectedModName = mod.Info.Name;
                    MelonLogger.Warning($"[Compatibility] Koexistierende Mod erkannt: {DetectedModName}. Eigener P2P-Host wird deaktiviert (Kompatibilitätsmodus).");
                    break;
                }
            }

            if (!OtherCoopModDetected)
            {
                MelonLogger.Msg("[Compatibility] Keine konkurrierenden Co-op-Mods aktiv. Eigenes Steamworks-P2P bereit.");
            }
        }
    }
}
