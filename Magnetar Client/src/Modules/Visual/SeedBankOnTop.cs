using Magnetar_Client.UI.Setting;
using UnityEngine;

#if MELONLOADER || RELEASE_MELON
using Il2Cpp;
#endif
namespace Magnetar_Client.Modules;

public class SeedBankOnTop : Module
{
    // Mod Info
    public override string Name { get; set; } = "Seed Bank On Top";
    public override string Description { get; set; } = "Puts the seed bank on top layer to avoid getting it blocked by Zombies.";
    public override string SearchHints { get; set; } = "seedbankontop seedbank topseedbank seedbanklayer uilayer seedbankfix" +
        " seedbankposition seedbankfront topmostseedbank seedbankordering uiorder seedbankdepth seedbanktop canvasorder" +
        " seedbankoverlay topui seedbankmanager seedbankplacement";
    public override ModuleCategory Category { get; set; } = ModuleCategory.Visual;
    public override bool EnableInVanillaMode { get; set; } = true;

    // Mod Data

    public static SeedBankOnTop instance;

    public SeedBankOnTop()
    {
        instance = this;
        GameData.Actions.OnInGameUIInstantiated += (_) => { if (Active) OnEnable(); };
    }

    public override void OnEnable()
    {
        var comp = GameAPP.canvas.GetComponent<Canvas>();
        if (comp!=null) comp.sortingLayerName = "UI";
    }

    public override void OnDisable()
    {
        var comp = GameAPP.canvas.GetComponent<Canvas>();
        if (comp != null) comp.sortingLayerName = "Default";
    }
}