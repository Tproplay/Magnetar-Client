using Il2Cpp;
using UnityEngine;
using System;
using System.Collections.Generic;
using Magnetar_Client.UI.Themes;
using static Magnetar_Client.NEF.Data.NEFRecipes;
using Magnetar_Client.Utils;

namespace Magnetar_Client.Core
{
    public static class NEFManager
    {
        
        public static bool ShowMenu = false;

        public static float elementHeight = 25f;
        public static Rect windowRect = new Rect(60, 60, 1000, 700);

        // ==========================================
        // API CONFIGURATION
        // ==========================================
        public static HashSet<int> BannedPlants = new HashSet<int>();
        public static HashSet<int> SearchHiddenPlants = new HashSet<int>();
        public static Dictionary<int, string> CustomNames = new Dictionary<int, string>();
        public static List<CustomRecipe> AddedRecipes = new List<CustomRecipe>();

        

        #region Internal State
        private static string searchQuery = "";
        private static float currentScrollY = 0f;

        private static Vector2 pyramidPan = Vector2.zero;
        private static float pyramidZoom = 1f;
        private static bool isDraggingPyramid = false;

        private static Dictionary<Texture2D, GUIStyle> cachedImageStyles = new Dictionary<Texture2D, GUIStyle>();

        private static List<PlantType> searchResults = new List<PlantType>();
        private static List<RecipeNode> currentPyramidRoots = new List<RecipeNode>();

        private static bool showUsagesView = false;
        private static PlantType usageViewTarget;
        private static List<CustomRecipe> currentUsages = new List<CustomRecipe>();
        private static float usageScrollY = 0f;
        #endregion

        public class RecipeNode
        {
            public PlantType Plant;
            public RecipeNode ParentA;
            public RecipeNode ParentB;
            public RecipeNode ParentC; // Added third parent support
            public float RenderX;
            public float RenderY;
            public bool IsTriple => ParentC != null;
        }

        public static void Init()
        {
            CustomNames = Translator.TranslateEnum(typeof(PlantType));
            NEF.Data.NEFBanned.InitBan();
            NEF.Data.NEFRecipes.InitRecipes();
            PerformSearch();
        }

        public static void Render()
        {
            windowRect.x = 60f;
            windowRect.y = 60f;
            windowRect.width = Config.WindowWidth - 120f;
            windowRect.height = Config.WindowHeight - 120f;

            windowRect = GUI.Window(
                2002,
                windowRect,
                (GUI.WindowFunction)DrawNEFWindow,
                "Not Enough Fusions",
                Magnetar_Default.ModuleWindow
            );
        }

