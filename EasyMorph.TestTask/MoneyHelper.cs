using System.Globalization;

namespace EasyMorph.TestTask
{
    public static class MoneyHelper
    {
        public static bool TryToCent(string value, out int cents)
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

        public static string CentToDollar(this int value)
        {
            return (value / 100d).ToString("F2", CultureInfo.InvariantCulture);
        }

        public static string CentToDollar(this long value)
        {
            return (value / 100d).ToString("F2", CultureInfo.InvariantCulture);
        }
    }
}