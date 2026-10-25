using GreenRide.Models;
using GreenRide.UI.Models;

namespace GreenRide.UI.Services;

/// <summary>
/// Batas antara UI dan penyimpanan data. Saat ini diimplementasi in-memory;
/// nanti cukup buat PostgresGreenRideRepository / MongoGreenRideRepository dan ganti di App.axaml.cs.
/// </summary>
public interface IGreenRideRepository
{
    // Akun
    AccountInfo? Authenticate(string email, string password);
    AccountInfo? Register(string nama, string email, string password); // null jika email sudah dipakai

    // Moda transportasi
    IReadOnlyList<TransportMode> GetTransportModes();

    // Log perjalanan (rentang tanggal inklusif)
    IReadOnlyList<DailyLog> GetLogs(int userId, DateTime from, DateTime to);
    DailyLog AddLog(DailyLog log);
    void UpdateLog(DailyLog log);

    // Badge milik user (baris ada = badge diraih)
    IReadOnlyList<UserBadge> GetUserBadges(int userId);
    void AddUserBadge(UserBadge badge);
}
