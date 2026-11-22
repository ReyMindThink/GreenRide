using Npgsql;
using NpgsqlTypes;
using GreenRide.UI.Helpers;
using GreenRide.UI.Models;

namespace GreenRide.UI.Services;

public sealed class PostgresGreenRideRepository
    : IAuthService, ITripService, IEmissionService,
      IBadgeProgressService, ILeaderboardService
{
    private readonly string _connString =
        Environment.GetEnvironmentVariable("GREENRIDE_DB")
        ?? throw new InvalidOperationException(
            "GREENRIDE_DB environment variable tidak ditemukan. " +
            "Pastikan file .env sudah ada di folder proyek.");

    private readonly IClock _clock;

    public PostgresGreenRideRepository(IClock clock) => _clock = clock;

    private NpgsqlConnection GetConnection() => new(_connString);

    // ── IAuthService ──────────────────────────────────────────────────────────

    public async Task<AuthResult> LoginAsync(string email, string password)
    {
        const string sql = @"SELECT id, nama, email, password_hash
                             FROM users WHERE email = @email LIMIT 1";
        try
        {
            await using var conn = GetConnection();
            await conn.OpenAsync();
            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("email", email.Trim().ToLower());
            await using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                return AuthResult.Fail("Email atau password salah.");

            var id   = reader.GetGuid(0);
            var nama = reader.GetString(1);
            var mail = reader.GetString(2);
            var hash = reader.GetString(3);

            if (!BCrypt.Net.BCrypt.Verify(password, hash))
                return AuthResult.Fail("Email atau password salah.");

            return AuthResult.Ok(new UserAccount(id, nama, mail));
        }
        catch (Exception ex)
        {
            Console.WriteLine("LOGIN ERROR: " + ex.Message);
            return AuthResult.Fail("Gagal terhubung ke server.");
        }
    }

    public async Task<AuthResult> RegisterAsync(string nama, string email, string password)
    {
        const string cekSql = "SELECT COUNT(*) FROM users WHERE email = @email";
        const string insSql = @"INSERT INTO users (nama, email, password_hash)
                                VALUES (@nama, @email, @hash)
                                RETURNING id";
        try
        {
            await using var conn = GetConnection();
            await conn.OpenAsync();

            await using (var cek = new NpgsqlCommand(cekSql, conn))
            {
                cek.Parameters.AddWithValue("email", email.Trim().ToLower());
                var count = Convert.ToInt64(await cek.ExecuteScalarAsync());
                if (count > 0)
                    return AuthResult.Fail("Email sudah terdaftar.");
            }

            var hash = BCrypt.Net.BCrypt.HashPassword(password);
            await using var ins = new NpgsqlCommand(insSql, conn);
            ins.Parameters.AddWithValue("nama",  nama.Trim());
            ins.Parameters.AddWithValue("email", email.Trim().ToLower());
            ins.Parameters.AddWithValue("hash",  hash);

            var newId = (Guid)(await ins.ExecuteScalarAsync())!;
            return AuthResult.Ok(new UserAccount(newId, nama.Trim(), email.Trim().ToLower()));
        }
        catch (Exception ex)
        {
            Console.WriteLine("REGISTER ERROR: " + ex.Message);
            return AuthResult.Fail("Gagal mendaftar. Coba lagi.");
        }
    }

    // ── ITripService ──────────────────────────────────────────────────────────

    public async Task<TripLog?> GetActiveTripAsync(Guid userId)
    {
        const string sql = @"SELECT id, user_id, moda_id, tanggal, waktu_mulai
                             FROM trip_logs
                             WHERE user_id = @uid AND waktu_selesai IS NULL
                             LIMIT 1";
        try
        {
            await using var conn = GetConnection();
            await conn.OpenAsync();
            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("uid", userId);
            await using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync()) return null;

            return new TripLog
            {
                Id         = reader.GetGuid(0),
                UserId     = reader.GetGuid(1),
                ModaId     = reader.GetInt32(2),
                Tanggal    = reader.GetFieldValue<DateOnly>(3),
                WaktuMulai = reader.GetDateTime(4),
            };
        }
        catch { return null; }
    }

    public async Task<TripLog> StartTripAsync(Guid userId, int modaId)
    {
        const string sql = @"INSERT INTO trip_logs (user_id, moda_id, tanggal, waktu_mulai)
                             VALUES (@uid, @mid, @tgl, @wm)
                             RETURNING id";
        try
        {
            await using var conn = GetConnection();
            await conn.OpenAsync();

            var now   = DateTime.Now;
            var today = DateOnly.FromDateTime(DateTime.Now);

            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("uid", userId);
            cmd.Parameters.AddWithValue("mid", modaId);
            cmd.Parameters.Add(new NpgsqlParameter("tgl", NpgsqlTypes.NpgsqlDbType.Date) { Value = today });
            cmd.Parameters.AddWithValue("wm",  now);

            var id = (Guid)(await cmd.ExecuteScalarAsync())!;
            return new TripLog
            {
                Id         = id,
                UserId     = userId,
                ModaId     = modaId,
                Tanggal    = today,
                WaktuMulai = now,
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine("STARTTRIP ERROR: " + ex.Message);
            throw;
        }
    }

    public async Task<TripLog> StopTripAsync(Guid userId)
    {
        const string sql = @"UPDATE trip_logs t
                             SET waktu_selesai = NOW() AT TIME ZONE 'Asia/Jakarta',
                                 durasi_menit  = EXTRACT(EPOCH FROM NOW() AT TIME ZONE 'Asia/Jakarta' - t.waktu_mulai) / 60,
                                 emisi_kg      = ROUND(
                                     (EXTRACT(EPOCH FROM NOW() AT TIME ZONE 'Asia/Jakarta' - t.waktu_mulai) / 60 * m.faktor_emisi)::numeric, 4)
                             FROM transport_modes m
                             WHERE t.user_id = @uid
                               AND t.waktu_selesai IS NULL
                               AND m.id = t.moda_id
                             RETURNING t.id, t.user_id, t.moda_id, t.tanggal, t.waktu_mulai,
                                       t.waktu_selesai, t.durasi_menit, t.emisi_kg, t.is_revised";
        try
        {
            await using var conn = GetConnection();
            await conn.OpenAsync();
            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("uid", userId);
            await using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                throw new ServiceException("Tidak ada trip yang sedang berjalan.");

            return ReadTripLog(reader);
        }
        catch (ServiceException) { throw; }
        catch (Exception ex)
        {
            Console.WriteLine("STOPTRIP ERROR: " + ex.Message);
            throw new ServiceException("Gagal menghentikan trip.");
        }
    }

    public async Task<IReadOnlyList<TripLog>> GetLogsAsync(Guid userId, DateOnly tanggal)
    {
        const string sql = @"SELECT id, user_id, moda_id, tanggal, waktu_mulai,
                                    waktu_selesai, durasi_menit, emisi_kg, is_revised
                             FROM trip_logs
                             WHERE user_id = @uid AND tanggal = @tgl
                             ORDER BY waktu_mulai";
        var list = new List<TripLog>();
        try
        {
            await using var conn = GetConnection();
            await conn.OpenAsync();
            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("uid", userId);
            cmd.Parameters.Add(new NpgsqlParameter("tgl", NpgsqlTypes.NpgsqlDbType.Date) { Value = tanggal });
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                list.Add(ReadTripLog(reader));
        }
        catch { }
        return list;
    }

    public async Task<TripLog> ReviseTripAsync(
        Guid userId, Guid logId, int modaId,
        DateTime waktuMulai, DateTime waktuSelesai)
    {
        const string checkSql = @"SELECT tanggal FROM trip_logs
                                  WHERE id = @id AND user_id = @uid LIMIT 1";
        const string updSql   = @"UPDATE trip_logs t
                                  SET moda_id       = @mid,
                                      waktu_mulai   = @wm,
                                      waktu_selesai = @ws,
                                      durasi_menit  = EXTRACT(EPOCH FROM (@ws - @wm)) / 60,
                                      emisi_kg      = ROUND(
                                          (EXTRACT(EPOCH FROM (@ws - @wm)) / 60 * m.faktor_emisi)::numeric, 4),
                                      is_revised    = TRUE
                                  FROM transport_modes m
                                  WHERE t.id = @id AND m.id = @mid
                                  RETURNING t.id, t.user_id, t.moda_id, t.tanggal, t.waktu_mulai,
                                            t.waktu_selesai, t.durasi_menit, t.emisi_kg, t.is_revised";
        try
        {
            await using var conn = GetConnection();
            await conn.OpenAsync();

            await using (var check = new NpgsqlCommand(checkSql, conn))
            {
                check.Parameters.AddWithValue("id",  logId);
                check.Parameters.AddWithValue("uid", userId);
                var raw = await check.ExecuteScalarAsync();
                if (raw is null)
                    throw new ServiceException("Log tidak ditemukan.");
                if (DateOnly.FromDateTime((DateTime)raw) != DateOnly.FromDateTime(DateTime.Now))
                    throw new ServiceException("Hanya log hari ini yang bisa direvisi.");
            }

            const string overlapSql = @"SELECT COUNT(*) FROM trip_logs
                             WHERE user_id = @uid
                               AND id != @id
                               AND tanggal = @tgl::date
                               AND waktu_selesai IS NOT NULL
                               AND waktu_mulai < @ws
                               AND waktu_selesai > @wm";

            await using (var ovl = new NpgsqlCommand(overlapSql, conn))
            {
                ovl.Parameters.AddWithValue("uid", userId);
                ovl.Parameters.AddWithValue("id",  logId);
                ovl.Parameters.Add(new NpgsqlParameter("tgl", NpgsqlTypes.NpgsqlDbType.Date) { Value = DateOnly.FromDateTime(DateTime.Now) });
                ovl.Parameters.Add(new NpgsqlParameter("wm", NpgsqlTypes.NpgsqlDbType.Timestamp) { Value = waktuMulai });
                ovl.Parameters.Add(new NpgsqlParameter("ws", NpgsqlTypes.NpgsqlDbType.Timestamp) { Value = waktuSelesai });
                var count = Convert.ToInt64(await ovl.ExecuteScalarAsync());
                if (count > 0)
                    throw new ServiceException("Waktu bertabrakan dengan perjalanan lain hari ini.");
            }

            await using var upd = new NpgsqlCommand(updSql, conn);
            upd.Parameters.AddWithValue("id",  logId);
            upd.Parameters.AddWithValue("mid", modaId);
            upd.Parameters.Add(new NpgsqlParameter("wm", NpgsqlTypes.NpgsqlDbType.Timestamp) { Value = waktuMulai   });
            upd.Parameters.Add(new NpgsqlParameter("ws", NpgsqlTypes.NpgsqlDbType.Timestamp) { Value = waktuSelesai });
            await using var reader = await upd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                throw new ServiceException("Revisi gagal.");

            return ReadTripLog(reader);
        }
        catch (ServiceException) { throw; }
        catch (Exception ex)
        {
            Console.WriteLine("REVISE ERROR: " + ex.Message);
            throw new ServiceException("Gagal merevisi log.");
        }
    }

    // ── IEmissionService ──────────────────────────────────────────────────────

    public async Task<IReadOnlyList<CalendarDayEmission>> GetMonthAsync(
        Guid userId, int year, int month)
    {
        const string sql = @"SELECT d::date,
                                    SUM(tl.emisi_kg)
                             FROM generate_series(
                                 date_trunc('month', make_date(@year, @month, 1)),
                                 date_trunc('month', make_date(@year, @month, 1)) + interval '1 month - 1 day',
                                 interval '1 day') d
                             LEFT JOIN trip_logs tl
                                    ON tl.tanggal = d::date
                                   AND tl.user_id = @uid
                                   AND tl.waktu_selesai IS NOT NULL
                             GROUP BY d
                             ORDER BY d";
        var list = new List<CalendarDayEmission>();
        try
        {
            await using var conn = GetConnection();
            await conn.OpenAsync();
            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("uid",   userId);
            cmd.Parameters.AddWithValue("year",  year);
            cmd.Parameters.AddWithValue("month", month);
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var tanggal = reader.GetFieldValue<DateOnly>(0);
                var emisi   = reader.IsDBNull(1) ? (decimal?)null : reader.GetDecimal(1);
                list.Add(new CalendarDayEmission(tanggal, emisi));
            }
        }
        catch (Exception ex) { Console.WriteLine("GETMONTH ERROR: " + ex.Message); }
        return list;
    }

    public async Task<IReadOnlyList<WeeklyEmissionPoint>> GetWeeklyAsync(
        Guid userId, int year, int month)
    {
        const string sql = @"SELECT date_trunc('week', tanggal::timestamp)::date AS minggu_mulai,
                                    SUM(emisi_kg) AS total_emisi
                             FROM trip_logs
                             WHERE user_id = @uid
                               AND EXTRACT(YEAR  FROM tanggal) = @year
                               AND EXTRACT(MONTH FROM tanggal) = @month
                               AND waktu_selesai IS NOT NULL
                             GROUP BY minggu_mulai
                             ORDER BY minggu_mulai";
        var list = new List<WeeklyEmissionPoint>();
        try
        {
            await using var conn = GetConnection();
            await conn.OpenAsync();
            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("uid",   userId);
            cmd.Parameters.AddWithValue("year",  year);
            cmd.Parameters.AddWithValue("month", month);
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var minggu = reader.GetFieldValue<DateOnly>(0);
                var total  = reader.GetDecimal(1);
                list.Add(new WeeklyEmissionPoint(minggu, total, true));
            }
        }
        catch (Exception ex) { Console.WriteLine("GETWEEKLY ERROR: " + ex.Message); }
        return list;
    }

    // ── IBadgeProgressService ─────────────────────────────────────────────────

    private const int MingguDicek = 8;   // berapa minggu ke belakang dinilai saat login

    public async Task SettleCompletedWeeksAsync(Guid userId)
    {
        var mingguIni = WeekHelper.MondayOf(_clock.Today);
        var batasAwal = mingguIni.AddDays(-7 * MingguDicek);

        const string cekSql = "SELECT minggu_mulai FROM weekly_badges WHERE user_id = @uid";
        const string logSql = @"SELECT tanggal, SUM(emisi_kg)
                                FROM trip_logs
                                WHERE user_id = @uid
                                  AND waktu_selesai IS NOT NULL
                                  AND tanggal >= @awal AND tanggal < @akhir
                                GROUP BY tanggal";
        const string insSql = @"INSERT INTO weekly_badges
                                    (user_id, minggu_mulai, minggu_selesai,
                                     total_emisi_kg, badge_name, hari_tercatat)
                                VALUES (@uid, @mulai, @selesai, @total, @badge, @hari)
                                ON CONFLICT (user_id, minggu_mulai) DO NOTHING";
        try
        {
            await using var conn = GetConnection();
            await conn.OpenAsync();

            // 1. Minggu yang sudah pernah dinilai
            var sudahAda = new HashSet<DateOnly>();
            await using (var cek = new NpgsqlCommand(cekSql, conn))
            {
                cek.Parameters.AddWithValue("uid", userId);
                await using var r = await cek.ExecuteReaderAsync();
                while (await r.ReadAsync()) sudahAda.Add(r.GetFieldValue<DateOnly>(0));
            }

            // 2. Total emisi harian 8 minggu terakhir, dikelompokkan per minggu (Senin–Minggu)
            var perMinggu = new Dictionary<DateOnly, (decimal Total, int Hari)>();
            await using (var log = new NpgsqlCommand(logSql, conn))
            {
                log.Parameters.AddWithValue("uid", userId);
                log.Parameters.Add("awal",  NpgsqlDbType.Date).Value = batasAwal;
                log.Parameters.Add("akhir", NpgsqlDbType.Date).Value = mingguIni;
                await using var r = await log.ExecuteReaderAsync();
                while (await r.ReadAsync())
                {
                    var senin = WeekHelper.MondayOf(r.GetFieldValue<DateOnly>(0));
                    var emisi = r.IsDBNull(1) ? 0m : r.GetDecimal(1);
                    perMinggu.TryGetValue(senin, out var cur);
                    perMinggu[senin] = (cur.Total + emisi, cur.Hari + 1);
                }
            }

            // 3. Simpan minggu yang belum ada (kurang dari 7 hari tercatat = tanpa badge)
            foreach (var (senin, data) in perMinggu)
            {
                if (sudahAda.Contains(senin)) continue;

                var level = BadgeRules.EvaluateWeek(data.Total, data.Hari);
                string? namaBadge = level == BadgeLevel.None ? null : BadgeCatalog.Get(level)?.DbName;

                await using var ins = new NpgsqlCommand(insSql, conn);
                ins.Parameters.AddWithValue("uid", userId);
                ins.Parameters.Add("mulai",   NpgsqlDbType.Date).Value = senin;
                ins.Parameters.Add("selesai", NpgsqlDbType.Date).Value = senin.AddDays(6);
                ins.Parameters.AddWithValue("total", data.Total);
                ins.Parameters.Add("badge", NpgsqlDbType.Varchar).Value = (object?)namaBadge ?? DBNull.Value;
                ins.Parameters.AddWithValue("hari", data.Hari);
                await ins.ExecuteNonQueryAsync();
            }
        }
        catch (Exception ex) { Console.WriteLine("SETTLEWEEKS ERROR: " + ex.Message); }
    }

    public async Task<WeeklyBadgeRecord?> GetLastWeekBadgeAsync(Guid userId)
    {
        var mulai = WeekHelper.MondayOf(_clock.Today).AddDays(-7);
        const string sql = @"SELECT minggu_mulai, minggu_selesai, total_emisi_kg, badge_name, hari_tercatat
                             FROM weekly_badges
                             WHERE user_id = @uid AND minggu_mulai = @mulai";
        try
        {
            await using var conn = GetConnection();
            await conn.OpenAsync();
            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("uid", userId);
            cmd.Parameters.Add("mulai", NpgsqlDbType.Date).Value = mulai;
            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync()) return ReadBadgeRecord(reader);
        }
        catch (Exception ex) { Console.WriteLine("GETLASTWEEKBADGE ERROR: " + ex.Message); }
        return null;
    }

    public async Task<IReadOnlyList<WeeklyBadgeRecord>> GetEarnedBadgesAsync(Guid userId)
    {
        const string sql = @"SELECT minggu_mulai, minggu_selesai, total_emisi_kg, badge_name, hari_tercatat
                             FROM weekly_badges
                             WHERE user_id = @uid AND badge_name IS NOT NULL
                             ORDER BY minggu_mulai DESC";
        var list = new List<WeeklyBadgeRecord>();
        try
        {
            await using var conn = GetConnection();
            await conn.OpenAsync();
            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("uid", userId);
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync()) list.Add(ReadBadgeRecord(reader));
        }
        catch (Exception ex) { Console.WriteLine("GETEARNEDBADGES ERROR: " + ex.Message); }
        return list;
    }

    // ── ILeaderboardService ───────────────────────────────────────────────────

    // Hanya pengguna yang mendaftar sebelum Senin minggu ini yang masuk leaderboard, supaya
    // seluruh Senin-Minggu bisa dihitung (akun baru tidak langsung menjadi peringkat 1).
    // created_at disimpan dalam UTC, jadi dikonversi ke waktu Jakarta sebelum dibandingkan.
    // Peringkat memakai ROW_NUMBER (bukan RANK) agar nomornya unik dan berurutan 1, 2, 3, ...
    // UI mencari podium per nomor; dengan RANK, emisi yang sama menghasilkan nomor kembar.
    public async Task<IReadOnlyList<LeaderboardEntry>> GetCurrentWeekAsync(Guid userId)
    {
        var senin = WeekHelper.MondayOf(_clock.Today);
        const string sql = @"SELECT ROW_NUMBER() OVER (ORDER BY COALESCE(SUM(tl.emisi_kg), 0), u.nama) AS peringkat,
                                    u.id, u.nama,
                                    COALESCE(SUM(tl.emisi_kg), 0) AS total
                             FROM users u
                             LEFT JOIN trip_logs tl
                                    ON tl.user_id = u.id
                                   AND tl.waktu_selesai IS NOT NULL
                                   AND tl.tanggal BETWEEN @awal AND @akhir
                             WHERE ((u.created_at AT TIME ZONE 'UTC') AT TIME ZONE 'Asia/Jakarta') < @awal
                             GROUP BY u.id, u.nama
                             ORDER BY total, u.nama";
        var list = new List<LeaderboardEntry>();
        try
        {
            await using var conn = GetConnection();
            await conn.OpenAsync();
            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.Add("awal",  NpgsqlDbType.Date).Value = senin;
            cmd.Parameters.Add("akhir", NpgsqlDbType.Date).Value = senin.AddDays(6);
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var total = reader.GetDecimal(3);
                list.Add(new LeaderboardEntry(
                    Rank: Convert.ToInt32(reader.GetValue(0)),
                    Nama: reader.GetString(2),
                    TotalEmisiKg: total,
                    Badge: BadgeRules.FromTotal(total),
                    IsCurrentUser: reader.GetGuid(1) == userId));
            }
        }
        catch (Exception ex) { Console.WriteLine("GETLEADERBOARD ERROR: " + ex.Message); }
        return list;
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    private static WeeklyBadgeRecord ReadBadgeRecord(NpgsqlDataReader r) => new WeeklyBadgeRecord(
        r.GetFieldValue<DateOnly>(0),
        r.GetFieldValue<DateOnly>(1),
        r.GetDecimal(2),
        BadgeCatalog.FromDbName(r.IsDBNull(3) ? null : r.GetString(3)),
        Convert.ToInt32(r.GetValue(4)));

    private static TripLog ReadTripLog(NpgsqlDataReader r) => new TripLog
    {
        Id           = r.GetGuid(0),
        UserId       = r.GetGuid(1),
        ModaId       = r.GetInt32(2),
        Tanggal      = r.GetFieldValue<DateOnly>(3),
        WaktuMulai   = r.GetDateTime(4),
        WaktuSelesai = r.IsDBNull(5) ? null : r.GetDateTime(5),
        DurasiMenit  = r.IsDBNull(6) ? null : r.GetDecimal(6),
        EmisiKg      = r.IsDBNull(7) ? null : r.GetDecimal(7),
        IsRevised    = r.GetBoolean(8),
    };
}
