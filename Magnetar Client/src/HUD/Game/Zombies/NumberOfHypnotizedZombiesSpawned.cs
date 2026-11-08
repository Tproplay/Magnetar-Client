using UnityEngine;
using HarmonyLib;
using static Magnetar_Client.GameData;

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
        RegisterTranslation("Hypno Zombies Spawned: NA", "Hypno Zombies Spawned: NA");
        RegisterRegexTranslation(@"^Hypno Zombies Spawned: (.*)$", "Hypno Zombies Spawned: {1}");
    }

    public static int Hypno_Zombies_Spawned;

    private string _naText = "Hypno Zombies Spawned: NA";
    private string _format = "Hypno Zombies Spawned: {0}";
    private string displayText = "Hypno Zombies Spawned: NA";

    protected override void DrawContent(float width, float height)
    {
        Draw(displayText, width, height);
    }

    public override void OnEnable()
    {
        UpdateTranslations();
        UpdateDisplay();
    }

    public override void OnLanguageChanged()
    {
        UpdateTranslations();
        UpdateDisplay();
    }

    private void UpdateTranslations()
    {
        _naText = Translate("Hypno Zombies Spawned: NA");
        _format = GetFormat(@"^Hypno Zombies Spawned: (.*)$", "Hypno Zombies Spawned: {0}");
    }

    public override void OnUpdateActive()
    {
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        if (!BoardInstanceIsNull)
            displayText = string.Format(_format, Hypno_Zombies_Spawned);
        else
            displayText = _naText;

        AdjustWidthToText(displayText, 10f);
    }

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
        static void AwakePostFix()
        {
            Hypno_Zombies_Spawned = 0;
        }
    }
}
