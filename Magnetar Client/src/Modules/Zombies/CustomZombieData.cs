using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Magnetar_Client.UI.Setting;
using Magnetar_Client.Utils;
using UnityEngine;

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

    private sealed class OriginalZombieStats
    {
        public long OrigMaxHealth;
        public int OrigFirstArmorMax;
        public int OrigSecondArmorMax;
        public int OrigAttackDamage;
        public float OrigArmor;
        public float OrigUniqueSpeed;

        public long PrevMaxHealth;
        public int PrevFirstArmorMax;
        public int PrevSecondArmorMax;

        public double HealthScale = 1.0;
        public double FirstArmorScale = 1.0;
        public double SecondArmorScale = 1.0;
        public double AttackScale = 1.0;
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
        public bool TemporarilyRestoredAttack;
        public ZombieType ZombieType;
        public int BaselineAttack;
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

    private readonly Dictionary<IntPtr, OriginalZombieStats> _originalZombies = new();
    private readonly Dictionary<IntPtr, OriginalCardStats> _originalCards = new();
    private readonly HashSet<IntPtr> _spawnedWithOverride = new();
    private readonly Dictionary<IntPtr, double> _pendingAttackScales = new();

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

        GameData.OnZombieSpawned += HandleZombieSpawned;
        GameData.OnZombieDie += HandleZombieDied;
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
        RestoreAllLiveEntities();
        _spawnedWithOverride.Clear();
        _pendingAttackScales.Clear();
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
            SyncAllLiveEntities();
        }
        else
        {
            RestoreManagerDictionary();
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

    private void HandleZombieSpawned(Zombie zombie)
    {
        if (zombie == null) return;

        CaptureBaselinesIfReady();
        IntPtr ptr = zombie.Pointer;
        bool spawnedViaOverride = _spawnedWithOverride.Remove(ptr);
        double pendingAtkScale = 1.0;
        bool hasPendingAtkScale = _pendingAttackScales.Remove(ptr, out pendingAtkScale);

        if (_originalZombies.ContainsKey(ptr))
        {
            // Already initialized on SetZombie before Start() ran.
            return;
        }

        ZombieType type = zombie.theZombieType;
        if (spawnedViaOverride &&
            TryGetEffectiveOverride(type, out var spawnOv) &&
            _originalZombieData.TryGetValue(type, out var baseline))
        {
            double hpScale = spawnOv.MaxHealth > 0 ? (double)zombie.theMaxHealth / spawnOv.MaxHealth : 1.0;
            double firstScale = spawnOv.FirstArmorMaxHealth > 0
                ? (double)zombie.theFirstArmorMaxHealth / spawnOv.FirstArmorMaxHealth
                : hpScale;
            double secondScale = spawnOv.SecondArmorMaxHealth > 0
                ? (double)zombie.theSecondArmorMaxHealth / spawnOv.SecondArmorMaxHealth
                : hpScale;
            double atkScale = hasPendingAtkScale
                ? pendingAtkScale
                : (spawnOv.AttackDamage > 0 ? (double)zombie.theAttackDamage / spawnOv.AttackDamage : hpScale);

            var orig = new OriginalZombieStats
            {
                OrigMaxHealth = Math.Max(1L, (long)Math.Round(baseline.MaxHealth * hpScale)),
                OrigFirstArmorMax = Math.Max(0, (int)Math.Round(baseline.FirstArmorMaxHealth * firstScale)),
                OrigSecondArmorMax = Math.Max(0, (int)Math.Round(baseline.SecondArmorMaxHealth * secondScale)),
                OrigAttackDamage = Math.Max(0, (int)Math.Round(baseline.AttackDamage * atkScale)),
                OrigArmor = baseline.Armor,
                OrigUniqueSpeed = zombie.uniqueSpeed,
                PrevMaxHealth = Math.Max(1L, zombie.theMaxHealth),
                PrevFirstArmorMax = Math.Max(0, zombie.theFirstArmorMaxHealth),
                PrevSecondArmorMax = Math.Max(0, zombie.theSecondArmorMaxHealth),
                HealthScale = hpScale,
                FirstArmorScale = firstScale,
                SecondArmorScale = secondScale,
                AttackScale = atkScale,
            };

            _originalZombies[ptr] = orig;
            zombie.uniqueSpeed = orig.OrigUniqueSpeed * spawnOv.SpeedMultiplier;
            return;
        }

        var captured = CaptureLiveZombieBaseline(zombie);
        _originalZombies[ptr] = captured;

        if (TryGetEffectiveOverride(type, out var activeOv))
        {
            ApplyOverrideToZombie(zombie, captured, activeOv);
        }
    }

    private OriginalZombieStats CaptureLiveZombieBaseline(Zombie zombie)
    {
        ZombieType type = zombie.theZombieType;
        _originalZombieData.TryGetValue(type, out var baseline);

        double hpScale = baseline != null && baseline.MaxHealth > 0
            ? (double)zombie.theMaxHealth / baseline.MaxHealth
            : 1.0;
        double firstScale = baseline != null && baseline.FirstArmorMaxHealth > 0
            ? (double)zombie.theFirstArmorMaxHealth / baseline.FirstArmorMaxHealth
            : hpScale;
        double secondScale = baseline != null && baseline.SecondArmorMaxHealth > 0
            ? (double)zombie.theSecondArmorMaxHealth / baseline.SecondArmorMaxHealth
            : hpScale;
        double atkScale = baseline != null && baseline.AttackDamage > 0
            ? (double)zombie.theAttackDamage / baseline.AttackDamage
            : 1.0;

        return new OriginalZombieStats
        {
            OrigMaxHealth = Math.Max(1L, zombie.theMaxHealth),
            OrigFirstArmorMax = Math.Max(0, zombie.theFirstArmorMaxHealth),
            OrigSecondArmorMax = Math.Max(0, zombie.theSecondArmorMaxHealth),
            OrigAttackDamage = Math.Max(0, zombie.theAttackDamage),
            OrigArmor = zombie.theArmor,
            OrigUniqueSpeed = zombie.uniqueSpeed,
            PrevMaxHealth = Math.Max(1L, zombie.theMaxHealth),
            PrevFirstArmorMax = Math.Max(0, zombie.theFirstArmorMaxHealth),
            PrevSecondArmorMax = Math.Max(0, zombie.theSecondArmorMaxHealth),
            HealthScale = hpScale,
            FirstArmorScale = firstScale,
            SecondArmorScale = secondScale,
            AttackScale = atkScale,
        };
    }

    private void HandleZombieDied(Zombie zombie)
    {
        if (zombie == null) return;

        IntPtr ptr = zombie.Pointer;
        _originalZombies.Remove(ptr);
        _spawnedWithOverride.Remove(ptr);
        _pendingAttackScales.Remove(ptr);
    }

    private void ClearLiveCaches()
    {
        _originalZombies.Clear();
        _originalCards.Clear();
        _spawnedWithOverride.Clear();
        _pendingAttackScales.Clear();
    }

    private void SyncAllLiveEntities()
    {
        if (GameData.BoardInstanceIsNull) return;

        foreach (Zombie zombie in GameData.ZombieList)
        {
            if (zombie == null || zombie.gameObject == null || zombie.beforeDying) continue;

            IntPtr ptr = zombie.Pointer;
            if (!_originalZombies.TryGetValue(ptr, out var orig))
            {
                orig = CaptureLiveZombieBaseline(zombie);
                _originalZombies[ptr] = orig;
            }

            if (TryGetEffectiveOverride(zombie.theZombieType, out var ov))
            {
                ApplyOverrideToZombie(zombie, orig, ov);
            }
            else
            {
                RestoreZombie(zombie, orig);
            }
        }

        SyncAllIZECards();
    }

    private void RestoreAllLiveEntities()
    {
        if (!GameData.BoardInstanceIsNull)
        {
            foreach (Zombie zombie in GameData.ZombieList)
            {
                if (zombie == null || zombie.gameObject == null) continue;
                if (_originalZombies.TryGetValue(zombie.Pointer, out var orig))
                {
                    RestoreZombie(zombie, orig);
                }
            }

            RestoreAllIZECards();
        }
    }

    private static void ApplyOverrideToZombie(Zombie zombie, OriginalZombieStats orig, ZombieOverrideData ov)
    {
        long targetMaxHp = Math.Max(1L, (long)Math.Round(ov.MaxHealth * orig.HealthScale));
        int targetArmor1Max = Math.Max(0, (int)Math.Round(ov.FirstArmorMaxHealth * orig.FirstArmorScale));
        int targetArmor2Max = Math.Max(0, (int)Math.Round(ov.SecondArmorMaxHealth * orig.SecondArmorScale));
        int targetAttack = Math.Max(0, (int)Math.Round(ov.AttackDamage * orig.AttackScale));

        ApplyHealthAndArmors(zombie, orig, targetMaxHp, targetArmor1Max, targetArmor2Max);

        zombie.theAttackDamage = targetAttack;
        zombie.theArmor = ov.Armor;
        zombie.uniqueSpeed = orig.OrigUniqueSpeed * ov.SpeedMultiplier;

        RefreshZombieHealthDisplay(zombie);
    }

    private static void RestoreZombie(Zombie zombie, OriginalZombieStats orig)
    {
        ApplyHealthAndArmors(zombie, orig, orig.OrigMaxHealth, orig.OrigFirstArmorMax, orig.OrigSecondArmorMax);

        zombie.theAttackDamage = orig.OrigAttackDamage;
        zombie.theArmor = orig.OrigArmor;
        zombie.uniqueSpeed = orig.OrigUniqueSpeed;

        RefreshZombieHealthDisplay(zombie);
    }

    private static void ApplyHealthAndArmors(
        Zombie zombie,
        OriginalZombieStats orig,
        long targetMaxHp,
        int targetArmor1Max,
        int targetArmor2Max)
    {
        long prevMaxHp = orig.PrevMaxHealth > 0 ? orig.PrevMaxHealth : Math.Max(1L, zombie.theMaxHealth);
        if (zombie.theMaxHealth != targetMaxHp || orig.PrevMaxHealth != targetMaxHp)
        {
            double hpRatio = prevMaxHp > 0 ? Math.Max(0.0, (double)zombie.theHealth / prevMaxHp) : 1.0;
            zombie.theMaxHealth = targetMaxHp;
            if (zombie.theHealth > 0)
            {
                zombie.theHealth = Math.Max(1L, (long)Math.Round(targetMaxHp * hpRatio));
            }
            orig.PrevMaxHealth = targetMaxHp;
        }

        ApplySingleArmor(
            ref orig.PrevFirstArmorMax,
            zombie.theFirstArmor != null,
            zombie.theFirstArmorMaxHealth,
            zombie.theFirstArmorHealth,
            targetArmor1Max,
            out int newFirstMax,
            out int newFirstCur);
        zombie.theFirstArmorMaxHealth = newFirstMax;
        zombie.theFirstArmorHealth = newFirstCur;

        ApplySingleArmor(
            ref orig.PrevSecondArmorMax,
            zombie.theSecondArmor != null,
            zombie.theSecondArmorMaxHealth,
            zombie.theSecondArmorHealth,
            targetArmor2Max,
            out int newSecondMax,
            out int newSecondCur);
        zombie.theSecondArmorMaxHealth = newSecondMax;
        zombie.theSecondArmorHealth = newSecondCur;
    }

    private static void ApplySingleArmor(
        ref int trackedPrevMax,
        bool hasArmorObject,
        int currentMax,
        int currentHealth,
        int targetMax,
        out int resultMax,
        out int resultHealth)
    {
        int prevMax = trackedPrevMax > 0 ? trackedPrevMax : currentMax;
        if (currentMax == targetMax && trackedPrevMax == targetMax)
        {
            resultMax = currentMax;
            resultHealth = currentHealth;
            return;
        }

        if (targetMax <= 0)
        {
            resultMax = 0;
            resultHealth = 0;
        }
        else if (currentHealth > 0)
        {
            double ratio = prevMax > 0 ? Math.Max(0.0, (double)currentHealth / prevMax) : 1.0;
            resultMax = targetMax;
            resultHealth = Math.Max(1, (int)Math.Round(targetMax * ratio));
        }
        else if (prevMax <= 0 && hasArmorObject)
        {
            resultMax = targetMax;
            resultHealth = targetMax;
        }
        else
        {
            // Keep broken armor broken instead of restoring it mid-fight.
            resultMax = targetMax;
            resultHealth = 0;
        }

        trackedPrevMax = targetMax;
    }

    private static void RefreshZombieHealthDisplay(Zombie zombie)
    {
        if (zombie != null && zombie.healthText != null)
        {
            zombie.UpdateHealthText();
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
                return;
            }

            if (ov.AttackDamage == 0 &&
                Instance._originalZombieData.TryGetValue(theZombieType, out var origData) &&
                origData.AttackDamage > 0)
            {
                // Temporarily use baseline attack so we can measure runtime difficulty scaling before zeroing it.
                entry.theAttackDamage = origData.AttackDamage;
                __state.TemporarilyRestoredAttack = true;
                __state.ZombieType = theZombieType;
                __state.BaselineAttack = origData.AttackDamage;
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
                return;
            }

            if (zombie == null || !Instance.Active) return;

            if (__state.TemporarilyRestoredAttack)
            {
                if (dict != null && dict.TryGetValue(__state.ZombieType, out var entry) && entry != null)
                {
                    entry.theAttackDamage = 0;
                }

                double atkScale = __state.BaselineAttack > 0
                    ? (double)zombie.theAttackDamage / __state.BaselineAttack
                    : 1.0;
                Instance._pendingAttackScales[zombie.Pointer] = atkScale;
                zombie.theAttackDamage = 0;
            }

            if (Instance.TryGetEffectiveOverride(theZombieType, out _))
            {
                Instance._spawnedWithOverride.Add(zombie.Pointer);
            }
        }
    }

    [HarmonyPatch(typeof(CreateZombie), nameof(CreateZombie.SetZombieWithMindControl))]
    private static class CreateZombieSetZombieWithMindControlPatch
    {
        [HarmonyPostfix]
        private static void Postfix(Zombie __result)
        {
            if (Instance == null || __result == null) return;
            Instance.HandleZombieSpawned(__result);
        }
    }

    [HarmonyPatch(typeof(Zombie), nameof(Zombie.SetMindControl))]
    private static class ZombieSetMindControlPatch
    {
        [HarmonyPostfix]
        private static void Postfix(Zombie __instance)
        {
            if (Instance == null || __instance == null) return;

            if (Instance._originalZombies.TryGetValue(__instance.Pointer, out var orig) &&
                Instance._originalZombieData.TryGetValue(__instance.theZombieType, out var baseline))
            {
                // SetMindControl pulls attack damage unscaled from ZombieDataManager.GetZombieData.
                orig.OrigAttackDamage = baseline.AttackDamage;
                orig.AttackScale = 1.0;

                if (Instance.TryGetEffectiveOverride(__instance.theZombieType, out var ov))
                {
                    __instance.theAttackDamage = ov.AttackDamage;
                }
                else
                {
                    __instance.theAttackDamage = baseline.AttackDamage;
                }
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
