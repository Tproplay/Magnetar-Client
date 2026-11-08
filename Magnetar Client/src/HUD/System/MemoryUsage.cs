using System.Diagnostics;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Profiling;

namespace Magnetar_Client.HUDElements;

#if !ANDROID
#region RAM

public class RAMElement : HudElement
{
    private Process currentProcess;
    private string displayValue = "0.0";
    private string _naText = "RAM: <color=yellow>Na</color>";
    private string _format = "RAM: <color={0}>{1} MB</color>";
    private string displayText = "RAM: <color=yellow>Na</color>";

    public RAMElement() : base("RAM Usage", HudElement.NewRect(150))
    {
        currentProcess = Process.GetCurrentProcess();
        UpdateInterval = 1;
        RegisterTranslation("RAM: <color=yellow>Na</color>", "RAM: <color=yellow>Na</color>");
        RegisterRegexTranslation(@"RAM: <color={color}>(\d+(?:\.\d+)?) MB</color>", "RAM: <color={color}>{1} MB</color>");
    }

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
        _naText = Translate("RAM: <color=yellow>Na</color>");
        _format = GetFormat(@"RAM: <color={color}>(\d+(?:\.\d+)?) MB</color>", "RAM: <color={0}>{1} MB</color>");
    }

    public override void OnUpdateActive()
    {
        currentProcess.Refresh();
        long totalBytes = currentProcess.WorkingSet64;
        displayValue = (totalBytes / 1024f / 1024f).ToString("F1");

        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        string color = "white";
        if (float.TryParse(displayValue, out float val) && val > 1500) color = "red";

        displayText = string.Format(_format, color, displayValue);
        AdjustWidthToText(displayText, 10f);
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
    private string totalInstalled = "0";
    private string systemLoad = "0";
    private string _naText = "RAM: <color=yellow>Na</color>";
    private string _format = "RAM: <color=white>{0}% of {1}GB</color>";
    private string displayText = "RAM: <color=yellow>Na</color>";

    public SystemRAMElement() : base("System RAM Usage", HudElement.NewRect(200))
    {
        currentProc = Process.GetCurrentProcess();
        MEMORYSTATUSEX memStatus = new();
        if (GlobalMemoryStatusEx(memStatus))
        {
            totalInstalled = (memStatus.ullTotalPhys / 1024f / 1024f / 1024f).ToString(); // GB
        }
        UpdateInterval = 1;
        RegisterTranslation("RAM: <color=yellow>Na</color>", "RAM: <color=yellow>Na</color>");
        RegisterRegexTranslation(@"RAM: <color=white>(\d+)% of (.*)GB</color>", "RAM: <color=white>{1}% of {2}GB</color>");
    }

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
        _naText = Translate("RAM: <color=yellow>Na</color>");
        _format = GetFormat(@"RAM: <color=white>(\d+)% of (.*)GB</color>", "RAM: <color=white>{0}% of {1}GB</color>");
    }

    public override void OnUpdateActive()
    {
        MEMORYSTATUSEX memStatus = new();
        if (GlobalMemoryStatusEx(memStatus))
            systemLoad = memStatus.dwMemoryLoad.ToString();

        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        string displayGb = totalInstalled.Length > 5 ? totalInstalled.Substring(0, 5) : totalInstalled;
        displayText = string.Format(_format, systemLoad, displayGb);
        AdjustWidthToText(displayText, 10f);
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
    private string _naText = "CPU: <color=yellow>Na</color>";
    private string _format = "CPU: <color={0}>{1:F1}%</color>";
    private string displayText = "CPU: <color=yellow>Na</color>";

    public CPUUsageElement() : base("CPU Usage", HudElement.NewRect(105))
    {
        currentProc = Process.GetCurrentProcess();
        lastCpuTime = currentProc.TotalProcessorTime;
        lastSampleTime = System.DateTime.Now;

        UpdateInterval = 1f;
        RegisterTranslation("CPU: <color=yellow>Na</color>", "CPU: <color=yellow>Na</color>");
        RegisterRegexTranslation(@"CPU: <color={color}>(\d+(?:\.\d+)?)%</color>", "CPU: <color={color}>{1}%</color>");
    }

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
        _naText = Translate("CPU: <color=yellow>Na</color>");
        _format = GetFormat(@"CPU: <color={color}>(\d+(?:\.\d+)?)%</color>", "CPU: <color={0}>{1:F1}%</color>");
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

        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        string color = cpuUsage > 80 ? "red" : (cpuUsage > 40 ? "yellow" : "lime");
        displayText = string.Format(_format, color, Mathf.Clamp(cpuUsage, 0, 100).ToString("F1"));
        AdjustWidthToText(displayText, 10f);
    }
}

#endregion

#region GPU

public class VramUsageElement : HudElement
{
    private int totalVram;
    private float lastVramMb;
    private string _naText = "VRAM: <color=yellow>Na</color>";
    private string _format = "VRAM: {0:F0} / {1} MB";
    private string displayText = "VRAM: <color=yellow>Na</color>";

    public VramUsageElement() : base("VRAM Usage", HudElement.NewRect(200))
    {
        totalVram = SystemInfo.graphicsMemorySize;
        RegisterTranslation("VRAM: <color=yellow>Na</color>", "VRAM: <color=yellow>Na</color>");
        RegisterRegexTranslation(@"^VRAM: (.*) / (.*) MB$", "VRAM: {1} / {2} MB");
    }

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
        _naText = Translate("VRAM: <color=yellow>Na</color>");
        _format = GetFormat(@"^VRAM: (.*) / (.*) MB$", "VRAM: {0:F0} / {1} MB");
    }

    public override void OnUpdateActive()
    {
        long usedBytes = Profiler.GetAllocatedMemoryForGraphicsDriver();

        if (usedBytes <= 0)
        {
            usedBytes = Profiler.GetTotalAllocatedMemory();
        }

        lastVramMb = usedBytes / 1024f / 1024f;
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        float displayVram = Mathf.Min(lastVramMb, totalVram);
        displayText = string.Format(_format, Mathf.RoundToInt(displayVram), totalVram);
        AdjustWidthToText(displayText, 10f);
    }
}

#endregion
#endif
