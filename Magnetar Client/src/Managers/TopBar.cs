using Magnetar_Client.Api;
using Magnetar_Client.Core.ModuleManager_;
using Magnetar_Client.UI;
using Magnetar_Client.UI.Themes;
using static Magnetar_Client.Utils.Translator;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Magnetar_Client.Core;

/// <summary>
/// Manages the different Tabs present at the top. Create a new tab using <see cref="TabType"/>.
/// </summary>
public static class TopBar
{
    public static readonly List<TabType> Order = new();
    private static readonly Dictionary<TabType, float> BaseWidth = new();
    private static float[] baseOffsets = Array.Empty<float>();

    private const float BaseHeight = 30f;

    public static void Init()
    {
        AnimationHandler.SetViewImmediate(TabType.AnimationGroup, ClientConfig.CurrentTab.Name, 1.0f);
        RebuildOffsets();
        ServiceRegistry.Register(new TopBarService());
    }

    public static TabType RegisterTab(string name, Action onSelected = null, Action onDeselected = null, Action onGUI = null)
    {
        return TabType.Register(name, onSelected, onDeselected, onGUI);
    }

    public static void RebuildOffsets()
    {
        Order.Clear();
        BaseWidth.Clear();

        foreach (TabType tab in TabType.AllTabs)
        {
            string translated = Translate(tab.Name);
            float btnWidth = Mathf.Max(translated.Length * 13f, 50f);

            Order.Add(tab);
            BaseWidth[tab] = btnWidth;
        }

        baseOffsets = new float[Order.Count + 1];
        float running = 0f;
        for (int i = 0; i < Order.Count; i++)
        {
            baseOffsets[i] = running;
            running += BaseWidth[Order[i]];
        }
        baseOffsets[Order.Count] = running;
    }

    /// <summary>
    /// Properly switches to another tab
    /// </summary>
    public static void SwitchTab(TabType tab)
    {
        if (tab == null || ClientConfig.CurrentTab == tab) return;

        TabType previousTab = ClientConfig.CurrentTab;
        previousTab?.OnDeselected?.Invoke();

        ClientConfig.CurrentTab = tab;
        AnimationHandler.SwitchView(TabType.AnimationGroup, tab.Name);
        tab.OnSelected?.Invoke();
    }

    public static void Render()
    {
        int count = Order.Count;
        if (count == 0) return;

        float[] scaledOffsets = new float[count + 1];
        for (int i = 0; i <= count; i++)
        {
            scaledOffsets[i] = Mathf.Round(GUIManager.S(baseOffsets[i]));
        }

        float scaledTotalWidth = scaledOffsets[count];
        float scaledHeight = Mathf.Round(GUIManager.S(BaseHeight));
        float startX = Mathf.Round((ClientConfig.NativeWidth / 2f) - (scaledTotalWidth / 2f));

        Rect barArea = new(startX, 0, scaledTotalWidth, scaledHeight);

        if (barArea.Contains(Event.current.mousePosition))
        {
            if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2))
            {
                Input.ResetInputAxes();
            }
        }

        Rect barGroupRect = new(startX, 0, scaledTotalWidth, scaledHeight);
        GUI.BeginGroup(barGroupRect);

        Event e = Event.current;

        for (int i = 0; i < count; i++)
        {
            TabType tab = Order[i];
            float btnX = scaledOffsets[i];
            float btnW = (scaledOffsets[i + 1] - scaledOffsets[i]) + (i < count - 1 ? 1f : 0f);

            Rect btnRect = new(btnX, 0, btnW, scaledHeight);
            string displayName = Translate(tab.Name);

            bool isSelected = (ClientConfig.CurrentTab == tab);
            GUIStyle btnStyle = isSelected
                ? ThemeManager.TopBarActiveStyle
                : ThemeManager.TopBarStyle;

            GUI.Box(btnRect, displayName, btnStyle);

            if (e.type == EventType.MouseDown && e.button == 0 && btnRect.Contains(e.mousePosition))
            {
                SwitchTab(tab);
                e.Use();
            }
        }

        GUI.EndGroup();
    }

    private class TopBarService : ILanguageAware
    {
        public string Name => "TopBar";
        public int Priority => ServicePriority.Highest;

        public void OnMenuGUI()
        {
            TopBar.Render();

            if (ClientConfig.CurrentTab != null && ClientConfig.CurrentTab.IsCustom)
            {
                ClientConfig.CurrentTab.OnGUI?.Invoke();
            }
        }

        public void OnLanguageChanged()
        {
            TopBar.RebuildOffsets();
        }
    }
}