using System;
using System.Collections.Generic;
using UnityEngine;


#if MELONLOADER || RELEASE_MELON
using Il2Cpp;
#endif

namespace Magnetar_Client.Modules.Helpers;

public enum StatType
{
    MaxHealth,
    Armor1Max,
    Armor2Max,
    Speed,
    AttackDamage,
    Scale
}

public enum ModifierOp
{
    Multiply,
    Add
}

/// <summary>
/// Centralized stat manager for Zombies.
/// Prevents order-dependent corruption by maintaining true spawn-time baselines
/// and calculating final stats deterministically from registered module modifiers:
/// Final = (Base * Product(Multiply)) + Sum(Add)
/// </summary>
public static class ZombieStatManager
{
    private sealed class ZombieTrackedData
    {
        public Zombie Zombie;
        public long BaseMaxHealth;
        public int BaseArmor1Max;
        public int BaseArmor2Max;
        public float BaseSpeed;
        public int BaseAttackDamage;
        public Vector3 BaseScale;

        public long LastMaxHealth;
        public int LastArmor1Max;
        public int LastArmor2Max;
    }

    private readonly struct Modifier
    {
        public readonly string Module;
        public readonly StatType Stat;
        public readonly ModifierOp Op;
        public readonly float Value;
        public readonly Func<Zombie, float> ValueSelector;
        public readonly Func<Zombie, bool> Filter;

        public Modifier(string module, StatType stat, ModifierOp op, float value, Func<Zombie, float> valueSelector, Func<Zombie, bool> filter)
        {
            Module = module;
            Stat = stat;
            Op = op;
            Value = value;
            ValueSelector = valueSelector;
            Filter = filter;
        }
    }

    private static readonly Dictionary<int, ZombieTrackedData> _trackedZombies = new();
    private static readonly Dictionary<(string Module, StatType Stat), Modifier> _modifiers = new();
    private static bool _initialized;

    static ZombieStatManager()
    {
        Init();
    }

    public static void Init()
    {
        if (_initialized) return;
        _initialized = true;

        GameData.OnZombieSpawned += Register;
        GameData.OnZombieDie += Unregister;
        GameData.Actions.OnBoardDestroyed += Clear;
    }

    public static void Clear()
    {
        _trackedZombies.Clear();
    }

    /// <summary>
    /// Captures the true baseline of a spawned zombie before any module modifies it,
    /// and immediately evaluates currently active modifiers.
    /// </summary>
    public static void Register(Zombie z)
    {
        if (z == null) return;
        int id = z.GetInstanceID();

        if (_trackedZombies.ContainsKey(id)) return;

        long baseMaxHp = Math.Max(1L, z.theMaxHealth);
        int baseArmor1 = Math.Max(0, z.theFirstArmorMaxHealth);
        int baseArmor2 = Math.Max(0, z.theSecondArmorMaxHealth);
        float baseSpeed = z.uniqueSpeed != 0f ? z.uniqueSpeed : 1f;
        int baseAttack = Math.Max(0, z.theAttackDamage);
        Vector3 baseScale = z.transform != null ? z.transform.localScale : Vector3.one;

        var tracked = new ZombieTrackedData
        {
            Zombie = z,
            BaseMaxHealth = baseMaxHp,
            BaseArmor1Max = baseArmor1,
            BaseArmor2Max = baseArmor2,
            BaseSpeed = baseSpeed,
            BaseAttackDamage = baseAttack,
            BaseScale = baseScale,
            LastMaxHealth = baseMaxHp,
            LastArmor1Max = baseArmor1,
            LastArmor2Max = baseArmor2,
        };

        _trackedZombies[id] = tracked;
        EvaluateZombie(tracked);
    }

    /// <summary>
    /// Drops state when a zombie dies or is despawned.
    /// </summary>
    public static void Unregister(Zombie z)
    {
        if (z == null) return;
        _trackedZombies.Remove(z.GetInstanceID());
    }

    /// <summary>
    /// Sets or updates a modifier keyed by (module, stat).
    /// Re-evaluates all active zombies.
    /// </summary>
    public static void SetModifier(string module, StatType stat, ModifierOp op, float value, Func<Zombie, bool> filter = null)
    {
        if (string.IsNullOrEmpty(module)) return;

        _modifiers[(module, stat)] = new Modifier(module, stat, op, value, null, filter);
        EvaluateAll();
    }

