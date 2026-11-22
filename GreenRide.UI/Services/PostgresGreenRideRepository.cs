using System;
using System.Collections.Generic;
using Npgsql;
using GreenRide.Models;
using GreenRide.UI.Models;

namespace GreenRide.UI.Services;

/// <summary>
/// POLYMORPHISM: Implementasi IGreenRideRepository menggunakan PostgreSQL via Supabase.
/// Dapat menggantikan InMemoryGreenRideRepository tanpa mengubah kode UI.
/// </summary>
public sealed class PostgresGreenRideRepository : IGreenRideRepository
{
    // ENCAPSULATION: connection string dibaca dari environment variable
    private readonly string _connString =
        Environment.GetEnvironmentVariable("GREENRIDE_DB")
        ?? throw new InvalidOperationException(
            "GREENRIDE_DB environment variable tidak ditemukan. " +
            "Pastikan file .env sudah ada di folder proyek.");

    private NpgsqlConnection GetConnection() => new(_connString);

    // ── Autentikasi ────────────────────────────────────────────────────────────
    public AccountInfo? Authenticate(string email, string password)
    {
        const string sql = @"SELECT id, nama, email, password_hash
                             FROM users WHERE email = @email LIMIT 1";
        try
        {
            using var conn = GetConnection();
            conn.Open();
            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("email", email.Trim().ToLower());
            using var reader = cmd.ExecuteReader();
            if (!reader.Read()) return null;

            var id    = reader.GetGuid(0);
            var nama  = reader.GetString(1);
            var mail  = reader.GetString(2);
            var hash  = reader.GetString(3);

            return BCrypt.Net.BCrypt.Verify(password, hash)
                ? new AccountInfo(id, nama, mail)
                : null;
        }
        catch { return null; }
    }

    // ── Registrasi ─────────────────────────────────────────────────────────────
    public AccountInfo? Register(string nama, string email, string password)
    {
        const string cekSql = "SELECT COUNT(*) FROM users WHERE email = @email";
        const string insSql = @"INSERT INTO users (nama, email, password_hash)
                                VALUES (@nama, @email, @hash)
                                RETURNING id";
        try
        {
            using var conn = GetConnection();
            conn.Open();

            using (var cek = new NpgsqlCommand(cekSql, conn))
            {
                cek.Parameters.AddWithValue("email", email.Trim().ToLower());
                if (Convert.ToInt64(cek.ExecuteScalar()) > 0) return null;
            }

            var hash = BCrypt.Net.BCrypt.HashPassword(password);
            using var ins = new NpgsqlCommand(insSql, conn);
            ins.Parameters.AddWithValue("nama",  nama.Trim());
            ins.Parameters.AddWithValue("email", email.Trim().ToLower());
            ins.Parameters.AddWithValue("hash",  hash);

            var newId = (Guid)ins.ExecuteScalar()!;
            return new AccountInfo(newId, nama.Trim(), email.Trim().ToLower());
        }
        catch (Exception ex) { Console.WriteLine("REGISTER ERROR: " + ex.Message); return null; }
    }