        private static void DrawNEFWindow(int windowID)
        {
            Event e = Event.current;

            float rightPanelWidth = windowRect.width * 0.35f;
            float topIndent = 35f;
            float leftPanelWidth = windowRect.width - rightPanelWidth - 30f;
            float contentHeight = windowRect.height - topIndent - 10f;

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
                if (currentPyramidRoots.Count == 0)
                {
                    GUI.Label(new Rect(pyramidBoxRect.x + 10f, pyramidBoxRect.y + 10f, 400f, elementHeight), "Select a plant to view its recipes.");
                }
                else
                {
                    GUI.BeginGroup(pyramidBoxRect);

                    float minX = currentPyramidRoots[0].RenderX;
                    float maxX = currentPyramidRoots[currentPyramidRoots.Count - 1].RenderX;
                    float centerOfAllTrees = (minX + maxX) / 2f;

                    for (int i = 0; i < currentPyramidRoots.Count; i++)
                    {
                        DrawTree(currentPyramidRoots[i], pyramidBoxRect, centerOfAllTrees, e);
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

            GUI.Label(new Rect(rx, ry, rightPanelWidth, elementHeight), "Search:");
            string newQuery = UI.WindowDrawing.DrawSetting.DrawManualTextField(new Rect(rx + 65f, ry, rightPanelWidth - 65f, elementHeight), searchQuery, "Search...");

            if (newQuery != searchQuery)
            {
                searchQuery = newQuery;
                currentScrollY = 0f;
                PerformSearch();
            }

            ry += elementHeight + 5f;

            Rect clearBtnRect = new Rect(rx, ry, rightPanelWidth, elementHeight);
            bool clearHover = clearBtnRect.Contains(e.mousePosition);

            GUI.Box(clearBtnRect, "Clear Search", Magnetar_Default.ModuleOff);
            GUI.backgroundColor = Color.white;

            if (clearHover && e.type == EventType.MouseDown && e.button == 0)
            {
                searchQuery = "";
                currentScrollY = 0f;
                PerformSearch();
                e.Use();
            }

            ry += elementHeight + 15f;

            GUI.Label(new Rect(rx, ry, rightPanelWidth, elementHeight), $"Results ({searchResults.Count}) | L-Click: Recipe | R-Click: Usages");
            ry += elementHeight;

            float scrollHeight = rightPanelRect.height - (ry - rightPanelRect.y);
            Rect scrollRect = new Rect(rx, ry, rightPanelWidth, scrollHeight);

            int columns = Mathf.Max(3, Mathf.FloorToInt(rightPanelWidth / 100f));
            float padding = 5f;
            float itemSize = (rightPanelWidth - (padding * (columns - 1))) / columns;
            int rowCount = Mathf.CeilToInt((float)searchResults.Count / columns);
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
                for (int i = 0; i < searchResults.Count; i++)
                {
                    int col = i % columns;
                    int row = i / columns;

                    float btnX = col * (itemSize + padding);
                    float btnY = row * (itemSize + padding) - currentScrollY;

                    if (btnY + itemSize < 0 || btnY > scrollRect.height) continue;

                    PlantType plant = searchResults[i];
                    Rect plantBtnRect = new Rect(btnX, btnY, itemSize, itemSize);

                    if (plantBtnRect.Contains(e.mousePosition) && e.type == EventType.MouseUp)
                    {
                        if (e.button == 0)
                        {
                            showUsagesView = false;
                            GeneratePyramid(plant);
                        }
                        else if (e.button == 1)
                        {
                            GenerateUsagesView(plant);
                        }
                        e.Use();
                    }

                    DrawSquareNodeBox(plantBtnRect, plant, 1.5f);
                    GUI.backgroundColor = Color.white;
                }
            }
            GUI.EndGroup();
        }

        // --- DATA & GENERATION ENGINE ---

        public static string GetPlantName(PlantType pt)
        {
            if (CustomNames.TryGetValue((int)pt, out string customName)) return customName;
            return pt.ToString();
        }

        private static void PerformSearch()
        {
            searchResults.Clear();
            if (!PlantMixTreeManager.IsInitialized) return;

            string query = searchQuery.ToLower();
            foreach (PlantType pt in Enum.GetValues(typeof(PlantType)))
            {
                if (BannedPlants.Contains((int)pt) || SearchHiddenPlants.Contains((int)pt)) continue;
                if (string.IsNullOrEmpty(query) || GetPlantName(pt).ToLower().Contains(query))
                {
                    searchResults.Add(pt);
                }
            }
        }

        private static List<CustomRecipe> GetRecipesForPlant(PlantType target)
        {
            List<CustomRecipe> recipes = new List<CustomRecipe>();
            HashSet<string> seenKeys = new HashSet<string>();

            if (PlantMixTreeManager.ChildToParents != null && PlantMixTreeManager.ChildToParents.ContainsKey(target))
            {
                foreach (var recipe in PlantMixTreeManager.ChildToParents[target])
                {
                    if (BannedPlants.Contains((int)recipe.ParentA) || BannedPlants.Contains((int)recipe.ParentB)) continue;

                    string a = recipe.ParentA.ToString();
                    string b = recipe.ParentB.ToString();
                    string key = string.Compare(a, b) < 0 ? $"{a}_{b}" : $"{b}_{a}";

                    if (!seenKeys.Contains(key))
                    {
                        seenKeys.Add(key);
                        recipes.Add(new CustomRecipe { Result = target, ParentA = recipe.ParentA, ParentB = recipe.ParentB });
                    }
                }
            }

            foreach (var custom in AddedRecipes)
            {
                if (custom.Result == target)
                {
                    if (BannedPlants.Contains((int)custom.ParentA) || BannedPlants.Contains((int)custom.ParentB) || (custom.IsTriple && BannedPlants.Contains((int)custom.ParentC))) continue;

                    string key = $"{custom.ParentA}_{custom.ParentB}_{custom.ParentC}";
                    if (!seenKeys.Contains(key))
                    {
                        seenKeys.Add(key);
                        recipes.Add(custom);
                    }
                }
            }
            return recipes;
        }

        private static void GeneratePyramid(PlantType target)
        {
            currentPyramidRoots.Clear();
            var recipes = GetRecipesForPlant(target);

            if (recipes.Count > 0)
            {
                foreach (var recipe in recipes)
                {
                    RecipeNode root = new RecipeNode { Plant = target };
                    HashSet<PlantType> tracker = new HashSet<PlantType> { target };

                    root.ParentA = BuildRecipeTree(recipe.ParentA, new HashSet<PlantType>(tracker), target);
                    root.ParentB = BuildRecipeTree(recipe.ParentB, new HashSet<PlantType>(tracker), target);
                    if (recipe.IsTriple)
                    {
                        root.ParentC = BuildRecipeTree(recipe.ParentC, new HashSet<PlantType>(tracker), target);
                    }

                    currentPyramidRoots.Add(root);
                }
            }
            else
            {
                currentPyramidRoots.Add(new RecipeNode { Plant = target });
            }

            float currentStartX = 0f;
            foreach (var root in currentPyramidRoots)
            {
                currentStartX = CalculateTreeLayout(root, currentStartX, 0f, 150f, 150f) + 200f;
            }

            pyramidPan = Vector2.zero;
            pyramidZoom = 1.0f;
        }

        private static RecipeNode BuildRecipeTree(PlantType target, HashSet<PlantType> visitedAncestors, PlantType rootPlant)
        {
            RecipeNode node = new RecipeNode { Plant = target };
            if (visitedAncestors.Contains(target)) return node;
            visitedAncestors.Add(target);

            var recipes = GetRecipesForPlant(target);
            if (recipes.Count > 0)
            {
                var recipe = recipes[0];
                bool hasLoop = visitedAncestors.Contains(recipe.ParentA) || visitedAncestors.Contains(recipe.ParentB) ||
                               (recipe.IsTriple && visitedAncestors.Contains(recipe.ParentC)) ||
                               recipe.ParentA == rootPlant || recipe.ParentB == rootPlant || (recipe.IsTriple && recipe.ParentC == rootPlant);

                if (!hasLoop && (recipe.ParentA != target || recipe.ParentB != target))
                {
                    node.ParentA = BuildRecipeTree(recipe.ParentA, new HashSet<PlantType>(visitedAncestors), rootPlant);
                    node.ParentB = BuildRecipeTree(recipe.ParentB, new HashSet<PlantType>(visitedAncestors), rootPlant);
                    if (recipe.IsTriple)
                    {
                        node.ParentC = BuildRecipeTree(recipe.ParentC, new HashSet<PlantType>(visitedAncestors), rootPlant);
                    }
                }
            }
            return node;
        }

        private static float CalculateTreeLayout(RecipeNode node, float startX, float currentY, float xSpacing, float ySpacing)
        {
            if (node == null) return startX;
            node.RenderY = currentY;

            if (node.ParentA == null && node.ParentB == null && node.ParentC == null)
            {
                node.RenderX = startX;
                return startX + xSpacing;
            }

            if (node.IsTriple)
            {
                float nextX = CalculateTreeLayout(node.ParentA, startX, currentY + ySpacing, xSpacing, ySpacing);
                float midX = CalculateTreeLayout(node.ParentB, nextX, currentY + ySpacing, xSpacing, ySpacing);
                float finalX = CalculateTreeLayout(node.ParentC, midX, currentY + ySpacing, xSpacing, ySpacing);

                if (node.ParentB != null)
                    node.RenderX = node.ParentB.RenderX;
                else
                    node.RenderX = (node.ParentA.RenderX + node.ParentC.RenderX) / 2f;

                return finalX;
            }
            else
            {
                float nextX = CalculateTreeLayout(node.ParentA, startX, currentY + ySpacing, xSpacing, ySpacing);
                float finalX = CalculateTreeLayout(node.ParentB, nextX, currentY + ySpacing, xSpacing, ySpacing);

                node.RenderX = (node.ParentA.RenderX + node.ParentB.RenderX) / 2f;
                return finalX;
            }
        }

        // ==========================================
        // USAGES VIEW (RIGHT CLICK ENGINE)
        // ==========================================
        private static void GenerateUsagesView(PlantType target)
        {
            usageViewTarget = target;
            currentUsages.Clear();
            usageScrollY = 0f;
            showUsagesView = true;

            HashSet<string> uniqueRecipes = new HashSet<string>();

            if (PlantMixTreeManager.ChildToParents != null)
            {
                foreach (var kvp in PlantMixTreeManager.ChildToParents)
                {
                    foreach (var recipe in kvp.Value)
                    {
                        if (recipe.ParentA == target || recipe.ParentB == target)
                        {
                            string key = $"{kvp.Key}_{recipe.ParentA}_{recipe.ParentB}";
                            if (!uniqueRecipes.Contains(key))
                            {
                                uniqueRecipes.Add(key);
                                currentUsages.Add(new CustomRecipe { Result = kvp.Key, ParentA = recipe.ParentA, ParentB = recipe.ParentB });
                            }
                        }
                    }
                }
            }

            foreach (var custom in AddedRecipes)
            {
                if (custom.ParentA == target || custom.ParentB == target || (custom.IsTriple && custom.ParentC == target))
                {
                    string key = $"{custom.Result}_{custom.ParentA}_{custom.ParentB}_{custom.ParentC}";
                    if (!uniqueRecipes.Contains(key))
                    {
                        uniqueRecipes.Add(key);
                        currentUsages.Add(custom);
                    }
                }
            }
        }

        private static void DrawUsagesView(Rect viewRect, Event e)
        {
            GUI.Label(new Rect(viewRect.x + 10f, viewRect.y + 10f, viewRect.width - 150f, 30f), $"Fusions requiring: {GetPlantName(usageViewTarget)} ({currentUsages.Count} found)");

            Rect backBtnRect = new Rect(viewRect.x + viewRect.width - 110f, viewRect.y + 10f, 100f, 30f);
            if (backBtnRect.Contains(e.mousePosition) && e.type == EventType.MouseDown && e.button == 0)
            {
                showUsagesView = false;
                e.Use();
            }

            GUI.Box(backBtnRect, "Back to Tree", Magnetar_Default.ModuleOff);
            GUI.backgroundColor = Color.white;

            if (currentUsages.Count == 0)
            {
                GUI.Label(new Rect(viewRect.x + 10f, viewRect.y + 50f, 400f, 30f), "This plant is not used as an ingredient in any fusion.");
                return;
            }

            Rect scrollAreaRect = new Rect(viewRect.x + 10f, viewRect.y + 50f, viewRect.width - 20f, viewRect.height - 60f);

            // Square grid shape layout formulation
            int columns = Mathf.Max(3, Mathf.FloorToInt(scrollAreaRect.width / 115f));
            float padding = 10f;
            float itemSize = (scrollAreaRect.width - (padding * (columns - 1))) / columns;
            int rowCount = Mathf.CeilToInt((float)currentUsages.Count / columns);
            float totalContentHeight = rowCount * (itemSize + padding);
            float maxScroll = Mathf.Max(0f, totalContentHeight - scrollAreaRect.height);

            if (scrollAreaRect.Contains(e.mousePosition) && e.type == EventType.ScrollWheel)
            {
                usageScrollY += e.delta.y * 30f;
                usageScrollY = Mathf.Clamp(usageScrollY, 0f, maxScroll);
                e.Use();
            }

            GUI.BeginGroup(scrollAreaRect);
            for (int i = 0; i < currentUsages.Count; i++)
            {
                int col = i % columns;
                int row = i / columns;

                float btnX = col * (itemSize + padding);
                float btnY = row * (itemSize + padding) - usageScrollY;

                if (btnY + itemSize < 0 || btnY > scrollAreaRect.height) continue;

                PlantType resultPlant = currentUsages[i].Result;
                Rect plantBtnRect = new Rect(btnX, btnY, itemSize, itemSize);

                if (plantBtnRect.Contains(e.mousePosition) && e.type == EventType.MouseUp)
                {
                    if (e.button == 0)
                    {
                        showUsagesView = false;
                        GeneratePyramid(resultPlant);
                    }
                    else if (e.button == 1)
                    {
                        GenerateUsagesView(resultPlant);
                    }
                    e.Use();
                }

                DrawSquareNodeBox(plantBtnRect, resultPlant, 1f);
            }
            GUI.EndGroup();
        }

        // --- RENDER COMPONENT METHODS ---

        private static Vector2 GetProjectedPosition(float logicX, float logicY, Rect canvasRect, float centerOfAllTrees)
        {
            float centeredX = logicX - centerOfAllTrees;
            float screenX = (canvasRect.width / 2f) + (centeredX * pyramidZoom) + pyramidPan.x;
            float screenY = 60f + (logicY * pyramidZoom) + pyramidPan.y;
            return new Vector2(screenX, screenY);
        }

        private static void DrawTree(RecipeNode node, Rect canvasRect, float centerOfAllTrees, Event e)
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

                DrawOrthogonalLine(new Vector2(pos.x, pos.y + scaledSize), new Vector2(childPosA.x, childPosA.y));
                DrawOrthogonalLine(new Vector2(pos.x, pos.y + scaledSize), new Vector2(childPosB.x, childPosB.y));

                DrawTree(node.ParentA, canvasRect, centerOfAllTrees, e);
                DrawTree(node.ParentB, canvasRect, centerOfAllTrees, e);

                if (node.IsTriple && node.ParentC != null)
                {
                    Vector2 childPosC = GetProjectedPosition(node.ParentC.RenderX, node.ParentC.RenderY, canvasRect, centerOfAllTrees);
                    DrawOrthogonalLine(new Vector2(pos.x, pos.y + scaledSize), new Vector2(childPosC.x, childPosC.y));
                    DrawTree(node.ParentC, canvasRect, centerOfAllTrees, e);
                }
            }

