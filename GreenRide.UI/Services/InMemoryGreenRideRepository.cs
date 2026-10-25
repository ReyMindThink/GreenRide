using GreenRide.Models;
using GreenRide.UI.Models;

namespace GreenRide.UI.Services;

public sealed class InMemoryGreenRideRepository : IGreenRideRepository
{
    private readonly List<TransportMode> _modes = new()
    {
        new TransportMode(1, "Jalan Kaki", 0.00),
        new TransportMode(2, "Sepeda",     0.00),
        new TransportMode(3, "Bus",        0.03),
        new TransportMode(4, "Motor",      0.06),
        new TransportMode(5, "Mobil",      0.10),
    };

    private readonly List<AccountInfo> _accounts = new()
    {
        new AccountInfo(1, "Demo GreenRider", "demo@greenride.id", "demo123"),
    };

    private readonly List<DailyLog> _logs = new();
    private readonly List<UserBadge> _badges = new();
    private int _nextLogId = 1, _nextBadgeId = 1, _nextUserId = 2;

    public InMemoryGreenRideRepository() => Seed();

    private void Seed()
    {
        // 14 hari terakhir untuk akun demo → minggu lalu ≈ 9,2 kg (Hijau Pemula)
        int[] modeIds = { 2, 3, 4, 1, 5, 3, 4, 5, 4, 2, 3, 5, 1, 4 };
        int[] durasi  = { 25, 30, 20, 15, 25, 40, 30, 30, 25, 35, 20, 28, 18, 22 };
        for (int i = 0; i < modeIds.Length; i++)
            AddLog(new DailyLog(1, DateTime.Today.AddDays(-i), durasi[i], modeIds[i]));
    }

    public AccountInfo? Authenticate(string email, string password) =>
        _accounts.FirstOrDefault(a =>
            a.Email.Equals(email.Trim(), StringComparison.OrdinalIgnoreCase) && a.Password == password);

    public AccountInfo? Register(string nama, string email, string password)
    {
        if (_accounts.Any(a => a.Email.Equals(email.Trim(), StringComparison.OrdinalIgnoreCase)))
            return null;
        var acc = new AccountInfo(_nextUserId++, nama.Trim(), email.Trim(), password);
        _accounts.Add(acc);
        return acc;
    }

    public IReadOnlyList<TransportMode> GetTransportModes() => _modes;

    public IReadOnlyList<DailyLog> GetLogs(int userId, DateTime from, DateTime to) =>
        _logs.Where(l => l.UserId == userId && l.Tanggal.Date >= from.Date && l.Tanggal.Date <= to.Date)
             .OrderBy(l => l.Tanggal).ThenBy(l => l.DailyLogId)
             .ToList();

    public DailyLog AddLog(DailyLog log)
    {
        log.SetDailyLogId(_nextLogId++);
        _logs.Add(log);
        return log;
    }

    // Objek DailyLog yang sama sudah dimutasi (RevisiModa/RevisiDurasi); di DB nyata di sinilah UPDATE dijalankan.
    public void UpdateLog(DailyLog log) { }

    public IReadOnlyList<UserBadge> GetUserBadges(int userId) =>
        _badges.Where(b => b.UserId == userId).ToList();

    public void AddUserBadge(UserBadge badge)
    {
        badge.SetUserBadgeId(_nextBadgeId++);
        _badges.Add(badge);
    }
}
