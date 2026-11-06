using System;
using System.Collections.Generic;
using System.Linq;
using Magnetar_Client.Utils;

namespace Magnetar_Client.Modules;

public class VanillaMode : Module
{
    // Mod Info
    public override string Name { get; set; } = "Vanilla Mode";
    public override string Description { get; set; } = "Disables all the modules that provide any kind of gameplay" +
        " advantage until the level is quitted.";
    public override string SearchHints { get; set; } = "vanillamode vanilla puremode vanilla-mode disablecheats" +
        " legitmode cleanmode unmodded vanillaexperience fairplay anticheat legitclean vanilla-game baseline" +
        " disableadvantages fairmode vanilla-rules vanilla-toggle";

    public override ModuleCategory Category { get; set; } = ModuleCategory.Misc;

    public override bool Active {
        get => base.Active;
        set
        {
            if (GameData.BoardInstanceIsNull) base.Active = value;
        }
    }

    public override bool EnableInVanillaMode { get; set; } = true;
    // Mod Data

    public static VanillaMode instance;

    public const string DisabledModulesKey = "DisabledModules";

    public VanillaMode()
    {
        instance = this;
    }

    /// <summary>
    /// Gets the list of module names suppressed by Vanilla Mode.
    /// </summary>
    public List<string> GetSuppressedModuleNames()
    {
        return GetSaved<List<string>>(DisabledModulesKey, null) ?? new List<string>();
    }

    /// <summary>
    /// Checks whether a module with the specified name is currently suppressed by Vanilla Mode.
    /// </summary>
    public bool IsModuleSuppressed(string moduleName)
    {
        if (!Active || string.IsNullOrEmpty(moduleName)) return false;
        var list = GetSaved<List<string>>(DisabledModulesKey, null);
        return list != null && list.Contains(moduleName, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Registers a module name as suppressed by Vanilla Mode.
    /// </summary>
    public void RegisterDisabledModule(string moduleName)
    {
        if (string.IsNullOrEmpty(moduleName)) return;
        var list = GetSuppressedModuleNames();
        if (!list.Contains(moduleName, StringComparer.OrdinalIgnoreCase))
        {
            list.Add(moduleName);
            SetSaved(DisabledModulesKey, list);
        }
    }

    // Mod Logic
    public override void OnEnable()
    {
        var disabledList = GetSuppressedModuleNames();

        foreach (var module in Core.ModuleManager.Modules)
        {
            if (module == null || module == this) continue;

            if (!module.EnableInVanillaMode && module.Active)
            {
                module.Active = false;
                try { module.OnDisable(); } catch { }
                Api.Actions.Invoke_OnModuleDisabled(module);

                if (!disabledList.Contains(module.Name, StringComparer.OrdinalIgnoreCase))
                {
                    disabledList.Add(module.Name);
                }
            }
        }

        SetSaved(DisabledModulesKey, disabledList);

        if (HUDElements.VanillaMode.instance != null)
        { 
            HUDElements.VanillaMode.instance.VanillaModeEnabled = Active;
            HUDElements.VanillaMode.instance.UpdateText();
        }
    }

    public override void OnDisable()
    {
        if (Active) return;

        var disabledList = GetSaved<List<string>>(DisabledModulesKey, null);
        if (disabledList != null && disabledList.Count > 0)
        {
            foreach (var modName in disabledList)
            {
                var module = Core.ModuleManager.Modules.Find(m => string.Equals(m.Name, modName, StringComparison.OrdinalIgnoreCase));
                if (module != null && !module.Active)
                {
                    module.Active = true;
                    try { module.OnEnable(); } catch { }
                    Api.Actions.Invoke_OnModuleEnabled(module);
                }
            }
            disabledList.Clear();
            SetSaved(DisabledModulesKey, disabledList);
            SaveLoad.Save(force: true);
        }

        if (HUDElements.VanillaMode.instance != null)
        {
            HUDElements.VanillaMode.instance.VanillaModeEnabled = Active;
            HUDElements.VanillaMode.instance.UpdateText();
        }
    }

    public bool IsAllowed(Module module)
    {
        if (!Active) return true;

        return module.EnableInVanillaMode;
    }
}
