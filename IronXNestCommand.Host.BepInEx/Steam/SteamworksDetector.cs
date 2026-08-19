using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
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

        public static bool IsOpenNestCoopDetected { get; private set; } = false;

        // Open Nest Co-op Reflection Cache (github.com/1499501762/OPEN_NEST_CO-OP, AGPL-3.0).
        // Reine Reflection-Bruecke ueber dessen oeffentliche API (OpenNestCoop.Core.CoopRuntime.Net,
        // NetManager, SteamLobby) -- kein Code aus diesem Projekt wird kopiert/vendored. Laut dessen
        // eigener docs/API.md-FAQ begruendet reines Aufrufen der oeffentlichen API keine Ableitung und
        // unterliegt keiner AGPL-Pflicht ("仅调用注册 API ... 不构成派生作品").
        private static Type _openNestRuntimeType;
        private static FieldInfo _openNestNetField;
        private static MethodInfo _openNestCreateLobby;
        private static MethodInfo _openNestLeaveSession;
        private static PropertyInfo _openNestLobbyProp;
        private static FieldInfo _openNestPendingNameField;
        private static FieldInfo _openNestPendingMaxField;
        private static PropertyInfo _openNestStateProp;
        private static PropertyInfo _openNestIsHostProp;
        private static PropertyInfo _openNestHostSteamIdProp;
        private static PropertyInfo _openNestRosterProp;
        private static MethodInfo _openNestLobbyJoinByUlong;
        private static PropertyInfo _openNestLobbyIdProp;

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

        // Steamworks.NET LobbyCreated_t/LobbyEnter_t Callback-Instanzen (autonomer Modus).
        // Muessen als Feld gehalten werden, sonst sammelt der GC sie ein und der Callback feuert nie.
        // Setzen KEINEN eigenen SteamAPI.RunCallbacks()-Pump auf -- verlaesst sich darauf, dass ein
        // anderer Steamworks.NET-Konsument im Prozess (z.B. Open Nest Co-op) das bereits jeden Frame
        // tut. Ohne einen solchen Pumper (BepInEx + IronXNestCommand ganz ohne weitere Coop-Mod)
        // feuert der Callback nie -- siehe DOCUMENTATION.md.
        private static object _lobbyCreatedCallback;
        private static object _lobbyEnteredCallback;

        private static float _pollTimer = 0f;
        private const float PollInterval = 2.0f;

        public static void Initialize()
        {
            ModLogger.Info("[SteamworksDetector] Initialisiere Steamworks & IronNestCoop Erkennung...");
            ResolveTypes();

            if (IsIronNestCoopDetected)
                ModLogger.Info("[SteamworksDetector] ★ IronNestCoop Bridge aktiv! Direkte Synchronisation mit Co-op Modus.");
            else if (IsOpenNestCoopDetected)
                ModLogger.Info("[SteamworksDetector] ★ Open Nest Co-op Bridge aktiv! Direkte Synchronisation mit Co-op Modus.");
            else if (IsSteamAvailable)
                ModLogger.Info("[SteamworksDetector] Steamworks.NET erkannt — Autonomer Lobby-Modus bereit.");
            else
                ModLogger.Warn("[SteamworksDetector] Weder IronNestCoop/Open Nest Co-op noch Steamworks.NET gefunden — Stub-Modus aktiv.");

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

            AppDomain.CurrentDomain.AssemblyResolve += ResolveMissingAssembly;
        }

        private static Assembly ResolveMissingAssembly(object sender, ResolveEventArgs args)
        {
            try
            {
                string asmName = new AssemblyName(args.Name).Name + ".dll";
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string[] searchDirs = {
                    System.IO.Path.Combine(baseDir, "BepInEx", "core"),
                    System.IO.Path.Combine(baseDir, "BepInEx", "plugins"),
                    System.IO.Path.Combine(baseDir, "Mods"),
                    System.IO.Path.Combine(baseDir, "UserLibs")
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

            // 2. Fallback: Open Nest Co-op (das tatsaechlich aktiv genutzte Community-Plugin,
            // siehe DOCUMENTATION.md §3.28) -- Soft-Dependency genau wie IronNestCoop oben, nur
            // ueber CoopRuntime.Net (NetManager) statt einer eigenen SteamNet-Bruecke.
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try { _openNestRuntimeType ??= asm.GetType("OpenNestCoop.Core.CoopRuntime"); }
                catch { }
            }

            if (_openNestRuntimeType != null)
            {
                try
                {
                    _openNestNetField = _openNestRuntimeType.GetField("Net", BindingFlags.Public | BindingFlags.Static);
                    object netInstance = _openNestNetField?.GetValue(null);
                    if (netInstance != null)
                    {
                        Type netManagerType = netInstance.GetType();
                        _openNestCreateLobby = netManagerType.GetMethod("CreateLobby", BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);
                        _openNestLeaveSession = netManagerType.GetMethod("LeaveSession", BindingFlags.Public | BindingFlags.Instance);
                        _openNestLobbyProp = netManagerType.GetProperty("Lobby", BindingFlags.Public | BindingFlags.Instance);
                        _openNestPendingNameField = netManagerType.GetField("PendingLobbyName", BindingFlags.Public | BindingFlags.Instance);
                        _openNestPendingMaxField = netManagerType.GetField("PendingMaxPlayers", BindingFlags.Public | BindingFlags.Instance);
                        _openNestStateProp = netManagerType.GetProperty("State", BindingFlags.Public | BindingFlags.Instance);
                        _openNestIsHostProp = netManagerType.GetProperty("IsHost", BindingFlags.Public | BindingFlags.Instance);
                        _openNestHostSteamIdProp = netManagerType.GetProperty("HostSteamId", BindingFlags.Public | BindingFlags.Instance);
                        _openNestRosterProp = netManagerType.GetProperty("Roster", BindingFlags.Public | BindingFlags.Instance);

                        object lobbyInstance = _openNestLobbyProp?.GetValue(netInstance);
                        if (lobbyInstance != null)
                        {
                            Type lobbyType = lobbyInstance.GetType();
                            _openNestLobbyJoinByUlong = lobbyType.GetMethod("JoinLobby", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(ulong) }, null);
                            _openNestLobbyIdProp = lobbyType.GetProperty("LobbyID", BindingFlags.Public | BindingFlags.Instance);
                        }

                        if (_openNestCreateLobby != null)
                        {
                            IsOpenNestCoopDetected = true;
                            IsSteamAvailable = true;
                            LastStatusMessage = "Open Nest Co-op erkannt (Bereit)";
                            ModLogger.Info("[SteamworksDetector] ★ Open Nest Co-op Bridge aktiv (CoopRuntime.Net).");
                            return;
                        }
                    }
                }
                catch (Exception ex)
                {
                    ModLogger.Warn($"[SteamworksDetector] Open Nest Co-op Anbindung fehlgeschlagen: {ex.Message}");
                }
            }

            // 3. Fallback: Generic Steamworks
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

                _lobbyCreatedCallback ??= RegisterSteamCallback("LobbyCreated_t", nameof(OnLobbyCreatedBoxed));
                _lobbyEnteredCallback ??= RegisterSteamCallback("LobbyEnter_t", nameof(OnLobbyEnteredBoxed));
            }
        }

        // Registriert per Reflection einen Steamworks.NET Callback<T> fuer einen Callback-Struct-Typ,
        // dessen Name wir nur als String kennen (kein Compile-Zeit-Reference auf Steamworks.NET).
        // Callback<T>.Create() erwartet ein DispatchDelegate(T param) -- T ist ein Value-Type-Struct,
        // dessen genauer Typ erst zur Laufzeit bekannt ist. Delegate.CreateDelegate kann eine Methode
        // mit Parametertyp "object" nicht direkt an einen Delegattyp mit Werttyp-Parameter binden, also
        // bauen wir per DynamicMethod einen minimalen IL-Trampolin, der das Struct boxt und an unseren
        // Handler (Parametertyp object) weiterreicht.
        private static object RegisterSteamCallback(string structTypeName, string handlerMethodName)
        {
            try
            {
                Assembly steamAsm = _steamMatchmakingType.Assembly;
                Type structType = steamAsm.GetType("Steamworks." + structTypeName) ?? steamAsm.GetType("Il2CppSteamworks." + structTypeName);
                Type callbackOpenType = steamAsm.GetType("Steamworks.Callback`1") ?? steamAsm.GetType("Il2CppSteamworks.Callback`1");
                if (structType == null || callbackOpenType == null) return null;

                Type callbackClosedType = callbackOpenType.MakeGenericType(structType);
                Type dispatchDelegateType = callbackClosedType.GetNestedType("DispatchDelegate");
                if (dispatchDelegateType == null) return null;

                MethodInfo createMethod = callbackClosedType.GetMethod("Create", BindingFlags.Public | BindingFlags.Static, null, new[] { dispatchDelegateType }, null);
                MethodInfo handlerMethod = typeof(SteamworksDetector).GetMethod(handlerMethodName, BindingFlags.NonPublic | BindingFlags.Static);
                if (createMethod == null || handlerMethod == null) return null;

                var dm = new DynamicMethod(
                    "IronXNestCommand_" + structTypeName + "_Thunk",
                    typeof(void),
                    new[] { structType },
                    typeof(SteamworksDetector).Module,
                    true);
                ILGenerator il = dm.GetILGenerator();
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Box, structType);
                il.EmitCall(OpCodes.Call, handlerMethod, null);
                il.Emit(OpCodes.Ret);

                Delegate thunk = dm.CreateDelegate(dispatchDelegateType);
                object callbackInstance = createMethod.Invoke(null, new object[] { thunk });
                ModLogger.Info($"[SteamworksDetector] {structTypeName} Callback registriert (Autonomer Modus).");
                return callbackInstance;
            }
            catch (Exception ex)
            {
                ModLogger.Warn($"[SteamworksDetector] Konnte {structTypeName} Callback nicht registrieren: {ex.Message}");
                return null;
            }
        }

        private static void OnLobbyCreatedBoxed(object lobbyCreatedResult)
        {
            try
            {
                Type t = lobbyCreatedResult.GetType();
                object resultField = t.GetField("m_eResult")?.GetValue(lobbyCreatedResult);
                object lobbyIdField = t.GetField("m_ulSteamIDLobby")?.GetValue(lobbyCreatedResult);
                int resultValue = resultField != null ? Convert.ToInt32(resultField) : -1;

                // Steamworks EResult.k_EResultOK == 1
                if (resultValue == 1 && lobbyIdField != null)
                {
                    CurrentLobbyId = Convert.ToUInt64(lobbyIdField);
                    IsInLobby = true;
                    LastStatusMessage = "✔ Lobby erstellt!";
                    ModLogger.Info($"[SteamworksDetector] Lobby erstellt: {CurrentLobbyId}");
                }
                else
                {
                    LastStatusMessage = $"❌ Lobby-Erstellung fehlgeschlagen (Result {resultValue}).";
                    ModLogger.Warn($"[SteamworksDetector] LobbyCreated_t Fehler-Result: {resultValue}");
                }
            }
            catch (Exception ex)
            {
                ModLogger.Warn($"[SteamworksDetector] Fehler beim Verarbeiten von LobbyCreated_t: {ex.Message}");
            }
        }

        private static void OnLobbyEnteredBoxed(object lobbyEnterResult)
        {
            try
            {
                Type t = lobbyEnterResult.GetType();
                object responseField = t.GetField("m_EChatRoomEnterResponse")?.GetValue(lobbyEnterResult);
                object lobbyIdField = t.GetField("m_ulSteamIDLobby")?.GetValue(lobbyEnterResult);
                int responseValue = responseField != null ? Convert.ToInt32(responseField) : -1;

                // Steamworks EChatRoomEnterResponse.k_EChatRoomEnterResponseSuccess == 1
                if (responseValue == 1 && lobbyIdField != null)
                {
                    CurrentLobbyId = Convert.ToUInt64(lobbyIdField);
                    IsInLobby = true;
                    LastStatusMessage = "✔ Lobby beigetreten!";
                    ModLogger.Info($"[SteamworksDetector] Lobby beigetreten: {CurrentLobbyId}");
                }
                else
                {
                    LastStatusMessage = $"❌ Lobby-Beitritt fehlgeschlagen (Response {responseValue}).";
                    ModLogger.Warn($"[SteamworksDetector] LobbyEnter_t Fehler-Response: {responseValue}");
                }
            }
            catch (Exception ex)
            {
                ModLogger.Warn($"[SteamworksDetector] Fehler beim Verarbeiten von LobbyEnter_t: {ex.Message}");
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
            if (!IsIronNestCoopDetected && !IsOpenNestCoopDetected)
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

            if (IsOpenNestCoopDetected && _openNestNetField != null && _openNestStateProp != null)
            {
                try
                {
                    object netInstance = _openNestNetField.GetValue(null);
                    if (netInstance == null) return;

                    int state = Convert.ToInt32(_openNestStateProp.GetValue(netInstance));
                    // OpenNestCoop.Net.SessionState: Idle=0, Hosting=1, Joined=2
                    bool inLobby = state != 0;
                    IsInLobby = inLobby;

                    if (inLobby)
                    {
                        ulong lobbyId = 0;
                        object lobbyInstance = _openNestLobbyProp?.GetValue(netInstance);
                        if (lobbyInstance != null && _openNestLobbyIdProp != null)
                        {
                            object cSteamId = _openNestLobbyIdProp.GetValue(lobbyInstance);
                            FieldInfo mSteamIdField = cSteamId?.GetType().GetField("m_SteamID");
                            if (mSteamIdField != null) lobbyId = Convert.ToUInt64(mSteamIdField.GetValue(cSteamId));
                        }
                        CurrentLobbyId = lobbyId;
                        CurrentLobbyShort = "";

                        ConnectedPlayers.Clear();
                        var roster = _openNestRosterProp?.GetValue(netInstance) as System.Collections.IEnumerable;
                        if (roster != null)
                        {
                            foreach (var session in roster)
                            {
                                string name = (string)session.GetType().GetField("Name")?.GetValue(session);
                                if (!string.IsNullOrEmpty(name) && !ConnectedPlayers.Contains(name))
                                {
                                    ConnectedPlayers.Add(name);
                                }
                            }
                        }
                        if (ConnectedPlayers.Count == 0) ConnectedPlayers.Add("Du");

                        LastStatusMessage = $"✔ Co-op Aktiv (Open Nest Co-op) · {ConnectedPlayers.Count} Spieler";
                        FairnessGuard.SetMultiplayerState(ConnectedPlayers.Count > 1);
                    }
                    else
                    {
                        CurrentLobbyId = 0;
                        CurrentLobbyShort = "";
                        ConnectedPlayers.Clear();
                        LastStatusMessage = "Open Nest Co-op bereit (Keine Lobby)";
                        FairnessGuard.SetMultiplayerState(false);
                    }
                    return;
                }
                catch (Exception ex)
                {
                    ModLogger.Warn($"[SteamworksDetector] Open Nest Co-op Status-Fehler: {ex.Message}");
                }
            }

            if (!IsSteamAvailable)
            {
                LastStatusMessage = "IronNestCoop-Mod nicht gefunden — für Lobby-Funktionen benötigt";
                return;
            }
        }

        public static void TryCreateLobby(int maxMembers = 4)
        {
            if (!IsIronNestCoopDetected && !IsOpenNestCoopDetected)
            {
                ResolveTypes();
            }

            if (IsIronNestCoopDetected && _coopCreateLobby != null)
            {
                try
                {
                    _coopCreateLobby.Invoke(null, null);
                    LastStatusMessage = "Erstelle Co-op Lobby...";
                    ModLogger.Info("[SteamworksDetector] IronNestCoop CreateLobby aufgerufen.");
                    CheckSteamState();
                    return;
                }
                catch (Exception ex)
                {
                    var inner = (ex is TargetInvocationException tie && tie.InnerException != null) ? tie.InnerException : ex;
                    LastStatusMessage = $"Fehler: {inner.Message}";
                    ModLogger.Warn($"[SteamworksDetector] CreateLobby Fehler: {inner}");
                    return;
                }
            }

            if (IsOpenNestCoopDetected && _openNestCreateLobby != null)
            {
                try
                {
                    object netInstance = _openNestNetField?.GetValue(null);
                    if (netInstance != null)
                    {
                        _openNestPendingNameField?.SetValue(netInstance, "IronXNestCommand Lobby");
                        _openNestPendingMaxField?.SetValue(netInstance, maxMembers);
                        _openNestCreateLobby.Invoke(netInstance, null);
                        LastStatusMessage = "⏳ Erstelle Lobby (Open Nest Co-op)...";
                        ModLogger.Info("[SteamworksDetector] OpenNestCoop NetManager.CreateLobby() aufgerufen.");
                        return;
                    }
                }
                catch (Exception ex)
                {
                    var inner = (ex is TargetInvocationException tie && tie.InnerException != null) ? tie.InnerException : ex;
                    LastStatusMessage = $"❌ Fehler: {inner.Message}";
                    ModLogger.Warn($"[SteamworksDetector] OpenNestCoop CreateLobby Fehler: {inner}");
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

        private static string SanitizeHex(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;
            return Regex.Replace(input, @"[^0-9a-fA-F]", "").ToUpperInvariant();
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

            if (IsOpenNestCoopDetected && _openNestLobbyJoinByUlong != null)
            {
                try
                {
                    ulong lobbyId = 0;
                    if (!ulong.TryParse(cleanCode, out lobbyId))
                    {
                        ulong.TryParse(sanitizedHex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out lobbyId);
                    }

                    if (lobbyId == 0)
                    {
                        LastStatusMessage = $"❌ Ungültiger Lobby-Code '{cleanCode}'.";
                        return;
                    }

                    object netInstance = _openNestNetField?.GetValue(null);
                    object lobbyInstance = netInstance != null ? _openNestLobbyProp?.GetValue(netInstance) : null;
                    if (lobbyInstance != null)
                    {
                        _openNestLobbyJoinByUlong.Invoke(lobbyInstance, new object[] { lobbyId });
                        LastStatusMessage = $"⏳ Trete Lobby {lobbyId} bei (Open Nest Co-op)...";
                        ModLogger.Info($"[SteamworksDetector] OpenNestCoop Lobby.JoinLobby aufgerufen (ID: {lobbyId}).");
                        return;
                    }
                }
                catch (Exception ex)
                {
                    var inner = (ex is TargetInvocationException tie && tie.InnerException != null) ? tie.InnerException : ex;
                    LastStatusMessage = $"❌ Fehler: {inner.Message}";
                    ModLogger.Warn($"[SteamworksDetector] OpenNestCoop JoinLobby Fehler: {inner}");
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

            if (IsOpenNestCoopDetected && _openNestLeaveSession != null)
            {
                try
                {
                    object netInstance = _openNestNetField?.GetValue(null);
                    if (netInstance != null)
                    {
                        _openNestLeaveSession.Invoke(netInstance, null);
                        LastStatusMessage = "Lobby verlassen.";
                        OnLobbyLeft();
                        return;
                    }
                }
                catch (Exception ex)
                {
                    ModLogger.Warn($"[SteamworksDetector] OpenNestCoop LeaveSession Fehler: {ex.Message}");
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
