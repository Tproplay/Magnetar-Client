using Magnetar_Client.UI.Themes;
using Magnetar_Client.UI.WindowDrawing;
using static Magnetar_Client.Utils.Translator;
using System;
using System.Globalization;
using UnityEngine;
using Magnetar_Client.Core;

namespace Magnetar_Client.UI.Setting;

/// <summary>
/// A setting module representing a 2D floating-point vector (X, Y) with dedicated text input controls,
/// value buffering, and per-component min/max clamping boundaries.
/// </summary>
public class Vector2Setting : Setting
{
    private Vector2 _value;

    /// <summary>
    /// The default fallback value restored when <see cref="Reset"/> is executed.
    /// </summary>
    public Vector2 DefaultValue;

    /// <summary>
    /// The minimum permissible value for each vector component.
    /// </summary>
    public Vector2 MinValue = new(float.MinValue, float.MinValue);

    /// <summary>
    /// The maximum permissible value for each vector component.
    /// </summary>
    public Vector2 MaxValue = new(float.MaxValue, float.MaxValue);

    /// <summary>
    /// Gets or sets the minimum limit for the X component.
    /// </summary>
    public float MinX { get => MinValue.x; set => MinValue = new Vector2(value, MinValue.y); }

    /// <summary>
    /// Gets or sets the maximum limit for the X component.
    /// </summary>
    public float MaxX { get => MaxValue.x; set => MaxValue = new Vector2(value, MaxValue.y); }

    /// <summary>
    /// Gets or sets the minimum limit for the Y component.
    /// </summary>
    public float MinY { get => MinValue.y; set => MinValue = new Vector2(MinValue.x, value); }

    /// <summary>
    /// Gets or sets the maximum limit for the Y component.
    /// </summary>
    public float MaxY { get => MaxValue.y; set => MaxValue = new Vector2(MaxValue.x, value); }

    private string _bufX;
    private string _bufY;

    /// <summary>
    /// Event invoked whenever the vector components are modified and successfully parsed.
    /// </summary>
    public Action<Vector2> OnValueChanged { get; set; }

    /// <summary>
    /// Gets or sets the current 2D vector value, automatically clamped within [<see cref="MinValue"/>, <see cref="MaxValue"/>].
    /// </summary>
    public Vector2 Value
    {
        get => _value;
        set
        {
            if (IsDisabled) return;
            Vector2 clamped = Clamp(value);
            if (_value != clamped)
            {
                _value = clamped;
                _bufX = _value.x.ToString("0.##");
                _bufY = _value.y.ToString("0.##");
                OnValueChanged?.Invoke(_value);
            }
        }
    }

    /// <summary>
    /// Initializes a new instance of <see cref="Vector2Setting"/> with a display name and initial value.
    /// </summary>
    public Vector2Setting(string name, Vector2 defaultValue)
    {
        Name = name;
        DefaultValue = defaultValue;
        _value = Clamp(defaultValue);
        _bufX = _value.x.ToString("0.##");
        _bufY = _value.y.ToString("0.##");
    }

    /// <summary>
    /// Initializes a new instance of <see cref="Vector2Setting"/> with component boundaries specified as vectors.
    /// </summary>
    public Vector2Setting(string name, Vector2 defaultValue, Vector2 minValue, Vector2 maxValue)
    {
        Name = name;
        MinValue = minValue;
        MaxValue = maxValue;
        DefaultValue = defaultValue;
        _value = Clamp(defaultValue);
        _bufX = _value.x.ToString("0.##");
        _bufY = _value.y.ToString("0.##");
    }

    /// <summary>
    /// Initializes a new instance of <see cref="Vector2Setting"/> with individual component boundaries.
    /// </summary>
    public Vector2Setting(string name, Vector2 defaultValue, float minX, float maxX, float minY, float maxY)
        : this(name, defaultValue, new Vector2(minX, minY), new Vector2(maxX, maxY)) { }