            if (nodeRect.Contains(e.mousePosition) && e.type == EventType.MouseUp)
            {
                if (e.button == 0) GeneratePyramid(node.Plant);
                else if (e.button == 1) GenerateUsagesView(node.Plant);
                e.Use();
            }

            DrawSquareNodeBox(nodeRect, node.Plant, pyramidZoom);
            GUI.backgroundColor = Color.white;
        }

        private static void DrawSquareNodeBox(Rect rect, PlantType plant, float scale)
        {
            Magnetar_Default.NEFNodeStyle.fontSize = Mathf.Max(1, (int)(8f * scale));
            string displayName = GetPlantName(plant);
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

        private static void DrawOrthogonalLine(Vector2 pointA, Vector2 pointB)
        {
            Color oldColor = GUI.color;
            GUI.color = Color.white;

            float thickness = Mathf.Max(1f, 3f * pyramidZoom);
            float halfThick = thickness / 2f;
            float midY = (pointA.y + pointB.y) / 2f;

            GUI.Box(new Rect(pointA.x - halfThick, pointA.y, thickness, midY - pointA.y + halfThick), "", Magnetar_Default.NEFLineStyle);

            float minX = Mathf.Min(pointA.x, pointB.x);
            float maxX = Mathf.Max(pointA.x, pointB.x);
            GUI.Box(new Rect(minX - halfThick, midY - halfThick, (maxX - minX) + thickness, thickness), "", Magnetar_Default.NEFLineStyle);

            GUI.Box(new Rect(pointB.x - halfThick, midY - halfThick, thickness, pointB.y - midY + halfThick), "", Magnetar_Default.NEFLineStyle);

            GUI.color = oldColor;
        }
    }
}