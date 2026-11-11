using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Magnetar_Client.UI.Setting;
using UnityEngine;

#if MELONLOADER || RELEASE_MELON
using Il2Cpp;
using Il2CppGameLevel;
#elif BEPINEX || RELEASE_BEPINEX
using GameLevel;
#endif

namespace Magnetar_Client.Modules;

public class CustomPlantData : Module
{
    // Mod Info
    public override string Name { get; set; } = "Custom Plant Data";
    public override string Description { get; set; } = "Allows you to modify base plant stats.";
    public override string SearchHints { get; set; } = "customplantdata plantdata plantdamage adjustdamage damage " +
        "attackspeed attackinterval atkspeed cooldown cd produceinterval maxhealth planthealth suncost cost " +
        "starbound staradvanture leveldata plantstats plantmodifier planteditor";

    public override ModuleCategory Category { get; set; } = ModuleCategory.Plant;

    // Mod Data
    public static CustomPlantData instance;

    private const string KeyPlant = "Plant";
    private const string KeyDamage = "Base Damage";
    private const string KeyAttackInterval = "Attack Interval";
    private const string KeyProduceInterval = "Produce Interval";
    private const string KeyMaxHealth = "Max Health";
    private const string KeyCooldown = "Cooldown";
    private const string KeyCost = "Sun Cost";

    public BoolSetting OverrideLevelData;
    public SectionSetting NormalSections;
    public SectionSetting StarboundSections;

    private readonly Dictionary<PlantType, PlantOverrideData> _normalOverrides = new();
    private readonly Dictionary<PlantType, PlantOverrideData> _starboundOverrides = new();
    private readonly Dictionary<(bool IsStarbound, PlantType Type), PlantDataManager.PlantData> _cachedPatchedData = new();
    private readonly Dictionary<Plant, OriginalPlantStats> _originalPlants = new();
    private readonly Dictionary<CardUI, OriginalCardStats> _originalCards = new();

    public readonly struct PlantOverrideData
    {
        public readonly int AttackDamage;
        public readonly float AttackInterval;
        public readonly float ProduceInterval;
        public readonly int MaxHealth;
        public readonly float Cooldown;
        public readonly int Cost;

        public PlantOverrideData(
            int attackDamage,
            float attackInterval,
            float produceInterval,
            int maxHealth,
            float cooldown,
            int cost)
        {
            AttackDamage = attackDamage;
            AttackInterval = attackInterval;
            ProduceInterval = produceInterval;
            MaxHealth = maxHealth;
            Cooldown = cooldown;
            Cost = cost;
        }

        public void ApplyTo(PlantDataManager.PlantData target)
        {
            target.attackDamage = AttackDamage;
            target.attackInterval = AttackInterval;
            target.produceInterval = ProduceInterval;
            target.maxHealth = MaxHealth;
            target.cd = Cooldown;
            target.cost = Cost;
        }
    }

    private readonly struct OriginalPlantStats
    {
        public readonly int AttackDamage;
        public readonly float AttackInterval;
        public readonly float ProduceInterval;
        public readonly int MaxHealth;
        public readonly int BaselineDamage;
        public readonly int BaselineMaxHealth;

        public OriginalPlantStats(
            int attackDamage,
            float attackInterval,
            float produceInterval,
            int maxHealth,
            int baselineDamage,
            int baselineMaxHealth)
        {
            AttackDamage = attackDamage;
            AttackInterval = attackInterval;
            ProduceInterval = produceInterval;
            MaxHealth = maxHealth;
            BaselineDamage = baselineDamage;
            BaselineMaxHealth = baselineMaxHealth;
        }
    }

    private readonly struct OriginalCardStats
    {
        public readonly float FullCD;
        public readonly int SeedCost;

        public OriginalCardStats(float fullCD, int seedCost)
        {
            FullCD = fullCD;
            SeedCost = seedCost;
        }
    }

