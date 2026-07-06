using System;
using System.Collections.Generic;

namespace Magnetar_Client.Utils
{
    public static class Maths
    {
        public static string FormatInternational(long number)
        {
            long absoluteValue = Math.Abs(number);

            if (absoluteValue >= 1_000_000_000_000) // Trillion
                return (number / 1_000_000_000_000D).ToString("0.##") + "T";

            if (absoluteValue >= 1_000_000_000) // Billion
                return (number / 1_000_000_000D).ToString("0.##") + "B";

            if (absoluteValue >= 1_000_000) // Million
                return (number / 1_000_000D).ToString("0.##") + "M";

            if (absoluteValue >= 1_000) // Thousand
                return (number / 1_000D).ToString("0.##") + "K";

            // Return the original number as a string if under 1,000
            return number.ToString();
        }

        public static string FormatInternational(double number)
        {
            if (double.IsNaN(number)) return "NaN";
            if (double.IsInfinity(number)) return number.ToString();

            double absoluteValue = Math.Abs(number);

            // Trillion
            if (absoluteValue >= 1_000_000_000_000D)
                return (number / 1_000_000_000_000D).ToString("0.##") + "T";

            // Billion
            if (absoluteValue >= 1_000_000_000D)
                return (number / 1_000_000_000D).ToString("0.##") + "B";

            // Million
            if (absoluteValue >= 1_000_000D)
                return (number / 1_000_000D).ToString("0.##") + "M";

            // Thousand
            if (absoluteValue >= 1_000D)
                return (number / 1_000D).ToString("0.##") + "K";

            // Return the original number under 1,000 (maintains up to two decimals)
            return number.ToString("0.##");
        }


        public static string FormatTime(long totalSeconds)
        {
            if (totalSeconds == 0) return "0s";

            string prefix = totalSeconds < 0 ? "-" : "";
            TimeSpan t = TimeSpan.FromSeconds(Math.Abs(totalSeconds));

            List<string> parts = new List<string>();

            int totalHours = (t.Days * 24) + t.Hours;

            if (totalHours > 0) parts.Add($"{totalHours}h");
            if (t.Minutes > 0) parts.Add($"{t.Minutes}m");
            if (t.Seconds > 0) parts.Add($"{t.Seconds}s");

            return prefix + string.Join(" ", parts);
        }
    }
    
}