    // ── Moda Transportasi ──────────────────────────────────────────────────────
    public IReadOnlyList<TransportMode> GetTransportModes()
    {
        const string sql = "SELECT id, nama, faktor_emisi FROM transport_modes ORDER BY id";
        var list = new List<TransportMode>();
        try
        {
            using var conn = GetConnection();
            conn.Open();
            using var cmd  = new NpgsqlCommand(sql, conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                list.Add(new TransportMode(
                    reader.GetInt32(0),
                    reader.GetString(1),
                    (double)reader.GetDecimal(2)));
        }
        catch { }
        return list;
    }

    // ── Trip ───────────────────────────────────────────────────────────────────
    public TripLog StartTrip(Guid userId, int modaId)
    {
        const string sql = @"INSERT INTO trip_logs (user_id, moda_id, tanggal, waktu_mulai)
                             VALUES (@uid, @mid, @tgl, @wm)
                             RETURNING id";
        try
        {
            using var conn = GetConnection();
            conn.Open();

            var now   = DateTime.UtcNow;                         // konsisten dengan NOW() (UTC) di PostgreSQL
            var today = DateOnly.FromDateTime(DateTime.Now);     // tanggal tetap mengikuti hari lokal

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("uid", userId);
            cmd.Parameters.AddWithValue("mid", modaId);
            cmd.Parameters.Add(new NpgsqlParameter("tgl", NpgsqlTypes.NpgsqlDbType.Date) { Value = today });
            cmd.Parameters.AddWithValue("wm", now);

            var id = (Guid)cmd.ExecuteScalar()!;
            return new TripLog(id, userId, modaId, today, now, null, null, null, false);
        }
        catch (Exception ex) { Console.WriteLine("STARTTRIP ERROR: " + ex.Message); throw; }
    }

    public TripLog? GetActiveTrip(Guid userId)
    {
        const string sql = @"SELECT id, user_id, moda_id, tanggal, waktu_mulai
                             FROM trip_logs
                             WHERE user_id = @uid AND waktu_selesai IS NULL
                             LIMIT 1";
        try
        {
            using var conn = GetConnection();
            conn.Open();
            using var cmd  = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("uid", userId);
            using var reader = cmd.ExecuteReader();
            if (!reader.Read()) return null;

            return new TripLog(
                reader.GetGuid(0),
                reader.GetGuid(1),
                reader.GetInt32(2),
                reader.GetFieldValue<DateOnly>(3),
                reader.GetDateTime(4),
                null, null, null, false);
        }
        catch { return null; }
    }

    public TripLog CompleteTrip(Guid tripId, decimal faktorEmisi)
    {
        // Durasi & emisi dihitung di SQL (waktu server) — "Cloud Timer".
        const string sql = @"UPDATE trip_logs
                             SET waktu_selesai = NOW(),
                                 durasi_menit  = EXTRACT(EPOCH FROM NOW() - waktu_mulai) / 60,
                                 emisi_kg      = ROUND((EXTRACT(EPOCH FROM NOW() - waktu_mulai) / 60 * @faktorEmisi)::numeric, 4)
                             WHERE id = @tripId
                             RETURNING id, user_id, moda_id, tanggal, waktu_mulai,
                                       waktu_selesai, durasi_menit, emisi_kg, is_revised";
        try
        {
            using var conn = GetConnection();
            conn.Open();
            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("tripId", tripId);
            cmd.Parameters.Add(new NpgsqlParameter("faktorEmisi", NpgsqlTypes.NpgsqlDbType.Numeric) { Value = faktorEmisi });
            using var reader = cmd.ExecuteReader();
            if (!reader.Read()) throw new InvalidOperationException("Trip tidak ditemukan.");

            return new TripLog(
                reader.GetGuid(0),
                reader.GetGuid(1),
                reader.GetInt32(2),
                reader.GetFieldValue<DateOnly>(3),
                reader.GetDateTime(4),
                reader.IsDBNull(5) ? null : reader.GetDateTime(5),
                reader.IsDBNull(6) ? null : reader.GetDecimal(6),
                reader.IsDBNull(7) ? null : reader.GetDecimal(7),
                reader.GetBoolean(8));
        }
        catch (Exception ex) { Console.WriteLine("COMPLETETRIP ERROR: " + ex.Message); throw; }
    }

    public IReadOnlyList<TripLog> GetTripLogs(Guid userId, DateOnly from, DateOnly to)
    {
        const string sql = @"SELECT id, user_id, moda_id, tanggal, waktu_mulai,
                                    waktu_selesai, durasi_menit, emisi_kg, is_revised
                             FROM trip_logs
                             WHERE user_id = @uid
                               AND tanggal BETWEEN @from AND @to
                             ORDER BY tanggal, waktu_mulai";
        var list = new List<TripLog>();
        try
        {
            using var conn = GetConnection();
            conn.Open();
            using var cmd  = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("uid", userId);
            cmd.Parameters.Add(new NpgsqlParameter("from", NpgsqlTypes.NpgsqlDbType.Date) { Value = from });
            cmd.Parameters.Add(new NpgsqlParameter("to",   NpgsqlTypes.NpgsqlDbType.Date) { Value = to   });
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                list.Add(new TripLog(
                    reader.GetGuid(0),
                    reader.GetGuid(1),
                    reader.GetInt32(2),
                    reader.GetFieldValue<DateOnly>(3),
                    reader.GetDateTime(4),
                    reader.IsDBNull(5)  ? null : reader.GetDateTime(5),
                    reader.IsDBNull(6)  ? null : reader.GetDecimal(6),
                    reader.IsDBNull(7)  ? null : reader.GetDecimal(7),
                    reader.GetBoolean(8)));
        }
        catch { }
        return list;
    }

    public void UpdateTripLog(Guid tripId, int modaIdBaru, decimal faktorEmisiBaru)
    {
        const string selSql = "SELECT durasi_menit FROM trip_logs WHERE id = @id";
        const string updSql = @"UPDATE trip_logs
                                SET moda_id    = @mid,
                                    emisi_kg   = @emisi,
                                    is_revised = TRUE
                                WHERE id = @id";
        try
        {
            using var conn = GetConnection();
            conn.Open();

            decimal? durasi;
            using (var sel = new NpgsqlCommand(selSql, conn))
            {
                sel.Parameters.AddWithValue("id", tripId);
                var raw = sel.ExecuteScalar();
                durasi = raw is null || raw is DBNull ? null : Convert.ToDecimal(raw);
            }

            var emisiKg = durasi.HasValue ? durasi.Value * faktorEmisiBaru : (decimal?)null;

            using var upd = new NpgsqlCommand(updSql, conn);
            upd.Parameters.AddWithValue("mid",   modaIdBaru);
            upd.Parameters.AddWithValue("emisi", (object?)emisiKg ?? DBNull.Value);
            upd.Parameters.AddWithValue("id",    tripId);
            upd.ExecuteNonQuery();
        }
        catch { }
    }

    /// <summary>
    /// Revisi waktu mulai/selesai. Durasi &amp; emisi dihitung ulang di SQL;
    /// faktor emisi diambil dari transport_modes lewat moda_id trip.
    /// </summary>
    public async Task<TripLog?> UpdateTripLog(Guid tripId, DateTime waktuMulai, DateTime waktuSelesai)
    {
        const string sql = @"UPDATE trip_logs t
                             SET waktu_mulai   = @waktuMulai,
                                 waktu_selesai = @waktuSelesai,
                                 durasi_menit  = EXTRACT(EPOCH FROM (@waktuSelesai - @waktuMulai)) / 60,
                                 emisi_kg      = ROUND((EXTRACT(EPOCH FROM (@waktuSelesai - @waktuMulai)) / 60 * m.faktor_emisi)::numeric, 4),
                                 is_revised    = TRUE
                             FROM transport_modes m
                             WHERE t.id = @tripId AND m.id = t.moda_id
                             RETURNING t.id, t.user_id, t.moda_id, t.tanggal, t.waktu_mulai,
                                       t.waktu_selesai, t.durasi_menit, t.emisi_kg, t.is_revised";
        try
        {
            await using var conn = GetConnection();
            await conn.OpenAsync();
            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("tripId", tripId);
            cmd.Parameters.Add(new NpgsqlParameter("waktuMulai",   NpgsqlTypes.NpgsqlDbType.Timestamp) { Value = waktuMulai });
            cmd.Parameters.Add(new NpgsqlParameter("waktuSelesai", NpgsqlTypes.NpgsqlDbType.Timestamp) { Value = waktuSelesai });
            await using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync()) return null;

            return new TripLog(
                reader.GetGuid(0),
                reader.GetGuid(1),
                reader.GetInt32(2),
                reader.GetFieldValue<DateOnly>(3),
                reader.GetDateTime(4),
                reader.IsDBNull(5) ? null : reader.GetDateTime(5),
                reader.IsDBNull(6) ? null : reader.GetDecimal(6),
                reader.IsDBNull(7) ? null : reader.GetDecimal(7),
                reader.GetBoolean(8));
        }
        catch (Exception ex) { Console.WriteLine("UPDATETRIPLOG ERROR: " + ex.Message); throw; }
    }

