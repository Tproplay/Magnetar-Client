using System;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using Magnetar_Client.Utils;
using static Magnetar_Client.Utils.Magnetar_Logger;

namespace Magnetar_Client.Core;

public class Main
{
    public static Main Instance { get; private set; }
    public static HarmonyLib.Harmony HarmonyInstance { get; private set; }
    public bool HasWarmedUp { get; private set; } = false;

    public static void Initialize(string harmonyId)
    {
        if (Instance != null) return;
        Instance = new Main();

        Utils.Magnetar_Logger.Init();
        HarmonyInstance = new(harmonyId);

        Instance.SafePatchAll();
        Instance.InitializeCore();
    }

    public void InitializeCore()
    {
        Api.Api.EarlyInitializeCore?.Invoke();

        SaveLoad.InitializePreferences();
        UI.Themes.Magnetar_Default.LoadThemesFromJson();

        ModuleManager.Init();
        HUDRenderer.Init();
        NEFManager.Init();
        TopBar.Init();
        ProfileManager.Init();

        Utils.Translator.LoadTranslations();
        SaveLoad.Load();
        GUIManager.Init();

        Api.Api.LateInitializeCore?.Invoke();

        DebugLogger.Msg("Magnetar Client Loaded!");
    }

    public void OnUpdate()
    {
        UI.GUIHelper._UpdateRainbowColor();

        if (HUDRenderer.Elements.Count != 0)
            HUDRenderer.UpdateElements();

        if (!ModuleManager.IsInitialized) return;

        if (Input.GetKeyDown(KeyCode.RightShift) && !HUDManager.forceShow)
        {
            BlockSKeysPatch.BlockEscKey = true;
            Config.showgui = !Config.showgui;
            SaveLoad.Save();
            Api.Api.OnConfigSaved?.Invoke();
        }

        if (!Config.showgui && !HUDManager.forceShow)
        {
            ModuleManager.HandleHotkeys();
        }

        foreach (var mod in ModuleManager.Modules)
        {
            mod?.OnUpdate();
        }

        Api.Api.OnUpdate?.Invoke();

        if (!HasWarmedUp) return;

        #region Handle Escape Key
        if (!Config.showgui) BlockSKeysPatch.BlockEscKey = false;

        Event currentEvent = Event.current;
        if (currentEvent != null && currentEvent.type == EventType.KeyDown && currentEvent.keyCode == KeyCode.Escape)
        {
            if (ModuleManager.showModules)
            {
                Config.showgui = false;
                currentEvent.Use();
                SaveLoad.Save();
                Api.Api.OnConfigSaved?.Invoke();
                ResetInputBind();
            }
            else if (!ModuleManager.showModules && ModuleManager.showSettings)
            {
                if (ModuleManager.bindingModuleId == -1 && UI.WindowDrawing.DrawSetting.focusedControlId == -1)
                {
                    ModuleManager.showModules = true;
                    ModuleManager.showSettings = false;
                    ModuleManager.showSelectionGui = false;
                    foreach (var m in ModuleManager.Modules) { m.ShowSettings = false; }
                   
                    ResetInputBind();
                    currentEvent.Use();
                }
            }
            else if (!ModuleManager.showModules && !ModuleManager.showSettings && ModuleManager.showSelectionGui)
            {
                if (ModuleManager.bindingModuleId == -1 && UI.WindowDrawing.DrawSetting.focusedControlId == -1)
                {
                    ModuleManager.showSettings = true;
                    ModuleManager.showSelectionGui = false;
                    ResetInputBind();
                    currentEvent.Use();
                }
            }
        }
        #endregion
    }

    public void OnGUI()
    {
        if (!ModuleManager.IsInitialized) return;

        Event e = Event.current;
        if (e == null) return;

        Matrix4x4 originalMatrix = GUI.matrix;

        try
        {
            float scaleX = (float)Screen.width / Config.NativeWidth;
            float scaleY = (float)Screen.height / Config.NativeHeight;
            float uniformScale = Mathf.Min(scaleX, scaleY);

            float offsetX = (Screen.width - (Config.NativeWidth * uniformScale)) * 0.5f;
            float offsetY = (Screen.height - (Config.NativeHeight * uniformScale)) * 0.5f;

            GUI.matrix = Matrix4x4.TRS(
                new Vector3(offsetX, offsetY, 0),
                Quaternion.identity,
                new Vector3(uniformScale, uniformScale, 1)
            );

            if (!HasWarmedUp)
            {
                WarmUp();
                HasWarmedUp = true;
                Api.Api.OnGUIWarmUp?.Invoke();
            }

            UI.Themes.Magnetar_Default.Rescale();

            MobileMenuUI.Render();
            HUDManager.Render();

            foreach (var mod in ModuleManager.Modules)
            {
                mod.OnGUI();
            }

            Api.Api.OnGUI?.Invoke();

            if (Config.showgui)
            {
                TopBar.Render();

                if (Config.CurrentTab == TabType.MODULES) ModuleManager.Render();
                if (Config.CurrentTab == TabType.NEF) NEFManager.Render();
                if (Config.CurrentTab == TabType.GUI) GUIManager.Render();
                if (Config.CurrentTab == TabType.PROFILE) ProfileGUI.Render();
            }
        }
        catch (Exception ex)
        {
            DebugLogger.Error($"[CoreGUI] Render exception: {ex}");
        }
        finally
        {
            GUI.matrix = originalMatrix;
        }
    }

    public void OnApplicationQuit()
    {
        try
        {
            Api.Api.OnApplicationQuit?.Invoke();
        }
        catch (Exception ex)
        {
            DebugLogger.Error($"[Api] Exception in OnApplicationQuit event: {ex}");
        }

        SaveLoad.Save(true);
        Api.Api.OnConfigSaved?.Invoke();
        DebugLogger.Msg("Magnetar Preferences Saved!");
    }

    public static void ResetInputBind()
    {
        UI.WindowDrawing.DrawSetting.activeDropdownId = -1;
        UI.WindowDrawing.DrawSetting.activeSliderId = -1;
        UI.WindowDrawing.DrawSetting.activeTextFieldId = -1;
        ModuleManager.bindingModuleId = -1;
    }

    public static void WarmUp()
    {
        LoadFont.Init();
        UI.Themes.Magnetar_Default.Init();

        ModuleManager.Render();
        NEFManager.Render();
        GUIManager.Render();
    }

    public void SafePatchAll()
    {
        var assembly = typeof(Main).Assembly;
        Type[] types;

        try
        {
            types = assembly.GetTypes();
        }
        catch (System.Reflection.ReflectionTypeLoadException e)
        {
            types = e.Types.Where(t => t != null).ToArray();
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

    [HarmonyPatch(typeof(Input), "GetKeyDown", new[] { typeof(KeyCode) })]
    public static class BlockSKeysPatch
    {
        public static bool BlockEscKey;
        public static bool Prefix(KeyCode key, ref bool __result)
        {
            if ((Config.showgui || HUDManager.forceShow) && key != KeyCode.RightShift)
            {
                __result = false;
                return false;
            }

            if (BlockEscKey && key == KeyCode.Escape)
            {
                __result = false;
                return false;
            }

            return true;
        }
    }
}