    public CustomPlantData()
    {
        instance = this;

        CreateCategory("General");

        OverrideLevelData = new BoolSetting("Override Custom Level's Data", false)
        {
            OnValueChanged = _ => OnOverridesChanged()
        };
        AddSettings(OverrideLevelData);

        EndCategory();

        CreateCategory("Normal Data");

        NormalSections = new SectionSetting("Normal Plants", _ => CreatePlantSectionTemplate(isStarbound: false), 0)
        {
            CustomSaveKey = "NormalData",
            TitleFormatter = index => FormatSectionTitle(NormalSections, index),
            OnSectionsChanged = () =>
            {
                RefreshSectionTitles(NormalSections);
                OnOverridesChanged();
            }
        };
        AddSettings(NormalSections);

        EndCategory();

        CreateCategory("Starbound Data");

        StarboundSections = new SectionSetting("Starbound Plants", _ => CreatePlantSectionTemplate(isStarbound: true), 0)
        {
            CustomSaveKey = "StarboundData",
            TitleFormatter = index => FormatSectionTitle(StarboundSections, index),
            OnSectionsChanged = () =>
            {
                RefreshSectionTitles(StarboundSections);
                OnOverridesChanged();
            }
        };
        AddSettings(StarboundSections);

        EndCategory();

        GameData.OnPlantSpawned += HandlePlantSpawned;
        GameData.OnPlantDie += HandlePlantDied;
        GameData.Actions.OnBoardDestroyed += ClearTrackedEntities;
        GameData.Actions.OnBoardDied += ClearTrackedEntities;
        GameData.Actions.OnInGameUIDestroyed += ClearTrackedCards;
        GameData.Actions.OnInGameUIInstantiated += _ =>
        {
            if (Active) SyncLiveEntities();
        };
    }

    private List<Setting> CreatePlantSectionTemplate(bool isStarbound)
    {
        var plantSelect = new EntitySelectSetting(KeyPlant, EntitySelectSetting.Plant)
        {
            MaxSelection = 1,
            CloseOnSingleSelect = true,
            Blacklist = Banned.PlantTypeBanned,
            CustomNames = TranslateEnum(typeof(PlantType))
        };

        var damage = new IntSetting(KeyDamage, 0, 5000, 0, 0, int.MaxValue)
        {
            OnValueChanged = _ => OnOverridesChanged(),
            IsLogarithmic = false,
        };
        var attackInterval = new FloatSetting(KeyAttackInterval, 0f, 30f, 0f, 3, 0f, float.MaxValue)
        {
            OnValueChanged = _ => OnOverridesChanged()
        };
        var produceInterval = new FloatSetting(KeyProduceInterval, 0f, 60f, 0f, 3, 0f, float.MaxValue)
        {
            OnValueChanged = _ => OnOverridesChanged()
        };
        var maxHealth = new IntSetting(KeyMaxHealth, 0, 50000, 300, 0, int.MaxValue)
        {
            OnValueChanged = _ => OnOverridesChanged(),
            IsLogarithmic = false,
        };
        var cooldown = new FloatSetting(KeyCooldown, 0f, 120f, 7.5f, 3, 0f, float.MaxValue)
        {
            OnValueChanged = _ => OnOverridesChanged()
        };
        var cost = new IntSetting(KeyCost, 0, 1000, 100, 0, int.MaxValue)
        {
            OnValueChanged = _ => OnOverridesChanged(),
            IsLogarithmic = false,
        };

        plantSelect.OnSelectionChanged = (plantId, isSelected) =>
        {
            SectionSetting owner = isStarbound ? StarboundSections : NormalSections;
            SectionInstance section = FindOwningSection(owner, plantSelect);

            if (!isSelected)
            {
                if (section != null)
                {
                    int idx = owner.Sections.IndexOf(section);
                    section.Title = $"Section #{idx + 1}";
                }
                OnOverridesChanged();
                return;
            }

            PlantType plantType = (PlantType)plantId;
            if (section != null)
            {
                section.Title = plantSelect.GetDisplayName(plantId);
            }

            if (TryGetManagerPlantData(plantType, isStarbound, out var data))
            {
                damage.DefaultValue = data.attackDamage;
                damage.SetDirect(data.attackDamage);

                attackInterval.DefaultValue = data.attackInterval;
                attackInterval.SetDirect(data.attackInterval);

                produceInterval.DefaultValue = data.produceInterval;
                produceInterval.SetDirect(data.produceInterval);

                maxHealth.DefaultValue = data.maxHealth;
                maxHealth.SetDirect(data.maxHealth);

                cooldown.DefaultValue = data.cd;
                cooldown.SetDirect(data.cd);

                cost.DefaultValue = data.cost;
                cost.SetDirect(data.cost);
            }

            OnOverridesChanged();
        };

        return new List<Setting>
        {
            plantSelect,
            damage,
            attackInterval,
            produceInterval,
            maxHealth,
            cooldown,
            cost
        };
    }

    public override void OnSettingOpened()
    {
        RefreshSectionMetadata(NormalSections, isStarbound: false);
        RefreshSectionMetadata(StarboundSections, isStarbound: true);
    }

