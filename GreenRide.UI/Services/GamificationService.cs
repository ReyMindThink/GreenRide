using GreenRide.Models;
using GreenRide.UI.Models;

namespace GreenRide.UI.Services;

/// <summary>Hitungan emisi mingguan, poin, dan pemberian badge berdasarkan aturan di BadgeRules.</summary>
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

    public double Emisi(IEnumerable<DailyLog> logs)
    {
        var map = ModeMap();
        return logs.Sum(l => map.TryGetValue(l.TransportModeId, out var m) ? l.HitungEmisiHarian(m) : 0);
    }

    public double EmisiMinggu(int userId, DateTime weekStart) =>
        Emisi(_repo.GetLogs(userId, weekStart, weekStart.AddDays(6)));

    public double EmisiDihindari(int userId)
    {
        var map = ModeMap();
        return _repo.GetLogs(userId, DateTime.Today.AddYears(-2), DateTime.Today)
            .Sum(l => map.TryGetValue(l.TransportModeId, out var m)
                ? Math.Max(0, ModeCatalog.FaktorAcuanMobil - m.FaktorEmisi) * l.Durasi
                : 0);
    }

    /// <summary>1 poin = 0,1 kg CO₂ yang dihindari dibanding naik mobil.</summary>
    public int TotalPoin(int userId) => (int)Math.Round(EmisiDihindari(userId) * 10);

    /// <summary>Badge tertinggi yang dipenuhi untuk total emisi mingguan tertentu.</summary>
    public BadgeDefinition? TierUntuk(double kg) =>
        BadgeRules.All.OrderBy(b => b.BatasKg).FirstOrDefault(b => kg <= b.BatasKg);

    public HashSet<int> BadgeDiraih(int userId) =>
        _repo.GetUserBadges(userId).Select(b => b.BadgeId).ToHashSet();

    /// <summary>Beri badge untuk minggu-minggu yang sudah selesai (8 minggu terakhir) bila belum tercatat.</summary>
    public void RefreshBadges(int userId)
    {
        var thisWeek = StartOfWeek(DateTime.Today);
        var existing = _repo.GetUserBadges(userId);
        for (int w = 1; w <= 8; w++)
        {
            var ws = thisWeek.AddDays(-7 * w);
            var we = ws.AddDays(6);
            var logs = _repo.GetLogs(userId, ws, we);
            if (logs.Count == 0) continue;

            var tier = TierUntuk(Emisi(logs));
            if (tier is null) continue;

            bool sudahAda = existing.Any(ub => ub.BadgeId == tier.BadgeId && ub.TanggalDiperoleh.Date == we);
            if (!sudahAda) _repo.AddUserBadge(new UserBadge(userId, tier.BadgeId, we));
        }
    }
}
