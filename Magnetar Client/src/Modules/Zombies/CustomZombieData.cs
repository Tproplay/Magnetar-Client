using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Magnetar_Client.UI.Setting;
using Magnetar_Client.Utils;
using UnityEngine;
using Magnetar_Client.Modules.Helpers;



#if MELONLOADER || RELEASE_MELON
using Il2Cpp;
using Il2CppGameLevel;
using Il2CppTMPro;
#elif BEPINEX || RELEASE_BEPINEX
using GameLevel;
using TMPro;
#endif

namespace Magnetar_Client.Modules;

public class CustomZombieData : Module
{
    #region Module Metadata

    public override string Name { get; set; } = "Custom Zombie Data";
    public override string Description { get; set; } =
        "Customize base stats, armors, cost, cooldown, summon weight/level, and live speed for individual zombie types.";
    public override string SearchHints { get; set; } =
        "customzombiedata zombiedata customzombie zombiehealth zombiedamage zombiespeed zombiearmor " +
        "ize cost cooldown summon weight level stats override";
    public override ModuleCategory Category { get; set; } = ModuleCategory.Zombie;

    #endregion

    #region Nested Types

    private sealed class ZombieDataBaseline
    {
        public int AttackDamage;
        public int MaxHealth;
        public int FirstArmorMaxHealth;
        public int SecondArmorMaxHealth;
        public int Cost;
        public float Cooldown;
        public int SummonLevel;
        public int SummonWeight;
        public float Armor;

        public static ZombieDataBaseline FromData(ZombieDataManager.ZombieData data) => new()
        {
            AttackDamage = data.theAttackDamage,
            MaxHealth = data.theMaxHealth,
            FirstArmorMaxHealth = data.theFirstArmorMaxHealth,
            SecondArmorMaxHealth = data.theSecondArmorMaxHealth,
            Cost = data.cost,
            Cooldown = data.cd,
            SummonLevel = data.summonLevel,
            SummonWeight = data.summonWeight,
            Armor = data.armor,
        };

        public void ApplyToData(ZombieDataManager.ZombieData data)
        {
            data.theAttackDamage = AttackDamage;
            data.theMaxHealth = MaxHealth;
            data.theFirstArmorMaxHealth = FirstArmorMaxHealth;
            data.theSecondArmorMaxHealth = SecondArmorMaxHealth;
            data.cost = Cost;
            data.cd = Cooldown;
            data.summonLevel = SummonLevel;
            data.summonWeight = SummonWeight;
            data.armor = Armor;
        }
    }

    private sealed class ZombieOverrideData
    {
        public int AttackDamage;
        public int MaxHealth;
        public int FirstArmorMaxHealth;
        public int SecondArmorMaxHealth;
        public float Armor;
        public int Cost;
        public float Cooldown;
        public int SummonLevel;
        public int SummonWeight;
        public float SpeedMultiplier;

        public void ApplyToData(ZombieDataManager.ZombieData data)
        {
            data.theAttackDamage = AttackDamage;
            data.theMaxHealth = MaxHealth;
            data.theFirstArmorMaxHealth = FirstArmorMaxHealth;
            data.theSecondArmorMaxHealth = SecondArmorMaxHealth;
            data.armor = Armor;
            data.cost = Cost;
            data.cd = Cooldown;
            data.summonLevel = SummonLevel;
            data.summonWeight = SummonWeight;
        }
    }

    private sealed class OriginalCardStats
    {
        public int OrigSeedCost;
        public float OrigFullCd;
        public float PrevFullCd;
    }

    private sealed class SectionControls
    {
        public EntitySelectSetting ZombieSelect;
        public IntSetting AttackDamage;
        public IntSetting MaxHealth;
        public IntSetting FirstArmorMaxHealth;
        public IntSetting SecondArmorMaxHealth;
        public FloatSetting Armor;
        public IntSetting Cost;
        public FloatSetting Cooldown;
        public IntSetting SummonLevel;
        public IntSetting SummonWeight;
        public FloatSetting SpeedMultiplier;
    }

    private struct GetZombieDataCallState
    {
        public bool SwappedToOriginal;
        public ZombieType ZombieType;
        public ZombieOverrideData OverrideData;
    }

    #endregion

    #region Constants & Fields

