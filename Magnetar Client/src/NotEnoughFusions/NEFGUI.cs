using UnityEngine;
using System.Collections.Generic;
using Magnetar_Client.UI.Themes;
using Magnetar_Client.Utils;
using Magnetar_Client.Core;
using static Magnetar_Client.UI.WindowDrawing.MiscDrawing;
using static Magnetar_Client.NEF.Data.NEFRecipes;
#if MELONLOADER || RELEASE_MELON
using Il2Cpp;
#endif

namespace Magnetar_Client.NEF
{
    public static class NEFGUI
    {
        public static string searchQuery = "";
        public static float currentScrollY = 0f;
        public static float usageScrollY = 0f;

        public static Vector2 pyramidPan = Vector2.zero;
        public static float pyramidZoom = 1f;
        public static bool isDraggingPyramid = false;

        // Hold-and-drag scroll tracking for Search Results Grid
        private static Vector2 _gridTouchStart = Vector2.zero;
        private static float _gridScrollStartVal = 0f;
        private static bool _isGridSwiping = false;

        // Hold-and-drag scroll tracking for Usages View
        private static Vector2 _usageTouchStart = Vector2.zero;
        private static float _usageScrollStartVal = 0f;
        private static bool _isUsageSwiping = false;

#if ANDROID
        // Long Press / Hold Tracking for Mobile Right Click (400ms threshold)
        private static RecipeEntity? _heldEntity = null;
        private static float _holdStartTime = 0f;
        private static Vector2 _holdStartScreenPos = Vector2.zero;
        private static bool _hasTriggeredHold = false;
        private const float LongPressThreshold = 0.40f;

        // Pinch-to-zoom tracking for Mobile
        private static float _lastPinchDistance = -1f;
        private static bool _isPinching = false;
#endif

        public static bool showUsagesView = false;
        public static Dictionary<Texture2D, GUIStyle> cachedImageStyles = new Dictionary<Texture2D, GUIStyle>();

        static bool firstLoad = true;

