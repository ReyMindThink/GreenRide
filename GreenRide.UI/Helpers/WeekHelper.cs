namespace GreenRide.UI.Helpers;

public static class WeekHelper
{
    /// <summary>Tanggal Senin pada minggu yang memuat <paramref name="date"/> (minggu = Senin..Minggu).</summary>
    public static DateOnly MondayOf(DateOnly date)
    {
        var offset = ((int)date.DayOfWeek + 6) % 7; // Senin=0 ... Minggu=6
        return date.AddDays(-offset);
    }

    public static DateOnly SundayOf(DateOnly date) => MondayOf(date).AddDays(6);
}
