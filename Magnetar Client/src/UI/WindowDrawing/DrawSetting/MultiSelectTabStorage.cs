using Magnetar_Client.Core;
using Magnetar_Client.UI.Setting;
using System.Collections.Generic;

namespace Magnetar_Client.UI.WindowDrawing;

/// <summary>
/// Manages per-tab state persistence for the multi-select modal dialog.
/// </summary>
public static class MultiSelectTabStorage
{
    private class TabSelectorState
    {
        public MultiSelectSetting ActiveSetting;
        public string SearchQuery = "";
        public float ScrollY;
        public float TargetScrollY;
    }

    private static readonly Dictionary<TabType, TabSelectorState> _states = new();

    /// <summary>
    /// Saves the current multi-select dialog state for the specified tab.
    /// </summary>
    /// <param name="tab">The tab whose state should be saved.</param>
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

    /// <summary>
    /// Restores the previously saved multi-select dialog state for the specified tab, or resets to default if none exists.
    /// </summary>
    /// <param name="tab">The tab whose state should be restored.</param>
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