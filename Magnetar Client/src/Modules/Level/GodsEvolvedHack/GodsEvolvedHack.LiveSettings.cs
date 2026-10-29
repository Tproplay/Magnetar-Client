using System;
using System.Collections.Generic;
using Magnetar_Client.UI.Setting;
using static Magnetar_Client.Utils.Translator;

#if MELONLOADER || RELEASE_MELON
using Il2Cpp;
using Il2CppGameLevel.RogueShooting;
using Il2CppUI;
#elif BEPINEX || RELEASE_BEPINEX
using GameLevel.RogueShooting;
using UI;
#endif

namespace Magnetar_Client.Modules;

public partial class GodsEvolvedHack
{
    public interface ILiveSetting
    {
        bool IsLocked { get; set; }
        bool HasCapturedOriginal { get; set; }
        void SetDisabled(bool disabled);
        void SyncFromGame(ShootingManager manager);
        void ApplyToGame(ShootingManager manager);
        void CaptureOriginal(ShootingManager manager);
        void RestoreOriginal(ShootingManager manager);
        void ResetSlider();
    }

    public class LiveFloatSetting : ILiveSetting
    {
        public FloatSetting Setting { get; }
        public bool IsLocked { get; set; }
        public float OriginalValue { get; set; }
        public bool HasCapturedOriginal { get; set; }
        public Func<ShootingManager, float> Getter { get; }
        public Action<ShootingManager, float> Setter { get; }

        public LiveFloatSetting(FloatSetting setting, Func<ShootingManager, float> getter, Action<ShootingManager, float> setter)
        {
            Setting = setting;
            Getter = getter;
            Setter = setter;
        }

        public void SetDisabled(bool disabled) => Setting.IsDisabled = disabled;

        public void SyncFromGame(ShootingManager manager)
        {
            if (manager == null || Setting.DragValue.HasValue) return;
            try { Setting.SetDirect(Getter(manager)); } catch { }
        }

        public void ApplyToGame(ShootingManager manager)
        {
            if (manager == null) return;
            try { Setter?.Invoke(manager, Setting.Value); } catch { }
        }

        public void CaptureOriginal(ShootingManager manager)
        {
            if (manager == null) return;
            try
            {
                OriginalValue = Getter(manager);
                HasCapturedOriginal = true;
            }
            catch { }
        }

        public void RestoreOriginal(ShootingManager manager)
        {
            if (manager == null || !HasCapturedOriginal) return;
            try { Setter?.Invoke(manager, OriginalValue); } catch { }
        }

        public void ResetSlider()
        {
            if (HasCapturedOriginal)
                Setting.SetDirect(OriginalValue);
        }
    }

    public class LiveBoolSetting : ILiveSetting
    {
        public BoolSetting Setting { get; }
        public bool IsLocked { get; set; }
        public bool OriginalValue { get; set; }
        public bool HasCapturedOriginal { get; set; }
        public Func<ShootingManager, bool> Getter { get; }
        public Action<ShootingManager, bool> Setter { get; }

        public LiveBoolSetting(BoolSetting setting, Func<ShootingManager, bool> getter, Action<ShootingManager, bool> setter)
        {
            Setting = setting;
            Getter = getter;
            Setter = setter;
        }

        public void SetDisabled(bool disabled) => Setting.IsDisabled = disabled;

        public void SyncFromGame(ShootingManager manager)
        {
            if (manager == null) return;
            try { Setting.SetDirect(Getter(manager)); } catch { }
        }

        public void ApplyToGame(ShootingManager manager)
        {
            if (manager == null) return;
            try { Setter?.Invoke(manager, Setting.Value); } catch { }
        }

        public void CaptureOriginal(ShootingManager manager)
        {
            if (manager == null) return;
            try
            {
                OriginalValue = Getter(manager);
                HasCapturedOriginal = true;
            }
            catch { }
        }

        public void RestoreOriginal(ShootingManager manager)
        {
            if (manager == null || !HasCapturedOriginal) return;
            try { Setter?.Invoke(manager, OriginalValue); } catch { }
        }

