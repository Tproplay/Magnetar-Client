using Magnetar_Client.Core;
using Magnetar_Client.UI;
using Magnetar_Client.UI.Themes;
using UnityEngine;
using static Magnetar_Client.NEF.Data.NEFRecipes;

namespace Magnetar_Client.NEF;

public static partial class NEFGUI
{
    public static float usageScrollY;
    private static Vector2 _usageTouchStart = Vector2.zero;
    private static float _usageScrollStartVal;
    private static bool _isUsageSwiping;

    private static void DrawUsagesView(Rect viewRect, Event e)
    {
        float pad = Config.S(10f);
        float btnW = Config.S(110f);
        float btnH = Config.S(30f);

        GUI.Label(
            new Rect(viewRect.x + pad, viewRect.y + pad, viewRect.width - btnW - (pad * 2f), btnH),
            $"{T("Fusions requiring")}: {NEFData.GetEntityName(NEFData.usageViewTarget)} ({NEFData.currentUsages.Count} {T("found")})"
        );

        Rect backBtnRect = new(viewRect.x + viewRect.width - btnW - pad, viewRect.y + pad, btnW, btnH);
        if (backBtnRect.Contains(e.mousePosition) && e.type == EventType.MouseDown && e.button == 0)
        {
            showUsagesView = false;
            AnimationHandler.SwitchView(NEFManager.Group, NEFManager.ViewTree);
            e.Use();
        }

        GUI.Box(backBtnRect, T("Back to Tree"), ThemeManager.CategoryModuleOffStyle);
        GUI.backgroundColor = Color.white;

        if (NEFData.currentUsages.Count == 0)
        {
            GUI.Label(
                new Rect(viewRect.x + pad, viewRect.y + Config.S(50f), viewRect.width - (pad * 2f), btnH),
                T("This entity is not used as an ingredient in any fusion.")
            );
            return;
        }

        float scrollStartY = viewRect.y + Config.S(50f);
        Rect scrollAreaRect = new(viewRect.x + pad, scrollStartY, viewRect.width - (pad * 2f), viewRect.height - Config.S(60f));

        int columns = Mathf.Max(3, Mathf.FloorToInt(scrollAreaRect.width / Config.S(115f)));
        float padding = Config.S(10f);
        float itemSize = (scrollAreaRect.width - (padding * (columns - 1))) / columns;
        int rowCount = Mathf.CeilToInt((float)NEFData.currentUsages.Count / columns);
        float totalContentHeight = rowCount * (itemSize + padding);
        float maxScroll = Mathf.Max(0f, totalContentHeight - scrollAreaRect.height);

        if (scrollAreaRect.Contains(e.mousePosition) && e.type == EventType.ScrollWheel)
        {
            usageScrollY += e.delta.y * Config.S(30f);
            usageScrollY = Mathf.Clamp(usageScrollY, 0f, maxScroll);
            e.Use();
        }

        if (e.type == EventType.MouseDown && e.button == 0 && scrollAreaRect.Contains(e.mousePosition))
        {
            _usageTouchStart = e.mousePosition;
            _usageScrollStartVal = usageScrollY;
            _isUsageSwiping = false;
        }

        if (e.type == EventType.MouseDrag && !_isUsageSwiping && scrollAreaRect.Contains(_usageTouchStart))
        {
            if (Vector2.Distance(e.mousePosition, _usageTouchStart) > Config.S(8f))
            {
                _isUsageSwiping = true;
#if ANDROID
                _heldEntity = null;
#endif
            }
        }

        if (_isUsageSwiping && (e.type == EventType.MouseDrag || e.type == EventType.MouseMove))
        {
            float deltaY = _usageTouchStart.y - e.mousePosition.y;
            usageScrollY = Mathf.Clamp(_usageScrollStartVal + deltaY, 0f, maxScroll);
            e.Use();
        }

        GUI.BeginGroup(scrollAreaRect);
        for (int i = 0; i < NEFData.currentUsages.Count; i++)
        {
            int col = i % columns;
            int row = i / columns;

            float btnX = col * (itemSize + padding);
            float btnY = row * (itemSize + padding) - usageScrollY;

            if (btnY + itemSize < 0 || btnY > scrollAreaRect.height) continue;

            RecipeEntity resultEntity = NEFData.currentUsages[i].Result;
            Rect plantBtnRect = new(btnX, btnY, itemSize, itemSize);

#if ANDROID
            if (plantBtnRect.Contains(e.mousePosition) && e.type == EventType.MouseDown && e.button == 0)
            {
                _heldEntity = resultEntity;
                _holdStartTime = Time.realtimeSinceStartup;
                _holdStartScreenPos = GUIUtility.GUIToScreenPoint(e.mousePosition);
                _hasTriggeredHold = false;
            }
#endif

            if (!_isUsageSwiping && plantBtnRect.Contains(e.mousePosition) && e.type == EventType.MouseUp)
            {
#if ANDROID
                if (!_hasTriggeredHold && e.button == 0)
                {
                    showUsagesView = false;
                    UIAnimationHelper.TriggerSubWindowTransition();
                    NEFData.GeneratePyramid(resultEntity);
                    e.Use();
                }
#else
                if (e.button == 0)
                {
                    showUsagesView = false;
                    AnimationHandler.SwitchView(NEFManager.Group, NEFManager.ViewTree);
                    NEFData.GeneratePyramid(resultEntity);
                }
                else if (e.button == 1)
                {
                    AnimationHandler.SwitchView(NEFManager.Group, NEFManager.ViewUsages);
                    NEFData.GenerateUsagesView(resultEntity);
                }
                e.Use();
#endif
            }

            DrawSquareNodeBox(plantBtnRect, resultEntity, 1f);
        }
        GUI.EndGroup();

        if (e.type == EventType.MouseUp)
        {
            _isUsageSwiping = false;
#if ANDROID
            _heldEntity = null;
            _hasTriggeredHold = false;
#endif
        }
    }
}