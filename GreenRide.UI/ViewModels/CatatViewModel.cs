using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GreenRide.Models;
using GreenRide.UI.Models;
using GreenRide.UI.Services;

namespace GreenRide.UI.ViewModels;

/// <summary>
/// Halaman pencatatan perjalanan — alur "Cloud Timer":
///   • Tidak ada trip aktif: ketuk satu moda → StartTrip.
///   • Ada trip aktif: ketuk moda yang sama → CompleteTrip (durasi &amp; emisi dihitung di SQL).
///   • Ada trip aktif: moda lain dinonaktifkan.
/// Trip aktif dibaca dari database saat inisialisasi, sehingga timer tetap jalan
/// meski aplikasi ditutup.
/// </summary>
public partial class CatatViewModel : PageViewModelBase
{
    private readonly IGreenRideRepository _repo;
    private readonly AccountInfo _account;
    private readonly IReadOnlyList<TransportMode> _transportModes;

    public ObservableCollection<TransportOption> Modes { get; }
    public ObservableCollection<LogEntryItem> RecentLogs { get; } = new();

    /// <summary>Trip yang sedang berjalan (waktu_selesai masih NULL).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasActiveTrip))]
    [NotifyPropertyChangedFor(nameof(PreviewText))]
    [NotifyPropertyChangedFor(nameof(CompareText))]
    private TripLog? _activeTrip;

    /// <summary>Moda dari trip aktif.</summary>
    [ObservableProperty] private int? _activeModaId;

    [ObservableProperty] private string _statusMessage = "";

    public bool HasActiveTrip => ActiveTrip is not null;

    public string PreviewText => ActiveTrip is null
        ? "Belum ada perjalanan"
        : $"{NamaModa(ActiveTrip.ModaId)} · mulai {ActiveTrip.WaktuMulai.ToLocalTime().ToString("HH:mm")}";

    public string CompareText => ActiveTrip is null
        ? "Ketuk satu moda untuk memulai perjalanan."
        : "Perjalanan berlangsung. Ketuk moda yang sama untuk menyelesaikan.";

    public CatatViewModel(IGreenRideRepository repo, AccountInfo account)
    {
        _repo           = repo;
        _account        = account;
        _transportModes = repo.GetTransportModes();
        Modes = new ObservableCollection<TransportOption>(
            _transportModes.Select(m => new TransportOption(m)));

        LoadActiveTrip();
        LoadRecent();
    }

    public override void OnNavigatedTo()
    {
        LoadActiveTrip();
        LoadRecent();
    }

    private string NamaModa(int modaId) =>
        _transportModes.FirstOrDefault(m => m.TransportModeId == modaId)?.NamaModa ?? "";

    private void LoadActiveTrip()
    {
        try
        {
            SetActiveTrip(_repo.GetActiveTrip(_account.UserId));
        }
        catch (Exception ex)
        {
            StatusMessage = "Gagal cek perjalanan aktif: " + ex.Message;
        }
    }

    private void SetActiveTrip(TripLog? trip)
    {
        ActiveTrip   = trip;
        ActiveModaId = trip?.ModaId;
        foreach (var o in Modes)
        {
            o.IsActive   = ActiveModaId is not null && o.ModaId == ActiveModaId;
            o.IsDisabled = ActiveTrip is not null && o.ModaId != ActiveModaId;
        }
    }

    [RelayCommand]
    private void PilihModa(int modaId)
    {
        try
        {
            if (ActiveTrip is null)
            {
                var trip = _repo.StartTrip(_account.UserId, modaId);
                SetActiveTrip(trip);
                StatusMessage = $"Perjalanan dimulai dengan {NamaModa(modaId)}.";
            }
            else if (modaId == ActiveTrip.ModaId)
            {
                var faktor = _transportModes.First(m => m.TransportModeId == ActiveTrip.ModaId).FaktorEmisi;
                var done   = _repo.CompleteTrip(ActiveTrip.Id, (decimal)faktor);
                SetActiveTrip(null);
                StatusMessage = $"Perjalanan selesai: {Fmt.Kg(done.EmisiKg ?? 0m)}.";
                LoadRecent();
            }
            // modaId lain saat trip aktif → diabaikan (kartu dinonaktifkan di UI)
        }
        catch (Exception ex)
        {
            StatusMessage = "Gagal memproses perjalanan: " + ex.Message;
        }
    }

    private void LoadRecent()
    {
        RecentLogs.Clear();
        var today = DateOnly.FromDateTime(DateTime.Today);
        var from  = today.AddDays(-14);
        try
        {
            var map  = _transportModes.ToDictionary(m => m.TransportModeId);
            var logs = _repo.GetTripLogs(_account.UserId, from, today)
                .Where(l => l.WaktuSelesai.HasValue)
                .OrderByDescending(l => l.Tanggal)
                .ThenByDescending(l => l.WaktuMulai)
                .Take(5);
            foreach (var l in logs)
            {
                map.TryGetValue(l.ModaId, out var m);
                RecentLogs.Add(new LogEntryItem(l, m));
            }
        }
        catch (Exception ex)
        {
            StatusMessage = "Gagal memuat riwayat: " + ex.Message;
        }
    }
}
