using UnityEngine;
using static Magnetar_Client.UI.Themes.ThemeManager;

namespace Magnetar_Client.HUDElements;

#if !ANDROID
public class GpuNameElement : HudElement
{
    private string gpuName;
    string displayText;
    public GpuNameElement() : base("GPU Name", HudElement.NewRect(400))
    {
        gpuName = SystemInfo.graphicsDeviceName;
        RegisterRegexTranslation(@"^<color=cyan>(.*)</color>$", "<color=cyan>{1}</color>");
    }

    protected override void DrawContent(float width, float height)
    {
        GUI.Label(new Rect(5, 5, width - 10, height - 10), displayText, HUDElementStyle);
    }

    public override void OnEnable()
    {
        displayText = Translate($"<color=cyan>{gpuName}</color>");
        AdjustWidthToText(displayText, HUDElementStyle, 10);
    }

    public override void OnLanguageChanged()
    {
        OnEnable();
    }
}

public class CPUElement : HudElement
{
    private string cpuName;
    string displayText;
    public CPUElement() : base("CPU Name", HudElement.NewRect(400))
    {
        cpuName = SystemInfo.processorType;
        RegisterRegexTranslation(@"^<color=cyan>(.*)</color>$", "<color=cyan>{1}</color>");
    }

    protected override void DrawContent(float width, float height)
    {
        GUI.Label(new Rect(5, 5, width - 10, height - 10), displayText, HUDElementStyle);
    }

    public override void OnEnable()
    {
        displayText = Translate($"<color=cyan>{cpuName}</color>");
        AdjustWidthToText(displayText, HUDElementStyle, 10);
    }

    public override void OnLanguageChanged()
    {
        OnEnable();
    }
}
#endif
