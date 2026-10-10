using UnityEngine;
using System.Collections.Generic;
using Magnetar_Client.UI.Themes;
using Magnetar_Client.Utils;
using Magnetar_Client.Core;
using Magnetar_Client.UI;

namespace Magnetar_Client.NEF;

public static partial class NEFGUI
{
    public static bool showUsagesView;
    private static bool firstLoad = true;

    public static void DrawNEFWindow(int windowID)
    {
        Color prevColor = GUI.color;
        Color prevContentColor = GUI.contentColor;
        float currentAlpha = AnimationHandler.CurrentEasedAlpha;

        GUI.color = new Color(prevColor.r, prevColor.g, prevColor.b, prevColor.a * currentAlpha);
        GUI.contentColor = new Color(prevContentColor.r, prevContentColor.g, prevContentColor.b, prevContentColor.a * currentAlpha);

        try
        {
            if (firstLoad)
            {
                firstLoad = false;
                NEFData.PerformSearch();
            }

            if (NEFData.currentPyramidRoots.Count > 0 && Mathf.Abs(NEFData.lastCalculatedScale - Config.GUIScale) > 0.001f)
            {
                NEFData.RelayoutCurrentTrees();
            }

            Event e = Event.current;

#if ANDROID
            UpdateMobileLongPress(e);
#endif

            float rightPanelWidth = NEFManager.windowRect.width * 0.3f;
            float titleFontSize = ThemeManager.CategoryWindowStyle != null ? ThemeManager.CategoryWindowStyle.fontSize : GUIManager.S(18f);
            float topIndent = Mathf.Max(GUIManager.S(48f), titleFontSize + GUIManager.S(18f));

            float pad = GUIManager.S(10f);
            float leftPanelWidth = NEFManager.windowRect.width - rightPanelWidth - (pad * 3f);
            float contentHeight = NEFManager.windowRect.height - topIndent - pad;

            Rect pyramidBoxRect = new(pad, topIndent, leftPanelWidth, contentHeight);
            Rect rightPanelRect = new(pad + leftPanelWidth + pad, topIndent, rightPanelWidth, contentHeight);

            // 1. Draw Left Panel (Visualizer or Usages)
            GUI.Box(pyramidBoxRect, "", ThemeManager.CategoryWindowStyle);
            if (showUsagesView)
            {
                DrawUsagesView(pyramidBoxRect, e);
            }
            else
            {
                DrawVisualizerView(pyramidBoxRect, leftPanelWidth, pad, e);
            }

            // 2. Draw Right Panel (Search & Entity Grid)
            DrawSearchAndGridPanel(rightPanelRect, rightPanelWidth, e);
        }
        finally
        {
            GUI.color = prevColor;
            GUI.contentColor = prevContentColor;
        }
    }
}