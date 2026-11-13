using Magnetar_Client.UI.Setting;
using System.Collections.Generic;
using UnityEngine;
using Magnetar_Client.Modules.Helpers;



#if MELONLOADER || RELEASE_MELON
using Il2Cpp;
#endif

namespace Magnetar_Client.Modules;

public class SmallerZombies : Module
{
    public override string Name { get; set; } = "Smaller Zombies";
    public override string Description { get; set; } = "Changes the size of selected zombies.\n" +
        "Note: Changing size also affects zombie's speed.";
    public override string SearchHints { get; set; } = "";

    public override ModuleCategory Category { get; set; } = ModuleCategory.Zombie;

    public static SmallerZombies instance;

    public FloatSetting sizeMultiplier;
    public EntitySelectSetting selectedZombies;

    private float _lastMultiplier = -1f;

    public SmallerZombies()
    {
        instance = this;

        CreateCategory("General");

        sizeMultiplier = new FloatSetting("Scale multiplier", 0.5f, 2f, 0.75f, 3, 0f);

        selectedZombies = new EntitySelectSetting("Entities", EntitySelectSetting.Zombie)
        {
            CustomNames = TranslateEnum(typeof(ZombieType)),
            Blacklist = Banned.ZombieTypeBanned,
        };
        selectedZombies.SelectAll(setDefault: true);

        AddSettings(sizeMultiplier, selectedZombies);
        EndCategory();
    }

    public override void OnLanguageChanged()
    {
        selectedZombies.CustomNames = TranslateEnum(typeof(ZombieType));
    }

    public override void OnEnable()
    {
        _lastMultiplier = sizeMultiplier.Value;
        UpdateModifier();
    }

    public override void OnUpdateActive()
    {
        if (sizeMultiplier.Value != _lastMultiplier)
        {
            _lastMultiplier = sizeMultiplier.Value;
            UpdateModifier();
        }
    }

    public override void OnDisable()
    {
        _lastMultiplier = -1f;
        ZombieStatManager.RemoveModifier(Name, StatType.Scale);
    }

    private void UpdateModifier()
    {
        if (!Active) return;

        ZombieStatManager.SetModifier(Name, StatType.Scale, ModifierOp.Multiply, sizeMultiplier.Value,
            z => selectedZombies.IsSelected((int)z.theZombieType));
    }
}
