using UnityEngine;

namespace Magnetar_Client.HUDElements;

#if !ANDROID
public class GpuNameElement : HudElement
{
    private string gpuName;

    public GpuNameElement() : base("GPU Name", HudElement.NewRect(400))
    {
        gpuName = SystemInfo.graphicsDeviceName;
    }

    protected override void DrawContent(float width, float height)
    {
        Draw(gpuName, width, height);
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
        AdjustWidthToText(gpuName, 10f);
    }
}

public class CPUElement : HudElement
{
    private string cpuName;

    public CPUElement() : base("CPU Name", HudElement.NewRect(400))
    {
        cpuName = SystemInfo.processorType;
    }

    protected override void DrawContent(float width, float height)
    {
        Draw(cpuName, width, height);
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
        AdjustWidthToText(cpuName, 10f);
    }
}
#endif
