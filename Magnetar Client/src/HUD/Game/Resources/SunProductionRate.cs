using System.Collections.Generic;
using UnityEngine;
using static Magnetar_Client.Utils.Maths;

namespace Magnetar_Client.HUDElements;

public class SunProductionRate : HudElement
{
    public SunProductionRate() : base("Sun Production Rate", HudElement.NewRect(160))
    {
        UpdateInterval = 0.5f;
        RegisterTranslation("Sun Rate: NA", "Sun Rate: NA");
        RegisterRegexTranslation(@"^Sun Rate: \+(.*)/min$", "Sun Rate: +{1}/min");
    }

    private struct SunSample
    {
        public float Time;
        public int Sun;
    }

    private readonly List<SunSample> _samples = new();
    private string _naText = "Sun Rate: NA";
    private string _format = "Sun Rate: +{0}/min";
    private string displayText = "Sun Rate: NA";

    protected override void DrawContent(float width, float height)
    {
        Draw(displayText, width, height);
    }

    public override void OnEnable()
    {
        UpdateTranslations();
        _samples.Clear();
        UpdateDisplay();
    }

    public override void OnDisable()
    {
        _samples.Clear();
    }

    public override void OnLanguageChanged()
    {
        UpdateTranslations();
        UpdateDisplay();
    }

    private void UpdateTranslations()
    {
        _naText = Translate("Sun Rate: NA");
        _format = GetFormat(@"^Sun Rate: \+(.*)/min$", "Sun Rate: +{0}/min");
    }

    public override void OnUpdateActive()
    {
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        if (GameData.BoardInstanceIsNull)
        {
            _samples.Clear();
            displayText = _naText;
            AdjustWidthToText(displayText, 10f);
            return;
        }

        int currentSun = 0;
        if (GameData.BoardInstance.boardStatistics != null)
        {
            currentSun = GameData.BoardInstance.boardStatistics.sunProduced;
        }

        float now = Time.time;

        _samples.Add(new SunSample { Time = now, Sun = currentSun });

        // Keep a 10 second rolling window
        while (_samples.Count > 1 && (now - _samples[0].Time) > 10f)
        {
            _samples.RemoveAt(0);
        }

        float ratePerMin = 0f;
        if (_samples.Count >= 2)
        {
            float dt = now - _samples[0].Time;
            int dSun = currentSun - _samples[0].Sun;

            if (dt >= 1.0f && dSun > 0)
            {
                ratePerMin = (dSun / dt) * 60f;
            }
        }

        displayText = string.Format(_format, FormatInternational(Mathf.RoundToInt(ratePerMin)));
        AdjustWidthToText(displayText, 10f);
    }
}
