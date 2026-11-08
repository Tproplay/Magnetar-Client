using UnityEngine;
using HarmonyLib;
using static Magnetar_Client.Utils.Maths;

#if MELONLOADER || RELEASE_MELON
using Il2Cpp;
#endif

namespace Magnetar_Client.HUDElements;

public class NumberOfBulletsSpawned : HudElement
{
    public NumberOfBulletsSpawned() : base("Number of Bullets Spawned", HudElement.NewRect(100))
    {
        UpdateInterval = 0.5f;
        RegisterTranslation("Bullets Spawned: NA", "Bullets Spawned: NA");
        RegisterRegexTranslation(@"^Bullets Spawned: (.*)$", "Bullets Spawned: {1}");
    }

    private static int TotalNumberOfBulletsSpawned;

    private string _format = "Bullets Spawned: {0}";
    private string displayText = "Bullets Spawned: NA";

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
        _format = GetFormat(@"^Bullets Spawned: (.*)$", "Bullets Spawned: {0}");
    }

    public override void OnUpdateActive()
    {
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        displayText = string.Format(_format, FormatInternational(TotalNumberOfBulletsSpawned));
        AdjustWidthToText(displayText, 10f);
    }

    [HarmonyPatch(typeof(Bullet))]
    private class BulletPatch
    {
        [HarmonyPatch(nameof(Bullet.InitData))]
        [HarmonyPostfix]
        public static void InitDataPatch(Bullet __instance)
        {
            if (__instance == null) return;

            TotalNumberOfBulletsSpawned++;
        }
    }

    [HarmonyPatch(typeof(Board))]
    public class BoardPatch
    {
        [HarmonyPatch(nameof(Board.Awake))]
        [HarmonyPostfix]
        static void AwakePostFix()
        {
            TotalNumberOfBulletsSpawned = 0;
        }
    }
}
