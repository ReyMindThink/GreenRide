using GreenRide.UI.Models;

namespace GreenRide.UI.Helpers;

/// <summary>Warna kalender (PRD 5.3): batas = target badge mingguan / 7.</summary>
public static class CalendarRules
{
    public const decimal GoodMaxKg = 0.43m;
    public const decimal AverageMaxKg = 1.43m;

    public static CalendarLevel LevelFor(decimal? totalHarian)
    {
        if (totalHarian is null) return CalendarLevel.None;
        if (totalHarian <= GoodMaxKg) return CalendarLevel.Good;
        if (totalHarian <= AverageMaxKg) return CalendarLevel.Average;
        return CalendarLevel.High;
    }
}
