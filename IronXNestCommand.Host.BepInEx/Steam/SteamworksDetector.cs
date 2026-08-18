using System;
using System.Collections.Generic;
using System.Reflection;
using IronXNestCommand.Core.Logging;
using IronXNestCommand.Host.BepInEx.Core;

namespace IronXNestCommand.Host.BepInEx.Steam
{
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
        private static PropertyInfo _coopPanelStatus;
        private static PropertyInfo _coopSteamReady;
        private static FieldInfo _coopPipeField;
        private static MethodInfo _coopActivateInviteDialog;

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
            ModLogger.Info("[SteamworksDetector] Initialisiere Steamworks & IronNestCoop Erkennung...");
            ResolveTypes();

            if (IsIronNestCoopDetected)
                ModLogger.Info("[SteamworksDetector] ★ IronNestCoop Bridge aktiv! Direkte Synchronisation mit Co-op Modus.");
            else if (IsSteamAvailable)
                ModLogger.Info("[SteamworksDetector] Steamworks.NET erkannt — Autonomer Lobby-Modus bereit.");
            else
                ModLogger.Warn("[SteamworksDetector] Weder IronNestCoop noch Steamworks.NET gefunden — Stub-Modus aktiv.");

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
                _coopPanelStatus = _coopSteamNetType.GetProperty("PanelStatus", BindingFlags.Public | BindingFlags.Static);
                _coopSteamReady = _coopSteamNetType.GetProperty("SteamReady", BindingFlags.Public | BindingFlags.Static);