    /// <summary>
    /// Clamps a vector within [<see cref="MinValue"/>, <see cref="MaxValue"/>].
    /// </summary>
    public Vector2 Clamp(Vector2 v)
    {
        return new Vector2(
            Mathf.Clamp(v.x, MinValue.x, MaxValue.x),
            Mathf.Clamp(v.y, MinValue.y, MaxValue.y)
        );
    }

    /// <summary>
    /// Restores the vector value and text buffers back to <see cref="DefaultValue"/>.
    /// </summary>
    public override void Reset()
    {
        Value = DefaultValue;
        _bufX = _value.x.ToString("0.##");
        _bufY = _value.y.ToString("0.##");
    }

    private static bool TryParseFloat(string s, out float result)
    {
        if (string.IsNullOrWhiteSpace(s))
        {
            result = 0f;
            return false;
        }
        return float.TryParse(s.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out result)
            || float.TryParse(s, out result);
    }

    /// <summary>
    /// Renders the vector setting row into the IMGUI layout, managing individual component fields and reset actions.
    /// </summary>
    public override void Draw(ref float y, float width)
    {
        float elemH = ClientConfig.elementHeight;
        float indent = ClientConfig.indent;
        float gap = SettingValues.Gap;
        float resetBtnW = SettingValues.ResetButtonW;
        float subLabelW = GUIManager.S(16f);
        float totalControlW = ClientConfig.SettingWidth;

        float labelW = width - (indent * 2f) - totalControlW - gap - resetBtnW;
        GUI.Label(new Rect(indent, y, labelW, elemH), DisplayName, ThemeManager.SettingLabelStyle);

        float itemW = (totalControlW - (gap * 3f) - (subLabelW * 2f)) / 2f;
        float resetStartX = width - indent - resetBtnW;
        float currX = resetStartX - gap - totalControlW;

        // X component
        GUI.Label(new Rect(currX, y, subLabelW, elemH), "X", ThemeManager.TextStyle);
        currX += subLabelW + gap;
        Rect xRect = new(currX, y, itemW, elemH);

        if (DrawSetting.ActiveTextFieldId != xRect.GetHashCode())
        {
            _bufX = Value.x.ToString("0.##");
        }
        _bufX = DrawSetting.DrawManualTextField(xRect, _bufX, "0");
        currX += itemW + gap;

        // Y component
        GUI.Label(new Rect(currX, y, subLabelW, elemH), "Y", ThemeManager.TextStyle);
        currX += subLabelW + gap;
        Rect yRect = new(currX, y, itemW, elemH);

        if (DrawSetting.ActiveTextFieldId != yRect.GetHashCode())
        {
            _bufY = Value.y.ToString("0.##");
        }
        _bufY = DrawSetting.DrawManualTextField(yRect, _bufY, "0");

        float rawX = TryParseFloat(_bufX, out float valX) ? valX : Value.x;
        float rawY = TryParseFloat(_bufY, out float valY) ? valY : Value.y;
        Vector2 parsed = Clamp(new Vector2(rawX, rawY));

        if (!Mathf.Approximately(Value.x, parsed.x) || !Mathf.Approximately(Value.y, parsed.y))
        {
            _value = parsed;
            OnValueChanged?.Invoke(_value);
        }

        // Reset Button
        Rect resetRect = new(resetStartX, y, resetBtnW, elemH);
        if (DrawResetButton(resetRect))
        {
            Reset();
        }

        y += elemH + ClientConfig.spacing;
    }
}

/// <summary>
/// A setting module representing a 2D integer vector (X, Y) with dedicated text input controls,
/// value buffering, and per-component min/max clamping boundaries.
/// </summary>
public class Vector2IntSetting : Setting
{
    private Vector2Int _value;

    /// <summary>
    /// The default fallback value restored when <see cref="Reset"/> is executed.
    /// </summary>
    public Vector2Int DefaultValue;

