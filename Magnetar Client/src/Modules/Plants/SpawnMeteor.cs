using System.Collections.Generic;
using System.Collections;
using static Magnetar_Client.Game.AppData;
using UnityEngine;

#if MELONLOADER || RELEASE_MELON
using Il2Cpp;
using MelonLoader;
#elif BEPINEX || RELEASE_BEPINEX
using BepInEx.Unity.IL2CPP.Utils;
#endif

using Magnetar_Client.UI.Setting;

namespace Magnetar_Client.Modules;

public class SpawnMeteor : Module
{
    // Mod Info
    public override string Name { get; set; } = "Spawn Meteor";
    public override string Description { get; set; } = "Spawns a meteor on the lawn";
    public override string SearchHints { get; set; } = "spawnmeteor meteorfall meteorshower meteorstrike " +
        "meteorspawn lawnmeteor meteordrop spacehazard impactmeteor meteorcheat meteorshowermod meteorimpact " +
        "summonmeteor fallingstar meteorite meteorsmash skyhazard";

    public override ModuleCategory Category { get; set; } = ModuleCategory.Plant;

    // Mod Data

    public static SpawnMeteor instance;

    public SectionSetting MeteorSectionSetting;

    public SpawnMeteor()
    {
        instance = this;

        CreateCategory("General");

        MeteorSectionSetting = new("Prefrence",
            (index) => new List<Setting>
            {
                new SelectSetting("Meteor Type", 0)
                {
                    Options = new Dictionary<int, string>
                    {
                        { 0, "Passive Meteorite" },
                        { 1, "Active Meteorite" },
                        { 2, "Super Meteorite" },
                        { 3, "Ultimate Meteorite" },
                    }
                },
                new BindSetting("Keybind"),
                new IntSetting("Count of Meteors", 1, 10, 1, 0),
                new FloatSetting("Delay between each meteor", 0, 3, 0, 3, 0)
            }
            );

        AddSettings( MeteorSectionSetting );
        EndCategory();
    }

    public override void OnLanguageChanged()
    {
        
    }

    // Mod Logic

    public override void OnUpdateActive()
    {
        if (BoardInstanceIsNull) return;

        foreach (var instance in MeteorSectionSetting.Sections)
        {
            var bind = (BindSetting)instance.ChildSettings[1];
            if (GetKeyComboDown(bind.BindKeys))
            {
                int type = ((SelectSetting)instance.ChildSettings[0]).Value;
                int count = ((IntSetting)instance.ChildSettings[2]).Value;
                float delay = ((FloatSetting)instance.ChildSettings[3]).Value;
#if MELONLOADER || RELEASE_MELON
                MelonCoroutines.Start(MeteorSpawn(type, count, delay));
#elif BEPINEX || RELEASE_BEPINEX
                MonoBehaviourExtensions.StartCoroutine(BoardInstance, MeteorSpawn(type,count,delay));
#endif
            }
        }
    }

    public static IEnumerator MeteorSpawn(int type, int count, float delay)
    {
        switch (type)
        {
            case 0:
                for (int _  = 0; _ < count; _++)
                {
                    if (BoardInstanceIsNull) yield break;
                    BoardInstance.CreatePassiveMateorite();
                    yield return new WaitForSeconds(delay);

                }
                yield break;

            case 1:
                for (int _ = 0; _ < count; _++)
                {
                    if (BoardInstanceIsNull) yield break;
                    BoardInstance.CreateActiveMateorite();
                    yield return new WaitForSeconds(delay);

                }
                yield break;

            case 2:
                for (int _ = 0; _ < count; _++)
                {
                    if (BoardInstanceIsNull) yield break;
                    BoardInstance.CreateUltimateMateorite();
                    yield return new WaitForSeconds(delay);

                }
                yield break;

            case 3:
                for (int _ = 0; _ < count; _++)
                {
                    if (BoardInstanceIsNull) yield break;
                    BoardInstance.CreateUltimateMateorite2();
                    yield return new WaitForSeconds(delay);

                }
                yield break;

            default:
                yield break;
        }

    }


}
