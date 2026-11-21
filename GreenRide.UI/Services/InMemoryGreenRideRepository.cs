using GreenRide.Models;
using GreenRide.UI.Models;

namespace GreenRide.UI.Services;

/// <summary>
/// POLYMORPHISM: Implementasi IGreenRideRepository menggunakan data di memori.
/// Digunakan untuk pengembangan/testing tanpa koneksi database.
/// </summary>
public sealed class InMemoryGreenRideRepository : IGreenRideRepository
{
    // 4 moda sesuai schema baru (faktorEmisi: kg CO₂/menit)
    private readonly List<TransportMode> _modes = new()
    {
        new TransportMode(1, "Motor",           0.027),
        new TransportMode(2, "Mobil",           0.072),
        new TransportMode(3, "Bus Trans Jogja", 0.043),
        new TransportMode(4, "Kereta Api",      0.016),
    };

    // Akun demo tanpa password (AccountInfo tidak lagi menyimpan password)
    private readonly List<AccountInfo> _accounts = new()
    {
        new AccountInfo(
            Guid.Parse("00000000-0000-0000-0000-000000000001"),
            "Demo GreenRider",
            "demo@greenride.id"),
    };

    private readonly List<TripLog> _trips = new();

    // ── Autentikasi ────────────────────────────────────────────────────────────
    public AccountInfo? Authenticate(string email, string password)
    {
        // Akun demo: email cocok + password "demo123"
        if (email.Trim().Equals("demo@greenride.id", StringComparison.OrdinalIgnoreCase)
            && password == "demo123")
            return _accounts[0];
        return null;
    }

    public AccountInfo? Register(string nama, string email, string password)
    {
        if (_accounts.Any(a =>
            a.Email.Equals(email.Trim(), StringComparison.OrdinalIgnoreCase)))
            return null;

        var acc = new AccountInfo(Guid.NewGuid(), nama.Trim(), email.Trim().ToLower());
        _accounts.Add(acc);
        return acc;
    }

    // ── Moda Transportasi ──────────────────────────────────────────────────────
    public IReadOnlyList<TransportMode> GetTransportModes() => _modes;

    // ── Trip ───────────────────────────────────────────────────────────────────
    public TripLog StartTrip(Guid userId, int modaId)
    {
        var now   = DateTime.Now;
        var today = DateOnly.FromDateTime(now);
        var trip  = new TripLog(userId, modaId, today, now);  // constructor 4-param
        _trips.Add(trip);
        return trip;
    }

    public TripLog? GetActiveTrip(Guid userId) =>
        _trips.FirstOrDefault(t => t.UserId == userId && t.IsActive);

    public TripLog CompleteTrip(Guid tripId, decimal faktorEmisi)
    {
        var trip = _trips.First(t => t.Id == tripId);
        trip.Selesaikan(DateTime.Now, faktorEmisi);
        return trip;
    }

    public IReadOnlyList<TripLog> GetTripLogs(Guid userId, DateOnly from, DateOnly to) =>
        _trips
            .Where(t => t.UserId == userId && t.Tanggal >= from && t.Tanggal <= to)
            .OrderBy(t => t.Tanggal).ThenBy(t => t.WaktuMulai)
            .ToList();

    public void UpdateTripLog(Guid tripId, int modaIdBaru, decimal faktorEmisiBaru) =>
        _trips.FirstOrDefault(t => t.Id == tripId)?.Revisi(modaIdBaru, faktorEmisiBaru);

    public Task<TripLog?> UpdateTripLog(Guid tripId, DateTime waktuMulai, DateTime waktuSelesai)
    {
        var idx = _trips.FindIndex(t => t.Id == tripId);
        if (idx < 0) return Task.FromResult<TripLog?>(null);

        var old    = _trips[idx];
        var faktor = (decimal)(_modes.FirstOrDefault(m => m.TransportModeId == old.ModaId)?.FaktorEmisi ?? 0);
        var durasi = (decimal)(waktuSelesai - waktuMulai).TotalMinutes;
        var baru   = new TripLog(old.Id, old.UserId, old.ModaId, old.Tanggal,
                                 waktuMulai, waktuSelesai, durasi, Math.Round(durasi * faktor, 4), true);
        _trips[idx] = baru;
        return Task.FromResult<TripLog?>(baru);
    }

    // ── Badge & Leaderboard (dikerjakan Aqidatul — belum diimplementasi) ───────
    public WeeklyBadge? GetPreviousWeekBadge(Guid userId)
        => throw new NotImplementedException();

    public IReadOnlyList<WeeklyBadge> GetAllWeeklyBadges(Guid userId)
        => throw new NotImplementedException();

    public void AddWeeklyBadge(WeeklyBadge badge)
        => throw new NotImplementedException();

    public Dictionary<DateOnly, decimal> GetCalendarEmission(Guid userId, int year, int month)
        => throw new NotImplementedException();

    public IReadOnlyList<LeaderboardEntry> GetLeaderboard(DateOnly weekStart)
        => throw new NotImplementedException();
}
