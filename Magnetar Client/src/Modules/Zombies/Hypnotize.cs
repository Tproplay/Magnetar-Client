using UnityEngine;
using Magnetar_Client.UI.Setting;
using System.Linq;
using static Magnetar_Client.Game.AppData;
using static Magnetar_Client.Game.GameData;
#if MELONLOADER || RELEASE_MELON
using Il2Cpp;
#endif

namespace Magnetar_Client.Modules;

public class HypnotizeZombies : Module
{
    // Mod Info
    public override string Name { get; set; } = "Hypnotize All Zombies";
    public override string Description { get; set; } = "Hypnotizes the selected zombie(s) while the module is active.";
    public override string SearchHints { get; set; } = "hypnotizeallzombies hypnozombies zombieshypnotize hypnotisezombies" +
        " charmzombies zombiecharm mindcontrolzombies zombiesmindcontrol allyzombies zombiealliance brainwashzombies " +
        "turnzombies hypnotizezombie hypnozombie hipnotizezombies hipnozombies hypnatizezombies hypnotiseallzombies " +
        "hypnotisedzombies hypnoallzombies zombiehypnotizer zombiehypnosis zombiespell zombieconvert convertzombies " +
        "friendlyzombies zombiebrainwash zombiemind hypnotiseallzombie";


    public override ModuleCategory Category { get; set; } = ModuleCategory.Zombie;

    // Mod Data

    public static HypnotizeZombies instance;

    public MultiSelectSetting ZombiesSelectedSetting;

    public readonly bool TurnOffAfterUse = true;
    public BoolSetting AutoTurnOff;

    public static float deltaTime = 0;


    public HypnotizeZombies()
    {
        instance = this;

        CreateCategory("General");

        ZombiesSelectedSetting = new MultiSelectSetting("Entities", typeof(ZombieType))
        {
            CustomNames = TranslatedNames(typeof(ZombieType)),
            Blacklist = Banned.ZombieTypeBanned,

        };
        ZombiesSelectedSetting.Options.Keys.ToList().ForEach(ZombiesSelectedSetting.Select);
        Settings.Add(ZombiesSelectedSetting);

        AutoTurnOff = new BoolSetting("Auto Turn Off", TurnOffAfterUse);
        Settings.Add(AutoTurnOff);

        EndCategory();

    }

    public override void OnLanguageChanged()
    {
        ZombiesSelectedSetting.CustomNames = TranslatedNames(typeof(ZombieType));
    }

    // Mod Logic
    public override void OnUpdateActive()
    {
        // Handle auto turn off
        if (AutoTurnOff.Value)
        {
            deltaTime += Time.deltaTime;
            if (deltaTime > 0.3f)
            {
                Active = false;
                deltaTime = 0;
            }
        }

        if (BoardInstanceIsNull) return;

        foreach (var zombie in zombieList)
        {
            if (ZombiesSelectedSetting.IsSelected((int)zombie.theZombieType))
            {
                zombie.SetMindControl(1);
            }
        }
        
    }
}
