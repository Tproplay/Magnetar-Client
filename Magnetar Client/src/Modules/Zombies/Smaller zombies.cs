using System.Collections.Generic;
using System.Linq;
using static Magnetar_Client.Game.AppData;
using Magnetar_Client.Game;
using UnityEngine;


#if MELONLOADER || RELEASE_MELON
using Il2Cpp;
#endif

namespace Magnetar_Client.Modules;

public class SmallerZombies : Module
{
    // Mod Info
    public override string Name { get; set; } = "Smaller Zombies";
    public override string Description { get; set; } = "Changes the size of selected zombies.\n" +
        "Note: Changing size also affects zombie's speed.";
    public override string SearchHints { get; set; } = "";

    public override ModuleCategory Category { get; set; } = ModuleCategory.Zombie;

    // Mod Data

    public static SmallerZombies instance;

    public FloatSetting sizeMultiplier;
    public MultiSelectSetting selectedZombies;


    public SmallerZombies()
    {
        instance = this;

        CreateCategory("General");

        sizeMultiplier = new FloatSetting("Scale multiplier", 0.5f, 2f, 0.75f, 3, 0f);

        selectedZombies = new MultiSelectSetting("Entities", typeof(ZombieType))
        {
            MaxSelection = -1,
            CustomNames = TranslatedNames(typeof(ZombieType)),
            Blacklist = Banned.ZombieTypeBanned,
        };
        selectedZombies.Options.Keys.ToList().ForEach(selectedZombies.Select);

        AddSettings(sizeMultiplier, selectedZombies);

        EndCategory();


    }

    public override void OnLanguageChanged()
    {
        selectedZombies.CustomNames = TranslatedNames(typeof(ZombieType));
    }

    // Mod Logic

    Dictionary<Zombie, Vector3> originaltheZombieScale = new();
    public override void OnUpdateActive()
    {
        if (BoardInstanceIsNull) return;

        foreach (var zombie in GameData.zombieList)
        {

            // Check if the zombie is selected and if we haven't already stored its original scale
            if (selectedZombies.IsSelected((int)zombie.theZombieType) &&
                !originaltheZombieScale.ContainsKey(zombie))
            {
                originaltheZombieScale[zombie] = zombie.transform.localScale;
            }

            // Check if the zombie is deselected while the module is running 
            if (!selectedZombies.IsSelected((int)zombie.theZombieType) &&
                originaltheZombieScale.ContainsKey(zombie))
            {
                zombie.transform.localScale = originaltheZombieScale[zombie];
                originaltheZombieScale.Remove(zombie);
            }

            // Update the Scale
            if (originaltheZombieScale.ContainsKey(zombie))
            {
                if (zombie.transform.localScale != originaltheZombieScale[zombie] * sizeMultiplier.Value)
                {
                    zombie.transform.localScale = originaltheZombieScale[zombie] * sizeMultiplier.Value;
                }
            }

        }

    }

    public override void OnDisable()
    {
        foreach (var zombie in GameData.zombieList)
        {
            if (originaltheZombieScale.ContainsKey(zombie))
            {
                zombie.transform.localScale = originaltheZombieScale[zombie];
            }
        }

        originaltheZombieScale.Clear();
    }

}
