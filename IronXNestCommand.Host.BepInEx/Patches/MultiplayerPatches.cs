using HarmonyLib;
using IronXNestCommand.Core.Logging;
using IronXNestCommand.Host.BepInEx.Core;
using IronXNestCommand.Host.BepInEx.Steam;

namespace IronXNestCommand.Host.BepInEx.Patches
{
    public static class MultiplayerPatches
    {
        public static void TryApplyDynamicPatches(Harmony harmony)
        {
            if (!SteamworksDetector.IsSteamAvailable) return;

            try
            {
                var steamType = System.Type.GetType("Steamworks.SteamMatchmaking, Steamworks.NET")
                             ?? System.Type.GetType("Steamworks.SteamMatchmaking, com.rlabrecque.steamworks.net");

                if (steamType != null)
                {
                    var leaveLobbyMethod = steamType.GetMethod("LeaveLobby",
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

                    if (leaveLobbyMethod != null)
                    {
                        var postfix = typeof(MultiplayerPatches)
                            .GetMethod(nameof(LeaveLobby_Postfix),
                                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);

                        harmony.Patch(leaveLobbyMethod, postfix: new HarmonyMethod(postfix));
                        ModLogger.Info("[MultiplayerPatches] LeaveLobby-Patch angewendet.");
                    }
                }
            }
            catch (System.Exception ex)
            {
                ModLogger.Warn($"[MultiplayerPatches] Patch Fehler: {ex.Message}");
            }
        }

        public static void LeaveLobby_Postfix()
        {
            SteamworksDetector.OnLobbyLeft();
        }
    }
}
