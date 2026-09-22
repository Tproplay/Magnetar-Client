using System;
using System.Collections.Generic;
using static Magnetar_Client.Game.AppData;

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
            if (BoardInstanceIsNull) base.Active = value;
        }
    }

    public override bool enableInVanillaMode { get; set; } = true;
    // Mod Data

    public static VanillaMode instance;

    public VanillaMode()
    {
        instance = this;
    }

    List<Module> _classesDisabled = new();

    // Mod Logic
    public override void OnEnable()
    {
        foreach (var module in Core.ModuleManager.Modules)
        {
            Type moduleType = module.GetType();

            if (!module.enableInVanillaMode && module.Active)
            {
                module.Active = false;
                module.OnDisable();
                _classesDisabled.Add(module);
            }
        }
        if (HUDElements.VanillaMode.instance!=null)
        { 
            HUDElements.VanillaMode.instance.VanillaModeEnabled = Active;
            HUDElements.VanillaMode.instance.UpdateText();
        }
    }

    public override void OnDisable()
    {
        if (Active) return;

        foreach (var module in _classesDisabled)
        {
            module.Active = true;
            module.OnEnable();
        }
        _classesDisabled.Clear();
        if (HUDElements.VanillaMode.instance != null)
        {
            HUDElements.VanillaMode.instance.VanillaModeEnabled = Active;
            HUDElements.VanillaMode.instance.UpdateText();
        }
    }

    public bool IsAllowed(Module module)
    {
        if (!Active) return true;

        return module.enableInVanillaMode;

    }

}
