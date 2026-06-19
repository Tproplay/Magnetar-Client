using Magnetar_Client.Core;
using Magnetar_Client.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Magnetar_Client.NEF.Data.NEFRecipes;
#if MELONLOADER || RELEASE_MELON
using Il2Cpp;
#endif

namespace Magnetar_Client.NEF
{
    public static class NEFData
    {
        // API CONFIGURATION
        public static HashSet<int> BannedPlants = new HashSet<int>();
        public static HashSet<int> SearchHiddenPlants = new HashSet<int>();
        public static Dictionary<int, string> CustomNames = new Dictionary<int, string>();
        public static List<CustomRecipe> AddedRecipes = new List<CustomRecipe>();

        public static int NextCustomPlantId = 3000;

        // Internal Data State

        public static bool hasSyncedCustomizeLib = false;

        public static List<RecipeEntity> searchResults = new List<RecipeEntity>();
        public static List<RecipeNode> currentPyramidRoots = new List<RecipeNode>();
        public static RecipeEntity usageViewTarget;
        public static List<CustomRecipe> currentUsages = new List<CustomRecipe>();

        public static HashSet<int> LegacyLoadEntities = new HashSet<int>();

        public class RecipeNode
        {
            public RecipeEntity Entity;
            public RecipeNode ParentA;
            public RecipeNode ParentB;
            public RecipeNode ParentC;
            public float RenderX;
            public float RenderY;

            public bool IsTriple => ParentC != null;
            public bool IsSingle => ParentA != null && ParentB == null;

            public string EdgeMessage = "";
            public Color EdgeMessageColor = Color.white;
        }

        public static void Init()
        {

            LegacyLoadEntities.UnionWith(new HashSet<int>
            {
                (int)PlantType.HelmetPlant, (int)PlantType.DoomSeed,
                (int)PlantType.IceNut
            });


            // Cache native names
            foreach (PlantType pt in Enum.GetValues(typeof(PlantType)))
            {
                if (!CustomNames.ContainsKey((int)pt)) CustomNames[(int)pt] = pt.ToString();
            }
#if RELEASE_MELON || RELEASE_BEPINEX
            foreach ( var Entry in Translator.TranslateEnum(typeof(PlantType)))
            {
                CustomNames[Entry.Key] = Entry.Value;
            }
#endif
            Magnetar_Client.NEF.Data.NEFBanned.InitBan();
            Magnetar_Client.NEF.Data.NEFBanned.InitHidden();
            Magnetar_Client.NEF.Data.NEFRecipes.InitRecipes();
        }

