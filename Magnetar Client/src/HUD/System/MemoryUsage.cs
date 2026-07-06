using Magnetar_Client.UI.Themes;
using System.Diagnostics;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Profiling;
using static Magnetar_Client.UI.Themes.Magnetar_Default;

namespace Magnetar_Client.HUDElements
{
    #region RAM
    public class RAMElement : HudElement
    {
        private Process currentProcess;
        private string displayValue = "0.0";

        public RAMElement() : base("RAM Usage", HudElement.NewRect(150))
        {
            currentProcess = Process.GetCurrentProcess();
            UpdateInterval = 1;
        }

        string displayText = "RAM: <color=yellow>{Na</color>";
        protected override void DrawContent(float width, float height)
        {
            GUI.Label(new Rect(5, 5, width - 5, height - 5), displayText, HUDElementStyle);
        }

        public override void OnUpdateActive()
        {
            currentProcess.Refresh();
            // WorkingSet64 is the total physical memory used by the process
            long totalBytes = currentProcess.WorkingSet64;
            displayValue = (totalBytes / 1024f / 1024f).ToString("F1");

            string color = "white";
            if (float.Parse(displayValue) > 1500) color = "red"; // Over 2GB turns red

            displayText = $"RAM: <color={color}>{displayValue} MB</color>";

            AdjustWidthToText(displayText, HUDElementStyle, 10);
        }
        public override void OnEnable()
        {
            AdjustWidthToText(displayText, HUDElementStyle, 10f);
        }
    }

    public class SystemRAMElement : HudElement
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private class MEMORYSTATUSEX
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;
            public MEMORYSTATUSEX() { this.dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX)); }
        }

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);

        private Process currentProc;
        private string totalInstalled;
        private string systemLoad;
        private float nextActionTime = 0f;

        public SystemRAMElement() : base("System RAM Usage", HudElement.NewRect(200))
        {
            currentProc = Process.GetCurrentProcess();
            MEMORYSTATUSEX memStatus = new MEMORYSTATUSEX();
            if (GlobalMemoryStatusEx(memStatus))
            {
                totalInstalled = (memStatus.ullTotalPhys / 1024f / 1024f / 1024f).ToString(); // GB
            }
            UpdateInterval = 1;
        }

        string displayText = "RAM: <color=yellow>Na</color>";
        protected override void DrawContent(float width, float height)
        {
            GUI.Label(new Rect(5, 5, width - 5, height - 5), displayText, HUDElementStyle);
        }

        public override void OnUpdateActive()
        {
            nextActionTime = Time.time + 1.5f;
            MEMORYSTATUSEX memStatus = new MEMORYSTATUSEX();
            if (GlobalMemoryStatusEx(memStatus))
                systemLoad = memStatus.dwMemoryLoad.ToString(); // % total system use

            displayText = $"RAM: <color=white>{systemLoad}% of {totalInstalled.Substring(0, 5)}GB</color>";

            AdjustWidthToText(displayText, HUDElementStyle, 10);
        }
    }

    #endregion

    #region CPU

    public class CPUUsageElement : HudElement
    {
        private Process currentProc;
        private System.TimeSpan lastCpuTime;
        private System.DateTime lastSampleTime;
        private float cpuUsage;

        public CPUUsageElement() : base("CPU Usage", HudElement.NewRect(105))
        {
            currentProc = Process.GetCurrentProcess();
            lastCpuTime = currentProc.TotalProcessorTime;
            lastSampleTime = System.DateTime.Now;

            UpdateInterval = 1f;
        }

        string displayText = "CPU: <color=yellow>Na</color>";

        protected override void DrawContent(float width, float height)
        {
            GUI.Label(new Rect(5, 5, width - 10, height - 10), displayText, HUDElementStyle);
        }

        public override void OnUpdateActive()
        {
            System.DateTime currentTime = System.DateTime.Now;
            System.TimeSpan currentCpuTime = currentProc.TotalProcessorTime;

            double timeDiff = (currentTime - lastSampleTime).TotalMilliseconds;
            double cpuDiff = (currentCpuTime - lastCpuTime).TotalMilliseconds;

            if (timeDiff > 0)
            {
                cpuUsage = (float)(cpuDiff / (System.Environment.ProcessorCount * timeDiff)) * 100f;
            }

            lastCpuTime = currentCpuTime;
            lastSampleTime = currentTime;

            string color = cpuUsage > 80 ? "red" : (cpuUsage > 40 ? "yellow" : "lime");
            displayText = $"CPU: <color={color}>{Mathf.Clamp(cpuUsage, 0, 100):F1}%</color>";

            AdjustWidthToText(displayText, HUDElementStyle, 10);
        }
        public override void OnEnable()
        {
            AdjustWidthToText(displayText, HUDElementStyle, 10f);
        }
    }

    #endregion

    #region GPU

    public class VramUsageElement : HudElement
    {
        private int totalVram;
        private float lastVramMb;

        public VramUsageElement() : base("VRAM Usage", HudElement.NewRect(200))
        {
            totalVram = SystemInfo.graphicsMemorySize;
        }

        string displayText = "VRAM: <color=yellow>Na</color>";
        protected override void DrawContent(float width, float height)
        {
            GUI.Label(new Rect(5, 5, width - 10, height - 10), displayText, HUDElementStyle);
        }

        public override void OnUpdateActive()
        {
            long usedBytes = Profiler.GetAllocatedMemoryForGraphicsDriver();

            // Fallback if GfxDriver returns 0
            if (usedBytes <= 0)
            {
                usedBytes = Profiler.GetTotalAllocatedMemory();
            }

            lastVramMb = usedBytes / 1024f / 1024f;

            float displayVram = Mathf.Min(lastVramMb, totalVram);

            displayText = $"VRAM: {displayVram:F0} / {totalVram} MB";

            AdjustWidthToText(displayText, HUDElementStyle, 10);
        }
        public override void OnEnable()
        {
            AdjustWidthToText(displayText, HUDElementStyle, 10f);
        }
    }


    #endregion
}