    private const string KeyGeneral = "General";
    private const string KeyOverrideLevelData = "Override LevelData";
    private const string KeyZombieData = "Zombie Data";
    private const string KeyZombie = "Zombie";
    private const string KeyAttackDamage = "Attack Damage";
    private const string KeyMaxHealth = "Max Health";
    private const string KeyFirstArmor = "1st Armor Max Health";
    private const string KeySecondArmor = "2nd Armor Max Health";
    private const string KeyArmor = "Armor";
    private const string KeyCost = "Cost";
    private const string KeyCooldown = "Cooldown";
    private const string KeySummonLevel = "Summon Level";
    private const string KeySummonWeight = "Summon Weight";
    private const string KeySpeedMultiplier = "Speed Multiplier";
    private const string KeySectionNumbered = "Zombie #{0}";

    private static readonly string[] TranslationKeys =
    {
        KeyGeneral,
        KeyOverrideLevelData,
        KeyZombieData,
        KeyZombie,
        KeyAttackDamage,
        KeyMaxHealth,
        KeyFirstArmor,
        KeySecondArmor,
        KeyArmor,
        KeyCost,
        KeyCooldown,
        KeySummonLevel,
        KeySummonWeight,
        KeySpeedMultiplier,
        KeySectionNumbered,
    };

    public static CustomZombieData Instance { get; private set; }

    public BoolSetting OverrideLevelDataSetting;
    public SectionSetting ZombieSections;

    private readonly Dictionary<ZombieType, ZombieDataBaseline> _originalZombieData = new();
    private readonly Dictionary<ZombieType, ZombieOverrideData> _overrideCache = new();
    private readonly HashSet<ZombieType> _mutatedManagerTypes = new();

    private readonly HashSet<ZombieType> _activeRegisteredStatTypes = new();
    private readonly Dictionary<IntPtr, OriginalCardStats> _originalCards = new();

    private bool _isInternalBatchUpdate;
    private int _lastSectionCount;

    #endregion

    #region Construction & Module Lifecycle

    public CustomZombieData()
    {
        Instance = this;
        RegisterTranslations(Translator.CreateDictionary(TranslationKeys));

        CreateCategory(KeyGeneral);

        OverrideLevelDataSetting = new BoolSetting(KeyOverrideLevelData, false)
        {
            OnValueChanged = _ => OnOverrideConfigChanged(),
        };

        ZombieSections = new SectionSetting(KeyZombieData, _ => CreateZombieSectionTemplate(), 0)
        {
            TranslateTitles = false,
        };

        AddSettings(OverrideLevelDataSetting, ZombieSections);
        EndCategory();

        GameData.Actions.OnBoardDestroyed += ClearLiveCaches;
        GameData.Actions.OnBoardDied += ClearLiveCaches;
    }

    public override void OnEnable()
    {
        CaptureBaselinesIfReady();
        RefreshAllSectionControls();
        OnOverrideConfigChanged();
    }

    public override void OnDisable()
    {
        RestoreManagerDictionary();
        RestoreZombieStatManagerModifiers();
        RestoreAllIZECards();
    }

    public override void OnSettingOpened()
    {
        CaptureBaselinesIfReady();
        RefreshAllSectionControls();
        OnOverrideConfigChanged();
    }

    public override void OnLanguageChanged()
    {
        RefreshAllSectionControls();
    }

    public override void OnUpdate()
    {
        if (ZombieSections?.Sections == null) return;

        int count = ZombieSections.Sections.Count;
        if (count != _lastSectionCount)
        {
            _lastSectionCount = count;
            RefreshAllSectionControls();
            OnOverrideConfigChanged();
        }
    }

    #endregion

    #region Section UI Creation & Sync