    /// <summary>
    /// The minimum permissible value for each integer component.
    /// </summary>
    public Vector2Int MinValue = new(int.MinValue, int.MinValue);

    /// <summary>
    /// The maximum permissible value for each integer component.
    /// </summary>
    public Vector2Int MaxValue = new(int.MaxValue, int.MaxValue);

    /// <summary>
    /// Gets or sets the minimum limit for the X component.
    /// </summary>
    public int MinX { get => MinValue.x; set => MinValue = new Vector2Int(value, MinValue.y); }

    /// <summary>
    /// Gets or sets the maximum limit for the X component.
    /// </summary>
    public int MaxX { get => MaxValue.x; set => MaxValue = new Vector2Int(value, MaxValue.y); }

    /// <summary>
    /// Gets or sets the minimum limit for the Y component.
    /// </summary>
    public int MinY { get => MinValue.y; set => MinValue = new Vector2Int(MinValue.x, value); }

    /// <summary>
    /// Gets or sets the maximum limit for the Y component.
    /// </summary>
    public int MaxY { get => MaxValue.y; set => MaxValue = new Vector2Int(MaxValue.x, value); }

    private string _bufX;
    private string _bufY;

    /// <summary>
    /// Event invoked whenever the vector components are modified and successfully parsed.
    /// </summary>
    public Action<Vector2Int> OnValueChanged { get; set; }

    /// <summary>
    /// Gets or sets the current 2D integer vector value, automatically clamped within [<see cref="MinValue"/>, <see cref="MaxValue"/>].
    /// </summary>
    public Vector2Int Value
    {
        get => _value;
        set
        {
            if (IsDisabled) return;
            Vector2Int clamped = Clamp(value);
            if (_value != clamped)
            {
                _value = clamped;
                _bufX = _value.x.ToString();
                _bufY = _value.y.ToString();
                OnValueChanged?.Invoke(_value);
            }
        }
    }

    /// <summary>
    /// Initializes a new instance of <see cref="Vector2IntSetting"/> with a display name and initial value.
    /// </summary>
    public Vector2IntSetting(string name, Vector2Int defaultValue)
    {
        Name = name;
        DefaultValue = defaultValue;
        _value = Clamp(defaultValue);
        _bufX = _value.x.ToString();
        _bufY = _value.y.ToString();
    }

    /// <summary>
    /// Initializes a new instance of <see cref="Vector2IntSetting"/> with component boundaries specified as vectors.
    /// </summary>
    public Vector2IntSetting(string name, Vector2Int defaultValue, Vector2Int minValue, Vector2Int maxValue)
    {
        Name = name;
        MinValue = minValue;
        MaxValue = maxValue;
        DefaultValue = defaultValue;
        _value = Clamp(defaultValue);
        _bufX = _value.x.ToString();
        _bufY = _value.y.ToString();
    }

    /// <summary>
    /// Initializes a new instance of <see cref="Vector2IntSetting"/> with individual component boundaries.
    /// </summary>
    public Vector2IntSetting(string name, Vector2Int defaultValue, int minX, int maxX, int minY, int maxY)
        : this(name, defaultValue, new Vector2Int(minX, minY), new Vector2Int(maxX, maxY)) { }

    /// <summary>
    /// Clamps an integer vector within [<see cref="MinValue"/>, <see cref="MaxValue"/>].
    /// </summary>
    public Vector2Int Clamp(Vector2Int v)
    {
        return new Vector2Int(
            Mathf.Clamp(v.x, MinValue.x, MaxValue.x),
            Mathf.Clamp(v.y, MinValue.y, MaxValue.y)
        );
    }

    /// <summary>
    /// Restores the integer vector value and text buffers back to <see cref="DefaultValue"/>.
    /// </summary>
    public override void Reset()
    {
        Value = DefaultValue;
        _bufX = _value.x.ToString();
        _bufY = _value.y.ToString();
    }

