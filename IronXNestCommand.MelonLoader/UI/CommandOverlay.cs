using System;
using System.Collections.Generic;
using UnityEngine;
using MelonLoader;
using IronXNestCommand.Ammo;
using IronXNestCommand.Core;
using IronXNestCommand.Patches;
using IronXNestCommand.Steam;

namespace IronXNestCommand.UI
{
    /// <summary>
    /// IronXNestCommand // Lobby & Besatzung (MelonLoader Modus)
    /// 1:1 Pixel- und Token-getreue Umsetzung der offiziellen GUI-Vorlage.
    /// Reines Co-op Lobby- & Besatzungs-Management mit Lochkarten-Sync für Iron Nest.
    /// </summary>
    public static class CommandOverlay
    {
        public static bool IsVisible { get; set; } = true;
        public static ModConfig Config { get; set; } = new();

        // ── Window Layout (520px Breite gemäß Vorlage) ─────────────────────────
        private static Rect _windowRect = new(60, 60, 520, 480);
        private static bool _isDragging = false;
        private static Vector2 _dragOffset = Vector2.zero;

        // ── Tabs ───────────────────────────────────────────────────────────────
        private static int _activeTab = 0; // 0 = Lobby & Besatzung, 1 = Einstellungen
        private static readonly string[] TabNames = { "🌐 LOBBY & BESATZUNG", "⚙️ EINSTELLUNGEN" };

        // ── State & Animations ─────────────────────────────────────────────────
        private static string _lobbyIdInput = "";
        private static bool _joinInputMode = false;
        private static bool _copiedFeedback = false;
        private static float _copiedTimer = 0f;
        private static bool _syncing = false;
        private static float _syncTimer = 0f;
        private static float _lastSyncTime = 0f;

        // Notification Banner
        private static string _notificationText = "";
        private static float _notificationTimer = 0f;

        // ── Textures & Color Palette (#0E0E10 / #18181B / #D97757) ─────────────
        private static Texture2D _texMasterBg;
        private static Texture2D _texCardBg;
        private static Texture2D _texCardDashed;
        private static Texture2D _texFooterBg;
        private static Texture2D _texTerracotta;
        private static Texture2D _texTerracottaHover;
        private static Texture2D _texBadgeBg;
        private static Texture2D _texButtonDark;
        private static Texture2D _texButtonDarkHover;
        private static Texture2D _texBorder;
        private static Texture2D _texDotGreen;
        private static Texture2D _texDotGrey;

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
        private static GUIStyle _footerTextStyle;
        private static GUIStyle _hotkeyStyle;
        private static GUIStyle _notificationStyle;

        private static bool _stylesInitialized = false;

        public static void Initialize(ModConfig config)
        {
            Config = config ?? new ModConfig();
            IsVisible = Config.StartVisible;
            AudioFeedback.Initialize();
            AmmoRequisitionBridge.Initialize();
        }

        public static void Update()
        {
            float dt = Time.unscaledDeltaTime;

            if (CheckToggleKey())
                IsVisible = !IsVisible;

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

            CoopPunchcardFix.UpdateWatchdog(dt);
            EnemyDespawnGuard.UpdateWatchdog(dt);
        }

        public static void ShowNotification(string text, float duration = 2.5f)
        {
            _notificationText = text;
            _notificationTimer = duration;
        }

        public static void OnGUI()
        {
            if (!IsVisible) return;

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
            DrawBox(new Rect(wx, wy, ww, wh), _texMasterBg, new Color(0.153f, 0.153f, 0.165f, 1f));

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

            DrawDivider(new Rect(wx + 18, tabY + 32, ww - 36, 1), new Color(0.153f, 0.153f, 0.165f, 1f));

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
                float bannerY = wy + wh - 64;
                DrawBox(new Rect(bannerX, bannerY, bannerW, 26), _texTerracotta, Color.white);
                GUI.Label(new Rect(bannerX + 10, bannerY + 4, bannerW - 20, 18), _notificationText, _notificationStyle);
            }

            // 6. Footer Status Bar
            DrawFooter(wx, wy + wh - 34, ww);
        }