    private List<Setting> CreateZombieSectionTemplate()
    {
        var zombieBlacklist = new HashSet<int>(Banned.ZombieTypeBanned)
        {
            (int)ZombieType.Nothing,
        };

        var zombieSelect = new EntitySelectSetting(KeyZombie, EntitySelectSetting.Zombie)
        {
            MaxSelection = 1,
            CloseOnSingleSelect = true,
            Blacklist = zombieBlacklist,
            CustomNames = TranslateEnum(typeof(ZombieType)),
            SaveTranslation = false,
            CustomName = Translate(KeyZombie),
        };

        var attackDamage = new IntSetting(KeyAttackDamage, 0, 5000, 20, 0, int.MaxValue)
        {
            SaveTranslation = false,
            CustomName = Translate(KeyAttackDamage),
        };
        var maxHealth = new IntSetting(KeyMaxHealth, 1, 100000, 270, 1, int.MaxValue)
        {
            SaveTranslation = false,
            CustomName = Translate(KeyMaxHealth),
        };
        var firstArmorMax = new IntSetting(KeyFirstArmor, 0, 100000, 0, 0, int.MaxValue)
        {
            SaveTranslation = false,
            CustomName = Translate(KeyFirstArmor),
        };
        var secondArmorMax = new IntSetting(KeySecondArmor, 0, 100000, 0, 0, int.MaxValue)
        {
            SaveTranslation = false,
            CustomName = Translate(KeySecondArmor),
        };
        var armor = new FloatSetting(KeyArmor, 0f, 1f, 0f, 2, 0f, 10f, false, false)
        {
            SaveTranslation = false,
            CustomName = Translate(KeyArmor),
        };
        var cost = new IntSetting(KeyCost, 0, 2000, 50, 0, int.MaxValue)
        {
            SaveTranslation = false,
            CustomName = Translate(KeyCost),
        };
        var cooldown = new FloatSetting(KeyCooldown, 0f, 120f, 7.5f, 1, 0f, float.MaxValue)
        {
            SaveTranslation = false,
            CustomName = Translate(KeyCooldown),
        };
        var summonLevel = new IntSetting(KeySummonLevel, 0, 10, 1, 0, 1000, false, false)
        {
            SaveTranslation = false,
            CustomName = Translate(KeySummonLevel),
        };
        var summonWeight = new IntSetting(KeySummonWeight, 0, 10000, 1000, 0, int.MaxValue)
        {
            SaveTranslation = false,
            CustomName = Translate(KeySummonWeight),
        };
        var speedMultiplier = new FloatSetting(KeySpeedMultiplier, 0.1f, 10f, 1f, 2, 0.01f, 100f)
        {
            SaveTranslation = false,
            CustomName = Translate(KeySpeedMultiplier),
        };

        zombieSelect.OnSelectionChanged = (zombieId, isSelected) =>
            HandleZombieSelectionChanged(zombieSelect, zombieId, isSelected);

        Action onStatChanged = () =>
        {
            if (!_isInternalBatchUpdate)
            {
                OnOverrideConfigChanged();
            }
        };

        attackDamage.OnValueChanged = _ => onStatChanged();
        maxHealth.OnValueChanged = _ => onStatChanged();
        firstArmorMax.OnValueChanged = _ => onStatChanged();
        secondArmorMax.OnValueChanged = _ => onStatChanged();
        armor.OnValueChanged = _ => onStatChanged();
        cost.OnValueChanged = _ => onStatChanged();
        cooldown.OnValueChanged = _ => onStatChanged();
        summonLevel.OnValueChanged = _ => onStatChanged();
        summonWeight.OnValueChanged = _ => onStatChanged();
        speedMultiplier.OnValueChanged = _ => onStatChanged();

        return new List<Setting>
        {
            zombieSelect,
            attackDamage,
            maxHealth,
            firstArmorMax,
            secondArmorMax,
            armor,
            cost,
            cooldown,
            summonLevel,
            summonWeight,
            speedMultiplier,
        };
    }

    private void HandleZombieSelectionChanged(EntitySelectSetting zombieSelect, int zombieId, bool isSelected)
    {
        if (_isInternalBatchUpdate) return;

        SectionInstance section = FindOwningSection(zombieSelect);
        if (section == null || !TryResolveControls(section, out var ctrls))
        {
            OnOverrideConfigChanged();
            return;
        }

        if (isSelected)
        {
            CaptureBaselinesIfReady();
            ZombieType type = (ZombieType)zombieId;
            if (type != ZombieType.Nothing && TryGetManagerBaseline(type, out var baseline))
            {
                ApplyBaselineToControls(ctrls, baseline, overwriteValues: true);
            }
        }

        RefreshAllSectionTitles();
        OnOverrideConfigChanged();
    }

    private void RefreshAllSectionControls()
    {
        if (ZombieSections?.Sections == null) return;

        Dictionary<int, string> zombieNames = TranslateEnum(typeof(ZombieType));
        for (int i = 0; i < ZombieSections.Sections.Count; i++)
        {
            SectionInstance section = ZombieSections.Sections[i];
            if (!TryResolveControls(section, out var ctrls)) continue;

            ctrls.ZombieSelect.CustomNames = zombieNames;
            ctrls.ZombieSelect.CustomName = Translate(KeyZombie);
            ctrls.AttackDamage.CustomName = Translate(KeyAttackDamage);
            ctrls.MaxHealth.CustomName = Translate(KeyMaxHealth);
            ctrls.FirstArmorMaxHealth.CustomName = Translate(KeyFirstArmor);
            ctrls.SecondArmorMaxHealth.CustomName = Translate(KeySecondArmor);
            ctrls.Armor.CustomName = Translate(KeyArmor);
            ctrls.Cost.CustomName = Translate(KeyCost);
            ctrls.Cooldown.CustomName = Translate(KeyCooldown);
            ctrls.SummonLevel.CustomName = Translate(KeySummonLevel);
            ctrls.SummonWeight.CustomName = Translate(KeySummonWeight);
            ctrls.SpeedMultiplier.CustomName = Translate(KeySpeedMultiplier);

            if (TryGetSelectedZombieType(ctrls.ZombieSelect, out ZombieType type) &&
                TryGetManagerBaseline(type, out var baseline))
            {
                ApplyBaselineToControls(ctrls, baseline, overwriteValues: false);
            }

            section.Title = FormatSectionTitle(ctrls, i);
        }

        _lastSectionCount = ZombieSections.Sections.Count;
    }