        public static void DrawNEFWindow(int windowID)
        {
            if (firstLoad)
            {
                firstLoad = false;
                NEFData.PerformSearch();
            }

            // Keep node layout distance in sync if GUI Scale changes dynamically
            if (NEFData.currentPyramidRoots.Count > 0 && Mathf.Abs(NEFData.lastCalculatedScale - Config.GUIScale) > 0.001f)
            {
                NEFData.RelayoutCurrentTrees();
            }

            Event e = Event.current;

#if ANDROID
            // 1. Long-press hold timer update
            if (_heldEntity.HasValue && !_hasTriggeredHold)
            {
                Vector2 currentScreenPos = GUIUtility.GUIToScreenPoint(e.mousePosition);
                float moveDist = Vector2.Distance(currentScreenPos, _holdStartScreenPos);

                if (moveDist > Config.S(20f))
                {
                    _heldEntity = null;
                }
                else if (Time.realtimeSinceStartup - _holdStartTime >= LongPressThreshold)
                {
                    _hasTriggeredHold = true;
                    NEFData.GenerateUsagesView(_heldEntity.Value);
                    _heldEntity = null;
                    e.Use();
                }
            }
#endif

            float rightPanelWidth = NEFManager.windowRect.width * 0.3f;

            // Dynamic top indent scaled with GUI font size to prevent overlapping the title bar
            float titleFontSize = Magnetar_Default.ModuleWindow != null ? Magnetar_Default.ModuleWindow.fontSize : Config.S(18f);
            float topIndent = Mathf.Max(Config.S(48f), titleFontSize + Config.S(18f));

            float pad = Config.S(10f);
            float leftPanelWidth = NEFManager.windowRect.width - rightPanelWidth - (pad * 3f);
            float contentHeight = NEFManager.windowRect.height - topIndent - pad;

            Rect pyramidBoxRect = new Rect(pad, topIndent, leftPanelWidth, contentHeight);
            Rect rightPanelRect = new Rect(pad + leftPanelWidth + pad, topIndent, rightPanelWidth, contentHeight);

            // ==========================================
            // 1. LEFT PANEL: VISUALIZER
            // ==========================================
            GUI.Box(pyramidBoxRect, "", Magnetar_Default.ModuleWindow);

            if (showUsagesView)
            {
                DrawUsagesView(pyramidBoxRect, e);
            }
            else
            {
                if (NEFData.currentPyramidRoots.Count == 0)
                {
                    GUI.Label(new Rect(pyramidBoxRect.x + pad, pyramidBoxRect.y + pad, leftPanelWidth - (pad * 2f), NEFManager.elementHeight),
                        Translator.Translate("Select an entity to view its recipes."));
                }
                else
                {
                    GUI.BeginGroup(pyramidBoxRect);

                    float minX = NEFData.currentPyramidRoots[0].RenderX;
                    float maxX = NEFData.currentPyramidRoots[NEFData.currentPyramidRoots.Count - 1].RenderX;
                    float centerOfAllTrees = (minX + maxX) / 2f;

                    for (int i = 0; i < NEFData.currentPyramidRoots.Count; i++)
                    {
                        DrawTree(NEFData.currentPyramidRoots[i], pyramidBoxRect, centerOfAllTrees, e);
                    }

                    GUI.EndGroup();
                }

                // --- PAN & ZOOM ---
                // --- PAN & ZOOM ---
                if (pyramidBoxRect.Contains(e.mousePosition))
                {
                    // 1. Mouse Scroll Wheel Zoom (Desktop)
                    if (e.type == EventType.ScrollWheel)
                    {
                        float oldZoom = pyramidZoom;
                        pyramidZoom -= e.delta.y * 0.05f;
                        pyramidZoom = Mathf.Clamp(pyramidZoom, 0.2f, 3.0f);

                        float originX = pyramidBoxRect.x + (pyramidBoxRect.width / 2f);
                        float originY = pyramidBoxRect.y + Config.S(60f);

                        float focusX = (e.mousePosition.x - originX - pyramidPan.x) / oldZoom;
                        float focusY = (e.mousePosition.y - originY - pyramidPan.y) / oldZoom;

                        pyramidPan.x = e.mousePosition.x - originX - (focusX * pyramidZoom);
                        pyramidPan.y = e.mousePosition.y - originY - (focusY * pyramidZoom);

                        e.Use();
                    }

                    // 2. Click / Touch Initiation
                    if (e.type == EventType.MouseDown && (e.button == 0 || e.button == 2))
                    {
                        isDraggingPyramid = true;
                        e.Use();
                    }
                }
#if ANDROID
                // 3. Pinch-to-Zoom (Touch Screens / Mobile)
                if (Input.touchCount >= 2)
                {
                    UnityEngine.Touch t0 = Input.GetTouch(0);
                    UnityEngine.Touch t1 = Input.GetTouch(1);

                    // Convert bottom-left screen space to top-left IMGUI window coordinates
                    Vector2 p0 = new Vector2(t0.position.x, Screen.height - t0.position.y);
                    Vector2 p1 = new Vector2(t1.position.x, Screen.height - t1.position.y);

                    // Only zoom if touches are within the visualizer box
                    if (pyramidBoxRect.Contains(p0) || pyramidBoxRect.Contains(p1))
                    {
                        float currentDist = Vector2.Distance(p0, p1);

                        if (t0.phase == TouchPhase.Began || t1.phase == TouchPhase.Began || !_isPinching || _lastPinchDistance <= 0f)
                        {
                            _isPinching = true;
                            _lastPinchDistance = currentDist;

                            _heldEntity = null;
            }
                        else if (t0.phase == TouchPhase.Moved || t1.phase == TouchPhase.Moved)
                        {
                            float deltaDist = currentDist - _lastPinchDistance;
                            if (Mathf.Abs(deltaDist) > 1f)
                            {
                                float oldZoom = pyramidZoom;
                                float zoomFactor = deltaDist * 0.005f;
                                pyramidZoom = Mathf.Clamp(pyramidZoom + zoomFactor, 0.2f, 3.0f);

                                Vector2 pinchCenter = (p0 + p1) * 0.5f;
                                float originX = pyramidBoxRect.x + (pyramidBoxRect.width / 2f);
                                float originY = pyramidBoxRect.y + Config.S(60f);

                                float focusX = (pinchCenter.x - originX - pyramidPan.x) / oldZoom;
                                float focusY = (pinchCenter.y - originY - pyramidPan.y) / oldZoom;

                                pyramidPan.x = pinchCenter.x - originX - (focusX * pyramidZoom);
                                pyramidPan.y = pinchCenter.y - originY - (focusY * pyramidZoom);

                                _lastPinchDistance = currentDist;
                                _heldEntity = null;

                                e.Use();
                            }
                        }
                    }
                }
                else
                {
                    _isPinching = false;
                    _lastPinchDistance = -1f;
                }
#endif
                // 4. Drag Pan (Only pan when not pinching)
                if (isDraggingPyramid && e.type == EventType.MouseDrag)
                {
#if ANDROID
                    if (!_isPinching)
                    {
                        pyramidPan.x += e.delta.x;
                        pyramidPan.y -= e.delta.y; // Inverted Y for Android touch
                        _heldEntity = null;
                    }
#else
                    pyramidPan += e.delta;
#endif
                    e.Use();
                }

                if (e.type == EventType.MouseUp || e.rawType == EventType.MouseUp)
                {
                    isDraggingPyramid = false;
                }

            }

            // ==========================================
            // 2. RIGHT PANEL: SEARCH & GRID
            // ==========================================
            float rx = rightPanelRect.x;
            float ry = rightPanelRect.y;

            string searchLabelText = Translator.Translate("Search:");
            GUIStyle labelStyle = Magnetar_Default.SettingDescriptionStyle ?? GUI.skin.label;
            float searchLabelWidth = labelStyle.CalcSize(new GUIContent(searchLabelText)).x + Config.S(8f);

            GUI.Label(new Rect(rx, ry, searchLabelWidth, NEFManager.elementHeight), searchLabelText, labelStyle);
            string newQuery = UI.WindowDrawing.DrawSetting.DrawManualTextField(
                new Rect(rx + searchLabelWidth, ry, rightPanelWidth - searchLabelWidth, NEFManager.elementHeight),
                searchQuery, Translator.Translate("Search..."));

            if (newQuery != searchQuery)
            {
                searchQuery = newQuery;
                currentScrollY = 0f;
                NEFData.PerformSearch();
            }

            ry += NEFManager.elementHeight + Config.S(6f);

            Rect clearBtnRect = new Rect(rx, ry, rightPanelWidth, NEFManager.elementHeight);
            bool clearHover = clearBtnRect.Contains(e.mousePosition);

            GUI.Box(clearBtnRect, Translator.Translate("Clear Search"), Magnetar_Default.ModuleOff);
            GUI.backgroundColor = Color.white;

            if (clearHover && e.type == EventType.MouseDown && e.button == 0)
            {
                searchQuery = "";
                currentScrollY = 0f;
                NEFData.PerformSearch();
                e.Use();
            }

            ry += NEFManager.elementHeight + Config.S(10f);

#if ANDROID
            GUI.Label(new Rect(rx, ry, rightPanelWidth, NEFManager.elementHeight),
                Translator.Translate($"Results") + " (" + NEFData.searchResults.Count + ") " +
                Translator.Translate("| Tap: Recipe | Hold: Usages"), labelStyle);
#else
            GUI.Label(new Rect(rx, ry, rightPanelWidth, NEFManager.elementHeight),
                Translator.Translate($"Results") + " (" + NEFData.searchResults.Count + ") " +
                Translator.Translate("| L-Click: Recipe | R-Click: Usages"), labelStyle);
#endif
            ry += NEFManager.elementHeight + Config.S(4f);

            float scrollHeight = rightPanelRect.height - (ry - rightPanelRect.y);
            Rect scrollRect = new Rect(rx, ry, rightPanelWidth, scrollHeight);

            int columns = Mathf.Max(3, Mathf.FloorToInt(rightPanelWidth / Config.S(100f)));
            float cellPadding = Config.S(5f);
            float itemSize = (rightPanelWidth - (cellPadding * (columns - 1))) / columns;
            int rowCount = Mathf.CeilToInt((float)NEFData.searchResults.Count / columns);
            float totalContentHeight = rowCount * (itemSize + cellPadding);
            float maxScrollY = Mathf.Max(0f, totalContentHeight - scrollRect.height);

            // Scroll Wheel
            if (scrollRect.Contains(e.mousePosition) && e.type == EventType.ScrollWheel)
            {
                currentScrollY += e.delta.y * Config.S(30f);
                currentScrollY = Mathf.Clamp(currentScrollY, 0f, maxScrollY);
                e.Use();
            }

            // Hold-and-Drag Scrolling for Results Grid
            if (e.type == EventType.MouseDown && e.button == 0 && scrollRect.Contains(e.mousePosition))
            {
                _gridTouchStart = e.mousePosition;
                _gridScrollStartVal = currentScrollY;
                _isGridSwiping = false;
            }

            if (e.type == EventType.MouseDrag && !_isGridSwiping && scrollRect.Contains(_gridTouchStart))
            {
                if (Vector2.Distance(e.mousePosition, _gridTouchStart) > Config.S(8f))
                {
                    _isGridSwiping = true;
#if ANDROID
                    _heldEntity = null;
#endif
                }
            }

            if (_isGridSwiping && (e.type == EventType.MouseDrag || e.type == EventType.MouseMove))
            {
                float deltaY = _gridTouchStart.y - e.mousePosition.y;
                currentScrollY = Mathf.Clamp(_gridScrollStartVal + deltaY, 0f, maxScrollY);
                e.Use();
            }

            GUI.BeginGroup(scrollRect);
            if (!PlantMixTreeManager.IsInitialized)
            {
                GUI.Label(new Rect(Config.S(5f), Config.S(5f), rightPanelWidth, Config.S(30f)), Translator.Translate("Loading data..."));
            }
            else
            {
                for (int i = 0; i < NEFData.searchResults.Count; i++)
                {
                    int col = i % columns;
                    int row = i / columns;

                    float btnX = col * (itemSize + cellPadding);
                    float btnY = row * (itemSize + cellPadding) - currentScrollY;

                    if (btnY + itemSize < 0 || btnY > scrollRect.height) continue;

                    RecipeEntity entity = NEFData.searchResults[i];
                    Rect plantBtnRect = new Rect(btnX, btnY, itemSize, itemSize);

#if ANDROID
                    if (plantBtnRect.Contains(e.mousePosition) && e.type == EventType.MouseDown && e.button == 0)
                    {
                        _heldEntity = entity;
                        _holdStartTime = Time.realtimeSinceStartup;
                        _holdStartScreenPos = GUIUtility.GUIToScreenPoint(e.mousePosition);
                        _hasTriggeredHold = false;
                    }
#endif

                    if (!_isGridSwiping && plantBtnRect.Contains(e.mousePosition) && e.type == EventType.MouseUp)
                    {
#if ANDROID
                        if (!_hasTriggeredHold && e.button == 0)
                        {
                            showUsagesView = false;
                            NEFData.GeneratePyramid(entity);
                            e.Use();
                        }
#else
                        if (e.button == 0)
                        {
                            showUsagesView = false;
                            NEFData.GeneratePyramid(entity);
                        }
                        else if (e.button == 1)
                        {
                            NEFData.GenerateUsagesView(entity);
                        }
                        e.Use();
#endif
                    }

                    DrawSquareNodeBox(plantBtnRect, entity, 1.5f);
                    GUI.backgroundColor = Color.white;
                }
            }
            GUI.EndGroup();

            if (e.type == EventType.MouseUp)
            {
                _isGridSwiping = false;
#if ANDROID
                _heldEntity = null;
                _hasTriggeredHold = false;
#endif
            }
        }

