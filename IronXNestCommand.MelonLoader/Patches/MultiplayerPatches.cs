using HarmonyLib;
using MelonLoader;
using IronXNestCommand.Core;
using IronXNestCommand.Steam;

namespace IronXNestCommand.Patches
{
    /// <summary>
    /// Harmony-Patches zur Erkennung von Steamworks Lobby-Events.
    /// Da Steamworks.NET per Reflection eingebunden ist, patchen wir den
    /// Spiel-eigenen NetworkManager (falls vorhanden) als primäre Quelle.
    /// </summary>
    public static class MultiplayerPatches
    {
        // ── LeaveLobby Patch (Steamworks.NET — wird aktiviert sobald der Typ bekannt ist) ──
        // Dieser Patch wird zur Laufzeit dynamisch via Harmony angewendet,
        // sobald SteamworksDetector.IsSteamAvailable == true ist.
        // Direkter Patch ohne Compile-Zeit-Abhängigkeit:

        /// <summary>
        /// Versucht den LeaveLobby-Patch auf Steamworks.SteamMatchmaking anzuwenden.
        /// Aufgerufen aus Main.OnInitializeMelon() nach SteamworksDetector.Initialize().
        /// </summary>
        public static void TryApplyDynamicPatches(HarmonyLib.Harmony harmony)
        {
            ApplyCoopUIPatches(harmony);

            if (!SteamworksDetector.IsSteamAvailable)
            {
                MelonLogger.Msg("[MultiplayerPatches] Steam nicht verfügbar — dynamische Patches übersprungen.");
                return;
            }

            try
            {
                // Patch auf SteamMatchmaking.LeaveLobby
                var steamType = System.Type.GetType("Steamworks.SteamMatchmaking, Steamworks.NET")
                             ?? FindTypeInAllAssemblies("Steamworks.SteamMatchmaking");

                if (steamType == null)
                {
                    MelonLogger.Warning("[MultiplayerPatches] Steamworks.SteamMatchmaking nicht gefunden.");
                    return;
                }

                var leaveLobbyMethod = steamType.GetMethod("LeaveLobby",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

                if (leaveLobbyMethod != null)
                {
                    var postfix = typeof(MultiplayerPatches)
                        .GetMethod(nameof(LeaveLobby_Postfix),
                            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);

                    harmony.Patch(leaveLobbyMethod,
                        postfix: new HarmonyMethod(postfix));

                    MelonLogger.Msg("[MultiplayerPatches] LeaveLobby-Patch angewendet.");
                }
            }
            catch (System.Exception ex)
            {
                MelonLogger.Warning($"[MultiplayerPatches] Dynamischer Patch fehlgeschlagen: {ex.Message}");
            }
        }

        private static void ApplyCoopUIPatches(HarmonyLib.Harmony harmony)
        {
            try
            {
                System.Type coopRunnerType = FindTypeInAllAssemblies("IronNestCoop.Core.CoopRunner");
                if (coopRunnerType != null)
                {
                    var prefix = typeof(MultiplayerPatches).GetMethod(nameof(SuppressDraw_Prefix), System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
                    var drawCoopPanel = coopRunnerType.GetMethod("DrawCoopPanel", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    var drawOptionsPanel = coopRunnerType.GetMethod("DrawOptionsPanel", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                    if (drawCoopPanel != null && prefix != null)
                    {
                        harmony.Patch(drawCoopPanel, prefix: new HarmonyMethod(prefix));
                        MelonLogger.Msg("[MultiplayerPatches] ✔ Standard IronNestCoop Panel erfolgreich deaktiviert (unser GUI ist aktiv).");
                    }

                    if (drawOptionsPanel != null && prefix != null)
                    {
                        harmony.Patch(drawOptionsPanel, prefix: new HarmonyMethod(prefix));
                    }
                }
            }
            catch (System.Exception ex)
            {
                MelonLogger.Warning($"[MultiplayerPatches] Fehler beim Deaktivieren des alten Coop-Panels: {ex.Message}");
            }
        }

        public static bool SuppressDraw_Prefix()
        {
            // Verhindert das Zeichnen des alten, unvollständigen IronNestCoop Standard-Panels oben links
            return false;
        }

        // Wird von Harmony aufgerufen wenn LeaveLobby() im Spiel ausgeführt wird
        public static void LeaveLobby_Postfix()
        {
            MelonLogger.Msg("[MultiplayerPatches] LeaveLobby() erkannt → Lobby-Zustand zurücksetzen.");
            SteamworksDetector.OnLobbyLeft();
        }

        // ── Generischer NetworkManager-Patch (Spiel-spezifisch, als Fallback) ──
        public static class NetworkManager_OnJoinedLobby_Patch
        {
            public static void Postfix()
            {
                MelonLogger.Msg("[MultiplayerPatches] Lobby beigetreten (NetworkManager).");
                FairnessGuard.SetMultiplayerState(true);
            }
        }

        public static class NetworkManager_OnLeftLobby_Patch
        {
            public static void Postfix()
            {
                MelonLogger.Msg("[MultiplayerPatches] Lobby verlassen (NetworkManager).");
                SteamworksDetector.OnLobbyLeft();
            }
        }

        private static System.Type FindTypeInAllAssemblies(string typeName)
        {
            foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var t = asm.GetType(typeName);
                    if (t != null) return t;
                }
                catch { }
            }
            return null;
        }
    }
}
