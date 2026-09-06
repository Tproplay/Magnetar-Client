using Magnetar_Client.Core;
using Magnetar_Client.UI.Themes;
using UnityEngine;

namespace Magnetar_Client.Core
{
    public static class MobileMenuUI
    {
        // Floating icon state
        private static Rect _btnRect = new Rect(40f, 200f, 68f, 68f);
        private static Vector2 _dragStartMousePos;
        private static Vector2 _dragStartBtnPos;
        private static bool _isPointerDown = false;
        private static bool _isDragging = false;
        private const float DragThreshold = 10f;

        // Textures & Styles
        private static Texture2D _circleTex;
        private static GUIStyle _circleBtnStyle;
        private static GUIStyle _closeBtnStyle;

        private static void EnsureResources()
        {
            float currentSize = Config.S(68f);
            if (Mathf.Abs(_btnRect.width - currentSize) > 0.5f)
            {
                _btnRect.width = currentSize;
                _btnRect.height = currentSize;
            }

            if (_circleTex == null)
            {
                _circleTex = CreateCircleTexture(128, Magnetar_Default.BackgroundColor, Magnetar_Default.AccentColor, 6);
            }

            if (_circleBtnStyle == null)
            {
                _circleBtnStyle = new GUIStyle();
                _circleBtnStyle.alignment = TextAnchor.MiddleCenter;
                _circleBtnStyle.fontStyle = FontStyle.Bold;
                _circleBtnStyle.normal.textColor = Magnetar_Default.AccentColor;
                _circleBtnStyle.normal.background = _circleTex;

                _circleBtnStyle.padding = new RectOffset();
                _circleBtnStyle.padding.left = 0;
                _circleBtnStyle.padding.right = 0;
                _circleBtnStyle.padding.top = 0;
                _circleBtnStyle.padding.bottom = 0;

                _circleBtnStyle.margin = new RectOffset();
                _circleBtnStyle.margin.left = 0;
                _circleBtnStyle.margin.right = 0;
                _circleBtnStyle.margin.top = 0;
                _circleBtnStyle.margin.bottom = 0;
            }
            _circleBtnStyle.fontSize = Mathf.RoundToInt(Config.S(26f));

            if (_closeBtnStyle == null)
            {
                _closeBtnStyle = new GUIStyle();
                _closeBtnStyle.alignment = TextAnchor.MiddleCenter;
                _closeBtnStyle.fontStyle = FontStyle.Bold;
                _closeBtnStyle.normal.textColor = Color.white;
                _closeBtnStyle.normal.background = Magnetar_Default.ModuleOn != null
                    ? Magnetar_Default.ModuleOn.normal.background
                    : Texture2D.whiteTexture;

                _closeBtnStyle.padding = new RectOffset();
                _closeBtnStyle.padding.left = 0;
                _closeBtnStyle.padding.right = 0;
                _closeBtnStyle.padding.top = 0;
                _closeBtnStyle.padding.bottom = 0;

                _closeBtnStyle.margin = new RectOffset();
                _closeBtnStyle.margin.left = 0;
                _closeBtnStyle.margin.right = 0;
                _closeBtnStyle.margin.top = 0;
                _closeBtnStyle.margin.bottom = 0;
            }
            _closeBtnStyle.fontSize = Mathf.RoundToInt(Config.S(20f));
        }

        public static void Render()
        {
#if ANDROID
            EnsureResources();
            Event e = Event.current;
            if (e == null) return;

            if (!Config.showgui && !HUDManager.forceShow)
            {
                DrawFloatingCircle(e);
            }
            else
            {
                DrawCloseButton(e);
            }
#endif
        }

        private static void DrawFloatingCircle(Event e)
        {
            Vector2 mousePos = e.mousePosition;

            // 1. Touch Down
            if (e.type == EventType.MouseDown && e.button == 0)
            {
                if (_btnRect.Contains(mousePos))
                {
                    _isPointerDown = true;
                    _isDragging = false;
                    _dragStartMousePos = mousePos;
                    _dragStartBtnPos = new Vector2(_btnRect.x, _btnRect.y);
                    e.Use();
                }
            }

            // 2. Touch Drag
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
                    _btnRect.x = Mathf.Clamp(_dragStartBtnPos.x + delta.x, 0f, Config.WindowWidth - _btnRect.width);
                    _btnRect.y = Mathf.Clamp(_dragStartBtnPos.y + delta.y, 0f, Config.WindowHeight - _btnRect.height);
                    e.Use();
                }
            }

            // 3. Touch Up (Tap vs Drag decision)
            if (_isPointerDown && (e.type == EventType.MouseUp || e.rawType == EventType.MouseUp) && e.button == 0)
            {
                _isPointerDown = false;
                if (!_isDragging)
                {
                    Config.showgui = true;
                }
                _isDragging = false;
                e.Use();
            }

            // 4. Repaint Floating Circle via GUI.Box (bypasses stripped DrawTexture)
            if (e.type == EventType.Repaint)
            {
                GUI.Box(_btnRect, "M", _circleBtnStyle);
            }
        }

        private static void DrawCloseButton(Event e)
        {
            float btnW = Config.S(46f);
            float btnH = Config.S(38f);
            Rect closeRect = new Rect(Config.WindowWidth - btnW - Config.S(16f), Config.S(10f), btnW, btnH);

            bool isHover = closeRect.Contains(e.mousePosition);
            if (isHover)
            {
                GUI.backgroundColor = Magnetar_Default.AccentColor;
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
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;

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
                    {
                        colors[index] = Color.clear;
                    }
                    else if (dist >= innerRadius)
                    {
                        colors[index] = ringColor;
                    }
                    else
                    {
                        colors[index] = bodyColor;
                    }
                }
            }

            tex.SetPixels(colors);
            tex.Apply();
            return tex;
        }
    }
}