        private static void DrawUsagesView(Rect viewRect, Event e)
        {
            float pad = Config.S(10f);
            float btnW = Config.S(110f);
            float btnH = Config.S(30f);

            GUI.Label(new Rect(viewRect.x + pad, viewRect.y + pad, viewRect.width - btnW - (pad * 2f), btnH),
                Translator.Translate("Fusions requiring") + ": " +
                NEFData.GetEntityName(NEFData.usageViewTarget) + " (" +
                NEFData.currentUsages.Count + " " +
                Translator.Translate("found") + ")"
                );

            Rect backBtnRect = new Rect(viewRect.x + viewRect.width - btnW - pad, viewRect.y + pad, btnW, btnH);
            if (backBtnRect.Contains(e.mousePosition) && e.type == EventType.MouseDown && e.button == 0)
            {
                showUsagesView = false;
                e.Use();
            }

            GUI.Box(backBtnRect, Translator.Translate("Back to Tree"), Magnetar_Default.ModuleOff);
            GUI.backgroundColor = Color.white;

            if (NEFData.currentUsages.Count == 0)
            {
                GUI.Label(new Rect(viewRect.x + pad, viewRect.y + Config.S(50f), viewRect.width - (pad * 2f), btnH),
                    Translator.Translate("This entity is not used as an ingredient in any fusion."));
                return;
            }

            float scrollStartY = viewRect.y + Config.S(50f);
            Rect scrollAreaRect = new Rect(viewRect.x + pad, scrollStartY, viewRect.width - (pad * 2f), viewRect.height - Config.S(60f));

            int columns = Mathf.Max(3, Mathf.FloorToInt(scrollAreaRect.width / Config.S(115f)));
            float padding = Config.S(10f);
            float itemSize = (scrollAreaRect.width - (padding * (columns - 1))) / columns;
            int rowCount = Mathf.CeilToInt((float)NEFData.currentUsages.Count / columns);
            float totalContentHeight = rowCount * (itemSize + padding);
            float maxScroll = Mathf.Max(0f, totalContentHeight - scrollAreaRect.height);

            if (scrollAreaRect.Contains(e.mousePosition) && e.type == EventType.ScrollWheel)
            {
                usageScrollY += e.delta.y * Config.S(30f);
                usageScrollY = Mathf.Clamp(usageScrollY, 0f, maxScroll);
                e.Use();
            }

            // Hold-and-Drag Scrolling for Usages View
            if (e.type == EventType.MouseDown && e.button == 0 && scrollAreaRect.Contains(e.mousePosition))
            {
                _usageTouchStart = e.mousePosition;
                _usageScrollStartVal = usageScrollY;
                _isUsageSwiping = false;
            }

            if (e.type == EventType.MouseDrag && !_isUsageSwiping && scrollAreaRect.Contains(_usageTouchStart))
            {
                if (Vector2.Distance(e.mousePosition, _usageTouchStart) > Config.S(8f))
                {
                    _isUsageSwiping = true;
#if ANDROID
                    _heldEntity = null;
#endif
                }
            }

            if (_isUsageSwiping && (e.type == EventType.MouseDrag || e.type == EventType.MouseMove))
            {
                float deltaY = _usageTouchStart.y - e.mousePosition.y;
                usageScrollY = Mathf.Clamp(_usageScrollStartVal + deltaY, 0f, maxScroll);
                e.Use();
            }

            GUI.BeginGroup(scrollAreaRect);
            for (int i = 0; i < NEFData.currentUsages.Count; i++)
            {
                int col = i % columns;
                int row = i / columns;

                float btnX = col * (itemSize + padding);
                float btnY = row * (itemSize + padding) - usageScrollY;

                if (btnY + itemSize < 0 || btnY > scrollAreaRect.height) continue;

                RecipeEntity resultEntity = NEFData.currentUsages[i].Result;
                Rect plantBtnRect = new Rect(btnX, btnY, itemSize, itemSize);

#if ANDROID
                if (plantBtnRect.Contains(e.mousePosition) && e.type == EventType.MouseDown && e.button == 0)
                {
                    _heldEntity = resultEntity;
                    _holdStartTime = Time.realtimeSinceStartup;
                    _holdStartScreenPos = GUIUtility.GUIToScreenPoint(e.mousePosition);
                    _hasTriggeredHold = false;
                }
#endif

                if (!_isUsageSwiping && plantBtnRect.Contains(e.mousePosition) && e.type == EventType.MouseUp)
                {
#if ANDROID
                    if (!_hasTriggeredHold && e.button == 0)
                    {
                        showUsagesView = false;
                        NEFData.GeneratePyramid(resultEntity);
                        e.Use();
                    }
#else
                    if (e.button == 0)
                    {
                        showUsagesView = false;
                        NEFData.GeneratePyramid(resultEntity);
                    }
                    else if (e.button == 1)
                    {
                        NEFData.GenerateUsagesView(resultEntity);
                    }
                    e.Use();
#endif
                }

                DrawSquareNodeBox(plantBtnRect, resultEntity, 1f);
            }
            GUI.EndGroup();

            if (e.type == EventType.MouseUp)
            {
                _isUsageSwiping = false;
#if ANDROID
                _heldEntity = null;
                _hasTriggeredHold = false;
#endif
            }
        }

