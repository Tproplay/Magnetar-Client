using Magnetar_Client.Api;
using Magnetar_Client.Core.Lifecycle;
using Magnetar_Client.UI;
using Magnetar_Client.UI.Themes;
using Magnetar_Client.Utils;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Magnetar_Client.Core;

public static class TopBar
{
    public static readonly TranslationDomain Domain = Translator.CreateDomain("TopBar");

    // Scoped translation helper
    private static string Translate(string tabName) => Domain.Translate(tabName, "tabs.json");

    private static readonly List<TabType> Order = new();
    private static readonly Dictionary<TabType, float> BaseWidth = new();
    private static float[] baseOffsets = Array.Empty<float>();

    private const float BaseHeight = 30f;

    public static void Init()
    {
        Domain.OnDumpEnglishTemplate += DumpEnglishTemplates;

        AnimationHandler.SetViewImmediate(TabType.AnimationGroup, Config.CurrentTab.Name, 1.0f);
        RebuildOffsets();
        ServiceRegistry.Register(new TopBarService());
    }

    private static void DumpEnglishTemplates(string englishDir)
    {
        var tabNames = new List<string>();
        foreach (TabType tab in TabType.AllTabs)
        {
            if (!string.IsNullOrEmpty(tab.Name))
                tabNames.Add(tab.Name);
        }

        var templateDict = Translator.CreateDictionary(tabNames.ToArray());
        Translator.SaveJson(englishDir, "tabs.json", templateDict);
    }

    /// <summary>
    /// Registers a custom tab directly from TopBar.
    /// </summary>
    public static TabType RegisterTab(string name, Action onSelected = null, Action onGUI = null)
    {
        return TabType.Register(name, onSelected, onGUI);
    }

    /// <summary>
    /// Recalculates base tab widths and cumulative offsets whenever tabs are registered or language changes.
    /// </summary>
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

    public static void Render()
    {
        int count = Order.Count;
        if (count == 0) return;

        float[] scaledOffsets = new float[count + 1];
        for (int i = 0; i <= count; i++)
        {
            scaledOffsets[i] = Mathf.Round(Config.S(baseOffsets[i]));
        }

        float scaledTotalWidth = scaledOffsets[count];
        float scaledHeight = Mathf.Round(Config.S(BaseHeight));
        float startX = Mathf.Round((Config.NativeWidth / 2f) - (scaledTotalWidth / 2f));

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

            bool isSelected = (Config.CurrentTab == tab);
            GUIStyle btnStyle = isSelected
                ? ThemeManager.TopBarActiveStyle
                : ThemeManager.TopBarStyle;

            // Render button visuals
            GUI.Box(btnRect, displayName, btnStyle);

            if (e.type == EventType.MouseDown && e.button == 0 && btnRect.Contains(e.mousePosition))
            {
                if (Config.CurrentTab != tab)
                {
                    TabType previousTab = Config.CurrentTab;

                    previousTab?.OnDeselected?.Invoke();

                    Config.CurrentTab = tab;

                    AnimationHandler.SwitchView(TabType.AnimationGroup, tab.Name);

                    tab.OnSelected?.Invoke();
                }

                e.Use();
            }
        }

        GUI.EndGroup();
    }

    private class TopBarService : IInitializable, IWarmUp, IMenuRenderable, ILanguageAware
    {
        public string Name => "TopBar";
        public int Priority => ServicePriority.Highest;

        public void Initialize() => TopBar.RebuildOffsets();
        public void OnWarmUp() => TopBar.RebuildOffsets();

        public void OnMenuGUI()
        {
            TopBar.Render();

            // Invoke custom tab render delegates if registered
            if (Config.CurrentTab != null && Config.CurrentTab.IsCustom)
            {
                Config.CurrentTab.OnGUI?.Invoke();
            }
        }

        public void OnLanguageChanged()
        {
            TopBar.RebuildOffsets();
        }
    }
}