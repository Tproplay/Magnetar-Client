using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Magnetar_Client.Api;
using Magnetar_Client.NEF;
using Magnetar_Client.UI.Setting;
using Magnetar_Client.Utils;

#if MELONLOADER || RELEASE_MELON
using Il2Cpp;
using Il2CppPlaceRule;
#elif BEPINEX || RELEASE_BEPINEX
using PlaceRule;
#endif

namespace Magnetar_Client.Modules;

public class CustomRecipes : Module
{
    // Mod Info
    public override string Name { get; set; } = "Custom Recipes";
    public override string Description { get; set; } = "Allows you to add custom recipes (also visible in NEF).";
    public override string SearchHints { get; set; } = "customrecipes recipemaker recipeeditor customcrafting recipeconfig " +
        "fusionrecipes recipebook nefrecipes recipecustomizer customfusion recipeadd recipemanager craftingsystem recipeoverride" +
        " recipelist customcraft recipebuilder recipetags recipegenerator recipecheat";

    public override ModuleCategory Category { get; set; } = ModuleCategory.Plant;

    // Mod Data
    public static CustomRecipes instance;

    private const string KeyType = "Recipe Type";
    private const string KeySlotA = "Slot A";
    private const string KeySlotB = "Slot B";
    private const string KeySlotC = "Slot C";
    private const string KeyResult = "Result";
    private const string KeyBidirectional = "Bidirectional";

    private enum RecipeMode
    {
        Standard = 0,
        MixBomb = 1,
        PineFurnace = 2
    }

    private sealed class SectionControls
    {
        public readonly SelectSetting TypeSelect;
        public readonly EntitySelectSetting SlotA;
        public readonly EntitySelectSetting SlotB;
        public readonly EntitySelectSetting SlotC;
        public readonly EntitySelectSetting Result;
        public readonly BoolSetting Bidirectional;

        public SectionControls(
            SelectSetting typeSelect,
            EntitySelectSetting slotA,
            EntitySelectSetting slotB,
            EntitySelectSetting slotC,
            EntitySelectSetting result,
            BoolSetting bidirectional)
        {
            TypeSelect = typeSelect;
            SlotA = slotA;
            SlotB = slotB;
            SlotC = slotC;
            Result = result;
            Bidirectional = bidirectional;
        }

        public RecipeMode Mode => Enum.IsDefined(typeof(RecipeMode), TypeSelect.Value)
            ? (RecipeMode)TypeSelect.Value
            : RecipeMode.Standard;
    }

    private readonly struct MixBombRuleEntry
    {
        public readonly PlantType Left;
        public readonly PlantType Middle;
        public readonly PlantType Right;
        public readonly PlantType Result;

        public MixBombRuleEntry(PlantType left, PlantType middle, PlantType right, PlantType result)
        {
            Left = left;
            Middle = middle;
            Right = right;
            Result = result;
        }
    }

    public readonly struct DictionaryBackup<TValue>
    {
        public readonly PlantType Key;
        public readonly bool HadOriginal;
        public readonly TValue OriginalValue;

        public DictionaryBackup(PlantType key, bool hadOriginal, TValue originalValue)
        {
            Key = key;
            HadOriginal = hadOriginal;
            OriginalValue = originalValue;
        }
    }

    public SectionSetting RecipeSections;

    private readonly ConditionalWeakTable<Setting, SectionControls> _controlsByType = new();
    private readonly Dictionary<(PlantType First, PlantType Second), PlantType> _standardLookup = new();
    private readonly Dictionary<PlantType, List<MixBombRuleEntry>> _mixBombByMiddle = new();
    private readonly Dictionary<PlantType, PlantType> _pineFurnaceRecipes = new();
    private readonly List<NEF.Data.NEFRecipes.CustomRecipe> _registeredNefRecipes = new();