        private static Vector2 GetProjectedPosition(float logicX, float logicY, Rect canvasRect, float centerOfAllTrees)
        {
            float centeredX = logicX - centerOfAllTrees;
            float screenX = (canvasRect.width / 2f) + (centeredX * pyramidZoom) + pyramidPan.x;
            float screenY = Config.S(60f) + (logicY * pyramidZoom) + pyramidPan.y;
            return new Vector2(screenX, screenY);
        }

        private static void DrawTree(NEFData.RecipeNode node, Rect canvasRect, float centerOfAllTrees, Event e)
        {
            if (node == null) return;

            float baseSize = Config.S(100f);
            float scaledSize = baseSize * pyramidZoom;

            Vector2 pos = GetProjectedPosition(node.RenderX, node.RenderY, canvasRect, centerOfAllTrees);
            Rect nodeRect = new Rect(pos.x - (scaledSize / 2f), pos.y, scaledSize, scaledSize);

            // Connection Lines
            if (node.IsSingle)
            {
                Vector2 childPosA = GetProjectedPosition(node.ParentA.RenderX, node.ParentA.RenderY, canvasRect, centerOfAllTrees);
                DrawOrthogonalLine(new Vector2(pos.x, pos.y + scaledSize), new Vector2(childPosA.x, childPosA.y), pyramidZoom);
                DrawTree(node.ParentA, canvasRect, centerOfAllTrees, e);
            }
            else if (node.ParentA != null && node.ParentB != null)
            {
                Vector2 childPosA = GetProjectedPosition(node.ParentA.RenderX, node.ParentA.RenderY, canvasRect, centerOfAllTrees);
                Vector2 childPosB = GetProjectedPosition(node.ParentB.RenderX, node.ParentB.RenderY, canvasRect, centerOfAllTrees);

                DrawOrthogonalLine(new Vector2(pos.x, pos.y + scaledSize), new Vector2(childPosA.x, childPosA.y), pyramidZoom);
                DrawOrthogonalLine(new Vector2(pos.x, pos.y + scaledSize), new Vector2(childPosB.x, childPosB.y), pyramidZoom);

                DrawTree(node.ParentA, canvasRect, centerOfAllTrees, e);
                DrawTree(node.ParentB, canvasRect, centerOfAllTrees, e);

                if (node.IsTriple && node.ParentC != null)
                {
                    Vector2 childPosC = GetProjectedPosition(node.ParentC.RenderX, node.ParentC.RenderY, canvasRect, centerOfAllTrees);
                    DrawOrthogonalLine(new Vector2(pos.x, pos.y + scaledSize), new Vector2(childPosC.x, childPosC.y), pyramidZoom);
                    DrawTree(node.ParentC, canvasRect, centerOfAllTrees, e);
                }
            }

            // Draw Edge Message
            if (!string.IsNullOrEmpty(node.EdgeMessage))
            {
                Color oldColor = GUI.contentColor;
                GUI.contentColor = node.EdgeMessageColor;
                GUIStyle msgStyle = new GUIStyle() { alignment = TextAnchor.LowerCenter, fontSize = Mathf.Max(1, (int)(Config.S(16f) * pyramidZoom)) };

                Rect msgRect = new Rect(pos.x - (Config.S(100f) * pyramidZoom), pos.y - (Config.S(30f) * pyramidZoom), Config.S(200f) * pyramidZoom, Config.S(30f) * pyramidZoom);
                GUI.Label(msgRect, node.EdgeMessage, msgStyle);
                GUI.contentColor = oldColor;
            }

#if ANDROID
            if (nodeRect.Contains(e.mousePosition) && e.type == EventType.MouseDown && e.button == 0)
            {
                _heldEntity = node.Entity;
                _holdStartTime = Time.realtimeSinceStartup;
                _holdStartScreenPos = GUIUtility.GUIToScreenPoint(e.mousePosition);
                _hasTriggeredHold = false;
            }
#endif

            if (nodeRect.Contains(e.mousePosition) && e.type == EventType.MouseUp)
            {
#if ANDROID
                if (!_hasTriggeredHold && e.button == 0)
                {
                    NEFData.GeneratePyramid(node.Entity);
                    e.Use();
                }
#else
                if (e.button == 0) NEFData.GeneratePyramid(node.Entity);
                else if (e.button == 1) NEFData.GenerateUsagesView(node.Entity);
                e.Use();
#endif
            }

            DrawSquareNodeBox(nodeRect, node.Entity, pyramidZoom);
            GUI.backgroundColor = Color.white;
        }

