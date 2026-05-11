using Magnetar_Client.UI.Themes;
using UnityEngine;

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
            string text = $"<color=cyan>{gpuName}</color>";

            Magnetar_Default.HUDElementStyle.richText = true;
            GUI.Label(new Rect(5, 5, width - 10, height - 10), text, Magnetar_Default.HUDElementStyle);
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
            string text = $"<color=cyan>{cpuName}</color>\n";

            Magnetar_Default.HUDElementStyle.richText = true;
            GUI.Label(new Rect(5, 5, width - 10, height - 10), text, Magnetar_Default.HUDElementStyle);
        }
    }
}
