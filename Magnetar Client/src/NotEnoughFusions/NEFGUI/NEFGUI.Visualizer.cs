using Magnetar_Client.Core;
using Magnetar_Client.UI;
using Magnetar_Client.Utils;
using UnityEngine;
using static Magnetar_Client.NEF.Data.NEFRecipes;
using static Magnetar_Client.UI.WindowDrawing.MiscDrawing;

namespace Magnetar_Client.NEF;

public static partial class NEFGUI
{
    public static Vector2 pyramidPan = Vector2.zero;
    public static float pyramidZoom = 1f;
    public static bool isDraggingPyramid = false;

#if ANDROID
    private static RecipeEntity? _heldEntity = null;
    private static float _holdStartTime = 0f;
    private static Vector2 _holdStartScreenPos = Vector2.zero;
    private static bool _hasTriggeredHold = false;
    private const float LongPressThreshold = 0.40f;

    private static float _lastPinchDistance = -1f;
    private static bool _isPinching = false;

    private static void UpdateMobileLongPress(Event e)
    {
        if (_heldEntity.HasValue && !_hasTriggeredHold)
        {
            Vector2 currentScreenPos = GUIUtility.GUIToScreenPoint(e.mousePosition);
            float moveDist = Vector2.Distance(currentScreenPos, _holdStartScreenPos);

            if (moveDist > Config.S(20f))
            {
                _heldEntity = null;
            }
            else if (Time.realtimeSinceStartup - _holdStartTime >= LongPressThreshold)
            {
                _hasTriggeredHold = true;
                showUsagesView = true;
                UIAnimationHelper.TriggerSubWindowTransition();
                NEFData.GenerateUsagesView(_heldEntity.Value);
                _heldEntity = null;
                e.Use();
            }
        }
    }
#endif

    private static void DrawVisualizerView(Rect pyramidBoxRect, float leftPanelWidth, float pad, Event e)
    {
        if (NEFData.currentPyramidRoots.Count == 0)
        {
            GUI.Label(
                new Rect(pyramidBoxRect.x + pad, pyramidBoxRect.y + pad, leftPanelWidth - (pad * 2f), NEFManager.elementHeight),
                T("Select an entity to view its recipes.")
            );
        }
        else
        {
            GUI.BeginGroup(pyramidBoxRect);

            float minX = NEFData.currentPyramidRoots[0].RenderX;
            float maxX = NEFData.currentPyramidRoots[NEFData.currentPyramidRoots.Count - 1].RenderX;
            float centerOfAllTrees = (minX + maxX) / 2f;

            for (int i = 0; i < NEFData.currentPyramidRoots.Count; i++)
            {
                DrawTree(NEFData.currentPyramidRoots[i], pyramidBoxRect, centerOfAllTrees, e);
            }

            GUI.EndGroup();
        }

        HandleVisualizerInput(pyramidBoxRect, e);
    }

    private static void HandleVisualizerInput(Rect pyramidBoxRect, Event e)
    {
        if (pyramidBoxRect.Contains(e.mousePosition))
        {
            if (e.type == EventType.ScrollWheel)
            {
                float oldZoom = pyramidZoom;
                pyramidZoom -= e.delta.y * 0.05f;
                pyramidZoom = Mathf.Clamp(pyramidZoom, 0.2f, 3.0f);

                float originX = pyramidBoxRect.x + (pyramidBoxRect.width / 2f);
                float originY = pyramidBoxRect.y + Config.S(60f);

                float focusX = (e.mousePosition.x - originX - pyramidPan.x) / oldZoom;
                float focusY = (e.mousePosition.y - originY - pyramidPan.y) / oldZoom;

                pyramidPan.x = e.mousePosition.x - originX - (focusX * pyramidZoom);
                pyramidPan.y = e.mousePosition.y - originY - (focusY * pyramidZoom);

                e.Use();
            }

            if (e.type == EventType.MouseDown && (e.button == 0 || e.button == 2))
            {
                isDraggingPyramid = true;
                e.Use();
            }
        }

#if ANDROID
        if (Input.touchCount >= 2)
        {
            Touch t0 = Input.GetTouch(0);
            Touch t1 = Input.GetTouch(1);

            Vector2 p0 = new(t0.position.x, Screen.height - t0.position.y);
            Vector2 p1 = new(t1.position.x, Screen.height - t1.position.y);

            if (pyramidBoxRect.Contains(p0) || pyramidBoxRect.Contains(p1))
            {
                float currentDist = Vector2.Distance(p0, p1);

                if (t0.phase == TouchPhase.Began || t1.phase == TouchPhase.Began || !_isPinching || _lastPinchDistance <= 0f)
                {
                    _isPinching = true;
                    _lastPinchDistance = currentDist;
                    _heldEntity = null;
                }
                else if (t0.phase == TouchPhase.Moved || t1.phase == TouchPhase.Moved)
                {
                    float deltaDist = currentDist - _lastPinchDistance;
                    if (Mathf.Abs(deltaDist) > 1f)
                    {
                        float oldZoom = pyramidZoom;
                        float zoomFactor = deltaDist * 0.005f;
                        pyramidZoom = Mathf.Clamp(pyramidZoom + zoomFactor, 0.2f, 3.0f);

                        Vector2 pinchCenter = (p0 + p1) * 0.5f;
                        float originX = pyramidBoxRect.x + (pyramidBoxRect.width / 2f);
                        float originY = pyramidBoxRect.y + Config.S(60f);

                        float focusX = (pinchCenter.x - originX - pyramidPan.x) / oldZoom;
                        float focusY = (pinchCenter.y - originY - pyramidPan.y) / oldZoom;

                        pyramidPan.x = pinchCenter.x - originX - (focusX * pyramidZoom);
                        pyramidPan.y = pinchCenter.y - originY - (focusY * pyramidZoom);

                        _lastPinchDistance = currentDist;
                        _heldEntity = null;
                        e.Use();
                    }
                }
            }
        }
        else
        {
            _isPinching = false;
            _lastPinchDistance = -1f;
        }
#endif

        if (isDraggingPyramid && e.type == EventType.MouseDrag)
        {
#if ANDROID
            if (!_isPinching)
            {
                pyramidPan.x += e.delta.x;
                pyramidPan.y -= e.delta.y;
                _heldEntity = null;
            }
#else
            pyramidPan += e.delta;
#endif
            e.Use();
        }

        if (e.type == EventType.MouseUp || e.rawType == EventType.MouseUp)
        {
            isDraggingPyramid = false;
        }
    }

