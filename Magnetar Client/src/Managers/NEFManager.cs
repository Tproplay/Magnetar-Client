using Magnetar_Client.Api;
using Magnetar_Client.Core.Lifecycle;
using Magnetar_Client.NEF;
using Magnetar_Client.UI;
using Magnetar_Client.UI.Themes;
using Magnetar_Client.UI.WindowDrawing;
using UnityEngine;
using static Magnetar_Client.Utils.Magnetar_Logger;

namespace Magnetar_Client.Core;

public static class NEFManager
{
    // Base (unscaled, GUIScale == 1) margin around the window - actual
    // margin is derived via Config.S() so it scales with the rest of the UI.
    private const float BaseMargin = 60f;

    public static bool ShowMenu = false;
    public static float elementHeight => Config.S(25f);
    public static Rect windowRect = new(60, 60, 1000, 700);

    public static void Init()
    {
        NEFData.Init();

        // Register to centralized ServiceRegistry and SafeToCloseManager
        ServiceRegistry.Register(new NEFManagerService());

        SafeToCloseManager.RegisterGuard(() =>
        {
            if (Config.CurrentTab == TabType.NEF)
            {
                return UI.WindowDrawing.DrawSetting.focusedControlId == -1
                       && UI.WindowDrawing.DrawSetting.activeTextFieldId == -1
                       && UI.WindowDrawing.DrawSetting.activeDropdownId == -1
                       && UI.WindowDrawing.DrawSetting.activeSliderId == -1;
            }
            return true;
        });

        DebugLogger.Msg("Initialized Not Enough Fusions");
    }

    public static void Render()
    {
        float margin = Config.S(BaseMargin);
        windowRect.x = margin;
        windowRect.y = margin;
        windowRect.width = Config.NativeWidth - margin * 2f;
        windowRect.height = Config.NativeHeight - margin * 2f;

        windowRect = GUI.Window(
            2002,
            windowRect,
            (GUI.WindowFunction)NEFGUI.DrawNEFWindow,
            "Not Enough Fusions",
            ThemeManager.CategoryWindowStyle
        );
    }

    private class NEFManagerService : IInitializable, IWarmUp, IMenuRenderable, ICloseHandler
    {
        public string Name => "NEFManager";
        public int Priority => ServicePriority.Standard;

        public void Initialize() { }

        public void OnWarmUp()
        {
            Magnetar_Client.NEF.Data.NEFBanned.InitBan();
            Magnetar_Client.NEF.Data.NEFBanned.InitHidden();
            Magnetar_Client.NEF.Data.NEFRecipes.InitRecipes();
            NEFManager.Render();
        }

        public void OnMenuGUI()
        {
            if (Config.CurrentTab == TabType.NEF)
            {
                NEFManager.Render();
            }
        }

        public bool CanClose()
        {
            return UI.WindowDrawing.DrawSetting.focusedControlId == -1
                   && UI.WindowDrawing.DrawSetting.activeTextFieldId == -1
                   && UI.WindowDrawing.DrawSetting.activeDropdownId == -1
                   && UI.WindowDrawing.DrawSetting.activeSliderId == -1
                   && !NEFGUI.showUsagesView;
        }

        public bool OnEscapePressed()
        {
            if (Config.CurrentTab != TabType.NEF) return false;

            // 1. If searching, typing, or interacting with a NEF filter control, unfocus it first
            if (DrawSetting.activeTextFieldId != -1 || DrawSetting.focusedControlId != -1 || DrawSetting.activeDropdownId != -1)
            {
                Main.ResetInputBind();
                Input.ResetInputAxes();
                return true;
            }

            // 2. If in Usages View, return to Tree view with a smooth transition
            if (NEFGUI.showUsagesView)
            {
                NEFGUI.showUsagesView = false;
                UIAnimationHelper.TriggerSubWindowTransition();
                Input.ResetInputAxes();
                return true;
            }

            // 3. If NEF has an open sub-menu/recipe overlay, dismiss it
            if (NEFManager.ShowMenu)
            {
                NEFManager.ShowMenu = false;
                UIAnimationHelper.TriggerSubWindowTransition();
                Input.ResetInputAxes();
                return true;
            }

            return false;
        }
    }
}