    private void RefreshAllSectionTitles()
    {
        if (ZombieSections?.Sections == null) return;

        for (int i = 0; i < ZombieSections.Sections.Count; i++)
        {
            SectionInstance section = ZombieSections.Sections[i];
            if (TryResolveControls(section, out var ctrls))
            {
                section.Title = FormatSectionTitle(ctrls, i);
            }
        }
    }

    private string FormatSectionTitle(SectionControls ctrls, int index)
    {
        if (TryGetSelectedZombieType(ctrls.ZombieSelect, out _))
        {
            string displayName = ctrls.ZombieSelect.GetPrimarySelectedDisplayName();
            if (!string.IsNullOrEmpty(displayName))
            {
                return displayName;
            }
        }

        return string.Format(Translate(KeySectionNumbered), index + 1);
    }

    private SectionInstance FindOwningSection(EntitySelectSetting target)
    {
        if (ZombieSections?.Sections == null || target == null) return null;

        for (int i = 0; i < ZombieSections.Sections.Count; i++)
        {
            SectionInstance section = ZombieSections.Sections[i];
            if (section.Find<EntitySelectSetting>(KeyZombie, out var s) && ReferenceEquals(s, target))
            {
                return section;
            }
        }

        return null;
    }

    private static bool TryResolveControls(SectionInstance section, out SectionControls ctrls)
    {
        ctrls = null;
        if (section == null) return false;

        if (!section.Find<EntitySelectSetting>(KeyZombie, out var zombieSelect) ||
            !section.Find<IntSetting>(KeyAttackDamage, out var attackDamage) ||
            !section.Find<IntSetting>(KeyMaxHealth, out var maxHealth) ||
            !section.Find<IntSetting>(KeyFirstArmor, out var firstArmorMax) ||
            !section.Find<IntSetting>(KeySecondArmor, out var secondArmorMax) ||
            !section.Find<FloatSetting>(KeyArmor, out var armor) ||
            !section.Find<IntSetting>(KeyCost, out var cost) ||
            !section.Find<FloatSetting>(KeyCooldown, out var cooldown) ||
            !section.Find<IntSetting>(KeySummonLevel, out var summonLevel) ||
            !section.Find<IntSetting>(KeySummonWeight, out var summonWeight) ||
            !section.Find<FloatSetting>(KeySpeedMultiplier, out var speedMultiplier))
        {
            return false;
        }

        ctrls = new SectionControls
        {
            ZombieSelect = zombieSelect,
            AttackDamage = attackDamage,
            MaxHealth = maxHealth,
            FirstArmorMaxHealth = firstArmorMax,
            SecondArmorMaxHealth = secondArmorMax,
            Armor = armor,
            Cost = cost,
            Cooldown = cooldown,
            SummonLevel = summonLevel,
            SummonWeight = summonWeight,
            SpeedMultiplier = speedMultiplier,
        };
        return true;
    }

