using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GreenRide.UI.Helpers;
using GreenRide.UI.Models;
using GreenRide.UI.Services;

namespace GreenRide.UI.ViewModels;

/// <summary>Satu kartu moda (Motor / Mobil / Bus / Kereta).</summary>
public sealed partial class ModeCardItem : ObservableObject
{
    public ModeCardItem(TransportMode mode, ICommand command)
    {
        Mode = mode;
        Command = command;
        IconUri = AssetUri.Of(mode.IconUri);
    }

    public TransportMode Mode { get; }
    public ICommand Command { get; }
    public string IconUri { get; }
    public string Label => Mode.Label;
    public string FaktorLabel => Mode.FaktorLabel;

    /// <summary>Timer sedang berjalan dengan moda ini (kartu hijau).</summary>
    [ObservableProperty] private bool _isActive;

    /// <summary>Kartu bisa ditekan. False untuk moda lain selama ada timer aktif (PRD 5.2).</summary>
    [ObservableProperty] private bool _isEnabled = true;
}

/// <summary>Satu perisai di kartu Koleksi Badge.</summary>
public sealed record BadgeShelfItem(string Nama, string BatasLabel, string IconUri, bool IsEarned);

/// <summary>Layar Catat: badge minggu lalu, koleksi badge, timer, dan kartu moda.</summary>
public sealed partial class CatatViewModel : ViewModelBase, INavigationAware, IDisposable
{
    private const string IdleTimer = "00.00.00";
    private const string IdleCaption = "Pilih moda transportasi untuk mulai mencatat";

    private readonly ISessionService _session;
    private readonly ITripService _trips;
    private readonly IBadgeProgressService _badges;
    private readonly IClock _clock;
    private readonly DispatcherTimer _timer;

    private TripLog? _active;
    private bool _disposed;

    public CatatViewModel(
        ISessionService session,
        ITripService trips,
        IBadgeProgressService badges,
        IClock clock)
    {
        _session = session;
        _trips = trips;
        _badges = badges;
        _clock = clock;

        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += (_, _) => Refresh();

        foreach (var mode in ModeCatalog.All)
        {
            var id = mode.Id;
            Modes.Add(new ModeCardItem(mode, new AsyncRelayCommand(() => ToggleModeAsync(id))));
        }

        ShowLastWeek(null);
        ShowShelf(Array.Empty<WeeklyBadgeRecord>());
    }

    public string Greeting => $"HALO, {(_session.CurrentUser?.Nama ?? "").ToUpperInvariant()}!";

    public ObservableCollection<ModeCardItem> Modes { get; } = new();
    public ObservableCollection<BadgeShelfItem> Shelf { get; } = new();

    // ---- Kartu badge minggu lalu
    [ObservableProperty] private string _lastBadgeIcon = "";
    [ObservableProperty] private string _lastBadgeName = "";
    [ObservableProperty] private string _lastBadgeDescription = "";

    // ---- Kartu timer
    [ObservableProperty] private string _timerText = IdleTimer;
    [ObservableProperty] private string _captionText = IdleCaption;
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private bool _isBusy;

    public async Task OnNavigatedToAsync()
    {
        var user = _session.CurrentUser;
        if (user is null) return;

        // PRD 5.2: timer berbasis cloud. Bila ada log tanpa waktu_selesai, kartu moda otomatis menyala.
        // Dipisah dari badge agar pemulihan timer tidak ikut gagal bila layanan badge error.
        try
        {
            var active = await _trips.GetActiveTripAsync(user.Id);
            if (_disposed) return;
            ApplyActive(active);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Catat] {ex}");
            ErrorMessage = ex is ServiceException ? ex.Message : "Data tidak dapat dimuat. Coba lagi.";
        }

