using GreenRide.Models;
using GreenRide.UI.Models;

namespace GreenRide.UI.Services;

/// <summary>
/// Hitungan emisi, poin, dan badge — semua menggunakan API baru
/// (Guid userId, TripLog, DateOnly).
/// Metode badge Aqidatul masih NotImplementedException di repository;
/// dibungkus try-catch agar tidak crash.
/// </summary>
public sealed class GamificationService
{
    private readonly IGreenRideRepository _repo;
    public GamificationService(IGreenRideRepository repo) => _repo = repo;

    public Dictionary<int, TransportMode> ModeMap() =>
        _repo.GetTransportModes().ToDictionary(m => m.TransportModeId);

    public static DateTime StartOfWeek(DateTime d)
    {
        int diff = ((int)d.DayOfWeek + 6) % 7; // Senin = 0
        return d.Date.AddDays(-diff);
    }

    // ── Emisi ──────────────────────────────────────────────────────────────

    public static double Emisi(IEnumerable<TripLog> logs) =>
        logs.Sum(l => (double)(l.EmisiKg ?? 0m));

    public double EmisiMinggu(Guid userId, DateOnly weekStart) =>
        Emisi(_repo.GetTripLogs(userId, weekStart, weekStart.AddDays(6)));

    public double EmisiDihindari(Guid userId)
    {
        var from = DateOnly.FromDateTime(DateTime.Today.AddYears(-2));
        var to   = DateOnly.FromDateTime(DateTime.Today);
        return _repo.GetTripLogs(userId, from, to)
            .Sum(l =>
            {
                if (!l.WaktuSelesai.HasValue || l.EmisiKg is null) return 0d;
                int dur = (int)(l.WaktuSelesai.Value - l.WaktuMulai).TotalMinutes;
                return Math.Max(0d,
                    ModeCatalog.FaktorAcuanMobil * dur - (double)l.EmisiKg.Value);
            });
    }

    // ── Poin & Badge ───────────────────────────────────────────────────────

    public int TotalPoin(Guid userId) =>
        (int)Math.Round(EmisiDihindari(userId) * 10);

    public BadgeDefinition? TierUntuk(double kg) =>
        BadgeRules.All.OrderBy(b => b.BatasKg).FirstOrDefault(b => kg <= b.BatasKg);

    /// <summary>
    /// Set BadgeId yang sudah diraih (dicocokkan lewat BadgeName di WeeklyBadge).
    /// Jika metode repository belum diimplementasi (Aqidatul), kembalikan set kosong.
    /// </summary>
    public HashSet<int> BadgeDiraih(Guid userId)
    {
        try
        {
            var names = _repo.GetAllWeeklyBadges(userId)
                             .Where(b => b.HasBadge)
                             .Select(b => b.BadgeName!)
                             .ToHashSet();
            return BadgeRules.All
                .Where(d => names.Contains(d.Nama))
                .Select(d => d.BadgeId)
                .ToHashSet();
        }
        catch (NotImplementedException)
        {
            return new HashSet<int>();
        }
    }

    /// <summary>
    /// Evaluasi dan simpan badge untuk 8 minggu terakhir.
    /// Jika metode repository belum diimplementasi, operasi dilewati.
    /// </summary>
    public void RefreshBadges(Guid userId)
    {
        try
        {
            var thisWeek = DateOnly.FromDateTime(StartOfWeek(DateTime.Today));
            var existing = _repo.GetAllWeeklyBadges(userId);

            for (int w = 1; w <= 8; w++)
            {
                var ws = thisWeek.AddDays(-7 * w);
                var we = ws.AddDays(6);
                var logs = _repo.GetTripLogs(userId, ws, we);
                if (logs.Count == 0) continue;

                var total = Emisi(logs);
                var tier  = TierUntuk(total);
                if (tier is null) continue;

                bool sudahAda = existing.Any(b => b.MingguMulai == ws);
                if (sudahAda) continue;

                int hari = logs.Select(l => l.Tanggal).Distinct().Count();
                _repo.AddWeeklyBadge(
                    new WeeklyBadge(userId, ws, we, (decimal)total, tier.Nama, hari));
            }
        }
        catch (NotImplementedException) { }
    }
}
