using System;
using System.Collections.Generic;
using UnityEngine;
using IronXNestCommand.Ammo;
using IronXNestCommand.Core;
using IronXNestCommand.Economy;
using IronXNestCommand.Progression;
using IronXNestCommand.Steam;

namespace IronXNestCommand.UI
{
    public static class CommandOverlay
    {
        public static bool IsVisible { get; set; } = true;
        public static ModConfig Config { get; set; } = new ModConfig();

        private static int _activeTab = 0;
        private static readonly string[] TabNames = { "STATUS", "ADVISOR", "ECONOMY", "RANKS", "CONFIG" };

        private static TargetCategory _selectedTarget = TargetCategory.MediumArmor;
        private static Vector2 _scrollPos = Vector2.zero;

        private static Texture2D _texBg;
        private static Texture2D _texHeader;
        private static Texture2D _texButton;
        private static Texture2D _texButtonActive;
        private static Texture2D _texProgressBarBg;
        private static Texture2D _texProgressBarFill;

        private static GUIStyle _titleStyle;
        private static GUIStyle _headerStyle;
        private static GUIStyle _textStyle;
        private static GUIStyle _highlightStyle;
        private static GUIStyle _mutedStyle;
        private static GUIStyle _dangerStyle;
        private static GUIStyle _tabButtonStyle;
        private static GUIStyle _activeTabButtonStyle;
        private static GUIStyle _actionButtonStyle;
        private static GUIStyle _boxStyle;

        private static Rect _windowRect = new Rect(25, 25, 620, 480);
        private static bool _stylesInitialized = false;

        public static void Initialize(ModConfig config)
        {
            Config = config ?? new ModConfig();
            IsVisible = Config.StartVisible;
        }

        public static void Update()
        {
            if (CheckToggleKey())
            {
                IsVisible = !IsVisible;
            }
        }

        public static void OnGUI()
        {
            if (!IsVisible)
                return;

            EnsureStyles();

            _windowRect = GUI.Window(889102, _windowRect, (GUI.WindowFunction)DrawWindowContent, "IRON X NEST COMMAND // OPERATOR CONSOLE");
        }

