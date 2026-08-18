using System;
using System.Collections.Generic;
using UnityEngine;
using IronXNestCommand.Core.Config;
using IronXNestCommand.Core.Logging;
using IronXNestCommand.Host.BepInEx.Ammo;
using IronXNestCommand.Host.BepInEx.Core;
using IronXNestCommand.Host.BepInEx.Patches;
using IronXNestCommand.Host.BepInEx.Steam;

namespace IronXNestCommand.Host.BepInEx.Overlay
{
    /// <summary>
    /// IronXNestCommand // Lobby & Besatzung
    /// 1:1 Pixel- und Token-getreue Umsetzung der offiziellen GUI-Vorlage.
    /// Reines Co-op Lobby- & Besatzungs-Management mit Lochkarten-Sync für Iron Nest.
    /// </summary>
    public sealed class CommandOverlay : MonoBehaviour
    {
        public static bool IsVisible { get; set; } = true;
        public static ModConfig Config { get; set; } = new();

        // ── Window Layout (kompakte Groesse — abgespeckt gegenueber der 520x480 Vorlage) ──
        private static Rect _windowRect = new(60, 60, 460, 420);
        private static bool _isDragging = false;
        private static Vector2 _dragOffset = Vector2.zero;

        // ── Tabs ───────────────────────────────────────────────────────────────
        private static int _activeTab = 0; // 0 = Home / Lobby & Besatzung, 1 = Einstellungen
        private static readonly string[] TabNames = { "🏠 HOME / LOBBY", "⚙️ EINSTELLUNGEN" };

        // ── State & Animations ─────────────────────────────────────────────────
        private static string _lobbyIdInput = "";
        private static bool _joinInputMode = false;
        private static bool _copiedFeedback = false;
        private static float _copiedTimer = 0f;
        private static bool _syncing = false;
        private static float _syncTimer = 0f;
        private static float _lastSyncTime = 0f;
        private static float _pulseTimer = 0f;

        // Notification Banner
        private static string _notificationText = "";
        private static float _notificationTimer = 0f;

        // ── Textures & Color Palette (#0E0E10 / #18181B / #D97757) ─────────────
        private static Texture2D _texMasterBg;
        private static Texture2D _texCardBg;
        private static Texture2D _texCardDashed;
        private static Texture2D _texTerracotta;
        private static Texture2D _texTerracottaHover;
        private static Texture2D _texBadgeBg;
        private static Texture2D _texButtonDark;
        private static Texture2D _texButtonDarkHover;
        private static Texture2D _texBorderLight;
        private static Texture2D _texDotGreen;
        private static Texture2D _texDotGrey;
        private static Texture2D _texCursor;

        // ── GUIStyles (100% IL2CPP-kompatibel) ──────────────────────────────────
        private static GUIStyle _titleStyle;
        private static GUIStyle _subtitleStyle;
        private static GUIStyle _sectionHeaderStyle;
        private static GUIStyle _lobbyCodeStyle;
        private static GUIStyle _lobbySubtextStyle;
        private static GUIStyle _memberNameStyle;
        private static GUIStyle _memberRoleStyle;
        private static GUIStyle _badgeInitialStyle;
        private static GUIStyle _emptySlotStyle;
        private static GUIStyle _statusPillStyle;
        private static GUIStyle _btnTerracottaStyle;
        private static GUIStyle _btnOutlineStyle;
        private static GUIStyle _btnDarkStyle;
        private static GUIStyle _hotkeyStyle;
        private static GUIStyle _notificationStyle;
        private static GUIStyle _inputFieldStyle;

        private static bool _stylesInitialized = false;

        public CommandOverlay(IntPtr ptr) : base(ptr) { }

        private void Awake()
        {
            try
            {
                DontDestroyOnLoad(gameObject);
                gameObject.hideFlags = HideFlags.HideAndDontSave;
            }
            catch { }

            Config = Plugin.Instance?.ConfigData ?? new ModConfig();
            SetVisible(Config.StartVisible);
            AudioFeedback.Initialize();
            AmmoRequisitionBridge.Initialize();
        }

        private void Update()
        {
            if (CheckToggleKey())
                SetVisible(!IsVisible);

            if (IsVisible)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            float dt = Time.unscaledDeltaTime;
            _pulseTimer += dt * 3f;

            if (_copiedTimer > 0f)
            {
                _copiedTimer -= dt;
                if (_copiedTimer <= 0f)
                    _copiedFeedback = false;
            }

            if (_syncing)
            {
                _syncTimer -= dt;
                if (_syncTimer <= 0f)
                {
                    _syncing = false;
                    _lastSyncTime = Time.unscaledTime;
                }
            }

            if (_notificationTimer > 0f)
            {
                _notificationTimer -= dt;
                if (_notificationTimer <= 0f)
                    _notificationText = "";
            }

            SteamworksDetector.Update(dt);
            TurretTelemetry.Update();
            CoopPunchcardFix.UpdateWatchdog(dt);
            EnemyDespawnGuard.UpdateWatchdog(dt);
        }

