using GreenRide.UI.Models;

namespace GreenRide.UI.Helpers;

/// <summary>Aturan badge mingguan (PRD 5.5).</summary>
public static class BadgeRules
{
    public const int RequiredDays = 7;

    /// <summary>Badge resmi minggu yang sudah selesai: wajib ada log di 7 hari.</summary>
    public static BadgeLevel EvaluateWeek(decimal totalEmisiKg, int hariTercatat) =>
        hariTercatat < RequiredDays ? BadgeLevel.None : FromTotal(totalEmisiKg);

    /// <summary>
    /// Tingkat tertinggi yang terpenuhi oleh total emisi. Dipakai juga untuk "badge saat ini"
    /// di Leaderboard (berdasarkan pace minggu berjalan, tanpa syarat 7 hari).
    /// </summary>
    public static BadgeLevel FromTotal(decimal totalEmisiKg)
    {
        if (totalEmisiKg <= 0m) return BadgeLevel.ZeroTrace;
        if (totalEmisiKg <= 3m) return BadgeLevel.TerraSentinel;
        if (totalEmisiKg <= 6m) return BadgeLevel.CarbonTamer;
        if (totalEmisiKg <= 10m) return BadgeLevel.EcoInitiate;
        return BadgeLevel.None;
    }
}