        private static void DrawWindowContent(int windowId)
        {
            GUI.DragWindow(new Rect(0, 0, _windowRect.width - 40, 24));

            if (GUI.Button(new Rect(_windowRect.width - 32, 4, 24, 18), "X", _actionButtonStyle))
            {
                IsVisible = false;
            }

            GUILayout.Space(10);

            // ================= TABS =================
            GUILayout.BeginHorizontal();
            for (int i = 0; i < TabNames.Length; i++)
            {
                var style = (i == _activeTab) ? _activeTabButtonStyle : _tabButtonStyle;
                if (GUILayout.Button(TabNames[i], style, GUILayout.Height(28)))
                {
                    _activeTab = i;
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(8);

            // Divider Line
            DrawHorizontalLine(new Color(0.769f, 0.639f, 0.353f, 0.6f), 2);
            GUILayout.Space(8);

            _scrollPos = GUILayout.BeginScrollView(_scrollPos, GUILayout.Width(_windowRect.width - 25), GUILayout.Height(370));

            switch (_activeTab)
            {
                case 0:
                    DrawStatusTab();
                    break;
                case 1:
                    DrawAdvisorTab();
                    break;
                case 2:
                    DrawEconomyTab();
                    break;
                case 3:
                    DrawProgressionTab();
                    break;
                case 4:
                    DrawConfigTab();
                    break;
            }

            GUILayout.EndScrollView();

            // Footer
            GUILayout.Space(4);
            GUILayout.BeginHorizontal();
            GUILayout.Label($"[Hotkey: {Config.ToggleKey}]", _mutedStyle);
            GUILayout.FlexibleSpace();
            var rank = ProgressionManager.GetCurrentRank();
            GUILayout.Label($"Operator: {rank.Title} · Level {rank.Level}", _highlightStyle);
            GUILayout.EndHorizontal();
        }

        // ==================== TAB 1: STATUS ====================
        private static void DrawStatusTab()
        {
            GUILayout.Label("SYSTEM & MULTIPLAYER STATUS", _headerStyle);
            GUILayout.Space(4);

            GUILayout.BeginVertical(_boxStyle);
            
            GUILayout.BeginHorizontal();
            GUILayout.Label("FairnessGuard Status:", _textStyle, GUILayout.Width(180));
            if (FairnessGuard.IsMultiplayerActive)
            {
                GUILayout.Label("● MULTIPLAYER AKTIV (Cheatschutz an)", _dangerStyle);
            }
            else
            {
                GUILayout.Label("● SINGLEPLAYER (Alle Funktionen aktiv)", _highlightStyle);
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Steam-Lobby:", _textStyle, GUILayout.Width(180));
            if (SteamworksDetector.IsInLobby)
            {
                GUILayout.Label($"Lobby ID: {SteamworksDetector.CurrentLobbyId}", _textStyle);
            }
            else
            {
                GUILayout.Label("Keine aktive Steam-Lobby", _mutedStyle);
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Erkannte Co-op Mods:", _textStyle, GUILayout.Width(180));
            if (ModCompatibility.OtherCoopModDetected)
            {
                GUILayout.Label($"Ja ({ModCompatibility.DetectedModName})", _highlightStyle);
            }
            else
            {
                GUILayout.Label("Keine (Eigenständiger Betrieb)", _mutedStyle);
            }
            GUILayout.EndHorizontal();

            if (SteamworksDetector.ConnectedPlayers.Count > 0)
            {
                GUILayout.Space(6);
                GUILayout.Label("Mitspieler in der Sitzung:", _textStyle);
                foreach (var player in SteamworksDetector.ConnectedPlayers)
                {
                    GUILayout.Label($"  - {player}", _mutedStyle);
                }
            }

            GUILayout.EndVertical();

            GUILayout.Space(12);
            GUILayout.Label("TEST & SIMULATION (Schnelltest)", _headerStyle);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Missionssieg simulieren (+250 XP, +Tokens)", _actionButtonStyle, GUILayout.Height(28)))
            {
                ProgressionManager.RecordMissionFinished(true, 15, 12, 2);
            }
            if (GUILayout.Button("Multiplayer Toggle", _actionButtonStyle, GUILayout.Height(28)))
            {
                FairnessGuard.SetMultiplayerState(!FairnessGuard.IsMultiplayerActive);
            }
            GUILayout.EndHorizontal();
        }

        // ==================== TAB 2: ADVISOR & LOADOUTS ====================
        private static void DrawAdvisorTab()
        {
            GUILayout.Label("BALLISTISCHER AMMO ADVISOR", _headerStyle);
            GUILayout.Label("Wähle den Zieltyp zur automatischen Berechnung der optimalen Ladung:", _mutedStyle);
            GUILayout.Space(6);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Infanterie", _selectedTarget == TargetCategory.InfantrySquad ? _activeTabButtonStyle : _actionButtonStyle))
                _selectedTarget = TargetCategory.InfantrySquad;
            if (GUILayout.Button("Spähwagen", _selectedTarget == TargetCategory.LightVehicle ? _activeTabButtonStyle : _actionButtonStyle))
                _selectedTarget = TargetCategory.LightVehicle;
            if (GUILayout.Button("Panzer", _selectedTarget == TargetCategory.MediumArmor ? _activeTabButtonStyle : _actionButtonStyle))
                _selectedTarget = TargetCategory.MediumArmor;
            if (GUILayout.Button("Bunker", _selectedTarget == TargetCategory.HeavyBunker ? _activeTabButtonStyle : _actionButtonStyle))
                _selectedTarget = TargetCategory.HeavyBunker;
            if (GUILayout.Button("Artillerie", _selectedTarget == TargetCategory.CounterBatteryArtillery ? _activeTabButtonStyle : _actionButtonStyle))
                _selectedTarget = TargetCategory.CounterBatteryArtillery;
            GUILayout.EndHorizontal();

            GUILayout.Space(8);

            var rec = AmmoAdvisor.GetRecommendation(_selectedTarget);
            GUILayout.BeginVertical(_boxStyle);
            GUILayout.Label($"ZIELANALYSE: {rec.TargetName.ToUpper()}", _headerStyle);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Empfohlene Shell:", _textStyle, GUILayout.Width(150));
            GUILayout.Label(rec.RecommendedShellName, _highlightStyle);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Treibladung (Powder):", _textStyle, GUILayout.Width(150));
            GUILayout.Label($"{rec.RecommendedPowderCharges} Ladungen", _highlightStyle);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Durchschlagsklasse:", _textStyle, GUILayout.Width(150));
            GUILayout.Label(rec.PenetrationRating, _textStyle);
            GUILayout.EndHorizontal();

            GUILayout.Space(4);
            GUILayout.Label($"Taktischer Hinweis: {rec.TacticalAdvice}", _mutedStyle);
            GUILayout.EndVertical();

            GUILayout.Space(10);
            GUILayout.Label("REGISTRIERTE CUSTOM SHELLS", _headerStyle);
            foreach (var shell in CustomShellManager.GetAllCustomShells())
            {
                GUILayout.BeginVertical(_boxStyle);
                GUILayout.BeginHorizontal();
                GUILayout.Label($"[ {shell.Name} ]", _highlightStyle);
                GUILayout.FlexibleSpace();
                GUILayout.Label($"Kosten: {shell.RequisitionCost} Req" + (shell.CommandFavorCost > 0 ? $" / {shell.CommandFavorCost} Favor" : ""), _mutedStyle);
                GUILayout.EndHorizontal();
                GUILayout.Label($"Schaden: {shell.KineticDamage} Kin / {shell.ExplosiveDamage} Exp · Durchschlag: {shell.ArmorPenetration}mm · Radius: {shell.BlastRadius}m", _textStyle);
                GUILayout.Label(shell.Description, _mutedStyle);
                GUILayout.EndVertical();
            }
        }

        // ==================== TAB 3: ECONOMY ====================
        private static void DrawEconomyTab()
        {
            GUILayout.Label("LOGISTIK & RESSOURCEN-KONTEN", _headerStyle);
            GUILayout.Space(6);

            var balances = CurrencyManager.CurrentBalances;

            GUILayout.BeginHorizontal();

            // Intel Points Box
            GUILayout.BeginVertical(_boxStyle, GUILayout.Width(180));
            GUILayout.Label("📡 INTEL POINTS", _headerStyle);
            GUILayout.Label($"{balances.IntelPoints}", _highlightStyle);
            GUILayout.Label("Aufklärung & Spotting", _mutedStyle);
            if (GUILayout.Button("+25 Intel (Test)", _actionButtonStyle))
                CurrencyManager.AddCurrency(CurrencyType.IntelPoints, 25);
            GUILayout.EndVertical();

            // Logistics Tokens Box
            GUILayout.BeginVertical(_boxStyle, GUILayout.Width(180));
            GUILayout.Label("📦 LOGISTICS TOKENS", _headerStyle);
            GUILayout.Label($"{balances.LogisticsTokens}", _highlightStyle);
            GUILayout.Label("Loadouts & Schnellnachschub", _mutedStyle);
            if (GUILayout.Button("+5 Tokens (Test)", _actionButtonStyle))
                CurrencyManager.AddCurrency(CurrencyType.LogisticsTokens, 5);
            GUILayout.EndVertical();

            // Command Favor Box
            GUILayout.BeginVertical(_boxStyle, GUILayout.Width(180));
            GUILayout.Label("⭐ COMMAND FAVOR", _headerStyle);
            GUILayout.Label($"{balances.CommandFavor}", _highlightStyle);
            GUILayout.Label("Experimentelle Shells & Gun", _mutedStyle);
            if (GUILayout.Button("+1 Favor (Test)", _actionButtonStyle))
                CurrencyManager.AddCurrency(CurrencyType.CommandFavor, 1);
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();

            GUILayout.Space(14);
            GUILayout.Label("AKTIVES LOADOUT-PRESET", _headerStyle);
            var activePreset = LoadoutManager.GetActivePreset();
            if (activePreset != null)
            {
                GUILayout.BeginVertical(_boxStyle);
                GUILayout.Label($"Name: {activePreset.Name}", _highlightStyle);
                GUILayout.Label($"Beschreibung: {activePreset.Description}", _textStyle);
                GUILayout.Space(4);
                GUILayout.Label("Enthaltene Munition:", _mutedStyle);
                foreach (var item in activePreset.Items)
                {
                    GUILayout.Label($"  - {item.Quantity}x {item.ShellId} (Standard-Ladung: {item.DefaultPowderCharges}x)", _textStyle);
                }
                GUILayout.EndVertical();
            }
        }

        // ==================== TAB 4: PROGRESSION ====================
        private static void DrawProgressionTab()
        {
            var rank = ProgressionManager.GetCurrentRank();
            var nextRank = ProgressionManager.GetNextRank();
            var data = ProgressionManager.Data;

            GUILayout.Label("OPERATOR PROGRESSION & RÄNGE", _headerStyle);
            GUILayout.Space(4);

            GUILayout.BeginVertical(_boxStyle);
            GUILayout.BeginHorizontal();
            GUILayout.Label($"AKTUELLER RANG: [ LEVEL {rank.Level} ]", _headerStyle);
            GUILayout.FlexibleSpace();
            GUILayout.Label($"{rank.Title.ToUpper()}", _highlightStyle);
            GUILayout.EndHorizontal();
            GUILayout.Label(rank.Description, _textStyle);

            GUILayout.Space(8);

            // XP Progress Bar
            float progress = ProgressionManager.GetProgressToNextRank();
            GUILayout.Label($"Erfahrung: {data.TotalXP} XP " + (nextRank != null ? $"/ {nextRank.RequiredXP} XP (Nächster Rang: {nextRank.Title})" : "(MAXIMALER RANG)"), _textStyle);

            DrawProgressBar(progress);

            GUILayout.Space(6);
            GUILayout.Label("Freigeschaltete Fähigkeiten:", _mutedStyle);
            foreach (var perk in rank.UnlockedPerks)
            {
                GUILayout.Label($"  ✔ {perk}", _textStyle);
            }
            GUILayout.EndVertical();

            GUILayout.Space(10);
            GUILayout.Label("LEBENSZEIT-STATISTIKEN", _headerStyle);
            GUILayout.BeginVertical(_boxStyle);
            GUILayout.Label($"Erfolgreiche Missionen: {data.MissionsCompleted}", _textStyle);
            GUILayout.Label($"Verschossene Shells: {data.ShellsFired}", _textStyle);
            GUILayout.Label($"Direkttreffer: {data.DirectHits} (Quote: {data.AccuracyPercentage:F1}%)", _highlightStyle);
            GUILayout.Label($"Zerstörte Feind-Artillerie: {data.CounterBatteryKills}", _textStyle);
            GUILayout.EndVertical();
        }

        // ==================== TAB 5: CONFIG ====================
        private static void DrawConfigTab()
        {
            GUILayout.Label("MOD-EINSTELLUNGEN", _headerStyle);
            GUILayout.Space(6);

            GUILayout.BeginVertical(_boxStyle);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Overlay-Umschalttaste:", _textStyle, GUILayout.Width(220));
            Config.ToggleKey = GUILayout.TextField(Config.ToggleKey, GUILayout.Width(60));
            GUILayout.EndHorizontal();

            GUILayout.Space(6);
            Config.StartVisible = GUILayout.Toggle(Config.StartVisible, " Overlay beim Spielstart direkt anzeigen", _textStyle);
            Config.AutoAdvisorEnabled = GUILayout.Toggle(Config.AutoAdvisorEnabled, " Automatischer Ammo Advisor aktiv", _textStyle);
            Config.DisableInMultiplayer = GUILayout.Toggle(Config.DisableInMultiplayer, " Strenge Fairness im Co-op (Empfohlen)", _textStyle);
            Config.SoundFeedbackEnabled = GUILayout.Toggle(Config.SoundFeedbackEnabled, " Audio-Feedback bei Advisor-Änderungen", _textStyle);

            GUILayout.Space(10);
            if (GUILayout.Button("Einstellungen speichern", _actionButtonStyle, GUILayout.Height(28)))
            {
                SaveManager.SaveConfig(Config);
            }
            GUILayout.EndVertical();
        }

        // ==================== HELPERS & STYLING ====================
        private static void DrawProgressBar(float progress)
        {
            var rect = GUILayoutUtility.GetRect(GUILayoutUtility.GetLastRect().width, 16);
            GUI.DrawTexture(rect, _texProgressBarBg);
            var fillRect = new Rect(rect.x + 1, rect.y + 1, (rect.width - 2) * Math.Clamp(progress, 0f, 1f), rect.height - 2);
            GUI.DrawTexture(fillRect, _texProgressBarFill);
        }

        private static void DrawHorizontalLine(Color color, float height)
        {
            var rect = GUILayoutUtility.GetRect(GUILayoutUtility.GetLastRect().width, height);
            var oldColor = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = oldColor;
        }

        private static bool CheckToggleKey()
        {
            try
            {
                if (Enum.TryParse<KeyCode>(Config.ToggleKey, true, out var key) && Input.GetKeyDown(key))
                {
                    return true;
                }
            }
            catch { }

            return false;
        }

        private static void EnsureStyles()
        {
            if (_stylesInitialized && _texBg != null)
                return;

            _texBg = MakeColorTexture(new Color(0.08f, 0.07f, 0.05f, 0.96f));
            _texHeader = MakeColorTexture(new Color(0.18f, 0.15f, 0.10f, 1.0f));
            _texButton = MakeColorTexture(new Color(0.16f, 0.14f, 0.11f, 1.0f));
            _texButtonActive = MakeColorTexture(new Color(0.40f, 0.32f, 0.18f, 1.0f));
            _texProgressBarBg = MakeColorTexture(new Color(0.12f, 0.10f, 0.08f, 1.0f));
            _texProgressBarFill = MakeColorTexture(new Color(0.769f, 0.639f, 0.353f, 1.0f));

            var gold = new Color(0.769f, 0.639f, 0.353f, 1f);
            var text = new Color(0.88f, 0.84f, 0.72f, 1f);
            var muted = new Color(0.60f, 0.54f, 0.42f, 1f);
            var green = new Color(0.45f, 0.85f, 0.50f, 1f);
            var red = new Color(0.92f, 0.35f, 0.30f, 1f);

            _headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold
            };
            _headerStyle.normal.textColor = gold;

            _textStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                wordWrap = true
            };
            _textStyle.normal.textColor = text;

            _highlightStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold
            };
            _highlightStyle.normal.textColor = green;

            _mutedStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Italic
            };
            _mutedStyle.normal.textColor = muted;

            _dangerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold
            };
            _dangerStyle.normal.textColor = red;

            _boxStyle = new GUIStyle(GUI.skin.box);
            _boxStyle.normal.background = _texHeader;
            _boxStyle.padding = new RectOffset(8, 8, 8, 8);

            _tabButtonStyle = new GUIStyle(GUI.skin.button);
            _tabButtonStyle.normal.background = _texButton;
            _tabButtonStyle.normal.textColor = text;
            _tabButtonStyle.fontStyle = FontStyle.Bold;
            _tabButtonStyle.fontSize = 11;

            _activeTabButtonStyle = new GUIStyle(GUI.skin.button);
            _activeTabButtonStyle.normal.background = _texButtonActive;
            _activeTabButtonStyle.normal.textColor = gold;
            _activeTabButtonStyle.fontStyle = FontStyle.Bold;
            _activeTabButtonStyle.fontSize = 11;

            _actionButtonStyle = new GUIStyle(GUI.skin.button);
            _actionButtonStyle.normal.background = _texButton;
            _actionButtonStyle.normal.textColor = text;
            _actionButtonStyle.fontSize = 11;

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