    /// <summary>
    /// Renders the integer vector setting row into the IMGUI layout, managing individual component fields and reset actions.
    /// </summary>
    public override void Draw(ref float y, float width)
    {
        float elemH = ClientConfig.elementHeight;
        float indent = ClientConfig.indent;
        float gap = SettingValues.Gap;
        float resetBtnW = SettingValues.ResetButtonW;
        float subLabelW = GUIManager.S(16f);
        float totalControlW = ClientConfig.SettingWidth;

        float labelW = width - (indent * 2f) - totalControlW - gap - resetBtnW;
        GUI.Label(new Rect(indent, y, labelW, elemH), DisplayName, ThemeManager.SettingLabelStyle);

        float itemW = (totalControlW - (gap * 3f) - (subLabelW * 2f)) / 2f;
        float resetStartX = width - indent - resetBtnW;
        float currX = resetStartX - gap - totalControlW;

        // X component
        GUI.Label(new Rect(currX, y, subLabelW, elemH), "X", ThemeManager.TextStyle);
        currX += subLabelW + gap;
        Rect xRect = new(currX, y, itemW, elemH);

        if (DrawSetting.ActiveTextFieldId != xRect.GetHashCode())
        {
            _bufX = Value.x.ToString();
        }
        _bufX = DrawSetting.DrawManualTextField(xRect, _bufX, "0");
        currX += itemW + gap;

        // Y component
        GUI.Label(new Rect(currX, y, subLabelW, elemH), "Y", ThemeManager.TextStyle);
        currX += subLabelW + gap;
        Rect yRect = new(currX, y, itemW, elemH);

        if (DrawSetting.ActiveTextFieldId != yRect.GetHashCode())
        {
            _bufY = Value.y.ToString();
        }
        _bufY = DrawSetting.DrawManualTextField(yRect, _bufY, "0");

        int rawX = int.TryParse(_bufX, out int valX) ? valX : Value.x;
        int rawY = int.TryParse(_bufY, out int valY) ? valY : Value.y;
        Vector2Int parsed = Clamp(new Vector2Int(rawX, rawY));

        if (Value.x != parsed.x || Value.y != parsed.y)
        {
            _value = parsed;
            OnValueChanged?.Invoke(_value);
        }

        // Reset Button
        Rect resetRect = new(resetStartX, y, resetBtnW, elemH);
        if (DrawResetButton(resetRect))
        {
            Reset();
        }

        y += elemH + ClientConfig.spacing;
    }
}

/// <summary>
/// A setting module representing a 3D floating-point vector (X, Y, Z) with dedicated text input controls,
/// value buffering, and per-component min/max clamping boundaries.
/// </summary>
public class Vector3Setting : Setting
{
    private Vector3 _value;

    /// <summary>
    /// The default fallback value restored when <see cref="Reset"/> is executed.
    /// </summary>
    public Vector3 DefaultValue;

    /// <summary>
    /// The minimum permissible value for each vector component.
    /// </summary>
    public Vector3 MinValue = new(float.MinValue, float.MinValue, float.MinValue);

    /// <summary>
    /// The maximum permissible value for each vector component.
    /// </summary>
    public Vector3 MaxValue = new(float.MaxValue, float.MaxValue, float.MaxValue);

    /// <summary>
    /// Gets or sets the minimum limit for the X component.
    /// </summary>
    public float MinX { get => MinValue.x; set => MinValue = new Vector3(value, MinValue.y, MinValue.z); }

    /// <summary>
    /// Gets or sets the maximum limit for the X component.
    /// </summary>
    public float MaxX { get => MaxValue.x; set => MaxValue = new Vector3(value, MaxValue.y, MaxValue.z); }

    /// <summary>
    /// Gets or sets the minimum limit for the Y component.
    /// </summary>
    public float MinY { get => MinValue.y; set => MinValue = new Vector3(MinValue.x, value, MinValue.z); }