        public void ResetSlider()
        {
            if (HasCapturedOriginal)
                Setting.SetDirect(OriginalValue);
        }
    }

    public class LiveSelectSetting : ILiveSetting
    {
        public SelectSetting Setting { get; }
        public bool IsLocked { get; set; }
        public int OriginalValue { get; set; }
        public bool HasCapturedOriginal { get; set; }
        public Func<ShootingManager, int> Getter { get; }
        public Action<ShootingManager, int> Setter { get; }

        public LiveSelectSetting(SelectSetting setting, Func<ShootingManager, int> getter, Action<ShootingManager, int> setter)
        {
            Setting = setting;
            Getter = getter;
            Setter = setter;
        }

        public void SetDisabled(bool disabled) => Setting.IsDisabled = disabled;

        public void SyncFromGame(ShootingManager manager)
        {
            if (manager == null) return;
            try { Setting.SetDirect(Getter(manager)); } catch { }
        }

        public void ApplyToGame(ShootingManager manager)
        {
            if (manager == null) return;
            try { Setter?.Invoke(manager, Setting.Value); } catch { }
        }

        public void CaptureOriginal(ShootingManager manager)
        {
            if (manager == null) return;
            try
            {
                OriginalValue = Getter(manager);
                HasCapturedOriginal = true;
            }
            catch { }
        }

        public void RestoreOriginal(ShootingManager manager)
        {
            if (manager == null || !HasCapturedOriginal) return;
            try { Setter?.Invoke(manager, OriginalValue); } catch { }
        }

        public void ResetSlider()
        {
            if (HasCapturedOriginal)
                Setting.SetDirect(OriginalValue);
        }
    }

    public class LiveMultiSelectSetting : ILiveSetting
    {
        public MultiSelectSetting Setting { get; }
        public bool IsLocked { get; set; }
        public int OriginalValue { get; set; }
        public bool HasCapturedOriginal { get; set; }
        public Func<ShootingManager, int> Getter { get; }
        public Action<ShootingManager, int> Setter { get; }

        public LiveMultiSelectSetting(MultiSelectSetting setting, Func<ShootingManager, int> getter, Action<ShootingManager, int> setter)
        {
            Setting = setting;
            Getter = getter;
            Setter = setter;
        }

        public void SetDisabled(bool disabled) => Setting.IsDisabled = disabled;

        public void SyncFromGame(ShootingManager manager)
        {
            if (manager == null) return;
            try { Setting.SetDirect(Getter(manager)); } catch { }
        }

        public void ApplyToGame(ShootingManager manager)
        {
            if (manager == null) return;
            try
            {
                if (Setting.SelectedValues.Count > 0)
                    Setter?.Invoke(manager, System.Linq.Enumerable.First(Setting.SelectedValues));
            }
            catch { }
        }

        public void CaptureOriginal(ShootingManager manager)
        {
            if (manager == null) return;
            try
            {
                OriginalValue = Getter(manager);
                HasCapturedOriginal = true;
            }
            catch { }
        }

        public void RestoreOriginal(ShootingManager manager)
        {
            if (manager == null || !HasCapturedOriginal) return;
            try { Setter?.Invoke(manager, OriginalValue); } catch { }
        }

        public void ResetSlider()
        {
            if (HasCapturedOriginal)
                Setting.SetDirect(OriginalValue);
        }
    }

    private LiveFloatSetting CreateLiveFloatSetting(
        string name,
        float min,
        float max,
        float def,
        int decPlaces,
        float trueMin,
        float trueMax,
        Func<ShootingManager, float> getter,
        Action<ShootingManager, float> setter,
        bool isLogarithmic = true)
    {
        var setting = new FloatSetting(name, min, max, def, decPlaces, trueMin, trueMax, false, isLogarithmic);
        setting.IsDisabled = true;

        var live = new LiveFloatSetting(setting, getter, setter);
        setting.OnValueChanged = val => OnUserModified(live, () => live.Setter?.Invoke(ShootingManager.Instance, val));

        allLiveSettings.Add(live);
        return live;
    }

