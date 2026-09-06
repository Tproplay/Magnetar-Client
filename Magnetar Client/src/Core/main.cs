using UnityEngine;
using HarmonyLib;
using System;
using static Magnetar_Client.Utils.Magnetar_Logger;
using Magnetar_Client.Utils;
using Magnetar_Client;



#if MELONLOADER || RELEASE_MELON
using MelonLoader;
[assembly: MelonInfo(typeof(Magnetar_Client.Core.main),Magnetar_Info.ModName, Magnetar_Info.Version, Magnetar_Info.Developer)]
[assembly: MelonGame("LanPiaoPiao", "PlantsVsZombiesRH")]
#elif BEPINEX || RELEASE_BEPINEX
using BepInEx;
using BepInEx.Unity.IL2CPP;
#endif

namespace Magnetar_Client.Core
{
#if MELONLOADER || RELEASE_MELON
    public class main : MelonMod
#elif BEPINEX || RELEASE_BEPINEX
    [BepInPlugin("com.tproplay.magnetar", Magnetar_Info.ModName, Magnetar_Info.Version)]
    public class main : BasePlugin
#endif
    {
        public static main Instance;
        public static HarmonyLib.Harmony HarmonyInstance;

        private readonly float nativeWidth = 1920f;
        private readonly float nativeHeight = 1080f;
        bool hasWarmedUp = false;

#if MELONLOADER || RELEASE_MELON
        public override void OnInitializeMelon()
        {
            Instance = this;
            Utils.Magnetar_Logger.Init();
            HarmonyInstance = new HarmonyLib.Harmony("com.tproplay.magnetar");

            InitializeCore();
        }
#elif BEPINEX || RELEASE_BEPINEX
        public override void Load()
        {
            Instance = this;

            Utils.Magnetar_Logger.Init();
            HarmonyInstance = new HarmonyLib.Harmony("com.tproplay.magnetar");

            SafePatchAll();
            InitializeCore();
            AddComponent<MagnetarHooks>();
        }
#endif

        private void InitializeCore()
        {
            SaveLoad.InitializePrefrences();
            Utils.Translator.LoadTranslations();

            ModuleManager.Init();
            HUDRenderer.Init();
            NEFManager.Init();
            GUIManager.Init();

            TopBar.TopBar.Init();

            ProfileManager.Init();
            SaveLoad.Load();
            DebugLogger.Msg("Magnetar Client Loaded!");
        }

        public void CoreApplicationQuit()
        {
            SaveLoad.Save(true);
            DebugLogger.Msg("Magnetar Prefrences Saved!");
        }

        public void CoreGUI()
        {
            if (!ModuleManager.IsInitialized) return;

            Event e = Event.current;
            if (e == null) return;

            // Only render on layout and repaint passes to prevent IL2CPP native assertion panics
            if (e.type != EventType.Repaint && e.type != EventType.Layout &&
                e.type != EventType.MouseDown && e.type != EventType.MouseUp &&
                e.type != EventType.MouseDrag && e.type != EventType.KeyDown)
            {
                return;
            }

            Matrix4x4 originalMatrix = GUI.matrix;

            try
            {
                float scaleX = (float)Screen.width / nativeWidth;
                float scaleY = (float)Screen.height / nativeHeight;
                float uniformScale = Mathf.Min(scaleX, scaleY);

                float offsetX = (Screen.width - (nativeWidth * uniformScale)) * 0.5f;
                float offsetY = (Screen.height - (nativeHeight * uniformScale)) * 0.5f;

                GUI.matrix = Matrix4x4.TRS(
                    new Vector3(offsetX, offsetY, 0),
                    Quaternion.identity,
                    new Vector3(uniformScale, uniformScale, 1)
                );

                if (!hasWarmedUp)
                {
                    WarmUp();
                    hasWarmedUp = true;
                }

                // Render HUD

                HUDManager.Render();

                // Render Modules
                foreach (var mod in ModuleManager.Modules)
                {
                    mod.OnGUI();
                }

                if (Magnetar_Client.Config.showgui)
                {
                    TopBar.TopBar.Render();

                    if (Magnetar_Client.Config.CurrentTab == TabType.MODULES) ModuleManager.Render();
                    if (Magnetar_Client.Config.CurrentTab == TabType.NEF) NEFManager.Render();
                    if (Magnetar_Client.Config.CurrentTab == TabType.GUI) GUIManager.Render();
                    if (Magnetar_Client.Config.CurrentTab == TabType.PROFILE) ProfileGUI.Render();
                }
            }
            catch (System.Exception ex)
            {
                DebugLogger.Error($"[CoreGUI] Render exception: {ex}");
            }
            finally
            {
                GUI.matrix = originalMatrix;
            }
        }

        public void CoreUpdate()
        {
            if (HUDRenderer.Elements.Count !=0)
                HUDRenderer.UpdateElements();

            if (!ModuleManager.IsInitialized) return;

            if (Input.GetKeyDown(KeyCode.RightShift) && !HUDManager.forceShow)
            {
                Magnetar_Client.Config.showgui = !Magnetar_Client.Config.showgui;
                SaveLoad.Save();
            }

            if (!Magnetar_Client.Config.showgui && !HUDManager.forceShow)
            {
                ModuleManager.HandleHotkeys();
            }

            foreach (var mod in ModuleManager.Modules)
            {
                if (mod != null) mod.OnUpdate();
            }

            if (!hasWarmedUp) return;
            #region handle Escape Key
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape && ModuleManager.showModules)
            {
                Magnetar_Client.Config.showgui = false;
                Event.current.Use();
                SaveLoad.Save();
                ResetInputBind();
            }
            else if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape &&
                !ModuleManager.showModules && ModuleManager.showSettings)
            {
                if (ModuleManager.bindingModuleId == -1 && UI.WindowDrawing.DrawSetting.focusedControlId == -1)
                {
                    ModuleManager.showModules = true;
                    ModuleManager.showSettings = false;
                    ModuleManager.showSelectionGui = false;
                    foreach (var m in ModuleManager.Modules) { m.ShowSettings = false; }
                    ResetInputBind();
                    Event.current.Use();
                }
            }
            else if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape &&
                !ModuleManager.showModules && !ModuleManager.showSettings && ModuleManager.showSelectionGui)
            {
                if (ModuleManager.bindingModuleId == -1 && UI.WindowDrawing.DrawSetting.focusedControlId == -1)
                {
                    ModuleManager.showSettings = true;
                    ModuleManager.showSelectionGui = false;
                    ResetInputBind();
                    Event.current.Use();
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

            UI.Themes.Magnetar_Default.Init();

            DebugLogger.Msg("Now rendering ModuleManager...");
            ModuleManager.Render();
            NEFManager.Render();
            GUIManager.Render();
        }

        public void SafePatchAll()
        {
            var assembly = typeof(main).Assembly;
            Type[] types;

            try { types = assembly.GetTypes(); }
            catch (System.Reflection.ReflectionTypeLoadException e)
            {
                types = System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Where(e.Types, t => t != null));
            }

            int successCount = 0;
            int failCount = 0;

            foreach (var type in types)
            {
                if (type == null) continue;
                try
                {
                    var patchedMethods = HarmonyInstance.CreateClassProcessor(type).Patch();
                    if (patchedMethods != null && patchedMethods.Count > 0) successCount++;
                }
                catch (Exception ex)
                {
                    DebugLogger.Error($"[Harmony] Failed to apply patch '{type.Name}'. Reason: {ex.Message}");
                    failCount++;
                }
            }

            DebugLogger.Msg($"[Harmony] Successfully applied {successCount} patch classes! Failed patches: {failCount}");
        }

