using Magnetar_Client.Utils;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;

namespace Magnetar_Client.Modules
{
    public enum ModuleCategory
    {
        Level,
        Tools,
        Plant,
        Zombie,
        Misc,
        Visual,
        Addon
    }


    public abstract class Module
    {
        /// <summary>
        /// Name to be displayed
        /// </summary>
        public abstract string Name { get; set; }
        /// <summary>
        /// Search hints to be used when searching for the module
        /// </summary>
        public abstract string SearchHints { get; set; }
        /// <summary>
        /// Optional name for mod author. Supports rich text.
        /// </summary>
        public virtual string Author { get; set; } = "";
        /// <summary>
        /// Description for the module. Supports rich text.
        /// </summary>
        public abstract string Description { get; set; }
        /// <summary>
        /// The category to put the module in.
        /// </summary>
        public abstract ModuleCategory Category { get; set; }

        // These will be in Every ModuleManager.
        // Edit if you want a different default keybind or want it to be enabled by default.

        public BindSetting KeyBind = new BindSetting("Keybind");

        public string GetBindString() => KeyBind.GetBindString();

        public List<KeyCode> BindKeys => KeyBind.BindKeys;
        public virtual bool HoldMode { get; set; } = false;
        public virtual bool Active { get; set; } = false;
        /// <summary>
        /// Used to determine whether the setting window of the module is opened.
        /// </summary>
        public virtual bool ShowSettings { get; set; } = false;


        /// <summary>
        /// Used to store all the settings for the module.
        /// </summary>
        public List<Setting> Settings = new List<Setting>();

        public void Toggle()
        {
            Active = !Active;
            if (Active) OnEnable();
            else OnDisable();
        }

        /// <summary>
        /// Runs once when the module is enabled. Runs Before OnUpdateActive.
        /// </summary>
        public virtual void OnEnable() { }
        /// <summary>
        /// Runs once when the module is disabled. Runs After OnUpdateActive.
        /// </summary>
        public virtual void OnDisable() { }


        /// <summary>
        /// Runs every frame regardless of whether the module is active or not.
        /// </summary>
        public virtual void OnUpdate() { if (Active) OnUpdateActive(); }

        /// <summary>
        /// Runs every frame only when the module is active. Will not run if OnUpdate is overridden without calling base.OnUpdate().
        /// </summary>
        public virtual void OnUpdateActive() { }

        /// <summary>
        /// Runs every frame on UnityEngine.OnGUI
        /// </summary>
        public virtual void OnGUI() { }

        /// <summary>
        /// Static method to add settings to the module. Call this in the constructor of your module with all the settings you want to add.
        /// </summary>
        protected void AddSettings(params Setting[] settings)
        {
            Settings.AddRange(settings);
        }
        public virtual bool Initialized { get; set; } = false;

        /// <summary>
        /// Runs When a the mod's language is changed
        /// </summary>
        public virtual void OnLanguageChanged() { }

        public static Dictionary<int, string> TranslatedNames(System.Type enumType)
        {
            var names = Translator.TranslateEnum(enumType);

            foreach (var name in names)
            {
                names[name.Key] = name.Value + $" ({name.Key})";
            }

            return names;

        }

        public virtual float SettingsWidth { get; set; } = Config.ModuleManager.SettingsWidth;

        // Add these category helper methods anywhere inside the ModuleManager class
        public void CreateCategory(string name, bool defaultExpanded = true)
        {
            Settings.Add(new CategorySetting(name, defaultExpanded));
        }

        public void EndCategory()
        {
            Settings.Add(new EndCategorySetting());
        }

        public static bool GetKeyComboDown(List<KeyCode> keyCodes)
        {
            if (keyCodes == null || keyCodes.Count == 0) return false;

            KeyCode triggerKey = keyCodes[keyCodes.Count - 1];
            if (!Input.GetKeyDown(triggerKey)) return false;

            for (int i = 0; i < keyCodes.Count - 1; i++)
            {
                if (!Input.GetKey(keyCodes[i]))
                {
                    return false;
                }
            }
            return true;
        }
    }

    public abstract class Setting
    {
        public string Name;
        public bool IsDisabled { get; set; } = false;
    }

    public class StringSetting : Setting
    {
        private string _value;
        public string DefaultValue;
        public List<string> AutocompleteVars;

        // Callbacks
        public Action<string> OnValueChanging { get; set; }
        public Action<string> OnValueChanged { get; set; }

