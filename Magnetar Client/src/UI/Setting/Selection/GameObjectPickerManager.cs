using Magnetar_Client.Api;
using Magnetar_Client.Core;
using Magnetar_Client.UI;
using Magnetar_Client.UI.Themes;
using Magnetar_Client.UI.WindowDrawing;
using static Magnetar_Client.Utils.Translator;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using HarmonyLib;

#if MELONLOADER || RELEASE_MELON
using Il2Cpp;
#endif

namespace Magnetar_Client.UI.Setting;

/// <summary>
/// Global manager responsible for lawn object picking, screen-space highlight rendering, and input interaction.
/// </summary>
public static class GameObjectPickerManager
{
    private static bool _initialized;
    private static bool _wasShowGui;
    private static int _pickStartFrame;
    private static float _pickStartTime;

    /// <summary>
    /// Gets the frame index when picking mode completed or cancelled, used to suppress click bleed-through.
    /// </summary>
    public static int PickEndFrame { get; private set; } = -1;

    private static readonly List<WeakReference<GameObjectSettingBase>> _registeredSettings = new();

    private static GUIStyle _swatchStyle;
    private static GUIStyle SwatchStyle
    {
        get
        {
            if (_swatchStyle == null || _swatchStyle.normal.background == null)
            {
                _swatchStyle = new GUIStyle();
                _swatchStyle.normal.background = ThemeData.GetTex(Color.white);
            }
            return _swatchStyle;
        }
    }

    private static GUIStyle _floatingLabelStyle;
    private static GUIStyle FloatingLabelStyle
    {
        get
        {
            if (_floatingLabelStyle == null)
            {
                _floatingLabelStyle = new GUIStyle
                {
                    alignment = TextAnchor.MiddleCenter,
                    wordWrap = false,
                    clipping = TextClipping.Clip,
                    richText = false
                };
            }
            if (ThemeManager.TextStyle != null)
            {
                _floatingLabelStyle.font = ThemeManager.TextStyle.font;
                _floatingLabelStyle.fontSize = Mathf.Max(12, ThemeManager.TextStyle.fontSize);
            }
            _floatingLabelStyle.normal.textColor = Color.white;
            return _floatingLabelStyle;
        }
    }

    private static GUIStyle _bannerLabelStyle;
    private static GUIStyle BannerLabelStyle
    {
        get
        {
            if (_bannerLabelStyle == null)
            {
                _bannerLabelStyle = new GUIStyle
                {
                    alignment = TextAnchor.MiddleCenter,
                    wordWrap = false,
                    clipping = TextClipping.Clip,
                    richText = false
                };
            }
            if (ThemeManager.TextStyle != null)
            {
                _bannerLabelStyle.font = ThemeManager.TextStyle.font;
                _bannerLabelStyle.fontSize = Mathf.Max(13, ThemeManager.TextStyle.fontSize + 1);
            }
            _bannerLabelStyle.normal.textColor = Color.white;
            return _bannerLabelStyle;
        }
    }

    /// <summary>
    /// Gets whether an interactive lawn object picking session is currently in progress.
    /// </summary>
    public static bool IsPicking => ActiveSetting != null;

    /// <summary>
    /// Gets the setting currently being populated by the lawn picker.
    /// </summary>
    public static GameObjectSettingBase ActiveSetting { get; private set; }

    /// <summary>
    /// Gets the GameObject currently hovered under the mouse cursor that satisfies the target criteria.
    /// </summary>
    public static GameObject HoveredObject { get; private set; }

    /// <summary>
    /// Registers a lawn object setting to receive board lifecycle notifications (e.g. auto-clearing dead references).
    /// </summary>
    /// <param name="setting">The setting instance to register.</param>
    public static void RegisterSetting(GameObjectSettingBase setting)
    {
        if (setting == null) return;
        lock (_registeredSettings)
        {
            // Prune dead references while searching
            for (int i = _registeredSettings.Count - 1; i >= 0; i--)
            {
                if (!_registeredSettings[i].TryGetTarget(out var target) || target == null)
                {
                    _registeredSettings.RemoveAt(i);
                }
                else if (ReferenceEquals(target, setting))
                {
                    return; // Already registered
                }
            }
            _registeredSettings.Add(new WeakReference<GameObjectSettingBase>(setting));
        }
    }

    private static EventSystem _suppressedEventSystem;

    /// <summary>
    /// Rescues and re-enables any Unity EventSystem in the active scene if it was disabled.
    /// </summary>
    public static void RescueEventSystem()
    {
        try
        {
            if (_suppressedEventSystem != null)
            {
                if (!_suppressedEventSystem.enabled)
                {
                    _suppressedEventSystem.enabled = true;
                }
                _suppressedEventSystem = null;
            }

            var all = Resources.FindObjectsOfTypeAll<EventSystem>();
            if (all != null)
            {
                for (int i = 0; i < all.Length; i++)
                {
                    var es = all[i];
                    if (es != null && !es.enabled && es.gameObject != null && es.gameObject.scene.IsValid())
                    {
                        es.enabled = true;
                    }
                }
            }
        }
        catch
        {
        }
    }

