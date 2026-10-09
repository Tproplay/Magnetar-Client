using Magnetar_Client.Api;
using Magnetar_Client.UI;
using Magnetar_Client.UI.Themes;
using Magnetar_Client.Utils;
using UnityEngine;

namespace Magnetar_Client.Core;

public static class MobileMenuUI
{
    private static Rect _btnRect = new(40f, 200f, 120f, 120f);
    private static Vector2 _dragStartMousePos;
    private static Vector2 _dragStartBtnPos;
    private static bool _isPointerDown;
    private static bool _isDragging;
    private const float DragThreshold = 10f;
    private static bool _hasClampedInitialPos;

    private static Texture2D _logoTex;
    private static Texture2D _circleTex;
    private static GUIStyle _circleBtnStyle;
    private static GUIStyle _closeBtnStyle;
    private static bool _attemptedLogoLoad;

    // --- Dormant / Double-Click System ---
    private static bool _isDormant;
    private static float _lastInteractionTime;
    private static float _lastClickTime;
    private const float DoubleClickInterval = 0.35f;
    private const float DormantDarkenFactor = 0.40f;

    public static void Init()
    {
        ServiceRegistry.Register(new MobileMenuUIService());
    }

    public static void ResetIdleTimer()
    {
        _lastInteractionTime = Time.realtimeSinceStartup;
        _isDormant = false;
    }

    public static Rect GetVisibleScreenBounds()
    {
        Matrix4x4 inv = GUI.matrix.inverse;
        Vector3 minScreen = inv.MultiplyPoint3x4(Vector3.zero);
        Vector3 maxScreen = inv.MultiplyPoint3x4(new Vector3(Screen.width, Screen.height, 0f));

        float left = Mathf.Min(minScreen.x, maxScreen.x);
        float right = Mathf.Max(minScreen.x, maxScreen.x);
        float top = Mathf.Min(minScreen.y, maxScreen.y);
        float bottom = Mathf.Max(minScreen.y, maxScreen.y);

        return new Rect(left, top, right - left, bottom - top);
    }

    private static Texture2D CreateCircularBadgeTexture(Texture2D source, Color ringColor, int ringThickness)
    {
        if (source == null) return null;

        int size = Mathf.Min(source.width, source.height);

        RenderTexture tempRT = RenderTexture.GetTemporary(size, size, 0, RenderTextureFormat.ARGB32);
        RenderTexture prevRT = RenderTexture.active;
        RenderTexture.active = tempRT;

        GL.Clear(false, true, new Color(0, 0, 0, 0));
        Graphics.Blit(source, tempRT);

        Texture2D readable = new(size, size, TextureFormat.RGBA32, false);
        readable.ReadPixels(new Rect(0, 0, size, size), 0, 0);
        readable.Apply();

        RenderTexture.active = prevRT;
        RenderTexture.ReleaseTemporary(tempRT);

        Texture2D circularTex = new(size, size, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            hideFlags = HideFlags.DontSave
        };

        Color[] srcPixels = readable.GetPixels();
        Color[] dstPixels = new Color[size * size];

        float center = size / 2f;
        float outerRadius = (size / 2f) - 1.5f;
        float innerRadius = outerRadius - ringThickness;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int idx = y * size + x;
                float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(center, center));

