using Magnetar_Client.Core;
using Magnetar_Client.UI.Setting;
using System.Collections.Generic;

namespace Magnetar_Client.UI.WindowDrawing;

public static class MultiSelectTabStorage
{
    private class TabSelectorState
    {
        public MultiSelectSetting ActiveSetting;
        public string SearchQuery = "";
        public float ScrollY = 0f;
        public float TargetScrollY = 0f;
    }

    private static readonly Dictionary<TabType, TabSelectorState> _states = new();

    public static void SaveState(TabType tab)
    {
        if (tab == null) return;
        _states[tab] = new TabSelectorState
        {
            ActiveSetting = DrawSetting.activeMultiSelect,
            SearchQuery = DrawSetting.multiSelectSearchQuery ?? "",
            ScrollY = DrawSetting.manualScrollY,
            TargetScrollY = DrawSetting.targetScrollY
        };
    }

    public static void RestoreState(TabType tab)
    {
        if (tab == null) return;
        if (_states.TryGetValue(tab, out var state))
        {
            DrawSetting.activeMultiSelect = state.ActiveSetting;
            DrawSetting.multiSelectSearchQuery = state.SearchQuery;
            DrawSetting.manualScrollY = state.ScrollY;
            DrawSetting.targetScrollY = state.TargetScrollY;
        }
        else
        {
            DrawSetting.activeMultiSelect = null;
            DrawSetting.multiSelectSearchQuery = "";
            DrawSetting.manualScrollY = 0f;
            DrawSetting.targetScrollY = 0f;
        }
    }
}