        public string Value
        {
            get => _value;
            set
            {
                if (IsDisabled) return;

                if (_value != value)
                {
                    OnValueChanging?.Invoke(value); // Pre
                    _value = value;
                    OnValueChanged?.Invoke(_value); // Post
                }
            }
        }

        public StringSetting(string name, string defaultValue, List<string> autocompleteVars = null)
        {
            Name = name;
            _value = defaultValue;
            DefaultValue = defaultValue;
            AutocompleteVars = autocompleteVars;
        }
    }

    public class IntSetting : Setting
    {
        private int _value;
        public int DefaultValue;

        public int Min;
        public int Max;
        public int TrueMin;
        public int TrueMax;

        // Callbacks
        public Action<int> OnValueChanging { get; set; }
        public Action<int> OnValueChanged { get; set; }

        public int Value
        {
            get => _value;
            set
            {
                if (IsDisabled) return;

                if (_value != value)
                {
                    OnValueChanging?.Invoke(value); // Pre
                    _value = value;
                    OnValueChanged?.Invoke(_value); // Post
                }
            }
        }

        public IntSetting(string name, int min, int max, int defaultValue, int trueMin = int.MinValue, int trueMax = int.MaxValue)
        {
            Name = name;
            Min = min;
            Max = max;
            TrueMin = trueMin;
            TrueMax = trueMax;
            _value = System.Math.Max(TrueMin, System.Math.Min(defaultValue, TrueMax));
            DefaultValue = _value;
        }
    }

    public class FloatSetting : Setting
    {
        private float _value;
        public float DefaultValue;

        public float Min;
        public float Max;
        public float TrueMin;
        public float TrueMax;
        public int DecimalPlaces;

        // Callbacks
        public Action<float> OnValueChanging { get; set; }
        public Action<float> OnValueChanged { get; set; }

        public float Value
        {
            get => _value;
            set
            {
                if (IsDisabled) return;

                if (_value != value)
                {
                    OnValueChanging?.Invoke(value); // Pre
                    _value = value;
                    OnValueChanged?.Invoke(_value); // Post
                }
            }
        }

        public FloatSetting(string name, float min, float max, float defaultValue, int decimalPlaces = 1, float trueMin = float.MinValue, float trueMax = float.MaxValue)
        {
            Name = name;
            Min = min;
            Max = max;
            TrueMin = trueMin;
            TrueMax = trueMax;
            _value = UnityEngine.Mathf.Clamp(defaultValue, TrueMin, TrueMax);
            DefaultValue = _value;
            DecimalPlaces = decimalPlaces;
        }
    }

    public class BoolSetting : Setting
    {
        private bool _value;
        public bool DefaultValue;

        // Callbacks
        public Action<bool> OnValueChanging { get; set; }
        public Action<bool> OnValueChanged { get; set; }

        public bool Value
        {
            get => _value;
            set
            {
                if (IsDisabled) return;

                if (_value != value)
                {
                    OnValueChanging?.Invoke(value); // Pre
                    _value = value;
                    OnValueChanged?.Invoke(_value); // Post
                }
            }
        }

        public BoolSetting(string name, bool defaultValue)
        {
            Name = name;
            _value = defaultValue;
            DefaultValue = defaultValue;
        }
    }

    public class BindSetting : Setting
    {
        private List<KeyCode> _bindKeys = new List<KeyCode>();
        public List<KeyCode> DefaultKeys { get; private set; } = new List<KeyCode>();

        public bool IsBinding = false;

        // Callbacks
        public Action<List<KeyCode>> OnValueChanging { get; set; }
        public Action<List<KeyCode>> OnValueChanged { get; set; }

        public List<KeyCode> BindKeys
        {
            get => _bindKeys;
            set
            {
                if (IsDisabled) return;

                if (value == null) value = new List<KeyCode>();

                if (!_bindKeys.SequenceEqual(value))
                {
                    OnValueChanging?.Invoke(value); // Pre
                    _bindKeys = value;
                    OnValueChanged?.Invoke(_bindKeys); // Post
                }
            }
        }

        public BindSetting(string name, List<KeyCode> defaultKeys = null)
        {
            Name = name;
            if (defaultKeys != null)
            {
                DefaultKeys = defaultKeys;
                _bindKeys = new List<KeyCode>(defaultKeys);
            }
        }