        // ==================== HEADER BAR ====================
        private static void DrawHeader(float wx, float wy, float ww)
        {
            // Accent Terracotta Icon (26x26 mit innerem Kreis)
            DrawBox(new Rect(wx + 18, wy + 12, 26, 26), _texTerracotta, new Color(0.851f, 0.467f, 0.341f, 1f));
            DrawBox(new Rect(wx + 25, wy + 19, 12, 12), _texMasterBg, new Color(0.094f, 0.094f, 0.106f, 1f));

            // Title & Subtitle
            GUI.Label(new Rect(wx + 52, wy + 10, 200, 18), "IronXNestCommand", _titleStyle);
            GUI.Label(new Rect(wx + 52, wy + 26, 200, 14), "LOBBY & BESATZUNG", _subtitleStyle);

            bool online = SteamworksDetector.IsInLobby;

            // Status Pill (99px Rounded Pill)
            float pillW = 110;
            float pillX = wx + ww - pillW - 60;
            float pillY = wy + 14;
            DrawBox(new Rect(pillX, pillY, pillW, 24), _texCardBg, new Color(0.153f, 0.153f, 0.165f, 1f));

            Texture2D dotTex = online ? _texDotGreen : _texDotGrey;
            GUI.DrawTexture(new Rect(pillX + 10, pillY + 9, 6, 6), dotTex);

            string statusLabel = online ? "Lobby offen" : "Keine Lobby";
            GUI.Label(new Rect(pillX + 22, pillY + 4, pillW - 26, 16), statusLabel, _statusPillStyle);

            // Hotkey Hint [F8]
            GUI.Label(new Rect(wx + ww - 52, wy + 17, 24, 16), Config.ToggleKey ?? "F8", _hotkeyStyle);

            // Close Button [✕]
            if (DrawButton(new Rect(wx + ww - 28, wy + 14, 20, 20), "✕", _btnDarkStyle))
            {
                IsVisible = false;
                AudioFeedback.PlayClick();
            }

            // Bottom line under header
            DrawDivider(new Rect(wx, wy + 48, ww, 1), new Color(0.153f, 0.153f, 0.165f, 1f));
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
                DrawBox(new Rect(x, y, w, 78), _texCardDashed, new Color(0.153f, 0.153f, 0.165f, 0.8f));
                GUI.Label(new Rect(x + 14, y + 10, w - 28, 20), "Noch keine Lobby aktiv. Erzeuge eine Hex-ID oder trete einer Besatzung bei.", _lobbySubtextStyle);

                float btnHalf = (w - 36) / 2f;
                if (DrawButton(new Rect(x + 14, y + 36, btnHalf, 30), "➕ Lobby-ID generieren", _btnTerracottaStyle))
                {
                    SteamworksDetector.TryCreateLobby(maxSlots);
                    AudioFeedback.PlayLevelUp();
                    ShowNotification("⏳ Erstelle neue Co-op Lobby...");
                }

                if (DrawButton(new Rect(x + 22 + btnHalf, y + 36, btnHalf, 30), "📥 Lobby Beitreten", _btnOutlineStyle))
                {
                    _joinInputMode = true;
                    AudioFeedback.PlayClick();
                }
                y += 88;
            }
            else if (!inLobby && _joinInputMode)
            {
                // Join Input Box
                DrawBox(new Rect(x, y, w, 84), _texCardBg, new Color(0.247f, 0.247f, 0.275f, 1f));
                GUI.Label(new Rect(x + 14, y + 8, w - 28, 16), "Lobby Hex-Code oder 64-Bit Steam-ID eingeben:", _lobbySubtextStyle);

                DrawBox(new Rect(x + 14, y + 28, w - 170, 28), _texMasterBg, new Color(0.247f, 0.247f, 0.275f, 1f));
                string disp = string.IsNullOrEmpty(_lobbyIdInput) ? "<Hex-Code oder ID>" : _lobbyIdInput;
                GUI.Label(new Rect(x + 22, y + 32, w - 186, 20), disp, string.IsNullOrEmpty(_lobbyIdInput) ? _lobbySubtextStyle : _memberNameStyle);

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
                if (DrawButton(new Rect(x + w - 82, y + 28, 70, 28), "Beitreten", hasInput ? _btnTerracottaStyle : _btnDarkStyle))
                {
                    if (hasInput)
                    {
                        SteamworksDetector.TryJoinLobby(_lobbyIdInput);
                        AudioFeedback.PlayClick();
                        ShowNotification($"⏳ Trete '{_lobbyIdInput.Trim()}' bei...");
                    }
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
                DrawBox(new Rect(x, y, w - 180, boxH), _texCardBg, new Color(0.153f, 0.153f, 0.165f, 1f));
                GUI.Label(new Rect(x + 14, y + 10, w - 240, 24), string.IsNullOrEmpty(formattedCode) ? "· · · ·" : formattedCode, _lobbyCodeStyle);
                GUI.Label(new Rect(x + w - 230, y + 14, 45, 16), "Hex-ID", _subtitleStyle);

                string copyLabel = _copiedFeedback ? "✔ Kopiert" : "Kopieren";
                if (DrawButton(new Rect(x + w - 172, y, 84, boxH), copyLabel, _btnTerracottaStyle))
                {
                    GUIUtility.systemCopyBuffer = string.IsNullOrEmpty(rawShort) ? formattedCode : rawShort;
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
                        GUIUtility.systemCopyBuffer = rawShort;
                        ShowNotification($"✔ Code '{rawShort}' kopiert!");
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
            DrawDivider(new Rect(x, y, w, 1), new Color(0.153f, 0.153f, 0.165f, 1f));
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
                DrawBox(new Rect(x, y, w, 36), _texCardDashed, new Color(0.153f, 0.153f, 0.165f, 0.8f));
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

            DrawBox(new Rect(x, y, w, 192), _texCardBg, new Color(0.153f, 0.153f, 0.165f, 1f));
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
            if (DrawButton(new Rect(x, y, w, 32), "💾 EINSTELLUNGEN SPEICHERN", _btnTerracottaStyle))
            {
                SaveManager.SaveConfig(Config);
                AudioFeedback.PlaySuccess();
                ShowNotification("✔ Einstellungen gespeichert!");
            }
        }

        // ==================== MEMBER CARD RENDERER ====================
        private static void DrawMemberCard(float x, float y, float w, string name, string initials, string rolePing, bool isHost)
        {
            float cardH = 40;
            DrawBox(new Rect(x, y, w, cardH), _texCardBg, new Color(0.153f, 0.153f, 0.165f, 1f));

            // Initials Avatar Badge (26x26)
            DrawBox(new Rect(x + 8, y + 7, 26, 26), _texBadgeBg, new Color(0.247f, 0.247f, 0.275f, 1f));
            GUI.Label(new Rect(x + 8, y + 10, 26, 20), initials, _badgeInitialStyle);

            // Name & Role
            GUI.Label(new Rect(x + 42, y + 4, w - 50, 18), name, _memberNameStyle);
            GUI.Label(new Rect(x + 42, y + 20, w - 50, 16), rolePing, isHost ? _memberRoleStyle : _subtitleStyle);
        }

        // ==================== FOOTER STATUS BAR ====================
        private static void DrawFooter(float wx, float wy, float ww)
        {
            DrawBox(new Rect(wx, wy, ww, 34), _texFooterBg, new Color(0.153f, 0.153f, 0.165f, 1f));

            bool online = SteamworksDetector.IsInLobby;
            Texture2D dotTex = online ? _texDotGreen : _texDotGrey;
            GUI.DrawTexture(new Rect(wx + 18, wy + 14, 5, 5), dotTex);

            string syncState = _syncing ? "SYNC LÄUFT" : (online ? "SYNCHRON" : "IM LEERLAUF");
            GUI.Label(new Rect(wx + 28, wy + 9, 85, 16), syncState, _footerTextStyle);

            // Vertical divider
            DrawDivider(new Rect(wx + 118, wy + 11, 1, 12), new Color(0.153f, 0.153f, 0.165f, 1f));

            // Relay Label
            string relayLabel = online ? "Relay Steam P2P · 28 ms" : "Kein Relay";
            GUI.Label(new Rect(wx + 128, wy + 9, 150, 16), relayLabel, _subtitleStyle);

            // Mini Progress Bar (2px height)
            float barX = wx + 280;
            float barW = ww - 380;
            float barY = wy + 16;
            DrawBox(new Rect(barX, barY, barW, 2), _texBorder, new Color(0.153f, 0.153f, 0.165f, 1f));
            float fillW = _syncing ? barW * 0.5f : (online ? barW : 0f);
            if (fillW > 0)
            {
                GUI.DrawTexture(new Rect(barX, barY, fillW, 2), _texTerracotta);
            }

            // Sync stamp
            float secondsAgo = _lastSyncTime > 0 ? (Time.unscaledTime - _lastSyncTime) : 4f;
            string stamp = _syncing ? "…" : $"Sync {Mathf.Max(1, (int)secondsAgo)}s";
            GUI.Label(new Rect(wx + ww - 95, wy + 9, 85, 16), stamp, _hotkeyStyle);
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
                borderColor = new Color(0.153f, 0.153f, 0.165f, 1f);
            }

            DrawBox(rect, bgTex, borderColor);

            var oldColor = GUI.color;
            if (hover && style != _btnTerracottaStyle) GUI.color = new Color(1.15f, 1.15f, 1.15f, 1f);
            GUI.Label(rect, label, style);
            GUI.color = oldColor;

            if (hover && Input.GetMouseButtonUp(0))
                return true;

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

        private static bool CheckToggleKey()
        {
            try
            {
                if (Input.GetKeyDown(KeyCode.F8)) return true;
                if (!string.IsNullOrEmpty(Config.ToggleKey) && Enum.TryParse<KeyCode>(Config.ToggleKey, true, out var key))
                {
                    if (Input.GetKeyDown(key)) return true;
                }
            }
            catch { }
            return false;
        }

        // ==================== STYLES & PALETTE INITIALIZER ====================
        private static void EnsureStyles()
        {
            if (_stylesInitialized && _texMasterBg != null) return;

            // Template Color Tokens
            var colorMasterBg = new Color(0.094f, 0.094f, 0.106f, 0.98f);     // #18181B Master Surface
            var colorCardBg = new Color(0.122f, 0.118f, 0.114f, 1.0f);       // #1F1E1D Card Surface
            var colorCardDashed = new Color(0.110f, 0.110f, 0.122f, 1.0f);   // #1C1C1F Empty/Dashed Card
            var colorFooterBg = new Color(0.078f, 0.078f, 0.086f, 1.0f);     // #141416 Footer Surface
            var colorTerracotta = new Color(0.851f, 0.467f, 0.341f, 1.0f);   // #D97757 Terracotta Accent
            var colorTerracottaHover = new Color(0.800f, 0.471f, 0.361f, 1f); // #CC785C Hover Accent
            var colorBadgeBg = new Color(0.153f, 0.153f, 0.165f, 1.0f);      // #27272A Badge Pill
            var colorButtonDark = new Color(0.13f, 0.13f, 0.14f, 1.0f);
            var colorButtonDarkHover = new Color(0.18f, 0.18f, 0.20f, 1.0f);
            var colorBorder = new Color(0.153f, 0.153f, 0.165f, 1.0f);       // #27272A Border
            var colorDotGreen = new Color(0.063f, 0.725f, 0.506f, 1.0f);     // #10B981 Green Dot
            var colorDotGrey = new Color(0.322f, 0.322f, 0.357f, 1.0f);      // #52525B Grey Dot

            var paperWhite = new Color(0.980f, 0.976f, 0.961f, 1.0f);       // #FAF9F5
            var textSecondary = new Color(0.831f, 0.831f, 0.847f, 1.0f);    // #D4D4D8
            var textMuted = new Color(0.443f, 0.443f, 0.478f, 1.0f);        // #71717A
            var textSubtle = new Color(0.322f, 0.322f, 0.357f, 1.0f);       // #52525B
            var darkButtonText = new Color(0.122f, 0.118f, 0.114f, 1.0f);   // #1F1E1D

            _texMasterBg = MakeColorTexture(colorMasterBg);
            _texCardBg = MakeColorTexture(colorCardBg);
            _texCardDashed = MakeColorTexture(colorCardDashed);
            _texFooterBg = MakeColorTexture(colorFooterBg);
            _texTerracotta = MakeColorTexture(colorTerracotta);
            _texTerracottaHover = MakeColorTexture(colorTerracottaHover);
            _texBadgeBg = MakeColorTexture(colorBadgeBg);
            _texButtonDark = MakeColorTexture(colorButtonDark);
            _texButtonDarkHover = MakeColorTexture(colorButtonDarkHover);
            _texBorder = MakeColorTexture(colorBorder);
            _texDotGreen = MakeColorTexture(colorDotGreen);
            _texDotGrey = MakeColorTexture(colorDotGrey);

            _titleStyle = new GUIStyle { fontSize = 13, alignment = TextAnchor.MiddleLeft };
            _titleStyle.m_Normal = new GUIStyleState { textColor = paperWhite };

            _subtitleStyle = new GUIStyle { fontSize = 10, alignment = TextAnchor.MiddleLeft };
            _subtitleStyle.m_Normal = new GUIStyleState { textColor = textSubtle };

            _sectionHeaderStyle = new GUIStyle { fontSize = 10, alignment = TextAnchor.MiddleLeft };
            _sectionHeaderStyle.m_Normal = new GUIStyleState { textColor = textMuted };

            _lobbyCodeStyle = new GUIStyle { fontSize = 15, alignment = TextAnchor.MiddleLeft };
            _lobbyCodeStyle.m_Normal = new GUIStyleState { textColor = paperWhite };

            _lobbySubtextStyle = new GUIStyle { fontSize = 11, alignment = TextAnchor.MiddleLeft };
            _lobbySubtextStyle.m_Normal = new GUIStyleState { textColor = textSubtle };

            _memberNameStyle = new GUIStyle { fontSize = 13, alignment = TextAnchor.MiddleLeft };
            _memberNameStyle.m_Normal = new GUIStyleState { textColor = paperWhite };

            _memberRoleStyle = new GUIStyle { fontSize = 10, alignment = TextAnchor.MiddleLeft };
            _memberRoleStyle.m_Normal = new GUIStyleState { textColor = colorTerracotta };

            _badgeInitialStyle = new GUIStyle { fontSize = 11, alignment = TextAnchor.MiddleCenter };
            _badgeInitialStyle.m_Normal = new GUIStyleState { textColor = new Color(0.631f, 0.631f, 0.667f, 1f) };

            _emptySlotStyle = new GUIStyle { fontSize = 11, alignment = TextAnchor.MiddleCenter };
            _emptySlotStyle.m_Normal = new GUIStyleState { textColor = textSubtle };

            _statusPillStyle = new GUIStyle { fontSize = 11, alignment = TextAnchor.MiddleLeft };
            _statusPillStyle.m_Normal = new GUIStyleState { textColor = textSecondary };

            _btnTerracottaStyle = new GUIStyle { fontSize = 11, alignment = TextAnchor.MiddleCenter };
            _btnTerracottaStyle.m_Normal = new GUIStyleState { textColor = darkButtonText };

            _btnOutlineStyle = new GUIStyle { fontSize = 11, alignment = TextAnchor.MiddleCenter };
            _btnOutlineStyle.m_Normal = new GUIStyleState { textColor = textSecondary };

            _btnDarkStyle = new GUIStyle { fontSize = 11, alignment = TextAnchor.MiddleCenter };
            _btnDarkStyle.m_Normal = new GUIStyleState { textColor = textMuted };

            _footerTextStyle = new GUIStyle { fontSize = 10, alignment = TextAnchor.MiddleLeft };
            _footerTextStyle.m_Normal = new GUIStyleState { textColor = new Color(0.631f, 0.631f, 0.667f, 1f) };

            _hotkeyStyle = new GUIStyle { fontSize = 10, alignment = TextAnchor.MiddleRight };
            _hotkeyStyle.m_Normal = new GUIStyleState { textColor = new Color(0.247f, 0.247f, 0.275f, 1f) };

            _notificationStyle = new GUIStyle { fontSize = 11, alignment = TextAnchor.MiddleCenter };
            _notificationStyle.m_Normal = new GUIStyleState { textColor = darkButtonText };

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
    }
}
