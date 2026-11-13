using HarmonyLib;
using System;
using System.Collections.Generic;
using UnityEngine;





#if MELONLOADER || RELEASE_MELON
using Il2Cpp;
#endif

namespace Magnetar_Client.Modules.Helpers;

public enum PlantStatType
{
    MaxHealth,
    Damage,
    AttackInterval,
    ProduceInterval,
    AnimationSpeed,
    Scale
}

/// <summary>
/// Centralized stat manager for Plants.
/// Maintains true plant baselines and stacks client modifiers deterministically:
/// Final = (Base * Product(Multiply)) + Sum(Add)
/// Seamlessly synchronizes with in-game native adders (Plant.ModifyHealth, Plant.ModifyDamage).
/// </summary>
public static class PlantStatManager
{
    private sealed class PlantTrackedData
    {
        public Plant Plant;
        public int BaseMaxHealth;
        public int BaseDamage;
        public float BaseAttackInterval;
        public float BaseProduceInterval;
        public float BaseAnimationSpeed;
        public Vector3 BaseScale;

        public int LastMaxHealth;
    }

    private readonly struct Modifier
    {
        public readonly string Module;
        public readonly PlantStatType Stat;
        public readonly ModifierOp Op;
        public readonly float Value;
        public readonly Func<Plant, float> ValueSelector;
        public readonly Func<Plant, bool> Filter;

        public Modifier(string module, PlantStatType stat, ModifierOp op, float value, Func<Plant, float> valueSelector, Func<Plant, bool> filter)
        {
            Module = module;
            Stat = stat;
            Op = op;
            Value = value;
            ValueSelector = valueSelector;
            Filter = filter;
        }
    }

    private static readonly Dictionary<int, PlantTrackedData> _trackedPlants = new();
    private static readonly Dictionary<(string Module, PlantStatType Stat), Modifier> _modifiers = new();
    private static bool _initialized;
    internal static bool _isEvaluatingInternally;

    static PlantStatManager()
    {
        Init();
    }

    public static void Init()
    {
        if (_initialized) return;
        _initialized = true;

        GameData.OnPlantSpawned += Register;
        GameData.OnPlantDie += Unregister;
        GameData.Actions.OnBoardDestroyed += Clear;
    }

    public static void Clear()
    {
        _trackedPlants.Clear();
    }

    public static void Register(Plant p)
    {
        if (p == null) return;
        int id = p.GetInstanceID();

        if (_trackedPlants.ContainsKey(id)) return;

        int baseMaxHp = Math.Max(1, p.thePlantMaxHealth);
        int baseDmg = Math.Max(0, p.attackDamage);
        float baseAtkInterval = p.thePlantAttackInterval > 0f ? p.thePlantAttackInterval : 1.4f;
        float baseProdInterval = p.thePlantProduceInterval > 0f ? p.thePlantProduceInterval : 24f;
        float baseAnimSpeed = 1f;
        Vector3 baseScale = p.transform != null ? p.transform.localScale : Vector3.one;

        var tracked = new PlantTrackedData
        {
            Plant = p,
            BaseMaxHealth = baseMaxHp,
            BaseDamage = baseDmg,
            BaseAttackInterval = baseAtkInterval,
            BaseProduceInterval = baseProdInterval,
            BaseAnimationSpeed = baseAnimSpeed,
            BaseScale = baseScale,
            LastMaxHealth = baseMaxHp,
        };

        _trackedPlants[id] = tracked;
        EvaluatePlant(tracked);
    }

    public static void Unregister(Plant p)
    {
        if (p == null) return;
        _trackedPlants.Remove(p.GetInstanceID());
    }

    public static void SetModifier(string module, PlantStatType stat, ModifierOp op, float value, Func<Plant, bool> filter = null)
    {
        if (string.IsNullOrEmpty(module)) return;

        _modifiers[(module, stat)] = new Modifier(module, stat, op, value, null, filter);
        EvaluateAll();
    }

    public static void SetModifier(string module, PlantStatType stat, ModifierOp op, Func<Plant, float> valueSelector, Func<Plant, bool> filter = null)
    {
        if (string.IsNullOrEmpty(module) || valueSelector == null) return;

        _modifiers[(module, stat)] = new Modifier(module, stat, op, 0f, valueSelector, filter);
        EvaluateAll();
    }

