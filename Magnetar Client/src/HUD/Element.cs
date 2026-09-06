using Magnetar_Client.Core;
using Magnetar_Client.UI.Themes;
using UnityEngine;
using System;

namespace Magnetar_Client.HUDElements
{
    public abstract class HudElement
    {
        private bool _isCurrentlyEnabled = false;
        public string Name { get; set; }
        public Rect Bounds;
        public int WindowId { get; set; }

        public static int ActiveDragId = -1;
        private Vector2 dragOffset;

        public float UpdateInterval = 0;
        private float _updateInterval = 0;

        private GUI.WindowFunction _cachedWindowDelegate;

        private GUI.WindowFunction GetWindowDelegate()
        {
            if (_cachedWindowDelegate == null)
            {
                _cachedWindowDelegate = Il2CppInterop.Runtime.DelegateSupport.ConvertDelegate<GUI.WindowFunction>((Action<int>)DrawWindowContext);
            }
            return _cachedWindowDelegate;
        }

        public HudElement(string name, Rect defaultBounds)
        {
            Name = name;
            Bounds = defaultBounds;
        }

        public virtual void OnEnable() { }
        public virtual void OnDisable() { }

        public virtual void OnUpdate()
        {
            if (_isCurrentlyEnabled)
            {
                if (UpdateInterval <= 0f)
                {
                    OnUpdateActive();
                }
                else
                {
                    _updateInterval += Time.unscaledDeltaTime;
                    if (_updateInterval >= UpdateInterval)
                    {
                        _updateInterval = 0f;
                        OnUpdateActive();
                    }
                }
            }
        }

        public virtual void OnUpdateActive() { }

        public void HandleLifecycle(bool isEnabled)
        {
            if (isEnabled && !_isCurrentlyEnabled)
            {
                _isCurrentlyEnabled = true;
                OnEnable();
            }
            else if (!isEnabled && _isCurrentlyEnabled)
            {
                _isCurrentlyEnabled = false;
                OnDisable();
            }

            OnUpdate();
        }

        public void Render()
        {
            Event e = Event.current;

            if (HUDManager.forceShow)
            {
                // A. Start Dragging
                if (e.type == EventType.MouseDown && e.button == 0 && Bounds.Contains(e.mousePosition))
                {
                    if (ActiveDragId == -1)
                    {
                        ActiveDragId = WindowId;
                        dragOffset = e.mousePosition - new Vector2(Bounds.x, Bounds.y);
                        e.Use();
                    }
                }

                // B. Process Drag
                if (ActiveDragId == WindowId)
                {
                    Rect intendedBounds = new Rect(e.mousePosition.x - dragOffset.x, e.mousePosition.y - dragOffset.y, Bounds.width, Bounds.height);
                    Bounds = ApplySnapping(intendedBounds);
                }

                // C. Stop Dragging
                if ((e.type == EventType.MouseUp || e.rawType == EventType.MouseUp) && e.button == 0)
                {
                    if (ActiveDragId == WindowId)
                    {
                        ActiveDragId = -1;
                        e.Use();
                    }
                }
            }

            // --- 2. RENDER THE ELEMENT ---
            GUIStyle windowStyle = (HUDManager.forceShow || HUDManager.showBackground) ? Magnetar_Default.ModuleOff : GUIStyle.none;

            if (HUDManager.forceShow)
            {
                GUI.Window(
                    WindowId,
                    Bounds,
                    GetWindowDelegate(),
                    "",
                    windowStyle
                );
            }
            else
            {
                if (windowStyle != GUIStyle.none && Event.current.type == EventType.Repaint)
                {
                    windowStyle.Draw(Bounds, false, false, false, false);
                }

                if (Bounds.width > 0 && Bounds.height > 0)
                {
                    GUI.BeginGroup(Bounds);
                    DrawWindowContext(WindowId);
                    GUI.EndGroup();
                }
            }
        }

        private Rect ApplySnapping(Rect rect)
        {
#if ANDROID
            // Mobile: Snapping is automatic and magnetic without requiring keyboard modifiers
            return CalculateFlushDocking(rect);
#else
            // PC: Preserves key combinations
            bool isShiftHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            bool isCtrlHeld = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);

            if (isCtrlHeld)
            {
                float gridSize = 10f;
                float gridX = Mathf.Round(rect.x / gridSize) * gridSize;
                float gridY = Mathf.Round(rect.y / gridSize) * gridSize;
                return new Rect(gridX, gridY, rect.width, rect.height);
            }

            if (isShiftHeld)
            {
                return CalculateFlushDocking(rect);
            }

            // Free-form movement when no keys are held on PC
            return rect;
#endif
        }