        private static void DrawSquareNodeBox(Rect rect, RecipeEntity entity, float scale)
        {
            Magnetar_Default.NEFNodeStyle.fontSize = Mathf.Max(1, (int)(Config.S(8f) * scale));
            string displayName = NEFData.GetEntityName(entity);
            GUI.Box(rect, displayName, Magnetar_Default.NEFNodeStyle);

            GUIStyle imgStyle = GetEntityStyle(entity);

            if (imgStyle != null && imgStyle.normal.background != null)
            {
                Texture2D tex = imgStyle.normal.background;

                float pad = Config.S(10f) * scale;
                float bottomTextSpace = Config.S(25f) * scale;

                float availWidth = rect.width - (pad * 2f);
                float availHeight = rect.height - pad - bottomTextSpace;

                float texAspect = (float)tex.width / (float)Mathf.Max(1, tex.height);
                float availAspect = availWidth / availHeight;

                float drawWidth = availWidth;
                float drawHeight = availHeight;

                if (texAspect > availAspect) drawHeight = availWidth / texAspect;
                else drawWidth = availHeight * texAspect;

                float centerX = rect.x + pad + (availWidth / 2f);
                float centerY = rect.y + pad + (availHeight / 2f);

                Rect imageRect = new Rect(centerX - (drawWidth / 2f), centerY - (drawHeight / 2f), drawWidth, drawHeight);
                GUI.Box(imageRect, GUIContent.none, imgStyle);
            }
        }

