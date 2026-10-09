using Magnetar_Client.Api;
using Magnetar_Client.NEF;
using Magnetar_Client.UI;
using Magnetar_Client.UI.Themes;
using Magnetar_Client.UI.WindowDrawing;
using UnityEngine;
using static Magnetar_Client.Utils.Magnetar_Logger;

namespace Magnetar_Client.Core;

public static class NEFManager
{
    public const string Group = "NEF";
    public const string ViewTree = "Tree";
    public const string ViewUsages = "Usages";

    private const float BaseMargin = 60f;

    public static bool ShowMenu;
    public static float elementHeight => Config.S(25f);
    public static Rect windowRect = new(60, 60, 1000, 700);

    public static void Init()
    {
        AnimationHandler.SetViewImmediate(Group, ViewTree, 1.0f);
        AnimationHandler.SetViewImmediate(Group, ViewUsages, 0.0f);

        NEFData.Init();
        ServiceRegistry.Register(new NEFManagerService());

        SafeToCloseManager.RegisterGuard(() =>
        {
            if (Config.CurrentTab == TabType.NEF)
            {
                return !DrawSetting.IsFocused;
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

    private class NEFManagerService : IInitializable, IWarmUp, IMenuRenderable, ICloseHandler, ILanguageAware
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
            AnimationHandler.RenderWithTabAlpha(TabType.NEF, NEFManager.Render);
        }

        public bool CanClose()
        {
            return !DrawSetting.IsFocused && !NEFGUI.showUsagesView;
        }

        public bool OnEscapePressed()
        {
            if (Config.CurrentTab != TabType.NEF) return false;

            if (DrawSetting.ActiveTextFieldId != -1 || DrawSetting.FocusedControlId != -1 || DrawSetting.ActiveDropdownId != -1)
            {
                DrawSetting.ResetInputBind();
                Input.ResetInputAxes();
                return true;
            }

            if (NEFGUI.showUsagesView)
            {
                NEFGUI.showUsagesView = false;
                AnimationHandler.SwitchView(Group, ViewTree);
                Input.ResetInputAxes();
                return true;
            }

            if (NEFManager.ShowMenu)
            {
                NEFManager.ShowMenu = false;
                Input.ResetInputAxes();
                return true;
            }

            return false;
        }

        public void OnLanguageChanged()
        {
            NEFData.OnLanguageChanged();
        }
    }
}