    private void ApplyBaselineToControls(SectionControls ctrls, ZombieDataBaseline baseline, bool overwriteValues)
    {
        _isInternalBatchUpdate = true;
        try
        {
            int maxHp = Math.Max(1, baseline.MaxHealth);

            ctrls.AttackDamage.DefaultValue = baseline.AttackDamage;
            ctrls.MaxHealth.DefaultValue = maxHp;
            ctrls.FirstArmorMaxHealth.DefaultValue = baseline.FirstArmorMaxHealth;
            ctrls.SecondArmorMaxHealth.DefaultValue = baseline.SecondArmorMaxHealth;
            ctrls.Armor.DefaultValue = baseline.Armor;
            ctrls.Cost.DefaultValue = baseline.Cost;
            ctrls.Cooldown.DefaultValue = baseline.Cooldown;
            ctrls.SummonLevel.DefaultValue = baseline.SummonLevel;
            ctrls.SummonWeight.DefaultValue = baseline.SummonWeight;
            ctrls.SpeedMultiplier.DefaultValue = 1f;

            if (overwriteValues)
            {
                ctrls.AttackDamage.SetDirect(baseline.AttackDamage);
                ctrls.MaxHealth.SetDirect(maxHp);
                ctrls.FirstArmorMaxHealth.SetDirect(baseline.FirstArmorMaxHealth);
                ctrls.SecondArmorMaxHealth.SetDirect(baseline.SecondArmorMaxHealth);
                ctrls.Armor.SetDirect(baseline.Armor);
                ctrls.Cost.SetDirect(baseline.Cost);
                ctrls.Cooldown.SetDirect(baseline.Cooldown);
                ctrls.SummonLevel.SetDirect(baseline.SummonLevel);
                ctrls.SummonWeight.SetDirect(baseline.SummonWeight);
                ctrls.SpeedMultiplier.SetDirect(1f);
            }
        }
        finally
        {
            _isInternalBatchUpdate = false;
        }
    }

    private static bool TryGetSelectedZombieType(EntitySelectSetting zombieSelect, out ZombieType type)
    {
        type = ZombieType.Nothing;
        if (zombieSelect == null || zombieSelect.SelectedValues.Count == 0) return false;

        int id = zombieSelect.SelectedValues.First();
        type = (ZombieType)id;
        return type != ZombieType.Nothing;
    }

    #endregion

    #region Override Cache & Manager Dictionary Sync

    private void CaptureBaselinesIfReady(bool forceRecapture = false)
    {
        var dict = ZombieDataManager.zombieDataDic;
        if (dict == null || dict.Count == 0) return;

        foreach (var kvp in dict)
        {
            ZombieType type = kvp.Key;
            ZombieDataManager.ZombieData data = kvp.Value;
            if (data == null) continue;

            if (forceRecapture || (!_originalZombieData.ContainsKey(type) && !_mutatedManagerTypes.Contains(type)))
            {
                _originalZombieData[type] = ZombieDataBaseline.FromData(data);
            }
        }
    }

    private bool TryGetManagerBaseline(ZombieType type, out ZombieDataBaseline baseline)
    {
        CaptureBaselinesIfReady();
        return _originalZombieData.TryGetValue(type, out baseline);
    }

    private void RebuildOverrideCache()
    {
        _overrideCache.Clear();
        if (ZombieSections?.Sections == null) return;

        for (int i = 0; i < ZombieSections.Sections.Count; i++)
        {
            SectionInstance section = ZombieSections.Sections[i];
            if (!TryResolveControls(section, out var ctrls)) continue;
            if (!TryGetSelectedZombieType(ctrls.ZombieSelect, out ZombieType type)) continue;

            _overrideCache[type] = new ZombieOverrideData
            {
                AttackDamage = Math.Max(0, ctrls.AttackDamage.Value),
                MaxHealth = Math.Max(1, ctrls.MaxHealth.Value),
                FirstArmorMaxHealth = Math.Max(0, ctrls.FirstArmorMaxHealth.Value),
                SecondArmorMaxHealth = Math.Max(0, ctrls.SecondArmorMaxHealth.Value),
                Armor = Math.Max(0f, ctrls.Armor.Value),
                Cost = Math.Max(0, ctrls.Cost.Value),
                Cooldown = Math.Max(0f, ctrls.Cooldown.Value),
                SummonLevel = Math.Max(0, ctrls.SummonLevel.Value),
                SummonWeight = Math.Max(0, ctrls.SummonWeight.Value),
                SpeedMultiplier = Math.Max(0.01f, ctrls.SpeedMultiplier.Value),
            };
        }
    }

    private void OnOverrideConfigChanged()
    {
        CaptureBaselinesIfReady();
        RebuildOverrideCache();

        if (Active)
        {
            SyncManagerDictionary();
            SyncZombieStatManagerModifiers();
            SyncAllIZECards();
        }
        else
        {
            RestoreManagerDictionary();
            RestoreZombieStatManagerModifiers();
            RestoreAllIZECards();
        }
    }

