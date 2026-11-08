using UnityEngine;

#if MELONLOADER || RELEASE_MELON
using Il2Cpp;
using Il2CppCore.RightMenu;
#elif BEPINEX || RELEASE_BEPINEX
using Core.RightMenu;
#endif

namespace Magnetar_Client.Modules;

public class RightClickMenu : Module
{
    public override string Name { get; set; } = "Right Click Menu";
    public override string Description { get; set; } = "Enables the in-game right-click context menu on plants for quick buffs, actions, and utilities.";
    public override string SearchHints { get; set; } = "rightclick right click menu rightclickmenu contextmenu plantmenu devmenu rightmenu core.rightmenu";
    public override ModuleCategory Category { get; set; } = ModuleCategory.Tools;

    public static RightClickMenu instance;
    private RightMenu _cachedMenu;

    public RightClickMenu()
    {
        instance = this;
    }

    public override void OnEnable()
    {
        RightMenu.on = true;

        if (Mouse.Instance == null)
            return;

        // If we previously cached an instance from this level, restore and reactivate it
        if (_cachedMenu != null && _cachedMenu.gameObject != null)
        {
            _cachedMenu.gameObject.SetActive(true);
            if (_cachedMenu.layout != null && _cachedMenu.layout.gameObject != null)
            {
                _cachedMenu.layout.gameObject.SetActive(false);
            }
            Mouse.Instance.rightMenu = _cachedMenu;
        }
        else if (Mouse.Instance.rightMenu != null)
        {
            _cachedMenu = Mouse.Instance.rightMenu;
            _cachedMenu.gameObject.SetActive(true);
        }
        else
        {
            // If turned on mid-level without an existing menu, instantiate under Board.UIParent (UI Canvas)
            Transform parent = null;
            if (!GameData.BoardInstanceIsNull && GameData.BoardInstance != null && GameData.BoardInstance.UIParent != null)
            {
                parent = GameData.BoardInstance.UIParent;
            }
            else if (Board.Instance != null && Board.Instance.UIParent != null)
            {
                parent = Board.Instance.UIParent;
            }
            else if (!GameData.BoardInstanceIsNull && GameData.BoardInstance != null)
            {
                parent = GameData.BoardInstance.transform;
            }

            var prefab = Resources.Load<RightMenu>("UI/RightMenu/RightMenu");
            if (prefab != null)
            {
                var menu = parent != null ? Object.Instantiate(prefab, parent) : Object.Instantiate(prefab);
                if (menu != null)
                {
                    menu.gameObject.SetActive(true);
                    if (menu.layout != null && menu.layout.gameObject != null)
                    {
                        menu.layout.gameObject.SetActive(false);
                    }
                    Mouse.Instance.rightMenu = menu;
                    _cachedMenu = menu;
                }
            }
        }
    }

    public override void OnDisable()
    {
        RightMenu.on = false;

        if (Mouse.Instance != null && Mouse.Instance.rightMenu != null)
        {
            _cachedMenu = Mouse.Instance.rightMenu;

            // Close popup menu if currently visible
            if (_cachedMenu.layout != null && _cachedMenu.layout.gameObject != null)
            {
                _cachedMenu.layout.gameObject.SetActive(false);
            }

            // Deactivate and clear Mouse.Instance.rightMenu so Mouse.MouseClick() doesn't intercept right clicks
            _cachedMenu.gameObject.SetActive(false);
            Mouse.Instance.rightMenu = null;
        }
    }
}