    private static Vector2 GetProjectedPosition(float logicX, float logicY, Rect canvasRect, float centerOfAllTrees)
    {
        float centeredX = logicX - centerOfAllTrees;
        float screenX = (canvasRect.width / 2f) + (centeredX * pyramidZoom) + pyramidPan.x;
        float screenY = Config.S(60f) + (logicY * pyramidZoom) + pyramidPan.y;
        return new Vector2(screenX, screenY);
    }

    private static void DrawTree(NEFData.RecipeNode node, Rect canvasRect, float centerOfAllTrees, Event e)
    {
        if (node == null) return;

        float baseSize = Config.S(100f);
        float scaledSize = baseSize * pyramidZoom;

        Vector2 pos = GetProjectedPosition(node.RenderX, node.RenderY, canvasRect, centerOfAllTrees);
        Rect nodeRect = new(pos.x - (scaledSize / 2f), pos.y, scaledSize, scaledSize);

        if (node.IsSingle)
        {
            Vector2 childPosA = GetProjectedPosition(node.ParentA.RenderX, node.ParentA.RenderY, canvasRect, centerOfAllTrees);
            DrawOrthogonalLine(new Vector2(pos.x, pos.y + scaledSize), new Vector2(childPosA.x, childPosA.y), pyramidZoom);
            DrawTree(node.ParentA, canvasRect, centerOfAllTrees, e);
        }
        else if (node.ParentA != null && node.ParentB != null)
        {
            Vector2 childPosA = GetProjectedPosition(node.ParentA.RenderX, node.ParentA.RenderY, canvasRect, centerOfAllTrees);
            Vector2 childPosB = GetProjectedPosition(node.ParentB.RenderX, node.ParentB.RenderY, canvasRect, centerOfAllTrees);

            DrawOrthogonalLine(new Vector2(pos.x, pos.y + scaledSize), new Vector2(childPosA.x, childPosA.y), pyramidZoom);
            DrawOrthogonalLine(new Vector2(pos.x, pos.y + scaledSize), new Vector2(childPosB.x, childPosB.y), pyramidZoom);

            DrawTree(node.ParentA, canvasRect, centerOfAllTrees, e);
            DrawTree(node.ParentB, canvasRect, centerOfAllTrees, e);

            if (node.IsTriple && node.ParentC != null)
            {
                Vector2 childPosC = GetProjectedPosition(node.ParentC.RenderX, node.ParentC.RenderY, canvasRect, centerOfAllTrees);
                DrawOrthogonalLine(new Vector2(pos.x, pos.y + scaledSize), new Vector2(childPosC.x, childPosC.y), pyramidZoom);
                DrawTree(node.ParentC, canvasRect, centerOfAllTrees, e);
            }
        }

        if (!string.IsNullOrEmpty(node.EdgeMessage))
        {
            Color oldColor = GUI.contentColor;
            GUI.contentColor = new Color(node.EdgeMessageColor.r, node.EdgeMessageColor.g, node.EdgeMessageColor.b, node.EdgeMessageColor.a * GUI.contentColor.a);
            GUIStyle msgStyle = new() { alignment = TextAnchor.LowerCenter, fontSize = Mathf.Max(1, (int)(Config.S(16f) * pyramidZoom)) };

            Rect msgRect = new(pos.x - (Config.S(100f) * pyramidZoom), pos.y - (Config.S(30f) * pyramidZoom), Config.S(200f) * pyramidZoom, Config.S(30f) * pyramidZoom);
            GUI.Label(msgRect, node.EdgeMessage, msgStyle);
            GUI.contentColor = oldColor;
        }

#if ANDROID
        if (nodeRect.Contains(e.mousePosition) && e.type == EventType.MouseDown && e.button == 0)
        {
            _heldEntity = node.Entity;
            _holdStartTime = Time.realtimeSinceStartup;
            _holdStartScreenPos = GUIUtility.GUIToScreenPoint(e.mousePosition);
            _hasTriggeredHold = false;
        }
#endif

        if (nodeRect.Contains(e.mousePosition) && e.type == EventType.MouseUp)
        {
#if ANDROID
            if (!_hasTriggeredHold && e.button == 0)
            {
                NEFData.GeneratePyramid(node.Entity);
                e.Use();
            }
#else
            if (e.button == 0)
            {
                NEFData.GeneratePyramid(node.Entity);
            }
            else if (e.button == 1)
            {
                showUsagesView = true;
                AnimationHandler.SwitchView(NEFManager.Group, NEFManager.ViewUsages);
                NEFData.GenerateUsagesView(node.Entity);
            }
            e.Use();
#endif
        }

        DrawSquareNodeBox(nodeRect, node.Entity, pyramidZoom);
        GUI.backgroundColor = Color.white;
    }
}