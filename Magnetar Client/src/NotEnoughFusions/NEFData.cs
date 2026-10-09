using static Magnetar_Client.Utils.Magnetar_Logger;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Magnetar_Client.NEF.Data.NEFRecipes;
using Magnetar_Client.UI;
using Magnetar_Client.Api;
using Newtonsoft.Json;
using System.IO;

using System.Text.RegularExpressions;

#if MELONLOADER || RELEASE_MELON
using Il2Cpp;
#endif

namespace Magnetar_Client.NEF;

public static class NEFData
{
    // API CONFIGURATION
    public static HashSet<int> BannedPlants = new();
    public static HashSet<int> SearchHiddenPlants = new();
    public static Dictionary<int, string> CustomNames = new();
    public static List<CustomRecipe> AddedRecipes = new();

    public static int NextCustomPlantId = 30000;

    // Internal Data State
    public static bool hasSyncedCustomizeLib;

    public static List<RecipeEntity> searchResults = new();
    public static List<RecipeNode> currentPyramidRoots = new();
    public static RecipeEntity usageViewTarget;
    public static List<CustomRecipe> currentUsages = new();

    public static HashSet<int> LegacyLoadEntities = new();

    public static float lastCalculatedScale = 1.0f;

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
        foreach (var Entry in TranslateNEFEnum(typeof(PlantType)))
        {
            CustomNames[Entry.Key] = Entry.Value;
        }
        
    }

    public static void SyncCustomizeLib()
    {
        try
        {
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
                    if (CustomNames.ContainsKey(id)) continue;

                    string defaultRawName = Enum.IsDefined(typeof(PlantType), id)
                        ? ((PlantType)id).ToString()
                        : $"Custom Plant #{id}";

                    // 1. Check NEF's translation domain scoped to PlantType enum or general NEF strings
                    string translated = NEFGUI.Domain.Translate(defaultRawName, new[] { "Enums/PlantType.json", "nef.json" });

                    // 2. If untranslated and raw name was just a number or default, assign fallback label
                    if (string.Equals(translated, defaultRawName, StringComparison.Ordinal))
                    {
                        CustomNames[id] = defaultRawName;
                    }
                    else
                    {
                        CustomNames[id] = translated;
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

    private static readonly Dictionary<Type, Dictionary<int, string>> _nefEnumCache = new();

    public static void OnLanguageChanged()
    {
        _nefEnumCache.Clear();

        CustomNames ??= new Dictionary<int, string>();

        // 1. Translate PlantType enums scoped to the NEF domain
        foreach (var entry in TranslateNEFEnum(typeof(PlantType)))
        {
            CustomNames[entry.Key] = entry.Value;
        }

        // 2. Translate ZombieType enums scoped to the NEF domain (if used in recipes/usages)
        foreach (var entry in TranslateNEFEnum(typeof(ZombieType)))
        {
            CustomNames[entry.Key] = entry.Value;
        }

        // 3. Refresh active tree layouts and search results with new localized strings
        if (currentPyramidRoots != null && currentPyramidRoots.Count > 0)
        {
            RelayoutCurrentTrees();
        }

        PerformSearch();
    }

    #region NEF Enum Translation Helper

    /// <summary>
    /// Translates an enum type stored under Translations/{Language}/NEF/Enums/{EnumType}.json
    /// </summary>
    public static Dictionary<int, string> TranslateNEFEnum(Type enumType)
    {
        if (enumType == null) return new Dictionary<int, string>();

        if (_nefEnumCache.TryGetValue(enumType, out var cached))
            return new Dictionary<int, string>(cached);

        string currentLang = Config.Language ?? "English";
        string domainName = NEFGUI.Domain.DomainName;

        string englishDir = Path.Combine(PathsManager.TranslationRootDir, "English", domainName, "Enums");
        string targetDir = Path.Combine(PathsManager.TranslationRootDir, currentLang, domainName, "Enums");

        if (!Directory.Exists(englishDir)) Directory.CreateDirectory(englishDir);
        if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);

        string englishFile = Path.Combine(englishDir, $"{enumType.Name}.json");
        string targetFile = Path.Combine(targetDir, $"{enumType.Name}.json");

        // 1. Generate/verify English baseline template
        Dictionary<int, string> englishDict = new();
        if (File.Exists(englishFile))
        {
            try { englishDict = JsonConvert.DeserializeObject<Dictionary<int, string>>(File.ReadAllText(englishFile)) ?? new(); } catch { }
        }

        bool dirtyEnglish = false;
        Array values = Enum.GetValues(enumType);
        foreach (object val in values)
        {
            int intVal = (int)val;
            if (!englishDict.ContainsKey(intVal))
            {
                englishDict[intVal] = Enum.GetName(enumType, intVal) ?? intVal.ToString();
                dirtyEnglish = true;
            }
        }

        if (dirtyEnglish || !File.Exists(englishFile))
        {
            var sorted = englishDict.OrderBy(k => k.Key).ToDictionary(k => k.Key, k => k.Value);
            File.WriteAllText(englishFile, JsonConvert.SerializeObject(sorted, Formatting.Indented));
        }

        // 2. Sync target language JSON if running non-English
        if (!string.Equals(currentLang, "English", StringComparison.OrdinalIgnoreCase))
        {
            SyncEnumJson(englishFile, targetFile);
        }

        // 3. Load active translations
        Dictionary<int, string> result = new();
        string activeFile = File.Exists(targetFile) ? targetFile : englishFile;

        if (File.Exists(activeFile))
        {
            try
            {
                var raw = JsonConvert.DeserializeObject<Dictionary<int, string>>(File.ReadAllText(activeFile));
                if (raw != null)
                {
                    foreach (var kvp in raw)
                    {
                        result[kvp.Key] = Regex.Replace(kvp.Value ?? "", "<.*?>", string.Empty);
                    }
                }
            }
            catch (Exception ex)
            {
                TranslatorLogger.Error($"[NEFData] Failed loading enum '{enumType.Name}': {ex.Message}");
            }
        }

        _nefEnumCache[enumType] = result;
        return new Dictionary<int, string>(result);
    }

    private static void SyncEnumJson(string engPath, string targetPath)
    {
        if (!File.Exists(engPath)) return;

        var engEnum = JsonConvert.DeserializeObject<Dictionary<int, string>>(File.ReadAllText(engPath)) ?? new();
        var targetEnum = new Dictionary<int, string>();

        if (File.Exists(targetPath))
        {
            try { targetEnum = JsonConvert.DeserializeObject<Dictionary<int, string>>(File.ReadAllText(targetPath)) ?? new(); } catch { }
        }

        bool dirty = false;
        foreach (var kvp in engEnum)
        {
            if (!targetEnum.ContainsKey(kvp.Key) || string.IsNullOrEmpty(targetEnum[kvp.Key]))
            {
                targetEnum[kvp.Key] = kvp.Value;
                dirty = true;
            }
        }

        if (dirty || !File.Exists(targetPath))
        {
            var sorted = targetEnum.OrderBy(k => k.Key).ToDictionary(k => k.Key, k => k.Value);
            File.WriteAllText(targetPath, JsonConvert.SerializeObject(sorted, Formatting.Indented));
        }
    }

    #endregion

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
        if (ent.IsZombie)
        {
            return ((ZombieType)ent.Id).ToString();
        }
        else
        {
            return ((PlantType)ent.Id).ToString();
        }

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
        HashSet<int> seenIds = new();

        foreach (PlantType pt in Enum.GetValues(typeof(PlantType)))
        {
            int id = (int)pt;
            if (BannedPlants.Contains(id) || SearchHiddenPlants.Contains(id)) continue;

            if (!seenIds.Add(id)) continue;

            RecipeEntity ent = RecipeEntity.Plant(pt);
            if (string.IsNullOrEmpty(query) || GetEntityName(ent).ToLower().Contains(query))
            {
                searchResults.Add(ent);
            }
        }

        foreach (var kvp in CustomNames)
        {
            if (seenIds.Contains(kvp.Key)) continue;

            if (!Enum.IsDefined(typeof(PlantType), kvp.Key) || kvp.Key >= 3000)
            {
                if (BannedPlants.Contains(kvp.Key) || SearchHiddenPlants.Contains(kvp.Key)) continue;

                seenIds.Add(kvp.Key);

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
        List<CustomRecipe> recipes = new();
        HashSet<string> seenKeys = new();

        if (!target.IsZombie && PlantMixTreeManager.ChildToParents != null)
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

        foreach (var custom in AddedRecipes)
        {
            if (custom.Result.Equals(target))
            {
                if (BannedPlants.Contains(custom.ParentA.Id) ||
                    (!custom.ParentB.IsNothing && BannedPlants.Contains(custom.ParentB.Id)) ||
                    (custom.IsTriple && BannedPlants.Contains(custom.ParentC.Id))) continue;

                string aString = $"{custom.ParentA.Id}_{(custom.ParentA.IsZombie ? "Z" : "P")}";
                string bString = $"{custom.ParentB.Id}_{(custom.ParentB.IsZombie ? "Z" : "P")}";
                string cString = custom.IsTriple ? $"{custom.ParentC.Id}_{(custom.ParentC.IsZombie ? "Z" : "P")}" : "NONE";

                string sortedParents = string.Compare(aString, bString) < 0
                    ? $"{aString}_{bString}"
                    : $"{bString}_{aString}";

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

    public static void RelayoutCurrentTrees()
    {
        if (currentPyramidRoots == null || currentPyramidRoots.Count == 0) return;

        // Spacing scales dynamically with GUI Scale to prevent overlap
        float spacingX = Config.S(160f);
        float spacingY = Config.S(160f);
        float treeGap = Config.S(220f);

        float currentStartX = 0f;
        foreach (var root in currentPyramidRoots)
        {
            currentStartX = CalculateTreeLayout(root, currentStartX, 0f, spacingX, spacingY) + treeGap;
        }
        lastCalculatedScale = Config.GUIScale;
    }

    public static void GeneratePyramid(RecipeEntity target)
    {
        currentPyramidRoots.Clear();
        var recipes = GetRecipesForPlant(target);

        if (recipes.Count > 0)
        {
            foreach (var recipe in recipes)
            {
                RecipeNode root = new() { Entity = target };
                HashSet<RecipeEntity> tracker = new() { target };

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

        RelayoutCurrentTrees();

        NEFGUI.pyramidPan = Vector2.zero;
        NEFGUI.pyramidZoom = 1.0f;
    }

    private static RecipeNode BuildRecipeTree(RecipeEntity target, HashSet<RecipeEntity> visitedAncestors, RecipeEntity rootPlant)
    {
        RecipeNode node = new() { Entity = target };
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

        HashSet<RecipeEntity> uniqueResults = new();

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