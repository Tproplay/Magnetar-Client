using System.Collections.Generic;
using System.Collections;
using static Magnetar_Client.Game.AppData;
using UnityEngine;
using System.Linq;
using Magnetar_Client.Utils;

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

        var strings = new string[]
        {
            "Passive Meteorite", "Active Meteorite", "Ultimate Meteorite", "Ultimate Meteorite (skin)",
        };

        RegisterTranslations(Translator.CreateDictionary(strings));

        CreateCategory("General");

        MeteorSectionSetting = new("Preferences",
            (index) => new List<Setting>
            {
                new SelectSetting("Meteor Type", 0)
                {
                    Options = new Dictionary<int, string>
                    {
                        { 0, "Passive Meteorite" },
                        { 1, "Active Meteorite" },
                        { 2, "Ultimate Meteorite" },
                        { 3, "Ultimate Meteorite (skin)" },
                    }
                },
                new BindSetting("Keybind"),
                new IntSetting("Count of Meteors", 1, 10, 1, 0),
                new FloatSetting("Delay between each meteor", 0, 3, 0, 3, 0)
            }
            );

        AddSettings(MeteorSectionSetting);
        EndCategory();
    }

    public override void OnLanguageChanged()
    {
        foreach (var instance in MeteorSectionSetting.Sections)
        {
            var select = (SelectSetting)instance.ChildSettings[0];
            select.CustomNames = select.Options.ToDictionary(kvp =>  kvp.Key, kvp => Translate(kvp.Value));
        }
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
                CoroutineManager.Start(MeteorSpawn(type, count, delay));
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