    /// <summary>
    /// Sets or updates a modifier with a dynamic value selector per zombie.
    /// Re-evaluates all active zombies.
    /// </summary>
    public static void SetModifier(string module, StatType stat, ModifierOp op, Func<Zombie, float> valueSelector, Func<Zombie, bool> filter = null)
    {
        if (string.IsNullOrEmpty(module) || valueSelector == null) return;

        _modifiers[(module, stat)] = new Modifier(module, stat, op, 0f, valueSelector, filter);
        EvaluateAll();
    }

    /// <summary>
    /// Removes a modifier for (module, stat).
    /// Re-evaluates all active zombies.
    /// </summary>
    public static void RemoveModifier(string module, StatType stat)
    {
        if (string.IsNullOrEmpty(module)) return;

        if (_modifiers.Remove((module, stat)))
        {
            EvaluateAll();
        }
    }

    /// <summary>
    /// Permanently modifies the base stats of a zombie (e.g. when absorbing mass in ClampZombies).
    /// </summary>
    public static void AddBase(Zombie z, StatType stat, float amount)
    {
        if (z == null) return;
        if (!_trackedZombies.TryGetValue(z.GetInstanceID(), out var tracked))
        {
            Register(z);
            if (!_trackedZombies.TryGetValue(z.GetInstanceID(), out tracked)) return;
        }

        switch (stat)
        {
            case StatType.MaxHealth:
                tracked.BaseMaxHealth = Math.Max(1L, tracked.BaseMaxHealth + (long)Math.Round(amount));
                break;
            case StatType.Armor1Max:
                tracked.BaseArmor1Max = Math.Max(0, tracked.BaseArmor1Max + Mathf.RoundToInt(amount));
                break;
            case StatType.Armor2Max:
                tracked.BaseArmor2Max = Math.Max(0, tracked.BaseArmor2Max + Mathf.RoundToInt(amount));
                break;
            case StatType.Speed:
                tracked.BaseSpeed += amount;
                break;
            case StatType.AttackDamage:
                tracked.BaseAttackDamage = Math.Max(0, tracked.BaseAttackDamage + Mathf.RoundToInt(amount));
                break;
            case StatType.Scale:
                tracked.BaseScale += new Vector3(amount, amount, 0f);
                break;
        }
    }

    /// <summary>
    /// Permanently scales the base stats of a zombie (e.g. speed reduction in ClampZombies).
    /// </summary>
    public static void MultiplyBase(Zombie z, StatType stat, float factor)
    {
        if (z == null) return;
        if (!_trackedZombies.TryGetValue(z.GetInstanceID(), out var tracked))
        {
            Register(z);
            if (!_trackedZombies.TryGetValue(z.GetInstanceID(), out tracked)) return;
        }

        switch (stat)
        {
            case StatType.MaxHealth:
                tracked.BaseMaxHealth = Math.Max(1L, (long)Math.Round(tracked.BaseMaxHealth * factor));
                break;
            case StatType.Armor1Max:
                tracked.BaseArmor1Max = Math.Max(0, Mathf.RoundToInt(tracked.BaseArmor1Max * factor));
                break;
            case StatType.Armor2Max:
                tracked.BaseArmor2Max = Math.Max(0, Mathf.RoundToInt(tracked.BaseArmor2Max * factor));
                break;
            case StatType.Speed:
                tracked.BaseSpeed *= factor;
                break;
            case StatType.AttackDamage:
                tracked.BaseAttackDamage = Math.Max(0, Mathf.RoundToInt(tracked.BaseAttackDamage * factor));
                break;
            case StatType.Scale:
                tracked.BaseScale *= factor;
                break;
        }
    }

    /// <summary>
    /// Explicitly evaluates active modifiers on a single zombie.
    /// </summary>
    public static void Evaluate(Zombie z)
    {
        if (z == null) return;
        if (_trackedZombies.TryGetValue(z.GetInstanceID(), out var tracked))
        {
            EvaluateZombie(tracked);
        }
    }

    public static void EvaluateAll()
    {
        // Auto-register any alive zombies from GameData that haven't been registered yet
        if (!GameData.BoardInstanceIsNull)
        {
            var list = GameData.ZombieList;
            for (int i = 0; i < list.Count; i++)
            {
                var z = list[i];
                if (z != null && z.gameObject != null && !_trackedZombies.ContainsKey(z.GetInstanceID()))
                {
                    Register(z);
                }
            }
        }

        List<int> deadKeys = null;
        foreach (var kvp in _trackedZombies)
        {
            var tracked = kvp.Value;
            if (tracked.Zombie == null || tracked.Zombie.gameObject == null)
            {
                deadKeys ??= new List<int>();
                deadKeys.Add(kvp.Key);
                continue;
            }

            EvaluateZombie(tracked);
        }

        if (deadKeys != null)
        {
            for (int i = 0; i < deadKeys.Count; i++)
            {
                _trackedZombies.Remove(deadKeys[i]);
            }
        }
    }

