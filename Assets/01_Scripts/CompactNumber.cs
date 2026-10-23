using System;
using System.Globalization;

public static class CompactNumber
{
    private static readonly string[] Suffixes = { "", "K", "M", "B", "T", "Qa", "Qi" };

    public static string Format(double value, int decimalPlaces = 2)
    {
        if (double.IsNaN(value) || double.IsInfinity(value)) return "—";
        decimalPlaces = Math.Max(0, Math.Min(4, decimalPlaces));
        var suffix = 0;
        while (Math.Abs(value) >= 1000d && suffix < Suffixes.Length - 1)
        {
            value /= 1000d;
            suffix++;
        }
        value = Math.Round(value, decimalPlaces, MidpointRounding.AwayFromZero);
        if (Math.Abs(value) >= 1000d && suffix < Suffixes.Length - 1)
        {
            value /= 1000d;
            suffix++;
        }
        var format = decimalPlaces == 0 ? "0" : "0." + new string('#', decimalPlaces);
        return value.ToString(format, CultureInfo.InvariantCulture) + Suffixes[suffix];
    }
}