    /// <summary>
    /// Gets or sets the maximum limit for the Y component.
    /// </summary>
    public float MaxY { get => MaxValue.y; set => MaxValue = new Vector3(MaxValue.x, value, MaxValue.z); }

    /// <summary>
    /// Gets or sets the minimum limit for the Z component.
    /// </summary>
    public float MinZ { get => MinValue.z; set => MinValue = new Vector3(MinValue.x, MinValue.y, value); }

    /// <summary>
    /// Gets or sets the maximum limit for the Z component.
    /// </summary>
    public float MaxZ { get => MaxValue.z; set => MaxValue = new Vector3(MaxValue.x, MaxValue.y, value); }

    private string _bufX;
    private string _bufY;
    private string _bufZ;

    /// <summary>
    /// Event invoked whenever the vector components are modified and successfully parsed.
    /// </summary>
    public Action<Vector3> OnValueChanged { get; set; }

    /// <summary>
    /// Gets or sets the current 3D vector value, automatically clamped within [<see cref="MinValue"/>, <see cref="MaxValue"/>].
    /// </summary>
    public Vector3 Value
    {
        get => _value;
        set
        {
            if (IsDisabled) return;
            Vector3 clamped = Clamp(value);
            if (_value != clamped)
            {
                _value = clamped;
                _bufX = _value.x.ToString("0.##");
                _bufY = _value.y.ToString("0.##");
                _bufZ = _value.z.ToString("0.##");
                OnValueChanged?.Invoke(_value);
            }
        }
    }

    /// <summary>
    /// Initializes a new instance of <see cref="Vector3Setting"/> with a display name and initial value.
    /// </summary>
    public Vector3Setting(string name, Vector3 defaultValue)
    {
        Name = name;
        DefaultValue = defaultValue;
        _value = Clamp(defaultValue);
        _bufX = _value.x.ToString("0.##");
        _bufY = _value.y.ToString("0.##");
        _bufZ = _value.z.ToString("0.##");
    }

    /// <summary>
    /// Initializes a new instance of <see cref="Vector3Setting"/> with component boundaries specified as vectors.
    /// </summary>
    public Vector3Setting(string name, Vector3 defaultValue, Vector3 minValue, Vector3 maxValue)
    {
        Name = name;
        MinValue = minValue;
        MaxValue = maxValue;
        DefaultValue = defaultValue;
        _value = Clamp(defaultValue);
        _bufX = _value.x.ToString("0.##");
        _bufY = _value.y.ToString("0.##");
        _bufZ = _value.z.ToString("0.##");
    }

    /// <summary>
    /// Initializes a new instance of <see cref="Vector3Setting"/> with individual component boundaries.
    /// </summary>
    public Vector3Setting(string name, Vector3 defaultValue, float minX, float maxX, float minY, float maxY, float minZ, float maxZ)
        : this(name, defaultValue, new Vector3(minX, minY, minZ), new Vector3(maxX, maxY, maxZ)) { }

    /// <summary>
    /// Clamps a 3D vector within [<see cref="MinValue"/>, <see cref="MaxValue"/>].
    /// </summary>
    public Vector3 Clamp(Vector3 v)
    {
        return new Vector3(
            Mathf.Clamp(v.x, MinValue.x, MaxValue.x),
            Mathf.Clamp(v.y, MinValue.y, MaxValue.y),
            Mathf.Clamp(v.z, MinValue.z, MaxValue.z)
        );
    }

    /// <summary>
    /// Restores the vector value and text buffers back to <see cref="DefaultValue"/>.
    /// </summary>
    public override void Reset()
    {
        Value = DefaultValue;
        _bufX = _value.x.ToString("0.##");
        _bufY = _value.y.ToString("0.##");
        _bufZ = _value.z.ToString("0.##");
    }

