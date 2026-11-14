using Il2CppInterop.Runtime;
using Magnetar_Client.Core;
using Magnetar_Client.UI.Themes;
using Magnetar_Client.UI.WindowDrawing;
using static Magnetar_Client.Utils.Translator;
using System;
using System.Text.RegularExpressions;
using UnityEngine;

#if MELONLOADER || RELEASE_MELON
using Il2Cpp;
#endif

namespace Magnetar_Client.UI.Setting;

/// <summary>
/// Abstract base class defining common state, matching rules, and UI rendering for lawn object selector settings.
/// </summary>
public abstract class GameObjectSettingBase : Setting
{
    private static readonly Regex CloneSuffixRegex = new(@"\s*\([Cc]lone\)", RegexOptions.Compiled);

    static GameObjectSettingBase()
    {
        GameObjectPickerManager.Init();
    }

    /// <summary>
    /// Gets or sets the target component or object type required for a GameObject to be selectable.
    /// Defaults to <see cref="GameObject"/> when any matching game object is allowed.
    /// </summary>
    public Type TargetType { get; set; } = typeof(GameObject);

    /// <summary>
    /// Gets or sets whether derived subclasses of <see cref="TargetType"/> are accepted.
    /// When <see langword="false"/>, only exact type matches are permitted.
    /// </summary>
    public bool AllowDerivatives { get; set; } = true;

    /// <summary>
    /// Optional user-defined filter predicate for custom selection conditions.
    /// </summary>
    public Func<GameObject, bool> Filter { get; set; }

    /// <summary>
    /// Gets or sets the untyped selected object value.
    /// </summary>
    public abstract object RawValue { get; set; }

    /// <summary>
    /// Gets whether a valid, non-destroyed object is currently selected.
    /// </summary>
    public abstract bool HasSelection { get; }

    /// <summary>
    /// Indicates whether this setting can be reset to its cleared state.
    /// </summary>
    public override bool CanReset => HasSelection;

    /// <summary>
    /// Indicates whether this setting is currently active in the lawn picker.
    /// </summary>
    public override bool IsFocused => GameObjectPickerManager.ActiveSetting == this;

    /// <summary>
    /// Formats a GameObject reference as: "Name (x, y)" without the "(clone)" suffix.
    /// </summary>
    /// <param name="go">The GameObject to format.</param>
    /// <returns>A formatted display string, or "(None)" if null or destroyed.</returns>
    public static string FormatObjectName(GameObject go)
    {
        if (go == null) return Translate("(None)");

        string rawName = go.name ?? "GameObject";
        string cleanName = CloneSuffixRegex.Replace(rawName, "").Trim();

        if (cleanName.Equals("Pumpiner", StringComparison.OrdinalIgnoreCase))
        {
            cleanName = "Pumpkin Chest";
        }
        else if (cleanName.Equals("EndPumpiner", StringComparison.OrdinalIgnoreCase))
        {
            cleanName = "Ender Pumpkin Chest";
        }

        Vector3 pos = go.transform != null ? go.transform.position : Vector3.zero;
        return $"{cleanName} ({pos.x:0.#}, {pos.y:0.#})";
    }

    /// <summary>
    /// Safely retrieves a component matching the specified managed type from a GameObject, its parents, or children in IL2CPP.
    /// </summary>
    /// <param name="go">The target GameObject.</param>
    /// <param name="type">The managed Type to search for.</param>
    /// <returns>The matching Component if found; otherwise, null.</returns>
    public static Component GetComponentDynamic(GameObject go, Type type)
    {
        if (go == null || type == null || type == typeof(GameObject)) return null;

        try
        {
            Il2CppSystem.Type il2CppType = Il2CppType.From(type);
            if (il2CppType != null)
            {
                return go.GetComponent(il2CppType) 
                    ?? go.GetComponentInParent(il2CppType) 
                    ?? go.GetComponentInChildren(il2CppType);
            }
        }
        catch
        {
        }

        return null;
    }

