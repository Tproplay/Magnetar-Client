using Il2Cpp;
using Magnetar_Client.Core;
using Magnetar_Client.UI;
using Magnetar_Client.UI.Themes;
using UnityEngine;
using static Magnetar_Client.Utils.Translator;
using static Magnetar_Client.NEF.Data.NEFRecipes;

namespace Magnetar_Client.NEF;

public static partial class NEFGUI
{
    public static string searchQuery = "";
    public static float currentScrollY;

    private static Vector2 _gridTouchStart = Vector2.zero;
    private static float _gridScrollStartVal;
    private static bool _isGridSwiping;

    private static void DrawSearchAndGridPanel(Rect rightPanelRect, float rightPanelWidth, Event e)
    {
        float rx = rightPanelRect.x;
        float ry = rightPanelRect.y;

        string searchLabelText = Translate("Search:");
        GUIStyle labelStyle = ThemeManager.SettingLabelStyle ?? GUI.skin.label;
        float searchLabelWidth = labelStyle.CalcSize(new GUIContent(searchLabelText)).x + GUIManager.S(8f);

        GUI.Label(new Rect(rx, ry, searchLabelWidth, NEFManager.elementHeight), searchLabelText, labelStyle);
        string newQuery = UI.WindowDrawing.DrawSetting.DrawManualTextField(
            new Rect(rx + searchLabelWidth, ry, rightPanelWidth - searchLabelWidth, NEFManager.elementHeight),
            searchQuery, Translate("Search..."));

        if (newQuery != searchQuery)
        {
            searchQuery = newQuery;
            currentScrollY = 0f;
            NEFData.PerformSearch();
        }

        ry += NEFManager.elementHeight + GUIManager.S(6f);

        Rect clearBtnRect = new(rx, ry, rightPanelWidth, NEFManager.elementHeight);
        bool clearHover = clearBtnRect.Contains(e.mousePosition);

        GUI.Box(clearBtnRect, Translate("Clear Search"), ThemeManager.CategoryModuleOffStyle);
        GUI.backgroundColor = Color.white;

        if (clearHover && e.type == EventType.MouseDown && e.button == 0)
        {
            searchQuery = "";
            currentScrollY = 0f;
            NEFData.PerformSearch();
            e.Use();
        }

        ry += NEFManager.elementHeight + GUIManager.S(10f);

#if ANDROID
        GUI.Label(
            new Rect(rx, ry, rightPanelWidth, NEFManager.elementHeight),
            $"{Translate("Results")} ({NEFData.searchResults.Count}) {Translate("| Tap: Recipe | Hold: Usages")}",
            labelStyle
        );
#else
        GUI.Label(
            new Rect(rx, ry, rightPanelWidth, NEFManager.elementHeight),
            $"{Translate("Results")} ({NEFData.searchResults.Count}) {Translate("| L-Click: Recipe | R-Click: Usages")}",
            labelStyle
        );
#endif
        ry += NEFManager.elementHeight + GUIManager.S(4f);

        float scrollHeight = rightPanelRect.height - (ry - rightPanelRect.y);
        Rect scrollRect = new(rx, ry, rightPanelWidth, scrollHeight);

        int columns = Mathf.Max(3, Mathf.FloorToInt(rightPanelWidth / GUIManager.S(100f)));
        float cellPadding = GUIManager.S(5f);
        float itemSize = (rightPanelWidth - (cellPadding * (columns - 1))) / columns;
        int rowCount = Mathf.CeilToInt((float)NEFData.searchResults.Count / columns);
        float totalContentHeight = rowCount * (itemSize + cellPadding);
        float maxScrollY = Mathf.Max(0f, totalContentHeight - scrollRect.height);

        if (scrollRect.Contains(e.mousePosition) && e.type == EventType.ScrollWheel)
        {
            currentScrollY += e.delta.y * GUIManager.S(30f);
            currentScrollY = Mathf.Clamp(currentScrollY, 0f, maxScrollY);
            e.Use();
        }

        if (e.type == EventType.MouseDown && e.button == 0 && scrollRect.Contains(e.mousePosition))
        {
            _gridTouchStart = e.mousePosition;
            _gridScrollStartVal = currentScrollY;
            _isGridSwiping = false;
        }

        if (e.type == EventType.MouseDrag && !_isGridSwiping && scrollRect.Contains(_gridTouchStart))
        {
            if (Vector2.Distance(e.mousePosition, _gridTouchStart) > GUIManager.S(8f))
            {
                _isGridSwiping = true;
#if ANDROID
                _heldEntity = null;
#endif
            }
        }

        if (_isGridSwiping && (e.type == EventType.MouseDrag || e.type == EventType.MouseMove))
        {
            float deltaY = _gridTouchStart.y - e.mousePosition.y;
            currentScrollY = Mathf.Clamp(_gridScrollStartVal + deltaY, 0f, maxScrollY);
            e.Use();
        }

        GUI.BeginGroup(scrollRect);
        if (!PlantMixTreeManager.IsInitialized)
        {
            GUI.Label(new Rect(GUIManager.S(5f), GUIManager.S(5f), rightPanelWidth, GUIManager.S(30f)), Translate("Loading data..."));
        }
        else
        {
            for (int i = 0; i < NEFData.searchResults.Count; i++)
            {
                int col = i % columns;
                int row = i / columns;

                float btnX = col * (itemSize + cellPadding);
                float btnY = row * (itemSize + cellPadding) - currentScrollY;

                if (btnY + itemSize < 0 || btnY > scrollRect.height) continue;

                RecipeEntity entity = NEFData.searchResults[i];
                Rect plantBtnRect = new(btnX, btnY, itemSize, itemSize);

#if ANDROID
                if (plantBtnRect.Contains(e.mousePosition) && e.type == EventType.MouseDown && e.button == 0)
                {
                    _heldEntity = entity;
                    _holdStartTime = Time.realtimeSinceStartup;
                    _holdStartScreenPos = GUIUtility.GUIToScreenPoint(e.mousePosition);
                    _hasTriggeredHold = false;
                }
#endif

                if (!_isGridSwiping && plantBtnRect.Contains(e.mousePosition) && e.type == EventType.MouseUp)
                {
#if ANDROID
                    if (!_hasTriggeredHold && e.button == 0)
                    {
                        if (showUsagesView) UIAnimationHelper.TriggerSubWindowTransition();
                        showUsagesView = false;
                        NEFData.GeneratePyramid(entity);
                        e.Use();
                    }
#else
                    if (e.button == 0)
                    {
                        if (showUsagesView) AnimationHandler.SwitchView(NEFManager.Group, NEFManager.ViewTree);
                        showUsagesView = false;
                        NEFData.GeneratePyramid(entity);
                    }
                    else if (e.button == 1)
                    {
                        if (!showUsagesView) AnimationHandler.SwitchView(NEFManager.Group, NEFManager.ViewUsages);
                        showUsagesView = true;
                        NEFData.GenerateUsagesView(entity);
                    }
                    e.Use();
#endif
                }

                DrawSquareNodeBox(plantBtnRect, entity, 1.5f);
                GUI.backgroundColor = Color.white;
            }
        }
        GUI.EndGroup();

        if (e.type == EventType.MouseUp)
        {
            _isGridSwiping = false;
#if ANDROID
            _heldEntity = null;
            _hasTriggeredHold = false;
#endif
        }
    }
}