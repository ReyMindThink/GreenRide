using GreenRide.UI.Helpers;
using GreenRide.UI.Models;

namespace GreenRide.UI.Services.Mock;

/// <summary>
/// Backend palsu di memori supaya seluruh UI bisa dijalankan tanpa Supabase.
/// Tiga akun demo (lihat <see cref="DemoAccounts"/>, password 123456) dengan riwayat yang diseed relatif
/// terhadap tanggal hari ini, jadi aplikasi terlihat sudah dipakai berminggu-minggu.
/// </summary>
public sealed class MockGreenRideBackend :
    IAuthService, ITripService, IEmissionService, IBadgeProgressService, ILeaderboardService
{
    public const string DemoEmail = "john@example.com";
    public const string DemoPassword = "123456";

    private sealed record StoredAccount(UserAccount User, string Password);

    private static readonly (string Nama, decimal Total)[] OtherUsers =
    {
        ("Nadia Putri", 0.85m), ("Alya Pramesti", 0.00m), ("Rizky Pratama", 1.20m),
        ("Dimas Saputra", 3.40m), ("Sinta Maharani", 4.15m), ("Bima Aditya", 7.25m),
        ("Citra Lestari", 8.10m), ("Fajar Ramadhan", 8.65m), ("Putri Amelia", 9.20m),
    };

    private readonly IClock _clock;
    private readonly List<StoredAccount> _accounts = new();
    private readonly List<TripLog> _logs = new();
    private readonly List<(Guid UserId, WeeklyBadgeRecord Record)> _weekly = new();

    public MockGreenRideBackend(IClock clock)
    {
        _clock = clock;
        SeedDemoData();
    }

    private static Task Latency() => Task.Delay(120);

    // ------------------------------------------------------------ Seed

    /// <summary>Akun demo. Semuanya memakai password <see cref="DemoPassword"/>.</summary>
    public static readonly (string Email, string Nama, string Info)[] DemoAccounts =
    {
        (DemoEmail, "John Doe", "pengguna aktif, riwayat 13 minggu"),
        ("sari@example.com", "Sari Wulandari", "semua badge, minggu lalu Zero Trace"),
        ("budi@example.com", "Budi Santoso", "pengguna baru, timer sedang berjalan"),
    };

    // Porsi emisi tiap hari dalam seminggu (jumlahnya 1,00).
    private static readonly decimal[] DayWeights = { 0.18m, 0.14m, 0.15m, 0.13m, 0.16m, 0.14m, 0.10m };

    // Urutan hari yang dikosongkan bila sebuah minggu tidak tercatat penuh.
    private static readonly int[] SkipOrder = { 2, 5, 0, 4, 1, 3 };

    private static readonly int[] MixedModes = { ModeCatalog.Motor, ModeCatalog.Bus, ModeCatalog.Mobil, ModeCatalog.Kereta };
    private static readonly int[] GreenModes = { ModeCatalog.Kereta, ModeCatalog.Bus, ModeCatalog.Kereta, ModeCatalog.Motor };
    private static readonly int[] CarModes = { ModeCatalog.Mobil, ModeCatalog.Motor };

    private UserAccount AddAccount(string nama, string email)
    {
        var user = new UserAccount(Guid.NewGuid(), nama, email);
        _accounts.Add(new StoredAccount(user, DemoPassword));
        return user;
    }

    private void SeedDemoData()
    {
        var today = _clock.Today;
        var monday = WeekHelper.MondayOf(today);
        var todayIndex = today.DayNumber - monday.DayNumber; // 0..6, hari lalu di minggu ini = todayIndex hari

        SeedJohn(monday, todayIndex);
        SeedSari(monday, todayIndex);
        SeedBudi(monday, todayIndex);
    }

    /// <summary>John: 13 minggu riwayat dengan campuran badge (Eco Initiate, Carbon Tamer, Terra Sentinel), dua minggu terakhir sama dengan desain Figma.</summary>
    private void SeedJohn(DateOnly monday, int todayIndex)
    {
        var john = AddAccount("John Doe", DemoEmail);

        // (minggu yang lalu, total kg, hari tercatat)
        var history = new (int Ago, decimal Total, int Days)[]
        {
            (13, 9.40m, 7), (12, 12.60m, 7), (11, 8.60m, 5), (10, 11.20m, 7), (9, 8.10m, 7),
            (8, 9.00m, 7), (7, 7.40m, 7), (6, 4.90m, 7), (5, 2.20m, 7), (4, 7.90m, 6), (3, 6.90m, 7),
        };
        foreach (var h in history)
            SeedWeek(john.Id, monday.AddDays(-7 * h.Ago), h.Total, h.Days, MixedModes, h.Ago);

        // Total harian dirancang agar total mingguan = 6,45 / 5,16 / 3,84 seperti desain.
        SeedFigmaWeek(john.Id, monday.AddDays(-14), new[] { 1.70m, 0.90m, 0.85m, 0.80m, 0.30m, 1.50m, 0.40m }, 7);
        SeedFigmaWeek(john.Id, monday.AddDays(-7), new[] { 1.50m, 0.70m, 0.65m, 0.40m, 0.80m, 0.30m, 0.81m }, 7);
        SeedFigmaWeek(john.Id, monday, new[] { 1.00m, 0.55m, 0.35m, 0.30m, 0.50m, 0.12m, 0m }, todayIndex);

        SeedToday(john.Id, (ModeCatalog.Motor, 75, 10), (ModeCatalog.Kereta, 50, 20), (ModeCatalog.Bus, 10, 10));
    }

    /// <summary>Sari: pengguna ramah lingkungan. Punya keempat badge; minggu lalu Zero Trace.</summary>
    private void SeedSari(DateOnly monday, int todayIndex)
    {
        var sari = AddAccount("Sari Wulandari", "sari@example.com");

        var history = new (int Ago, decimal Total, int Days)[]
        {
            (12, 9.10m, 7), (11, 8.20m, 7), (10, 7.60m, 7), (9, 5.30m, 7), (8, 4.70m, 7), (7, 4.90m, 7),
            (6, 2.30m, 7), (5, 2.20m, 7), (4, 2.00m, 7), (3, 1.20m, 7), (2, 1.60m, 7),
            (1, 0m, 7), // Zero Trace: tujuh hari tercatat, tanpa emisi
        };
        foreach (var h in history)
            SeedWeek(sari.Id, monday.AddDays(-7 * h.Ago), h.Total, h.Days, GreenModes, h.Ago);

        SeedWeek(sari.Id, monday, 1.40m, 7, GreenModes, 0, maxDay: todayIndex);
        SeedToday(sari.Id, (ModeCatalog.Kereta, 70, 20), (ModeCatalog.Kereta, 30, 20));
    }

    /// <summary>Budi: baru memakai app. Minggu lalu hanya 4 hari tercatat (tanpa badge) dan ada timer Mobil yang sedang berjalan.</summary>
    private void SeedBudi(DateOnly monday, int todayIndex)
    {
        var budi = AddAccount("Budi Santoso", "budi@example.com");

        SeedWeek(budi.Id, monday.AddDays(-7), 9.80m, 4, CarModes, 1);
        SeedWeek(budi.Id, monday, 6.00m, 7, CarModes, 0, maxDay: todayIndex);
        SeedToday(budi.Id, (ModeCatalog.Bus, 60, 25));

        // Timer yang sedang berjalan (waktu_selesai NULL), dimulai 12 menit lalu.
        var start = _clock.Now.AddMinutes(-12);
        _logs.Add(new TripLog
        {
            UserId = budi.Id,
            ModaId = ModeCatalog.Mobil,
            Tanggal = DateOnly.FromDateTime(start),
            WaktuMulai = start,
        });
    }

    /// <summary>
    /// Mengisi satu minggu: dua perjalanan per hari (pagi dan sore) dengan moda bergantian, total mingguan
    /// mendekati <paramref name="weekTotal"/>. Bila total = 0 dibuat perjalanan berdurasi nol (untuk Zero Trace).
    /// </summary>
    private void SeedWeek(Guid userId, DateOnly monday, decimal weekTotal, int daysLogged, int[] modes, int salt, int maxDay = 7)
    {
        var skipped = new HashSet<int>(SkipOrder.Take(Math.Max(0, 7 - daysLogged)));
        for (var d = 0; d < maxDay; d++)
        {
            if (skipped.Contains(d)) continue;

            var date = monday.AddDays(d);
            if (weekTotal <= 0m)
            {
                AddFinished(userId, ModeCatalog.Kereta, date.ToDateTime(new TimeOnly(8, 0)), 0m, 0m);
                continue;
            }

            var dayTotal = weekTotal * DayWeights[d];
            var shares = new[] { 0.6m, 0.4m };
            var starts = new[] { new TimeOnly(7, 30), new TimeOnly(17, 0) };
            for (var i = 0; i < 2; i++)
            {
                var modaId = modes[(salt + d + i) % modes.Length];
                var faktor = ModeCatalog.Get(modaId).FaktorEmisi;
                var durasi = Math.Max(1m, Math.Round(dayTotal * shares[i] / faktor, 0, MidpointRounding.AwayFromZero));
                AddFinished(userId, modaId, date.ToDateTime(starts[i]).AddMinutes(d * 3), durasi);
            }
        }
    }

    /// <summary>Satu perjalanan motor per hari dengan total harian persis seperti desain.</summary>
    private void SeedFigmaWeek(Guid userId, DateOnly monday, decimal[] totals, int count)
    {
        var faktor = ModeCatalog.Get(ModeCatalog.Motor).FaktorEmisi;
        for (var i = 0; i < count; i++)
        {
            var mulai = monday.AddDays(i).ToDateTime(new TimeOnly(8, 0));
            var durasi = Math.Round(totals[i] / faktor, 2);
            AddFinished(userId, ModeCatalog.Motor, mulai, durasi, totals[i]);
        }
    }

    /// <summary>
    /// Perjalanan yang sudah selesai hari ini. Waktunya dihitung mundur dari sekarang supaya selalu ada
    /// isinya, berapa pun jam aplikasi dibuka (menit sebelum titik acuan = 20 menit lalu).
    /// </summary>
    private void SeedToday(Guid userId, params (int Moda, int MenitSebelum, int Durasi)[] trips)
    {
        var now = _clock.Now;
        var midnight = _clock.Today.ToDateTime(TimeOnly.MinValue);
        var anchor = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0).AddMinutes(-20);

        foreach (var t in trips)
        {
            var mulai = anchor.AddMinutes(-t.MenitSebelum);
            if (mulai < midnight || mulai.AddMinutes(t.Durasi) > now) continue;
            AddFinished(userId, t.Moda, mulai, t.Durasi);
        }
    }

    private void AddFinished(Guid userId, int modaId, DateTime mulai, decimal durasi, decimal? emisi = null)
    {
        var mode = ModeCatalog.Get(modaId);
        _logs.Add(new TripLog
        {
            UserId = userId,
            ModaId = modaId,
            Tanggal = DateOnly.FromDateTime(mulai),
            WaktuMulai = mulai,
            WaktuSelesai = mulai.AddMinutes((double)durasi),
            DurasiMenit = durasi,
            EmisiKg = emisi ?? EmissionCalculator.EmissionKg(durasi, mode.FaktorEmisi),
        });
    }

    // ------------------------------------------------------------ Auth

    public async Task<AuthResult> LoginAsync(string email, string password)
    {
        await Latency();
        var acc = _accounts.FirstOrDefault(a =>
            a.User.Email.Equals(email.Trim(), StringComparison.OrdinalIgnoreCase));
        if (acc is null || acc.Password != password)
            return AuthResult.Fail("Email atau password salah.");
        return AuthResult.Ok(acc.User);
    }

    public async Task<AuthResult> RegisterAsync(string nama, string email, string password)
    {
        await Latency();
        if (_accounts.Any(a => a.User.Email.Equals(email.Trim(), StringComparison.OrdinalIgnoreCase)))
            return AuthResult.Fail("Email sudah terdaftar.");

        var user = new UserAccount(Guid.NewGuid(), nama.Trim(), email.Trim());
        _accounts.Add(new StoredAccount(user, password));
        return AuthResult.Ok(user);
    }

    // ------------------------------------------------------------ Trips

    public async Task<TripLog?> GetActiveTripAsync(Guid userId)
    {
        await Latency();
        return _logs.FirstOrDefault(l => l.UserId == userId && l.IsRunning);
    }

    public async Task<TripLog> StartTripAsync(Guid userId, int modaId)
    {
        await Latency();
        if (_logs.Any(l => l.UserId == userId && l.IsRunning))
            throw new ServiceException("Masih ada perjalanan yang sedang berjalan.");

        var now = _clock.Now;
        var log = new TripLog
        {
            UserId = userId,
            ModaId = modaId,
            Tanggal = DateOnly.FromDateTime(now),
            WaktuMulai = now,
        };
        _logs.Add(log);
        return log;
    }

    public async Task<TripLog> StopTripAsync(Guid userId)
    {
        await Latency();
        var log = _logs.FirstOrDefault(l => l.UserId == userId && l.IsRunning)
                  ?? throw new ServiceException("Tidak ada perjalanan yang sedang berjalan.");

        var selesai = _clock.Now;
        var (menit, kg) = EmissionCalculator.Calculate(log.WaktuMulai, selesai, ModeCatalog.Get(log.ModaId).FaktorEmisi);
        log.WaktuSelesai = selesai;
        log.DurasiMenit = menit;
        log.EmisiKg = kg;
        return log;
    }

    public async Task<IReadOnlyList<TripLog>> GetLogsAsync(Guid userId, DateOnly tanggal)
    {
        await Latency();
        return _logs
            .Where(l => l.UserId == userId && l.Tanggal == tanggal)
            .OrderBy(l => l.WaktuMulai)
            .ToList();
    }

    public async Task<TripLog> ReviseTripAsync(Guid userId, Guid logId, int modaId, DateTime waktuMulai, DateTime waktuSelesai)
    {
        await Latency();
        var today = _clock.Today;
        var now = _clock.Now;

        var log = _logs.FirstOrDefault(l => l.Id == logId && l.UserId == userId)
                  ?? throw new ServiceException("Perjalanan tidak ditemukan.");

        if (log.Tanggal != today)
            throw new ServiceException("Revisi hanya tersedia untuk perjalanan hari ini.");
        if (DateOnly.FromDateTime(waktuMulai) != today || DateOnly.FromDateTime(waktuSelesai) != today)
            throw new ServiceException("Waktu revisi harus berada di hari ini.");
        if (waktuSelesai <= waktuMulai)
            throw new ServiceException("Waktu selesai harus setelah waktu mulai.");
        if (waktuSelesai > now)
            throw new ServiceException("Waktu selesai tidak boleh di masa depan.");

        var overlaps = _logs.Any(o =>
            o.UserId == userId && o.Id != logId && o.Tanggal == today &&
            waktuMulai < (o.WaktuSelesai ?? now) && waktuSelesai > o.WaktuMulai);
        if (overlaps)
            throw new ServiceException("Waktu tumpang tindih dengan perjalanan lain.");

        var (menit, kg) = EmissionCalculator.Calculate(waktuMulai, waktuSelesai, ModeCatalog.Get(modaId).FaktorEmisi);
        log.ModaId = modaId;
        log.WaktuMulai = waktuMulai;
        log.WaktuSelesai = waktuSelesai;
        log.DurasiMenit = menit;
        log.EmisiKg = kg;
        log.IsRevised = true;
        return log;
    }

    // ------------------------------------------------------------ Emission

    public async Task<IReadOnlyList<CalendarDayEmission>> GetMonthAsync(Guid userId, int year, int month)
    {
        await Latency();
        var totals = _logs
            .Where(l => l.UserId == userId && l.EmisiKg.HasValue &&
                        l.Tanggal.Year == year && l.Tanggal.Month == month)
            .GroupBy(l => l.Tanggal)
            .ToDictionary(g => g.Key, g => g.Sum(l => l.EmisiKg!.Value));

        return Enumerable.Range(1, DateTime.DaysInMonth(year, month))
            .Select(d =>
            {
                var date = new DateOnly(year, month, d);
                return new CalendarDayEmission(date, totals.TryGetValue(date, out var t) ? (decimal?)t : null);
            })
            .ToList();
    }

    public async Task<IReadOnlyList<WeeklyEmissionPoint>> GetWeeklyAsync(Guid userId, int year, int month)
    {
        await Latency();
        var today = _clock.Today;
        return Enumerable.Range(1, DateTime.DaysInMonth(year, month))
            .Select(d => new DateOnly(year, month, d))
            .Where(d => d.DayOfWeek == DayOfWeek.Monday)
            .Select(monday =>
            {
                var sunday = monday.AddDays(6);
                var total = _logs
                    .Where(l => l.UserId == userId && l.EmisiKg.HasValue &&
                                l.Tanggal >= monday && l.Tanggal <= sunday)
                    .Sum(l => l.EmisiKg!.Value);
                return new WeeklyEmissionPoint(monday, total, monday <= today);
            })
            .ToList();
    }

    // ------------------------------------------------------------ Badges

    public async Task SettleCompletedWeeksAsync(Guid userId)
    {
        await Latency();
        var mine = _logs.Where(l => l.UserId == userId && l.EmisiKg.HasValue).ToList();
        if (mine.Count == 0) return;

        var firstMonday = WeekHelper.MondayOf(mine.Min(l => l.Tanggal));
        var lastCompleted = WeekHelper.MondayOf(_clock.Today).AddDays(-7);

        for (var monday = firstMonday; monday <= lastCompleted; monday = monday.AddDays(7))
        {
            if (_weekly.Any(w => w.UserId == userId && w.Record.MingguMulai == monday)) continue;

            var sunday = monday.AddDays(6);
            var week = mine.Where(l => l.Tanggal >= monday && l.Tanggal <= sunday).ToList();
            var total = week.Sum(l => l.EmisiKg!.Value);
            var days = week.Select(l => l.Tanggal).Distinct().Count();
            _weekly.Add((userId, new WeeklyBadgeRecord(monday, sunday, total, BadgeRules.EvaluateWeek(total, days), days)));
        }
    }

    public async Task<WeeklyBadgeRecord?> GetLastWeekBadgeAsync(Guid userId)
    {
        await Latency();
        var lastMonday = WeekHelper.MondayOf(_clock.Today).AddDays(-7);
        return _weekly.FirstOrDefault(w => w.UserId == userId && w.Record.MingguMulai == lastMonday).Record;
    }

    public async Task<IReadOnlyList<WeeklyBadgeRecord>> GetEarnedBadgesAsync(Guid userId)
    {
        await Latency();
        return _weekly
            .Where(w => w.UserId == userId && w.Record.Badge != BadgeLevel.None)
            .Select(w => w.Record)
            .OrderByDescending(r => r.MingguMulai)
            .ToList();
    }

    // ------------------------------------------------------------ Leaderboard

    public async Task<IReadOnlyList<LeaderboardEntry>> GetCurrentWeekAsync(Guid userId)
    {
        await Latency();
        var monday = WeekHelper.MondayOf(_clock.Today);
        var me = _accounts.First(a => a.User.Id == userId).User;
        var myTotal = _logs
            .Where(l => l.UserId == userId && l.EmisiKg.HasValue && l.Tanggal >= monday)
            .Sum(l => l.EmisiKg!.Value);

        var rows = OtherUsers.Select(o => (o.Nama, Total: o.Total, IsMe: false))
            .Append((me.Nama, Total: myTotal, IsMe: true))
            .OrderBy(r => r.Total)
            .ThenBy(r => r.Nama)
            .ToList();

        return rows
            .Select((r, i) => new LeaderboardEntry(i + 1, r.Nama, r.Total, BadgeRules.FromTotal(r.Total), r.IsMe))
            .ToList();
    }
}