    // ── Badge, Kalender & Leaderboard ──────────────────────────────────────────
    public WeeklyBadge? GetPreviousWeekBadge(Guid userId)
    {
        const string sql = @"SELECT id, user_id, minggu_mulai, minggu_selesai,
                                total_emisi_kg, badge_name, hari_tercatat
                         FROM weekly_badges
                         WHERE user_id = @uid AND minggu_selesai < @mingguIni
                         ORDER BY minggu_selesai DESC
                         LIMIT 1";
        // Senin minggu berjalan (Senin = hari ke-0)
        var hariIni = DateOnly.FromDateTime(DateTime.Today);
        var mingguIni = hariIni.AddDays(-(((int)hariIni.DayOfWeek + 6) % 7));
        try
        {
            using var conn = GetConnection();
            conn.Open();
            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("uid", userId);
            cmd.Parameters.Add(new NpgsqlParameter("mingguIni", NpgsqlTypes.NpgsqlDbType.Date) { Value = mingguIni });
            using var reader = cmd.ExecuteReader();
            if (!reader.Read()) return null;

            return new WeeklyBadge(
                reader.GetGuid(0),
                reader.GetGuid(1),
                reader.GetFieldValue<DateOnly>(2),
                reader.GetFieldValue<DateOnly>(3),
                reader.GetDecimal(4),
                reader.IsDBNull(5) ? null : reader.GetString(5),
                reader.GetInt32(6));
        }
        catch (Exception ex) { Console.WriteLine("GETPREVIOUSWEEKBADGE ERROR: " + ex.Message); return null; }
    }

