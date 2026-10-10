using Magnetar_Client.UI.Themes;
using System;
using UnityEngine;
using static Magnetar_Client.UI.WindowDrawing.DrawSetting;
using static Magnetar_Client.Utils.Translator;

namespace Magnetar_Client.Core.ModuleManager_;

public static class SearchWindowDrawer
{
    public static string SearchQuery = "";
    public static Rect SearchWindowRect;
    public static bool IsSearchOpen;
    public static float SearchAnimProgress;
    public static bool SearchWasFocused;
    public static bool RequestSearchFocus;

    private static GUI.WindowFunction _cachedSearchDelegate;
    private static GUI.WindowFunction SearchDelegate => _cachedSearchDelegate ??=
        Il2CppInterop.Runtime.DelegateSupport.ConvertDelegate<GUI.WindowFunction>(DrawSearchWindow);

    public static void Initialize()
    {
#if ANDROID
        IsSearchOpen = true;
        SearchAnimProgress = 1f;
#endif
    }

    public static void Render(Event currentEvent)
    {
#if ANDROID
        IsSearchOpen = true;
        SearchAnimProgress = 1f;
#else
        if (currentEvent.type == EventType.KeyDown && (currentEvent.keyCode == KeyCode.Return || currentEvent.keyCode == KeyCode.KeypadEnter))
        {
            IsSearchOpen = true;
            RequestSearchFocus = true;

            float searchWidth = Config.ModuleWindowWidth;
            float tfY = GUIManager.S(10f);
            Rect anticipatedTfRect = new(Config.indent, tfY, searchWidth - (Config.indent * 2), GUIManager.S(20f));
            ActiveTextFieldId = anticipatedTfRect.GetHashCode();

            GUI.FocusWindow(999);
            currentEvent.Use();
        }

        if (IsSearchOpen)
        {
            if (ActiveTextFieldId != -1) SearchWasFocused = true;

            if (SearchWasFocused && ActiveTextFieldId == -1 && string.IsNullOrEmpty(SearchQuery))
            {
                IsSearchOpen = false;
                RequestSearchFocus = true;
                SearchWasFocused = false;
            }
        }

        if (currentEvent.type == EventType.Repaint)
        {
            float targetProgress = IsSearchOpen ? 1f : 0f;
            SearchAnimProgress = Mathf.Lerp(SearchAnimProgress, targetProgress, Time.unscaledDeltaTime * ModuleManager.SearchAnimationSpeed);
        }
#endif

        if (SearchAnimProgress > 0.01f)
        {
            float searchWidth = Config.ModuleWindowWidth * ModuleManager.SearchWidthMultiplier;
            float searchHeight = GUIManager.S(30f);

            float targetY = Config.NativeHeight - searchHeight - GUIManager.S(20f);
            float hiddenY = Config.NativeHeight + GUIManager.S(10f);
            float currentY = Mathf.Lerp(hiddenY, targetY, SearchAnimProgress);
            float currentX = (Config.NativeWidth / 2f) - (searchWidth / 2f);

            SearchWindowRect = new Rect(currentX, currentY, searchWidth, searchHeight);
            SearchWindowRect = GUI.Window(999, SearchWindowRect, SearchDelegate, "", ThemeManager.CategoryWindowStyle);
        }
    }

    private static void DrawSearchWindow(int id)
    {
        Rect tfRect = new(GUIManager.S(5f), GUIManager.S(5f), SearchWindowRect.width - GUIManager.S(10f), GUIManager.S(20f));

        if (RequestSearchFocus)
        {
            ActiveTextFieldId = tfRect.GetHashCode();
            GUI.FocusWindow(999);

            if (Event.current.type == EventType.Repaint)
            {
                RequestSearchFocus = false;
            }
        }

        SearchQuery = DrawManualTextField(tfRect, SearchQuery, Translate("Search..."));
    }
}
