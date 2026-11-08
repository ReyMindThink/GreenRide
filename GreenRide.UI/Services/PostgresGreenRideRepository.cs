using System;
using System.Collections.Generic;
using Npgsql;
using GreenRide.Models;
using GreenRide.UI.Models;

namespace GreenRide.UI.Services;

/// <summary>
/// POLYMORPHISM: Implementasi IGreenRideRepository menggunakan PostgreSQL.
/// Dapat menggantikan InMemoryGreenRideRepository tanpa mengubah kode UI.
/// </summary>
public sealed class PostgresGreenRideRepository : IGreenRideRepository
{
    // ENCAPSULATION: connection string tersimpan private
    private readonly string _connString =
        "Host=localhost;Port=5432;Username=postgres;" +
        "Password=amina;Database=greenride";

    private NpgsqlConnection GetConnection() =>
        new NpgsqlConnection(_connString);

    // ── Autentikasi ────────────────────────────────────────────────
    public AccountInfo? Authenticate(string email, string password)
    {
        const string sql = @"SELECT user_id, nama, email, password_hash
                             FROM gr_users WHERE email = @email";
        try
        {
            using var conn = GetConnection();
            conn.Open();
            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("email", email.Trim());
            using var reader = cmd.ExecuteReader();
            if (!reader.Read()) return null;

            var user = new User(
                reader.GetInt32(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3));

            return user.Login(password)
                ? new AccountInfo(user.UserId, user.Nama, user.Email, "")
                : null;
        }
        catch { return null; }
    }

    // ── Registrasi ─────────────────────────────────────────────────
    public AccountInfo? Register(string nama, string email, string password)
    {
        // Cek email sudah ada
        const string cekSql = "SELECT COUNT(*) FROM gr_users WHERE email = @email";
        const string insertSql = @"INSERT INTO gr_users (nama, email, password_hash)
                                   VALUES (@nama, @email, @hash)
                                   RETURNING user_id";
        try
        {
            using var conn = GetConnection();
            conn.Open();

            using (var cek = new NpgsqlCommand(cekSql, conn))
            {
                cek.Parameters.AddWithValue("email", email.Trim());
                if (Convert.ToInt64(cek.ExecuteScalar()) > 0) return null;
            }

            var user = new User(nama, email, password); // BCrypt di sini
            using var ins = new NpgsqlCommand(insertSql, conn);
            ins.Parameters.AddWithValue("nama",  user.Nama);
            ins.Parameters.AddWithValue("email", user.Email);
            ins.Parameters.AddWithValue("hash",  user.Email); // placeholder
            // Ambil hash via reflection tidak perlu — pakai konstruktor User saja
            // Karena PasswordHash private, kita hash langsung:
            var hash = BCrypt.Net.BCrypt.HashPassword(password);
            ins.Parameters["hash"].Value = hash;

            var newId = Convert.ToInt32(ins.ExecuteScalar());
            return new AccountInfo(newId, nama.Trim(), email.Trim(), "");
        }
        catch { return null; }
    }

    // ── Moda Transportasi ──────────────────────────────────────────
    public IReadOnlyList<TransportMode> GetTransportModes()
    {
        const string sql = "SELECT transport_mode_id, nama_moda, faktor_emisi FROM gr_transport_modes ORDER BY transport_mode_id";
        var list = new List<TransportMode>();
        try
        {
            using var conn = GetConnection();
            conn.Open();
            using var cmd = new NpgsqlCommand(sql, conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                list.Add(new TransportMode(
                    reader.GetInt32(0),
                    reader.GetString(1),
                    reader.GetDouble(2)));
        }
        catch { }
        return list;
    }

    // ── Log Harian ─────────────────────────────────────────────────
    public IReadOnlyList<DailyLog> GetLogs(int userId, DateTime from, DateTime to)
    {
        const string sql = @"SELECT daily_log_id, user_id, tanggal, durasi, transport_mode_id
                             FROM gr_daily_logs
                             WHERE user_id = @uid AND tanggal BETWEEN @from AND @to
                             ORDER BY tanggal, daily_log_id";
        var list = new List<DailyLog>();
        try
        {
            using var conn = GetConnection();
            conn.Open();
            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("uid",  userId);
            cmd.Parameters.AddWithValue("from", from.Date);
            cmd.Parameters.AddWithValue("to",   to.Date);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var log = new DailyLog(
                    reader.GetInt32(1),
                    reader.GetDateTime(2),
                    reader.GetInt32(3),
                    reader.GetInt32(4));
                log.SetDailyLogId(reader.GetInt32(0));
                list.Add(log);
            }
        }
        catch { }
        return list;
    }

    public DailyLog AddLog(DailyLog log)
    {
        const string sql = @"INSERT INTO gr_daily_logs (user_id, tanggal, durasi, transport_mode_id)
                             VALUES (@uid, @tgl, @dur, @mid) RETURNING daily_log_id";
        try
        {
            using var conn = GetConnection();
            conn.Open();
            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("uid", log.UserId);
            cmd.Parameters.AddWithValue("tgl", log.Tanggal.Date);
            cmd.Parameters.AddWithValue("dur", log.Durasi);
            cmd.Parameters.AddWithValue("mid", log.TransportModeId);
            var newId = Convert.ToInt32(cmd.ExecuteScalar());
            log.SetDailyLogId(newId);
        }
        catch { }
        return log;
    }

    public void UpdateLog(DailyLog log)
    {
        const string sql = @"UPDATE gr_daily_logs
                             SET durasi = @dur, transport_mode_id = @mid
                             WHERE daily_log_id = @id";
        try
        {
            using var conn = GetConnection();
            conn.Open();
            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("dur", log.Durasi);
            cmd.Parameters.AddWithValue("mid", log.TransportModeId);
            cmd.Parameters.AddWithValue("id",  log.DailyLogId);
            cmd.ExecuteNonQuery();
        }
        catch { }
    }

    // ── Badge ──────────────────────────────────────────────────────
    public IReadOnlyList<UserBadge> GetUserBadges(int userId)
    {
        const string sql = @"SELECT user_badge_id, user_id, badge_id, tanggal_diperoleh
                             FROM gr_user_badges WHERE user_id = @uid";
        var list = new List<UserBadge>();
        try
        {
            using var conn = GetConnection();
            conn.Open();
            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("uid", userId);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var ub = new UserBadge(
                    reader.GetInt32(1),
                    reader.GetInt32(2),
                    reader.GetDateTime(3));
                ub.SetUserBadgeId(reader.GetInt32(0));
                list.Add(ub);
            }
        }
        catch { }
        return list;
    }

    public void AddUserBadge(UserBadge badge)
    {
        const string sql = @"INSERT INTO gr_user_badges (user_id, badge_id, tanggal_diperoleh)
                             VALUES (@uid, @bid, @tgl) RETURNING user_badge_id";
        try
        {
            using var conn = GetConnection();
            conn.Open();
            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("uid", badge.UserId);
            cmd.Parameters.AddWithValue("bid", badge.BadgeId);
            cmd.Parameters.AddWithValue("tgl", badge.TanggalDiperoleh);
            var newId = Convert.ToInt32(cmd.ExecuteScalar());
            badge.SetUserBadgeId(newId);
        }
        catch { }
    }
}
