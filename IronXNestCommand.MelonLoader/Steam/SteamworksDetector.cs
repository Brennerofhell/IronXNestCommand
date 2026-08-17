using System;
using System.Collections.Generic;
using MelonLoader;
using IronXNestCommand.Core;

namespace IronXNestCommand.Steam
{
    /// <summary>
    /// Erkennt Multiplayer-Sitzungen und Mitspieler direkt über Steamworks.
    /// Nutzt defensiven Code / Reflection oder direkte Steamworks.NET Aufrufe, falls verfügbar.
    /// </summary>
    public static class SteamworksDetector
    {
        public static bool IsInLobby { get; private set; } = false;
        public static ulong CurrentLobbyId { get; private set; } = 0;
        public static List<string> ConnectedPlayers { get; private set; } = new List<string>();

        private static float _pollTimer = 0f;
        private const float PollInterval = 2.0f; // Alle 2 Sekunden prüfen

        public static void Initialize()
        {
            MelonLogger.Msg("[SteamworksDetector] Initialisiere Steamworks Multiplayer-Erkennung...");
            CheckSteamState();
        }

        /// <summary>
        /// Wird periodisch im OnUpdate aufgerufen, um Lobby-Status & Mitspieler abzufragen.
        /// </summary>
        public static void Update(float deltaTime)
        {
            _pollTimer += deltaTime;
            if (_pollTimer < PollInterval)
                return;

            _pollTimer = 0f;
            CheckSteamState();
        }

        public static void CheckSteamState()
        {
            try
            {
                // Versuche SteamMatchmaking / SteamFriends Methoden über Reflection oder Direktaufruf zu prüfen
                // Dies stellt sicher, dass die Mod auch läuft, wenn Steamworks.NET als DLL zur Laufzeit geladen wird.
                Type steamMatchmakingType = Type.GetType("Steamworks.SteamMatchmaking, Steamworks.NET") 
                                         ?? Type.GetType("Steamworks.SteamMatchmaking, Assembly-CSharp-firstpass")
                                         ?? Type.GetType("Steamworks.SteamMatchmaking");

                Type steamFriendsType = Type.GetType("Steamworks.SteamFriends, Steamworks.NET")
                                     ?? Type.GetType("Steamworks.SteamFriends, Assembly-CSharp-firstpass")
                                     ?? Type.GetType("Steamworks.SteamFriends");

                if (steamMatchmakingType == null)
                {
                    // Steamworks.NET nicht direkt auffindbar (z.B. standalone/offline oder anderer Namespace)
                    return;
                }

                // Wenn wir eine gültige Lobby-Prüfung durchführen können:
                // Im echten Steamworks-Umfeld kann hier die aktive Lobby abgefragt werden.
                // Sobald mehr als 1 Mitglied in einer Lobby ist:
                // FairnessGuard.SetMultiplayerState(true);
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"[SteamworksDetector] Fehler bei der Steam-Prüfung: {ex.Message}");
            }
        }

        /// <summary>
        /// Wird aufgerufen, wenn ein Spieler einer Steam-Lobby beitritt oder eine erstellt.
        /// </summary>
        public static void OnLobbyEntered(ulong lobbyId, int memberCount)
        {
            CurrentLobbyId = lobbyId;
            IsInLobby = true;
            ConnectedPlayers.Clear();

            MelonLogger.Msg($"[SteamworksDetector] Steam-Lobby erkannt (ID: {lobbyId}, Mitglieder: {memberCount})");

            if (memberCount > 1)
            {
                MelonLogger.Warning("[SteamworksDetector] Mehrere Spieler in Steam-Lobby -> Multiplayer aktiv!");
                FairnessGuard.SetMultiplayerState(true);
            }
            else
            {
                // Alleine in der Lobby (z.B. beim Vorbereiten)
                FairnessGuard.SetMultiplayerState(false);
            }
        }

        /// <summary>
        /// Wird aufgerufen, wenn die Steam-Lobby verlassen wird.
        /// </summary>
        public static void OnLobbyLeft()
        {
            CurrentLobbyId = 0;
            IsInLobby = false;
            ConnectedPlayers.Clear();

            MelonLogger.Msg("[SteamworksDetector] Steam-Lobby verlassen -> Zurück im Einzelspieler-Modus.");
            FairnessGuard.SetMultiplayerState(false);
        }

        /// <summary>
        /// Aktualisiert die Liste der erkannten Mitspieler.
        /// </summary>
        public static void UpdateLobbyMembers(IEnumerable<string> memberNames)
        {
            ConnectedPlayers.Clear();
            ConnectedPlayers.AddRange(memberNames);

            MelonLogger.Msg($"[SteamworksDetector] Aktuelle Mitspieler in Lobby ({ConnectedPlayers.Count}): {string.Join(", ", ConnectedPlayers)}");
            
            if (ConnectedPlayers.Count > 1)
            {
                FairnessGuard.SetMultiplayerState(true);
            }
        }
    }
}
