using IronXNestCommand.Core;
using IronXNestCommand.Core.Config;
using IronXNestCommand.Core.Paths;
using UnityEngine;
using UnityEngine.InputSystem;

namespace IronXNestCommand.Host.BepInEx.Overlay;

public sealed class CommandOverlay : MonoBehaviour
{
    private const float PanelWidth = 360f;
    private const float PanelHeight = 268f;
    private const float Margin = 16f;

    private static readonly Color PanelBg = new(0.08f, 0.07f, 0.05f, 0.90f);
    private static readonly Color Gold = new(0.769f, 0.639f, 0.353f, 1f);
    private static readonly Color Text = new(0.86f, 0.80f, 0.62f, 1f);
    private static readonly Color Muted = new(0.62f, 0.56f, 0.42f, 1f);

    private bool _visible = true;
    private string _toggleKey = "F8";
    private GUIStyle? _titleStyle;
    private GUIStyle? _bodyStyle;
    private GUIStyle? _mutedStyle;
    private Texture2D? _panelTex;
    private Texture2D? _lineTex;

    public CommandOverlay(IntPtr ptr) : base(ptr)
    {
    }

    private void Awake()
    {
        var config = Plugin.Instance?.ConfigData ?? new ModConfig();
        _visible = config.StartVisible;
        _toggleKey = string.IsNullOrWhiteSpace(config.ToggleKey) ? "F8" : config.ToggleKey;
    }

    private void Update()
    {
        if (WasTogglePressed())
            _visible = !_visible;
    }

    private void OnGUI()
    {
        if (!_visible)
            return;

        EnsureStyles();

        var x = Screen.width - PanelWidth - Margin;
        var y = Margin;
        var rect = new Rect(x, y, PanelWidth, PanelHeight);

        GUI.color = Color.white;
        GUI.DrawTexture(rect, _panelTex);
        GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, 2f), _lineTex);
        GUI.DrawTexture(new Rect(rect.x, rect.yMax - 2f, rect.width, 2f), _lineTex);

        var pad = 14f;
        var inner = new Rect(rect.x + pad, rect.y + 10f, rect.width - pad * 2f, rect.height - 20f);

        GUI.Label(new Rect(inner.x, inner.y, inner.width, 22f), "IRON X NEST COMMAND", _titleStyle);
        GUI.Label(new Rect(inner.x, inner.y + 22f, inner.width, 18f), $"v{ModInfo.Version}  ·  PHASE 0 GERÜST", _mutedStyle);

        var coop = CoopPresence.IsPluginLoaded();
        var body =
            "Status          LOADED\n" +
            $"Co-op Plugin    {(coop ? "ERKANNT" : "nicht geladen")}\n" +
            "Adapter         wartet auf Phase 1\n" +
            "\n" +
            "INVENTAR   ADVISOR   LOADOUTS\n" +
            "RANK       CONFIG\n" +
            "\n" +
            "Assistenz-Overlay. Kein Auto-Feuer.\n" +
            $"Hotkey {_toggleKey} blendet das Panel aus.";

        GUI.Label(new Rect(inner.x, inner.y + 52f, inner.width, 150f), body, _bodyStyle);
        GUI.Label(new Rect(inner.x, inner.yMax - 28f, inner.width, 28f), ModPaths.DataRoot, _mutedStyle);
    }

    private bool WasTogglePressed()
    {
        try
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && TryGetInputKey(_toggleKey, out var key) && keyboard[key].wasPressedThisFrame)
                return true;
        }
        catch
        {
            // Input System may be unavailable during early load.
        }

        try
        {
            if (TryGetLegacyKey(_toggleKey, out var legacy) && Input.GetKeyDown(legacy))
                return true;
        }
        catch
        {
            // Legacy input may be disabled.
        }

        return false;
    }

    private static bool TryGetInputKey(string name, out Key key)
    {
        if (Enum.TryParse(name, ignoreCase: true, out key) && key != Key.None)
            return true;

        key = Key.F8;
        return string.Equals(name, "F8", StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryGetLegacyKey(string name, out KeyCode key)
    {
        if (Enum.TryParse(name, ignoreCase: true, out key))
            return true;

        key = KeyCode.F8;
        return string.Equals(name, "F8", StringComparison.OrdinalIgnoreCase);
    }

    private void EnsureStyles()
    {
        if (_titleStyle != null)
            return;

        _panelTex = MakeColorTexture(PanelBg);
        _lineTex = MakeColorTexture(Gold);

        _titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 15,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.UpperLeft
        };
        _titleStyle.normal.textColor = Gold;

        _bodyStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 12,
            wordWrap = true,
            alignment = TextAnchor.UpperLeft
        };
        _bodyStyle.normal.textColor = Text;

        _mutedStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 10,
            wordWrap = true,
            alignment = TextAnchor.UpperLeft
        };
        _mutedStyle.normal.textColor = Muted;
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

    private void OnDestroy()
    {
        if (_panelTex != null)
            Destroy(_panelTex);
        if (_lineTex != null)
            Destroy(_lineTex);
    }
}
