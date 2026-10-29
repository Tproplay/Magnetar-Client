using Magnetar_Client.UI;
using Magnetar_Client.UI.Setting;
using Magnetar_Client.UI.Themes;
using UnityEngine;
using static Magnetar_Client.UI.WindowDrawing.DrawSetting;
using static Magnetar_Client.Utils.Translator;

namespace Magnetar_Client.Core.ModuleManager_;

public static class MultiSelectWindowDrawer
{
    public static Rect WindowRect;
    public static MultiSelectSetting ActiveMultiSelect { get; set; }

    private static GUI.WindowFunction _cachedMultiSelectDelegate;
    private static GUI.WindowFunction MultiSelectDelegate => _cachedMultiSelectDelegate ??=
        Il2CppInterop.Runtime.DelegateSupport.ConvertDelegate<GUI.WindowFunction>(DrawBridge);

    public static void InitializeLayout()
    {
        WindowRect = new Rect(
            ClientConfig.NativeWidth / 2f - ModuleManager.MultiSelectWindowWidth / 2f,
            ClientConfig.NativeHeight / 2f - ModuleManager.MultiSelectWindowHeight / 2f,
ModuleManager.MultiSelectWindowWidth,
ModuleManager
.MultiSelectWindowHeight
        );
    }

    public static void Render(Event currentEvent)
    {
        float viewAlpha = AnimationHandler.GetViewAlpha(ModuleManager.Group, ModuleManager.WindowType.SelectionGUI.ToString());
        float currentAlpha = AnimationHandler.CurrentEasedAlpha * viewAlpha;

        if (currentAlpha <= 0.001f) return;

        float targetW = Mathf.Min(ModuleManager.MultiSelectWindowWidth, ClientConfig.NativeWidth * 0.95f);
        float targetH = Mathf.Min(ModuleManager.MultiSelectWindowHeight, ClientConfig.NativeHeight * 0.8f);

        if (ModuleManager.resetWindowPos)
        {
            WindowRect = new Rect(
                (ClientConfig.NativeWidth - targetW) / 2f,
                (ClientConfig.NativeHeight - targetH) / 2f,
                targetW,
                targetH
            );
            ModuleManager.resetWindowPos = false;
        }
        else
        {
            ClientConfig.RescaleAroundCenter(ref WindowRect, targetW, targetH);
        }

        WindowRect = ScreenBoundaryHelper.Clamp(WindowRect);

        if (ActiveMultiSelect != null)
        {
            Color prevColor = GUI.color;
            Color prevContentColor = GUI.contentColor;

            GUI.color = new Color(prevColor.r, prevColor.g, prevColor.b, prevColor.a * currentAlpha);
            GUI.contentColor = new Color(prevContentColor.r, prevContentColor.g, prevContentColor.b, prevContentColor.a * currentAlpha);

            try
            {
                WindowRect = GUI.Window(1000, WindowRect, MultiSelectDelegate, "", ThemeManager.CategoryWindowStyle);

                if (currentEvent != null && WindowRect.Contains(currentEvent.mousePosition) && currentEvent.type == EventType.MouseDown)
                {
                    Input.ResetInputAxes();
                }
            }
            finally
            {
                GUI.color = prevColor;
                GUI.contentColor = prevContentColor;
            }
        }
        else
        {
            ModuleManager.CurrentWindow = ModuleManager.WindowType.Settings;
        }
    }