    public override void OnLanguageChanged()
    {
        RefreshSectionMetadata(NormalSections, isStarbound: false);
        RefreshSectionMetadata(StarboundSections, isStarbound: true);
    }

    public override void OnEnable()
    {
        RebuildOverrideCaches();
        SyncLiveEntities();
    }

    public override void OnDisable()
    {
        _cachedPatchedData.Clear();
        RestoreAllLiveEntities();
    }

    private void OnOverridesChanged()
    {
        RebuildOverrideCaches();
        if (Active)
        {
            SyncLiveEntities();
        }
    }

    private void RebuildOverrideCaches()
    {
        _cachedPatchedData.Clear();
        RebuildOverrideCache(NormalSections, _normalOverrides);
        RebuildOverrideCache(StarboundSections, _starboundOverrides);
    }

    private static void RebuildOverrideCache(SectionSetting sectionSetting, Dictionary<PlantType, PlantOverrideData> target)
    {
        target.Clear();
        if (sectionSetting?.Sections == null) return;

        // Higher-index sections overwrite lower-index sections for duplicate plant types
        for (int i = 0; i < sectionSetting.Sections.Count; i++)
        {
            var sec = sectionSetting.Sections[i];
            if (sec == null) continue;

            if (!sec.Find<EntitySelectSetting>(KeyPlant, out var plantSelect) || plantSelect.SelectedValues.Count == 0)
            {
                continue;
            }

            PlantType plantType = (PlantType)plantSelect.SelectedValues.First();
            int dmg = sec.Find<IntSetting>(KeyDamage)?.Value ?? 0;
            float atkInterval = sec.Find<FloatSetting>(KeyAttackInterval)?.Value ?? 0f;
            float prodInterval = sec.Find<FloatSetting>(KeyProduceInterval)?.Value ?? 0f;
            int maxHp = sec.Find<IntSetting>(KeyMaxHealth)?.Value ?? 300;
            float cd = sec.Find<FloatSetting>(KeyCooldown)?.Value ?? 7.5f;
            int sunCost = sec.Find<IntSetting>(KeyCost)?.Value ?? 100;

            target[plantType] = new PlantOverrideData(dmg, atkInterval, prodInterval, maxHp, cd, sunCost);
        }
    }

    private static SectionInstance FindOwningSection(SectionSetting group, EntitySelectSetting plantSelect)
    {
        if (group?.Sections == null) return null;

        for (int i = 0; i < group.Sections.Count; i++)
        {
            var sec = group.Sections[i];
            if (sec != null && ReferenceEquals(sec.Find(KeyPlant), plantSelect))
            {
                return sec;
            }
        }
        return null;
    }

    private static string FormatSectionTitle(SectionSetting group, int index)
    {
        if (group?.Sections != null && index >= 0 && index < group.Sections.Count)
        {
            var sec = group.Sections[index];
            if (sec != null && sec.Find<EntitySelectSetting>(KeyPlant, out var plantSelect) && plantSelect.SelectedValues.Count > 0)
            {
                int id = plantSelect.SelectedValues.First();
                return plantSelect.GetDisplayName(id);
            }
        }
        return $"Section #{index + 1}";
    }

    private static void RefreshSectionTitles(SectionSetting group)
    {
        if (group?.Sections == null) return;

        for (int i = 0; i < group.Sections.Count; i++)
        {
            var sec = group.Sections[i];
            if (sec != null)
            {
                sec.Title = FormatSectionTitle(group, i);
            }
        }
    }

    private void RefreshSectionMetadata(SectionSetting group, bool isStarbound)
    {
        if (group?.Sections == null) return;

        Dictionary<int, string> translatedNames = TranslateEnum(typeof(PlantType));

        for (int i = 0; i < group.Sections.Count; i++)
        {
            var sec = group.Sections[i];
            if (sec == null) continue;

            if (sec.Find<EntitySelectSetting>(KeyPlant, out var plantSelect))
            {
                plantSelect.CustomNames = translatedNames;
                if (plantSelect.SelectedValues.Count > 0)
                {
                    int id = plantSelect.SelectedValues.First();
                    sec.Title = plantSelect.GetDisplayName(id);

                    if (TryGetManagerPlantData((PlantType)id, isStarbound, out var origData))
                    {
                        if (sec.Find<IntSetting>(KeyDamage, out var dmg)) dmg.DefaultValue = origData.attackDamage;
                        if (sec.Find<FloatSetting>(KeyAttackInterval, out var atk)) atk.DefaultValue = origData.attackInterval;
                        if (sec.Find<FloatSetting>(KeyProduceInterval, out var prod)) prod.DefaultValue = origData.produceInterval;
                        if (sec.Find<IntSetting>(KeyMaxHealth, out var hp)) hp.DefaultValue = origData.maxHealth;
                        if (sec.Find<FloatSetting>(KeyCooldown, out var cd)) cd.DefaultValue = origData.cd;
                        if (sec.Find<IntSetting>(KeyCost, out var cost)) cost.DefaultValue = origData.cost;
                    }
                }
                else
                {
                    sec.Title = $"Section #{i + 1}";
                }
            }
        }
    }