    public CustomRecipes()
    {
        instance = this;

        RegisterTranslations(Translator.CreateDictionary(new[]
        {
            // Recipe type dropdown options
            "Standard (2-Plant)",
            "MixBomb (3-Plant)",
            "PineFurnace Absorption",

            // Dynamic section setting labels
            "Plant A",
            "Base Plant",
            "Plant B",
            "Used Plant",
            "Left Plant",
            "Middle Plant",
            "Right Plant",
            "Ingredient Plant",
            "Result Plant",
            "Result Card",
            KeyBidirectional,

            // Section header tags
            "2P",
            "2P Ordered",
            "MixBomb",
            "Furnace",
            "Recipe"
        }));

        CreateCategory("Recipes");

        RecipeSections = new SectionSetting("Custom Recipes", _ => CreateRecipeSectionTemplate(), 0)
        {
            CustomSaveKey = "CustomRecipes",
            TranslateTitles = false,
            TitleFormatter = index => FormatSectionTitle(RecipeSections, index),
            OnSectionsChanged = OnRecipesChanged
        };
        AddSettings(RecipeSections);

        EndCategory();

        Actions.OnWarmUp += () =>
        {
            if (Active)
            {
                SyncNefRecipes();
            }
        };
    }

    private Dictionary<int, string> BuildTypeCustomNames()
    {
        return new Dictionary<int, string>
        {
            { (int)RecipeMode.Standard, Translate("Standard (2-Plant)") },
            { (int)RecipeMode.MixBomb, Translate("MixBomb (3-Plant)") },
            { (int)RecipeMode.PineFurnace, Translate("PineFurnace Absorption") }
        };
    }

    private List<Setting> CreateRecipeSectionTemplate()
    {
        var typeOptions = new Dictionary<int, string>
        {
            { (int)RecipeMode.Standard, "Standard (2-Plant)" },
            { (int)RecipeMode.MixBomb, "MixBomb (3-Plant)" },
            { (int)RecipeMode.PineFurnace, "PineFurnace Absorption" }
        };

        var typeSelect = new SelectSetting(KeyType, typeOptions, (int)RecipeMode.Standard)
        {
            CustomSaveKey = "Type",
            CustomNames = BuildTypeCustomNames(),
            OnSelectionChanged = _ => OnRecipesChanged()
        };

        var plantNames = TranslateEnum(typeof(PlantType));
        var slotA = CreatePlantSelector(KeySlotA, "PlantA", plantNames);
        slotA.CustomName = Translate("Plant A");
        var slotB = CreatePlantSelector(KeySlotB, "PlantB", plantNames);
        slotB.CustomName = Translate("Plant B");
        var slotC = CreatePlantSelector(KeySlotC, "PlantC", plantNames);
        slotC.CustomName = Translate("Right Plant");
        var result = CreatePlantSelector(KeyResult, "Result", plantNames);
        result.CustomName = Translate("Result Plant");

        var bidirectional = new BoolSetting(KeyBidirectional, true)
        {
            CustomSaveKey = "Bidirectional",
            SaveTranslation = false,
            CustomName = Translate(KeyBidirectional),
            OnValueChanged = _ => OnRecipesChanged()
        };

        var ctrls = new SectionControls(typeSelect, slotA, slotB, slotC, result, bidirectional);
        _controlsByType.AddOrUpdate(typeSelect, ctrls);

        return new List<Setting>
        {
            typeSelect,
            slotA,
            slotB,
            slotC,
            result,
            bidirectional
        };
    }

    private EntitySelectSetting CreatePlantSelector(string name, string saveKey, Dictionary<int, string> plantNames)
    {
        return new EntitySelectSetting(name, EntitySelectSetting.Plant)
        {
            CustomSaveKey = saveKey,
            SaveTranslation = false,
            MaxSelection = 1,
            CloseOnSingleSelect = true,
            Blacklist = Banned.PlantTypeBanned,
            CustomNames = plantNames,
            OnSelectionChanged = (_, _) => OnRecipesChanged()
        };
    }

    public override void OnSettingOpened()
    {
        RefreshAllLocalizedNames();
        SyncAllSections();
    }

    public override void OnLanguageChanged()
    {
        RefreshAllLocalizedNames();
        SyncAllSections();
    }

    public override void OnEnable()
    {
        RebuildAndSync();
    }

    public override void OnDisable()
    {
        _standardLookup.Clear();
        _mixBombByMiddle.Clear();
        _pineFurnaceRecipes.Clear();
        ClearNefRecipes();
    }

    private void OnRecipesChanged()
    {
        SyncAllSections();
        if (Active)
        {
            RebuildAndSync();
        }
    }

    private void RebuildAndSync()
    {
        RebuildRecipeLookups();
        SyncNefRecipes();
    }