    /// <summary>
    /// Tests whether the given GameObject satisfies this setting's target type and filtering criteria.
    /// </summary>
    /// <param name="go">The candidate GameObject under cursor.</param>
    /// <returns><see langword="true"/> if the object can be selected; otherwise, <see langword="false"/>.</returns>
    public virtual bool IsMatch(GameObject go)
    {
        if (go == null) return false;

        Type target = TargetType ?? typeof(GameObject);
        if (target != typeof(GameObject))
        {
            Component comp = GetComponentDynamic(go, target);
            if (comp == null) return false;
        }

        if (Filter != null && !Filter(go))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Assigns the selected GameObject to this setting, resolving any required typed components.
    /// </summary>
    /// <param name="go">The selected GameObject from the lawn.</param>
    public abstract void SetSelectedRaw(GameObject go);

    /// <summary>
    /// Clears the currently selected object.
    /// </summary>
    public abstract void ClearSelection();

    /// <summary>
    /// Returns the formatted display text representing the current selection.
    /// </summary>
    public abstract string GetDisplayText();

    /// <summary>
    /// Resets the setting back to its cleared state.
    /// </summary>
    public override void Reset() => ClearSelection();

    /// <summary>
    /// Renders the setting row in the module settings window.
    /// </summary>
    /// <param name="y">Reference to the current vertical layout offset.</param>
    /// <param name="width">The total width available for rendering this row.</param>
    public override void Draw(ref float y, float width)
    {
        Event e = Event.current;
        bool isCurrentlyPicking = GameObjectPickerManager.ActiveSetting == this;

        bool prevGUIEnabled = GUI.enabled;
        if (IsDisabled)
        {
            if (isCurrentlyPicking)
            {
                GameObjectPickerManager.CancelPicking();
                isCurrentlyPicking = false;
            }
            GUI.enabled = false;
        }

        try
        {
            float elemH = ClientConfig.elementHeight;
            float indent = ClientConfig.indent;
            float gap = SettingValues.Gap;
            float resetBtnW = SettingValues.ResetButtonW;
            float pickBtnW = GUIManager.S(64f);

            float totalControlW = Mathf.Min(ClientConfig.SettingWidth * 1.55f, width * 0.60f);
            float rightEdge = width - indent;

            // 1. Reset button (rightmost)
            Rect resetRect = new(rightEdge - resetBtnW, y, resetBtnW, elemH);
            rightEdge -= resetBtnW + gap;

            // 2. Pick / Cancel button
            Rect pickRect = new(rightEdge - pickBtnW, y, pickBtnW, elemH);
            rightEdge -= pickBtnW + gap;

            // 3. Summary display box
            float summaryLeft = width - indent - totalControlW;
            float summaryW = Mathf.Max(GUIManager.S(80f), rightEdge - summaryLeft);
            Rect summaryRect = new(rightEdge - summaryW, y, summaryW, elemH);

            // 4. Label (left side)
            float labelW = Mathf.Max(width * 0.30f, summaryRect.x - indent - gap);
            Rect labelRect = new(indent, y, labelW, elemH);

            // Draw label
            GUI.Label(labelRect, DisplayName, ThemeManager.SettingLabelStyle);

            // Draw summary box
            string displayText = GetDisplayText();
            GUI.Box(summaryRect, displayText, ThemeManager.SettingOff);

            // Click summary box to toggle picking
            if (GUI.enabled && e.type == EventType.MouseDown && e.button == 0 && summaryRect.Contains(e.mousePosition))
            {
                Input.ResetInputAxes();
                if (isCurrentlyPicking) GameObjectPickerManager.CancelPicking();
                else GameObjectPickerManager.StartPicking(this);
                e.Use();
            }

            // Draw Pick / Cancel button
            string pickBtnText = isCurrentlyPicking ? Translate("Cancel") : Translate("Pick");
            GUIStyle pickBtnStyle = isCurrentlyPicking ? ThemeManager.SettingOn : ThemeManager.SettingOff;

            GUI.Box(pickRect, pickBtnText, pickBtnStyle);
            if (GUI.enabled && e.type == EventType.MouseDown && e.button == 0 && pickRect.Contains(e.mousePosition))
            {
                Input.ResetInputAxes();
                if (isCurrentlyPicking) GameObjectPickerManager.CancelPicking();
                else GameObjectPickerManager.StartPicking(this);
                e.Use();
            }

            // Draw Reset button
            if (CanReset)
            {
                if (DrawResetButton(resetRect))
                {
                    ClearSelection();
                }
            }

            y += elemH + ClientConfig.spacing;
        }
        finally
        {
            GUI.enabled = prevGUIEnabled;
        }
    }
}

/// <summary>
/// Generic lawn object setting allowing the user to interactively click and select a GameObject or component on the lawn.
/// </summary>
/// <typeparam name="T">The component type or <see cref="GameObject"/> to target.</typeparam>
public class GameObjectSetting<T> : GameObjectSettingBase where T : class
{
    private T _value;

