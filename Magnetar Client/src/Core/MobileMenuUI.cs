using Il2CppInterop.Runtime;
using Il2CppSystem.IO;
using Magnetar_Client.Core;
using Magnetar_Client.UI.Themes;
using Magnetar_Client.Utils;
using System;
using System.IO;
using UnityEngine;

namespace Magnetar_Client.Core
{
    public static class MobileMenuUI
    {
        private static Rect _btnRect = new Rect(40f, 200f, 68f, 68f);
        private static Vector2 _dragStartMousePos;
        private static Vector2 _dragStartBtnPos;
        private static bool _isPointerDown = false;
        private static bool _isDragging = false;
        private const float DragThreshold = 10f;
        private static bool _hasClampedInitialPos = false;

        private static Texture2D _logoTex;
        private static Texture2D _circleTex;
        private static GUIStyle _circleBtnStyle;
        private static GUIStyle _closeBtnStyle;
        private static bool _attemptedLogoLoad = false;

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

        private static Texture2D LoadLogoTexture()
        {
            // --- 1. Load from the dedicated magnetar_ui AssetBundle ---
            try
            {
                string bundlePath = System.IO.Path.Combine(SaveLoad.ModsDir, "Magnetar Data", "magnetar_ui");
                if (System.IO.File.Exists(bundlePath))
                {
                    AssetBundle uiBundle = AssetBundle.LoadFromFile(bundlePath);
                    if (uiBundle != null)
                    {
                        string[] targetNames = new string[]
                        {
                            "assets/assets/magnetar_logo.png",
                            "magnetar_logo"
                        };

                        foreach (string name in targetNames)
                        {
#if MELONLOADER || RELEASE_MELON
                            Texture2D tex = uiBundle.LoadAsset<Texture2D>(name);
                            if (tex != null)
                            {
                                Magnetar_Logger.DebugLogger.Msg($"[MobileMenuUI] Successfully loaded '{name}' as Texture2D from magnetar_ui!");
                                return tex;
                            }

                            Sprite spr = uiBundle.LoadAsset<Sprite>(name);
                            if (spr != null)
                            {
                                Magnetar_Logger.DebugLogger.Msg($"[MobileMenuUI] Successfully loaded '{name}' as Sprite from magnetar_ui!");
                                return ExtractSpriteTexture(spr);
                            }
#elif BEPINEX || RELEASE_BEPINEX || ANDROID
                            var rawTex = uiBundle.LoadAsset(name, Il2CppType.Of<Texture2D>());
                            if (rawTex != null)
                            {
                                Texture2D tex = rawTex.TryCast<Texture2D>();
                                if (tex != null)
                                {
                                    Magnetar_Logger.DebugLogger.Msg($"[MobileMenuUI] Successfully loaded '{name}' as Texture2D from magnetar_ui!");
                                    return tex;
                                }
                            }

                            var rawSpr = uiBundle.LoadAsset(name, Il2CppType.Of<Sprite>());
                            if (rawSpr != null)
                            {
                                Sprite spr = rawSpr.TryCast<Sprite>();
                                if (spr != null)
                                {
                                    Magnetar_Logger.DebugLogger.Msg($"[MobileMenuUI] Successfully loaded '{name}' as Sprite from magnetar_ui!");
                                    return ExtractSpriteTexture(spr);
                                }
                            }
#endif
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Magnetar_Logger.DebugLogger.Error($"[MobileMenuUI] Error loading from magnetar_ui bundle: {ex.Message}");
            }

            // --- 2. Loose disk fallback (Magnetar Data/Magnetar_logo.png) ---
            string[] candidatePaths = new string[]
            {
                System.IO.Path.Combine(SaveLoad.ModsDir, "Magnetar Data", "Magnetar_logo.png"),
                System.IO.Path.Combine(SaveLoad.ModsDir, "Magnetar_logo.png")
            };

            foreach (string path in candidatePaths)
            {
                if (System.IO.File.Exists(path))
                {
                    try
                    {
                        byte[] rawBytes = System.IO.File.ReadAllBytes(path);
                        Texture2D diskTex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                        if (ImageConversion.LoadImage(diskTex, rawBytes))
                        {
                            Magnetar_Logger.DebugLogger.Msg($"[MobileMenuUI] Successfully loaded logo from disk: {path}");
                            return diskTex;
                        }
                    }
                    catch (Exception ex)
                    {
                        Magnetar_Logger.DebugLogger.Error($"[MobileMenuUI] Failed reading disk logo: {ex.Message}");
                    }
                }
            }

            return null;
        }

        private static Texture2D CreateCircularBadgeTexture(Texture2D source, Color ringColor, int ringThickness)
        {
            if (source == null) return null;

            int size = Mathf.Min(source.width, source.height);

            // Ensure source texture is readable by blitting to an active RenderTexture
            RenderTexture tempRT = RenderTexture.GetTemporary(size, size, 0, RenderTextureFormat.ARGB32);
            RenderTexture prevRT = RenderTexture.active;
            RenderTexture.active = tempRT;

            GL.Clear(false, true, new Color(0, 0, 0, 0));
            Graphics.Blit(source, tempRT);

            Texture2D readable = new Texture2D(size, size, TextureFormat.RGBA32, false);
            readable.ReadPixels(new Rect(0, 0, size, size), 0, 0);
            readable.Apply();

            RenderTexture.active = prevRT;
            RenderTexture.ReleaseTemporary(tempRT);

            Texture2D circularTex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            circularTex.wrapMode = TextureWrapMode.Clamp;
            circularTex.filterMode = FilterMode.Bilinear;

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
                        // Outside circle: completely transparent
                        dstPixels[idx] = Color.clear;
                    }
                    else if (dist >= innerRadius)
                    {
                        // Border ring band
                        dstPixels[idx] = ringColor;
                    }
                    else
                    {
                        // Inside badge: star logo pixel
                        dstPixels[idx] = srcPixels[idx];
                    }
                }
            }

            circularTex.SetPixels(dstPixels);
            circularTex.Apply();

            UnityEngine.Object.Destroy(readable);
            return circularTex;
        }