        public string GetBindString()
        {
            if (BindKeys == null || BindKeys.Count == 0) return "None";
            return string.Join(" + ", BindKeys.Select(k => k.ToString()).ToArray());
        }
    }

    public class MultiSelectSetting : Setting
    {
        public int MaxSelection = -1;
        public Dictionary<int, string> Options { get; set; }
        public HashSet<int> SelectedValues = new HashSet<int>();
        public HashSet<int> Blacklist = new HashSet<int>();
        public HashSet<string> NameBlacklist = new HashSet<string>();
        public System.Type EnumType { get; private set; }

        // Callback passes: (int optionId, bool isSelected)
        public Action<int, bool> OnSelectionChanged { get; set; }

        private Dictionary<int, string> _customNames;
        public Dictionary<int, string> CustomNames
        {
            get => _customNames;
            set
            {
                _customNames = value;
                if (_customNames != null)
                {
                    foreach (var kvp in _customNames) Options[kvp.Key] = kvp.Value;
                }
            }
        }

        public MultiSelectSetting(string name)
        {
            Name = name;
            Options = new Dictionary<int, string>();
        }

        public MultiSelectSetting(string name, System.Type enumType)
        {
            Name = name;
            EnumType = enumType;
            Options = new Dictionary<int, string>();

            if (enumType != null && enumType.IsEnum)
            {
                foreach (var val in System.Enum.GetValues(enumType))
                {
                    Options[System.Convert.ToInt32(val)] = val.ToString();
                }
            }
        }

        public string GetDisplayName(int id, string fallbackName) => Options.ContainsKey(id) ? Options[id] : fallbackName;
        public void AddOption(int id, string displayName) => Options[id] = displayName;

        public void RemoveOption(int id)
        {
            if (IsDisabled) return;

            if (Options.ContainsKey(id))
            {
                Options.Remove(id);
                if (SelectedValues.Contains(id))
                {
                    SelectedValues.Remove(id);
                    OnSelectionChanged?.Invoke(id, false);
                }
            }
        }

        public void Toggle(int id)
        {
            if (IsDisabled) return;

            if (IsSelected(id)) Deselect(id);
            else Select(id);
        }

        public void Select(int id)
        {
            if (IsDisabled) return;

            if (!SelectedValues.Contains(id) && !Blacklist.Contains(id))
            {
                if (MaxSelection == -1 || SelectedValues.Count < MaxSelection)
                {
                    SelectedValues.Add(id);
                    OnSelectionChanged?.Invoke(id, true);
                }
            }
        }

        public void Deselect(int id)
        {
            if (IsDisabled) return;

            if (SelectedValues.Contains(id))
            {
                SelectedValues.Remove(id);
                OnSelectionChanged?.Invoke(id, false);
            }
        }

        public bool IsSelected(int id) => SelectedValues.Contains(id);
    }

    public class SelectSetting : Setting
    {
        private int _value;
        public int DefaultValue;
        public Dictionary<int, string> Options { get; set; }
        public System.Type EnumType { get; private set; }
        public Dictionary<int, string> CustomNames { get; set; }

        // Selection Callback
        public Action<int> OnSelectionChanged { get; set; }

        public int Value
        {
            get => _value;
            set
            {
                if (IsDisabled) return;

                if (_value != value)
                {
                    _value = value;
                    OnSelectionChanged?.Invoke(_value);
                }
            }
        }

        public SelectSetting(string name, int defaultValue)
        {
            Name = name;
            _value = defaultValue;
            DefaultValue = defaultValue;
            Options = new Dictionary<int, string>();
            CustomNames = new Dictionary<int, string>();
        }

        public SelectSetting(string name, System.Type enumType, int defaultValue)
        {
            Name = name;
            _value = defaultValue;
            DefaultValue = defaultValue;
            EnumType = enumType;
            Options = new Dictionary<int, string>();
            CustomNames = new Dictionary<int, string>();

            if (enumType != null && enumType.IsEnum)
            {
                foreach (var val in System.Enum.GetValues(enumType))
                {
                    Options[System.Convert.ToInt32(val)] = val.ToString();
                }
            }
        }

        public void AddOption(int id, string displayName) => Options[id] = displayName;
    }

    public class CategorySetting : Setting
    {
        public bool IsExpanded;
        public CategorySetting(string name, bool defaultExpanded = true)
        {
            Name = name;
            IsExpanded = defaultExpanded;
        }
    }

    public class EndCategorySetting : Setting { }
}