    private SectionControls ResolveSectionControls(SectionInstance section)
    {
        if (section == null) return null;
        if (section.Tag is SectionControls cached) return cached;

        var typeSetting = section.Find(KeyType);
        if (typeSetting != null && _controlsByType.TryGetValue(typeSetting, out var ctrls))
        {
            section.Tag = ctrls;
            return ctrls;
        }
        return null;
    }

    private void SyncAllSections()
    {
        if (RecipeSections?.Sections == null) return;

        for (int i = 0; i < RecipeSections.Sections.Count; i++)
        {
            var section = RecipeSections.Sections[i];
            var ctrls = ResolveSectionControls(section);
            if (ctrls == null) continue;

            ApplySectionLayout(section, ctrls);
            section.Title = FormatSectionTitle(section, ctrls, i);
        }
    }

    private void RefreshAllLocalizedNames()
    {
        if (RecipeSections?.Sections == null) return;

        var plantNames = TranslateEnum(typeof(PlantType));
        var typeNames = BuildTypeCustomNames();
        for (int i = 0; i < RecipeSections.Sections.Count; i++)
        {
            var ctrls = ResolveSectionControls(RecipeSections.Sections[i]);
            if (ctrls == null) continue;

            ctrls.SlotA.CustomNames = plantNames;
            ctrls.SlotB.CustomNames = plantNames;
            ctrls.SlotC.CustomNames = plantNames;
            ctrls.Result.CustomNames = plantNames;
            ctrls.TypeSelect.CustomNames = typeNames;
        }
    }

    private void ApplySectionLayout(SectionInstance section, SectionControls ctrls)
    {
        section.ChildSettings.Clear();
        section.ChildSettings.Add(ctrls.TypeSelect);

        switch (ctrls.Mode)
        {
            case RecipeMode.Standard:
                bool bidirectional = ctrls.Bidirectional.Value;
                ctrls.SlotA.CustomName = Translate(bidirectional ? "Plant A" : "Base Plant");
                ctrls.SlotB.CustomName = Translate(bidirectional ? "Plant B" : "Used Plant");
                ctrls.Result.CustomName = Translate("Result Plant");
                ctrls.Bidirectional.CustomName = Translate(KeyBidirectional);

                section.ChildSettings.Add(ctrls.SlotA);
                section.ChildSettings.Add(ctrls.SlotB);
                section.ChildSettings.Add(ctrls.Result);
                section.ChildSettings.Add(ctrls.Bidirectional);
                break;

            case RecipeMode.MixBomb:
                ctrls.SlotA.CustomName = Translate("Left Plant");
                ctrls.SlotB.CustomName = Translate("Middle Plant");
                ctrls.SlotC.CustomName = Translate("Right Plant");
                ctrls.Result.CustomName = Translate("Result Card");

                section.ChildSettings.Add(ctrls.SlotA);
                section.ChildSettings.Add(ctrls.SlotB);
                section.ChildSettings.Add(ctrls.SlotC);
                section.ChildSettings.Add(ctrls.Result);
                break;

            case RecipeMode.PineFurnace:
                ctrls.SlotA.CustomName = Translate("Ingredient Plant");
                ctrls.Result.CustomName = Translate("Result Plant");

                section.ChildSettings.Add(ctrls.SlotA);
                section.ChildSettings.Add(ctrls.Result);
                break;
        }
    }

    private string FormatSectionTitle(SectionSetting group, int index)
    {
        if (group?.Sections == null || index < 0 || index >= group.Sections.Count)
            return $"{Translate("Recipe")} #{index + 1}";

        var section = group.Sections[index];
        var ctrls = ResolveSectionControls(section);
        return FormatSectionTitle(section, ctrls, index);
    }

    private string FormatSectionTitle(SectionInstance section, SectionControls ctrls, int index)
    {
        if (section == null || ctrls == null) return $"{Translate("Recipe")} #{index + 1}";

        string a = GetSlotLabel(ctrls.SlotA);
        string b = GetSlotLabel(ctrls.SlotB);
        string c = GetSlotLabel(ctrls.SlotC);
        string res = GetSlotLabel(ctrls.Result);

        return ctrls.Mode switch
        {
            RecipeMode.MixBomb => $"[{Translate("MixBomb")}] {a} + {b} + {c} -> {res}",
            RecipeMode.PineFurnace => $"[{Translate("Furnace")}] {a} -> {res}",
            _ => ctrls.Bidirectional.Value
                ? $"[{Translate("2P")}] {a} + {b} -> {res}"
                : $"[{Translate("2P Ordered")}] {a} + {b} -> {res}"
        };
    }

