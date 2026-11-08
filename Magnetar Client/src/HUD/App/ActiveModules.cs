using Magnetar_Client.Core;
using Magnetar_Client.Modules;
using Magnetar_Client.UI;
using Magnetar_Client.UI.Themes;
using UnityEngine;
using System.Collections.Generic;

namespace Magnetar_Client.HUDElements;

/// <summary>
/// HUD Element that displays the list of currently active modules in waterfall order
/// (longest width to shortest), auto-aligning to the left or right depending on screen placement.
/// </summary>
public class ActiveModules : HudElement
{
    private struct ActiveItem
    {
        public string DisplayName;
        public float Width;
    }

    private readonly List<ActiveItem> _items = new();
    private float _savedRightEdge;

    protected override TextAnchor TextAlignment => IsRightAligned ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft;

    public ActiveModules() : base("Active Modules", HudElement.NewRect(160, 24))
    {
        UpdateInterval = 0.15f;
        RegisterTranslation("Active Modules", "Active Modules");
        RegisterTranslation("Active Modules (Empty)", "Active Modules (Empty)");
    }

    private bool IsRightAligned => (Bounds.x + (Bounds.width / 2f)) >= (ClientConfig.NativeWidth / 2f);

    public override void OnEnable()
    {
        Api.Actions.OnModuleEnabled += OnModuleStateChanged;
        Api.Actions.OnModuleDisabled += OnModuleStateChanged;

        UpdateActiveList();
        AdjustBounds();
    }

    public override void OnDisable()
    {
        Api.Actions.OnModuleEnabled -= OnModuleStateChanged;
        Api.Actions.OnModuleDisabled -= OnModuleStateChanged;
    }

    public override void OnLanguageChanged()
    {
        UpdateActiveList();
        AdjustBounds();
    }

    public override void OnUpdateActive()
    {
        UpdateActiveList();
        AdjustBounds();
    }

    private void OnModuleStateChanged(Module _)
    {
        UpdateActiveList();
        AdjustBounds();
    }

    private void UpdateActiveList()
    {
        var modules = ModuleManager.GetModulesSnapshot();
        if (modules == null || modules.Length == 0)
        {
            _items.Clear();
            return;
        }

        var baseStyle = ThemeManager.HUDElementStyle ?? GUI.skin.label;
        var tempList = new List<ActiveItem>();

        for (int i = 0; i < modules.Length; i++)
        {
            var m = modules[i];
            if (m != null && m.Active)
            {
                string displayName = Translate(m.Name);
                float textWidth = baseStyle.CalcSize(new GUIContent(displayName)).x;
                tempList.Add(new ActiveItem
                {
                    DisplayName = displayName,
                    Width = textWidth
                });
            }
        }

        // Waterfall sort: Longest text width at the top, shortest at the bottom
        tempList.Sort((a, b) => b.Width.CompareTo(a.Width));

        _items.Clear();
        _items.AddRange(tempList);
    }

    private float GetItemHeight()
    {
        var baseStyle = ThemeManager.HUDElementStyle ?? GUI.skin.label;
        float textHeight = baseStyle.CalcSize(new GUIContent("A")).y;
        return Mathf.Max(textHeight + (2f * GUIManager.ElementScaleSetting.Value), 20f * GUIManager.ElementScaleSetting.Value);
    }

    private void AdjustBounds()
    {
        // When dragging, capture the intended right edge
        if (ActiveDragId == WindowId)
        {
            _savedRightEdge = Bounds.x + Bounds.width;
            return;
        }

        if (_savedRightEdge <= 0f && Bounds.width > 0f)
        {
            _savedRightEdge = Bounds.x + Bounds.width;
        }

        float padding = 10f * GUIManager.ElementScaleSetting.Value;
        float itemHeight = GetItemHeight();
        float spacing = 2f * GUIManager.ElementScaleSetting.Value;

        float targetWidth;
        float targetHeight;

        if (_items.Count == 0)
        {
            if (HUDManager.forceShow)
            {
                var baseStyle = ThemeManager.HUDElementStyle ?? GUI.skin.label;
                string placeholder = Translate("Active Modules");
                targetWidth = baseStyle.CalcSize(new GUIContent(placeholder)).x + padding;
                targetHeight = itemHeight + (4f * GUIManager.ElementScaleSetting.Value);
            }
            else
            {
                targetWidth = 0f;
                targetHeight = 0f;
            }
        }
        else
        {
            float maxWidth = 0f;
            for (int i = 0; i < _items.Count; i++)
            {
                if (_items[i].Width > maxWidth)
                    maxWidth = _items[i].Width;
            }

            targetWidth = maxWidth + padding;
            targetHeight = (_items.Count * itemHeight) + ((_items.Count - 1) * spacing) + (4f * GUIManager.ElementScaleSetting.Value);
        }

        if (IsRightAligned)
        {
            Bounds.x = _savedRightEdge - targetWidth;
            Bounds.width = targetWidth;
        }
        else
        {
            Bounds.width = targetWidth;
            _savedRightEdge = Bounds.x + targetWidth;
        }

        Bounds.height = targetHeight;
    }

    protected override void DrawContent(float width, float height)
    {
        if (width <= 0f || height <= 0f) return;

        float itemHeight = GetItemHeight();
        float spacing = 2f * GUIManager.ElementScaleSetting.Value;
        float padding = 6f * GUIManager.ElementScaleSetting.Value;

        if (_items.Count == 0)
        {
            if (HUDManager.forceShow)
            {
                string placeholder = Translate("Active Modules");
                Rect placeholderRect = new(padding, 2f * GUIManager.ElementScaleSetting.Value, width - (padding * 2f), itemHeight);
                Draw(placeholderRect, placeholder);
            }
            return;
        }

        float y = 2f * GUIManager.ElementScaleSetting.Value;
        for (int i = 0; i < _items.Count; i++)
        {
            Rect labelRect = new(padding, y, width - (padding * 2f), itemHeight);
            Draw(labelRect, _items[i].DisplayName);
            y += itemHeight + spacing;
        }
    }
}
