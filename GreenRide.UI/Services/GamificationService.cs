using GreenRide.Models;
using GreenRide.UI.Models;

namespace GreenRide.UI.Services;

/// <summary>
/// Hitungan emisi, poin, dan badge mingguan. Aturan badge (PRD 5.5):
/// periode Senin–Minggu, wajib tercatat 7 hari, hanya badge tertinggi yang diberikan.
/// </summary>
public sealed class GamificationService
{
    private const int HariMinimum = 7;   // syarat hari tercatat dalam satu minggu
    private const int MingguDicek = 8;   // berapa minggu ke belakang dicek saat refresh

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

    /// <summary>Selisih emisi dibanding naik mobil, dihitung dari durasi sebenarnya (menit desimal).</summary>
    public double EmisiDihindari(Guid userId)
    {
        var from = DateOnly.FromDateTime(DateTime.Today.AddYears(-2));
        var to = DateOnly.FromDateTime(DateTime.Today);
        return _repo.GetTripLogs(userId, from, to)
            .Where(l => !l.IsActive)
            .Sum(l => Math.Max(0d,
                ModeCatalog.FaktorAcuanMobil * (double)(l.DurasiMenit ?? 0m)
                - (double)(l.EmisiKg ?? 0m)));
    }

    // ── Poin & Badge ───────────────────────────────────────────────────────

    /// <summary>1 poin = 0,1 kg CO₂ yang dihindari dibanding naik mobil.</summary>
    public int TotalPoin(Guid userId) =>
        (int)Math.Round(EmisiDihindari(userId) * 10);

    /// <summary>Badge tertinggi (batas paling ketat) yang dipenuhi total emisi mingguan tertentu.</summary>
    public BadgeDefinition? TierUntuk(double kg) =>
        BadgeRules.All.OrderBy(b => b.BatasKg).FirstOrDefault(b => kg <= b.BatasKg);

    /// <summary>BadgeId yang pernah diraih (dicocokkan lewat BadgeName di weekly_badges).</summary>
    public HashSet<int> BadgeDiraih(Guid userId)
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

    /// <summary>
    /// Menilai minggu-minggu yang sudah lewat (maks. 8 minggu ke belakang) yang belum tersimpan.
    /// Minggu dengan catatan selalu disimpan; badge kosong bila hari tercatat kurang dari 7.
    /// </summary>
    public void RefreshBadges(Guid userId)
    {
        var mingguIni = DateOnly.FromDateTime(StartOfWeek(DateTime.Today));
        var sudahAda = _repo.GetAllWeeklyBadges(userId).Select(b => b.MingguMulai).ToHashSet();

        for (int w = 1; w <= MingguDicek; w++)
        {
            var mulai = mingguIni.AddDays(-7 * w);
            var selesai = mulai.AddDays(6);
            if (sudahAda.Contains(mulai)) continue;

            var logs = _repo.GetTripLogs(userId, mulai, selesai)
                            .Where(l => !l.IsActive)
                            .ToList();
            if (logs.Count == 0) continue;   // minggu tanpa catatan tidak disimpan

            var total = logs.Sum(l => l.EmisiKg ?? 0m);
            var hari = logs.Select(l => l.Tanggal).Distinct().Count();

            // Kurang dari 7 hari tercatat = tidak dapat badge
            var badge = hari < HariMinimum ? null : TierUntuk((double)total);

            _repo.AddWeeklyBadge(new WeeklyBadge(userId, mulai, selesai, total, badge?.Nama, hari));
        }
    }
}