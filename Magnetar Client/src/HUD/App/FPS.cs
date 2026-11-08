using UnityEngine;

namespace Magnetar_Client.HUDElements;

public class FpsElement : HudElement
{
    public FpsElement() : base("FPS Counter", HudElement.NewRect(90))
    {
        UpdateInterval = 0.7f;
        RegisterTranslation("FPS: <color=yellow>Na</color>", "FPS: <color=yellow>Na</color>");
        RegisterRegexTranslation(@"FPS: <color={colorName}>(\d+)</color>", "FPS: <color={colorName}>{1}</color>");
    }

    private float fps;
    private string _format = "FPS: <color={0}>{1}</color>";
    private string _naText = "FPS: <color=yellow>Na</color>";
    private string displayText = "FPS: <color=yellow>Na</color>";

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
        _naText = Translate("FPS: <color=yellow>Na</color>");
        _format = GetFormat(@"FPS: <color={colorName}>(\d+)</color>", "FPS: <color={0}>{1}</color>");
    }

    public override void OnUpdateActive()
    {
        if (Time.timeScale != 0)
        {
            fps = 1.0f / Time.smoothDeltaTime;
        }
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        if (fps <= 0 && Time.timeScale == 0)
        {
            displayText = _naText;
        }
        else
        {
            string colorName = fps > 55 ? "lime" : (fps > 30 ? "yellow" : "red");
            displayText = string.Format(_format, colorName, (int)fps);
        }

        AdjustWidthToText(displayText, 10f);
    }
}

public class RealFpsElement : HudElement
{
    public RealFpsElement() : base("Real FPS Counter", HudElement.NewRect(90))
    {
        UpdateInterval = 0.7f;
        RegisterTranslation("FPS (Real): <color=yellow>Na</color>", "FPS (Real): <color=yellow>Na</color>");
        RegisterRegexTranslation(@"FPS (Real): <color={colorName}>(\d+)</color>", "FPS (Real): <color={colorName}>{1}</color>");
    }

    private float fps;
    private string _format = "FPS (Real): <color={0}>{1}</color>";
    private string _naText = "FPS (Real): <color=yellow>Na</color>";
    private string displayText = "FPS (Real): <color=yellow>Na</color>";

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
        _naText = Translate("FPS (Real): <color=yellow>Na</color>");
        _format = GetFormat(@"FPS (Real): <color={colorName}>(\d+)</color>", "FPS (Real): <color={0}>{1}</color>");
    }

    public override void OnUpdateActive()
    {
        fps = 1.0f / Time.unscaledDeltaTime;
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        string colorName = fps > 55 ? "lime" : (fps > 30 ? "yellow" : "red");
        displayText = string.Format(_format, colorName, (int)fps);
        AdjustWidthToText(displayText, 10f);
    }
}

public class FrameTimeElement : HudElement
{
    public FrameTimeElement() : base("Frame Time", HudElement.NewRect(155))
    {
        UpdateInterval = 0.7f;
        RegisterTranslation("FrameTime: <color=yellow>Na</color>", "FrameTime: <color=yellow>Na</color>");
        RegisterRegexTranslation(@"FrameTime: <color={colorName}>(\d+)</color>ms", "FrameTime: <color={colorName}>{1}</color>ms");
    }

    private float currentFrameTime;
    private string _format = "FrameTime: <color={0}>{1:f0}</color>ms";
    private string _naText = "FrameTime: <color=yellow>Na</color>";
    private string displayText = "FrameTime: <color=yellow>Na</color>";

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
        _naText = Translate("FrameTime: <color=yellow>Na</color>");
        _format = GetFormat(@"FrameTime: <color={colorName}>(\d+)</color>ms", "FrameTime: <color={0}>{1:f0}</color>ms");
    }

    public override void OnUpdateActive()
    {
        currentFrameTime = Time.smoothDeltaTime;
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        string colorName = currentFrameTime < 1 / 55f ? "lime" : (currentFrameTime < 1 / 30f ? "yellow" : "red");
        displayText = string.Format(_format, colorName, currentFrameTime * 1000f);
        AdjustWidthToText(displayText, 10f);
    }
}