    private static bool TryGetManagerPlantData(PlantType plantType, bool isStarbound, out PlantDataManager.PlantData data)
    {
        data = null;
        try
        {
            if (isStarbound)
            {
                var modifiedDict = PlantDataManager.PlantData_Modified;
                if (modifiedDict != null && modifiedDict.TryGetValue(plantType, out data) && data != null)
                {
                    return true;
                }
            }

            var defaultDict = PlantDataManager.PlantData_Default;
            if (defaultDict != null && defaultDict.TryGetValue(plantType, out data) && data != null)
            {
                return true;
            }
        }
        catch
        {
            // PlantDataManager may not be initialized yet during very early startup
        }

        return false;
    }

    private static bool TryGetLevelDataPlantOverride(PlantType plantType, out PlantDataManager.PlantData levelPlantData)
    {
        levelPlantData = null;
        try
        {
            if (!LevelManager.TryGetLevelData(out var levelData) || levelData == null)
            {
                return false;
            }

            var plantDatas = levelData.PlantDatas;
            if (plantDatas == null) return false;

            for (int i = 0; i < plantDatas.Count; i++)
            {
                var entry = plantDatas[i];
                if (entry != null && entry.thePlantType == plantType)
                {
                    levelPlantData = entry;
                    return true;
                }
            }
        }
        catch
        {
            // Safe fallback if LevelManager is not ready
        }

        return false;
    }

    private static bool TryGetBaselinePlantData(PlantType plantType, out PlantDataManager.PlantData data)
    {
        if (TryGetLevelDataPlantOverride(plantType, out data))
        {
            return true;
        }

        bool isStarbound = GameAPP.theBoardType == LevelType.StarAdvanture;
        return TryGetManagerPlantData(plantType, isStarbound, out data);
    }

    private bool TryGetOverride(PlantType plantType, out PlantOverrideData overrideData, out bool isStarboundMode)
    {
        isStarboundMode = GameAPP.theBoardType == LevelType.StarAdvanture;
        overrideData = default;

        if (!OverrideLevelData.Value && TryGetLevelDataPlantOverride(plantType, out _))
        {
            return false;
        }

        var map = isStarboundMode ? _starboundOverrides : _normalOverrides;
        return map.TryGetValue(plantType, out overrideData);
    }

    private void HandlePlantSpawned(Plant plant)
    {
        if (!Active || plant == null || plant.gameObject == null) return;
        if (_originalPlants.ContainsKey(plant)) return;
        if (!TryGetOverride(plant.thePlantType, out var ov, out _)) return;
        if (!TryGetBaselinePlantData(plant.thePlantType, out var baseData)) return;

        int origDamage = baseData.attackDamage;
        if (ov.AttackDamage > 0 && plant.attackDamage != ov.AttackDamage)
        {
            origDamage = Mathf.RoundToInt(baseData.attackDamage * (plant.attackDamage / (float)ov.AttackDamage));
        }

        int origMaxHealth = baseData.maxHealth;
        if (ov.MaxHealth > 0 && plant.thePlantMaxHealth != ov.MaxHealth)
        {
            int scaled = Mathf.RoundToInt(baseData.maxHealth * (plant.thePlantMaxHealth / (float)ov.MaxHealth));
            origMaxHealth = baseData.maxHealth > 0 ? Mathf.Max(1, scaled) : scaled;
        }

        _originalPlants[plant] = new OriginalPlantStats(
            origDamage,
            baseData.attackInterval,
            baseData.produceInterval,
            origMaxHealth,
            baseData.attackDamage,
            baseData.maxHealth);
    }

    private void HandlePlantDied(Plant plant)
    {
        if (plant != null)
        {
            _originalPlants.Remove(plant);
        }
    }

    private void ClearTrackedEntities()
    {
        _originalPlants.Clear();
        _originalCards.Clear();
    }