        private Rect CalculateFlushDocking(Rect rect)
        {
            float snapThreshold = 14f * Mathf.Max(1f, Config.ElementScale);
            float canvasWidth = 1920f;
            float canvasHeight = 1080f;

            float bestX = rect.x;
            float bestDiffX = snapThreshold;

            float bestY = rect.y;
            float bestDiffY = snapThreshold;

            // 1. Canvas Boundary Snapping
            if (Mathf.Abs(rect.x) < bestDiffX)
            {
                bestX = 0f;
                bestDiffX = Mathf.Abs(rect.x);
            }
            if (Mathf.Abs(rect.x + rect.width - canvasWidth) < bestDiffX)
            {
                bestX = canvasWidth - rect.width;
                bestDiffX = Mathf.Abs(rect.x + rect.width - canvasWidth);
            }
            if (Mathf.Abs(rect.x + rect.width / 2f - canvasWidth / 2f) < bestDiffX)
            {
                bestX = (canvasWidth - rect.width) / 2f;
                bestDiffX = Mathf.Abs(rect.x + rect.width / 2f - canvasWidth / 2f);
            }

            if (Mathf.Abs(rect.y) < bestDiffY)
            {
                bestY = 0f;
                bestDiffY = Mathf.Abs(rect.y);
            }
            if (Mathf.Abs(rect.y + rect.height - canvasHeight) < bestDiffY)
            {
                bestY = canvasHeight - rect.height;
                bestDiffY = Mathf.Abs(rect.y + rect.height - canvasHeight);
            }
            if (Mathf.Abs(rect.y + rect.height / 2f - canvasHeight / 2f) < bestDiffY)
            {
                bestY = (canvasHeight - rect.height) / 2f;
                bestDiffY = Mathf.Abs(rect.y + rect.height / 2f - canvasHeight / 2f);
            }

            // 2. Element-to-Element Flush Docking (0px gap)
            if (HUDRenderer.Elements != null)
            {
                for (int i = 0; i < HUDRenderer.Elements.Count; i++)
                {
                    var other = HUDRenderer.Elements[i];
                    if (other == null || other.WindowId == this.WindowId) continue;
                    if (HUDRenderer.HudToggles != null && !HUDRenderer.HudToggles.IsSelected(other.WindowId)) continue;

                    Rect otherR = other.Bounds;

                    // X-Axis Alignment & Docking
                    float dLeft = Mathf.Abs(rect.x - otherR.x);
                    if (dLeft < bestDiffX) { bestX = otherR.x; bestDiffX = dLeft; }

                    float dRight = Mathf.Abs((rect.x + rect.width) - (otherR.x + otherR.width));
                    if (dRight < bestDiffX) { bestX = otherR.x + otherR.width - rect.width; bestDiffX = dRight; }

                    float dDockRight = Mathf.Abs(rect.x - (otherR.x + otherR.width));
                    if (dDockRight < bestDiffX) { bestX = otherR.x + otherR.width; bestDiffX = dDockRight; }

                    float dDockLeft = Mathf.Abs((rect.x + rect.width) - otherR.x);
                    if (dDockLeft < bestDiffX) { bestX = otherR.x - rect.width; bestDiffX = dDockLeft; }

                    // Y-Axis Alignment & Docking
                    float dDockUnder = Mathf.Abs(rect.y - (otherR.y + otherR.height));
                    if (dDockUnder < bestDiffY) { bestY = otherR.y + otherR.height; bestDiffY = dDockUnder; }

                    float dDockAbove = Mathf.Abs((rect.y + rect.height) - otherR.y);
                    if (dDockAbove < bestDiffY) { bestY = otherR.y - rect.height; bestDiffY = dDockAbove; }

                    float dTop = Mathf.Abs(rect.y - otherR.y);
                    if (dTop < bestDiffY) { bestY = otherR.y; bestDiffY = dTop; }

                    float dBottom = Mathf.Abs((rect.y + rect.height) - (otherR.y + otherR.height));
                    if (dBottom < bestDiffY) { bestY = otherR.y + otherR.height - rect.height; bestDiffY = dBottom; }
                }
            }

            return new Rect(bestX, bestY, rect.width, rect.height);
        }

        private void DrawWindowContext(int id)
        {
            float width = Bounds.width;
            float height = Bounds.height;
            Event e = Event.current;

            if (HUDManager.forceShow)
            {
                Rect localBounds = new Rect(0, 0, width, height);

                if ((localBounds.Contains(e.mousePosition) || ActiveDragId == WindowId) && HUDManager.forceShow)
                {
                    GUI.backgroundColor = new Color(1f, 0f, 0f, 0.3f);
                    GUI.Box(localBounds, "", Magnetar_Default.ModuleOn);
                    GUI.backgroundColor = Color.white;
                }
            }

            DrawContent(width, height);
        }

        protected abstract void DrawContent(float width, float height);

        protected void AdjustWidthToText(string text, GUIStyle style, float padding = 8f)
        {
            if (style == null || string.IsNullOrEmpty(text)) return;

            Vector2 textSize = style.CalcSize(new GUIContent(text));
            float targetWidth = textSize.x + (padding * Config.ElementScale);
            float targetHeight = textSize.y + (4f * Config.ElementScale);

            if (Mathf.Abs(Bounds.width - targetWidth) > 0.5f)
            {
                Bounds.width = targetWidth;
            }
            if (Mathf.Abs(Bounds.height - targetHeight) > 0.5f)
            {
                Bounds.height = targetHeight;
            }
        }

        private static Vector2 windowPos = new Vector2(10, 10);
        public static Rect NewRect(float width = 250, float height = 24)
        {
            float actualHeight = height * Config.ElementScale;
            Rect rect = new Rect(windowPos.x, windowPos.y, width * Config.ElementScale, actualHeight);

            if (windowPos.y > 800)
            {
                windowPos.x += 300;
                windowPos.y = 10;
            }
            else
            {
                windowPos.y += actualHeight;
            }

            return rect;
        }
    }
}