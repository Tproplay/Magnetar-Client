using System;
using System.Collections.Generic;

namespace Magnetar_Client.Utils;

public static class Maths
{
    public static string FormatInternational(long number)
    {
        long absoluteValue = Math.Abs(number);

        // Quintillion
        if (absoluteValue >= 1_000_000_000_000_000_000)
            return (number / 1_000_000_000_000_000_000D).ToString("0.##") + "Qi";

        // Quadrillion
        if (absoluteValue >= 1_000_000_000_000_000)
            return (number / 1_000_000_000_000_000D).ToString("0.##") + "Q";

        // Trillion
        if (absoluteValue >= 1_000_000_000_000)
            return (number / 1_000_000_000_000D).ToString("0.##") + "Translate";

        // Billion
        if (absoluteValue >= 1_000_000_000)
            return (number / 1_000_000_000D).ToString("0.##") + "B";

        // Million
        if (absoluteValue >= 1_000_000)
            return (number / 1_000_000D).ToString("0.##") + "M";

        // Thousand
        if (absoluteValue >= 1_000)
            return (number / 1_000D).ToString("0.##") + "K";

        return number.ToString();
    }

    public static string FormatInternational(double number)
    {
        if (double.IsNaN(number)) return "NaN";
        if (double.IsInfinity(number)) return number.ToString();

        double absoluteValue = Math.Abs(number);

        // Note: Using 999,950... thresholds fixes floating-point rounding bugs 
        // (e.g., preventing 999,999 from rendering as "1000K" instead of "1M")

        // Quintillion
        if (absoluteValue >= 999_950_000_000_000_000D)
            return (number / 1_000_000_000_000_000_000D).ToString("0.##") + "Qi";

        // Quadrillion
        if (absoluteValue >= 999_950_000_000_000D)
            return (number / 1_000_000_000_000_000D).ToString("0.##") + "Q";

        // Trillion
        if (absoluteValue >= 999_950_000_000D)
            return (number / 1_000_000_000_000D).ToString("0.##") + "Translate";

        // Billion
        if (absoluteValue >= 999_950_000D)
            return (number / 1_000_000_000D).ToString("0.##") + "B";

        // Million
        if (absoluteValue >= 999_950D)
            return (number / 1_000_000D).ToString("0.##") + "M";

        // Thousand
        if (absoluteValue >= 999.95D)
            return (number / 1_000D).ToString("0.##") + "K";

        return number.ToString("0.##");
    }


    public static string FormatTime(long totalSeconds)
    {
        if (totalSeconds == 0) return "0s";

        string prefix = totalSeconds < 0 ? "-" : "";
        TimeSpan t = TimeSpan.FromSeconds(Math.Abs(totalSeconds));

        List<string> parts = new();

        int totalHours = (t.Days * 24) + t.Hours;

        if (totalHours > 0) parts.Add($"{totalHours}h");
        if (t.Minutes > 0) parts.Add($"{t.Minutes}m");
        if (t.Seconds > 0) parts.Add($"{t.Seconds}s");

        return prefix + string.Join(" ", parts);
    }
}

