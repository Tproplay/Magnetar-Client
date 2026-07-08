using static Magnetar_Client.UI.Themes.Magnetar_Default;
using UnityEngine;
using Magnetar_Client.Utils;
namespace Magnetar_Client.HUDElements
{

    public class FpsElement : HudElement
    {
        public FpsElement() : base("FPS Counter", HudElement.NewRect(90))
        { UpdateInterval = 0.7f; }

        float fps;
        string displayText = "FPS: <color=yellow>Na</color>";

        protected override void DrawContent(float width, float height)
        {
            GUI.Label(new Rect(5, 4, width - 10, height - 10), displayText, HUDElementStyle);
        }

        public override void OnUpdateActive()
        {
            if (Time.timeScale != 0)
            {
                fps = 1.0f / Time.smoothDeltaTime;
            }
            string colorName = fps > 55 ? "lime" : (fps > 30 ? "yellow" : "red");

            displayText = $"FPS: <color={colorName}>{(int)fps}</color>";

            AdjustWidthToText(displayText, HUDElementStyle, 10f);
        }
        public override void OnEnable()
        {
            AdjustWidthToText(displayText, HUDElementStyle, 10f);
        }
    }

    public class RealFpsElement : HudElement
    {
        public RealFpsElement() : base("Real FPS Counter", HudElement.NewRect(90))
        { UpdateInterval = 0.7f; }

        float fps;
        string displayText = "FPS (Real): <color=yellow>Na</color>";

        protected override void DrawContent(float width, float height)
        {
            GUI.Label(new Rect(5, 4, width - 10, height - 10), displayText, HUDElementStyle);
        }

        public override void OnUpdateActive()
        {
            fps = 1.0f / Time.unscaledDeltaTime;
            string colorName = fps > 55 ? "lime" : (fps > 30 ? "yellow" : "red");

            displayText = $"FPS (Real): <color={colorName}>{(int)fps}</color>";

            AdjustWidthToText(displayText, HUDElementStyle, 10f);
        }
        public override void OnEnable()
        {
            AdjustWidthToText(displayText, HUDElementStyle, 10f);
        }
    }

    public class FrameTimeElement : HudElement
    {
        public FrameTimeElement() : base("Frame Time", HudElement.NewRect(155))
        { UpdateInterval = 0.7f;}

        float currentFrameTime;
        string displayText = "FrameTime: <color=yellow>Na</color>";

        protected override void DrawContent(float width, float height)
        {
            GUI.Label(new Rect(5, 4, width - 10, height - 10), displayText, HUDElementStyle);
        }


        public override void OnUpdateActive()
        {
            currentFrameTime = Time.smoothDeltaTime;

            string colorName = currentFrameTime < 1 / 55f ? "lime" : (currentFrameTime < 1 / 30f ? "yellow" : "red");

            displayText = $"FrameTime: <color={colorName}>{currentFrameTime * 1000:f0}</color>ms";

            AdjustWidthToText(displayText, HUDElementStyle, 10f);
            
        }
        public override void OnEnable()
        {
            AdjustWidthToText(displayText, HUDElementStyle, 10f);
        }
    }
    
}