    /// <summary>
    /// Initializes event hooks for updates, overlay rendering, and board lifecycle events.
    /// </summary>
    public static void Init()
    {
        if (_initialized) return;
        _initialized = true;

        RescueEventSystem();

        Actions.OnUpdate += Update;
        Actions.OnLateGUI += DrawOverlay;
        GameData.Actions.OnBoardDestroyed += HandleBoardDestroyed;
        GameData.Actions.OnBoardDied += HandleBoardDestroyed;
    }

    /// <summary>
    /// Initiates picking mode for the specified lawn object setting.
    /// </summary>
    /// <param name="setting">The setting to populate.</param>
    public static void StartPicking(GameObjectSettingBase setting)
    {
        if (setting == null) return;
        Init();

        ActiveSetting = setting;
        HoveredObject = null;
        _wasShowGui = ClientConfig.showgui;
        _pickStartFrame = Time.frameCount;
        _pickStartTime = Time.unscaledTime;

        DrawSetting.ResetInputBind();
        ClientConfig.showgui = false;

        // Apply LockUI patch to disable canvas during entity selection
        LockUI.BlockCanvas = true;

        try
        {
            _suppressedEventSystem = EventSystem.current;
            if (_suppressedEventSystem == null)
            {
                _suppressedEventSystem = UnityEngine.Object.FindObjectOfType<EventSystem>();
            }
            if (_suppressedEventSystem != null)
            {
                _suppressedEventSystem.enabled = false;
            }
        }
        catch
        {
        }
    }

    /// <summary>
    /// Confirms and commits the selection, restoring previous GUI state.
    /// </summary>
    public static void FinishPicking()
    {
        ActiveSetting = null;
        HoveredObject = null;
        PickEndFrame = Time.frameCount;
        ClientConfig.showgui = true;
        Input.ResetInputAxes();
    }

    /// <summary>
    /// Cancels picking mode without altering the setting's previous value, restoring previous GUI state.
    /// </summary>
    public static void CancelPicking()
    {
        ActiveSetting = null;
        HoveredObject = null;
        PickEndFrame = Time.frameCount;
        ClientConfig.showgui = _wasShowGui;
        Input.ResetInputAxes();
        LockUI.BlockCanvas = false;
        RescueEventSystem();
    }

    private static void HandleBoardDestroyed()
    {
        if (IsPicking)
        {
            CancelPicking();
        }
        LockUI.BlockCanvas = false;
        RescueEventSystem();

        lock (_registeredSettings)
        {
            for (int i = _registeredSettings.Count - 1; i >= 0; i--)
            {
                if (_registeredSettings[i].TryGetTarget(out var setting) && setting != null)
                {
                    setting.ClearSelection();
                }
                else
                {
                    _registeredSettings.RemoveAt(i);
                }
            }
        }
    }

