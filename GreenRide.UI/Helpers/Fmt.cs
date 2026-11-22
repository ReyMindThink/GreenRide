using System.Globalization;

namespace GreenRide.UI.Helpers;

/// <summary>Format teks Indonesia (koma desimal, nama bulan/hari) tanpa bergantung pada ICU/culture OS.</summary>
public static class Fmt
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    private static readonly string[] Bulan =
    {
        "Januari", "Februari", "Maret", "April", "Mei", "Juni",
        "Juli", "Agustus", "September", "Oktober", "November", "Desember",
    };

    private static readonly string[] Hari =
        { "Minggu", "Senin", "Selasa", "Rabu", "Kamis", "Jumat", "Sabtu" };

    /// <summary>1,0200 (koma desimal).</summary>
    public static string Number(decimal value, int decimals) =>
        value.ToString("F" + decimals, Inv).Replace('.', ',');

    /// <summary>"0,2700 kg"</summary>
    public static string Kg(decimal value, int decimals = 4) => Number(value, decimals) + " kg";

    /// <summary>"15,45 kg CO₂"</summary>
    public static string Co2(decimal value, int decimals = 2) => Number(value, decimals) + " kg CO₂";

    /// <summary>"08.05"</summary>
    public static string Clock(DateTime t) => t.ToString("HH'.'mm", Inv);

    /// <summary>"08.00–08.10"</summary>
    public static string TimeRange(DateTime mulai, DateTime selesai) => Clock(mulai) + "–" + Clock(selesai);

    /// <summary>Tampilan timer: 00.12.37</summary>
    public static string Stopwatch(TimeSpan t)
    {
        if (t < TimeSpan.Zero) t = TimeSpan.Zero;
        return $"{(int)t.TotalHours:00}.{t.Minutes:00}.{t.Seconds:00}";
    }

    /// <summary>"10 menit"</summary>
    public static string Minutes(decimal menit) =>
        Math.Round(menit, 0, MidpointRounding.AwayFromZero).ToString("0", Inv) + " menit";

    /// <summary>"22 November 2026"</summary>
    public static string DateLong(DateOnly d) => $"{d.Day} {Bulan[d.Month - 1]} {d.Year}";

    /// <summary>"22 November"</summary>
    public static string DayMonth(DateOnly d) => $"{d.Day} {Bulan[d.Month - 1]}";

    /// <summary>"November"</summary>
    public static string MonthName(int month) => Bulan[month - 1];

    /// <summary>"Minggu, 22 November 2026"</summary>
    public static string DateWithDay(DateOnly d) => $"{Hari[(int)d.DayOfWeek]}, {DateLong(d)}";

    /// <summary>"November 2026"</summary>
    public static string MonthYear(int year, int month) => $"{Bulan[month - 1]} {year}";

    /// <summary>"02 Nov"</summary>
    public static string DayMonthShort(DateOnly d) => $"{d.Day:00} {Bulan[d.Month - 1][..3]}";

    /// <summary>"16–22 November 2026" (rentang minggu; menangani beda bulan/tahun).</summary>
    public static string WeekRange(DateOnly senin, DateOnly minggu)
    {
        if (senin.Year != minggu.Year) return $"{DateLong(senin)} – {DateLong(minggu)}";
        if (senin.Month != minggu.Month) return $"{senin.Day} {Bulan[senin.Month - 1]} – {DateLong(minggu)}";
        return $"{senin.Day}–{DateLong(minggu)}";
    }
}