    private static string GetSlotLabel(EntitySelectSetting slot)
    {
        if (slot == null || slot.SelectedValues.Count == 0) return "?";
        return slot.GetDisplayName(slot.SelectedValues.First());
    }

    private static bool TryGetSelectedPlant(EntitySelectSetting slot, out PlantType plantType)
    {
        if (slot != null && slot.SelectedValues.Count > 0)
        {
            plantType = (PlantType)slot.SelectedValues.First();
            return plantType != PlantType.Nothing;
        }
        plantType = PlantType.Nothing;
        return false;
    }

    private void RebuildRecipeLookups()
    {
        _standardLookup.Clear();
        _mixBombByMiddle.Clear();
        _pineFurnaceRecipes.Clear();

        if (RecipeSections?.Sections == null) return;

        for (int i = 0; i < RecipeSections.Sections.Count; i++)
        {
            var ctrls = ResolveSectionControls(RecipeSections.Sections[i]);
            if (ctrls == null) continue;

            if (!TryGetSelectedPlant(ctrls.SlotA, out PlantType a) ||
                !TryGetSelectedPlant(ctrls.Result, out PlantType result))
            {
                continue;
            }

            switch (ctrls.Mode)
            {
                case RecipeMode.Standard:
                    if (!TryGetSelectedPlant(ctrls.SlotB, out PlantType b)) continue;
                    _standardLookup[(a, b)] = result;
                    if (ctrls.Bidirectional.Value)
                    {
                        _standardLookup[(b, a)] = result;
                    }
                    break;

                case RecipeMode.MixBomb:
                    if (!TryGetSelectedPlant(ctrls.SlotB, out PlantType middle) ||
                        !TryGetSelectedPlant(ctrls.SlotC, out PlantType right))
                    {
                        continue;
                    }

                    if (!_mixBombByMiddle.TryGetValue(middle, out var list))
                    {
                        list = new List<MixBombRuleEntry>();
                        _mixBombByMiddle[middle] = list;
                    }

                    // Keep most recent definition first for matching combinations
                    list.RemoveAll(e => e.Left == a && e.Right == right);
                    list.Insert(0, new MixBombRuleEntry(a, middle, right, result));
                    break;

                case RecipeMode.PineFurnace:
                    _pineFurnaceRecipes[a] = result;
                    break;
            }
        }
    }

    private void ClearNefRecipes()
    {
        if (_registeredNefRecipes.Count > 0)
        {
            for (int i = 0; i < _registeredNefRecipes.Count; i++)
            {
                NEFData.AddedRecipes.Remove(_registeredNefRecipes[i]);
            }
            _registeredNefRecipes.Clear();
        }
        RefreshActiveNefViews();
    }

    private void SyncNefRecipes()
    {
        for (int i = 0; i < _registeredNefRecipes.Count; i++)
        {
            NEFData.AddedRecipes.Remove(_registeredNefRecipes[i]);
        }
        _registeredNefRecipes.Clear();

        if (!Active || RecipeSections?.Sections == null)
        {
            RefreshActiveNefViews();
            return;
        }

        for (int i = 0; i < RecipeSections.Sections.Count; i++)
        {
            var ctrls = ResolveSectionControls(RecipeSections.Sections[i]);
            if (ctrls == null) continue;

            if (!TryGetSelectedPlant(ctrls.SlotA, out PlantType a) ||
                !TryGetSelectedPlant(ctrls.Result, out PlantType result))
            {
                continue;
            }

            NEF.Data.NEFRecipes.CustomRecipe customRecipe = null;

            switch (ctrls.Mode)
            {
                case RecipeMode.Standard:
                    if (!TryGetSelectedPlant(ctrls.SlotB, out PlantType b)) continue;
                    customRecipe = new NEF.Data.NEFRecipes.CustomRecipe
                    {
                        Result = NEF.Data.NEFRecipes.RecipeEntity.Plant(result),
                        ParentA = NEF.Data.NEFRecipes.RecipeEntity.Plant(a),
                        ParentB = NEF.Data.NEFRecipes.RecipeEntity.Plant(b)
                    };
                    break;

                case RecipeMode.MixBomb:
                    if (!TryGetSelectedPlant(ctrls.SlotB, out PlantType middle) ||
                        !TryGetSelectedPlant(ctrls.SlotC, out PlantType right))
                    {
                        continue;
                    }
                    customRecipe = new NEF.Data.NEFRecipes.CustomRecipe
                    {
                        Result = NEF.Data.NEFRecipes.RecipeEntity.Plant(result),
                        ParentA = NEF.Data.NEFRecipes.RecipeEntity.Plant(a),
                        ParentB = NEF.Data.NEFRecipes.RecipeEntity.Plant(middle),
                        ParentC = NEF.Data.NEFRecipes.RecipeEntity.Plant(right)
                    };
                    break;

                case RecipeMode.PineFurnace:
                    customRecipe = new NEF.Data.NEFRecipes.CustomRecipe
                    {
                        Result = NEF.Data.NEFRecipes.RecipeEntity.Plant(result),
                        ParentA = NEF.Data.NEFRecipes.RecipeEntity.Plant(PlantType.PineFurnace),
                        ParentB = NEF.Data.NEFRecipes.RecipeEntity.Plant(a)
                    };
                    break;
            }

            if (customRecipe != null)
            {
                _registeredNefRecipes.Add(customRecipe);
                NEFData.AddedRecipes.Add(customRecipe);
            }
        }

        RefreshActiveNefViews();
    }

