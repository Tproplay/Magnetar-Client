using Magnetar_Client.UI.Themes;
using UnityEngine;
using static Magnetar_Client.UI.Themes.Magnetar_Default;

namespace Magnetar_Client.UI.HUDElements
{
    public class GpuNameElement : HudElement
    {
        private string gpuName;

        public GpuNameElement() : base("GPU Name", HudElement.NewRect(400))
        {
            gpuName = SystemInfo.graphicsDeviceName;
        }

        protected override void DrawContent(float width, float height)
        {
            string displayText = $"<color=cyan>{gpuName}</color>";

            AdjustWidthToText(displayText, HUDElementStyle, 10);

            GUI.Label(new Rect(5, 5, width - 10, height - 10), displayText, HUDElementStyle);
        }
    }

    public class CPUElement : HudElement
    {
        private string cpuName;
        private int cores;

        public CPUElement() : base("CPU Name", HudElement.NewRect(400))
        {
            cpuName = SystemInfo.processorType;
            cores = SystemInfo.processorCount;
        }

        protected override void DrawContent(float width, float height)
        {
            string displayText = $"<color=cyan>{cpuName}</color>\n";

            AdjustWidthToText(displayText, HUDElementStyle, 10);

            GUI.Label(new Rect(5, 5, width - 10, height - 10), displayText, HUDElementStyle);
        }
    }
}