    private LiveBoolSetting CreateLiveBoolSetting(
        string name,
        bool def,
        Func<ShootingManager, bool> getter,
        Action<ShootingManager, bool> setter)
    {
        var setting = new BoolSetting(name, def);
        setting.IsDisabled = true;

        var live = new LiveBoolSetting(setting, getter, setter);
        setting.OnValueChanged = val => OnUserModified(live, () => live.Setter?.Invoke(ShootingManager.Instance, val));

        allLiveSettings.Add(live);
        return live;
    }

    private LiveSelectSetting CreateLiveSelectSetting(
        string name,
        Type enumType,
        int def,
        Func<ShootingManager, int> getter,
        Action<ShootingManager, int> setter)
    {
        var setting = new SelectSetting(name, enumType, def);
        setting.IsDisabled = true;

        var live = new LiveSelectSetting(setting, getter, setter);
        setting.OnSelectionChanged = val => OnUserModified(live, () => live.Setter?.Invoke(ShootingManager.Instance, val));

        allLiveSettings.Add(live);
        return live;
    }

    private LiveMultiSelectSetting CreateLiveMultiSelectSetting(
        string name,
        Type enumType,
        int def,
        Func<ShootingManager, int> getter,
        Action<ShootingManager, int> setter,
        float width = 120f,
        bool showCountText = true)
    {
        var setting = new MultiSelectSetting(name, enumType)
        {
            MaxSelection = 1,
            CustomNames = TranslateEnum(enumType),
            IsDisabled = true,
            Width = width,
            ShowCountText = showCountText
        };
        setting.SetDirect(def);
        setting.DefaultSelectedValues = new HashSet<int> { def };

        var live = new LiveMultiSelectSetting(setting, getter, setter);
        setting.OnSelectionChanged = (val, selected) =>
        {
            if (selected)
            {
                OnUserModified(live, () => live.Setter?.Invoke(ShootingManager.Instance, val));
            }
        };

        allLiveSettings.Add(live);
        return live;
    }

    private LiveFloatSetting CreateWeightSetting(string name, Quality quality, float def)
    {
        var live = CreateLiveFloatSetting(
            name, 0, 100, def, 2, 0, 100,
            m => m.qualityWeights != null && m.qualityWeights.ContainsKey(quality) ? m.qualityWeights[quality] : 0f,
            (m, val) =>
            {
                if (m.qualityWeights != null)
                    m.qualityWeights[quality] = val;
            }
        );
        qualitySettings[quality] = live;
        return live;
    }

    private static float GetGodCoinRewards(ShootingManager m)
    {
        if (m.godCoinRewards != null)
            return m.godCoinRewards.TotalAwarded;
        if (ShootingManager.Data != null)
            return ShootingManager.Data.godCoins;
        return 0f;
    }

    private static void SetGodCoinRewards(ShootingManager m, float v)
    {
        int coins = (int)v;
        if (m.godCoinRewards != null)
            m.godCoinRewards.TotalAwarded = coins;
        if (ShootingManager.Data != null)
            ShootingManager.Data.godCoins = coins;
    }

    private void OnUserModified(ILiveSetting live, Action applyAction)
    {
        if (!Active) return;

        live.IsLocked = true;

        if (ShootingManager.Instance != null)
        {
            applyAction?.Invoke();
        }
    }

    public void CaptureOriginals(ShootingManager manager)
    {
        if (manager == null) return;
        foreach (var ls in allLiveSettings)
        {
            ls.CaptureOriginal(manager);
        }
    }

    public void RestoreOriginals(ShootingManager manager)
    {
        if (manager == null) return;
        foreach (var ls in allLiveSettings)
        {
            ls.RestoreOriginal(manager);
        }
    }

    private void SyncUnlockedFromGame()
    {
        if (ShootingManager.Instance == null) return;

        foreach (var ls in allLiveSettings)
        {
            if (!Active || !ls.IsLocked)
            {
                ls.SyncFromGame(ShootingManager.Instance);
            }
        }
    }
}
