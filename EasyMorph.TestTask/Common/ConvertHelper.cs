using System.Globalization;

namespace EasyMorph.TestTask.Common
{
    public static class ConvertHelper
    {
        /// <summary>
        /// Converts a monetary value to an integer number of cents.
        /// Using integer long values avoids floating-point precision issues when processing or accumulating monetary amounts.
        /// <para> For example: "24.45" -> 2445. </para>
        /// <para> Returns <c>false</c> for negative values, invalid input, or values with more than two decimal places. </para>
        /// <para> The <see cref="long"/> type provides sufficient range for virtually any real-world monetary amount. </para>
        /// </summary>
        public static bool TryParseCents(string value, out int cents)
        {
            if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var d) && d >= 0)
            {
                var scaled = d * 100;
                var rounded = Math.Round(scaled);
                if (Math.Abs(scaled - rounded) < 1e-9)
                {
                    cents = (int)rounded;
                    return true;
                }
            }
            cents = 0;
            return false;
        }

        public static string ToMoneyString(this int value)
        {
            return (value / 100d).ToString("F2", CultureInfo.InvariantCulture);
        }

        public static string ToMoneyString(this long value)
        {
            return (value / 100d).ToString("F2", CultureInfo.InvariantCulture);
        }

        public static bool TryParseDate(this string value, out DateOnly date)
        {
            return DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
        }
    }
}