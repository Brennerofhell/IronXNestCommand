using HarmonyLib;
using MelonLoader;
using IronXNestCommand.Core;
using IronXNestCommand.Steam;

namespace IronXNestCommand.Patches
{
    /// <summary>
    /// Diese Klasse enthält Harmony-Patches zur Erkennung von Multiplayer-Lobbys (z.B. über Steamworks).
    /// </summary>
    public static class MultiplayerPatches
    {
        // Beispiel-Patch für Steamworks.NET SteamMatchmaking
        // Sobald Steamworks.NET im Spiel eingebunden ist, kann dieser Patch direkt aktiv werden:
        /*
        [HarmonyPatch(typeof(Steamworks.SteamMatchmaking), "LeaveLobby")]
        public static class SteamMatchmaking_LeaveLobby_Patch
        {
            public static void Postfix()
            {
                SteamworksDetector.OnLobbyLeft();
            }
        }
        */

        // Platzhalter für generische Netzwerk-/Lobby-Manager des Spiels
        public static class NetworkManager_OnJoinedLobby_Patch
        {
            public static void Postfix()
            {
                MelonLogger.Msg("[MultiplayerPatches] Lobby beigetreten.");
                FairnessGuard.SetMultiplayerState(true);
            }
        }

        public static class NetworkManager_OnLeftLobby_Patch
        {
            public static void Postfix()
            {
                MelonLogger.Msg("[MultiplayerPatches] Lobby verlassen.");
                FairnessGuard.SetMultiplayerState(false);
            }
        }
    }
}
