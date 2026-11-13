using Magnetar_Client.UI.Setting;
using System.Collections.Generic;
using Magnetar_Client.Modules.Helpers;



#if MELONLOADER || RELEASE_MELON
using Il2Cpp;
#endif

namespace Magnetar_Client.Modules;

public class BuffZombies : Module
{
    // Mod Info
    public override string Name { get; set; } = "Buff Zombies";
    public override string Description { get; set; } = "Buffs the selected zombie(s) while the module is active.";
    public override string SearchHints { get; set; } = "buffzombies buffzombie bufzombie buffingzombies hpbuff healthboost " +
        "strongzombies zombiehp morehealth tankyzombies zombiefy buffzombs biffzombies buffzombes hpup extrahealth " +
        "buffedzombs healthup zombieboost buffmod buffactive buff-zombies hpplus zombiefying zombiebiff buffzom biffzom " +
        "hpp boost buffzombiez";

    public override ModuleCategory Category { get; set; } = ModuleCategory.Zombie;

    // Mod Data
    public static BuffZombies instance;

    public EntitySelectSetting ZombieSelectedSetting;
    public FloatSetting HpMultiplierSettig;

    public override bool Active { get; set; }

    private float _lastMultiplier = -1f;

    public BuffZombies()
    {
        instance = this;

        CreateCategory("General");

        ZombieSelectedSetting = new EntitySelectSetting("Entities", EntitySelectSetting.Zombie)
        {
            CustomNames = TranslateEnum(typeof(ZombieType)),
            Blacklist = Banned.ZombieTypeBanned,
        };

        ZombieSelectedSetting.SelectAll(setDefault: true);

        HpMultiplierSettig = new FloatSetting("Hp Multiply", 0.1f, 100, 2, 3);

        AddSettings(ZombieSelectedSetting, HpMultiplierSettig);
        EndCategory();
    }

    public override void OnLanguageChanged()
    {
        ZombieSelectedSetting.CustomNames = TranslateEnum(typeof(ZombieType));
    }

    public override void OnEnable()
    {
        _lastMultiplier = HpMultiplierSettig.Value;
        UpdateModifiers();
    }

    public override void OnUpdateActive()
    {
        if (HpMultiplierSettig.Value != _lastMultiplier)
        {
            _lastMultiplier = HpMultiplierSettig.Value;
            UpdateModifiers();
        }
    }

    public override void OnDisable()
    {
        _lastMultiplier = -1f;
        ZombieStatManager.RemoveModifier(Name, StatType.MaxHealth);
        ZombieStatManager.RemoveModifier(Name, StatType.Armor1Max);
        ZombieStatManager.RemoveModifier(Name, StatType.Armor2Max);
    }

    private void UpdateModifiers()
    {
        if (!Active) return;

        float multiplier = HpMultiplierSettig.Value;
        ZombieStatManager.SetModifier(Name, StatType.MaxHealth, ModifierOp.Multiply, multiplier,
            z => ZombieSelectedSetting.IsSelected((int)z.theZombieType));
        ZombieStatManager.SetModifier(Name, StatType.Armor1Max, ModifierOp.Multiply, multiplier,
            z => ZombieSelectedSetting.IsSelected((int)z.theZombieType));
        ZombieStatManager.SetModifier(Name, StatType.Armor2Max, ModifierOp.Multiply, multiplier,
            z => ZombieSelectedSetting.IsSelected((int)z.theZombieType));
    }
}
