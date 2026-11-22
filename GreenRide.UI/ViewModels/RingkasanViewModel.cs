using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GreenRide.UI.Helpers;
using GreenRide.UI.Models;
using GreenRide.UI.Services;

namespace GreenRide.UI.ViewModels;

/// <summary>Satu batang pada grafik emisi mingguan.</summary>
public sealed record WeekBarItem(string Label, string ValueText, double BarHeight, bool Started, bool IsCurrent);

/// <summary>Satu sel kalender. Sel kosong (Date = null) hanya mengisi posisi hari pertama bulan.</summary>
public sealed partial class CalendarCellItem : ObservableObject
{
    public CalendarCellItem(DateOnly? date, CalendarLevel level, bool isToday, ICommand? command)
    {
        Date = date;
        Level = level;
        IsToday = isToday;
        Command = command;
    }

    public DateOnly? Date { get; }
    public CalendarLevel Level { get; }
    public bool IsToday { get; }
    public ICommand? Command { get; }

    public string Text => Date is { } d ? d.Day.ToString() : "";
    public bool IsBlank => Date is null;
    public bool IsNotBlank => Date is not null;
    public bool IsGood => Level == CalendarLevel.Good;
    public bool IsAverage => Level == CalendarLevel.Average;
    public bool IsHigh => Level == CalendarLevel.High;
    public bool IsNone => Level == CalendarLevel.None;

    [ObservableProperty] private bool _isSelected;
}

/// <summary>Satu baris di tabel "Catatan perjalanan".</summary>
public sealed class TripRowItem
{
    public TripRowItem(TripLog log, bool canRevise, ICommand reviseCommand)
    {
        var mode = ModeCatalog.Get(log.ModaId);
        ModeName = mode.Nama;
        IconUri = AssetUri.Of(mode.IconUri);
        TimeText = log.WaktuSelesai is { } selesai
            ? Fmt.TimeRange(log.WaktuMulai, selesai)
            : Fmt.Clock(log.WaktuMulai) + "–…";
        DurasiText = log.DurasiMenit is { } menit ? Fmt.Minutes(menit) : "—";
        EmisiText = log.EmisiKg is { } kg ? Fmt.Kg(kg, 4) : "—";
        StatusText = log.IsRunning ? "Berjalan" : log.IsRevised ? "Direvisi" : "Tercatat";
        CanRevise = canRevise && !log.IsRunning;
        ReviseCommand = reviseCommand;
    }

    public string ModeName { get; }
    public string IconUri { get; }
    public string TimeText { get; }
    public string DurasiText { get; }
    public string EmisiText { get; }
    public string StatusText { get; }
    public bool CanRevise { get; }
    public ICommand ReviseCommand { get; }

    /// <summary>Baris kedua pada tampilan sempit.</summary>
    public string DetailText => $"{TimeText} · {DurasiText} · {StatusText}";
}

/// <summary>Layar Ringkasan: grafik emisi mingguan, kalender emisi, dan catatan perjalanan tanggal terpilih.</summary>
public sealed partial class RingkasanViewModel : ViewModelBase, INavigationAware, IDisposable
{
    /// <summary>Tinggi area batang (px). Harus sama dengan area garis bantu di RingkasanView.axaml.</summary>
    private const double PlotHeight = 160;

    private readonly ISessionService _session;
    private readonly ITripService _trips;
    private readonly IEmissionService _emission;
    private readonly DateOnly _today;

    private int _year;
    private int _month;
    private DateOnly _selected;
    private bool _disposed;

    public RingkasanViewModel(ISessionService session, ITripService trips, IEmissionService emission, IClock clock)
    {
        _session = session;
        _trips = trips;
        _emission = emission;

        _today = clock.Today;
        _year = _today.Year;
        _month = _today.Month;
        _selected = _today;
        MonthLabel = Fmt.MonthYear(_year, _month);
        MonthName = Fmt.MonthName(_month);
    }

    /// <summary>Diisi Shell: dipanggil saat tombol Revisi pada sebuah perjalanan ditekan.</summary>
    public Action<TripLog>? OnRevise { get; set; }

    public ObservableCollection<WeekBarItem> Bars { get; } = new();
    public ObservableCollection<CalendarCellItem> Cells { get; } = new();
    public ObservableCollection<TripRowItem> Rows { get; } = new();

