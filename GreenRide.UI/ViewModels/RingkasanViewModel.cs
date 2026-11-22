using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GreenRide.Models;
using GreenRide.UI.Models;
using GreenRide.UI.Services;
using LiveChartsCore;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace GreenRide.UI.ViewModels;

public partial class RingkasanViewModel : PageViewModelBase
{
    private readonly IGreenRideRepository _repo;
    private readonly GamificationService _gami;
    private readonly AccountInfo _account;

    public string[] WeekdayNames { get; } = { "Sen", "Sel", "Rab", "Kam", "Jum", "Sab", "Min" };
    public ObservableCollection<CalendarDay> Days { get; } = new();
    public ObservableCollection<LogEntryItem> SelectedLogs { get; } = new();

    [ObservableProperty] private ISeries[] _series = Array.Empty<ISeries>();
    [ObservableProperty] private Axis[] _xAxes = Array.Empty<Axis>();
    [ObservableProperty] private Axis[] _yAxes = Array.Empty<Axis>();
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(WeekLabel))] private DateTime _weekStart;
    [ObservableProperty] private string _weekTotalText = "";
    [ObservableProperty] private string _weekBadgeText = "";

    [ObservableProperty] [NotifyPropertyChangedFor(nameof(MonthLabel))] private DateTime _month;
    [ObservableProperty] private DateTime _selectedDate;

    [ObservableProperty] private string _dayTitle = "";
    [ObservableProperty] private string _dayTotalText = "";
    [ObservableProperty] private bool _hasNoLogs;

    public string WeekLabel =>
        $"{WeekStart.ToString("d MMM", Fmt.Id)} – {WeekStart.AddDays(6).ToString("d MMM yyyy", Fmt.Id)}";
    public string MonthLabel => Month.ToString("MMMM yyyy", Fmt.Id);

    public RingkasanViewModel(IGreenRideRepository repo, GamificationService gami, AccountInfo account)
    {
        _repo    = repo;
        _gami    = gami;
        _account = account;

        var today  = DateTime.Today;
        _weekStart = GamificationService.StartOfWeek(today);
        _month     = new DateTime(today.Year, today.Month, 1);
        _selectedDate = today;
        Reload();
    }

    public override void OnNavigatedTo() => Reload();

    private void Reload()
    {
        BuildChart();
        BuildCalendar();
        LoadDay();
    }

    // ── Grafik ─────────────────────────────────────────────────────────────

    private void BuildChart()
    {
        var wsDate = DateOnly.FromDateTime(WeekStart);
        var logs   = _repo.GetTripLogs(_account.UserId, wsDate, wsDate.AddDays(6));

        var values = new double[7];
        var labels = new string[7];
        for (int i = 0; i < 7; i++)
        {
            var dOnly = DateOnly.FromDateTime(WeekStart.AddDays(i));
            values[i] = Math.Round(
                logs.Where(l => l.Tanggal == dOnly)
                    .Sum(l => (double)(l.EmisiKg ?? 0m)), 2);
            labels[i] = WeekStart.AddDays(i).ToString("ddd d", Fmt.Id);
        }

        var green = SKColor.Parse("#43A047");
        var dark  = SKColor.Parse("#1B5E20");

        Series = new ISeries[]
        {
            new ColumnSeries<double>
            {
                Name   = "kg CO₂",
                Values = values,
                Fill   = new SolidColorPaint(green),
                MaxBarWidth = 34, Rx = 6, Ry = 6,
                DataLabelsPaint     = new SolidColorPaint(dark),
                DataLabelsSize      = 12,
                DataLabelsPosition  = DataLabelsPosition.End,
                DataLabelsFormatter = p => p.Coordinate.PrimaryValue.ToString("0.0"),
            }
        };
        XAxes = new[] { new Axis { Labels = labels, TextSize = 12, LabelsPaint = new SolidColorPaint(dark) } };
        YAxes = new[]
        {
            new Axis
            {
                MinLimit = 0, TextSize = 12,
                LabelsPaint     = new SolidColorPaint(dark),
                SeparatorsPaint = new SolidColorPaint(SKColor.Parse("#C8E6C9")) { StrokeThickness = 1 },
            }
        };

        var total  = values.Sum();
        WeekTotalText = Fmt.Kg(total);
        WeekBadgeText = logs.Count == 0
            ? "Belum ada catatan di minggu ini"
            : _gami.TierUntuk(total) is { } t
                ? $"Memenuhi {t.Nama} ({t.Syarat})"
                : "Belum memenuhi syarat badge minggu ini";
    }

    [RelayCommand] private void PrevWeek() { WeekStart = WeekStart.AddDays(-7); BuildChart(); }
    [RelayCommand]
    private void NextWeek()
    {
        if (WeekStart >= GamificationService.StartOfWeek(DateTime.Today)) return;
        WeekStart = WeekStart.AddDays(7);
        BuildChart();
    }

    // ── Kalender ──────────────────────────────────────────────────────────

    private void BuildCalendar()
    {
        var gridStart     = GamificationService.StartOfWeek(Month);
        var gridEnd       = gridStart.AddDays(41);
        var gridStartDate = DateOnly.FromDateTime(gridStart);
        var gridEndDate   = DateOnly.FromDateTime(gridEnd);
        var logs          = _repo.GetTripLogs(_account.UserId, gridStartDate, gridEndDate);

        Days.Clear();
        for (int i = 0; i < 42; i++)
        {
            var d       = gridStart.AddDays(i);
            var dOnly   = DateOnly.FromDateTime(d);
            var dayLogs = logs.Where(l => l.Tanggal == dOnly).ToList();

            double? kg = dayLogs.Count == 0
                ? null
                : (double)dayLogs.Sum(l => l.EmisiKg ?? 0m);

            // Kolom emojis dikosongkan — front end yang handle tampilan moda di kalender
            Days.Add(new CalendarDay(d, d.Month == Month.Month, d == DateTime.Today, "", kg)
            {
                IsSelected = d.Date == SelectedDate.Date,
            });
        }
    }

    [RelayCommand] private void PrevMonth() { Month = Month.AddMonths(-1); BuildCalendar(); }
    [RelayCommand] private void NextMonth() { Month = Month.AddMonths(1);  BuildCalendar(); }

    [RelayCommand]
    private void SelectDay(CalendarDay? day)
    {
        if (day is null) return;
        SelectedDate = day.Date;
        foreach (var d in Days) d.IsSelected = d.Date == day.Date;
        LoadDay();
    }

    // ── Detail hari ───────────────────────────────────────────────────────

    private void LoadDay()
    {
        var map   = _gami.ModeMap();
        var dOnly = DateOnly.FromDateTime(SelectedDate);
        SelectedLogs.Clear();

        foreach (var l in _repo.GetTripLogs(_account.UserId, dOnly, dOnly))
        {
            map.TryGetValue(l.ModaId, out var m);
            SelectedLogs.Add(new LogEntryItem(l, m));
        }

        DayTitle     = SelectedDate.ToString("dddd, d MMMM yyyy", Fmt.Id);
        HasNoLogs    = SelectedLogs.Count == 0;
        DayTotalText = HasNoLogs
            ? "Belum ada perjalanan dicatat"
            : "Total " + Fmt.Kg(SelectedLogs.Sum(x => x.Emisi));
    }

    // ── Revisi (waktu mulai/selesai) — form dibuka oleh Shell ───────────────

    /// <summary>Diisi Shell: membuka halaman revisi untuk trip terpilih.</summary>
    public Action<LogEntryItem>? RequestRevisi { get; set; }

    [RelayCommand]
    private void MulaiRevisi(LogEntryItem? item)
    {
        if (item is null || !item.CanRevise) return;
        RequestRevisi?.Invoke(item);
    }
}
