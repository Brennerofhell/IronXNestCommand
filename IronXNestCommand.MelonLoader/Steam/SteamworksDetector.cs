using System;
using System.Collections.Generic;
using System.Reflection;
using MelonLoader;
using IronXNestCommand.Core;

namespace IronXNestCommand.Steam
{
    /// <summary>
    /// Verwaltet Steam-Lobbys (Erstellen, Beitreten, Verlassen) über Reflection auf SteamNet (IronNestCoop) oder Steamworks.NET.
    /// </summary>
    public static class SteamworksDetector
    {
        public static bool IsInLobby { get; private set; } = false;
        public static ulong CurrentLobbyId { get; private set; } = 0;
        public static string CurrentLobbyShort { get; private set; } = "";
        public static bool IsSteamAvailable { get; private set; } = false;
        public static bool IsIronNestCoopDetected { get; private set; } = false;
        public static List<string> ConnectedPlayers { get; private set; } = new();
        public static string LastStatusMessage { get; private set; } = "Nicht initialisiert";

        // IronNestCoop Reflection Cache
        private static Type _coopSteamNetType;
        private static MethodInfo _coopCreateLobby;
        private static MethodInfo _coopResolveLobbyId;
        private static MethodInfo _coopJoinLobbyById;
        private static MethodInfo _coopLeaveLobby;
        private static MethodInfo _coopGetPeerName;
        private static PropertyInfo _coopInLobby;
        private static PropertyInfo _coopCurrentLobbyId;
        private static PropertyInfo _coopCurrentLobbyShort;
        private static PropertyInfo _coopConnectedPeers;
        private static PropertyInfo _coopLocalPlayerName;

        // Generic Steamworks Reflection Cache
        private static Type _steamMatchmakingType;
        private static Type _steamFriendsType;
        private static Type _steamUserType;
        private static Type _cSteamIDType;
        private static MethodInfo _mCreateLobby;
        private static MethodInfo _mJoinLobby;
        private static MethodInfo _mLeaveLobby;
        private static MethodInfo _mGetNumLobbyMembers;
        private static MethodInfo _mGetLobbyMemberByIndex;
        private static MethodInfo _mGetFriendPersonaName;

        private static float _pollTimer = 0f;
        private const float PollInterval = 2.0f;

        public static void Initialize()
        {
            MelonLogger.Msg("[SteamworksDetector] Initialisiere Steam-Lobby-System...");
            ResolveTypes();

            if (IsIronNestCoopDetected)
                MelonLogger.Msg("[SteamworksDetector] ✔ IronNestCoop SteamNet erfolgreich angebunden.");
            else if (IsSteamAvailable)
                MelonLogger.Msg("[SteamworksDetector] ✔ Generic Steamworks.NET angebunden.");
            else
                MelonLogger.Warning("[SteamworksDetector] Kein Steamworks Modul im Speicher gefunden — Standalone-Modus aktiv.");

            CheckSteamState();
        }

        public static void Update(float deltaTime)
        {
            _pollTimer += deltaTime;
            if (_pollTimer < PollInterval) return;
            _pollTimer = 0f;
            CheckSteamState();
        }

        private static void ResolveTypes()
        {
            // 1. Suche nach IronNestCoop.Core.Net.SteamNet
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    _coopSteamNetType ??= asm.GetType("IronNestCoop.Core.Net.SteamNet");
                }
                catch { }
            }