    private void ClearTrackedCards()
    {
        _originalCards.Clear();
    }

    private void SyncLiveEntities()
    {
        if (GameData.BoardInstanceIsNull) return;

        PruneDestroyedTrackedEntities();

        var plants = GameData.PlantList;
        if (plants != null)
        {
            for (int i = 0; i < plants.Count; i++)
            {
                Plant plant = plants[i];
                if (plant == null || plant.gameObject == null) continue;

                if (TryGetOverride(plant.thePlantType, out var ov, out _))
                {
                    if (!_originalPlants.TryGetValue(plant, out var orig))
                    {
                        TryGetBaselinePlantData(plant.thePlantType, out var baseData);
                        orig = new OriginalPlantStats(
                            plant.attackDamage,
                            plant.thePlantAttackInterval,
                            plant.thePlantProduceInterval,
                            plant.thePlantMaxHealth,
                            baseData?.attackDamage ?? plant.attackDamage,
                            baseData?.maxHealth ?? plant.thePlantMaxHealth);
                        _originalPlants[plant] = orig;
                    }

                    ApplyOverrideToPlant(plant, orig, ov);
                }
                else if (_originalPlants.TryGetValue(plant, out var orig))
                {
                    RestorePlant(plant, orig);
                    _originalPlants.Remove(plant);
                }
            }
        }

        if (InGameUI.Instance != null && InGameUI.Instance.Cards != null)
        {
            foreach (CardUI card in InGameUI.Instance.Cards)
            {
                if (card == null || card.gameObject == null) continue;
                if (card.thePlantType == PlantType.Nothing || (int)card.thePlantType < 0) continue;

                if (TryGetOverride(card.thePlantType, out var ov, out _))
                {
                    if (!_originalCards.ContainsKey(card))
                    {
                        _originalCards[card] = new OriginalCardStats(card.fullCD, card.theSeedCost);
                    }

                    ApplyOverrideToCard(card, ov);
                }
                else if (_originalCards.TryGetValue(card, out var origCard))
                {
                    RestoreCard(card, origCard);
                    _originalCards.Remove(card);
                }
            }
        }
    }

    private void RestoreAllLiveEntities()
    {
        foreach (var kvp in _originalPlants)
        {
            Plant plant = kvp.Key;
            if (plant == null || plant.gameObject == null) continue;
            RestorePlant(plant, kvp.Value);
        }
        _originalPlants.Clear();

        foreach (var kvp in _originalCards)
        {
            CardUI card = kvp.Key;
            if (card == null || card.gameObject == null) continue;
            RestoreCard(card, kvp.Value);
        }
        _originalCards.Clear();
    }

    private void PruneDestroyedTrackedEntities()
    {
        if (_originalPlants.Count > 0)
        {
            List<Plant> deadPlants = null;
            foreach (var key in _originalPlants.Keys)
            {
                if (key == null || key.gameObject == null)
                {
                    deadPlants ??= new List<Plant>();
                    deadPlants.Add(key);
                }
            }
            if (deadPlants != null)
            {
                for (int i = 0; i < deadPlants.Count; i++)
                {
                    _originalPlants.Remove(deadPlants[i]);
                }
            }
        }

        if (_originalCards.Count > 0)
        {
            List<CardUI> deadCards = null;
            foreach (var key in _originalCards.Keys)
            {
                if (key == null || key.gameObject == null)
                {
                    deadCards ??= new List<CardUI>();
                    deadCards.Add(key);
                }
            }
            if (deadCards != null)
            {
                for (int i = 0; i < deadCards.Count; i++)
                {
                    _originalCards.Remove(deadCards[i]);
                }
            }
        }
    }

    private static void ApplyOverrideToPlant(Plant plant, OriginalPlantStats orig, PlantOverrideData ov)
    {
        int targetDamage = orig.BaselineDamage > 0
            ? Mathf.RoundToInt(orig.AttackDamage * (ov.AttackDamage / (float)orig.BaselineDamage))
            : orig.AttackDamage + ov.AttackDamage;
        plant.attackDamage = targetDamage;

        plant.thePlantAttackInterval = ov.AttackInterval;
        if (plant.thePlantAttackCountDown > ov.AttackInterval)
        {
            plant.thePlantAttackCountDown = ov.AttackInterval;
        }

        plant.thePlantProduceInterval = ov.ProduceInterval;
        if (plant.thePlantProduceCountDown > ov.ProduceInterval)
        {
            plant.thePlantProduceCountDown = ov.ProduceInterval;
        }

        int targetMaxHealth = orig.BaselineMaxHealth > 0
            ? Mathf.Max(ov.MaxHealth > 0 ? 1 : 0, Mathf.RoundToInt(orig.MaxHealth * (ov.MaxHealth / (float)orig.BaselineMaxHealth)))
            : ov.MaxHealth;

        ApplyPlantMaxHealth(plant, targetMaxHealth);
        plant.UpdateText();
    }