    public IReadOnlyList<WeeklyBadge> GetAllWeeklyBadges(Guid userId)
    {
        const string sql = @"SELECT id, user_id, minggu_mulai, minggu_selesai,
                                total_emisi_kg, badge_name, hari_tercatat
                         FROM weekly_badges
                         WHERE user_id = @uid
                         ORDER BY minggu_mulai";
        var list = new List<WeeklyBadge>();
        try
        {
            using var conn = GetConnection();
            conn.Open();
            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("uid", userId);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                list.Add(new WeeklyBadge(
                    reader.GetGuid(0),
                    reader.GetGuid(1),
                    reader.GetFieldValue<DateOnly>(2),
                    reader.GetFieldValue<DateOnly>(3),
                    reader.GetDecimal(4),
                    reader.IsDBNull(5) ? null : reader.GetString(5),
                    reader.GetInt32(6)));
        }
        catch (Exception ex) { Console.WriteLine("GETALLWEEKLYBADGES ERROR: " + ex.Message); }
        return list;
    }

    public void AddWeeklyBadge(WeeklyBadge badge)
    {
        const string sql = @"INSERT INTO weekly_badges
                             (id, user_id, minggu_mulai, minggu_selesai,
                              total_emisi_kg, badge_name, hari_tercatat)
                         VALUES (@id, @uid, @mulai, @selesai, @total, @badge, @hari)
                         ON CONFLICT (user_id, minggu_mulai) DO NOTHING";
        try
        {
            using var conn = GetConnection();
            conn.Open();
            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("id", badge.Id);
            cmd.Parameters.AddWithValue("uid", badge.UserId);
            cmd.Parameters.Add(new NpgsqlParameter("mulai", NpgsqlTypes.NpgsqlDbType.Date) { Value = badge.MingguMulai });
            cmd.Parameters.Add(new NpgsqlParameter("selesai", NpgsqlTypes.NpgsqlDbType.Date) { Value = badge.MingguSelesai });
            cmd.Parameters.AddWithValue("total", badge.TotalEmisiKg);
            cmd.Parameters.AddWithValue("badge", (object?)badge.BadgeName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("hari", badge.HariTercatat);
            cmd.ExecuteNonQuery();
        }
        catch (Exception ex) { Console.WriteLine("ADDWEEKLYBADGE ERROR: " + ex.Message); }
    }

    public Dictionary<DateOnly, decimal> GetCalendarEmission(Guid userId, int year, int month)
    {
        const string sql = @"SELECT tanggal, SUM(emisi_kg) AS total_emisi_harian
                         FROM trip_logs
                         WHERE user_id = @uid
                           AND emisi_kg IS NOT NULL
                           AND tanggal BETWEEN @awal AND @akhir
                         GROUP BY tanggal";
        var hasil = new Dictionary<DateOnly, decimal>();
        var awal = new DateOnly(year, month, 1);
        var akhir = awal.AddMonths(1).AddDays(-1);
        try
        {
            using var conn = GetConnection();
            conn.Open();
            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("uid", userId);
            cmd.Parameters.Add(new NpgsqlParameter("awal", NpgsqlTypes.NpgsqlDbType.Date) { Value = awal });
            cmd.Parameters.Add(new NpgsqlParameter("akhir", NpgsqlTypes.NpgsqlDbType.Date) { Value = akhir });
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                hasil[reader.GetFieldValue<DateOnly>(0)] = reader.GetDecimal(1);
        }
        catch (Exception ex) { Console.WriteLine("GETCALENDAREMISSION ERROR: " + ex.Message); }
        return hasil;
    }

    public IReadOnlyList<LeaderboardEntry> GetLeaderboard(DateOnly weekStart)
    {
        const string sql = @"SELECT RANK() OVER (ORDER BY COALESCE(SUM(tl.emisi_kg), 0)) AS peringkat,
                                u.nama,
                                COALESCE(SUM(tl.emisi_kg), 0) AS total_emisi_minggu_ini
                         FROM users u
                         LEFT JOIN trip_logs tl
                                ON tl.user_id = u.id
                               AND tl.tanggal BETWEEN @senin AND @hariIni
                         GROUP BY u.id, u.nama
                         ORDER BY total_emisi_minggu_ini, u.nama";
        var list = new List<LeaderboardEntry>();
        var hariIni = DateOnly.FromDateTime(DateTime.Today);
        try
        {
            using var conn = GetConnection();
            conn.Open();
            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.Add(new NpgsqlParameter("senin", NpgsqlTypes.NpgsqlDbType.Date) { Value = weekStart });
            cmd.Parameters.Add(new NpgsqlParameter("hariIni", NpgsqlTypes.NpgsqlDbType.Date) { Value = hariIni });
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                list.Add(new LeaderboardEntry(
                    (int)reader.GetInt64(0),
                    reader.GetString(1),
                    reader.GetDecimal(2)));
        }
        catch (Exception ex) { Console.WriteLine("GETLEADERBOARD ERROR: " + ex.Message); }
        return list;
    }
}
