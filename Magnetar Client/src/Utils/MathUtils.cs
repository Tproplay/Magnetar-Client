using System;

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

    }
    
}