    private static bool TryParseFloat(string s, out float result)
    {
        if (string.IsNullOrWhiteSpace(s))
        {
            result = 0f;
            return false;
        }
        return float.TryParse(s.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out result)
            || float.TryParse(s, out result);
    }

    /// <summary>
    /// Renders the vector setting row into the IMGUI layout, managing individual component fields and reset actions.
    /// </summary>
    public override void Draw(ref float y, float width)
    {
        float elemH = ClientConfig.elementHeight;
        float indent = ClientConfig.indent;
        float gap = SettingValues.Gap;
        float resetBtnW = SettingValues.ResetButtonW;
        float subLabelW = GUIManager.S(14f);
        float totalControlW = ClientConfig.SettingWidth * 1.15f;

        float labelW = width - (indent * 2f) - totalControlW - gap - resetBtnW;
        GUI.Label(new Rect(indent, y, labelW, elemH), DisplayName, ThemeManager.SettingLabelStyle);

        float itemW = (totalControlW - (gap * 5f) - (subLabelW * 3f)) / 3f;
        float resetStartX = width - indent - resetBtnW;
        float currX = resetStartX - gap - totalControlW;

        // X component
        GUI.Label(new Rect(currX, y, subLabelW, elemH), "X", ThemeManager.TextStyle);
        currX += subLabelW + gap;
        Rect xRect = new(currX, y, itemW, elemH);

        if (DrawSetting.ActiveTextFieldId != xRect.GetHashCode())
        {
            _bufX = Value.x.ToString("0.##");
        }
        _bufX = DrawSetting.DrawManualTextField(xRect, _bufX, "0");
        currX += itemW + gap;

        // Y component
        GUI.Label(new Rect(currX, y, subLabelW, elemH), "Y", ThemeManager.TextStyle);
        currX += subLabelW + gap;
        Rect yRect = new(currX, y, itemW, elemH);

        if (DrawSetting.ActiveTextFieldId != yRect.GetHashCode())
        {
            _bufY = Value.y.ToString("0.##");
        }
        _bufY = DrawSetting.DrawManualTextField(yRect, _bufY, "0");
        currX += itemW + gap;

        // Z component
        GUI.Label(new Rect(currX, y, subLabelW, elemH), "Z", ThemeManager.TextStyle);
        currX += subLabelW + gap;
        Rect zRect = new(currX, y, itemW, elemH);

        if (DrawSetting.ActiveTextFieldId != zRect.GetHashCode())
        {
            _bufZ = Value.z.ToString("0.##");
        }
        _bufZ = DrawSetting.DrawManualTextField(zRect, _bufZ, "0");

        float rawX = TryParseFloat(_bufX, out float valX) ? valX : Value.x;
        float rawY = TryParseFloat(_bufY, out float valY) ? valY : Value.y;
        float rawZ = TryParseFloat(_bufZ, out float valZ) ? valZ : Value.z;
        Vector3 parsed = Clamp(new Vector3(rawX, rawY, rawZ));

        if (!Mathf.Approximately(Value.x, parsed.x) || !Mathf.Approximately(Value.y, parsed.y) || !Mathf.Approximately(Value.z, parsed.z))
        {
            _value = parsed;
            OnValueChanged?.Invoke(_value);
        }

        // Reset Button
        Rect resetRect = new(resetStartX, y, resetBtnW, elemH);
        if (DrawResetButton(resetRect))
        {
            Reset();
        }

        y += elemH + ClientConfig.spacing;
    }
}

/// <summary>
/// A setting module representing a 3D integer vector (X, Y, Z) with dedicated text input controls,
/// value buffering, and per-component min/max clamping boundaries.
/// </summary>
public class Vector3IntSetting : Setting
{
    private Vector3Int _value;

    /// <summary>
    /// The default fallback value restored when <see cref="Reset"/> is executed.
    /// </summary>
    public Vector3Int DefaultValue;

    /// <summary>
    /// The minimum permissible value for each integer component.
    /// </summary>
    public Vector3Int MinValue = new(int.MinValue, int.MinValue, int.MinValue);

