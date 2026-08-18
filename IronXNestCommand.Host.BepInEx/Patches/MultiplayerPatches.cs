using System;
using System.Reflection;
using HarmonyLib;
using IronXNestCommand.Core.Logging;
using IronXNestCommand.Host.BepInEx.Steam;

namespace IronXNestCommand.Host.BepInEx.Patches
{
    public static class MultiplayerPatches
    {
        public static void TryApplyDynamicPatches(Harmony harmony)
        {
            ApplyCoopUIPatches(harmony);

            if (!SteamworksDetector.IsSteamAvailable) return;

            try
            {
                var steamType = Type.GetType("Steamworks.SteamMatchmaking, Steamworks.NET")
                             ?? Type.GetType("Steamworks.SteamMatchmaking, com.rlabrecque.steamworks.net");

                if (steamType != null)
                {
                    var leaveLobbyMethod = steamType.GetMethod("LeaveLobby",
                        BindingFlags.Public | BindingFlags.Static);

                    if (leaveLobbyMethod != null)
                    {
                        var postfix = typeof(MultiplayerPatches)
                            .GetMethod(nameof(LeaveLobby_Postfix),
                                BindingFlags.Static | BindingFlags.Public);

                        harmony.Patch(leaveLobbyMethod, postfix: new HarmonyMethod(postfix));
                        ModLogger.Info("[MultiplayerPatches] LeaveLobby-Patch angewendet.");
                    }
                }
            }
            catch (Exception ex)
            {
                ModLogger.Warn($"[MultiplayerPatches] Patch Fehler: {ex.Message}");
            }
        }

        private static void ApplyCoopUIPatches(Harmony harmony)
        {
            try
            {
                Type coopRunnerType = null;
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    try
                    {
                        coopRunnerType ??= asm.GetType("IronNestCoop.Core.CoopRunner");
                    }
                    catch { }
                }

                if (coopRunnerType != null)
                {
                    var prefix = typeof(MultiplayerPatches).GetMethod(nameof(SuppressDraw_Prefix), BindingFlags.Static | BindingFlags.Public);
                    var drawCoopPanel = coopRunnerType.GetMethod("DrawCoopPanel", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    var drawOptionsPanel = coopRunnerType.GetMethod("DrawOptionsPanel", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                    if (drawCoopPanel != null && prefix != null)
                    {
                        harmony.Patch(drawCoopPanel, prefix: new HarmonyMethod(prefix));
                        ModLogger.Info("[MultiplayerPatches] ✔ Standard IronNestCoop Panel erfolgreich deaktiviert (unser GUI ist aktiv).");
                    }

                    if (drawOptionsPanel != null && prefix != null)
                    {
                        harmony.Patch(drawOptionsPanel, prefix: new HarmonyMethod(prefix));
                    }
                }
            }
            catch (Exception ex)
            {
                ModLogger.Warn($"[MultiplayerPatches] Fehler beim Deaktivieren des alten Coop-Panels: {ex.Message}");
            }
        }

        public static bool SuppressDraw_Prefix()
        {
            // Verhindert das Zeichnen des alten, unvollständigen IronNestCoop Standard-Panels oben links
            return false;
        }

        public static void LeaveLobby_Postfix()
        {
            SteamworksDetector.OnLobbyLeft();
        }
    }
}

