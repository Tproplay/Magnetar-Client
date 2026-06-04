using Magnetar_Client.Core;
using Magnetar_Client.UI.Themes;
using UnityEngine;

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

        public HudElement(string name, Rect defaultBounds)
        {
            Name = name;
            Bounds = defaultBounds;
        }
        /// <summary>
        /// Runs once when the HUD element is enabled. Runs before OnUpdate.
        /// </summary>
        public virtual void OnEnable() { }
        /// <summary>
        /// Runs once when the HUD element is disabled. Runs after OnUpdate.
        /// </summary>
        public virtual void OnDisable() { }


        /// <summary>
        /// Runs every frame regardless of whether the HUD element is active or not.
        /// </summary>
        public virtual void OnUpdate() { if (_isCurrentlyEnabled) OnUpdateActive(); }

        /// <summary>
        /// Runs every frame only when the HUD element is active.
        /// Will not run if OnUpdate is overridden without calling base.OnUpdate().
        /// </summary>
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

                        // Calculate where the mouse is relative to the top-left of the element
                        dragOffset = e.mousePosition - new Vector2(Bounds.x, Bounds.y);

                        e.Use();
                    }
                }

                // B. Stop Dragging
                if (e.rawType == EventType.MouseUp && e.button == 0)
                {
                    if (ActiveDragId == WindowId)
                    {
                        ActiveDragId = -1;
                    }
                }

                // C. Process the Drag
                if (ActiveDragId == WindowId)
                {
                    Rect intendedBounds = new Rect(e.mousePosition.x - dragOffset.x, e.mousePosition.y - dragOffset.y, Bounds.width, Bounds.height);
                    Bounds = ApplySnapping(intendedBounds);
                }
            }

            // --- 2. RENDER THE ELEMENT ---

            GUIStyle windowStyle = (HUDManager.forceShow || HUDManager.showBackground) ? Magnetar_Default.ModuleOff : GUIStyle.none;

            if (HUDManager.forceShow)
            {
                GUI.Window(
                    WindowId,
                    Bounds,
                    (GUI.WindowFunction)DrawWindowContext,
                    "",
                    windowStyle
                );
            }
            else
            {
                if (windowStyle != GUIStyle.none)
                {
                    GUI.Box(Bounds, "", windowStyle);
                }

                GUI.BeginGroup(Bounds);
                DrawWindowContext(WindowId);
                GUI.EndGroup();
            }
        }

        private Rect ApplySnapping(Rect rect)
        {
            // --- CHECK MODIFIER KEYS ---
            bool isShiftHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            bool isCtrlHeld = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);

            // 1. GRID SNAPPING (Hold Ctrl)
            if (isCtrlHeld)
            {
                float gridSize = 10f;

                float gridX = Mathf.Round(rect.x / gridSize) * gridSize;
                float gridY = Mathf.Round(rect.y / gridSize) * gridSize;

                return new Rect(gridX, gridY, rect.width, rect.height);
            }

            // 2. MAGNETIC EDGE & ELEMENT SNAPPING (Hold Shift)
            if (isShiftHeld)
            {
                float snapDist = 10f;
                float canvasWidth = 1920f;
                float canvasHeight = 1080f;

                float snappedX = rect.x;
                float snappedY = rect.y;

                // --- SCREEN EDGE & CENTER SNAPPING ---
                if (Mathf.Abs(snappedX) < snapDist) snappedX = 0;
                else if (Mathf.Abs(snappedX + rect.width - canvasWidth) < snapDist) snappedX = canvasWidth - rect.width;
                else if (Mathf.Abs((snappedX + rect.width / 2f) - (canvasWidth / 2f)) < snapDist) snappedX = (canvasWidth / 2f) - (rect.width / 2f);

                if (Mathf.Abs(snappedY) < snapDist) snappedY = 0;
                else if (Mathf.Abs(snappedY + rect.height - canvasHeight) < snapDist) snappedY = canvasHeight - rect.height;
                else if (Mathf.Abs((snappedY + rect.height / 2f) - (canvasHeight / 2f)) < snapDist) snappedY = (canvasHeight / 2f) - (rect.height / 2f);

                // --- ELEMENT-TO-ELEMENT SNAPPING ---
                foreach (var other in HUDRenderer.Elements)
                {
                    if (other.WindowId == this.WindowId || !HUDRenderer.HudToggles.IsSelected(other.WindowId)) continue;

                    Rect otherR = other.Bounds;

                    // X-Axis
                    if (Mathf.Abs(snappedX - otherR.x) < snapDist) snappedX = otherR.x;
                    else if (Mathf.Abs(snappedX - (otherR.x + otherR.width)) < snapDist) snappedX = otherR.x + otherR.width;
                    else if (Mathf.Abs((snappedX + rect.width) - otherR.x) < snapDist) snappedX = otherR.x - rect.width;
                    else if (Mathf.Abs((snappedX + rect.width) - (otherR.x + otherR.width)) < snapDist) snappedX = otherR.x + otherR.width - rect.width;

                    // Y-Axis
                    if (Mathf.Abs(snappedY - otherR.y) < snapDist) snappedY = otherR.y;
                    else if (Mathf.Abs(snappedY - (otherR.y + otherR.height)) < snapDist) snappedY = otherR.y + otherR.height;
                    else if (Mathf.Abs((snappedY + rect.height) - otherR.y) < snapDist) snappedY = otherR.y - rect.height;
                    else if (Mathf.Abs((snappedY + rect.height) - (otherR.y + otherR.height)) < snapDist) snappedY = otherR.y + otherR.height - rect.height;
                }

                return new Rect(snappedX, snappedY, rect.width, rect.height);
            }

            // 3. FREE MOVE (No modifiers held)
            return rect;
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

        protected void AdjustWidthToText(string text, GUIStyle style, float padding = 10f)
        {
            Vector2 textSize = style.CalcSize(new GUIContent(text));
            float targetWidth = textSize.x + padding;

            if (Mathf.Abs(Bounds.width - targetWidth) > 1f)
            {
                Bounds.width = targetWidth;
            }
        }

        private static Vector2 windowPos = new Vector2(10, 10);
        public static Rect NewRect(float width = 250, float height = 28)
        {
            Rect rect = new Rect(windowPos.x, windowPos.y, width, height);

            if (windowPos.y > 800) { windowPos.x += 300; windowPos.y = 10; }
            else windowPos.y += height;

            return rect;
        }

    }
}
