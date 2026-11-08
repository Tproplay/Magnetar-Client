using HarmonyLib;
using UnityEngine;
using static Magnetar_Client.Utils.Maths;

#if MELONLOADER || RELEASE_MELON
using Il2Cpp;
#endif

namespace Magnetar_Client.HUDElements;

public class TotalFusions : HudElement
{
    public TotalFusions() : base("Total Fusions Performed", HudElement.NewRect(120))
    {
        UpdateInterval = 0.5f;
        RegisterTranslation("Fusions: NA", "Fusions: NA");
        RegisterRegexTranslation(@"^Fusions: (.*)$", "Fusions: {1}");

        GameData.Actions.OnBoardInstantiated += OnBoardReset;
        GameData.Actions.OnBoardDestroyed += OnBoardDestroyed;
    }

    private static int TotalNumberOfFusions;

    private static void OnBoardReset(Board board) => TotalNumberOfFusions = 0;
    private static void OnBoardDestroyed() => TotalNumberOfFusions = 0;

    private string _naText = "Fusions: NA";
    private string _format = "Fusions: {0}";
    private string displayText = "Fusions: NA";

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
        _naText = Translate("Fusions: NA");
        _format = GetFormat(@"^Fusions: (.*)$", "Fusions: {0}");
    }

    public override void OnUpdateActive()
    {
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        if (!GameData.BoardInstanceIsNull)
        {
            displayText = string.Format(_format, FormatInternational(TotalNumberOfFusions));
        }
        else
        {
            displayText = _naText;
        }

        AdjustWidthToText(displayText, 10f);
    }

    [HarmonyPatch(typeof(CreatePlant))]
    private class CreatePlantMixPatch
    {
        [HarmonyPatch(nameof(CreatePlant.MixEvent))]
        [HarmonyPostfix]
        private static void MixEventPostfix()
        {
            TotalNumberOfFusions++;
        }
    }
}