    public static void RemoveModifier(string module, PlantStatType stat)
    {
        if (string.IsNullOrEmpty(module)) return;

        if (_modifiers.Remove((module, stat)))
        {
            EvaluateAll();
        }
    }

    public static void AddBase(Plant p, PlantStatType stat, float amount)
    {
        if (p == null) return;
        if (!_trackedPlants.TryGetValue(p.GetInstanceID(), out var tracked))
        {
            Register(p);
            if (!_trackedPlants.TryGetValue(p.GetInstanceID(), out tracked)) return;
        }

        switch (stat)
        {
            case PlantStatType.MaxHealth:
                tracked.BaseMaxHealth = Math.Max(1, tracked.BaseMaxHealth + Mathf.RoundToInt(amount));
                break;
            case PlantStatType.Damage:
                tracked.BaseDamage = Math.Max(0, tracked.BaseDamage + Mathf.RoundToInt(amount));
                break;
            case PlantStatType.AttackInterval:
                tracked.BaseAttackInterval = Math.Max(0.01f, tracked.BaseAttackInterval + amount);
                break;
            case PlantStatType.ProduceInterval:
                tracked.BaseProduceInterval = Math.Max(0.01f, tracked.BaseProduceInterval + amount);
                break;
            case PlantStatType.AnimationSpeed:
                tracked.BaseAnimationSpeed = Math.Max(0.01f, tracked.BaseAnimationSpeed + amount);
                break;
            case PlantStatType.Scale:
                tracked.BaseScale += new Vector3(amount, amount, 0f);
                break;
        }
    }

    public static void MultiplyBase(Plant p, PlantStatType stat, float factor)
    {
        if (p == null) return;
        if (!_trackedPlants.TryGetValue(p.GetInstanceID(), out var tracked))
        {
            Register(p);
            if (!_trackedPlants.TryGetValue(p.GetInstanceID(), out tracked)) return;
        }

        switch (stat)
        {
            case PlantStatType.MaxHealth:
                tracked.BaseMaxHealth = Math.Max(1, Mathf.RoundToInt(tracked.BaseMaxHealth * factor));
                break;
            case PlantStatType.Damage:
                tracked.BaseDamage = Math.Max(0, Mathf.RoundToInt(tracked.BaseDamage * factor));
                break;
            case PlantStatType.AttackInterval:
                tracked.BaseAttackInterval = Math.Max(0.01f, tracked.BaseAttackInterval * factor);
                break;
            case PlantStatType.ProduceInterval:
                tracked.BaseProduceInterval = Math.Max(0.01f, tracked.BaseProduceInterval * factor);
                break;
            case PlantStatType.AnimationSpeed:
                tracked.BaseAnimationSpeed = Math.Max(0.01f, tracked.BaseAnimationSpeed * factor);
                break;
            case PlantStatType.Scale:
                tracked.BaseScale *= factor;
                break;
        }
    }

    public static void Evaluate(Plant p)
    {
        if (p == null) return;
        if (_trackedPlants.TryGetValue(p.GetInstanceID(), out var tracked))
        {
            EvaluatePlant(tracked);
        }
    }

    public static void EvaluateAll()
    {
        if (!GameData.BoardInstanceIsNull)
        {
            var list = GameData.PlantList;
            for (int i = 0; i < list.Count; i++)
            {
                var p = list[i];
                if (p != null && p.gameObject != null && !_trackedPlants.ContainsKey(p.GetInstanceID()))
                {
                    Register(p);
                }
            }
        }

        List<int> deadKeys = null;
        foreach (var kvp in _trackedPlants)
        {
            var tracked = kvp.Value;
            if (tracked.Plant == null || tracked.Plant.gameObject == null)
            {
                deadKeys ??= new List<int>();
                deadKeys.Add(kvp.Key);
                continue;
            }

            EvaluatePlant(tracked);
        }

        if (deadKeys != null)
        {
            for (int i = 0; i < deadKeys.Count; i++)
            {
                _trackedPlants.Remove(deadKeys[i]);
            }
        }
    }