            if (_coopSteamNetType != null)
            {
                IsIronNestCoopDetected = true;
                IsSteamAvailable = true;

                _coopCreateLobby = _coopSteamNetType.GetMethod("CreateLobby", BindingFlags.Public | BindingFlags.Static);
                _coopResolveLobbyId = _coopSteamNetType.GetMethod("ResolveLobbyId", BindingFlags.Public | BindingFlags.Static);
                _coopJoinLobbyById = _coopSteamNetType.GetMethod("JoinLobbyById", BindingFlags.Public | BindingFlags.Static);
                _coopLeaveLobby = _coopSteamNetType.GetMethod("LeaveLobby", BindingFlags.Public | BindingFlags.Static);
                _coopGetPeerName = _coopSteamNetType.GetMethod("GetPeerName", BindingFlags.Public | BindingFlags.Static);

                _coopInLobby = _coopSteamNetType.GetProperty("InLobby", BindingFlags.Public | BindingFlags.Static);
                _coopCurrentLobbyId = _coopSteamNetType.GetProperty("CurrentLobbyId", BindingFlags.Public | BindingFlags.Static);
                _coopCurrentLobbyShort = _coopSteamNetType.GetProperty("CurrentLobbyShort", BindingFlags.Public | BindingFlags.Static);
                _coopConnectedPeers = _coopSteamNetType.GetProperty("ConnectedPeers", BindingFlags.Public | BindingFlags.Static);
                _coopLocalPlayerName = _coopSteamNetType.GetProperty("LocalPlayerName", BindingFlags.Public | BindingFlags.Static);

                LastStatusMessage = "IronNestCoop verbunden (Bereit)";
                return;
            }

            // 2. Fallback: Generic Steamworks
            string[] steamAssemblies = { "Steamworks.NET", "Assembly-CSharp-firstpass", "Assembly-CSharp", "com.rlabrecque.steamworks.net" };
            foreach (var asmName in steamAssemblies)
            {
                _steamMatchmakingType ??= Type.GetType($"Steamworks.SteamMatchmaking, {asmName}");
                _steamFriendsType ??= Type.GetType($"Steamworks.SteamFriends, {asmName}");
                _steamUserType ??= Type.GetType($"Steamworks.SteamUser, {asmName}");
                _cSteamIDType ??= Type.GetType($"Steamworks.CSteamID, {asmName}");
            }

