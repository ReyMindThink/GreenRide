using System.Text.RegularExpressions;

namespace GreenRide.UI.Helpers;

/// <summary>Membaca jam yang diketik pengguna: 08.15, 8.15, 08:15 atau 0815.</summary>
public static class TimeInput
{
    private static readonly Regex Pattern =
        new(@"^(?:(\d{1,2})[.:](\d{2})|(\d{2})(\d{2}))$", RegexOptions.Compiled);

    public static bool TryParse(string? text, out TimeOnly time)
    {
        time = default;
        if (string.IsNullOrWhiteSpace(text)) return false;

        var m = Pattern.Match(text.Trim());
        if (!m.Success) return false;

        var hourText = m.Groups[1].Success ? m.Groups[1].Value : m.Groups[3].Value;
        var minuteText = m.Groups[1].Success ? m.Groups[2].Value : m.Groups[4].Value;
        var hour = int.Parse(hourText);
        var minute = int.Parse(minuteText);
        if (hour > 23 || minute > 59) return false;

        time = new TimeOnly(hour, minute);
        return true;
    }
}
