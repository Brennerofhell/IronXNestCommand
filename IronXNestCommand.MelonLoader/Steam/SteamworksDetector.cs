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
        private static MethodInfo _mActivateGameOverlayInviteDialog;

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

        private static bool _resolverRegistered = false;

        private static void EnsureAssemblyResolver()
        {
            if (_resolverRegistered) return;
            _resolverRegistered = true;

            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                try
                {
                    string asmName = new AssemblyName(args.Name).Name + ".dll";
                    string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                    string[] searchDirs = {
                        System.IO.Path.Combine(baseDir, "BepInEx", "core"),
                        System.IO.Path.Combine(baseDir, "BepInEx", "plugins"),
                        System.IO.Path.Combine(baseDir, "Mods"),
                        System.IO.Path.Combine(baseDir, "UserLibs"),
                        System.IO.Path.Combine(baseDir, "MelonLoader", "net6"),
                        System.IO.Path.Combine(baseDir, "MelonLoader", "Dependencies")
                    };
                    foreach (var dir in searchDirs)
                    {
                        string fullPath = System.IO.Path.Combine(dir, asmName);
                        if (System.IO.File.Exists(fullPath))
                        {
                            try { return Assembly.LoadFrom(fullPath); } catch { }
                        }
                    }
                }
                catch { }
                return null;
            };
        }

        private static void ResolveTypes()
        {
            EnsureAssemblyResolver();

            // 0. Suche und lade ggf. BepInEx.Core.dll & IronNestCoop.Core.dll von der Festplatte
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string[] preloadPaths = {
                System.IO.Path.Combine(baseDir, "BepInEx", "core", "BepInEx.Core.dll"),
                System.IO.Path.Combine(baseDir, "BepInEx", "core", "BepInEx.Unity.IL2CPP.dll"),
                System.IO.Path.Combine(baseDir, "BepInEx", "plugins", "IronNestCoop.Core.dll"),
                System.IO.Path.Combine(baseDir, "Mods", "IronNestCoop.Core.dll"),
                System.IO.Path.Combine(baseDir, "UserLibs", "IronNestCoop.Core.dll"),
                System.IO.Path.Combine(baseDir, "Plugins", "IronNestCoop.Core.dll"),
                System.IO.Path.Combine(baseDir, "IronNestCoop.Core.dll")
            };
            foreach (var path in preloadPaths)
            {
                if (System.IO.File.Exists(path))
                {
                    try
                    {
                        Assembly.LoadFrom(path);
                    }
                    catch { }
                }
            }

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
            string[] steamAssemblies = {
                "Il2Cppcom.rlabrecque.steamworks.net",
                "Il2CppHeathen.Steamworks",
                "Steamworks.NET",
                "Assembly-CSharp-firstpass",
                "Assembly-CSharp",
                "com.rlabrecque.steamworks.net"
            };
            foreach (var asmName in steamAssemblies)
            {
                try
                {
                    var asm = Assembly.Load(asmName);
                    if (asm != null)
                    {
                        _steamMatchmakingType ??= asm.GetType("Steamworks.SteamMatchmaking") ?? asm.GetType("Il2CppSteamworks.SteamMatchmaking");
                        _steamFriendsType ??= asm.GetType("Steamworks.SteamFriends") ?? asm.GetType("Il2CppSteamworks.SteamFriends");
                        _steamUserType ??= asm.GetType("Steamworks.SteamUser") ?? asm.GetType("Il2CppSteamworks.SteamUser");
                        _cSteamIDType ??= asm.GetType("Steamworks.CSteamID") ?? asm.GetType("Il2CppSteamworks.CSteamID");
                    }
                }
                catch { }
            }

            if (_steamMatchmakingType == null)
            {
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    try
                    {
                        _steamMatchmakingType ??= asm.GetType("Steamworks.SteamMatchmaking") ?? asm.GetType("Il2CppSteamworks.SteamMatchmaking");
                        _steamFriendsType ??= asm.GetType("Steamworks.SteamFriends") ?? asm.GetType("Il2CppSteamworks.SteamFriends");
                        _steamUserType ??= asm.GetType("Steamworks.SteamUser") ?? asm.GetType("Il2CppSteamworks.SteamUser");
                        _cSteamIDType ??= asm.GetType("Steamworks.CSteamID") ?? asm.GetType("Il2CppSteamworks.CSteamID");
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
                {
                    _mGetFriendPersonaName = _steamFriendsType.GetMethod("GetFriendPersonaName", BindingFlags.Public | BindingFlags.Static);
                    _mActivateGameOverlayInviteDialog = _steamFriendsType.GetMethod("ActivateGameOverlayInviteDialog", BindingFlags.Public | BindingFlags.Static)
                                                     ?? _steamFriendsType.GetMethod("ActivateGameOverlay", BindingFlags.Public | BindingFlags.Static);
                }

                LastStatusMessage = "Steamworks bereit";
            }
        }

        private static void CheckSteamState()
        {
            if (!IsIronNestCoopDetected)
            {
                ResolveTypes();
            }

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

                        LastStatusMessage = $"✔ Co-op Aktiv · Code: {CurrentLobbyShort} ({ConnectedPlayers.Count} Spieler)";
                        FairnessGuard.SetMultiplayerState(true);
                    }
                    else
                    {
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
                    MelonLogger.Warning($"[SteamworksDetector] Fehler beim Abruf von IronNestCoop: {ex.Message}");
                }
            }

            if (!IsSteamAvailable)
            {
                LastStatusMessage = "Steam nicht verfügbar (Offline / Stub)";
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
                object lobbyIdBoxed = MakeSteamID(CurrentLobbyId);
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
            if (!IsIronNestCoopDetected)
            {
                ResolveTypes();
            }

            if (IsIronNestCoopDetected && _coopCreateLobby != null)
            {
                try
                {
                    _coopCreateLobby.Invoke(null, null);
                    LastStatusMessage = "Erstelle Co-op Lobby...";
                    MelonLogger.Msg("[SteamworksDetector] IronNestCoop CreateLobby aufgerufen.");
                    CheckSteamState();
                    return;
                }
                catch (Exception ex)
                {
                    var inner = (ex is TargetInvocationException tie && tie.InnerException != null) ? tie.InnerException : ex;
                    LastStatusMessage = $"Fehler: {inner.Message}";
                    MelonLogger.Warning($"[SteamworksDetector] CreateLobby Fehler: {inner}");
                    return;
                }
            }

            if (!IsSteamAvailable || _mCreateLobby == null)
            {
                LastStatusMessage = "❌ IronNestCoop-Mod fehlt (für Lobby-Erstellung benötigt).";
                return;
            }

            try
            {
                Type eLobbyType = _steamMatchmakingType?.Assembly.GetType("Steamworks.ELobbyType")
                               ?? Type.GetType("Steamworks.ELobbyType, Steamworks.NET")
                               ?? Type.GetType("Steamworks.ELobbyType, com.rlabrecque.steamworks.net");
                object lobbyTypeValue = eLobbyType != null ? Enum.ToObject(eLobbyType, 2) : 2;

                _mCreateLobby.Invoke(null, new object[] { lobbyTypeValue, maxPlayers });
                LastStatusMessage = $"⏳ Erstelle Lobby für {maxPlayers} Spieler...";
                MelonLogger.Msg($"[SteamworksDetector] SteamMatchmaking.CreateLobby für {maxPlayers} Spieler aufgerufen.");
            }
            catch (Exception ex)
            {
                LastStatusMessage = $"❌ Fehler beim Erstellen: {ex.Message}";
                MelonLogger.Warning($"[SteamworksDetector] Fehler beim Erstellen der Steam-Lobby: {ex.Message}");
            }
        }

        private static string SanitizeHex(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;
            return System.Text.RegularExpressions.Regex.Replace(input, @"[^0-9a-fA-F]", "").ToUpperInvariant();
        }

        public static void TryJoinLobby(string codeOrId)
        {
            if (string.IsNullOrWhiteSpace(codeOrId))
            {
                LastStatusMessage = "❌ Bitte Lobby-Code oder ID eingeben.";
                return;
            }

            string cleanCode = codeOrId.Trim();
            string sanitizedHex = SanitizeHex(cleanCode);

            if (IsIronNestCoopDetected)
            {
                try
                {
                    ulong lobbyId = 0;
                    if (_coopResolveLobbyId != null && !string.IsNullOrEmpty(sanitizedHex))
                    {
                        try
                        {
                            lobbyId = (ulong)_coopResolveLobbyId.Invoke(null, new object[] { sanitizedHex });
                        }
                        catch { }
                    }

                    if (lobbyId == 0 && _coopResolveLobbyId != null)
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
                object lobbyIdBoxed = MakeSteamID(lobbyId);
                _mJoinLobby.Invoke(null, new[] { lobbyIdBoxed });
                LastStatusMessage = $"⏳ Trete Lobby {lobbyId} bei...";
                MelonLogger.Msg($"[SteamworksDetector] SteamMatchmaking.JoinLobby aufgerufen (ID: {lobbyId}).");
            }
            catch (Exception ex)
            {
                LastStatusMessage = $"❌ Fehler beim Beitreten: {ex.Message}";
                MelonLogger.Warning($"[SteamworksDetector] Fehler beim Beitreten zu Lobby {lobbyId}: {ex.Message}");
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
                catch { }
            }

            if (IsSteamAvailable && _mLeaveLobby != null && CurrentLobbyId != 0)
            {
                try
                {
                    object lobbyIdBoxed = MakeSteamID(CurrentLobbyId);
                    _mLeaveLobby.Invoke(null, new[] { lobbyIdBoxed });
                    LastStatusMessage = "Lobby verlassen.";
                }
                catch { }
            }

            OnLobbyLeft();
        }

        // Nur der lokale Zustand-Reset, OHNE die native LeaveLobby-Methode erneut aufzurufen —
        // wird vom Harmony-Postfix in MultiplayerPatches.LeaveLobby_Postfix aufgerufen, NACHDEM
        // das native LeaveLobby bereits gelaufen ist. Ein erneuter Aufruf hier würde denselben
        // Postfix wieder auslösen → unendliche Rekursion → StackOverflowException.
        public static void OnLobbyLeft()
        {
            IsInLobby = false;
            CurrentLobbyId = 0;
            CurrentLobbyShort = "";
            ConnectedPlayers.Clear();
            LastStatusMessage = "Lobby verlassen.";
            FairnessGuard.SetMultiplayerState(false);
        }

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
                        MelonLogger.Msg($"[SteamworksDetector] Steam-Einladungs-Dialog für Lobby {lobbyId} erfolgreich via SteamPipe geöffnet.");
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    MelonLogger.Warning($"[SteamworksDetector] Fehler bei SteamPipe.ActivateInviteDialog: {ex.Message}");
                }
            }

            // 2. Sekundärer Pfad: Generic Steamworks.SteamFriends
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

                if (_steamFriendsType != null)
                {
                    var mActivateGameOverlay = _steamFriendsType.GetMethod("ActivateGameOverlay", BindingFlags.Public | BindingFlags.Static);
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

        private static object MakeSteamID(ulong id)
        {
            if (_cSteamIDType == null) return id;
            try
            {
                var ctor = _cSteamIDType.GetConstructor(new[] { typeof(ulong) });
                if (ctor != null) return ctor.Invoke(new object[] { id });
                return Activator.CreateInstance(_cSteamIDType, id);
            }
            catch { return id; }
        }
    }
}