    private void SyncManagerDictionary()
    {
        var dict = ZombieDataManager.zombieDataDic;
        if (dict == null) return;

        // Restore any types whose sections were removed or deselected.
        foreach (ZombieType type in _mutatedManagerTypes.ToList())
        {
            if (!_overrideCache.ContainsKey(type))
            {
                if (_originalZombieData.TryGetValue(type, out var orig) &&
                    dict.TryGetValue(type, out var entry) && entry != null)
                {
                    orig.ApplyToData(entry);
                }
                _mutatedManagerTypes.Remove(type);
            }
        }

        // Apply active overrides cleanly onto manager dictionary entries.
        foreach (var kvp in _overrideCache)
        {
            ZombieType type = kvp.Key;
            ZombieOverrideData ov = kvp.Value;

            if (dict.TryGetValue(type, out var entry) && entry != null)
            {
                if (!_originalZombieData.ContainsKey(type))
                {
                    _originalZombieData[type] = ZombieDataBaseline.FromData(entry);
                }

                ov.ApplyToData(entry);
                _mutatedManagerTypes.Add(type);
            }
        }
    }

    private void RestoreManagerDictionary()
    {
        var dict = ZombieDataManager.zombieDataDic;
        if (dict == null)
        {
            _mutatedManagerTypes.Clear();
            return;
        }

        foreach (ZombieType type in _mutatedManagerTypes.ToList())
        {
            if (_originalZombieData.TryGetValue(type, out var orig) &&
                dict.TryGetValue(type, out var entry) && entry != null)
            {
                orig.ApplyToData(entry);
            }
        }

        _mutatedManagerTypes.Clear();
    }

    private static bool TryGetLevelDataZombieOverride(ZombieType type, out ZombieDataManager.ZombieData levelEntry)
    {
        levelEntry = null;
        if (!LevelManager.TryGetLevelData(out var levelData) || levelData == null) return false;

        var list = levelData.ZombieDatas;
        if (list == null) return false;

        for (int i = 0; i < list.Count; i++)
        {
            var entry = list[i];
            if (entry != null && entry.theZombieType == type)
            {
                levelEntry = entry;
                return true;
            }
        }

        return false;
    }

    private bool TryGetEffectiveOverride(ZombieType type, out ZombieOverrideData ov)
    {
        ov = null;
        if (!Active || !_overrideCache.TryGetValue(type, out ov)) return false;

        if (!OverrideLevelDataSetting.Value && TryGetLevelDataZombieOverride(type, out _))
        {
            ov = null;
            return false;
        }

        return true;
    }

    #endregion

    #region Live Zombie & IZECard Synchronization

    private void ClearLiveCaches()
    {
        _originalCards.Clear();
    }

    private void SyncZombieStatManagerModifiers()
    {
        // 1. Remove previously registered modifiers
        foreach (var type in _activeRegisteredStatTypes)
        {
            string key = "CustomZombieData_" + (int)type;
            ZombieStatManager.RemoveModifier(key, StatType.MaxHealth);
            ZombieStatManager.RemoveModifier(key, StatType.Armor1Max);
            ZombieStatManager.RemoveModifier(key, StatType.Armor2Max);
            ZombieStatManager.RemoveModifier(key, StatType.AttackDamage);
            ZombieStatManager.RemoveModifier(key, StatType.Speed);
        }
        _activeRegisteredStatTypes.Clear();

        if (!Active) return;

        // 2. Register active overrides with ZombieStatManager
        foreach (var kvp in _overrideCache)
        {
            ZombieType type = kvp.Key;
            ZombieOverrideData ov = kvp.Value;
            if (!_originalZombieData.TryGetValue(type, out var baseline)) continue;

            string key = "CustomZombieData_" + (int)type;
            Func<Zombie, bool> filter = z => z.theZombieType == type;

            if (ov.MaxHealth != baseline.MaxHealth)
            {
                ZombieStatManager.SetModifier(key, StatType.MaxHealth, ModifierOp.Add, ov.MaxHealth - baseline.MaxHealth, filter);
            }
            if (ov.FirstArmorMaxHealth != baseline.FirstArmorMaxHealth)
            {
                ZombieStatManager.SetModifier(key, StatType.Armor1Max, ModifierOp.Add, ov.FirstArmorMaxHealth - baseline.FirstArmorMaxHealth, filter);
            }
            if (ov.SecondArmorMaxHealth != baseline.SecondArmorMaxHealth)
            {
                ZombieStatManager.SetModifier(key, StatType.Armor2Max, ModifierOp.Add, ov.SecondArmorMaxHealth - baseline.SecondArmorMaxHealth, filter);
            }
            if (ov.AttackDamage != baseline.AttackDamage)
            {
                ZombieStatManager.SetModifier(key, StatType.AttackDamage, ModifierOp.Add, ov.AttackDamage - baseline.AttackDamage, filter);
            }
            if (Math.Abs(ov.SpeedMultiplier - 1f) > 0.001f)
            {
                ZombieStatManager.SetModifier(key, StatType.Speed, ModifierOp.Multiply, ov.SpeedMultiplier, filter);
            }

            _activeRegisteredStatTypes.Add(type);
        }

        // Apply armor damage reduction float
        if (!GameData.BoardInstanceIsNull)
        {
            var list = GameData.ZombieList;
            for (int i = 0; i < list.Count; i++)
            {
                var z = list[i];
                if (z != null && _overrideCache.TryGetValue(z.theZombieType, out var ov))
                {
                    z.theArmor = ov.Armor;
                }
            }
        }
    }