        private static Dictionary<int, GUIStyle> cachedEntityStyles = new Dictionary<int, GUIStyle>();

        private static GUIStyle GetEntityStyle(RecipeEntity entity)
        {
            if (cachedEntityStyles.TryGetValue(entity.Id, out GUIStyle style))
            {
                return style;
            }

            Texture2D finalTex = null;

            if (NEFData.LegacyLoadEntities.Contains(entity.Id) || entity.Id >= 3000)
            {
                finalTex = entity.IsZombie
                    ? Utils.TextureLoader.GetZombieTexture(entity.Id)
                    : Utils.TextureLoader.GetPlantTexture(entity.Id);
            }

            if (finalTex == null)
            {
                Sprite sprite = GetEntitySprite(entity);
                if (sprite != null && sprite.texture != null)
                {
                    if (sprite.rect.width == sprite.texture.width && sprite.rect.height == sprite.texture.height)
                    {
                        finalTex = sprite.texture;
                    }
                    else
                    {
                        RenderTexture tmp = RenderTexture.GetTemporary(
                            sprite.texture.width,
                            sprite.texture.height,
                            0,
                            RenderTextureFormat.Default,
                            RenderTextureReadWrite.Linear);

                        Graphics.Blit(sprite.texture, tmp);
                        RenderTexture previous = RenderTexture.active;
                        RenderTexture.active = tmp;

                        finalTex = new Texture2D((int)sprite.rect.width, (int)sprite.rect.height, TextureFormat.RGBA32, false);
                        finalTex.ReadPixels(new Rect(sprite.rect.x, sprite.rect.y, sprite.rect.width, sprite.rect.height), 0, 0);
                        finalTex.Apply();

                        RenderTexture.active = previous;
                        RenderTexture.ReleaseTemporary(tmp);
                    }
                }
            }

            if (finalTex == null) return null;

            GUIStyle newStyle = new GUIStyle();
            newStyle.normal.background = finalTex;
            cachedEntityStyles[entity.Id] = newStyle;

            return newStyle;
        }

        private static Sprite GetEntitySprite(RecipeEntity entity)
        {
            if (GameAPP.resourcesManager == null) return null;

            if (entity.IsZombie)
            {
                ZombieType zType = (ZombieType)entity.Id;
                if (GameAPP.resourcesManager.zombieSprites.ContainsKey(zType))
                {
                    return GameAPP.resourcesManager.zombieSprites[zType];
                }
            }
            else
            {
                PlantType pType = (PlantType)entity.Id;
                if (GameAPP.resourcesManager.plantPreviews.ContainsKey(pType))
                {
                    GameObject previewObj = GameAPP.resourcesManager.plantPreviews[pType];
                    if (previewObj != null)
                    {
                        SpriteRenderer sr = previewObj.GetComponent<SpriteRenderer>();
                        if (sr != null) return sr.sprite;

                        UnityEngine.UI.Image img = previewObj.GetComponent<UnityEngine.UI.Image>();
                        if (img != null) return img.sprite;
                    }
                }
            }
            return null;
        }
    }
}