                if (dist > outerRadius)
                {
                    dstPixels[idx] = Color.clear;
                }
                else if (dist >= innerRadius)
                {
                    dstPixels[idx] = ringColor;
                }
                else
                {
                    dstPixels[idx] = srcPixels[idx];
                }
            }
        }

        circularTex.SetPixels(dstPixels);
        circularTex.Apply();

        UnityEngine.Object.Destroy(readable);
        return circularTex;
    }

    private static void EnsureResources()
    {
        float currentSize = Config.S(120f);
        if (Mathf.Abs(_btnRect.width - currentSize) > 0.5f)
        {
            _btnRect.width = currentSize;
            _btnRect.height = currentSize;
        }

        // Direct load logo via ResourceManager with no AssetBundle wrapper
        if (!_attemptedLogoLoad)
        {
            _attemptedLogoLoad = true;
            Texture2D rawLogo = ResourceManager.LoadTexture("Magnetar_logo.png"); ;
            if (rawLogo != null)
            {
                int borderThickness = Mathf.Max(4, Mathf.RoundToInt(rawLogo.width * 0.045f));
                _logoTex = CreateCircularBadgeTexture(rawLogo, ThemeManager.AccentColor, borderThickness);
            }
            else
            {
                Magnetar_Logger.GUILogger.Warning("Logo 'Magnetar_logo' not found. Falling back to procedural badge.");
            }
        }

        if (_circleTex == null)
        {
            _circleTex = CreateCircleTexture(128, ThemeManager.BackgroundColor, ThemeManager.AccentColor, 6);
        }

        Texture2D activeBadgeTex = _logoTex != null ? _logoTex : _circleTex;

        if (_circleBtnStyle == null)
        {
            _circleBtnStyle = new GUIStyle
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                border = new RectOffset(),
                padding = new RectOffset(),
                margin = new RectOffset(),
                overflow = new RectOffset()
            };
            _circleBtnStyle.normal.textColor = ThemeManager.AccentColor;
        }
        _circleBtnStyle.normal.background = activeBadgeTex;
        _circleBtnStyle.hover.background = activeBadgeTex;
        _circleBtnStyle.active.background = activeBadgeTex;
        _circleBtnStyle.fontSize = Mathf.RoundToInt(Config.S(24f));

        _closeBtnStyle ??= new GUIStyle
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                border = new RectOffset(),
                padding = new RectOffset(),
                margin = new RectOffset(),
                overflow = new RectOffset()
            };

        if (ThemeManager.CategoryModuleOnStyle != null)
        {
            _closeBtnStyle.normal.background = ThemeManager.CategoryModuleOnStyle.normal.background;
            _closeBtnStyle.normal.textColor = ThemeManager.CategoryModuleOnStyle.normal.textColor;
            _closeBtnStyle.hover.background = ThemeManager.CategoryModuleOnStyle.hover.background;
            _closeBtnStyle.hover.textColor = ThemeManager.CategoryModuleOnStyle.hover.textColor;
            _closeBtnStyle.active.background = ThemeManager.CategoryModuleOnStyle.active.background;
            _closeBtnStyle.active.textColor = ThemeManager.CategoryModuleOnStyle.active.textColor;
        }
        _closeBtnStyle.fontSize = Mathf.RoundToInt(Config.S(20f));

        if (!_hasClampedInitialPos)
        {
            Rect visible = GetVisibleScreenBounds();
            _btnRect.x = Mathf.Clamp(_btnRect.x, visible.xMin, visible.xMax - _btnRect.width);
            _btnRect.y = Mathf.Clamp(_btnRect.y, visible.yMin, visible.yMax - _btnRect.height);
            _hasClampedInitialPos = true;
            _lastInteractionTime = Time.realtimeSinceStartup;
        }
    }

    public static void Render()
    {
        EnsureResources();
        Event e = Event.current;
        if (e == null) return;

        if (!Config.showgui && !HUDManager.forceShow)
        {
            if (Config.ShowFloatingIcon)
            {
                DrawFloatingCircle(e);
            }
        }
        else if (Config.showgui && !HUDManager.forceShow && Config.ShowMobileButtons)
        {
            DrawCloseButton(e);
        }
    }

    private static void DrawFloatingCircle(Event e)
    {
        Vector2 mousePos = e.mousePosition;
        Rect visible = GetVisibleScreenBounds();
        float now = Time.realtimeSinceStartup;

        if (!_isPointerDown && !_isDragging)
        {
            if (now - _lastInteractionTime > Config.FloatingIconIdleTimeout)
            {
                _isDormant = true;
            }
        }

        if (e.type == EventType.MouseDown && e.button == 0 && _btnRect.Contains(mousePos))
        {
            if (_isDormant)
            {
                if (now - _lastClickTime <= DoubleClickInterval)
                {
                    ResetIdleTimer();
                    e.Use();
                    return;
                }
                _lastClickTime = now;
                e.Use();
                return;
            }

            _isPointerDown = true;
            _isDragging = false;
            _dragStartMousePos = mousePos;
            _dragStartBtnPos = new Vector2(_btnRect.x, _btnRect.y);
            _lastInteractionTime = now;
            e.Use();
        }

        if (_isPointerDown && (e.type == EventType.MouseDrag || e.type == EventType.MouseMove))
        {
            float dist = Vector2.Distance(mousePos, _dragStartMousePos);
            if (dist > DragThreshold)
            {
                _isDragging = true;
            }

            if (_isDragging)
            {
                Vector2 delta = mousePos - _dragStartMousePos;
                _btnRect.x = Mathf.Clamp(_dragStartBtnPos.x + delta.x, visible.xMin, visible.xMax - _btnRect.width);
                _btnRect.y = Mathf.Clamp(_dragStartBtnPos.y + delta.y, visible.yMin, visible.yMax - _btnRect.height);
                _lastInteractionTime = now;
                e.Use();
            }
        }

        if (_isPointerDown && (e.type == EventType.MouseUp || e.rawType == EventType.MouseUp) && e.button == 0)
        {
            _isPointerDown = false;
            _lastInteractionTime = now;

            if (!_isDragging)
            {
                Config.showgui = true;
            }
            _isDragging = false;
            e.Use();
        }

        if (e.type == EventType.Repaint)
        {
            Color prevColor = GUI.color;

            float baseOpacity = Mathf.Clamp01(Config.FloatingIconOpacity);
            float renderAlpha = _isDormant ? baseOpacity * 0.70f : baseOpacity;
            float shadeMultiplier = _isDormant ? DormantDarkenFactor : 1.0f;

            GUI.color = new Color(shadeMultiplier, shadeMultiplier, shadeMultiplier, renderAlpha);

            string badgeLabel = _logoTex != null ? "" : "M";
            GUI.Box(_btnRect, badgeLabel, _circleBtnStyle);

            GUI.color = prevColor;
        }
    }

    private static void DrawCloseButton(Event e)
    {
        Rect visibleScreen = GetVisibleScreenBounds();
        float btnW = Config.S(44f);
        float btnH = Config.S(34f);
        Rect closeRect = new(visibleScreen.xMax - btnW - Config.S(16f), visibleScreen.yMin + Config.S(12f), btnW, btnH);

        bool isHover = closeRect.Contains(e.mousePosition);
        if (isHover)
        {
            GUI.backgroundColor = ThemeManager.AccentColor;
        }

        GUI.Box(closeRect, "✕", _closeBtnStyle);
        GUI.backgroundColor = Color.white;

        if (e.type == EventType.MouseDown && e.button == 0 && isHover)
        {
            Config.showgui = false;
            e.Use();
        }
    }

    private static Texture2D CreateCircleTexture(int size, Color bodyColor, Color ringColor, int ringThickness)
    {
        Texture2D tex = new(size, size, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            hideFlags = HideFlags.DontSave
        };

        float center = size / 2f;
        float radius = (size / 2f) - 2f;
        float innerRadius = radius - ringThickness;

        Color[] colors = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(center, center));
                int index = y * size + x;

                if (dist > radius)
                    colors[index] = Color.clear;
                else if (dist >= innerRadius)
                    colors[index] = ringColor;
                else
                    colors[index] = bodyColor;
            }
        }

        tex.SetPixels(colors);
        tex.Apply();
        return tex;
    }

    private class MobileMenuUIService : IRenderable
    {
        public string Name => "MobileMenuUI";
        public int Priority => ServicePriority.UI;

        public void OnGUI()
        {
            MobileMenuUI.Render();
        }
    }
}