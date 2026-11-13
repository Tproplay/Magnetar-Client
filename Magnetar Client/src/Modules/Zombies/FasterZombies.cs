using Magnetar_Client.UI.Setting;
using System.Collections.Generic;
using Magnetar_Client.Modules.Helpers;



#if MELONLOADER || RELEASE_MELON
using Il2Cpp;
#endif

namespace Magnetar_Client.Modules;

public class FasterZombies : Module
{
    // Mod Info
    public override string Name { get; set; } = "Faster Zombies";
    public override string Description { get; set; } = "Makes the selected zombie(s) faster while the module is active.";
    public override string SearchHints { get; set; } = "fasterzombies speedzombies zombieboost zombiespeed fastzombies " +
        "zombiespeedup quickzombies zombievelocity zombierun runzombies fastzombie zombiespeedmod zombiespeeder " +
        "speedupzombies rapidzombies swiftzombies turbozombies zombieagility zombiequickness zombiealacrity " +
        "zombiefast zombiesprint zombiesprinting zombieoverdrive zombiemovement fasterzombie speedyzombies " +
        "fastmovezombies zombiehurry zombierapid";

    public override ModuleCategory Category { get; set; } = ModuleCategory.Zombie;

    // Mod Data
    public static FasterZombies instance;

    public EntitySelectSetting ZombieSelectedSetting;
    public FloatSetting theSpeedSetting;
    public override bool Active { get; set; }

    private float _lastSpeed = -1f;

    public FasterZombies()
    {
        instance = this;

        CreateCategory("General");

        ZombieSelectedSetting = new EntitySelectSetting("Entities", EntitySelectSetting.Zombie)
        {
            CustomNames = TranslateEnum(typeof(ZombieType)),
            Blacklist = Banned.ZombieTypeBanned,
        };
        ZombieSelectedSetting.SelectAll(setDefault: true);

        theSpeedSetting = new FloatSetting("Speed", 0.1f, 10f, 2f, 3);

        AddSettings(ZombieSelectedSetting, theSpeedSetting);
        EndCategory();
    }

    public override void OnLanguageChanged()
    {
        ZombieSelectedSetting.CustomNames = TranslateEnum(typeof(ZombieType));
    }

    public override void OnEnable()
    {
        _lastSpeed = theSpeedSetting.Value;
        UpdateModifier();
    }

    public override void OnUpdateActive()
    {
        if (theSpeedSetting.Value != _lastSpeed)
        {
            _lastSpeed = theSpeedSetting.Value;
            UpdateModifier();
        }
    }

    public override void OnDisable()
    {
        _lastSpeed = -1f;
        ZombieStatManager.RemoveModifier(Name, StatType.Speed);
    }

    private void UpdateModifier()
    {
        if (!Active) return;

        ZombieStatManager.SetModifier(Name, StatType.Speed, ModifierOp.Multiply, theSpeedSetting.Value,
            z => ZombieSelectedSetting.IsSelected((int)z.theZombieType));
    }
}
