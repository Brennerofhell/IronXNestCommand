using System.IO;
using MelonLoader;
using MelonLoader.Utils;

namespace IronXNestCommand.Core
{
    public static class SaveManager
    {
        public static string ModDataDirectory { get; private set; } = string.Empty;

        public static void Initialize()
        {
            // Pfad: <Spielverzeichnis>/UserData/IronXNestCommand/
            ModDataDirectory = Path.Combine(MelonEnvironment.UserDataDirectory, "IronXNestCommand");

            if (!Directory.Exists(ModDataDirectory))
            {
                Directory.CreateDirectory(ModDataDirectory);
                MelonLogger.Msg($"[SaveManager] Verzeichnis erstellt: {ModDataDirectory}");
            }
        }
    }
}
