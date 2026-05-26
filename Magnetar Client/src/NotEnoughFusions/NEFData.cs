using Il2Cpp;
using Magnetar_Client.Core;
using Magnetar_Client.Utils;
using System;
using System.Collections.Generic;
using UnityEngine;
using static Magnetar_Client.NEF.Data.NEFRecipes;

namespace Magnetar_Client.NEF
{
    public static class NEFData
    {
        // API CONFIGURATION
        public static HashSet<int> BannedPlants = new HashSet<int>();
        public static HashSet<int> SearchHiddenPlants = new HashSet<int>();
        public static Dictionary<int, string> CustomNames = new Dictionary<int, string>();
        public static List<CustomRecipe> AddedRecipes = new List<CustomRecipe>();

        // Internal Data State
        public static List<PlantType> searchResults = new List<PlantType>();
        public static List<RecipeNode> currentPyramidRoots = new List<RecipeNode>();
        public static PlantType usageViewTarget;
        public static List<CustomRecipe> currentUsages = new List<CustomRecipe>();

        public class RecipeNode
        {
            public PlantType Plant;
            public RecipeNode ParentA;
            public RecipeNode ParentB;
            public RecipeNode ParentC;
            public float RenderX;
            public float RenderY;
            public bool IsTriple => ParentC != null;
        }

        public static void Init()
        {
#if !DEBUG
            CustomNames = Translator.TranslateEnum(typeof(PlantType));
#endif
            Magnetar_Client.NEF.Data.NEFBanned.InitBan();
            Magnetar_Client.NEF.Data.NEFBanned.InitHidden();
            Magnetar_Client.NEF.Data.NEFRecipes.InitRecipes();
            PerformSearch();
        }

        public static string GetPlantName(PlantType pt)
        {
            if (CustomNames.TryGetValue((int)pt, out string customName)) return customName;
            return pt.ToString();
        }

        public static void PerformSearch()
        {
            searchResults.Clear();
            if (!PlantMixTreeManager.IsInitialized) return;

            string query = NEFGUI.searchQuery.ToLower();
            foreach (PlantType pt in Enum.GetValues(typeof(PlantType)))
            {
                if (BannedPlants.Contains((int)pt) || SearchHiddenPlants.Contains((int)pt)) continue;
                if (string.IsNullOrEmpty(query) || GetPlantName(pt).ToLower().Contains(query))
                {
                    searchResults.Add(pt);
                }
            }
        }

        public static List<CustomRecipe> GetRecipesForPlant(PlantType target)
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

        public static void GeneratePyramid(PlantType target)
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

            NEFGUI.pyramidPan = Vector2.zero;
            NEFGUI.pyramidZoom = 1.0f;
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
        // USAGES VIEW (RIGHT CLICK)
        // ==========================================
        public static void GenerateUsagesView(PlantType target)
        {
            usageViewTarget = target;
            currentUsages.Clear();
            NEFGUI.usageScrollY = 0f;
            NEFGUI.showUsagesView = true;

            HashSet<PlantType> uniqueResults = new HashSet<PlantType>();

            if (PlantMixTreeManager.ChildToParents != null)
            {
                foreach (var kvp in PlantMixTreeManager.ChildToParents)
                {
                    foreach (var recipe in kvp.Value)
                    {
                        if (recipe.ParentA == target || recipe.ParentB == target)
                        {
                            if (uniqueResults.Add(kvp.Key)) 
                            {
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
                    if (uniqueResults.Add(custom.Result))
                    {
                        currentUsages.Add(custom);
                    }
                }
            }
        }
    }
}