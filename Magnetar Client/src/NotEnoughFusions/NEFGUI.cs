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
                    GUI.Label(new Rect(pyramidBoxRect.x + 10f, pyramidBoxRect.y + 10f, 400f, NEFManager.elementHeight),
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

            GUI.Label(new Rect(rx, ry, rightPanelWidth, NEFManager.elementHeight), Translator.Translate("Search:"));
            string newQuery = UI.WindowDrawing.DrawSetting.DrawManualTextField(
                new Rect(rx + 65f, ry, rightPanelWidth - 65f, NEFManager.elementHeight), 
                searchQuery, Translator.Translate("Search..."));

            if (newQuery != searchQuery)
            {
                searchQuery = newQuery;
                currentScrollY = 0f;
                NEFData.PerformSearch();
            }

            ry += NEFManager.elementHeight + 5f;

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

            ry += NEFManager.elementHeight + 15f;

            GUI.Label(new Rect(rx, ry, rightPanelWidth, NEFManager.elementHeight),
                Translator.Translate($"Results") + " ("+NEFData.searchResults.Count+ ") "+
                Translator.Translate("| L-Click: Recipe | R-Click: Usages"));
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
                GUI.Label(new Rect(5, 5, rightPanelWidth, 30), Translator.Translate("Loading data..."));
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

                    RecipeEntity entity = NEFData.searchResults[i];
                    Rect plantBtnRect = new Rect(btnX, btnY, itemSize, itemSize);

                    if (plantBtnRect.Contains(e.mousePosition) && e.type == EventType.MouseUp)
                    {
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
                    }

                    DrawSquareNodeBox(plantBtnRect, entity, 1.5f);
                    GUI.backgroundColor = Color.white;
                }
            }
            GUI.EndGroup();
        }

        private static void DrawUsagesView(Rect viewRect, Event e)
        {
            GUI.Label(new Rect(viewRect.x + 10f, viewRect.y + 10f, viewRect.width - 150f, 30f), 
                Translator.Translate("Fusions requiring") + ": " + 
                NEFData.GetEntityName(NEFData.usageViewTarget) + " (" + 
                NEFData.currentUsages.Count + " " +
                Translator.Translate("found)")
                );

            Rect backBtnRect = new Rect(viewRect.x + viewRect.width - 110f, viewRect.y + 10f, 100f, 30f);
            if (backBtnRect.Contains(e.mousePosition) && e.type == EventType.MouseDown && e.button == 0)
            {
                showUsagesView = false;
                e.Use();
            }

            GUI.Box(backBtnRect, Translator.Translate("Back to Tree"), Magnetar_Default.ModuleOff);
            GUI.backgroundColor = Color.white;

            if (NEFData.currentUsages.Count == 0)
            {
                GUI.Label(new Rect(viewRect.x + 10f, viewRect.y + 50f, 400f, 30f), Translator.Translate("This entity is not used as an ingredient in any fusion."));
                return;
            }

            Rect scrollAreaRect = new Rect(viewRect.x + 10f, viewRect.y + 50f, viewRect.width - 20f, viewRect.height - 60f);

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

                RecipeEntity resultEntity = NEFData.currentUsages[i].Result;
                Rect plantBtnRect = new Rect(btnX, btnY, itemSize, itemSize);

                if (plantBtnRect.Contains(e.mousePosition) && e.type == EventType.MouseUp)
                {
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
                }

                DrawSquareNodeBox(plantBtnRect, resultEntity, 1f);
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

            // Draw Edge Message (Centered above the node)
            if (!string.IsNullOrEmpty(node.EdgeMessage))
            {
                Color oldColor = GUI.contentColor;
                GUI.contentColor = node.EdgeMessageColor;
                GUIStyle msgStyle = new GUIStyle() { alignment = TextAnchor.LowerCenter, fontSize = Mathf.Max(1, (int)(16 * pyramidZoom)) };

                Rect msgRect = new Rect(pos.x - (100f * pyramidZoom), pos.y - (30f * pyramidZoom), 200f * pyramidZoom, 30f * pyramidZoom);
                GUI.Label(msgRect, node.EdgeMessage, msgStyle);
                GUI.contentColor = oldColor;
            }

            if (nodeRect.Contains(e.mousePosition) && e.type == EventType.MouseUp)
            {
                if (e.button == 0) NEFData.GeneratePyramid(node.Entity);
                else if (e.button == 1) NEFData.GenerateUsagesView(node.Entity);
                e.Use();
            }

            DrawSquareNodeBox(nodeRect, node.Entity, pyramidZoom);
            GUI.backgroundColor = Color.white;
        }

        private static void DrawSquareNodeBox(Rect rect, RecipeEntity entity, float scale)
        {
            Magnetar_Default.NEFNodeStyle.fontSize = Mathf.Max(1, (int)(8f * scale));
            string displayName = NEFData.GetEntityName(entity);
            GUI.Box(rect, displayName, Magnetar_Default.NEFNodeStyle);

            GUIStyle imgStyle = GetEntityStyle(entity);

            if (imgStyle != null && imgStyle.normal.background != null)
            {
                Texture2D tex = imgStyle.normal.background;

                float pad = 10f * scale;
                float bottomTextSpace = 25f * scale;

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