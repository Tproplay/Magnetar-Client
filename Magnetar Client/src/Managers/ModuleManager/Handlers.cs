using UnityEngine;

namespace Magnetar_Client.Core.ModuleManager_;

public static class MobileInputHandler
{
    private static Modules.Module _pressedModule = null;
#if ANDROID
    private static float _pressStartTime = 0f;
    private static Vector2 _pressStartScreenPos = Vector2.zero;
    private static bool _hasTriggeredLongPress = false;
    private const float LongPressThreshold = 0.40f; // 400ms hold opens settings
#endif

    public static void Reset()
    {
        _pressedModule = null;
#if ANDROID
        _hasTriggeredLongPress = false;
#endif
    }

#if ANDROID
    public static void OnTouchDown(Modules.Module mod, Rect windowPos, float headerHeight, Vector2 mousePos)
    {
        _pressedModule = mod;
        _pressStartTime = Time.realtimeSinceStartup;
        _pressStartScreenPos = new Vector2(windowPos.x + mousePos.x, windowPos.y + headerHeight + mousePos.y);
        _hasTriggeredLongPress = false;
    }

    public static void OnTouchUp(Modules.Module mod)
    {
        if (_pressedModule == mod && !_hasTriggeredLongPress)
        {
            if (VanillaMode.instance.IsAllowed(mod)) mod.Toggle();
            Reset();
        }
    }
#endif

    public static void Update(Event currentEvent)
    {
#if ANDROID
        if (_pressedModule != null && !_hasTriggeredLongPress)
        {
            float moveDist = Vector2.Distance(currentEvent.mousePosition, _pressStartScreenPos);
            if (moveDist > Config.S(22f))
            {
                _pressedModule = null;
            }
            else if (Time.realtimeSinceStartup - _pressStartTime >= LongPressThreshold)
            {
                _hasTriggeredLongPress = true;
                ModuleManager.OpenModuleSettings(_pressedModule);
                _pressedModule = null;
                if (currentEvent.isMouse) currentEvent.Use();
            }
        }

        if (currentEvent.type == EventType.MouseUp || currentEvent.rawType == EventType.MouseUp)
        {
            if (_pressedModule != null)
            {
                if (!_hasTriggeredLongPress)
                {
                    float moveDist = Vector2.Distance(currentEvent.mousePosition, _pressStartScreenPos);
                    if (moveDist <= Config.S(22f) && VanillaMode.instance.IsAllowed(_pressedModule))
                    {
                        _pressedModule.Toggle();
                    }
                }
                Reset();
            }
        }
#endif
    }
}

public static class ScreenBoundaryHelper
{
    public static Rect Clamp(Rect rect)
    {
        float scaleX = Screen.width / Config.NativeWidth;
        float scaleY = Screen.height / Config.NativeHeight;
        float uniformScale = Mathf.Min(scaleX, scaleY);

        // Calculate the virtual coordinate range visible inside the transformed GUI.matrix
        float virtualWidth = Screen.width / uniformScale;
        float virtualHeight = Screen.height / uniformScale;

        float minX = -(virtualWidth - Config.NativeWidth) * 0.5f;
        float maxX = Config.NativeWidth + ((virtualWidth - Config.NativeWidth) * 0.5f) - rect.width;

        float minY = -(virtualHeight - Config.NativeHeight) * 0.5f;
        float maxY = Config.NativeHeight + ((virtualHeight - Config.NativeHeight) * 0.5f) - rect.height;

        rect.x = Mathf.Clamp(rect.x, minX, Mathf.Max(minX, maxX));
        rect.y = Mathf.Clamp(rect.y, minY, Mathf.Max(minY, maxY));
        return rect;
    }
}