        public static void SyncCustomizeLib()
        {
            try
            {
                // Check if CustomizeLib is installed
                var customizeLib = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == "PVZCustomization" || a.FullName.Contains("CustomizeLib"));

                if (customizeLib == null) return;

                Type customCoreType = customizeLib.GetType("CustomizeLib.BepInEx.CustomCore");
                if (customCoreType == null) return;

                Magnetar_Client.Utils.Magnetar_Logger.DebugLogger.Msg("[NEF] Scaping recipies from CustomizeLib");

                var plantNamesProp = customCoreType.GetProperty("CustomPlantNames", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                if (plantNamesProp?.GetValue(null) is System.Collections.IDictionary customPlantNames)
                {
                    foreach (System.Collections.DictionaryEntry entry in customPlantNames)
                        CustomNames[Convert.ToInt32(entry.Key)] = entry.Value.ToString();
                }

                var plantTypesProp = customCoreType.GetProperty("CustomPlantTypes", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                if (plantTypesProp?.GetValue(null) is System.Collections.IEnumerable customPlantTypes)
                {
                    foreach (var pt in customPlantTypes)
                    {
                        int id = Convert.ToInt32(pt);
                        if (!CustomNames.ContainsKey(id))
                        {
                            string translated = Utils.Translator.Translate(((PlantType)id).ToString());
                            CustomNames[id] = (translated != ((PlantType)id).ToString()) ? translated : $"Custom Plant #{id}";
                        }
                    }
                }

                var fusionsProp = customCoreType.GetProperty("CustomFusions", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                if (fusionsProp?.GetValue(null) is System.Collections.IEnumerable customFusions)
                {
                    foreach (var fusion in customFusions)
                    {
                        Type type = fusion.GetType();
                        int target = Convert.ToInt32(type.GetField("Item1").GetValue(fusion));
                        int item1 = Convert.ToInt32(type.GetField("Item2").GetValue(fusion));
                        int item2 = Convert.ToInt32(type.GetField("Item3").GetValue(fusion));

                        if (!CustomNames.ContainsKey(target)) CustomNames[target] = $"Unknown Target #{target}";
                        if (!CustomNames.ContainsKey(item1)) CustomNames[item1] = $"Unknown Ingredient #{item1}";
                        if (!CustomNames.ContainsKey(item2)) CustomNames[item2] = $"Unknown Ingredient #{item2}";

                        AddedRecipes.Add(new CustomRecipe
                        {
                            Result = RecipeEntity.Custom(target),
                            ParentA = RecipeEntity.Custom(item1),
                            ParentB = RecipeEntity.Custom(item2)
                        });
                    }
                }

                Magnetar_Client.Utils.Magnetar_Logger.DebugLogger.Msg($"[NEF] Scrape Complete! Synced {CustomNames.Count(k => k.Key >= 3000)} entities and {AddedRecipes.Count} recipes.");
            }
            catch (Exception ex)
            {
                Magnetar_Client.Utils.Magnetar_Logger.DebugLogger.Error($"[NEF] Failed to sync with CustomizeLib: {ex.Message}");
            }
        }

        public static void OnLanguageChanged()
        {
            foreach (var Entry in Translator.TranslateEnum(typeof(PlantType)))
            {
                CustomNames[Entry.Key] = Entry.Value;
            }
        }

        public static RecipeEntity RegisterCustomEntity(string displayName, string texturePath, bool legacyLoad = false)
        {
            int id = NextCustomPlantId++;
            CustomNames[id] = displayName;
            TextureLoader.PlantTextureOverrides[id] = texturePath;

            if (legacyLoad)
            {
                LegacyLoadEntities.Add(id);
            }

            return RecipeEntity.Custom(id);
        }

        public static string GetEntityName(RecipeEntity ent)
        {
            if (CustomNames.TryGetValue(ent.Id, out string customName)) return customName;
            return ent.IsZombie ? ((ZombieType)ent.Id).ToString() : ((PlantType)ent.Id).ToString();
        }

        public static void PerformSearch()
        {
            if (!hasSyncedCustomizeLib)
            {
                SyncCustomizeLib();
                hasSyncedCustomizeLib = true;
            }

            searchResults.Clear();
            if (!PlantMixTreeManager.IsInitialized) return;

            string query = NEFGUI.searchQuery.ToLower();

            foreach (PlantType pt in Enum.GetValues(typeof(PlantType)))
            {
                if (BannedPlants.Contains((int)pt) || SearchHiddenPlants.Contains((int)pt)) continue;
                RecipeEntity ent = RecipeEntity.Plant(pt);
                if (string.IsNullOrEmpty(query) || GetEntityName(ent).ToLower().Contains(query))
                {
                    searchResults.Add(ent);
                }
            }

            foreach (var kvp in CustomNames)
            {
                if (!Enum.IsDefined(typeof(PlantType), kvp.Key) || kvp.Key >= 3000)
                {
                    if (BannedPlants.Contains(kvp.Key) || SearchHiddenPlants.Contains(kvp.Key)) continue;

                    RecipeEntity customEnt = RecipeEntity.Custom(kvp.Key);
                    if (string.IsNullOrEmpty(query) || kvp.Value.ToLower().Contains(query))
                    {
                        searchResults.Add(customEnt);
                    }
                }
            }
        }

        public static List<CustomRecipe> GetRecipesForPlant(RecipeEntity target)
        {
            List<CustomRecipe> recipes = new List<CustomRecipe>();
            HashSet<string> seenKeys = new HashSet<string>();

            if (!target.IsZombie && target.Id < 3000 && PlantMixTreeManager.ChildToParents != null)
            {
                PlantType nativeTarget = (PlantType)target.Id;
                if (PlantMixTreeManager.ChildToParents.ContainsKey(nativeTarget))
                {
                    foreach (var recipe in PlantMixTreeManager.ChildToParents[nativeTarget])
                    {
                        if (BannedPlants.Contains((int)recipe.ParentA) || BannedPlants.Contains((int)recipe.ParentB)) continue;

                        string a = recipe.ParentA.ToString();
                        string b = recipe.ParentB.ToString();
                        string key = string.Compare(a, b) < 0 ? $"{a}_{b}" : $"{b}_{a}";

                        if (!seenKeys.Contains(key))
                        {
                            seenKeys.Add(key);
                            recipes.Add(new CustomRecipe
                            {
                                Result = target,
                                ParentA = RecipeEntity.Plant(recipe.ParentA),
                                ParentB = RecipeEntity.Plant(recipe.ParentB)
                            });
                        }
                    }
                }
            }

            // Add custom injected recipes
            foreach (var custom in AddedRecipes)
            {
                if (custom.Result.Equals(target))
                {
                    if (BannedPlants.Contains(custom.ParentA.Id) ||
                        (!custom.ParentB.IsNothing && BannedPlants.Contains(custom.ParentB.Id)) ||
                        (custom.IsTriple && BannedPlants.Contains(custom.ParentC.Id))) continue;

                    // 1. Create a unique string for each ingredient
                    string aString = $"{custom.ParentA.Id}_{(custom.ParentA.IsZombie ? "Z" : "P")}";
                    string bString = $"{custom.ParentB.Id}_{(custom.ParentB.IsZombie ? "Z" : "P")}";
                    string cString = custom.IsTriple ? $"{custom.ParentC.Id}_{(custom.ParentC.IsZombie ? "Z" : "P")}" : "NONE";

                    // 2. Sort Parent A and Parent B alphabetically so order doesn't matter
                    string sortedParents = string.Compare(aString, bString) < 0
                        ? $"{aString}_{bString}"
                        : $"{bString}_{aString}";

                    // 3. Generate the final deduplication key
                    string key = $"{sortedParents}_{cString}";

                    if (!seenKeys.Contains(key))
                    {
                        seenKeys.Add(key);
                        recipes.Add(custom);
                    }
                }
            }
            return recipes;
        }

        public static void GeneratePyramid(RecipeEntity target)
        {
            currentPyramidRoots.Clear();
            var recipes = GetRecipesForPlant(target);

            if (recipes.Count > 0)
            {
                foreach (var recipe in recipes)
                {
                    RecipeNode root = new RecipeNode { Entity = target };
                    HashSet<RecipeEntity> tracker = new HashSet<RecipeEntity> { target };

                    root.ParentA = BuildRecipeTree(recipe.ParentA, new HashSet<RecipeEntity>(tracker), target);

                    if (!recipe.IsSingle)
                    {
                        root.ParentB = BuildRecipeTree(recipe.ParentB, new HashSet<RecipeEntity>(tracker), target);
                    }
                    if (recipe.IsTriple)
                    {
                        root.ParentC = BuildRecipeTree(recipe.ParentC, new HashSet<RecipeEntity>(tracker), target);
                    }

                    root.EdgeMessage = recipe.EdgeMessage;
                    root.EdgeMessageColor = recipe.EdgeMessageColor;

                    currentPyramidRoots.Add(root);
                }
            }
            else
            {
                currentPyramidRoots.Add(new RecipeNode { Entity = target });
            }

            float currentStartX = 0f;
            foreach (var root in currentPyramidRoots)
            {
                currentStartX = CalculateTreeLayout(root, currentStartX, 0f, 150f, 150f) + 200f;
            }

            NEFGUI.pyramidPan = Vector2.zero;
            NEFGUI.pyramidZoom = 1.0f;
        }

        private static RecipeNode BuildRecipeTree(RecipeEntity target, HashSet<RecipeEntity> visitedAncestors, RecipeEntity rootPlant)
        {
            RecipeNode node = new RecipeNode { Entity = target };
            if (visitedAncestors.Contains(target)) return node;
            visitedAncestors.Add(target);

            var recipes = GetRecipesForPlant(target);
            if (recipes.Count > 0)
            {
                var recipe = recipes[0];
                bool hasLoop = visitedAncestors.Contains(recipe.ParentA) ||
                               (!recipe.IsSingle && visitedAncestors.Contains(recipe.ParentB)) ||
                               (recipe.IsTriple && visitedAncestors.Contains(recipe.ParentC)) ||
                               recipe.ParentA.Equals(rootPlant) ||
                               (!recipe.IsSingle && recipe.ParentB.Equals(rootPlant)) ||
                               (recipe.IsTriple && recipe.ParentC.Equals(rootPlant));

                if (!hasLoop && (!recipe.ParentA.Equals(target) && (recipe.IsSingle || !recipe.ParentB.Equals(target))))
                {
                    node.ParentA = BuildRecipeTree(recipe.ParentA, new HashSet<RecipeEntity>(visitedAncestors), rootPlant);

                    if (!recipe.IsSingle)
                    {
                        node.ParentB = BuildRecipeTree(recipe.ParentB, new HashSet<RecipeEntity>(visitedAncestors), rootPlant);
                    }
                    if (recipe.IsTriple)
                    {
                        node.ParentC = BuildRecipeTree(recipe.ParentC, new HashSet<RecipeEntity>(visitedAncestors), rootPlant);
                    }

                    // Assign edge messages upward
                    node.EdgeMessage = recipe.EdgeMessage;
                    node.EdgeMessageColor = recipe.EdgeMessageColor;
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

            if (node.IsSingle)
            {
                float nextX = CalculateTreeLayout(node.ParentA, startX, currentY + ySpacing, xSpacing, ySpacing);
                node.RenderX = node.ParentA.RenderX;
                return nextX;
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

        public static void GenerateUsagesView(RecipeEntity target)
        {
            usageViewTarget = target;
            currentUsages.Clear();
            NEFGUI.usageScrollY = 0f;
            NEFGUI.showUsagesView = true;

            HashSet<RecipeEntity> uniqueResults = new HashSet<RecipeEntity>();

            if (!target.IsZombie && target.Id < 3000 && PlantMixTreeManager.ChildToParents != null)
            {
                PlantType ptTarget = (PlantType)target.Id;
                foreach (var kvp in PlantMixTreeManager.ChildToParents)
                {
                    foreach (var recipe in kvp.Value)
                    {
                        if (recipe.ParentA == ptTarget || recipe.ParentB == ptTarget)
                        {
                            RecipeEntity resultEnt = RecipeEntity.Plant(kvp.Key);
                            if (uniqueResults.Add(resultEnt))
                            {
                                currentUsages.Add(new CustomRecipe
                                {
                                    Result = resultEnt,
                                    ParentA = RecipeEntity.Plant(recipe.ParentA),
                                    ParentB = RecipeEntity.Plant(recipe.ParentB)
                                });
                            }
                        }
                    }
                }
            }

            foreach (var custom in AddedRecipes)
            {
                if (custom.ParentA.Equals(target) || custom.ParentB.Equals(target) || (custom.IsTriple && custom.ParentC.Equals(target)))
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