    private static void RestorePlant(Plant plant, OriginalPlantStats orig)
    {
        plant.attackDamage = orig.AttackDamage;
        plant.thePlantAttackInterval = orig.AttackInterval;
        if (plant.thePlantAttackCountDown > orig.AttackInterval)
        {
            plant.thePlantAttackCountDown = orig.AttackInterval;
        }

        plant.thePlantProduceInterval = orig.ProduceInterval;
        if (plant.thePlantProduceCountDown > orig.ProduceInterval)
        {
            plant.thePlantProduceCountDown = orig.ProduceInterval;
        }

        ApplyPlantMaxHealth(plant, orig.MaxHealth);
        plant.UpdateText();
    }

    private static void ApplyPlantMaxHealth(Plant plant, int newMaxHealth)
    {
        int prevMax = plant.thePlantMaxHealth;
        plant.thePlantMaxHealth = newMaxHealth;

        if (plant.thePlantHealth <= 0) return;

        if (newMaxHealth <= 0)
        {
            plant.thePlantHealth = 0;
            return;
        }

        if (prevMax > 0)
        {
            float ratio = (float)plant.thePlantHealth / prevMax;
            plant.thePlantHealth = Mathf.Max(1, Mathf.RoundToInt(newMaxHealth * ratio));
        }
        else
        {
            plant.thePlantHealth = newMaxHealth;
        }
    }

    private static void ApplyOverrideToCard(CardUI card, PlantOverrideData ov)
    {
        card.fullCD = ov.Cooldown;
        if (card.CD > card.fullCD)
        {
            card.CD = card.fullCD;
        }

        card.theSeedCost = ov.Cost;
        if (card.textBg != null)
        {
            card.textBg.text = card.theSeedCost.ToString();
        }
    }

    private static void RestoreCard(CardUI card, OriginalCardStats orig)
    {
        card.fullCD = orig.FullCD;
        if (card.CD > card.fullCD)
        {
            card.CD = card.fullCD;
        }

        card.theSeedCost = orig.SeedCost;
        if (card.textBg != null)
        {
            card.textBg.text = card.theSeedCost.ToString();
        }
    }

    // Harmony Patches
    [HarmonyPatch(typeof(PlantDataManager), nameof(PlantDataManager.GetPlantData))]
    public static class PlantDataManagerGetPlantDataPatch
    {
        [HarmonyPostfix]
        public static void Postfix(PlantType plantType, ref PlantDataManager.PlantData __result)
        {
            if (instance == null || !instance.Active || __result == null) return;
            if (!instance.TryGetOverride(plantType, out var ov, out bool isStarboundMode)) return;

            var key = (isStarboundMode, plantType);
            if (!instance._cachedPatchedData.TryGetValue(key, out var patched) || patched == null)
            {
                patched = new PlantDataManager.PlantData(__result);
                instance._cachedPatchedData[key] = patched;
            }

            patched.thePlantType = plantType;
            ov.ApplyTo(patched);
            __result = patched;
        }
    }

    [HarmonyPatch(typeof(CardUI), "Start")]
    public static class CardUIStartPatch
    {
        [HarmonyPostfix]
        public static void Postfix(CardUI __instance)
        {
            if (instance == null || !instance.Active || __instance == null) return;
            if (__instance.thePlantType == PlantType.Nothing || (int)__instance.thePlantType < 0) return;
            if (InGameUI.Instance == null || InGameUI.Instance.Cards == null || !InGameUI.Instance.Cards.Contains(__instance)) return;
            if (instance._originalCards.ContainsKey(__instance)) return;
            if (!instance.TryGetOverride(__instance.thePlantType, out var ov, out _)) return;
            if (!TryGetBaselinePlantData(__instance.thePlantType, out var baseData)) return;

            float origFullCD = ov.Cooldown > 0f
                ? __instance.fullCD * (baseData.cd / ov.Cooldown)
                : baseData.cd;
            int origCost = Mathf.Max(0, __instance.theSeedCost - ov.Cost + baseData.cost);

            instance._originalCards[__instance] = new OriginalCardStats(origFullCD, origCost);
        }
    }
}