    private static void DrawBridge(int id)
    {
        Color prevColor = GUI.color;
        Color prevContentColor = GUI.contentColor;
        float viewAlpha = AnimationHandler.GetViewAlpha(ModuleManager.Group, ModuleManager.WindowType.SelectionGUI.ToString());
        float currentAlpha = AnimationHandler.CurrentEasedAlpha * viewAlpha;

        GUI.color = new Color(prevColor.r, prevColor.g, prevColor.b, prevColor.a * currentAlpha);
        GUI.contentColor = new Color(prevContentColor.r, prevContentColor.g, prevContentColor.b, prevContentColor.a * currentAlpha);

        try
        {
            DrawMultiSelectWindow(WindowRect, ActiveMultiSelect, () =>
            {
                ActiveMultiSelect = null;
                ModuleManager.CurrentWindow = ModuleManager.WindowType.Settings;
                AnimationHandler.SwitchView(ModuleManager.Group, ModuleManager.CurrentWindow.ToString());
            });

            float titleHeight = GUIManager.S(34f);
            float closeBtnWidth = GUIManager.S(40f);
            GUI.DragWindow(new Rect(0, 0, WindowRect.width - closeBtnWidth, titleHeight));
        }
        finally
        {
            GUI.color = prevColor;
            GUI.contentColor = prevContentColor;
        }
    }

    public static void HandleMultiSelectSetting(MultiSelectSetting set, ref float y, float width)
    {
        Event e = Event.current;

        bool prevEnabled = GUI.enabled;
        if (set.IsDisabled) GUI.enabled = false;

        Color origColor = GUI.contentColor;
        if (set.IsDisabled) GUI.contentColor = ThemeManager.TextDim;

        string countLabel = null;
        float textW = 0f;

        if (set.ShowCountText)
        {
            if (set.MaxSelection == 1 && set.SelectedValues.Count == 1)
            {
                countLabel = '(' + set.GetDisplayName(System.Linq.Enumerable.First(set.SelectedValues)) + ')';
            }
            else
            {
                countLabel = '(' + Translate($"{set.SelectedValues.Count} selected") + ')';
            }

            textW = (set.Width > 0f) ? GUIManager.S(set.Width) : MultiSelectSetting.MultiSelectCountTextStyle.CalcSize(new GUIContent(countLabel)).x;
        }

        float elemH = ClientConfig.elementHeight;
        float indent = ClientConfig.indent;
        float gap = SettingValues.Gap;
        float selectBtnW = ClientConfig.selectButtonWidth;

        float countStartX = width - indent - textW;
        float btnStartX = set.ShowCountText ? (countStartX - gap - selectBtnW) : (width - indent - selectBtnW);
        float labelW = Mathf.Max(0f, btnStartX - indent - gap);

        Rect labelRect = new(indent, y, labelW, elemH);
        Rect btnRect = new(btnStartX, y, selectBtnW, elemH);

        GUI.Label(labelRect, Translate(set.Name), ThemeManager.SettingLabelStyle);

        if (btnRect.Contains(e.mousePosition) && !set.IsDisabled)
            GUI.backgroundColor = ThemeManager.AccentColor;

        if (e.type == EventType.MouseDown && e.button == 0 && btnRect.Contains(e.mousePosition))
        {
            if (!set.IsDisabled)
            {
                if (set.OnWindowOpen != null)
                {
                    set.OnWindowOpen.Invoke();
                }
                else
                {
                    ActiveMultiSelect = set;
                    ModuleManager.CurrentWindow = ModuleManager.WindowType.SelectionGUI;
                    AnimationHandler.SwitchView(ModuleManager.Group, ModuleManager.WindowType.SelectionGUI.ToString());
                }

                multiSelectSearchQuery = "";
                manualScrollY = 0f;
            }
            e.Use();
        }

        GUI.Box(btnRect, Translate("Select"), ThemeManager.SettingOff);
        GUI.backgroundColor = Color.white;

        if (set.ShowCountText && !string.IsNullOrEmpty(countLabel))
        {
            Rect countRect = new(countStartX, y, textW, elemH);
            GUI.contentColor = ThemeManager.TextDim;
            GUI.Label(countRect, countLabel, MultiSelectSetting.MultiSelectCountTextStyle);
        }

        GUI.contentColor = origColor;
        GUI.enabled = prevEnabled;

        y += elemH + ClientConfig.spacing;
    }
}