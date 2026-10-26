using Magnetar_Client.Core;
using UnityEngine;
using static Magnetar_Client.UI.Themes.ThemeManager;

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
        GUI.Label(new Rect(5, 4, width - 10, height - 10), displayText, HUDElementStyle);
    }

    public override void OnEnable()
    {
        displayText = Translate($"Magnetar Client v{MagnetarInfo.Version}");
        AdjustWidthToText(displayText, HUDElementStyle, 10f);
    }

    public override void OnLanguageChanged()
    {
        OnEnable();
    }
}


public class VanillaMode : HudElement
{
    public static VanillaMode instance {  get; private set; }
    public VanillaMode() : base("Using Vanilla Mode", HudElement.NewRect(90))
    {
        RegisterTranslation("Active", "Active");
        RegisterTranslation("Inactive", "Inactive");
        RegisterTranslation("Vanilla Mode: <color=yellow>Na</color>", "Vanilla Mode: <color=yellow>Na</color>");
        RegisterRegexTranslation(@"Vanilla Mode: <color={color}>(.*)</color>", "Vanilla Mode: <color={color}>{1}</color>");
    }
    private string displayText = "Vanilla Mode: <color=yellow>Na</color>";

    public bool VanillaModeEnabled;

    protected override void DrawContent(float width, float height)
    {
        GUI.Label(new Rect(5, 4, width - 10, height - 10), displayText, HUDElementStyle);
    }

    public override void OnEnable()
    {
        instance = this;
        UpdateText();
    }

    public override void OnLanguageChanged()
    {
        UpdateText();
    }

    public void UpdateText()
    {
        string color, text;

        if (VanillaModeEnabled) 
        { 
            color = "green";
            text = "Active";
        }
        else 
        { 
            color = "red";
            text = "Inactive";
        }

        displayText = Translate($"Vanilla Mode: <color={color}>{Translate(text)}</color>");
        AdjustWidthToText(displayText, HUDElementStyle, 10f);
    }
}