    /// <summary>
    /// The maximum permissible value for each integer component.
    /// </summary>
    public Vector3Int MaxValue = new(int.MaxValue, int.MaxValue, int.MaxValue);

    /// <summary>
    /// Gets or sets the minimum limit for the X component.
    /// </summary>
    public int MinX { get => MinValue.x; set => MinValue = new Vector3Int(value, MinValue.y, MinValue.z); }

    /// <summary>
    /// Gets or sets the maximum limit for the X component.
    /// </summary>
    public int MaxX { get => MaxValue.x; set => MaxValue = new Vector3Int(value, MaxValue.y, MaxValue.z); }

    /// <summary>
    /// Gets or sets the minimum limit for the Y component.
    /// </summary>
    public int MinY { get => MinValue.y; set => MinValue = new Vector3Int(MinValue.x, value, MinValue.z); }

    /// <summary>
    /// Gets or sets the maximum limit for the Y component.
    /// </summary>
    public int MaxY { get => MaxValue.y; set => MaxValue = new Vector3Int(MaxValue.x, value, MaxValue.z); }

    /// <summary>
    /// Gets or sets the minimum limit for the Z component.
    /// </summary>
    public int MinZ { get => MinValue.z; set => MinValue = new Vector3Int(MinValue.x, MinValue.y, value); }

    /// <summary>
    /// Gets or sets the maximum limit for the Z component.
    /// </summary>
    public int MaxZ { get => MaxValue.z; set => MaxValue = new Vector3Int(MaxValue.x, MaxValue.y, value); }

    private string _bufX;
    private string _bufY;
    private string _bufZ;

    /// <summary>
    /// Event invoked whenever the vector components are modified and successfully parsed.
    /// </summary>
    public Action<Vector3Int> OnValueChanged { get; set; }

    /// <summary>
    /// Gets or sets the current 3D integer vector value, automatically clamped within [<see cref="MinValue"/>, <see cref="MaxValue"/>].
    /// </summary>
    public Vector3Int Value
    {
        get => _value;
        set
        {
            if (IsDisabled) return;
            Vector3Int clamped = Clamp(value);
            if (_value != clamped)
            {
                _value = clamped;
                _bufX = _value.x.ToString();
                _bufY = _value.y.ToString();
                _bufZ = _value.z.ToString();
                OnValueChanged?.Invoke(_value);
            }
        }
    }

    /// <summary>
    /// Initializes a new instance of <see cref="Vector3IntSetting"/> with a display name and initial value.
    /// </summary>
    public Vector3IntSetting(string name, Vector3Int defaultValue)
    {
        Name = name;
        DefaultValue = defaultValue;
        _value = Clamp(defaultValue);
        _bufX = _value.x.ToString();
        _bufY = _value.y.ToString();
        _bufZ = _value.z.ToString();
    }

    /// <summary>
    /// Initializes a new instance of <see cref="Vector3IntSetting"/> with component boundaries specified as vectors.
    /// </summary>
    public Vector3IntSetting(string name, Vector3Int defaultValue, Vector3Int minValue, Vector3Int maxValue)
    {
        Name = name;
        MinValue = minValue;
        MaxValue = maxValue;
        DefaultValue = defaultValue;
        _value = Clamp(defaultValue);
        _bufX = _value.x.ToString();
        _bufY = _value.y.ToString();
        _bufZ = _value.z.ToString();
    }

    /// <summary>
    /// Initializes a new instance of <see cref="Vector3IntSetting"/> with individual component boundaries.
    /// </summary>
    public Vector3IntSetting(string name, Vector3Int defaultValue, int minX, int maxX, int minY, int maxY, int minZ, int maxZ)
        : this(name, defaultValue, new Vector3Int(minX, minY, minZ), new Vector3Int(maxX, maxY, maxZ)) { }

