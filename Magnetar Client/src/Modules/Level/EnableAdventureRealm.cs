using System.Collections.Generic;
using Magnetar_Client.UI.Setting;
using Magnetar_Client.UI;
using Magnetar_Client.Api;
using System.IO;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Events;

#if MELONLOADER || RELEASE_MELON
using Il2Cpp;

#endif

namespace Magnetar_Client.Modules;

public class EnableAdventureRealm : Module
{
    // Mod Info
    public override string Name { get; set; } = "Enable Adventure Realm";
    public override string Description { get; set; } = "Allows you to go to new Adventure Realm (incomplete) from the challenges tab.";
    public override string SearchHints { get; set; } = "enablenewabyssmode abyssmode newabyss abyss uncompletedmode challengestab " +
        "secretmode abysmallevel abysslevelabyss abyssmod hiddenmode challengemenu challengeunlocker abyssstage experimentalmode " +
        "unfinishedmode abysschallenge customchallenge abyssstageunlocker";

    public override ModuleCategory Category { get; set; } = ModuleCategory.Level;

    // Mod Data

    public static EnableAdventureRealm instance;

    public EnableAdventureRealm()
    {
        instance = this;
    }

    // Mod Logic

    public override void OnEnable()
    {
        var realmBtn = GameAPP.canvas?.transform.Find("ChallengeMenu(Clone)/Levels/FirstBtns/AbyssLevel");
        realmBtn?.gameObject.SetActive(true);
    }

    public override void OnDisable()
    {
        var realmBtn = GameAPP.canvas.transform.Find("ChallengeMenu(Clone)/Levels/FirstBtns/AbyssLevel");
        realmBtn?.gameObject.SetActive(false);
    }

    [HarmonyPatch(typeof(BaseMenu))]
    public static class BaseMenuPatch
    {
        [HarmonyPatch(nameof(BaseMenu.Awake))]
        [HarmonyPostfix]
        public static void AwakePostfix(BaseMenu __instance)
        {
            if (instance == null || !instance.Active) return;
            if (__instance == null || __instance.gameObject.name!= "ChallengeMenu(Clone)") return;

            var realmBtn = __instance.transform.Find("Levels/FirstBtns/AbyssLevel");
            realmBtn?.gameObject.SetActive(true);

        }
    }
}