#if MELONLOADER || RELEASE_MELON
        public override void OnApplicationQuit() => CoreApplicationQuit();
        public override void OnGUI() => CoreGUI();
        public override void OnUpdate() => CoreUpdate();
#endif

        [HarmonyPatch(typeof(Input), "GetKeyDown", new[] { typeof(KeyCode) })]
        public static class BlockSKeysPatch
        {
            public static bool Prefix(KeyCode key, ref bool __result)
            {
                if ((Magnetar_Client.Config.showgui || HUDManager.forceShow) && key != KeyCode.RightShift)
                {
                    __result = false;
                    return false;
                }
                return true;
            }
        }

        public static string ModsDirectory
        {
            get
            {
#if MELONLOADER || RELEASE_MELON
                return MelonLoader.Utils.MelonEnvironment.ModsDirectory;
#elif BEPINEX || RELEASE_BEPINEX
                return BepInEx.Paths.PluginPath;
#endif
            }
        }
    }

#if BEPINEX || RELEASE_BEPINEX
    public class MagnetarHooks : MonoBehaviour
    {
        void Update() { if (main.Instance != null) main.Instance.CoreUpdate(); }
        void OnGUI() { if (main.Instance != null) main.Instance.CoreGUI(); }
        void OnApplicationQuit() { if (main.Instance != null) main.Instance.CoreApplicationQuit(); }
    }
#endif


}