    /// <summary>
    /// Clamps an integer 3D vector within [<see cref="MinValue"/>, <see cref="MaxValue"/>].
    /// </summary>
    public Vector3Int Clamp(Vector3Int v)
    {
        return new Vector3Int(
            Mathf.Clamp(v.x, MinValue.x, MaxValue.x),
            Mathf.Clamp(v.y, MinValue.y, MaxValue.y),
            Mathf.Clamp(v.z, MinValue.z, MaxValue.z)
        );
    }

    /// <summary>
    /// Restores the integer vector value and text buffers back to <see cref="DefaultValue"/>.
    /// </summary>
    public override void Reset()
    {
        Value = DefaultValue;
        _bufX = _value.x.ToString();
        _bufY = _value.y.ToString();
        _bufZ = _value.z.ToString();
    }

    /// <summary>
    /// Renders the integer vector setting row into the IMGUI layout, managing individual component fields and reset actions.
    /// </summary>
    public override void Draw(ref float y, float width)
    {
        float elemH = ClientConfig.elementHeight;
        float indent = ClientConfig.indent;
        float gap = SettingValues.Gap;
        float resetBtnW = SettingValues.ResetButtonW;
        float subLabelW = GUIManager.S(14f);
        float totalControlW = ClientConfig.SettingWidth * 1.15f;

        float labelW = width - (indent * 2f) - totalControlW - gap - resetBtnW;
        GUI.Label(new Rect(indent, y, labelW, elemH), DisplayName, ThemeManager.SettingLabelStyle);

        float itemW = (totalControlW - (gap * 5f) - (subLabelW * 3f)) / 3f;
        float resetStartX = width - indent - resetBtnW;
        float currX = resetStartX - gap - totalControlW;

        // X component
        GUI.Label(new Rect(currX, y, subLabelW, elemH), "X", ThemeManager.TextStyle);
        currX += subLabelW + gap;
        Rect xRect = new(currX, y, itemW, elemH);

        if (DrawSetting.ActiveTextFieldId != xRect.GetHashCode())
        {
            _bufX = Value.x.ToString();
        }
        _bufX = DrawSetting.DrawManualTextField(xRect, _bufX, "0");
        currX += itemW + gap;

        // Y component
        GUI.Label(new Rect(currX, y, subLabelW, elemH), "Y", ThemeManager.TextStyle);
        currX += subLabelW + gap;
        Rect yRect = new(currX, y, itemW, elemH);

        if (DrawSetting.ActiveTextFieldId != yRect.GetHashCode())
        {
            _bufY = Value.y.ToString();
        }
        _bufY = DrawSetting.DrawManualTextField(yRect, _bufY, "0");
        currX += itemW + gap;

        // Z component
        GUI.Label(new Rect(currX, y, subLabelW, elemH), "Z", ThemeManager.TextStyle);
        currX += subLabelW + gap;
        Rect zRect = new(currX, y, itemW, elemH);

        if (DrawSetting.ActiveTextFieldId != zRect.GetHashCode())
        {
            _bufZ = Value.z.ToString();
        }
        _bufZ = DrawSetting.DrawManualTextField(zRect, _bufZ, "0");

        int rawX = int.TryParse(_bufX, out int valX) ? valX : Value.x;
        int rawY = int.TryParse(_bufY, out int valY) ? valY : Value.y;
        int rawZ = int.TryParse(_bufZ, out int valZ) ? valZ : Value.z;
        Vector3Int parsed = Clamp(new Vector3Int(rawX, rawY, rawZ));

        if (Value.x != parsed.x || Value.y != parsed.y || Value.z != parsed.z)
        {
            _value = parsed;
            OnValueChanged?.Invoke(_value);
        }

        // Reset Button
        Rect resetRect = new(resetStartX, y, resetBtnW, elemH);
        if (DrawResetButton(resetRect))
        {
            Reset();
        }

        y += elemH + ClientConfig.spacing;
    }
}