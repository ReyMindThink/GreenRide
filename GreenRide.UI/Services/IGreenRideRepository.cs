using GreenRide.Models;
using GreenRide.UI.Models;

namespace GreenRide.UI.Services;

public interface IGreenRideRepository
{
    AccountInfo? Authenticate(string email, string password);
    AccountInfo? Register(string nama, string email, string password);
    IReadOnlyList<TransportMode> GetTransportModes();
    TripLog StartTrip(Guid userId, int modaId);
    TripLog? GetActiveTrip(Guid userId);
    TripLog CompleteTrip(Guid tripId, decimal faktorEmisi);
    IReadOnlyList<TripLog> GetTripLogs(Guid userId, DateOnly from, DateOnly to);
    void UpdateTripLog(Guid tripId, int modaIdBaru, decimal faktorEmisiBaru);
    WeeklyBadge? GetPreviousWeekBadge(Guid userId);
    IReadOnlyList<WeeklyBadge> GetAllWeeklyBadges(Guid userId);
    void AddWeeklyBadge(WeeklyBadge badge);
    Dictionary<DateOnly, decimal> GetCalendarEmission(Guid userId, int year, int month);
    IReadOnlyList<LeaderboardEntry> GetLeaderboard(DateOnly weekStart);
}
