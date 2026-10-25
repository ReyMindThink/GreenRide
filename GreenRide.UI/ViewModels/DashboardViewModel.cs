using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GreenRide.UI.Models;
using GreenRide.UI.Services;

namespace GreenRide.UI.ViewModels;

public partial class DashboardViewModel : PageViewModelBase
{
    private readonly IGreenRideRepository _repo;
    private readonly GamificationService _gami;
    private readonly AccountInfo _account;

    public ObservableCollection<BadgeItem> Badges { get; } = new();
    public ObservableCollection<LogEntryItem> TodayLogs { get; } = new();
    public Action? RequestCatat { get; set; }

    [ObservableProperty] private string _greeting = "";
    [ObservableProperty] private BadgeItem? _currentBadge;
    [ObservableProperty] private int _totalPoin;
    [ObservableProperty] private string _weekEmissionText = "0,00 kg";
    [ObservableProperty] private string _avoidedText = "0,0 kg";
    [ObservableProperty] private string _progressText = "";
    [ObservableProperty] private bool _hasTodayLogs;

    public DashboardViewModel(IGreenRideRepository repo, GamificationService gami, AccountInfo account)
    {
        _repo = repo;
        _gami = gami;
        _account = account;
        Refresh();
    }

    public override void OnNavigatedTo() => Refresh();

    [RelayCommand] private void CatatSekarang() => RequestCatat?.Invoke();

    public void Refresh()
    {
        var uid = _account.UserId;
        _gami.RefreshBadges(uid);

        Greeting = $"Halo, {_account.Nama} 👋";
        TotalPoin = _gami.TotalPoin(uid);
        AvoidedText = _gami.EmisiDihindari(uid).ToString("0.0", Fmt.Id) + " kg";

        // Badge: IsEarned dari keberadaan baris UserBadge → tampil abu-abu bila belum diraih
        var earned = _gami.BadgeDiraih(uid);
        Badges.Clear();
        foreach (var def in BadgeRules.All)
            Badges.Add(new BadgeItem(def, earned.Contains(def.BadgeId)));

        CurrentBadge = BadgeRules.All
            .Where(d => earned.Contains(d.BadgeId))
            .OrderBy(d => d.BatasKg)
            .Select(d => new BadgeItem(d, true))
            .FirstOrDefault()
            ?? new BadgeItem(BadgeRules.All[0], false);

        // Progres minggu berjalan
        var ws = GamificationService.StartOfWeek(DateTime.Today);
        var weekLogs = _repo.GetLogs(uid, ws, ws.AddDays(6));
        var kg = _gami.Emisi(weekLogs);
        WeekEmissionText = kg.ToString("0.00", Fmt.Id) + " kg";

        if (weekLogs.Count == 0)
            ProgressText = "Belum ada perjalanan minggu ini. Yuk catat perjalanan pertamamu!";
        else if (_gami.TierUntuk(kg) is { } tier)
            ProgressText = $"Minggu ini {Fmt.Kg(kg)} — on track meraih {tier.Nama} (sisa batas {(tier.BatasKg - kg).ToString("0.00", Fmt.Id)} kg).";
        else
            ProgressText = $"Minggu ini {Fmt.Kg(kg)} sudah melewati batas 10 kg. Tetap semangat, minggu depan mulai baru 💪";

        // Perjalanan hari ini
        var map = _gami.ModeMap();
        TodayLogs.Clear();
        foreach (var l in _repo.GetLogs(uid, DateTime.Today, DateTime.Today))
            if (map.TryGetValue(l.TransportModeId, out var m)) TodayLogs.Add(new LogEntryItem(l, m));
        HasTodayLogs = TodayLogs.Count > 0;
    }
}