    private void RestoreZombieStatManagerModifiers()
    {
        foreach (var type in _activeRegisteredStatTypes)
        {
            string key = "CustomZombieData_" + (int)type;
            ZombieStatManager.RemoveModifier(key, StatType.MaxHealth);
            ZombieStatManager.RemoveModifier(key, StatType.Armor1Max);
            ZombieStatManager.RemoveModifier(key, StatType.Armor2Max);
            ZombieStatManager.RemoveModifier(key, StatType.AttackDamage);
            ZombieStatManager.RemoveModifier(key, StatType.Speed);
        }
        _activeRegisteredStatTypes.Clear();

        if (!GameData.BoardInstanceIsNull)
        {
            var list = GameData.ZombieList;
            for (int i = 0; i < list.Count; i++)
            {
                var z = list[i];
                if (z != null && _originalZombieData.TryGetValue(z.theZombieType, out var baseline))
                {
                    z.theArmor = baseline.Armor;
                }
            }
        }
    }

    private void SyncAllIZECards()
    {
        var cards = UnityEngine.Object.FindObjectsOfType<IZECard>();
        if (cards == null) return;

        foreach (IZECard card in cards)
        {
            if (card == null || card.gameObject == null) continue;

            OriginalCardStats orig = EnsureCardBaseline(card);
            if (TryGetEffectiveOverride(card.theZombieType, out var ov))
            {
                ApplyCostAndCdToCard(card, orig, ov.Cost, ov.Cooldown);
            }
            else
            {
                ApplyCostAndCdToCard(card, orig, orig.OrigSeedCost, orig.OrigFullCd);
            }
        }
    }

    private void RestoreAllIZECards()
    {
        var cards = UnityEngine.Object.FindObjectsOfType<IZECard>();
        if (cards == null) return;

        foreach (IZECard card in cards)
        {
            if (card == null || card.gameObject == null) continue;
            if (_originalCards.TryGetValue(card.Pointer, out var orig))
            {
                ApplyCostAndCdToCard(card, orig, orig.OrigSeedCost, orig.OrigFullCd);
            }
        }
    }

    private OriginalCardStats EnsureCardBaseline(IZECard card)
    {
        IntPtr ptr = card.Pointer;
        if (_originalCards.TryGetValue(ptr, out var existing)) return existing;

        int origCost = card.theSeedCost;
        float origCd = card.fullCD;

        // If the card was instantiated while an override was active, recover the un-modded baseline cost/CD.
        if (TryGetEffectiveOverride(card.theZombieType, out var ov) &&
            _originalZombieData.TryGetValue(card.theZombieType, out var baseline))
        {
            if (card.theSeedCost == ov.Cost) origCost = baseline.Cost;
            if (Mathf.Approximately(card.fullCD, ov.Cooldown)) origCd = baseline.Cooldown;
        }

        var created = new OriginalCardStats
        {
            OrigSeedCost = origCost,
            OrigFullCd = origCd,
            PrevFullCd = card.fullCD,
        };
        _originalCards[ptr] = created;
        return created;
    }

    private static void ApplyCostAndCdToCard(IZECard card, OriginalCardStats orig, int targetCost, float targetFullCd)
    {
        card.theSeedCost = targetCost;

        float prevFullCd = orig.PrevFullCd > 0f ? orig.PrevFullCd : card.fullCD;
        if (card.CD >= prevFullCd - 0.001f || prevFullCd <= 0f)
        {
            card.fullCD = targetFullCd;
            card.CD = targetFullCd;
        }
        else
        {
            float progress = Mathf.Clamp01(card.CD / prevFullCd);
            card.fullCD = targetFullCd;
            card.CD = targetFullCd * progress;
        }

        orig.PrevFullCd = targetFullCd;

        if (card.transform != null && card.transform.childCount > 1)
        {
            Transform costTransform = card.transform.GetChild(1);
            TextMeshProUGUI costText = costTransform != null ? costTransform.GetComponent<TextMeshProUGUI>() : null;
            if (costText != null)
            {
                costText.text = targetCost.ToString();
            }
        }
    }

