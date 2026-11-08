using Magnetar_Client.Core;
using UnityEngine;

namespace Magnetar_Client.HUDElements;

public class MagnetarVersion : HudElement
{
    private string displayText;

    public MagnetarVersion() : base("Magnetar Name and Version", HudElement.NewRect(90))
    {
        RegisterRegexTranslation(@"^Magnetar Client v(.*)$", "Magnetar Client v{1}");
    }

    protected override void DrawContent(float width, float height)
    {
        Draw(displayText, width, height);
    }

    public override void OnEnable()
    {
        UpdateDisplay();
    }

    public override void OnLanguageChanged()
    {
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        string format = GetFormat(@"^Magnetar Client v(.*)$", "Magnetar Client v{0}");
        displayText = string.Format(format, MagnetarInfo.Version);
        AdjustWidthToText(displayText, 10f);
    }
}

public class VanillaMode : HudElement
{
    public static VanillaMode instance { get; private set; }

    public VanillaMode() : base("Using Vanilla Mode", HudElement.NewRect(90))
    {
        RegisterTranslation("Active", "Active");
        RegisterTranslation("Inactive", "Inactive");
        RegisterTranslation("Vanilla Mode: <color=yellow>Na</color>", "Vanilla Mode: <color=yellow>Na</color>");
        RegisterRegexTranslation(@"Vanilla Mode: <color={color}>(.*)</color>", "Vanilla Mode: <color={color}>{1}</color>");
    }

    private string _activeText = "Active";
    private string _inactiveText = "Inactive";
    private string _format = "Vanilla Mode: <color={0}>{1}</color>";
    private string displayText = "Vanilla Mode: <color=yellow>Na</color>";

    public bool VanillaModeEnabled;

    protected override void DrawContent(float width, float height)
    {
        Draw(displayText, width, height);
    }

    public override void OnEnable()
    {
        instance = this;
        UpdateTranslations();
        UpdateText();
    }

    public override void OnLanguageChanged()
    {
        UpdateTranslations();
        UpdateText();
    }

    private void UpdateTranslations()
    {
        _activeText = Translate("Active");
        _inactiveText = Translate("Inactive");
        _format = GetFormat(@"Vanilla Mode: <color={color}>(.*)</color>", "Vanilla Mode: <color={0}>{1}</color>");
    }

    public void UpdateText()
    {
        string color = VanillaModeEnabled ? "green" : "red";
        string text = VanillaModeEnabled ? _activeText : _inactiveText;

        displayText = string.Format(_format, color, text);
        AdjustWidthToText(displayText, 10f);
    }
}
