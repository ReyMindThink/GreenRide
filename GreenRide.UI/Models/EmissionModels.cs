namespace GreenRide.UI.Models;

public enum CalendarLevel
{
    None,     // tanpa catatan  -> abu
    Good,     // <= 0,43 kg     -> hijau
    Average,  // <= 1,43 kg     -> kuning
    High,     // >  1,43 kg     -> merah
}

/// <summary>Total emisi satu tanggal. TotalEmisiKg null = tidak ada log.</summary>
public sealed record CalendarDayEmission(DateOnly Tanggal, decimal? TotalEmisiKg);

/// <summary>Satu batang pada grafik mingguan.</summary>
public sealed record WeeklyEmissionPoint(DateOnly MingguMulai, decimal TotalEmisiKg, bool Started);

public sealed record LeaderboardEntry(
    int Rank,
    string Nama,
    decimal TotalEmisiKg,
    BadgeLevel Badge,
    bool IsCurrentUser);
