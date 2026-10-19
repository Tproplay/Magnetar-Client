using UnityEngine;
using Magnetar_Client.UI.Setting;
using System.Collections.Generic;

#if MELONLOADER || RELEASE_MELON
using Il2Cpp;
#endif

namespace Magnetar_Client.Modules;

public partial class DebugMode : Module
{
    // Mod Info
    public override string Name { get; set; } = "Debug Mode (for devs)";
    public override string Description { get; set; } = "Logs debug info onto the console.";
    public override string SearchHints { get; set; } = "debugmode debuginfo consolelog loginfo debuglog " +
        "debugconsole logger debugshow debugdata logs debugtools debugconsolelog devlog devmode debugprint " +
        "systemlog debugdump printdebug debugmonitor showlogs debugmessages debugview debugactive loggingmode" +
        " debugtrace debugpanel infolog";

    public override ModuleCategory Category { get; set; } = ModuleCategory.Misc;
    public override bool enableInVanillaMode { get; set; } = true;

    // Mod Data
    public static DebugMode instance;

    public FloatSetting speed;
    public MultiSelectSetting selected;

    public enum Options
    {
        TheGameStatus = 1,
        BoardTag = 2,
        ZombieList,
        PlantList,
        SoundPlayed,
        ZombieAnimations,
        PlantDieReason,
        ZombieDieReason,
        CheatKeys,
        ParticleEmitted,
        AllRecipes,
        CreatePlant_SetPlant,
        AllNodes,
    }

    public static Dictionary<int, string> ParticleNameTranslated;
    private static List<string> CheatKeys = new();
    private static float _time;

    public DebugMode()
    {
        instance = this;

        ParticleNameTranslated = TranslateEnum(typeof(ParticleType));

        CreateCategory("General");

        selected = new MultiSelectSetting("elements")
        {
            Options = new Dictionary<int, string>
            {
                { (int)Options.TheGameStatus, "GameStatus" },
                { (int)Options.BoardTag, "BoardTag" },
                { (int)Options.ZombieList, "ZombieList" },
                { (int)Options.PlantList, "PlantList" },
                { (int)Options.SoundPlayed, "Sound Played" },
                { (int)Options.ZombieAnimations, "Zombie Animations (single use)" },
                { (int)Options.PlantDieReason, "Plant die reason" },
                { (int)Options.ZombieDieReason, "Zombie die reason" },
                { (int)Options.CheatKeys, "Cheat keys (single use)" },
                { (int)Options.ParticleEmitted, "Particle effect emitted" },
                { (int)Options.AllRecipes, "All recipes (single use)" },
                { (int)Options.CreatePlant_SetPlant, "CreatePlant.SetPlant logs" },
                { (int)Options.AllNodes, "All Nodes info (single use)" },
            }
        };
        Settings.Add(selected);

        speed = new FloatSetting("Time between Logs", 0.1f, 10, 1, 2);
        Settings.Add(speed);

        EndCategory();
    }

    public override void OnEnable()
    {
        _time = Time.realtimeSinceStartup;
    }

    public override void OnUpdate()
    {
        base.OnUpdate();
        GameAPPPatch.logged = false;
    }
}