        try
        {
            var lastTask = _badges.GetLastWeekBadgeAsync(user.Id);
            var earnedTask = _badges.GetEarnedBadgesAsync(user.Id);
            await Task.WhenAll(lastTask, earnedTask);
            if (_disposed) return;

            ShowLastWeek(lastTask.Result);
            ShowShelf(earnedTask.Result);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Catat] badge: {ex}");
        }
    }

    // ------------------------------------------------------------ Timer & moda

    private async Task ToggleModeAsync(int modaId)
    {
        var user = _session.CurrentUser;
        if (user is null || IsBusy) return;

        IsBusy = true;
        ErrorMessage = null;
        try
        {
            if (_active is null)
            {
                var trip = await _trips.StartTripAsync(user.Id, modaId);
                if (_disposed) return;
                ApplyActive(trip);
            }
            else if (_active.ModaId == modaId)
            {
                var done = await _trips.StopTripAsync(user.Id);
                if (_disposed) return;
                ApplyActive(null);
                CaptionText = done.EmisiKg is { } kg
                    ? $"Perjalanan selesai. Kamu menghasilkan {Fmt.Number(kg, 4)} kg CO₂."
                    : "Perjalanan selesai.";
            }
            // moda lain saat timer berjalan: kartunya sudah disabled.
        }
        catch (ServiceException ex)
        {
            ErrorMessage = ex.Message;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Catat] {ex}");
            ErrorMessage = "Tidak dapat menyimpan perjalanan. Coba lagi.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ApplyActive(TripLog? trip)
    {
        _active = trip;

        foreach (var card in Modes)
        {
            card.IsActive = trip is not null && card.Mode.Id == trip.ModaId;
            card.IsEnabled = trip is null || card.IsActive;
        }

        if (trip is null)
        {
            _timer.Stop();
            TimerText = IdleTimer;
            CaptionText = IdleCaption;
        }
        else
        {
            Refresh();
            _timer.Start();
        }
    }

    /// <summary>Memperbarui tampilan timer dan emisi sementara.</summary>
    private void Refresh()
    {
        if (_active is null) return;

        var now = _clock.Now;
        TimerText = Fmt.Stopwatch(now - _active.WaktuMulai);

        var factor = ModeCatalog.Get(_active.ModaId).FaktorEmisi;
        var kg = EmissionCalculator.LiveKg(_active.WaktuMulai, now, factor);
        CaptionText = $"Kamu menghasilkan {Fmt.Number(kg, 4)} kg CO₂ dari perjalanan ini";
    }

    // ------------------------------------------------------------ Badge

    private void ShowLastWeek(WeeklyBadgeRecord? record)
    {
        var def = record is null ? null : BadgeCatalog.Get(record.Badge);
        if (record is not null && def is not null)
        {
            LastBadgeIcon = AssetUri.Of(def.IconUri);
            LastBadgeName = def.Nama;
            var limit = def.Level == BadgeLevel.ZeroTrace ? "0 kg CO₂" : $"≤ {Fmt.Number(def.BatasMingguKg, 0)} kg CO₂";
            LastBadgeDescription = $"Minggu lalu total emisi yang kamu hasilkan\n{limit}";
            return;
        }

        // Tanpa badge: perisai terkunci.
        LastBadgeIcon = AssetUri.Of(BadgeCatalog.All[0].LockedIconUri);
        LastBadgeName = "Belum ada badge";
        LastBadgeDescription = record is null
            ? "Belum ada data untuk minggu lalu"
            : $"Minggu lalu total emisimu {Fmt.Number(record.TotalEmisiKg, 2)} kg CO₂";
    }

    private void ShowShelf(IReadOnlyList<WeeklyBadgeRecord> earned)
    {
        var levels = earned.Where(r => r.Badge != BadgeLevel.None).Select(r => r.Badge).ToHashSet();

        Shelf.Clear();
        foreach (var def in BadgeCatalog.All)
        {
            var isEarned = levels.Contains(def.Level);
            Shelf.Add(new BadgeShelfItem(
                def.Nama,
                def.BatasLabel,
                AssetUri.Of(isEarned ? def.IconUri : def.LockedIconUri),
                isEarned));
        }
    }

    public void Dispose()
    {
        _disposed = true;
        _timer.Stop();
    }
}
