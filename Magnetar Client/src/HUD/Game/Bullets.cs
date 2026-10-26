using UnityEngine;
using HarmonyLib;
using static Magnetar_Client.UI.Themes.ThemeManager;
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
        RegisterTranslation("Bullets Spawned: Na", "Bullets Spawned: Na");
        RegisterRegexTranslation(@"^Bullets Spawned: (.*)$", "Bullets Spawned: {1}");
    }

    static int TotalNumberOfBulletsSpawned;

    string displayText = "Bullets Spawned: Na";
    protected override void DrawContent(float width, float height)
    {
        GUI.Label(new Rect(5, 4, width - 10, height - 10), displayText, HUDElementStyle);
    }

    public override void OnUpdateActive()
    {
        displayText = Translate($"Bullets Spawned: {FormatInternational(TotalNumberOfBulletsSpawned)}");
        AdjustWidthToText(displayText, HUDElementStyle, 10f);
    }
    public override void OnEnable()
    {
        displayText = Translate(displayText);
        AdjustWidthToText(displayText, HUDElementStyle, 10f);
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