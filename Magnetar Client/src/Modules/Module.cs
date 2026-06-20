using Il2CppSystem;
using Magnetar_Client.Utils;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;

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

        // These will be in Every Module.
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

        public virtual float SettingsWidth { get; set; } = 500f;

        // Add these category helper methods anywhere inside the Module class
        public void CreateCategory(string name, bool defaultExpanded = true)
        {
            Settings.Add(new CategorySetting(name, defaultExpanded));
        }

        public void EndCategory()
        {
            Settings.Add(new EndCategorySetting());
        }
    }

    public abstract class Setting
    {
        public string Name;
    }

    public class StringSetting : Setting
    {
        public string Value;
        public string DefaultValue;

        public List<string> AutocompleteVars;

        /// <summary>
        /// Initializes a new instance of the StringSetting class with the specified name and default value, 
        /// with an optional list of autocomplete variables.
        /// </summary>
        /// <param name="name">The unique name that identifies the setting.</param>
        /// <param name="defaultValue">The default string value assigned to the setting.</param>
        /// <param name="autocompleteVars">Optional list of variables for the rich-text autocomplete dropdown.</param>
        public StringSetting(string name, string defaultValue, List<string> autocompleteVars = null)
        {
            Name = name;
            Value = defaultValue;
            DefaultValue = defaultValue;
            AutocompleteVars = autocompleteVars;
        }
    }

    public class IntSetting : Setting
    {
        public int Value;
        public int DefaultValue;
        public int Min;
        public int Max;

        /// <summary>
        /// Initializes a new instance of the IntSetting class with the specified name, minimum and maximum values, and
        /// default value.
        /// </summary>
        /// <param name="name">The unique name that identifies the setting.</param>
        /// <param name="min">The minimum allowed value for the setting.</param>
        /// <param name="max">The maximum allowed value for the setting.</param>
        /// <param name="defaultValue">The default value assigned to the setting. Must be within the specified minimum and maximum range.</param>
        public IntSetting(string name, int min, int max, int defaultValue)
        {
            Name = name;
            Min = min;
            Max = max;
            Value = defaultValue;
            DefaultValue = defaultValue;
        }
    }

    public class FloatSetting : Setting
    {
        public float Value;
        public float DefaultValue;
        public float Min;
        public float Max;
        public int DecimalPlaces;

        /// <summary>
        /// Initializes a new instance of the FloatSetting class with the specified name, value range, default value,
        /// and number of decimal places.
        /// </summary>
        /// <param name="name">The unique name that identifies the setting.</param>
        /// <param name="min">The minimum allowed value for the setting.</param>
        /// <param name="max">The maximum allowed value for the setting.</param>
        /// <param name="defaultValue">The default value assigned to the setting. Must be within the specified range.</param>
        /// <param name="decimalPlaces">The number of decimal places to use when displaying or storing the value. Must be zero or greater. The
        /// default is 1.</param>
        public FloatSetting(string name, float min, float max, float defaultValue, int decimalPlaces = 1)
        {
            Name = name;
            Min = min;
            Max = max;
            Value = defaultValue;
            DefaultValue = defaultValue;
            DecimalPlaces = decimalPlaces;
        }
    }


    public class BoolSetting : Setting
    {
        public bool Value;
        public bool DefaultValue;

        /// <summary>
        /// Initializes a new instance of the BoolSetting class with the specified name and default value.
        /// </summary>
        /// <param name="name">The unique name that identifies the setting. Cannot be null or empty.</param>
        /// <param name="defaultValue">The default boolean value to assign to the setting.</param>
        public BoolSetting(string name, bool defaultValue)
        {
            Name = name;
            Value = defaultValue;
            DefaultValue = defaultValue;
        }
    }


    public class BindSetting : Setting
    {
        public List<KeyCode> BindKeys = new List<KeyCode>();
        public bool IsBinding = false;

        /// <summary>
        /// Initializes a new instance of the BindSetting class with the specified name and optional default key
        /// bindings.
        /// </summary>
        /// <param name="name">The unique name that identifies this binding setting. Cannot be null.</param>
        /// <param name="defaultKeys">An optional list of default key codes to assign to this binding. If null, no default keys are set.</param>
        public BindSetting(string name, List<KeyCode> defaultKeys = null)
        {
            Name = name;
            if (defaultKeys != null) BindKeys = defaultKeys;
        }
        /// <summary>
        /// Returns a string representation of the current key binding.
        /// </summary>
        /// <returns>A string listing the bound keys separated by "+". Returns "None" if no keys are bound.</returns>
        public string GetBindString()
        {
            if (BindKeys == null || BindKeys.Count == 0) return "None";
            return string.Join(" + ", BindKeys.Select(k => k.ToString()).ToArray());
        }
    }


    public class MultiSelectSetting : Setting
    {
        /// <summary>
        /// Specifies the maximum number of items that can be selected. A value of -1 indicates no limit.
        /// </summary>
        public int MaxSelection = -1;

        /// <summary>
        /// Gets the collection of available options, where each key is an option identifier and each value is the
        /// option's display name.
        /// </summary>
        public Dictionary<int, string> Options { get; set; }

        /// <summary>
        /// Represents the set of currently selected values.
        /// </summary>
        public HashSet<int> SelectedValues = new HashSet<int>();
        /// <summary>
        /// Contains the set of integer values that are blacklisted and should be excluded from processing.
        /// </summary>
        /// <remarks>Modifying this collection directly affects which values are considered blacklisted.
        /// Thread safety is not guaranteed; synchronize access if used concurrently.</remarks>
        public HashSet<int> Blacklist = new HashSet<int>();
        /// <summary>
        /// Contains the set of names that are excluded from processing or usage.
        /// </summary>
        /// <remarks>Names included in this blacklist will be ignored or rejected by operations that
        /// reference this collection. Modifying this set affects which names are considered valid throughout the
        /// application.</remarks>
        public HashSet<string> NameBlacklist = new HashSet<string>();
        public System.Type EnumType { get; private set; }

        private Dictionary<int, string> _customNames;
        public Dictionary<int, string> CustomNames
        {
            get => _customNames;
            set
            {
                _customNames = value;
                if (_customNames != null)
                {
                    foreach (var kvp in _customNames)
                    {
                        Options[kvp.Key] = kvp.Value;
                    }
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the MultiSelectSetting class with the specified setting name.
        /// </summary>
        /// <param name="name">The name of the setting. Cannot be null or empty.</param>
        public MultiSelectSetting(string name)
        {
            Name = name;
            Options = new Dictionary<int, string>();
            EnumType = null;
        }

        /// <summary>
        /// Initializes a new instance of the MultiSelectSetting class for the specified enumeration type.
        /// </summary>
        /// <remarks>If the specified enum type is valid, the constructor populates the options with all
        /// defined values from the enumeration.</remarks>
        /// <param name="name">The name of the setting. Cannot be null.</param>
        /// <param name="enumType">The enumeration type to use for the available options. Must be a valid enum type.</param>
        public MultiSelectSetting(string name, System.Type enumType)
        {
            Name = name;
            EnumType = enumType;
            Options = new Dictionary<int, string>();

            if (enumType != null && enumType.IsEnum)
            {
                var values = System.Enum.GetValues(enumType);
                foreach (var val in values)
                {
                    int intVal = System.Convert.ToInt32(val);
                    string displayName = val.ToString();

                    Options[intVal] = displayName;
                }
            }
        }

        /// <summary>
        /// Retrieves the display name associated with the specified identifier, or returns a fallback name if no match
        /// is found.
        /// </summary>
        /// <param name="id">The identifier for which to retrieve the display name.</param>
        /// <param name="fallbackName">The name to return if no display name is associated with the specified identifier. Cannot be null.</param>
        /// <returns>The display name associated with the specified identifier if found; otherwise, the value of <paramref
        /// name="fallbackName"/>.</returns>
        public string GetDisplayName(int id, string fallbackName)
        {
            if (Options.ContainsKey(id)) return Options[id];
            return fallbackName;
        }

        /// <summary>
        /// Adds an option with the specified identifier and display name to the collection.
        /// </summary>
        /// <param name="id">The unique identifier for the option to add.</param>
        /// <param name="displayName">The display name associated with the option.</param>
        public void AddOption(int id, string displayName)
        {
            Options[id] = displayName;
        }

        /// <summary>
        /// Removes the option with the specified identifier from the collection of available options and selected
        /// values.
        /// </summary>
        /// <remarks>If the specified identifier does not exist in the collection, no action is
        /// taken.</remarks>
        /// <param name="id">The identifier of the option to remove.</param>
        public void RemoveOption(int id)
        {
            if (Options.ContainsKey(id))
            {
                Options.Remove(id);
                SelectedValues.Remove(id);
            }
        }

        /// <summary>
        /// Toggles the selection state of the specified item by its identifier.
        /// </summary>
        /// <remarks>If the item is already selected, it is removed from the selection. If the item is not
        /// selected and the maximum selection limit has not been reached, it is added to the selection. If the maximum
        /// selection limit is set to -1, there is no limit to the number of selected items.</remarks>
        /// <param name="id">The identifier of the item to toggle in the selection.</param>
        public void Toggle(int id)
        {
            if (IsSelected(id)) SelectedValues.Remove(id);
            else if (MaxSelection == -1 || SelectedValues.Count < MaxSelection) SelectedValues.Add(id);
        }

        public void Select(int id) => SelectedValues.Add(id);
        public void Deselect(int id) => SelectedValues.Remove(id);
        public bool IsSelected(int id)
        {
            return SelectedValues.Contains(id);
        }

    }

    public class SelectSetting : Setting
    {
        public int Value;
        public int DefaultValue;

        public Dictionary<int, string> Options { get; set; }
        public System.Type EnumType { get; private set; }

        public Dictionary<int, string> CustomNames { get; set; }

        /// <summary>
        /// Initializes an empty SelectSetting.
        /// </summary>
        public SelectSetting(string name, int defaultValue)
        {
            Name = name;
            Value = defaultValue;
            DefaultValue = defaultValue;
            Options = new Dictionary<int, string>();
            CustomNames = new Dictionary<int, string>();
            EnumType = null;
        }

        /// <summary>
        /// Initializes a SelectSetting populated by an Enum.
        /// </summary>
        public SelectSetting(string name, System.Type enumType, int defaultValue)
        {
            Name = name;
            Value = defaultValue;
            DefaultValue = defaultValue;
            EnumType = enumType;
            Options = new Dictionary<int, string>();
            CustomNames = new Dictionary<int, string>();

            if (enumType != null && enumType.IsEnum)
            {
                var values = System.Enum.GetValues(enumType);
                foreach (var val in values)
                {
                    int intVal = System.Convert.ToInt32(val);
                    string displayName = val.ToString();
                    Options[intVal] = displayName;
                }
            }
        }

        public void AddOption(int id, string displayName)
        {
            Options[id] = displayName;
        }
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