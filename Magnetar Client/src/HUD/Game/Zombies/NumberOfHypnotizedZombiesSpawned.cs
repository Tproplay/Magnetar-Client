using static Magnetar_Client.UI.Themes.ThemeManager;
using static Magnetar_Client.GameData;
using UnityEngine;
using HarmonyLib;

#if MELONLOADER || RELEASE_MELON
using Il2Cpp;
#endif

namespace Magnetar_Client.HUDElements;

public class NumberOfHypnotizedZombiesSpawned : HudElement
{
    public NumberOfHypnotizedZombiesSpawned() : base("Hypnotized Zombies Spawned", HudElement.NewRect(100))
    { 
        UpdateInterval = 0.2f;
        GameData.OnZombieSpawned += (zombie) => Hypno_Zombies_Spawned += zombie.isMindControlled ? 1 : 0;
        RegisterTranslation("Hypno Zombies Spawned: Na", "Hypno Zombies Spawned: Na");
        RegisterRegexTranslation(@"^Hypno Zombies Spawned: (.*)$", "Hypno Zombies Spawned: {1}");
    }

    string displayText = "Hypno Zombies Spawned: Na";
    protected override void DrawContent(float width, float height)
    {
        GUI.Label(new Rect(5, 4, width - 10, height - 10), displayText, HUDElementStyle);
    }

    public override void OnUpdateActive()
    {
        if (GameData.BoardInstanceIsNull) return;

        displayText = Translate($"Hypno Zombies Spawned: {Hypno_Zombies_Spawned}");

        AdjustWidthToText(displayText, HUDElementStyle, 10f);
    }
    public override void OnEnable()
    {
        displayText = Translate(displayText);
        AdjustWidthToText(displayText, HUDElementStyle, 10f);
    }

    public static int Hypno_Zombies_Spawned;

    [HarmonyPatch(typeof(Zombie))]
    private static class ZombieStatsPatch
    {
        [HarmonyPatch(nameof(Zombie.SetMindControl))]
        [HarmonyPostfix]
        public static void SetMindControlPostfix(Zombie __instance)
        {
            if (__instance == null || BoardInstanceIsNull) return;
            Hypno_Zombies_Spawned++;
        }
    }

    [HarmonyPatch(typeof(Board))]
    public class BoardPatch
    {
        [HarmonyPatch(nameof(Board.Awake))]
        [HarmonyPostfix]
        static void AwakePostFix(Board __instance)
        {
            Hypno_Zombies_Spawned = 0;
        }
    }
}