                _coopPipeField = _coopSteamNetType.GetField("_pipe", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
                if (_coopPipeField != null)
                {
                    var pipeType = _coopPipeField.FieldType;
                    _coopActivateInviteDialog = pipeType.GetMethod("ActivateInviteDialog", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                }

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

            IsSteamAvailable = _steamMatchmakingType != null && _cSteamIDType != null;

            if (IsSteamAvailable)
            {
                _mCreateLobby = _steamMatchmakingType.GetMethod("CreateLobby", BindingFlags.Public | BindingFlags.Static);
                _mJoinLobby = _steamMatchmakingType.GetMethod("JoinLobby", BindingFlags.Public | BindingFlags.Static);
                _mLeaveLobby = _steamMatchmakingType.GetMethod("LeaveLobby", BindingFlags.Public | BindingFlags.Static);
                _mGetNumLobbyMembers = _steamMatchmakingType.GetMethod("GetNumLobbyMembers", BindingFlags.Public | BindingFlags.Static);
                _mGetLobbyMemberByIndex = _steamMatchmakingType.GetMethod("GetLobbyMemberByIndex", BindingFlags.Public | BindingFlags.Static);

                if (_steamFriendsType != null)
                {
                    _mGetFriendPersonaName = _steamFriendsType.GetMethod("GetFriendPersonaName", BindingFlags.Public | BindingFlags.Static);
                    _mActivateGameOverlayInviteDialog = _steamFriendsType.GetMethod("ActivateGameOverlayInviteDialog", BindingFlags.Public | BindingFlags.Static)
                                                     ?? _steamFriendsType.GetMethod("ActivateGameOverlay", BindingFlags.Public | BindingFlags.Static);
                }

                LastStatusMessage = "Steamworks bereit";
            }
        }

        private static MethodInfo _mActivateGameOverlayInviteDialog;

        public static bool TryOpenInviteOverlay()
        {
            if (CurrentLobbyId == 0)
            {
                CheckSteamState();
            }

            ulong lobbyId = CurrentLobbyId;
            if (lobbyId == 0 && _coopCurrentLobbyId != null)
            {
                try { lobbyId = (ulong)_coopCurrentLobbyId.GetValue(null); } catch { }
            }

            if (lobbyId == 0) return false;

            // 1. Primärer Pfad: Über IronNestCoop SteamPipe
            if (_coopPipeField != null && _coopActivateInviteDialog != null)
            {
                try
                {
                    object pipeInstance = _coopPipeField.GetValue(null);
                    if (pipeInstance != null)
                    {
                        _coopActivateInviteDialog.Invoke(pipeInstance, new object[] { lobbyId });
                        ModLogger.Info($"[SteamworksDetector] Steam-Einladungs-Dialog für Lobby {lobbyId} erfolgreich via SteamPipe geöffnet.");
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    ModLogger.Warn($"[SteamworksDetector] Fehler bei SteamPipe.ActivateInviteDialog: {ex.Message}");
                }
            }

            // 2. Sekundärer Pfad: Generic Steamworks.SteamFriends.ActivateGameOverlayInviteDialog
            try
            {
                if (_mActivateGameOverlayInviteDialog != null)
                {
                    var pars = _mActivateGameOverlayInviteDialog.GetParameters();
                    if (pars.Length == 1 && pars[0].ParameterType.Name.Contains("CSteamID"))
                    {
                        object steamLobbyId = MakeSteamID(lobbyId);
                        _mActivateGameOverlayInviteDialog.Invoke(null, new object[] { steamLobbyId });
                        return true;
                    }
                    else if (pars.Length == 1 && pars[0].ParameterType == typeof(string))
                    {
                        _mActivateGameOverlayInviteDialog.Invoke(null, new object[] { "LobbyInvite" });
                        return true;
                    }
                }
            }
            catch { }

            return false;
        }

        public static void CheckSteamState()
        {
            if (!IsIronNestCoopDetected)
            {
                ResolveTypes();
            }

            if (IsIronNestCoopDetected && _coopInLobby != null)
            {
                try
                {
                    bool inLobby = (bool)_coopInLobby.GetValue(null);
                    IsInLobby = inLobby;

                    if (inLobby)
                    {
                        CurrentLobbyId = (ulong)_coopCurrentLobbyId.GetValue(null);
                        CurrentLobbyShort = (string)_coopCurrentLobbyShort?.GetValue(null) ?? "";

                        ConnectedPlayers.Clear();
                        string localName = (string)_coopLocalPlayerName?.GetValue(null) ?? "Du";
                        ConnectedPlayers.Add(localName);

                        if (_coopConnectedPeers != null && _coopGetPeerName != null)
                        {
                            var peers = _coopConnectedPeers.GetValue(null) as System.Collections.IEnumerable;
                            if (peers != null)
                            {
                                foreach (var peerObj in peers)
                                {
                                    ulong peerId = (ulong)peerObj;
                                    string name = (string)_coopGetPeerName.Invoke(null, new object[] { peerId });
                                    if (!string.IsNullOrEmpty(name) && !ConnectedPlayers.Contains(name))
                                    {
                                        ConnectedPlayers.Add(name);
                                    }
                                }
                            }
                        }

                        LastStatusMessage = $"✔ Co-op Aktiv · Code: {CurrentLobbyShort} ({ConnectedPlayers.Count} Spieler)";
                        FairnessGuard.SetMultiplayerState(ConnectedPlayers.Count > 1);
                    }
                    else
                    {
                        CurrentLobbyId = 0;
                        CurrentLobbyShort = "";
                        ConnectedPlayers.Clear();

                        string panel = (string)_coopPanelStatus?.GetValue(null);
                        if (!string.IsNullOrEmpty(panel))
                        {
                            LastStatusMessage = panel;
                        }
                        else
                        {
                            LastStatusMessage = "IronNestCoop bereit (Keine Lobby)";
                        }
                        FairnessGuard.SetMultiplayerState(false);
                    }
                    return;
                }
                catch (Exception ex)
                {
                    ModLogger.Warn($"[SteamworksDetector] Coop-Status-Fehler: {ex.Message}");
                }
            }

            if (!IsSteamAvailable)
            {
                LastStatusMessage = "Steam nicht verfügbar (Offline / Stub)";
                return;
            }
        }

        public static void TryCreateLobby(int maxMembers = 4)
        {
            if (!IsIronNestCoopDetected)
            {
                ResolveTypes();
            }

            if (IsIronNestCoopDetected && _coopCreateLobby != null)
            {
                try
                {
                    _coopCreateLobby.Invoke(null, null);
                    LastStatusMessage = "⏳ Erstelle Co-op Lobby...";
                    ModLogger.Info("[SteamworksDetector] IronNestCoop CreateLobby aufgerufen.");
                    CheckSteamState();
                    return;
                }
                catch (Exception ex)
                {
                    LastStatusMessage = $"❌ Fehler: {ex.Message}";
                    ModLogger.Warn($"[SteamworksDetector] CreateLobby Fehler: {ex}");
                    return;
                }
            }

            if (!IsSteamAvailable || _mCreateLobby == null)
            {
                LastStatusMessage = "❌ Steam / Co-op Mod nicht verfügbar.";
                return;
            }

            try
            {
                Type eLobbyType = _steamMatchmakingType.Assembly.GetType("Steamworks.ELobbyType")
                               ?? Type.GetType("Steamworks.ELobbyType, Steamworks.NET")
                               ?? Type.GetType("Steamworks.ELobbyType, com.rlabrecque.steamworks.net");
                object eLobbyTypePublic = eLobbyType != null ? Enum.ToObject(eLobbyType, 2) : 2;

                _mCreateLobby.Invoke(null, new object[] { eLobbyTypePublic, maxMembers });
                LastStatusMessage = $"⏳ Lobby wird erstellt ({maxMembers} Slots)...";
                ModLogger.Info($"[SteamworksDetector] SteamMatchmaking.CreateLobby für {maxMembers} Spieler aufgerufen.");
            }
            catch (Exception ex)
            {
                LastStatusMessage = $"❌ Fehler: {ex.Message}";
                ModLogger.Warn($"[SteamworksDetector] Fehler beim Erstellen der Steam-Lobby: {ex.Message}");
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
                        ModLogger.Info($"[SteamworksDetector] IronNestCoop JoinLobbyById aufgerufen (ID: {lobbyId}).");
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
                object steamLobbyId = MakeSteamID(lobbyId);
                _mJoinLobby.Invoke(null, new object[] { steamLobbyId });
                LastStatusMessage = $"⏳ Trete Lobby {lobbyId} bei...";
            }
            catch (Exception ex)
            {
                LastStatusMessage = $"❌ Fehler: {ex.Message}";
            }
        }

        public static void TryLeaveLobby()
        {
            if (IsIronNestCoopDetected && _coopLeaveLobby != null)
            {
                try
                {
                    _coopLeaveLobby.Invoke(null, null);
                    LastStatusMessage = "Lobby verlassen.";
                    OnLobbyLeft();
                    return;
                }
                catch (Exception ex)
                {
                    ModLogger.Warn($"[SteamworksDetector] Coop-LeaveLobby Fehler: {ex.Message}");
                }
            }

            if (IsInLobby && CurrentLobbyId != 0 && _mLeaveLobby != null)
            {
                try
                {
                    object steamLobbyId = MakeSteamID(CurrentLobbyId);
                    _mLeaveLobby.Invoke(null, new object[] { steamLobbyId });
                }
                catch { }
            }

            OnLobbyLeft();
        }

        public static void OnLobbyLeft()
        {
            CurrentLobbyId = 0;
            CurrentLobbyShort = "";
            IsInLobby = false;
            ConnectedPlayers.Clear();
            LastStatusMessage = "Keine aktive Lobby";
            FairnessGuard.SetMultiplayerState(false);
            ModLogger.Info("[SteamworksDetector] Lobby verlassen.");
        }

        private static object MakeSteamID(ulong id)
        {
            if (_cSteamIDType == null) return id;
            try
            {
                var ctor = _cSteamIDType.GetConstructor(new[] { typeof(ulong) });
                if (ctor != null) return ctor.Invoke(new object[] { id });
                return id;
            }
            catch { return id; }
        }
    }
}
