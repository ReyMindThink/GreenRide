using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GreenRide.Models;
using GreenRide.UI.Models;
using GreenRide.UI.Services;

namespace GreenRide.UI.ViewModels;

public partial class CatatViewModel : PageViewModelBase
{
    private readonly IGreenRideRepository _repo;
    private readonly GamificationService _gami;
    private readonly AccountInfo _account;

    public ObservableCollection<TransportOption> Modes { get; }
    public ObservableCollection<LogEntryItem> RecentLogs { get; } = new();
    public DateTime MaxTanggal => DateTime.Today;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PreviewText))]
    [NotifyPropertyChangedFor(nameof(CompareText))]
    [NotifyCanExecuteChangedFor(nameof(SimpanCommand))]
    private TransportOption? _selectedMode;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PreviewText))]
    [NotifyPropertyChangedFor(nameof(CompareText))]
    [NotifyCanExecuteChangedFor(nameof(SimpanCommand))]
    private decimal? _durasiMenit = 30;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SimpanCommand))]
    private DateTime? _tanggal = DateTime.Today;

    [ObservableProperty] private string _statusMessage = "";

    public CatatViewModel(IGreenRideRepository repo, GamificationService gami, AccountInfo account)
    {
        _repo = repo;
        _gami = gami;
        _account = account;
        Modes = new ObservableCollection<TransportOption>(repo.GetTransportModes().Select(m => new TransportOption(m)));
        LoadRecent();
    }

    public override void OnNavigatedTo() => LoadRecent();

    public string PreviewText =>
        SelectedMode is null || DurasiMenit is not { } d || d <= 0
            ? "Pilih moda & durasi"
            : Fmt.Kg((double)d * SelectedMode.Mode.FaktorEmisi);

    public string CompareText
    {
        get
        {
            if (SelectedMode is null || DurasiMenit is not { } d || d <= 0) return "Estimasi emisi akan tampil di sini.";
            var hemat = (double)d * (ModeCatalog.FaktorAcuanMobil - SelectedMode.Mode.FaktorEmisi);
            return hemat > 0
                ? $"🌱 Hemat {Fmt.Kg(hemat)} dibanding naik mobil"
                : "Sama dengan emisi mobil. Coba moda lain untuk perjalanan berikutnya!";
        }
    }

    [RelayCommand]
    private void SetDurasi(string? menit)
    {
        if (decimal.TryParse(menit, out var v)) DurasiMenit = v;
    }

    private bool CanSimpan() => SelectedMode is not null && DurasiMenit is > 0 && Tanggal is not null;

    [RelayCommand(CanExecute = nameof(CanSimpan))]
    private void Simpan()
    {
        var mode = SelectedMode!.Mode;
        var log = new DailyLog(_account.UserId, Tanggal!.Value.Date, (int)DurasiMenit!.Value, mode.TransportModeId);
        _repo.AddLog(log);

        StatusMessage = $"✅ Tersimpan: {SelectedMode.Emoji} {SelectedMode.Nama}, {log.Durasi} menit ({Fmt.Kg(log.HitungEmisiHarian(mode))})";
        SelectedMode = null;
        DurasiMenit = 30;
        Tanggal = DateTime.Today;
        LoadRecent();
    }

    private void LoadRecent()
    {
        var map = _gami.ModeMap();
        RecentLogs.Clear();
        var logs = _repo.GetLogs(_account.UserId, DateTime.Today.AddDays(-14), DateTime.Today)
            .OrderByDescending(l => l.Tanggal).ThenByDescending(l => l.DailyLogId).Take(5);
        foreach (var l in logs)
            if (map.TryGetValue(l.TransportModeId, out var m)) RecentLogs.Add(new LogEntryItem(l, m));
    }
}