    private static void RefreshActiveNefViews()
    {
        if (NEFData.currentPyramidRoots != null && NEFData.currentPyramidRoots.Count > 0)
        {
            NEFData.GeneratePyramid(NEFData.currentPyramidRoots[0].Entity);
        }

        if (NEFGUI.showUsagesView && NEFData.usageViewTarget.Id >= 0)
        {
            NEFData.GenerateUsagesView(NEFData.usageViewTarget);
        }
    }

    private static void RestoreDictionary<TValue>(
        Il2CppSystem.Collections.Generic.Dictionary<PlantType, TValue> dict,
        List<DictionaryBackup<TValue>> backups)
    {
        if (dict == null || backups == null) return;

        for (int i = backups.Count - 1; i >= 0; i--)
        {
            var entry = backups[i];
            if (entry.HadOriginal)
            {
                dict[entry.Key] = entry.OriginalValue;
            }
            else
            {
                dict.Remove(entry.Key);
            }
        }
    }

    private static bool TryFindBestMixBombRule(
        Plant middlePlant,
        List<MixBombRuleEntry> rules,
        out MixBombRuleEntry matchedRule,
        out bool isComplete)
    {
        Board board = Board.Instance;
        if (board != null && middlePlant != null)
        {
            for (int i = 0; i < rules.Count; i++)
            {
                var rule = rules[i];
                if (Lawnf.GetCertainPlant(middlePlant.thePlantColumn - 1, middlePlant.thePlantRow, rule.Left, board) != null &&
                    Lawnf.GetCertainPlant(middlePlant.thePlantColumn + 1, middlePlant.thePlantRow, rule.Right, board) != null)
                {
                    matchedRule = rule;
                    isComplete = true;
                    return true;
                }
            }
        }

        if (rules.Count > 0)
        {
            matchedRule = rules[0];
            isComplete = false;
            return true;
        }

        matchedRule = default;
        isComplete = false;
        return false;
    }

    // Harmony Patches
    [HarmonyPatch(typeof(MixData), nameof(MixData.TryGetMix))]
    public static class MixDataTryGetMixPatch
    {
        [ThreadStatic]
        private static bool _bypassCustomLookup;

