using MelonLoader;
using UnityEngine;
using HarmonyLib;
using static Magnetar_Client.Utils.SaveLoad;
using static Magnetar_Client.Utils.Magnetar_Logger;

namespace Magnetar_Client.Core
{


    public class main : MelonMod
    {

        public override void OnInitializeMelon()
        {
            // Init
            Utils.Magnetar_Logger.Init();

            Utils.Translator.LoadTranslations();

            UI.Themes.Magnetar_Default.Init();
            TopBar.TopBar.Init();
            ModuleManager.Init();
            HUDRenderer.Init();
            NEFManager.Init();
            GUIManager.Init();
            

            Load(); // Load Save to override default values

            DebugLogger.Msg("Magnetar Client Loaded!");
            
        }


        public override void OnApplicationQuit()
        {
            Save(true);
            DebugLogger.Msg("Magnetar Prefrences Saved!");
        }

        // Do not configure this or it will Scale the GUI
        private readonly float nativeWidth = 1920f;
        private readonly float nativeHeight = 1080f;


        
        bool hasWarmedUp = false;

        public override void OnGUI()
        {
            

            // Calculate the ratio for scaling
            float rx = (float)(Screen.width) / nativeWidth;
            float ry = (float)(Screen.height) / nativeHeight;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(rx, ry, 1));

            // Render The HUDManager
            HUDManager.Render();

            if (!hasWarmedUp) { WarmUp(); hasWarmedUp = true; DebugLogger.Msg("Warmed Up the Menu!"); }

            // If the menu is hidden, we stop here
            if (!Config.showgui) return;

            TopBar.TopBar.Render();
            if (Config.CurrentTab == TabType.MODULES) ModuleManager.Render();

            if (Config.CurrentTab == TabType.NEF) NEFManager.Render();

            if (Config.CurrentTab == TabType.GUI) GUIManager.Render();

            #region handle Escape Key

            // Modules Window -> None
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape && ModuleManager.showModules)
            { 
                Config.showgui = false;
                Event.current.Use();
                Save();
                ResetInputBind();
#if DEBUG
                DebugLogger.Msg("Escape Triggerd : Modules Window -> None");
#endif
            }

            // Settings Window -> Modules Window
            else if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape &&
                !ModuleManager.showModules && ModuleManager.showSettings)
            {
                if (ModuleManager.bindingModuleId == -1 && UI.WindowDrawing.DrawSetting.focusedControlId == -1)
                {
                    ModuleManager.showModules = true;
                    ModuleManager.showSettings = false;
                    ModuleManager.showSelectionGui = false;

                    // Reset all ShowSettings flags so windows actually close
                    foreach (var m in ModuleManager.Modules)
                    {
                        m.ShowSettings = false;
                    }

                    ResetInputBind();
#if DEBUG
                    DebugLogger.Msg("Escape Triggerd : Settings Window -> Modules Window");
#endif
                    Event.current.Use();
                }
            }

            // Multi Select Window -> Settings Window
            else if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape &&
                !ModuleManager.showModules && !ModuleManager.showSettings && ModuleManager.showSelectionGui)
            {
                if (ModuleManager.bindingModuleId == -1 && UI.WindowDrawing.DrawSetting.focusedControlId == -1)
                {
                    ModuleManager.showSettings = true;
                    ModuleManager.showSelectionGui = false;

                    ResetInputBind();

                    Event.current.Use();
#if DEBUG
                    DebugLogger.Msg("Escape Triggerd : Selection Window -> Settings Window");
#endif
                }
            }

            #endregion
        }

        public static void ResetInputBind()
        {
            Magnetar_Client.UI.WindowDrawing.DrawSetting.activeDropdownId = -1;
            Magnetar_Client.UI.WindowDrawing.DrawSetting.activeSliderId = -1;
            Magnetar_Client.UI.WindowDrawing.DrawSetting.activeTextFieldId = -1;
            ModuleManager.bindingModuleId = -1;
            ModuleManager.activeSliderId = -1;
        }

        public static void WarmUp()
        {
            Magnetar_Client.Utils.LoadFont.Init();
            ModuleManager.Render();
            NEFManager.Render();
            GUIManager.Render();
        }


        public override void OnUpdate()
        {
            if (!ModuleManager.IsInitialized) return;

            // Toggle GUI
            if (Input.GetKeyDown(KeyCode.RightShift) && !HUDManager.forceShow)
            {
                Config.showgui = !Config.showgui;
                Save();
#if DEBUG
                DebugLogger.Msg("RightShift Triggerd : Toggle GUI");
#endif
            }


            // Only run hotkeys when the menu is hidden
            if (!Config.showgui && !HUDManager.forceShow)
            {
                ModuleManager.HandleHotkeys();
            }

            foreach (var mod in ModuleManager.Modules)
            {
                if (mod != null) mod.OnUpdate();
            }

        }





        // Patch the Input method of the game doesn't sees the Input
        [HarmonyPatch(typeof(Input), "GetKeyDown", new[] { typeof(KeyCode) })]
        public static class BlockSKeysPatch
        {
            public static bool Prefix(KeyCode key, ref bool __result)
            {

                if ((Config.showgui || HUDManager.forceShow) && key != KeyCode.RightShift)
                {
                    __result = false;
                    return false;
                }
                return true;
            }
        }

        
    }


}
