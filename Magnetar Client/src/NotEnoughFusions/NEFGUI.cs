using Il2Cpp;
using UnityEngine;
using System.Collections.Generic;
using Magnetar_Client.UI.Themes;
using Magnetar_Client.Utils;
using Magnetar_Client.Core;
using static Magnetar_Client.UI.WindowDrawing.MiscDrawing;
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

        public static bool showUsagesView = false;
        public static Dictionary<Texture2D, GUIStyle> cachedImageStyles = new Dictionary<Texture2D, GUIStyle>();

        public static void DrawNEFWindow(int windowID)
        {
            Event e = Event.current;

            float rightPanelWidth = NEFManager.windowRect.width * 0.3f;
            float topIndent = 35f;
            float leftPanelWidth = NEFManager.windowRect.width - rightPanelWidth - 30f;
            float contentHeight = NEFManager.windowRect.height - topIndent - 10f;

            Rect pyramidBoxRect = new Rect(10f, topIndent, leftPanelWidth, contentHeight);
            Rect rightPanelRect = new Rect(10f + leftPanelWidth + 10f, topIndent, rightPanelWidth, contentHeight);

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
                    GUI.Label(new Rect(pyramidBoxRect.x + 10f, pyramidBoxRect.y + 10f, 400f, NEFManager.elementHeight), "Select a plant to view its recipes.");
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
                if (pyramidBoxRect.Contains(e.mousePosition))
                {
                    if (e.type == EventType.ScrollWheel)
                    {
                        float oldZoom = pyramidZoom;
                        pyramidZoom -= e.delta.y * 0.05f;
                        pyramidZoom = Mathf.Clamp(pyramidZoom, 0.2f, 3.0f);

                        float originX = pyramidBoxRect.x + (pyramidBoxRect.width / 2f);
                        float originY = pyramidBoxRect.y + 60f;

                        float focusX = (e.mousePosition.x - originX - pyramidPan.x) / oldZoom;
                        float focusY = (e.mousePosition.y - originY - pyramidPan.y) / oldZoom;

                        pyramidPan.x = e.mousePosition.x - originX - (focusX * pyramidZoom);
                        pyramidPan.y = e.mousePosition.y - originY - (focusY * pyramidZoom);

                        e.Use();
                    }

                    if (e.type == EventType.MouseDown && (e.button == 0 || e.button == 2))
                    {
                        isDraggingPyramid = true;
                        e.Use();
                    }
                }

                if (isDraggingPyramid && e.type == EventType.MouseDrag)
                {
                    pyramidPan += e.delta;
                    e.Use();
                }

                if (e.rawType == EventType.MouseUp)
                {
                    isDraggingPyramid = false;
                }
            }

            // ==========================================
            // 2. RIGHT PANEL: SEARCH & GRID
            // ==========================================
            float rx = rightPanelRect.x;
            float ry = rightPanelRect.y;

            GUI.Label(new Rect(rx, ry, rightPanelWidth, NEFManager.elementHeight), "Search:");
            string newQuery = Magnetar_Client.UI.WindowDrawing.DrawSetting.DrawManualTextField(new Rect(rx + 65f, ry, rightPanelWidth - 65f, NEFManager.elementHeight), searchQuery, "Search...");

            if (newQuery != searchQuery)
            {
                searchQuery = newQuery;
                currentScrollY = 0f;
                NEFData.PerformSearch();
            }

            ry += NEFManager.elementHeight + 5f;

            Rect clearBtnRect = new Rect(rx, ry, rightPanelWidth, NEFManager.elementHeight);
            bool clearHover = clearBtnRect.Contains(e.mousePosition);

            GUI.Box(clearBtnRect, "Clear Search", Magnetar_Default.ModuleOff);
            GUI.backgroundColor = Color.white;

            if (clearHover && e.type == EventType.MouseDown && e.button == 0)
            {
                searchQuery = "";
                currentScrollY = 0f;
                NEFData.PerformSearch();
                e.Use();
            }

            ry += NEFManager.elementHeight + 15f;

            GUI.Label(new Rect(rx, ry, rightPanelWidth, NEFManager.elementHeight), $"Results ({NEFData.searchResults.Count}) | L-Click: Recipe | R-Click: Usages");
            ry += NEFManager.elementHeight;

            float scrollHeight = rightPanelRect.height - (ry - rightPanelRect.y);
            Rect scrollRect = new Rect(rx, ry, rightPanelWidth, scrollHeight);

            int columns = Mathf.Max(3, Mathf.FloorToInt(rightPanelWidth / 100f));
            float padding = 5f;
            float itemSize = (rightPanelWidth - (padding * (columns - 1))) / columns;
            int rowCount = Mathf.CeilToInt((float)NEFData.searchResults.Count / columns);
            float totalContentHeight = rowCount * (itemSize + padding);
            float maxScrollY = Mathf.Max(0f, totalContentHeight - scrollRect.height);

            if (scrollRect.Contains(e.mousePosition) && e.type == EventType.ScrollWheel)
            {
                currentScrollY += e.delta.y * 30f;
                currentScrollY = Mathf.Clamp(currentScrollY, 0f, maxScrollY);
                e.Use();
            }

            GUI.BeginGroup(scrollRect);
            if (!PlantMixTreeManager.IsInitialized)
            {
                GUI.Label(new Rect(5, 5, rightPanelWidth, 30), "Loading data...");
            }
            else
            {
                for (int i = 0; i < NEFData.searchResults.Count; i++)
                {
                    int col = i % columns;
                    int row = i / columns;

                    float btnX = col * (itemSize + padding);
                    float btnY = row * (itemSize + padding) - currentScrollY;

                    if (btnY + itemSize < 0 || btnY > scrollRect.height) continue;

                    PlantType plant = NEFData.searchResults[i];
                    Rect plantBtnRect = new Rect(btnX, btnY, itemSize, itemSize);

                    if (plantBtnRect.Contains(e.mousePosition) && e.type == EventType.MouseUp)
                    {
                        if (e.button == 0)
                        {
                            showUsagesView = false;
                            NEFData.GeneratePyramid(plant);
                        }
                        else if (e.button == 1)
                        {
                            NEFData.GenerateUsagesView(plant);
                        }
                        e.Use();
                    }

                    DrawSquareNodeBox(plantBtnRect, plant, 1.5f);
                    GUI.backgroundColor = Color.white;
                }
            }
            GUI.EndGroup();
        }

        private static void DrawUsagesView(Rect viewRect, Event e)
        {
            GUI.Label(new Rect(viewRect.x + 10f, viewRect.y + 10f, viewRect.width - 150f, 30f), $"Fusions requiring: {NEFData.GetPlantName(NEFData.usageViewTarget)} ({NEFData.currentUsages.Count} found)");

            Rect backBtnRect = new Rect(viewRect.x + viewRect.width - 110f, viewRect.y + 10f, 100f, 30f);
            if (backBtnRect.Contains(e.mousePosition) && e.type == EventType.MouseDown && e.button == 0)
            {
                showUsagesView = false;
                e.Use();
            }

            GUI.Box(backBtnRect, "Back to Tree", Magnetar_Default.ModuleOff);
            GUI.backgroundColor = Color.white;

            if (NEFData.currentUsages.Count == 0)
            {
                GUI.Label(new Rect(viewRect.x + 10f, viewRect.y + 50f, 400f, 30f), "This plant is not used as an ingredient in any fusion.");
                return;
            }

            Rect scrollAreaRect = new Rect(viewRect.x + 10f, viewRect.y + 50f, viewRect.width - 20f, viewRect.height - 60f);

            // Square grid shape layout formulation
            int columns = Mathf.Max(3, Mathf.FloorToInt(scrollAreaRect.width / 115f));
            float padding = 10f;
            float itemSize = (scrollAreaRect.width - (padding * (columns - 1))) / columns;
            int rowCount = Mathf.CeilToInt((float)NEFData.currentUsages.Count / columns);
            float totalContentHeight = rowCount * (itemSize + padding);
            float maxScroll = Mathf.Max(0f, totalContentHeight - scrollAreaRect.height);

            if (scrollAreaRect.Contains(e.mousePosition) && e.type == EventType.ScrollWheel)
            {
                usageScrollY += e.delta.y * 30f;
                usageScrollY = Mathf.Clamp(usageScrollY, 0f, maxScroll);
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

                PlantType resultPlant = NEFData.currentUsages[i].Result;
                Rect plantBtnRect = new Rect(btnX, btnY, itemSize, itemSize);

                if (plantBtnRect.Contains(e.mousePosition) && e.type == EventType.MouseUp)
                {
                    if (e.button == 0)
                    {
                        showUsagesView = false;
                        NEFData.GeneratePyramid(resultPlant);
                    }
                    else if (e.button == 1)
                    {
                        NEFData.GenerateUsagesView(resultPlant);
                    }
                    e.Use();
                }

                DrawSquareNodeBox(plantBtnRect, resultPlant, 1f);
            }
            GUI.EndGroup();
        }

        private static Vector2 GetProjectedPosition(float logicX, float logicY, Rect canvasRect, float centerOfAllTrees)
        {
            float centeredX = logicX - centerOfAllTrees;
            float screenX = (canvasRect.width / 2f) + (centeredX * pyramidZoom) + pyramidPan.x;
            float screenY = 60f + (logicY * pyramidZoom) + pyramidPan.y;
            return new Vector2(screenX, screenY);
        }

        private static void DrawTree(NEFData.RecipeNode node, Rect canvasRect, float centerOfAllTrees, Event e)
        {
            if (node == null) return;

            float baseSize = 100f;
            float scaledSize = baseSize * pyramidZoom;

            Vector2 pos = GetProjectedPosition(node.RenderX, node.RenderY, canvasRect, centerOfAllTrees);
            Rect nodeRect = new Rect(pos.x - (scaledSize / 2f), pos.y, scaledSize, scaledSize);

            // Connect lines depending on binary or ternary tree sizes
            if (node.ParentA != null && node.ParentB != null)
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

            if (nodeRect.Contains(e.mousePosition) && e.type == EventType.MouseUp)
            {
                if (e.button == 0) NEFData.GeneratePyramid(node.Plant);
                else if (e.button == 1) NEFData.GenerateUsagesView(node.Plant);
                e.Use();
            }

            DrawSquareNodeBox(nodeRect, node.Plant, pyramidZoom);
            GUI.backgroundColor = Color.white;
        }

        private static void DrawSquareNodeBox(Rect rect, PlantType plant, float scale)
        {
            Magnetar_Default.NEFNodeStyle.fontSize = Mathf.Max(1, (int)(8f * scale));
            string displayName = NEFData.GetPlantName(plant);
            GUI.Box(rect, displayName, Magnetar_Default.NEFNodeStyle);

            Texture2D plantTex = Utils.TextureLoader.GetPlantTexture((int)plant);
            if (plantTex != null)
            {
                if (!cachedImageStyles.TryGetValue(plantTex, out GUIStyle imgStyle))
                {
                    imgStyle = new GUIStyle { normal = { background = plantTex } };
                    RectOffset offset = new RectOffset();
                    imgStyle.border = offset; imgStyle.margin = offset; imgStyle.padding = offset;
                    cachedImageStyles[plantTex] = imgStyle;
                }

                float pad = 10f * scale;
                float bottomTextSpace = 25f * scale;

                float availWidth = rect.width - (pad * 2f);
                float availHeight = rect.height - pad - bottomTextSpace;

                float texAspect = (float)plantTex.width / (float)Mathf.Max(1, plantTex.height);
                float availAspect = availWidth / availHeight;

                float drawWidth = availWidth;
                float drawHeight = availHeight;

                if (texAspect > availAspect) drawHeight = availWidth / texAspect;
                else drawWidth = availHeight * texAspect;

                float centerX = rect.x + pad + (availWidth / 2f);
                float centerY = rect.y + pad + (availHeight / 2f);

                Rect imageRect = new Rect(centerX - (drawWidth / 2f), centerY - (drawHeight / 2f), drawWidth, drawHeight);
                GUI.Box(imageRect, "", imgStyle);
            }
        }
    }
}