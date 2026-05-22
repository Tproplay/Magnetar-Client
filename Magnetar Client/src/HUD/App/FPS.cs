using static Magnetar_Client.UI.Themes.Magnetar_Default;
using UnityEngine;
namespace Magnetar_Client.HUDElements
{

    public class FpsElement : HudElement
    {
        public FpsElement() : base("FPS Counter", HudElement.NewRect(90))
        { }

        float fps;

        protected override void DrawContent(float width, float height)
        {
            string colorName = fps > 55 ? "lime" : (fps > 30 ? "yellow" : "red");

            string displayText = $"<size=18>FPS: <color={colorName}>{(int)fps}</color></size>";

            AdjustWidthToText(displayText, HUDElementStyle, 10f);

            GUI.Label(new Rect(5, 4, width - 10, height - 10), displayText, HUDElementStyle);
        }

        float deltaTime = 1;

        public override void OnUpdateActive()
        {
            deltaTime += Time.deltaTime;

            if (deltaTime >= 1)
            {
                fps = 1.0f / Time.smoothDeltaTime;
                deltaTime = 0;
            }
        }

        public override void OnEnable()
        {
            deltaTime = 1;
        }
    }

    public class FrameTimeElement : HudElement
    {
        public FrameTimeElement() : base("Frame Time", HudElement.NewRect(155))
        { }

        float currentFrameTime;

        protected override void DrawContent(float width, float height)
        {
            string colorName = currentFrameTime < 1/55f ? "lime" : (currentFrameTime < 1/30f ? "yellow" : "red");

            string displayText = $"<size=18>FrameTime: <color={colorName}>{currentFrameTime*1000:f0}</color>ms</size>";

            AdjustWidthToText(displayText, HUDElementStyle, 10f);

            GUI.Label(new Rect(5, 4, width - 10, height - 10), displayText, HUDElementStyle);
        }

        float deltaTime = 1;

        public override void OnUpdateActive()
        {
            deltaTime += Time.deltaTime;

            if (deltaTime >= 1)
            {
                currentFrameTime = Time.smoothDeltaTime;
                deltaTime = 0;
            }
        }

        public override void OnEnable()
        {
            deltaTime = 1;
        }
    }
    
}