    // ---- Header
    [ObservableProperty] private string _monthLabel = "";
    [ObservableProperty] private string _monthName = "";
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(NextMonthCommand))]
    private bool _canGoNext;

    // ---- Kartu Emisi mingguan
    [ObservableProperty] private string _monthTotalText = "";
    [ObservableProperty] private string _monthTotalCaption = "";
    [ObservableProperty] private string _compareTitle = "";
    [ObservableProperty] private string _compareSubtitle = "";
    [ObservableProperty] private bool _compareIsHigher;
    [ObservableProperty] private string _axis0 = "0";
    [ObservableProperty] private string _axis1 = "2";
    [ObservableProperty] private string _axis2 = "4";
    [ObservableProperty] private string _axis3 = "6";
    [ObservableProperty] private string _axis4 = "8";

    // ---- Kartu Kalender
    [ObservableProperty] private string _selectedDayLabel = "";
    [ObservableProperty] private string _selectedDayTotal = "";

    // ---- Kartu Catatan perjalanan
    [ObservableProperty] private string _tripsTitle = "Catatan perjalanan hari ini";
    [ObservableProperty] private string _tripsSubtitle = "";
    [ObservableProperty] private bool _hasTrips;
    [ObservableProperty] private bool _showPastNote;
    [ObservableProperty] private string? _errorMessage;

    public async Task OnNavigatedToAsync() => await LoadMonthAsync();

    // ------------------------------------------------------------ Navigasi bulan

    [RelayCommand]
    private Task PreviousMonthAsync() => ChangeMonthAsync(-1);

    [RelayCommand(CanExecute = nameof(CanGoNext))]
    private Task NextMonthAsync() => ChangeMonthAsync(1);

    private async Task ChangeMonthAsync(int delta)
    {
        var first = new DateOnly(_year, _month, 1).AddMonths(delta);
        _year = first.Year;
        _month = first.Month;
        _selected = default; // dipilih ulang di LoadMonthAsync
        await LoadMonthAsync();
    }

    // ------------------------------------------------------------ Memuat data

    private async Task LoadMonthAsync()
    {
        var user = _session.CurrentUser;
        if (user is null) return;

        ErrorMessage = null;
        try
        {
            var daysTask = _emission.GetMonthAsync(user.Id, _year, _month);
            var weeksTask = _emission.GetWeeklyAsync(user.Id, _year, _month);
            await Task.WhenAll(daysTask, weeksTask);
            if (_disposed) return;

            var days = daysTask.Result;
            var weeks = weeksTask.Result;
            var isCurrentMonth = _year == _today.Year && _month == _today.Month;

            MonthLabel = Fmt.MonthYear(_year, _month);
            MonthName = Fmt.MonthName(_month);
            CanGoNext = new DateOnly(_year, _month, 1) < new DateOnly(_today.Year, _today.Month, 1);

            if (_selected == default || _selected.Year != _year || _selected.Month != _month)
            {
                _selected = isCurrentMonth
                    ? _today
                    : days.LastOrDefault(d => d.TotalEmisiKg.HasValue)?.Tanggal ?? new DateOnly(_year, _month, 1);
            }

            BuildCalendar(days);
            BuildChart(weeks);

            var total = days.Sum(d => d.TotalEmisiKg ?? 0m);
            MonthTotalText = Fmt.Co2(total);
            MonthTotalCaption = isCurrentMonth
                ? $"Total bulan ini · hingga {Fmt.DayMonth(_today)}"
                : $"Total bulan {Fmt.MonthName(_month)}";

            await BuildCompareAsync(user.Id, weeks);
            await LoadDayAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Ringkasan] {ex}");
            ErrorMessage = ex is ServiceException ? ex.Message : "Data tidak dapat dimuat. Coba lagi.";
        }
    }

    private void BuildCalendar(IReadOnlyList<CalendarDayEmission> days)
    {
        Cells.Clear();
        if (days.Count == 0) return;

        // Senin = kolom pertama
        var offset = ((int)days[0].Tanggal.DayOfWeek + 6) % 7;
        for (var i = 0; i < offset; i++)
            Cells.Add(new CalendarCellItem(null, CalendarLevel.None, false, null));

        foreach (var d in days)
        {
            var date = d.Tanggal;
            Cells.Add(new CalendarCellItem(
                date,
                CalendarRules.LevelFor(d.TotalEmisiKg),
                date == _today,
                new AsyncRelayCommand(() => SelectDayAsync(date)))
            {
                IsSelected = date == _selected,
            });
        }
    }

    private void BuildChart(IReadOnlyList<WeeklyEmissionPoint> weeks)
    {
        var max = weeks.Where(w => w.Started).Select(w => w.TotalEmisiKg).DefaultIfEmpty(0m).Max();
        var axisMax = Math.Max(8m, Math.Ceiling(max / 4m) * 4m);
        var step = axisMax / 4m;

        Axis0 = "0";
        Axis1 = Fmt.Number(step, 0);
        Axis2 = Fmt.Number(step * 2, 0);
        Axis3 = Fmt.Number(step * 3, 0);
        Axis4 = Fmt.Number(axisMax, 0);

        var currentMonday = WeekHelper.MondayOf(_today);
        Bars.Clear();
        foreach (var w in weeks)
        {
            var height = w.Started ? Math.Max(3d, (double)(w.TotalEmisiKg / axisMax) * PlotHeight) : 0d;
            Bars.Add(new WeekBarItem(
                Fmt.DayMonthShort(w.MingguMulai),
                w.Started ? Fmt.Number(w.TotalEmisiKg, 2) : "Belum mulai",
                height,
                w.Started,
                w.MingguMulai == currentMonday));
        }
    }

    private async Task BuildCompareAsync(Guid userId, IReadOnlyList<WeeklyEmissionPoint> weeks)
    {
        CompareIsHigher = false;

        var started = weeks.Where(w => w.Started).ToList();
        if (started.Count == 0)
        {
            CompareTitle = "Belum ada data";
            CompareSubtitle = "Belum ada minggu yang dimulai";
            return;
        }

        var current = started[^1];
        var previous = await WeekTotalAsync(userId, current.MingguMulai.AddDays(-7), weeks);
        if (_disposed) return;

        CompareSubtitle = current.MingguMulai == WeekHelper.MondayOf(_today)
            ? "Minggu ini vs minggu lalu"
            : $"Minggu {Fmt.DayMonthShort(current.MingguMulai)} vs minggu sebelumnya";

        if (previous <= 0m)
        {
            CompareTitle = "Belum ada pembanding";
            return;
        }

        var percent = (int)Math.Round(
            Math.Abs(current.TotalEmisiKg - previous) / previous * 100m, 0, MidpointRounding.AwayFromZero);

        if (current.TotalEmisiKg < previous)
        {
            CompareTitle = $"{percent}% lebih rendah";
        }
        else if (current.TotalEmisiKg > previous)
        {
            CompareTitle = $"{percent}% lebih tinggi";
            CompareIsHigher = true;
        }
        else
        {
            CompareTitle = "Sama dengan minggu sebelumnya";
        }
    }

    /// <summary>Total sebuah minggu; bila minggunya ada di bulan lain, datanya diminta terpisah.</summary>
    private async Task<decimal> WeekTotalAsync(Guid userId, DateOnly monday, IReadOnlyList<WeeklyEmissionPoint> loaded)
    {
        var hit = loaded.FirstOrDefault(p => p.MingguMulai == monday);
        if (hit is not null) return hit.TotalEmisiKg;

        var points = await _emission.GetWeeklyAsync(userId, monday.Year, monday.Month);
        return points.FirstOrDefault(p => p.MingguMulai == monday)?.TotalEmisiKg ?? 0m;
    }

    // ------------------------------------------------------------ Tanggal terpilih

    private async Task SelectDayAsync(DateOnly date)
    {
        _selected = date;
        foreach (var cell in Cells)
            cell.IsSelected = cell.Date == date;

        try
        {
            await LoadDayAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Ringkasan] {ex}");
            ErrorMessage = ex is ServiceException ? ex.Message : "Data tidak dapat dimuat. Coba lagi.";
        }
    }

    private async Task LoadDayAsync()
    {
        var user = _session.CurrentUser;
        if (user is null) return;

        var date = _selected;
        var logs = await _trips.GetLogsAsync(user.Id, date);
        if (_disposed || date != _selected) return;

        var isToday = date == _today;
        Rows.Clear();
        foreach (var log in logs)
        {
            var captured = log;
            Rows.Add(new TripRowItem(log, isToday, new RelayCommand(() => OnRevise?.Invoke(captured))));
        }

        var total = logs.Where(l => l.EmisiKg.HasValue).Sum(l => l.EmisiKg!.Value);

        SelectedDayLabel = isToday ? $"{Fmt.DayMonthShort(date)} · Hari ini" : Fmt.DayMonthShort(date);
        SelectedDayTotal = logs.Count == 0 ? "Tanpa catatan" : Fmt.Co2(total);

        TripsTitle = isToday ? "Catatan perjalanan hari ini" : "Catatan perjalanan";
        TripsSubtitle = logs.Count == 0
            ? Fmt.DateLong(date)
            : $"{Fmt.DateLong(date)} · Total {Fmt.Co2(total)}";
        HasTrips = logs.Count > 0;
        ShowPastNote = !isToday && logs.Count > 0;
    }

    public void Dispose() => _disposed = true;
}