    /// <summary>
    /// Gets or sets the strongly typed selected object or component reference.
    /// Automatically clears dead native IL2CPP references.
    /// </summary>
    public T Value
    {
        get
        {
            if (_value is UnityEngine.Object u && u == null)
            {
                _value = null;
            }
            return _value;
        }
        set
        {
            if (ReferenceEquals(_value, value)) return;

            _value = value;
            OnValueChanged?.Invoke(_value);
            OnSelectionChanged?.Invoke(_value);

            if (_value != null)
            {
                OnSelected?.Invoke(_value);
            }
            else
            {
                OnCleared?.Invoke();
            }
        }
    }

    /// <summary>
    /// Gets the underlying GameObject associated with the current selection.
    /// </summary>
    public GameObject TargetGameObject
    {
        get
        {
            T val = Value;
            if (val == null) return null;
            if (val is GameObject go) return go;
            if (val is Component comp && comp != null) return comp.gameObject;
            return null;
        }
    }

    /// <summary>
    /// Gets or sets the untyped selected object value.
    /// </summary>
    public override object RawValue
    {
        get => Value;
        set => Value = value as T;
    }

    /// <summary>
    /// Gets whether a valid, non-destroyed object is currently selected.
    /// </summary>
    public override bool HasSelection => Value != null;

    /// <summary>
    /// Event invoked after the selected object changes.
    /// </summary>
    public Action<T> OnValueChanged { get; set; }

    /// <summary>
    /// Event invoked after the selected object changes.
    /// </summary>
    public Action<T> OnSelectionChanged { get; set; }

    /// <summary>
    /// Event invoked when an object is selected.
    /// </summary>
    public Action<T> OnSelected { get; set; }

    /// <summary>
    /// Event invoked when the selection is cleared.
    /// </summary>
    public Action OnCleared { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="GameObjectSetting{T}"/> class.
    /// </summary>
    /// <param name="name">The display name or translation key for this setting.</param>
    /// <param name="allowDerivatives">Whether derived subclasses of <typeparamref name="T"/> are allowed.</param>
    /// <param name="filter">Optional custom predicate filter.</param>
    public GameObjectSetting(string name, bool allowDerivatives = true, Func<GameObject, bool> filter = null)
    {
        Name = name;
        TargetType = typeof(T);
        AllowDerivatives = allowDerivatives;
        Filter = filter;
        GameObjectPickerManager.RegisterSetting(this);
    }

    /// <summary>
    /// Assigns the selected GameObject to this setting, extracting the typed <typeparamref name="T"/> component if applicable.
    /// </summary>
    /// <param name="go">The selected GameObject from the lawn.</param>
    public override void SetSelectedRaw(GameObject go)
    {
        if (go == null)
        {
            Value = null;
            return;
        }

        if (typeof(T) == typeof(GameObject))
        {
            Value = go as T;
            return;
        }

        try
        {
            Il2CppSystem.Type il2CppType = Il2CppType.From(typeof(T));
            if (il2CppType != null)
            {
                Component comp = go.GetComponent(il2CppType) 
                    ?? go.GetComponentInParent(il2CppType) 
                    ?? go.GetComponentInChildren(il2CppType);

                if (comp != null)
                {
                    if (comp is T matched)
                    {
                        Value = matched;
                        return;
                    }

                    #if MELONLOADER || RELEASE_MELON || BEPINEX || RELEASE_BEPINEX
                    try
                    {
                        Value = Activator.CreateInstance(typeof(T), comp.Pointer) as T;
                        if (Value != null) return;
                    }
                    catch
                    {
                    }
                    #endif

                    Value = comp as T;
                    return;
                }
            }
        }
        catch
        {
        }

        Value = null;
    }

    /// <summary>
    /// Clears the current selection.
    /// </summary>
    public override void ClearSelection()
    {
        Value = null;
    }

    /// <summary>
    /// Returns the formatted display text for the current selection.
    /// </summary>
    public override string GetDisplayText()
    {
        return FormatObjectName(TargetGameObject);
    }
}

/// <summary>
/// Non-generic lawn object selector setting for selecting any GameObject on the lawn, optionally filtered by a component type.
/// </summary>
public class GameObjectSetting : GameObjectSetting<GameObject>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GameObjectSetting"/> class.
    /// </summary>
    /// <param name="name">The display name or translation key for this setting.</param>
    /// <param name="targetType">Optional component type to require on selected GameObjects (e.g. typeof(Plant)).</param>
    /// <param name="allowDerivatives">Whether derived subclasses of <paramref name="targetType"/> are allowed.</param>
    /// <param name="filter">Optional custom predicate filter.</param>
    public GameObjectSetting(string name, Type targetType = null, bool allowDerivatives = true, Func<GameObject, bool> filter = null)
        : base(name, allowDerivatives, filter)
    {
        if (targetType != null)
        {
            TargetType = targetType;
        }
    }
}