            if (_steamMatchmakingType == null)
            {
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    try
                    {
                        _steamMatchmakingType ??= asm.GetType("Steamworks.SteamMatchmaking");
                        _steamFriendsType ??= asm.GetType("Steamworks.SteamFriends");
                        _steamUserType ??= asm.GetType("Steamworks.SteamUser");
                        _cSteamIDType ??= asm.GetType("Steamworks.CSteamID");
                    }
                    catch { }
                }
            }

            if (_steamMatchmakingType != null)
            {
                IsSteamAvailable = true;
                _mCreateLobby = _steamMatchmakingType.GetMethod("CreateLobby", BindingFlags.Public | BindingFlags.Static);
                _mJoinLobby = _steamMatchmakingType.GetMethod("JoinLobby", BindingFlags.Public | BindingFlags.Static);
                _mLeaveLobby = _steamMatchmakingType.GetMethod("LeaveLobby", BindingFlags.Public | BindingFlags.Static);
                _mGetNumLobbyMembers = _steamMatchmakingType.GetMethod("GetNumLobbyMembers", BindingFlags.Public | BindingFlags.Static);
                _mGetLobbyMemberByIndex = _steamMatchmakingType.GetMethod("GetLobbyMemberByIndex", BindingFlags.Public | BindingFlags.Static);

                if (_steamFriendsType != null)
                    _mGetFriendPersonaName = _steamFriendsType.GetMethod("GetFriendPersonaName", BindingFlags.Public | BindingFlags.Static);

                LastStatusMessage = "Steamworks.NET bereit";
            }
        }

        private static void CheckSteamState()
        {
            if (IsIronNestCoopDetected && _coopInLobby != null)
            {
                try
                {
                    IsInLobby = (bool)(_coopInLobby.GetValue(null) ?? false);
                    CurrentLobbyId = IsInLobby && _coopCurrentLobbyId != null ? (ulong)(_coopCurrentLobbyId.GetValue(null) ?? 0UL) : 0UL;
                    CurrentLobbyShort = IsInLobby && _coopCurrentLobbyShort != null ? (string)(_coopCurrentLobbyShort.GetValue(null) ?? "") : "";

                    ConnectedPlayers.Clear();
                    if (IsInLobby)
                    {
                        string localName = _coopLocalPlayerName != null ? (string)_coopLocalPlayerName.GetValue(null) : "Host";
                        if (!string.IsNullOrEmpty(localName))
                            ConnectedPlayers.Add(localName);

                        if (_coopConnectedPeers != null && _coopGetPeerName != null)
                        {
                            var peers = _coopConnectedPeers.GetValue(null) as System.Collections.IEnumerable;
                            if (peers != null)
                            {
                                foreach (var peer in peers)
                                {
                                    if (peer is ulong peerId && peerId != 0)
                                    {
                                        string peerName = _coopGetPeerName.Invoke(null, new object[] { peerId }) as string;
                                        if (!string.IsNullOrEmpty(peerName) && !ConnectedPlayers.Contains(peerName))
                                            ConnectedPlayers.Add(peerName);
                                    }
                                }
                            }
                        }

                        LastStatusMessage = $"Aktiv in Lobby [{CurrentLobbyShort}]";
                        FairnessGuard.SetMultiplayerState(true);
                    }
                    else
                    {
                        LastStatusMessage = "Keine aktive Lobby";
                    }
                    return;
                }
                catch (Exception ex)
                {
                    MelonLogger.Warning($"[SteamworksDetector] Fehler beim Abruf von IronNestCoop: {ex.Message}");
                }
            }

            if (!IsSteamAvailable)
            {
                LastStatusMessage = "Steamworks nicht verfügbar (Offline)";
                return;
            }

            if (IsInLobby && CurrentLobbyId != 0)
            {
                UpdateConnectedPlayersGeneric();
            }
        }

        private static void UpdateConnectedPlayersGeneric()
        {
            try
            {
                if (_mGetNumLobbyMembers == null || _cSteamIDType == null) return;
                object lobbyIdBoxed = Activator.CreateInstance(_cSteamIDType, CurrentLobbyId);
                int count = (int)_mGetNumLobbyMembers.Invoke(null, new[] { lobbyIdBoxed });

                ConnectedPlayers.Clear();
                for (int i = 0; i < count; i++)
                {
                    if (_mGetLobbyMemberByIndex != null && _mGetFriendPersonaName != null)
                    {
                        object memberId = _mGetLobbyMemberByIndex.Invoke(null, new[] { lobbyIdBoxed, (object)i });
                        if (memberId != null)
                        {
                            string name = (string)_mGetFriendPersonaName.Invoke(null, new[] { memberId });
                            if (!string.IsNullOrEmpty(name))
                                ConnectedPlayers.Add(name);
                        }
                    }
                }
            }
            catch { }
        }

        public static void TryCreateLobby(int maxPlayers = 4)
        {
            if (IsIronNestCoopDetected && _coopCreateLobby != null)
            {
                try
                {
                    _coopCreateLobby.Invoke(null, null);
                    LastStatusMessage = "⏳ Erstelle IronNestCoop-Lobby...";
                    MelonLogger.Msg("[SteamworksDetector] IronNestCoop CreateLobby aufgerufen.");
                    return;
                }
                catch (Exception ex)
                {
                    LastStatusMessage = $"❌ Fehler: {ex.Message}";
                    return;
                }
            }

            if (!IsSteamAvailable || _mCreateLobby == null)
            {
                LastStatusMessage = "❌ Steamworks nicht verfügbar.";
                return;
            }

            try
            {
                _mCreateLobby.Invoke(null, new object[] { 2, maxPlayers });
                LastStatusMessage = $"⏳ Erstelle Lobby für {maxPlayers} Spieler...";
            }
            catch (Exception ex)
            {
                LastStatusMessage = $"❌ Fehler beim Erstellen: {ex.Message}";
            }
        }

        public static void TryJoinLobby(string codeOrId)
        {
            if (string.IsNullOrWhiteSpace(codeOrId))
            {
                LastStatusMessage = "❌ Bitte Lobby-Code oder ID eingeben.";
                return;
            }

            string cleanCode = codeOrId.Trim();

            if (IsIronNestCoopDetected)
            {
                try
                {
                    ulong lobbyId = 0;
                    if (_coopResolveLobbyId != null)
                    {
                        try
                        {
                            lobbyId = (ulong)_coopResolveLobbyId.Invoke(null, new object[] { cleanCode });
                        }
                        catch { }
                    }

                    if (lobbyId == 0 && ulong.TryParse(cleanCode, out ulong parsedId))
                    {
                        lobbyId = parsedId;
                    }

                    if (lobbyId != 0 && _coopJoinLobbyById != null)
                    {
                        _coopJoinLobbyById.Invoke(null, new object[] { lobbyId });
                        LastStatusMessage = $"⏳ Trete Lobby '{cleanCode}' bei...";
                        MelonLogger.Msg($"[SteamworksDetector] IronNestCoop JoinLobbyById aufgerufen (ID: {lobbyId}).");
                        return;
                    }
                    else if (lobbyId == 0)
                    {
                        LastStatusMessage = $"❌ Ungültiger Lobby-Code '{cleanCode}'.";
                        return;
                    }
                }
                catch (Exception ex)
                {
                    LastStatusMessage = $"❌ Fehler: {ex.Message}";
                    return;
                }
            }

            if (ulong.TryParse(cleanCode, out ulong id))
            {
                TryJoinLobbyUlong(id);
            }
            else
            {
                LastStatusMessage = "❌ Ungültige 64-Bit Steam-Lobby-ID.";
            }
        }

        private static void TryJoinLobbyUlong(ulong lobbyId)
        {
            if (!IsSteamAvailable || _mJoinLobby == null)
            {
                LastStatusMessage = "❌ Steam nicht verfügbar.";
                return;
            }

            try
            {
                object lobbyIdBoxed = Activator.CreateInstance(_cSteamIDType, lobbyId);
                _mJoinLobby.Invoke(null, new[] { lobbyIdBoxed });
                LastStatusMessage = $"⏳ Trete Lobby {lobbyId} bei...";
            }
            catch (Exception ex)
            {
                LastStatusMessage = $"❌ Fehler beim Beitreten: {ex.Message}";
            }
        }

        public static void TryLeaveLobby()
        {
            if (IsIronNestCoopDetected && _coopLeaveLobby != null)
            {
                try
                {
                    _coopLeaveLobby.Invoke(null, null);
                    IsInLobby = false;
                    CurrentLobbyId = 0;
                    CurrentLobbyShort = "";
                    ConnectedPlayers.Clear();
                    LastStatusMessage = "Lobby verlassen.";
                    FairnessGuard.SetMultiplayerState(false);
                    return;
                }
                catch { }
            }

            if (!IsSteamAvailable || _mLeaveLobby == null || CurrentLobbyId == 0) return;

            try
            {
                object lobbyIdBoxed = Activator.CreateInstance(_cSteamIDType, CurrentLobbyId);
                _mLeaveLobby.Invoke(null, new[] { lobbyIdBoxed });
                IsInLobby = false;
                CurrentLobbyId = 0;
                ConnectedPlayers.Clear();
                LastStatusMessage = "Lobby verlassen.";
                FairnessGuard.SetMultiplayerState(false);
            }
            catch { }
        }

        public static void OnLobbyLeft()
        {
            TryLeaveLobby();
        }

        public static bool TryOpenInviteOverlay()
        {
            try
            {
                Type steamFriends = _steamFriendsType;
                if (steamFriends != null)
                {
                    var mActivateGameOverlay = steamFriends.GetMethod("ActivateGameOverlay", BindingFlags.Public | BindingFlags.Static);
                    if (mActivateGameOverlay != null)
                    {
                        mActivateGameOverlay.Invoke(null, new object[] { "LobbyInvite" });
                        return true;
                    }
                }
            }
            catch { }
            return false;
        }
    }
}