        private static Texture2D ExtractSpriteTexture(Sprite sprite)
        {
            if (sprite == null || sprite.texture == null) return null;

            Rect r = sprite.textureRect;
            int width = Mathf.RoundToInt(r.width);
            int height = Mathf.RoundToInt(r.height);

            RenderTexture tempRT = RenderTexture.GetTemporary(
                sprite.texture.width,
                sprite.texture.height,
                0,
                RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.Default
            );

            RenderTexture prev = RenderTexture.active;
            RenderTexture.active = tempRT;
            GL.Clear(false, true, new Color(0, 0, 0, 0));
            Graphics.Blit(sprite.texture, tempRT);

            Texture2D copy = new Texture2D(sprite.texture.width, sprite.texture.height, TextureFormat.RGBA32, false);
            copy.ReadPixels(new Rect(0, 0, sprite.texture.width, sprite.texture.height), 0, 0);
            copy.Apply();

            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(tempRT);

            Texture2D cropped = new Texture2D(width, height, TextureFormat.RGBA32, false);
            cropped.SetPixels(copy.GetPixels((int)r.x, (int)r.y, width, height));
            cropped.Apply();

            UnityEngine.Object.Destroy(copy);
            return cropped;
        }

        private static void EnsureResources()
        {
            float currentSize = Config.S(68f);
            if (Mathf.Abs(_btnRect.width - currentSize) > 0.5f)
            {
                _btnRect.width = currentSize;
                _btnRect.height = currentSize;
            }

            // Only attempt loading ONCE to eliminate all frame lag
            if (!_attemptedLogoLoad)
            {
                _attemptedLogoLoad = true;
                Texture2D rawLogo = LoadLogoTexture();
                if (rawLogo != null)
                {
                    // Calculate border thickness proportional to resolution (~4.5% of width)
                    int borderThickness = Mathf.Max(4, Mathf.RoundToInt(rawLogo.width * 0.045f));

                    // Circularize and add accent border
                    _logoTex = CreateCircularBadgeTexture(rawLogo, Magnetar_Default.AccentColor, borderThickness);

                    Magnetar_Logger.DebugLogger.Msg("[MobileMenuUI] Successfully circularized logo with accent border!");
                }
                else
                {
                    Magnetar_Logger.DebugLogger.Warning("[MobileMenuUI] Logo 'Magnetar_logo' not found. Falling back to default badge.");
                }
            }

            if (_circleTex == null)
            {
                _circleTex = CreateCircleTexture(128, Magnetar_Default.BackgroundColor, Magnetar_Default.AccentColor, 6);
            }

            Texture2D activeBadgeTex = _logoTex != null ? _logoTex : _circleTex;

            if (_circleBtnStyle == null)
            {
                _circleBtnStyle = new GUIStyle();
                _circleBtnStyle.alignment = TextAnchor.MiddleCenter;
                _circleBtnStyle.fontStyle = FontStyle.Bold;
                _circleBtnStyle.normal.textColor = Magnetar_Default.AccentColor;

                _circleBtnStyle.border = new RectOffset();
                _circleBtnStyle.padding = new RectOffset();
                _circleBtnStyle.margin = new RectOffset();
                _circleBtnStyle.overflow = new RectOffset();
            }
            _circleBtnStyle.normal.background = activeBadgeTex;
            _circleBtnStyle.hover.background = activeBadgeTex;
            _circleBtnStyle.active.background = activeBadgeTex;
            _circleBtnStyle.fontSize = Mathf.RoundToInt(Config.S(24f));

            if (_closeBtnStyle == null)
            {
                _closeBtnStyle = new GUIStyle();
                _closeBtnStyle.alignment = TextAnchor.MiddleCenter;
                _closeBtnStyle.fontStyle = FontStyle.Bold;

                _closeBtnStyle.border = new RectOffset();
                _closeBtnStyle.padding = new RectOffset();
                _closeBtnStyle.margin = new RectOffset();
                _closeBtnStyle.overflow = new RectOffset();
            }

            if (Magnetar_Default.ModuleOn != null)
            {
                _closeBtnStyle.normal.background = Magnetar_Default.ModuleOn.normal.background;
                _closeBtnStyle.normal.textColor = Magnetar_Default.ModuleOn.normal.textColor;
                _closeBtnStyle.hover.background = Magnetar_Default.ModuleOn.hover.background;
                _closeBtnStyle.hover.textColor = Magnetar_Default.ModuleOn.hover.textColor;
                _closeBtnStyle.active.background = Magnetar_Default.ModuleOn.active.background;
                _closeBtnStyle.active.textColor = Magnetar_Default.ModuleOn.active.textColor;
            }
            _closeBtnStyle.fontSize = Mathf.RoundToInt(Config.S(20f));

            if (!_hasClampedInitialPos)
            {
                Rect visible = GetVisibleScreenBounds();
                _btnRect.x = Mathf.Clamp(_btnRect.x, visible.xMin, visible.xMax - _btnRect.width);
                _btnRect.y = Mathf.Clamp(_btnRect.y, visible.yMin, visible.yMax - _btnRect.height);
                _hasClampedInitialPos = true;
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
            else if (Config.showgui && !HUDManager.forceShow)
            {
                DrawCloseButton(e);
            }
        }

        private static void DrawFloatingCircle(Event e)
        {
            Vector2 mousePos = e.mousePosition;
            Rect visible = GetVisibleScreenBounds();

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
                    e.Use();
                }
            }

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

            if (e.type == EventType.Repaint)
            {
                string badgeLabel = _logoTex != null ? "" : "M";
                GUI.Box(_btnRect, badgeLabel, _circleBtnStyle);
            }
        }

        private static void DrawCloseButton(Event e)
        {
            Rect visibleScreen = GetVisibleScreenBounds();
            float btnW = Config.S(44f);
            float btnH = Config.S(34f);
            Rect closeRect = new Rect(visibleScreen.xMax - btnW - Config.S(16f), visibleScreen.yMin + Config.S(12f), btnW, btnH);

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
    }
}