using UnityEngine;

namespace Magnetar_Client.HUDElements;

#if !ANDROID
public class GpuNameElement : HudElement
{
    private string gpuName;
    private string displayText;

    public GpuNameElement() : base("GPU Name", HudElement.NewRect(400))
    {
        gpuName = SystemInfo.graphicsDeviceName;
        RegisterRegexTranslation(@"^<color=cyan>(.*)</color>$", "<color=cyan>{1}</color>");
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
        string format = GetFormat(@"^<color=cyan>(.*)</color>$", "<color=cyan>{0}</color>");
        displayText = string.Format(format, gpuName);
        AdjustWidthToText(displayText, 10f);
    }
}

public class CPUElement : HudElement
{
    private string cpuName;
    private string displayText;

    public CPUElement() : base("CPU Name", HudElement.NewRect(400))
    {
        cpuName = SystemInfo.processorType;
        RegisterRegexTranslation(@"^<color=cyan>(.*)</color>$", "<color=cyan>{1}</color>");
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
        string format = GetFormat(@"^<color=cyan>(.*)</color>$", "<color=cyan>{0}</color>");
        displayText = string.Format(format, cpuName);
        AdjustWidthToText(displayText, 10f);
    }
}
#endif