    private static void EvaluateZombie(ZombieTrackedData tracked)
    {
        Zombie z = tracked.Zombie;
        if (z == null || z.gameObject == null || z.beforeDying) return;

        // 1. MaxHealth
        ComputeModifiers(z, StatType.MaxHealth, out float hpMult, out float hpAdd);
        long newMaxHp = Math.Max(1L, (long)Math.Round((tracked.BaseMaxHealth * hpMult) + hpAdd));
        long prevMaxHp = tracked.LastMaxHealth > 0 ? tracked.LastMaxHealth : (z.theMaxHealth > 0 ? z.theMaxHealth : newMaxHp);
        tracked.LastMaxHealth = newMaxHp;
        z.theMaxHealth = newMaxHp;

        if (z.theHealth > 0)
        {
            float ratio = prevMaxHp > 0 ? (float)z.theHealth / prevMaxHp : 1f;
            z.theHealth = Math.Max(1L, (long)Math.Round(newMaxHp * ratio));
        }
        z.UpdateHealthText();

        // 2. Armor1Max
        ComputeModifiers(z, StatType.Armor1Max, out float a1Mult, out float a1Add);
        int newArmor1Max = Math.Max(0, Mathf.RoundToInt((tracked.BaseArmor1Max * a1Mult) + a1Add));
        int prevArmor1Max = tracked.LastArmor1Max > 0 ? tracked.LastArmor1Max : (z.theFirstArmorMaxHealth > 0 ? z.theFirstArmorMaxHealth : newArmor1Max);
        tracked.LastArmor1Max = newArmor1Max;
        z.theFirstArmorMaxHealth = newArmor1Max;

        if (z.theFirstArmorHealth > 0)
        {
            float ratio = prevArmor1Max > 0 ? (float)z.theFirstArmorHealth / prevArmor1Max : 1f;
            z.theFirstArmorHealth = Math.Max(1, Mathf.RoundToInt(newArmor1Max * ratio));
        }

        // 3. Armor2Max
        ComputeModifiers(z, StatType.Armor2Max, out float a2Mult, out float a2Add);
        int newArmor2Max = Math.Max(0, Mathf.RoundToInt((tracked.BaseArmor2Max * a2Mult) + a2Add));
        int prevArmor2Max = tracked.LastArmor2Max > 0 ? tracked.LastArmor2Max : (z.theSecondArmorMaxHealth > 0 ? z.theSecondArmorMaxHealth : newArmor2Max);
        tracked.LastArmor2Max = newArmor2Max;
        z.theSecondArmorMaxHealth = newArmor2Max;

        if (z.theSecondArmorHealth > 0)
        {
            float ratio = prevArmor2Max > 0 ? (float)z.theSecondArmorHealth / prevArmor2Max : 1f;
            z.theSecondArmorHealth = Math.Max(1, Mathf.RoundToInt(newArmor2Max * ratio));
        }

        // 4. Speed
        ComputeModifiers(z, StatType.Speed, out float spdMult, out float spdAdd);
        float newSpeed = (tracked.BaseSpeed * spdMult) + spdAdd;
        if (z.uniqueSpeed != newSpeed)
        {
            z.uniqueSpeed = newSpeed;
        }

        // 5. AttackDamage
        ComputeModifiers(z, StatType.AttackDamage, out float atkMult, out float atkAdd);
        int newAttack = Math.Max(0, Mathf.RoundToInt((tracked.BaseAttackDamage * atkMult) + atkAdd));
        z.theAttackDamage = newAttack;

        // 6. Scale
        ComputeModifiers(z, StatType.Scale, out float scMult, out float scAdd);
        Vector3 newScale = (tracked.BaseScale * scMult) + new Vector3(scAdd, scAdd, 0f);
        if (z.transform != null && z.transform.localScale != newScale)
        {
            z.transform.localScale = newScale;
        }
    }

    private static void ComputeModifiers(Zombie z, StatType stat, out float product, out float sum)
    {
        product = 1f;
        sum = 0f;

        foreach (var kvp in _modifiers)
        {
            Modifier mod = kvp.Value;
            if (mod.Stat != stat) continue;
            if (mod.Filter != null && !mod.Filter(z)) continue;

            float val = mod.ValueSelector != null ? mod.ValueSelector(z) : mod.Value;

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
}