        [HarmonyPrefix]
        public static bool Prefix(PlantType a, PlantType b, ref PlantType c, bool order, ref bool __result)
        {
            if (_bypassCustomLookup || instance == null || !instance.Active) return true;
            if (instance._standardLookup.Count == 0) return true;

            if (instance._standardLookup.TryGetValue((a, b), out PlantType exactResult))
            {
                c = exactResult;
                __result = true;
                return false;
            }

            if (!order && instance._standardLookup.TryGetValue((b, a), out PlantType reverseResult))
            {
                bool hasExactVanilla;
                _bypassCustomLookup = true;
                try
                {
                    hasExactVanilla = MixData.TryGetMix(a, b, out _, true);
                }
                finally
                {
                    _bypassCustomLookup = false;
                }

                if (!hasExactVanilla)
                {
                    c = reverseResult;
                    __result = true;
                    return false;
                }
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(MixBombRule), nameof(MixBombRule.Check))]
    public static class MixBombRuleCheckPatch
    {
        [HarmonyPostfix]
        public static void Postfix(BoardGrid grid, ref bool __result)
        {
            if (__result || instance == null || !instance.Active || grid == null) return;
            if (instance._mixBombByMiddle.Count == 0) return;

            var plants = grid.plantList;
            if (plants == null) return;

            for (int i = 0; i < plants.Count; i++)
            {
                Plant plant = plants[i];
                if (plant != null && instance._mixBombByMiddle.ContainsKey(plant.thePlantType))
                {
                    __result = true;
                    return;
                }
            }
        }
    }

    [HarmonyPatch(typeof(MixBomb), "AttributeEvent")]
    public static class MixBombAttributeEventPatch
    {
        [HarmonyPrefix]
        public static void Prefix(MixBomb __instance, out List<DictionaryBackup<Il2CppSystem.Collections.Generic.List<PlantType>>> __state)
        {
            __state = null;
            if (instance == null || !instance.Active || __instance == null) return;
            if (instance._mixBombByMiddle.Count == 0 || MixBomb.Recipe == null) return;

            var tilePlants = Lawnf.Get1x1Plants(__instance.thePlantColumn, __instance.thePlantRow);
            if (tilePlants == null || tilePlants.Count == 0) return;

            var backups = new List<DictionaryBackup<Il2CppSystem.Collections.Generic.List<PlantType>>>();
            var handledMiddleTypes = new HashSet<PlantType>();

            for (int i = 0; i < tilePlants.Count; i++)
            {
                Plant plant = tilePlants[i];
                if (plant == null) continue;

                PlantType middleType = plant.thePlantType;
                if (!handledMiddleTypes.Add(middleType)) continue;
                if (!instance._mixBombByMiddle.TryGetValue(middleType, out var rules)) continue;
                if (!TryFindBestMixBombRule(plant, rules, out var chosenRule, out bool isComplete)) continue;

                // Preserve vanilla MixBomb hint/fallback when custom trio isn't complete
                bool hadOriginal = MixBomb.Recipe.TryGetValue(middleType, out var originalList);
                if (!isComplete && hadOriginal) continue;

                backups.Add(new DictionaryBackup<Il2CppSystem.Collections.Generic.List<PlantType>>(
                    middleType,
                    hadOriginal,
                    originalList));

                var overrideList = new Il2CppSystem.Collections.Generic.List<PlantType>();
                overrideList.Add(chosenRule.Left);
                overrideList.Add(chosenRule.Right);
                overrideList.Add(chosenRule.Result);
                MixBomb.Recipe[middleType] = overrideList;
            }

            if (backups.Count > 0)
            {
                __state = backups;
            }
        }

        [HarmonyPostfix]
        public static void Postfix(List<DictionaryBackup<Il2CppSystem.Collections.Generic.List<PlantType>>> __state)
        {
            RestoreDictionary(MixBomb.Recipe, __state);
        }
    }

    [HarmonyPatch(typeof(PineFurnace), "AttributeEvent")]
    public static class PineFurnaceAttributeEventPatch
    {
        [HarmonyPrefix]
        public static void Prefix(out List<DictionaryBackup<PlantType>> __state)
        {
            __state = null;
            if (instance == null || !instance.Active) return;
            if (instance._pineFurnaceRecipes.Count == 0 || PineFurnace.mixDic == null) return;

            var backups = new List<DictionaryBackup<PlantType>>(instance._pineFurnaceRecipes.Count);
            foreach (var kvp in instance._pineFurnaceRecipes)
            {
                bool hadOriginal = PineFurnace.mixDic.TryGetValue(kvp.Key, out PlantType originalResult);
                backups.Add(new DictionaryBackup<PlantType>(kvp.Key, hadOriginal, originalResult));
                PineFurnace.mixDic[kvp.Key] = kvp.Value;
            }

            if (backups.Count > 0)
            {
                __state = backups;
            }
        }

        [HarmonyPostfix]
        public static void Postfix(List<DictionaryBackup<PlantType>> __state)
        {
            RestoreDictionary(PineFurnace.mixDic, __state);
        }
    }
}
