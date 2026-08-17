using HarmonyLib;
using MelonLoader;
using IronXNestCommand.Core;

namespace IronXNestCommand.Patches
{
    /// <summary>
    /// Diese Klasse enthält Harmony-Patches zur Erkennung von Multiplayer-Lobbys.
    /// Da die internen Klassennamen noch unbekannt sind, sind dies Platzhalter.
    /// </summary>
    public static class MultiplayerPatches
    {
        // TODO: Ersetze 'NetworkManager' durch die echte Klasse (z.B. SteamMatchmaking, PhotonNetwork).
        // [HarmonyPatch(typeof(NetworkManager), "OnJoinedLobby")] 
        public static class NetworkManager_OnJoinedLobby_Patch
        {
            public static void Postfix()
            {
                MelonLogger.Msg("[MultiplayerPatches] Lobby beigetreten.");
                FairnessGuard.SetMultiplayerState(true);
            }
        }

        // [HarmonyPatch(typeof(NetworkManager), "OnLeftLobby")] 
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