    /// <summary>
    /// Updates raycasting, mouse hover detection, and picking input every frame.
    /// </summary>
    public static void Update()
    {
        if (!IsPicking)
        {
            // Restore EventSystem and Canvas raycasts after commit frame passes
            if (Time.frameCount > PickEndFrame)
            {
                if (LockUI.BlockCanvas)
                {
                    LockUI.BlockCanvas = false;
                }

                if (EventSystem.current == null || (_suppressedEventSystem != null && !_suppressedEventSystem.enabled))
                {
                    RescueEventSystem();
                }
            }
            HoveredObject = null;
            return;
        }

        // Cancel on Escape or Right-click
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
        {
            CancelPicking();
            return;
        }

        // Prevent click bleed-through from the frame or moment the Pick button was clicked
        if (Time.frameCount <= _pickStartFrame || Time.unscaledTime - _pickStartTime < 0.15f)
        {
            return;
        }

        // Determine mouse coordinates in 2D world space
        Vector2 worldPos = Camera.main != null
            ? (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition)
            : (Mouse.Instance != null ? Mouse.Instance.MousePosition : Vector2.zero);

        GameObject match = null;

        // 1. Direct query of plants under mouse via PvZ Fusion's Mouse.GetPlantsOnMouse
        if (Mouse.Instance != null && ActiveSetting.TargetType != null &&
            (typeof(Plant).IsAssignableFrom(ActiveSetting.TargetType) || ActiveSetting.TargetType.Name.Contains("Plant") || ActiveSetting.TargetType.Name.Contains("Pumpiner")))
        {
            try
            {
                var plants = Mouse.Instance.GetPlantsOnMouse();
                if (plants != null && plants.Count > 0)
                {
                    for (int pIdx = 0; pIdx < plants.Count; pIdx++)
                    {
                        var p = plants[pIdx];
                        if (p != null && p.gameObject != null && ActiveSetting.IsMatch(p.gameObject))
                        {
                            match = p.gameObject;
                            break;
                        }
                    }
                }
            }
            catch
            {
            }
        }

        // 2. Physics2D OverlapPoint
        if (match == null)
        {
            var cols = Physics2D.OverlapPointAll(worldPos);
            if (cols != null && cols.Length > 0)
            {
                for (int i = 0; i < cols.Length; i++)
                {
                    var c = cols[i];
                    if (c == null) continue;
                    var go = c.gameObject;
                    if (go != null && ActiveSetting.IsMatch(go))
                    {
                        match = go;
                        break;
                    }
                }
            }
        }

        // 3. Physics2D RaycastAll
        if (match == null)
        {
            var hits = Physics2D.RaycastAll(worldPos, Vector2.zero);
            if (hits != null && hits.Length > 0)
            {
                for (int i = 0; i < hits.Length; i++)
                {
                    var col = hits[i].collider;
                    if (col == null) continue;
                    var go = col.gameObject;
                    if (go != null && ActiveSetting.IsMatch(go))
                    {
                        match = go;
                        break;
                    }
                }
            }
        }

        // 4. Physics2D OverlapCircle (forgiving 0.4f radius for sprite edges/stems)
        if (match == null)
        {
            var circleHits = Physics2D.OverlapCircleAll(worldPos, 0.4f);
            if (circleHits != null && circleHits.Length > 0)
            {
                for (int i = 0; i < circleHits.Length; i++)
                {
                    var c = circleHits[i];
                    if (c == null) continue;
                    var go = c.gameObject;
                    if (go != null && ActiveSetting.IsMatch(go))
                    {
                        match = go;
                        break;
                    }
                }
            }
        }

        // 5. Grid tile lookup via Lawnf.Get1x1Plants when targeting plants
        if (match == null && Mouse.Instance != null && ActiveSetting.TargetType != null &&
            (typeof(Plant).IsAssignableFrom(ActiveSetting.TargetType) || ActiveSetting.TargetType.Name.Contains("Plant") || ActiveSetting.TargetType.Name.Contains("Pumpiner")))
        {
            try
            {
                int col = Mouse.Instance.theMouseColumn;
                int row = Mouse.Instance.theMouseRow;
                var gridPlants = Lawnf.Get1x1Plants(col, row);
                if (gridPlants != null)
                {
                    foreach (var p in gridPlants)
                    {
                        if (p != null && p.gameObject != null && ActiveSetting.IsMatch(p.gameObject))
                        {
                            match = p.gameObject;
                            break;
                        }
                    }
                }
            }
            catch
            {
            }
        }

        HoveredObject = match;

        // Select on Left-click if hovering a valid match
        if (Input.GetMouseButtonDown(0))
        {
            if (HoveredObject != null)
            {
                var target = HoveredObject;
                var setting = ActiveSetting;
                setting.SetSelectedRaw(target);
                FinishPicking();
            }
        }
    }

    /// <summary>
    /// Renders screen-space highlights, bounding boxes, and target info overlays during picking mode.
    /// </summary>
    public static void DrawOverlay()
    {
        if (!IsPicking || ActiveSetting == null) return;

        Matrix4x4 prevMatrix = GUI.matrix;
        Color prevColor = GUI.color;
        GUI.matrix = Matrix4x4.identity;

        try
        {
            Color accent = ThemeData.AccentColor;
            if (accent.a < 0.1f) accent = new Color(0.2f, 0.8f, 0.2f, 1f);

            DrawTopBanner(accent);

            if (HoveredObject != null)
            {
                DrawObjectHighlight(HoveredObject, accent);
            }
        }
        catch
        {
            // Suppress render exceptions during scene transitions
        }
        finally
        {
            GUI.color = prevColor;
            GUI.matrix = prevMatrix;
        }
    }

    private static void DrawTopBanner(Color accent)
    {
        float bannerW = GUIManager.S(640f);
        float bannerH = GUIManager.S(38f);
        float bannerX = (Screen.width - bannerW) / 2f;
        float bannerY = GUIManager.S(20f);
        Rect bannerRect = new(bannerX, bannerY, bannerW, bannerH);

        DrawRectFill(bannerRect, new Color(0.08f, 0.08f, 0.12f, 0.94f));
        DrawRectBorder(bannerRect, accent, 2f);

        string typeName = ActiveSetting.TargetType != null ? ActiveSetting.TargetType.Name : "GameObject";
        if (typeName.Equals("Pumpiner", StringComparison.OrdinalIgnoreCase))
        {
            typeName = "Pumpkin Chest";
        }
        else if (typeName.Equals("EndPumpiner", StringComparison.OrdinalIgnoreCase))
        {
            typeName = "Ender Pumpkin Chest";
        }

        string bannerText = $"{Translate("Picking")}: {ActiveSetting.DisplayName} ({typeName})  |  {Translate("Left-Click: Select")}  |  {Translate("Right-Click / ESC: Cancel")}";
        GUI.Label(bannerRect, bannerText, BannerLabelStyle);
    }