        public static void ShowNotification(string text, float duration = 2.5f)
        {
            _notificationText = text;
            _notificationTimer = duration;
        }

        // Beim Öffnen geben wir den Cursor frei (CursorLockMode.None & visible = true).
        // Beim Schließen erzwingen wir kein CursorLockMode.Locked, damit der Mauszeiger in
        // Menüs, Hangar und Lobby frei beweglich bleibt und nicht 'stuck' wird.
        private static void SetVisible(bool visible)
        {
            IsVisible = visible;
            if (visible)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        private void OnGUI()
        {
            // Event check for F8 / ToggleKey keypress in IMGUI event stream
            if (Event.current != null && Event.current.isKey && Event.current.type == EventType.KeyDown)
            {
                KeyCode targetKey = KeyCode.F8;
                if (!string.IsNullOrEmpty(Config.ToggleKey) && Enum.TryParse<KeyCode>(Config.ToggleKey, true, out var parsedKey))
                    targetKey = parsedKey;

                if (Event.current.keyCode == targetKey || Event.current.keyCode == KeyCode.F8)
                {
                    SetVisible(!IsVisible);
                    Event.current.Use();
                    return;
                }
            }

            if (!IsVisible) return;

            GUI.depth = -1000;
            EnsureStyles();

            var currentEvent = Event.current;
            if (currentEvent != null && currentEvent.isMouse)
            {
                Rect titleBarRect = new(_windowRect.x, _windowRect.y, _windowRect.width - 40, 48);
                if (currentEvent.type == EventType.MouseDown && titleBarRect.Contains(currentEvent.mousePosition))
                {
                    _isDragging = true;
                    _dragOffset = currentEvent.mousePosition - new Vector2(_windowRect.x, _windowRect.y);
                }
                else if (currentEvent.type == EventType.MouseUp)
                {
                    _isDragging = false;
                }
                else if (_isDragging && currentEvent.type == EventType.MouseDrag)
                {
                    _windowRect.x = Mathf.Clamp(currentEvent.mousePosition.x - _dragOffset.x, 10, Mathf.Max(100, Screen.width - _windowRect.width - 10));
                    _windowRect.y = Mathf.Clamp(currentEvent.mousePosition.y - _dragOffset.y, 10, Mathf.Max(100, Screen.height - _windowRect.height - 10));
                }
            }

            float wx = _windowRect.x;
            float wy = _windowRect.y;
            float ww = _windowRect.width;
            float wh = _windowRect.height;

            // 1. Master Dialog Background (#18181B mit #27272A Border)
            DrawBox(new Rect(wx, wy, ww, wh), _texMasterBg, new Color(0.820f, 0.800f, 0.765f, 1f));

            // 2. Header Bar
            DrawHeader(wx, wy, ww);

            // 3. Tab Switcher
            float tabWidth = (ww - 36) / 2f;
            float tabY = wy + 52;
            for (int i = 0; i < TabNames.Length; i++)
            {
                bool active = (i == _activeTab);
                var btnStyle = active ? _btnTerracottaStyle : _btnOutlineStyle;
                if (DrawButton(new Rect(wx + 18 + (i * (tabWidth + 4)), tabY, tabWidth, 26), TabNames[i], btnStyle))
                {
                    _activeTab = i;
                    AudioFeedback.PlayClick();
                }
            }

            DrawDivider(new Rect(wx + 18, tabY + 32, ww - 36, 1), new Color(0.820f, 0.800f, 0.765f, 1f));

            // 4. Content Area
            float cx = wx + 18;
            float cy = tabY + 42;
            float cw = ww - 36;

            if (_activeTab == 0)
            {
                DrawLobbyCrewContent(cx, cy, cw);
            }
            else
            {
                DrawSettingsContent(cx, cy, cw);
            }

            // 5. Notification Banner
            if (!string.IsNullOrEmpty(_notificationText) && _notificationTimer > 0f)
            {
                float bannerW = 380;
                float bannerX = wx + (ww - bannerW) / 2f;
                float bannerY = wy + wh - 34;
                DrawBox(new Rect(bannerX, bannerY, bannerW, 26), _texTerracotta, Color.white);
                GUI.Label(new Rect(bannerX + 10, bannerY + 4, bannerW - 20, 18), _notificationText, _notificationStyle);
            }

            // 6. On-Top Cursor (sorgt dafür, dass der Zeiger immer sichtbar über dem Overlay schwebt)
            if (_texCursor != null && Event.current != null)
            {
                Vector2 mp = Event.current.mousePosition;
                GUI.DrawTexture(new Rect(mp.x, mp.y, 16, 17), _texCursor);
            }
        }

        // ==================== HEADER BAR ====================
        private static void DrawHeader(float wx, float wy, float ww)
        {
            // 🏠 Home Icon Button (28x28)
            if (DrawButton(new Rect(wx + 16, wy + 11, 28, 28), "🏠", _activeTab == 0 ? _btnTerracottaStyle : _btnDarkStyle))
            {
                _activeTab = 0;
                _joinInputMode = false;
                AudioFeedback.PlayClick();
            }

            // Title
            GUI.Label(new Rect(wx + 50, wy + 15, 180, 18), "IronXNestCommand", _titleStyle);

            bool online = SteamworksDetector.IsInLobby;

            // Status Pill (99px Rounded Pill)
            float pillW = 110;
            float pillX = wx + ww - pillW - 60;
            float pillY = wy + 14;
            DrawBox(new Rect(pillX, pillY, pillW, 24), _texCardBg, new Color(0.820f, 0.800f, 0.765f, 1f));

            Texture2D dotTex = online ? _texDotGreen : _texDotGrey;
            GUI.DrawTexture(new Rect(pillX + 10, pillY + 9, 6, 6), dotTex);

            string statusLabel = online ? "Lobby offen" : "Keine Lobby";
            GUI.Label(new Rect(pillX + 22, pillY + 4, pillW - 26, 16), statusLabel, _statusPillStyle);

            // Hotkey Hint [F8]
            GUI.Label(new Rect(wx + ww - 52, wy + 17, 24, 16), Config.ToggleKey ?? "F8", _hotkeyStyle);

            // Close Button [✕]
            if (DrawButton(new Rect(wx + ww - 28, wy + 14, 20, 20), "✕", _btnDarkStyle))
            {
                SetVisible(false);
                AudioFeedback.PlayClick();
            }

            // Bottom line under header
            DrawDivider(new Rect(wx, wy + 48, ww, 1), new Color(0.820f, 0.800f, 0.765f, 1f));
        }

        // ==================== TAB 0: LOBBY & BESATZUNG CONTENT ====================
        private static void DrawLobbyCrewContent(float x, float y, float w)
        {
            bool inLobby = SteamworksDetector.IsInLobby;
            string rawShort = SteamworksDetector.CurrentLobbyShort?.Trim() ?? "";
            string formattedCode = FormatLobbyCode(rawShort);
            var players = SteamworksDetector.ConnectedPlayers;
            int maxSlots = 4;

            // ── 1. STEAM-LOBBY SECTION ──────────────────────────────────────────
            GUI.Label(new Rect(x, y, w, 14), "STEAM-LOBBY", _sectionHeaderStyle);
            y += 18;

            if (!inLobby && !_joinInputMode)
            {
                // Unconnected State (Dashed Container)
                DrawBox(new Rect(x, y, w, 82), _texCardDashed, new Color(0.820f, 0.800f, 0.765f, 0.8f));
                string desc = string.IsNullOrEmpty(SteamworksDetector.LastStatusMessage) || SteamworksDetector.LastStatusMessage == "Nicht initialisiert"
                    ? "Noch keine Lobby aktiv. Erzeuge eine Hex-ID oder trete einer Besatzung bei."
                    : $"Status: {SteamworksDetector.LastStatusMessage}";
                GUI.Label(new Rect(x + 14, y + 8, w - 28, 20), desc, _lobbySubtextStyle);

                float btnHalf = (w - 36) / 2f;
                if (DrawButton(new Rect(x + 14, y + 34, btnHalf, 34), "➕ Lobby erstellen", _btnTerracottaStyle))
                {
                    SteamworksDetector.TryCreateLobby(maxSlots);
                    AudioFeedback.PlayLevelUp();
                    ShowNotification("⏳ Erstelle neue Co-op Lobby...");
                }

                if (DrawButton(new Rect(x + 22 + btnHalf, y + 34, btnHalf, 34), "📥 Lobby Beitreten", _btnOutlineStyle))
                {
                    _joinInputMode = true;
                    AudioFeedback.PlayClick();
                }
                y += 92;
            }
            else if (!inLobby && _joinInputMode)
            {
                // Join Input Box
                DrawBox(new Rect(x, y, w, 84), _texCardBg, new Color(0.820f, 0.800f, 0.765f, 1f));
                GUI.Label(new Rect(x + 14, y + 8, w - 28, 16), "Lobby Hex-Code oder 64-Bit Steam-ID eingeben:", _lobbySubtextStyle);

                DrawBox(new Rect(x + 14, y + 28, w - 170, 28), _texMasterBg, new Color(0.820f, 0.800f, 0.765f, 1f));
                
                GUI.SetNextControlName("LobbyJoinInput");
                _lobbyIdInput = GUI.TextField(new Rect(x + 20, y + 30, w - 182, 24), _lobbyIdInput ?? "", 32, _inputFieldStyle ?? _memberNameStyle);

                if (DrawButton(new Rect(x + w - 150, y + 28, 64, 28), "📋 Paste", _btnOutlineStyle))
                {
                    string clip = GUIUtility.systemCopyBuffer?.Trim() ?? "";
                    if (!string.IsNullOrEmpty(clip))
                    {
                        _lobbyIdInput = clip;
                        AudioFeedback.PlayClick();
                        ShowNotification($"📋 Code eingefügt: {clip}");
                    }
                }

                bool hasInput = !string.IsNullOrWhiteSpace(_lobbyIdInput);
                bool submitJoin = false;

                if (DrawButton(new Rect(x + w - 82, y + 28, 70, 28), "Beitreten", hasInput ? _btnTerracottaStyle : _btnDarkStyle))
                {
                    submitJoin = true;
                }

                if (hasInput && Event.current != null && Event.current.isKey && Event.current.type == EventType.KeyDown && (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter))
                {
                    submitJoin = true;
                    Event.current.Use();
                }

                if (submitJoin && hasInput)
                {
                    SteamworksDetector.TryJoinLobby(_lobbyIdInput);
                    AudioFeedback.PlayClick();
                    ShowNotification($"⏳ Trete '{_lobbyIdInput.Trim()}' bei...");
                }

                if (DrawButton(new Rect(x + 14, y + 60, 100, 18), "← Zurück", _btnDarkStyle))
                {
                    _joinInputMode = false;
                    AudioFeedback.PlayClick();
                }
                y += 94;
            }
            else
            {
                // Active Lobby State (Large Hex-ID + Kopieren + Einladen)
                float boxH = 44;
                DrawBox(new Rect(x, y, w - 180, boxH), _texCardBg, new Color(0.820f, 0.800f, 0.765f, 1f));
                GUI.Label(new Rect(x + 14, y + 10, w - 240, 24), string.IsNullOrEmpty(formattedCode) ? "· · · ·" : formattedCode, _lobbyCodeStyle);
                GUI.Label(new Rect(x + w - 230, y + 14, 45, 16), "Hex-ID", _subtitleStyle);

                string copyLabel = _copiedFeedback ? "✔ Kopiert" : "Kopieren";
                if (DrawButton(new Rect(x + w - 172, y, 84, boxH), copyLabel, _btnTerracottaStyle))
                {
                    string toCopy = !string.IsNullOrEmpty(rawShort) ? rawShort : (SteamworksDetector.CurrentLobbyId != 0 ? SteamworksDetector.CurrentLobbyId.ToString() : formattedCode);
                    GUIUtility.systemCopyBuffer = toCopy;
                    _copiedFeedback = true;
                    _copiedTimer = 1.8f;
                    AudioFeedback.PlaySuccess();
                    ShowNotification("✔ Lobby-Code in Zwischenablage kopiert!");
                }

                if (DrawButton(new Rect(x + w - 82, y, 82, boxH), "Einladen", _btnOutlineStyle))
                {
                    if (SteamworksDetector.TryOpenInviteOverlay())
                    {
                        ShowNotification("👥 Steam Einladungs-Overlay geöffnet!");
                    }
                    else
                    {
                        string toCopy = !string.IsNullOrEmpty(rawShort) ? rawShort : (SteamworksDetector.CurrentLobbyId != 0 ? SteamworksDetector.CurrentLobbyId.ToString() : formattedCode);
                        GUIUtility.systemCopyBuffer = toCopy;
                        ShowNotification($"✔ Code '{toCopy}' kopiert!");
                    }
                    AudioFeedback.PlayClick();
                }

                y += boxH + 6;
                GUI.Label(new Rect(x, y, w - 90, 16), "Code an deine Besatzung weitergeben. Beitritt erfolgt direkt über das Steam-Overlay.", _lobbySubtextStyle);

                if (DrawButton(new Rect(x + w - 82, y - 2, 82, 20), "🚪 Verlassen", _btnDarkStyle))
                {
                    SteamworksDetector.TryLeaveLobby();
                    AudioFeedback.PlayClick();
                    ShowNotification("🚪 Lobby verlassen.");
                }
                y += 24;
            }

            // Divider
            DrawDivider(new Rect(x, y, w, 1), new Color(0.820f, 0.800f, 0.765f, 1f));
            y += 12;

            // ── 2. BESATZUNG SECTION ────────────────────────────────────────────
            int crewCount = players.Count > 0 ? players.Count : (inLobby ? 1 : 0);
            string crewCountText = $"{crewCount} / {maxSlots}";

            GUI.Label(new Rect(x, y, 100, 14), "BESATZUNG", _sectionHeaderStyle);
            GUI.Label(new Rect(x + w - 80, y, 80, 14), crewCountText, _hotkeyStyle);
            y += 18;

            if (players.Count == 0 && !inLobby)
            {
                // Offline / Einzelspieler Slot
                DrawMemberCard(x, y, w, "Du (Operator)", "HM", "👑 Kommandant (Lokal) · 0 ms", isHost: true);
                y += 44;
            }
            else
            {
                for (int i = 0; i < players.Count; i++)
                {
                    string pName = players[i];
                    string initials = GetInitials(pName);
                    string role = i == 0 ? "👑 Kommandant (Host) · 28 ms" : (i == 1 ? "🎯 Richtschütze · 34 ms" : (i == 2 ? "📦 Ladeschütze · 45 ms" : "🔭 Beobachter · 39 ms"));
                    DrawMemberCard(x, y, w, pName, initials, role, isHost: (i == 0));
                    y += 44;
                }
            }

            // Freier Platz an Rohr wenn weniger als maxSlots
            if (crewCount < maxSlots)
            {
                int freeSlot = crewCount + 1;
                DrawBox(new Rect(x, y, w, 36), _texCardDashed, new Color(0.820f, 0.800f, 0.765f, 0.8f));
                GUI.Label(new Rect(x, y + 9, w, 18), $"Freier Platz an Rohr {freeSlot}", _emptySlotStyle);
                y += 42;
            }

            y += 4;

            // ── 3. RE-SYNC ACTION ROW ───────────────────────────────────────────
            string resyncLabel = _syncing ? "⏳ Synchronisiere …" : "🔄 Besatzung re-syncen";
            if (DrawButton(new Rect(x, y, 170, 32), resyncLabel, _btnOutlineStyle))
            {
                _syncing = true;
                _syncTimer = 1.0f;
                AmmoRequisitionBridge.TriggerCoopResync();
                PunchcardSpawner.EnsureGuestFireMissionCard();
                AudioFeedback.PlaySuccess();
                ShowNotification("🔄 Lochkarten & Raum-Sync ausgeführt!");
            }

            float secondsAgo = _lastSyncTime > 0 ? (Time.unscaledTime - _lastSyncTime) : 4f;
            string stampText = _syncing ? "…" : $"Sync vor {Mathf.Max(1, (int)secondsAgo)} s";
            GUI.Label(new Rect(x + w - 120, y + 8, 120, 16), stampText, _hotkeyStyle);
        }

        // ==================== TAB 1: EINSTELLUNGEN CONTENT ====================
        private static void DrawSettingsContent(float x, float y, float w)
        {
            GUI.Label(new Rect(x, y, w, 14), "EINSTELLUNGEN & TASTENBELEGUNG", _sectionHeaderStyle);
            y += 20;

            DrawBox(new Rect(x, y, w, 192), _texCardBg, new Color(0.820f, 0.800f, 0.765f, 1f));
            float iy = y + 14;

            GUI.Label(new Rect(x + 14, iy, 180, 22), "Lobby-Overlay Hotkey:", _memberNameStyle);
            if (DrawButton(new Rect(x + 200, iy, 75, 24), $"[ {Config.ToggleKey} ]", _btnTerracottaStyle))
            {
                Config.ToggleKey = Config.ToggleKey switch
                {
                    "F8" => "F7",
                    "F7" => "F9",
                    "F9" => "F10",
                    "F10" => "F11",
                    "F11" => "F12",
                    _ => "F8"
                };
                AudioFeedback.PlayClick();
                ShowNotification($"Hotkey geändert auf: {Config.ToggleKey}");
            }
            GUI.Label(new Rect(x + 285, iy, 140, 22), "(Klick zum Wechseln)", _lobbySubtextStyle);

            iy += 36;
            Config.PreventEnemyDespawn = DrawToggle(new Rect(x + 14, iy, w - 28, 22), Config.PreventEnemyDespawn, "🛡️ Gegner-Despawn Schutz (Permanente Ziel-Sichtbarkeit)");

            iy += 32;
            Config.StartVisible = DrawToggle(new Rect(x + 14, iy, w - 28, 22), Config.StartVisible, "Lobbymenü beim Spielstart direkt anzeigen");

            iy += 32;
            Config.SoundFeedbackEnabled = DrawToggle(new Rect(x + 14, iy, w - 28, 22), Config.SoundFeedbackEnabled, "Audio-Rückmeldung bei Klicks & Aktionen");

            y += 204;
            float halfBtn = (w - 10) / 2f;
            if (DrawButton(new Rect(x, y, halfBtn, 32), "💾 SPEICHERN", _btnTerracottaStyle))
            {
                SaveManager.SaveConfig(Config);
                AudioFeedback.PlaySuccess();
                ShowNotification("✔ Einstellungen gespeichert!");
            }

            if (DrawButton(new Rect(x + halfBtn + 10, y, halfBtn, 32), "🏠 ZU HOME", _btnOutlineStyle))
            {
                _activeTab = 0;
                _joinInputMode = false;
                AudioFeedback.PlayClick();
            }
        }

        // ==================== MEMBER CARD RENDERER ====================
        private static void DrawMemberCard(float x, float y, float w, string name, string initials, string rolePing, bool isHost)
        {
            float cardH = 40;
            DrawBox(new Rect(x, y, w, cardH), _texCardBg, new Color(0.820f, 0.800f, 0.765f, 1f));

            // Initials Avatar Badge (26x26)
            DrawBox(new Rect(x + 8, y + 7, 26, 26), _texBadgeBg, new Color(0.247f, 0.247f, 0.275f, 1f));
            GUI.Label(new Rect(x + 8, y + 10, 26, 20), initials, _badgeInitialStyle);

            // Name & Role
            GUI.Label(new Rect(x + 42, y + 4, w - 50, 18), name, _memberNameStyle);
            GUI.Label(new Rect(x + 42, y + 20, w - 50, 16), rolePing, isHost ? _memberRoleStyle : _subtitleStyle);
        }

        // ==================== COMPONENT HELPERS ====================
        private static bool DrawButton(Rect rect, string label, GUIStyle style)
        {
            Vector2 mp = Event.current != null ? Event.current.mousePosition : new Vector2(-1, -1);
            bool hover = rect.Contains(mp);

            Texture2D bgTex = _texCardBg;
            Color borderColor = new Color(0.247f, 0.247f, 0.275f, 1f);

            if (style == _btnTerracottaStyle)
            {
                bgTex = hover ? _texTerracottaHover : _texTerracotta;
                borderColor = new Color(0.851f, 0.467f, 0.341f, 0.9f);
            }
            else if (style == _btnOutlineStyle)
            {
                bgTex = hover ? _texButtonDarkHover : _texCardBg;
                borderColor = hover ? new Color(0.851f, 0.467f, 0.341f, 0.8f) : new Color(0.247f, 0.247f, 0.275f, 1f);
            }
            else if (style == _btnDarkStyle)
            {
                bgTex = hover ? _texButtonDarkHover : _texButtonDark;
                borderColor = new Color(0.820f, 0.800f, 0.765f, 1f);
            }

            DrawBox(rect, bgTex, borderColor);

            var oldColor = GUI.color;
            if (hover && style != _btnTerracottaStyle) GUI.color = new Color(1.15f, 1.15f, 1.15f, 1f);
            GUI.Label(rect, label, style);
            GUI.color = oldColor;

            // Input.GetMouseButtonUp(0) bleibt für das gesamte physische Frame true, aber Unity IMGUI
            // ruft OnGUI pro Frame mehrfach auf (Layout- und Repaint-Pass) — dadurch feuerte ein
            // einzelner Klick den Button mehrfach. Event.current.type liefert pro Aufruf nur einmal
            // MouseUp, und Use() verhindert, dass darunterliegende Elemente dasselbe Event nochmal sehen.
            var evt = Event.current;
            if (hover && evt != null && evt.type == EventType.MouseUp && evt.button == 0)
            {
                evt.Use();
                return true;
            }

            return false;
        }

        private static bool DrawToggle(Rect rect, bool value, string label)
        {
            float toggleW = 48;
            var style = value ? _btnTerracottaStyle : _btnDarkStyle;
            string stateText = value ? "AN" : "AUS";

            if (DrawButton(new Rect(rect.x, rect.y, toggleW, rect.height), stateText, style))
            {
                value = !value;
                AudioFeedback.PlayClick();
            }

            GUI.Label(new Rect(rect.x + toggleW + 10, rect.y + 2, rect.width - toggleW - 10, rect.height), label, _memberNameStyle);
            return value;
        }

        private static void DrawBox(Rect rect, Texture2D bgTex, Color borderColor)
        {
            if (bgTex != null)
                GUI.DrawTexture(rect, bgTex);

            var oldColor = GUI.color;
            GUI.color = borderColor;

            // 1px Border
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, 1), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.y + rect.height - 1, rect.width, 1), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.y, 1, rect.height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x + rect.width - 1, rect.y, 1, rect.height), Texture2D.whiteTexture);

            GUI.color = oldColor;
        }

        private static void DrawDivider(Rect rect, Color color)
        {
            var oldColor = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = oldColor;
        }

        private static string FormatLobbyCode(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return "";
            if (raw.Length == 12 && !raw.Contains("-"))
            {
                return $"{raw.Substring(0, 4)}-{raw.Substring(4, 4)}-{raw.Substring(8, 4)}";
            }
            if (raw.Length == 8 && !raw.Contains("-"))
            {
                return $"{raw.Substring(0, 4)}-{raw.Substring(4, 4)}";
            }
            return raw;
        }

        private static string GetInitials(string name)
        {
            if (string.IsNullOrEmpty(name)) return "OP";
            string[] parts = name.Split(new[] { ' ', '_', '-' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
            {
                return $"{char.ToUpper(parts[0][0])}{char.ToUpper(parts[1][0])}";
            }
            return name.Length >= 2 ? name.Substring(0, 2).ToUpper() : name.ToUpper();
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        private static bool _toggleKeyWasPressed = false;

        private static int KeyCodeToVirtualKey(KeyCode code)
        {
            return code switch
            {
                KeyCode.F1 => 0x70,
                KeyCode.F2 => 0x71,
                KeyCode.F3 => 0x72,
                KeyCode.F4 => 0x73,
                KeyCode.F5 => 0x74,
                KeyCode.F6 => 0x75,
                KeyCode.F7 => 0x76,
                KeyCode.F8 => 0x77,
                KeyCode.F9 => 0x78,
                KeyCode.F10 => 0x79,
                KeyCode.F11 => 0x7A,
                KeyCode.F12 => 0x7B,
                _ => 0x77
            };
        }

        private static bool CheckToggleKey()
        {
            KeyCode targetKey = KeyCode.F8;
            if (!string.IsNullOrEmpty(Config.ToggleKey) && Enum.TryParse<KeyCode>(Config.ToggleKey, true, out var parsedKey))
            {
                targetKey = parsedKey;
            }

            // 1. Unity Legacy Input
            try
            {
                if (Input.GetKeyDown(targetKey) || (targetKey != KeyCode.F8 && Input.GetKeyDown(KeyCode.F8)))
                    return true;
            }
            catch { }

            // 2. Hardware / Win32 GetAsyncKeyState Fallback (Immune to locked cursor & New Input System modes)
            try
            {
                int vk = KeyCodeToVirtualKey(targetKey);
                bool isDown = (GetAsyncKeyState(vk) & 0x8000) != 0;
                if (targetKey != KeyCode.F8 && !isDown)
                {
                    isDown = (GetAsyncKeyState(0x77) & 0x8000) != 0;
                }

                if (isDown)
                {
                    if (!_toggleKeyWasPressed)
                    {
                        _toggleKeyWasPressed = true;
                        return true;
                    }
                }
                else
                {
                    _toggleKeyWasPressed = false;
                }
            }
            catch { }

            return false;
        }

        // ==================== STYLES & PALETTE INITIALIZER ====================
        private static void EnsureStyles()
        {
            if (_stylesInitialized && _texMasterBg != null) return;

            // Helles Farbschema (Warmes technisches Papier / High-Contrast)
            var colorMasterBg = new Color(0.965f, 0.960f, 0.948f, 1.0f);     // #F6F5F2 Heller Haupt-Hintergrund
            var colorCardBg = new Color(1.0f, 1.0f, 1.0f, 1.0f);             // #FFFFFF Reine weiße Karten
            var colorCardDashed = new Color(0.925f, 0.915f, 0.890f, 1.0f);   // #ECE9E3 Leere Slots
            var colorTerracotta = new Color(0.851f, 0.353f, 0.200f, 1.0f);   // #D95A33 Kräftiges Terrakotta
            var colorTerracottaHover = new Color(0.920f, 0.420f, 0.260f, 1f);// #EB6B42 Hover Terrakotta
            var colorBadgeBg = new Color(0.890f, 0.875f, 0.840f, 1.0f);      // #E3DFD6 Initialen-Badge
            var colorButtonDark = new Color(0.925f, 0.915f, 0.885f, 1.0f);   // #ECE9E2 Sekundär-Button
            var colorButtonDarkHover = new Color(0.865f, 0.845f, 0.810f, 1.0f);// #DDD7CF Button-Hover
            var colorBorderLight = new Color(0.880f, 0.865f, 0.835f, 1.0f);  // Hellerer Rahmen
            var colorDotGreen = new Color(0.063f, 0.680f, 0.420f, 1.0f);     // #10AD6B Grüner Punkt
            var colorDotGrey = new Color(0.600f, 0.590f, 0.570f, 1.0f);      // #999691 Grauer Punkt

            var textPrimary = new Color(0.094f, 0.094f, 0.110f, 1.0f);       // #18181C Tiefes Dunkelgrau
            var textSecondary = new Color(0.280f, 0.280f, 0.320f, 1.0f);     // #474752 Lesbares Mittelgrau
            var textMuted = new Color(0.420f, 0.420f, 0.470f, 1.0f);         // #6B6B78 Dezentes Grau
            var textSubtle = new Color(0.520f, 0.520f, 0.560f, 1.0f);        // #85858F
            var buttonWhiteText = new Color(1.0f, 1.0f, 1.0f, 1.0f);         // Weißer Text auf Buttons

            _texMasterBg = MakeColorTexture(colorMasterBg);
            _texCardBg = MakeColorTexture(colorCardBg);
            _texCardDashed = MakeColorTexture(colorCardDashed);
            _texTerracotta = MakeColorTexture(colorTerracotta);
            _texTerracottaHover = MakeColorTexture(colorTerracottaHover);
            _texBadgeBg = MakeColorTexture(colorBadgeBg);
            _texButtonDark = MakeColorTexture(colorButtonDark);
            _texButtonDarkHover = MakeColorTexture(colorButtonDarkHover);
            _texBorderLight = MakeColorTexture(colorBorderLight);
            _texDotGreen = MakeColorTexture(colorDotGreen);
            _texDotGrey = MakeColorTexture(colorDotGrey);
            _texCursor = CreateCursorTexture();

            _titleStyle = new GUIStyle { fontSize = 13, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft };
            _titleStyle.m_Normal = new GUIStyleState { textColor = textPrimary };

            _subtitleStyle = new GUIStyle { fontSize = 10, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft };
            _subtitleStyle.m_Normal = new GUIStyleState { textColor = colorTerracotta };

            _sectionHeaderStyle = new GUIStyle { fontSize = 10, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft };
            _sectionHeaderStyle.m_Normal = new GUIStyleState { textColor = textSecondary };

            _lobbyCodeStyle = new GUIStyle { fontSize = 15, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft };
            _lobbyCodeStyle.m_Normal = new GUIStyleState { textColor = textPrimary };

            _lobbySubtextStyle = new GUIStyle { fontSize = 11, alignment = TextAnchor.MiddleLeft };
            _lobbySubtextStyle.m_Normal = new GUIStyleState { textColor = textSecondary };

            _memberNameStyle = new GUIStyle { fontSize = 13, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft };
            _memberNameStyle.m_Normal = new GUIStyleState { textColor = textPrimary };

            _memberRoleStyle = new GUIStyle { fontSize = 10, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft };
            _memberRoleStyle.m_Normal = new GUIStyleState { textColor = colorTerracotta };

            _badgeInitialStyle = new GUIStyle { fontSize = 11, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            _badgeInitialStyle.m_Normal = new GUIStyleState { textColor = textPrimary };

            _emptySlotStyle = new GUIStyle { fontSize = 11, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            _emptySlotStyle.m_Normal = new GUIStyleState { textColor = textMuted };

            _statusPillStyle = new GUIStyle { fontSize = 11, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft };
            _statusPillStyle.m_Normal = new GUIStyleState { textColor = textPrimary };

            _btnTerracottaStyle = new GUIStyle { fontSize = 11, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            _btnTerracottaStyle.m_Normal = new GUIStyleState { textColor = buttonWhiteText };

            _btnOutlineStyle = new GUIStyle { fontSize = 11, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            _btnOutlineStyle.m_Normal = new GUIStyleState { textColor = textPrimary };

            _btnDarkStyle = new GUIStyle { fontSize = 11, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            _btnDarkStyle.m_Normal = new GUIStyleState { textColor = textSecondary };

            _hotkeyStyle = new GUIStyle { fontSize = 10, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleRight };
            _hotkeyStyle.m_Normal = new GUIStyleState { textColor = textSecondary };

            _notificationStyle = new GUIStyle { fontSize = 11, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            _notificationStyle.m_Normal = new GUIStyleState { textColor = buttonWhiteText };

            _inputFieldStyle = new GUIStyle
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };
            _inputFieldStyle.m_Normal = new GUIStyleState { textColor = textPrimary };

            _stylesInitialized = true;
        }

        private static Texture2D MakeColorTexture(Color color)
        {
            var texture = new Texture2D(1, 1, TextureFormat.ARGB32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Point,
                hideFlags = HideFlags.HideAndDontSave
            };
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }

        private static readonly string[] CursorBitmap = new string[]
        {
            "X               ",
            "XX              ",
            "X.X             ",
            "X..X            ",
            "X...X           ",
            "X....X          ",
            "X.....X         ",
            "X......X        ",
            "X.......X       ",
            "X........X      ",
            "X.....XXXXX     ",
            "X..X..X         ",
            "X.X X..X        ",
            "XX  X..X        ",
            "X    X..X       ",
            "     X..X       ",
            "      XX        "
        };

        private static Texture2D CreateCursorTexture()
        {
            int height = CursorBitmap.Length;
            int width = CursorBitmap[0].Length;
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Point,
                hideFlags = HideFlags.HideAndDontSave
            };

            var dark = new Color(0.094f, 0.094f, 0.110f, 1.0f);
            var light = new Color(1.0f, 1.0f, 1.0f, 1.0f);

            for (int y = 0; y < height; y++)
            {
                string row = CursorBitmap[y];
                for (int x = 0; x < width; x++)
                {
                    char c = x < row.Length ? row[x] : ' ';
                    Color col = c switch
                    {
                        'X' => dark,
                        '.' => light,
                        _ => Color.clear
                    };
                    tex.SetPixel(x, height - 1 - y, col);
                }
            }
            tex.Apply();
            return tex;
        }

        private void OnDestroy()
        {
            if (_texMasterBg != null) Destroy(_texMasterBg);
            if (_texCardBg != null) Destroy(_texCardBg);
            if (_texCardDashed != null) Destroy(_texCardDashed);
            if (_texTerracotta != null) Destroy(_texTerracotta);
            if (_texTerracottaHover != null) Destroy(_texTerracottaHover);
            if (_texBadgeBg != null) Destroy(_texBadgeBg);
            if (_texButtonDark != null) Destroy(_texButtonDark);
            if (_texButtonDarkHover != null) Destroy(_texButtonDarkHover);
            if (_texBorderLight != null) Destroy(_texBorderLight);
            if (_texDotGreen != null) Destroy(_texDotGreen);
            if (_texDotGrey != null) Destroy(_texDotGrey);
            if (_texCursor != null) Destroy(_texCursor);
        }
    }
}
