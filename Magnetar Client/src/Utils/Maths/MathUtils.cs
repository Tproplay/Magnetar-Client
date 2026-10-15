using System;
using System.Text;
using System.Globalization;

namespace Magnetar_Client.Utils;

/// <summary>
/// Contains some maths related functions
/// </summary>
public static class Maths
{
    /// <summary>
    /// Formats a number with suffix like 24.24k, 17B
    /// </summary>
    public static string FormatInternational(long number, int decimalPlaces = 2)
    {
        long absoluteValue = Math.Abs(number);
        decimalPlaces = Math.Max(0, decimalPlaces);
        string format = decimalPlaces > 0 ? "0." + new string('#', decimalPlaces) : "0";

        // Dynamic rounding threshold offset: e.g. for 2 decimal places, 1 - 0.5 * 10^-2 = 0.9995
        double thresholdFactor = 1.0 - (0.5 * Math.Pow(10, -decimalPlaces));

        // Quintillion
        if (absoluteValue >= 1_000_000_000_000_000_000L * thresholdFactor)
            return (number / 1_000_000_000_000_000_000D).ToString(format, CultureInfo.InvariantCulture) + "Qi";

        // Quadrillion
        if (absoluteValue >= 1_000_000_000_000_000L * thresholdFactor)
            return (number / 1_000_000_000_000_000D).ToString(format, CultureInfo.InvariantCulture) + "Q";

        // Trillion
        if (absoluteValue >= 1_000_000_000_000L * thresholdFactor)
            return (number / 1_000_000_000_000D).ToString(format, CultureInfo.InvariantCulture) + "T";

        // Billion
        if (absoluteValue >= 1_000_000_000L * thresholdFactor)
            return (number / 1_000_000_000D).ToString(format, CultureInfo.InvariantCulture) + "B";

        // Million
        if (absoluteValue >= 1_000_000L * thresholdFactor)
            return (number / 1_000_000D).ToString(format, CultureInfo.InvariantCulture) + "M";

        // Thousand
        if (absoluteValue >= 1_000L * thresholdFactor)
            return (number / 1_000D).ToString(format, CultureInfo.InvariantCulture) + "K";

        return number.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Formats a number with suffix like 24.24k, 17B
    /// </summary>
    public static string FormatInternational(double number, int decimalPlaces = 2)
    {
        if (double.IsNaN(number)) return "NaN";
        if (double.IsInfinity(number)) return number.ToString(CultureInfo.InvariantCulture);

        double absoluteValue = Math.Abs(number);
        decimalPlaces = Math.Max(0, decimalPlaces);
        string format = decimalPlaces > 0 ? "0." + new string('#', decimalPlaces) : "0";

        // Dynamic rounding threshold offset based on decimalPlaces:
        // e.g., 2 places -> 0.9995 * unit (999.95), 1 place -> 0.995 * unit (999.5), 0 places -> 0.95 * unit (950)
        double thresholdFactor = 1.0 - (0.5 * Math.Pow(10, -decimalPlaces));

        // Quintillion
        if (absoluteValue >= 1_000_000_000_000_000_000D * thresholdFactor)
            return (number / 1_000_000_000_000_000_000D).ToString(format, CultureInfo.InvariantCulture) + "Qi";

        // Quadrillion
        if (absoluteValue >= 1_000_000_000_000_000D * thresholdFactor)
            return (number / 1_000_000_000_000_000D).ToString(format, CultureInfo.InvariantCulture) + "Q";

        // Trillion
        if (absoluteValue >= 1_000_000_000_000D * thresholdFactor)
            return (number / 1_000_000_000_000D).ToString(format, CultureInfo.InvariantCulture) + "T";

        // Billion
        if (absoluteValue >= 1_000_000_000D * thresholdFactor)
            return (number / 1_000_000_000D).ToString(format, CultureInfo.InvariantCulture) + "B";

        // Million
        if (absoluteValue >= 1_000_000D * thresholdFactor)
            return (number / 1_000_000D).ToString(format, CultureInfo.InvariantCulture) + "M";

        // Thousand
        if (absoluteValue >= 1_000D * thresholdFactor)
            return (number / 1_000D).ToString(format, CultureInfo.InvariantCulture) + "K";

        return number.ToString(format, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Formats time to "HHh MMm SSs" format
    /// </summary>
    public static string FormatTime(long totalSeconds, bool compact = true)
    {
        if (totalSeconds == 0) return "0s";

        bool isNegative = totalSeconds < 0;
        ulong absSeconds = isNegative
            ? (ulong)(-(totalSeconds + 1)) + 1UL
            : (ulong)totalSeconds;

        ulong seconds = absSeconds % 60;
        ulong totalMinutes = absSeconds / 60;
        ulong minutes = totalMinutes % 60;
        ulong totalHours = totalMinutes / 60;

        StringBuilder sb = new StringBuilder(16);
        if (isNegative) sb.Append('-');

        if (compact)
        {
            // Omits zero components: e.g., 3605 -> "1h 5s"
            bool hasHours = totalHours > 0;
            bool hasMinutes = minutes > 0;
            bool hasSeconds = seconds > 0;

            if (hasHours)
            {
                sb.Append(totalHours).Append('h');
            }

            if (hasMinutes)
            {
                if (sb.Length > (isNegative ? 1 : 0)) sb.Append(' ');
                sb.Append(minutes).Append('m');
            }

            if (hasSeconds || (!hasHours && !hasMinutes))
            {
                if (sb.Length > (isNegative ? 1 : 0)) sb.Append(' ');
                sb.Append(seconds).Append('s');
            }
        }
        else
        {
            // Continuous hierarchical layout: e.g., 3605 -> "1h 0m 5s"
            if (totalHours > 0)
            {
                sb.Append(totalHours).Append("h ")
                  .Append(minutes).Append("m ")
                  .Append(seconds).Append('s');
            }
            else if (minutes > 0)
            {
                sb.Append(minutes).Append("m ")
                  .Append(seconds).Append('s');
            }
            else
            {
                sb.Append(seconds).Append('s');
            }
        }

        return sb.ToString();
    }
}