    private static void DrawObjectHighlight(GameObject go, Color accent)
    {
        Camera cam = Camera.main;
        if (cam == null || go == null) return;

        Bounds worldBounds = default;
        bool hasBounds = false;

        var col = go.GetComponent<Collider2D>() ?? go.GetComponentInChildren<Collider2D>();
        if (col != null)
        {
            worldBounds = col.bounds;
            hasBounds = true;
        }
        else
        {
            var sr = go.GetComponent<SpriteRenderer>() ?? go.GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
            {
                worldBounds = sr.bounds;
                hasBounds = true;
            }
            else
            {
                var rend = go.GetComponent<Renderer>() ?? go.GetComponentInChildren<Renderer>();
                if (rend != null)
                {
                    worldBounds = rend.bounds;
                    hasBounds = true;
                }
            }
        }

        Rect screenRect;
        if (hasBounds)
        {
            Vector3 cMin = cam.WorldToScreenPoint(worldBounds.min);
            Vector3 cMax = cam.WorldToScreenPoint(worldBounds.max);

            float x1 = Mathf.Min(cMin.x, cMax.x);
            float x2 = Mathf.Max(cMin.x, cMax.x);
            float y1 = Screen.height - Mathf.Max(cMin.y, cMax.y);
            float y2 = Screen.height - Mathf.Min(cMin.y, cMax.y);

            float pad = 4f;
            screenRect = new Rect(x1 - pad, y1 - pad, Mathf.Max(20f, x2 - x1 + pad * 2f), Mathf.Max(20f, y2 - y1 + pad * 2f));
        }
        else
        {
            Vector3 screenPoint = cam.WorldToScreenPoint(go.transform.position);
            float cx = screenPoint.x;
            float cy = Screen.height - screenPoint.y;
            screenRect = new Rect(cx - 30f, cy - 30f, 60f, 60f);
        }

        // Translucent highlight fill
        DrawRectFill(screenRect, new Color(accent.r, accent.g, accent.b, 0.25f));

        // Solid accent border
        DrawRectBorder(screenRect, new Color(accent.r, accent.g, accent.b, 0.95f), 2f);

        // Floating pill label with formatted object name and coordinates
        string label = GameObjectSettingBase.FormatObjectName(go);
        float pillW = FloatingLabelStyle.CalcSize(new GUIContent(label)).x + 20f;
        float pillH = GUIManager.S(24f);
        float pillX = screenRect.x + (screenRect.width - pillW) / 2f;
        float pillY = screenRect.y - pillH - 6f;
        if (pillY < GUIManager.S(65f))
        {
            pillY = screenRect.y + screenRect.height + 6f; // Flip below if near banner
        }

        Rect pillRect = new(pillX, pillY, pillW, pillH);
        DrawRectFill(pillRect, new Color(0.1f, 0.1f, 0.14f, 0.92f));
        DrawRectBorder(pillRect, accent, 1.5f);
        GUI.Label(pillRect, label, FloatingLabelStyle);
    }

    private static void DrawRectFill(Rect r, Color c)
    {
        Color prev = GUI.color;
        GUI.color = c;
        GUI.Box(r, "", SwatchStyle);
        GUI.color = prev;
    }

    private static void DrawRectBorder(Rect r, Color c, float thickness = 2f)
    {
        Color prev = GUI.color;
        GUI.color = c;
        GUI.Box(new Rect(r.x, r.y, r.width, thickness), "", SwatchStyle);
        GUI.Box(new Rect(r.x, r.y + r.height - thickness, r.width, thickness), "", SwatchStyle);
        GUI.Box(new Rect(r.x, r.y + thickness, thickness, r.height - thickness * 2f), "", SwatchStyle);
        GUI.Box(new Rect(r.x + r.width - thickness, r.y + thickness, thickness, r.height - thickness * 2f), "", SwatchStyle);
        GUI.color = prev;
    }

    #region Harmony Patches

    /// <summary>
    /// Suppresses all in-game mouse processing, tools, and clicks during lawn picking sessions and the commit frame.
    /// </summary>
    [HarmonyPatch(typeof(Mouse), nameof(Mouse.Update))]
    public static class Mouse_Update_PickerPatch
    {
        [HarmonyPrefix]
        public static bool Prefix()
        {
            if (IsPicking || Time.frameCount <= PickEndFrame)
            {
                return false; // Skip entire game mouse frame
            }
            return true;
        }
    }

    #endregion
}
