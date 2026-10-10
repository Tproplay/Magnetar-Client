using Magnetar_Client.HUDElements;
using Magnetar_Client.UI.Setting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using static Magnetar_Client.Utils.Magnetar_Logger;

namespace Magnetar_Client.Core.HUDManager_;

public static class HUDRenderer
{
    public static List<HudElement> Elements = new();
    public static MultiSelectSetting HudToggles = new("Active Elements")
    {
        CustomNames = new Dictionary<int, string>(),
        DisplayAlphabetically = true,
    };
    private static bool isMasterVisible;

    public static int currentWindowId = 4000;

    public static void Init()
    {
        var types = Assembly.GetExecutingAssembly().GetTypes()
            .Where(t => t.IsSubclassOf(typeof(HudElement)) && !t.IsAbstract);

        foreach (var type in types)
        {
            RegisterElement(type);
        }

        DebugLogger.Msg($"Registered {Elements.Count} HUD elements");
    }

    public static void RegisterElement(Type element)
    {
        HudElement instance = (HudElement)Activator.CreateInstance(element);
        instance.WindowId = currentWindowId;
        Elements.Add(instance);
        HudToggles.AddOption(instance.WindowId, instance.Name);
        HudToggles.CustomNames[instance.WindowId] = instance.Name;
        currentWindowId++;
    }

    public static void RenderOverlay()
    {
        isMasterVisible = HUDManager.Enabled;

        if (Config.showgui && Config.CurrentTab != TabType.HUD)
        {
            isMasterVisible = false;
        }

        if (!isMasterVisible) return;

        for (int i = 0; i < Elements.Count; i++)
        {
            var element = Elements[i];
            if (element == null) continue;

            bool isElementEnabled = HudToggles != null && HudToggles.IsSelected(element.WindowId);

            if (isElementEnabled)
            {
                try
                {
                    element.Render();
                }
                catch (Exception ex)
                {
                    DebugLogger.Error($"[HUDRenderer] CRASH in element '{element.Name}': {ex}");
                }
            }
        }
    }

    public static void UpdateElements()
    {
        if (!isMasterVisible) return;

        for (int i = 0; i < Elements.Count; i++)
        {
            var element = Elements[i];
            if (element == null) continue;

            bool isElementEnabled = isMasterVisible && HudToggles.IsSelected(element.WindowId);
            element.HandleLifecycle(isElementEnabled);
        }
    }
}