    #endregion

    #region Harmony Patches

    [HarmonyPatch(typeof(ZombieDataManager), nameof(ZombieDataManager.LoadData))]
    private static class ZombieDataManagerLoadDataPatch
    {
        [HarmonyPostfix]
        private static void Postfix()
        {
            if (Instance == null) return;

            Instance._mutatedManagerTypes.Clear();
            Instance.CaptureBaselinesIfReady(forceRecapture: true);
            Instance.RefreshAllSectionControls();

            if (Instance.Active)
            {
                Instance.RebuildOverrideCache();
                Instance.SyncManagerDictionary();
            }
        }
    }

    [HarmonyPatch(typeof(ZombieDataManager), nameof(ZombieDataManager.GetZombieData))]
    private static class ZombieDataManagerGetZombieDataPatch
    {
        [HarmonyPostfix]
        private static void Postfix(ZombieType zombieType, ref ZombieDataManager.ZombieData __result)
        {
            if (Instance == null || !Instance.Active) return;
            if (!Instance.TryGetEffectiveOverride(zombieType, out var ov)) return;

            // Clone instead of mutating __result in place so LevelData.ZombieDatas entries remain intact.
            var clone = __result != null ? new ZombieDataManager.ZombieData(__result) : new ZombieDataManager.ZombieData();
            clone.theZombieType = zombieType;
            ov.ApplyToData(clone);
            __result = clone;
        }
    }

    [HarmonyPatch(typeof(CreateZombie), nameof(CreateZombie.GetZombieData))]
    private static class CreateZombieGetZombieDataPatch
    {
        [HarmonyPrefix]
        private static void Prefix(Zombie zombie, ZombieType theZombieType, ref GetZombieDataCallState __state)
        {
            __state = default;
            if (Instance == null || !Instance.Active) return;

            Instance.CaptureBaselinesIfReady();
            var dict = ZombieDataManager.zombieDataDic;
            if (dict == null || !dict.TryGetValue(theZombieType, out var entry) || entry == null) return;

            if (!Instance._overrideCache.TryGetValue(theZombieType, out var ov)) return;

            if (!Instance.OverrideLevelDataSetting.Value &&
                TryGetLevelDataZombieOverride(theZombieType, out _))
            {
                // LevelData defines this zombie and OverrideLevelData is OFF: temporarily restore vanilla baseline.
                if (Instance._originalZombieData.TryGetValue(theZombieType, out var baseline))
                {
                    baseline.ApplyToData(entry);
                    __state.SwappedToOriginal = true;
                    __state.ZombieType = theZombieType;
                    __state.OverrideData = ov;
                }
            }
        }

        [HarmonyPostfix]
        private static void Postfix(Zombie zombie, ZombieType theZombieType, ref GetZombieDataCallState __state)
        {
            if (Instance == null) return;

            var dict = ZombieDataManager.zombieDataDic;
            if (__state.SwappedToOriginal)
            {
                if (dict != null && dict.TryGetValue(__state.ZombieType, out var entry) && entry != null)
                {
                    __state.OverrideData?.ApplyToData(entry);
                }
            }
        }
    }

    [HarmonyPatch(typeof(CreateZombie), nameof(CreateZombie.SetZombieWithMindControl))]
    private static class CreateZombieSetZombieWithMindControlPatch
    {
        [HarmonyPostfix]
        private static void Postfix(Zombie __result)
        {
            if (__result != null)
            {
                ZombieStatManager.Register(__result);
            }
        }
    }

    [HarmonyPatch(typeof(Zombie), nameof(Zombie.SetMindControl))]
    private static class ZombieSetMindControlPatch
    {
        [HarmonyPostfix]
        private static void Postfix(Zombie __instance)
        {
            if (__instance != null)
            {
                ZombieStatManager.Evaluate(__instance);
            }
        }
    }

    [HarmonyPatch(typeof(IZECard), nameof(IZECard.Start))]
    private static class IZECardStartPatch
    {
        [HarmonyPrefix]
        private static void Prefix(IZECard __instance)
        {
            if (Instance == null || !Instance.Active || __instance == null) return;

            OriginalCardStats orig = Instance.EnsureCardBaseline(__instance);
            if (Instance.TryGetEffectiveOverride(__instance.theZombieType, out var ov))
            {
                ApplyCostAndCdToCard(__instance, orig, ov.Cost, ov.Cooldown);
            }
        }
    }

    #endregion
}