    private static void EvaluatePlant(PlantTrackedData tracked)
    {
        Plant p = tracked.Plant;
        if (p == null || p.gameObject == null) return;

        _isEvaluatingInternally = true;
        try
        {
            // 1. MaxHealth
            ComputeModifiers(p, PlantStatType.MaxHealth, out float hpMult, out float hpAdd);
            int newMaxHp = Math.Max(1, Mathf.RoundToInt((tracked.BaseMaxHealth * hpMult) + hpAdd));
            int prevMaxHp = tracked.LastMaxHealth > 0 ? tracked.LastMaxHealth : (p.thePlantMaxHealth > 0 ? p.thePlantMaxHealth : newMaxHp);
            tracked.LastMaxHealth = newMaxHp;
            p.thePlantMaxHealth = newMaxHp;

            if (p.thePlantHealth > 0)
            {
                float ratio = prevMaxHp > 0 ? (float)p.thePlantHealth / prevMaxHp : 1f;
                p.thePlantHealth = Math.Max(1, Mathf.RoundToInt(newMaxHp * ratio));
            }

            // 2. Damage
            ComputeModifiers(p, PlantStatType.Damage, out float dmgMult, out float dmgAdd);
            int newDamage = Math.Max(0, Mathf.RoundToInt((tracked.BaseDamage * dmgMult) + dmgAdd));
            p.attackDamage = newDamage;

            // 3. AttackInterval
            ComputeModifiers(p, PlantStatType.AttackInterval, out float atkIntMult, out float atkIntAdd);
            float newAtkInterval = Math.Max(0.01f, (tracked.BaseAttackInterval * atkIntMult) + atkIntAdd);
            p.thePlantAttackInterval = newAtkInterval;
            if (p.thePlantAttackCountDown > newAtkInterval)
            {
                p.thePlantAttackCountDown = newAtkInterval;
            }

            // 4. ProduceInterval
            ComputeModifiers(p, PlantStatType.ProduceInterval, out float prodMult, out float prodAdd);
            float newProdInterval = Math.Max(0.01f, (tracked.BaseProduceInterval * prodMult) + prodAdd);
            p.thePlantProduceInterval = newProdInterval;
            if (p.thePlantProduceCountDown > newProdInterval)
            {
                p.thePlantProduceCountDown = newProdInterval;
            }

            // 5. AnimationSpeed
            ComputeModifiers(p, PlantStatType.AnimationSpeed, out float animMult, out float animAdd);
            float newAnimSpeed = (tracked.BaseAnimationSpeed * animMult) + animAdd;
            p.thePlantSpeed = newAnimSpeed;
            var animator = p.GetComponent<Animator>();
            if (animator != null && animator.speed != newAnimSpeed)
            {
                animator.speed = newAnimSpeed;
            }

            // 6. Scale
            ComputeModifiers(p, PlantStatType.Scale, out float scMult, out float scAdd);
            Vector3 newScale = (tracked.BaseScale * scMult) + new Vector3(scAdd, scAdd, 0f);
            if (p.transform != null && p.transform.localScale != newScale)
            {
                p.transform.localScale = newScale;
            }
        }
        finally
        {
            _isEvaluatingInternally = false;
        }
    }

    private static void ComputeModifiers(Plant p, PlantStatType stat, out float product, out float sum)
    {
        product = 1f;
        sum = 0f;

        foreach (var kvp in _modifiers)
        {
            Modifier mod = kvp.Value;
            if (mod.Stat != stat) continue;
            if (mod.Filter != null && !mod.Filter(p)) continue;

            float val = mod.ValueSelector != null ? mod.ValueSelector(p) : mod.Value;

            if (mod.Op == ModifierOp.Multiply)
            {
                product *= val;
            }
            else if (mod.Op == ModifierOp.Add)
            {
                sum += val;
            }
        }
    }

    [HarmonyPatch(typeof(Plant))]
    private static class NativePlantAdderSyncPatches
    {
        [HarmonyPatch(nameof(Plant.ModifyHealth))]
        [HarmonyPostfix]
        private static void ModifyHealthPostfix(Plant __instance)
        {
            if (_isEvaluatingInternally || __instance == null) return;
            if (_trackedPlants.TryGetValue(__instance.GetInstanceID(), out var tracked))
            {
                tracked.BaseMaxHealth = Math.Max(1, __instance.thePlantMaxHealth);
                EvaluatePlant(tracked);
            }
        }

        // NOTE: Plant.ModifyDamage takes 'Nullable<float> max'. In Il2CppInterop, hooking methods
        // with System.Nullable<T> parameters crashes the native->managed trampoline with a
        // NullReferenceException in System.Buffer.Memmove when null is passed by native callers.
        // BaseDamage is already captured at plant spawn time post-initialization in Register(Plant).
    }
}
