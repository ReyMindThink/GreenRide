using System.ComponentModel;
using GreenRide.UI.Models;
using GreenRide.UI.ViewModels;

namespace GreenRide.UI.Services;

/// <summary>Kesalahan yang pesannya aman ditampilkan langsung ke pengguna.</summary>
public sealed class ServiceException : Exception
{
    public ServiceException(string message) : base(message) { }
}

/// <summary>Sumber waktu. Semua tanggal/waktu dihitung di sisi aplikasi dengan waktu lokal.</summary>
public interface IClock
{
    DateTime Now { get; }
    DateOnly Today { get; }
}

public interface ISessionService
{
    UserAccount? CurrentUser { get; }
    bool IsLoggedIn { get; }
    event Action? Changed;
    void SignIn(UserAccount user);
    void SignOut();
}

/// <summary>ViewModel yang ingin dijalankan sesuatu begitu layarnya tampil.</summary>
public interface INavigationAware
{
    Task OnNavigatedToAsync();
}

public interface INavigationService : INotifyPropertyChanged
{
    ViewModelBase? Current { get; }
    void NavigateTo<TViewModel>(Action<TViewModel>? init = null) where TViewModel : ViewModelBase;
}

// ---------- Data (PRD bagian 5) ----------

public interface IAuthService
{
    Task<AuthResult> LoginAsync(string email, string password);
    Task<AuthResult> RegisterAsync(string nama, string email, string password);
}

public interface ITripService
{
    /// <summary>Log dengan waktu_selesai IS NULL milik user (maksimal satu).</summary>
    Task<TripLog?> GetActiveTripAsync(Guid userId);
    Task<TripLog> StartTripAsync(Guid userId, int modaId);
    Task<TripLog> StopTripAsync(Guid userId);
    Task<IReadOnlyList<TripLog>> GetLogsAsync(Guid userId, DateOnly tanggal);
    /// <summary>Hanya log hari ini (PRD 5.4). Melempar <see cref="ServiceException"/> bila tidak valid.</summary>
    Task<TripLog> ReviseTripAsync(Guid userId, Guid logId, int modaId, DateTime waktuMulai, DateTime waktuSelesai);
}

public interface IEmissionService
{
    /// <summary>Satu entri untuk setiap tanggal dalam bulan (TotalEmisiKg null bila tanpa log).</summary>
    Task<IReadOnlyList<CalendarDayEmission>> GetMonthAsync(Guid userId, int year, int month);
    /// <summary>Satu batang untuk setiap hari Senin dalam bulan tersebut.</summary>
    Task<IReadOnlyList<WeeklyEmissionPoint>> GetWeeklyAsync(Guid userId, int year, int month);
}

public interface IBadgeProgressService
{
    /// <summary>Menghitung dan menyimpan badge untuk minggu-minggu lampau yang belum tercatat (dipanggil saat login).</summary>
    Task SettleCompletedWeeksAsync(Guid userId);
    Task<WeeklyBadgeRecord?> GetLastWeekBadgeAsync(Guid userId);
    /// <summary>Semua badge yang pernah didapat, terbaru dulu.</summary>
    Task<IReadOnlyList<WeeklyBadgeRecord>> GetEarnedBadgesAsync(Guid userId);
}

public interface ILeaderboardService
{
    /// <summary>Seluruh peringkat minggu berjalan (emisi terendah = peringkat 1). VM yang memotong top 10.</summary>
    Task<IReadOnlyList<LeaderboardEntry>> GetCurrentWeekAsync(Guid userId);
}
