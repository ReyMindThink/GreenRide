using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GreenRide.UI.Helpers;
using GreenRide.UI.Models;
using GreenRide.UI.Services;

namespace GreenRide.UI.ViewModels;

/// <summary>Satu pilihan moda pada form Revisi.</summary>
public sealed partial class ModeChoiceItem : ObservableObject
{
    public ModeChoiceItem(TransportMode mode, ICommand command)
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

    [ObservableProperty] private bool _isSelected;
}

/// <summary>
/// Form Revisi Perjalanan (PRD 5.4): moda, waktu mulai, dan waktu selesai sebuah log hari ini.
/// Panel kanan menampilkan pratinjau emisi sebelum dan sesudah revisi, dihitung ulang setiap input berubah.
/// </summary>
public sealed partial class RevisiViewModel : ViewModelBase, INavigationAware
{
    private readonly ISessionService _session;
    private readonly ITripService _trips;
    private readonly IClock _clock;

    // Salinan nilai awal log (objek aslinya bisa berubah setelah disimpan).
    private bool _initialized;
    private Guid _logId;
    private DateOnly _tanggal;
    private DateTime _origStart;
    private DateTime? _origEnd;
    private decimal _origKg;
    private decimal _otherTotal;
    private Action _onClose = () => { };

    public RevisiViewModel(ISessionService session, ITripService trips, IClock clock)
    {
        _session = session;
        _trips = trips;
        _clock = clock;
    }

    public ObservableCollection<ModeChoiceItem> Modes { get; } = new();

    // ---- Panel kiri
    [ObservableProperty] private string _subtitle = "";
    [ObservableProperty] private string _dateText = "";
    [ObservableProperty] private int _selectedModeId;
    [ObservableProperty] private string _startText = "";
    [ObservableProperty] private string _endText = "";
    [ObservableProperty] private string? _startError;
    [ObservableProperty] private string? _endError;
    [ObservableProperty] private string? _formError;

    // ---- Panel kanan (pratinjau)
    [ObservableProperty] private string _oldSummary = "";
    [ObservableProperty] private string _oldDetail = "";
    [ObservableProperty] private string _newHeadline = "—";
    [ObservableProperty] private string _newDetail = "";
    [ObservableProperty] private string _deltaText = "";
    [ObservableProperty] private string _formulaText = "";
    [ObservableProperty] private string _newDayTotal = "—";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private bool _canSave;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    [NotifyPropertyChangedFor(nameof(SaveText))]
    private bool _isBusy;

    public string SaveText => IsBusy ? "Menyimpan..." : "Simpan revisi";

    /// <summary>Dipanggil Shell sebelum layar ditampilkan.</summary>
    public void Initialize(TripLog log, Action onClose)
    {
        _logId = log.Id;
        _tanggal = log.Tanggal;
        _origStart = log.WaktuMulai;
        _origEnd = log.WaktuSelesai;
        _origKg = log.EmisiKg ?? 0m;
        _onClose = onClose;
        _initialized = true;

        var mode = ModeCatalog.Get(log.ModaId);
        var oldRange = log.WaktuSelesai is { } end ? Fmt.TimeRange(log.WaktuMulai, end) : Fmt.Clock(log.WaktuMulai);

        Subtitle = $"Perjalanan {mode.Label} · sebelumnya {oldRange}";
        DateText = $"{Fmt.DateWithDay(log.Tanggal)} · Hari ini";
        OldSummary = $"{(log.DurasiMenit is { } m ? Fmt.Minutes(m) : "—")} · {Fmt.Co2(_origKg, 4)}";
        OldDetail = $"{mode.Label} · {oldRange}";

        Modes.Clear();
        foreach (var option in ModeCatalog.All)
        {
            var id = option.Id;
            Modes.Add(new ModeChoiceItem(option, new RelayCommand(() => SelectMode(id))) { IsSelected = id == log.ModaId });
        }

        SelectedModeId = log.ModaId;
        StartText = Fmt.Clock(log.WaktuMulai);
        EndText = Fmt.Clock(log.WaktuSelesai ?? _clock.Now);
        Recompute();
    }

    /// <summary>Total log lain hari ini, untuk "Total hari ini jika disimpan".</summary>
    public async Task OnNavigatedToAsync()
    {
        var user = _session.CurrentUser;
        if (user is null || !_initialized) return;

        var logs = await _trips.GetLogsAsync(user.Id, _tanggal);
        _otherTotal = logs.Where(l => l.Id != _logId && l.EmisiKg.HasValue).Sum(l => l.EmisiKg!.Value);
        Recompute();
    }

    private void SelectMode(int id)
    {
        SelectedModeId = id;
        foreach (var m in Modes) m.IsSelected = m.Mode.Id == id;
    }

    partial void OnSelectedModeIdChanged(int value) => Recompute();
    partial void OnStartTextChanged(string value) => Recompute();
    partial void OnEndTextChanged(string value) => Recompute();

    // ------------------------------------------------------------ Validasi & pratinjau

    /// <summary>Mengubah teks jam menjadi waktu pada tanggal log. Bila jamnya tidak diubah, nilai asli (dengan detik) dipertahankan.</summary>
    private DateTime? Resolve(string text, DateTime? original)
    {
        if (!TimeInput.TryParse(text, out var time)) return null;
        var candidate = _tanggal.ToDateTime(time);
        return original is { } o && Fmt.Clock(o) == Fmt.Clock(candidate) ? o : candidate;
    }

    private void Recompute()
    {
        if (!_initialized) return;

        FormError = null;
        StartError = null;
        EndError = null;

        var start = Resolve(StartText, _origStart);
        var end = Resolve(EndText, _origEnd);

        if (start is null) StartError = "Format waktu HH.MM, mis. 08.00";
        if (end is null) EndError = "Format waktu HH.MM, mis. 08.15";

        if (start is { } s && end is { } e)
        {
            if (e <= s) EndError = "Waktu selesai harus setelah waktu mulai.";
            else if (e > _clock.Now) EndError = "Waktu selesai tidak boleh di masa depan.";
        }

        if (StartError is null && EndError is null && start is { } st && end is { } en)
        {
            var mode = ModeCatalog.Get(SelectedModeId);
            var (menit, kg) = EmissionCalculator.Calculate(st, en, mode.FaktorEmisi);
            var delta = kg - _origKg;

            NewHeadline = Fmt.Co2(kg, 4);
            NewDetail = $"{Fmt.Minutes(menit)} · {mode.Label}";
            DeltaText = delta > 0 ? $"Bertambah {Fmt.Co2(delta, 4)}"
                      : delta < 0 ? $"Berkurang {Fmt.Co2(-delta, 4)}"
                      : "Tidak berubah";
            FormulaText = $"{Fmt.Minutes(menit)} × {Fmt.Number(mode.FaktorEmisi, 4)} kg CO₂/menit";
            NewDayTotal = Fmt.Co2(_otherTotal + kg, 4);
            CanSave = true;
        }
        else
        {
            NewHeadline = "—";
            NewDetail = "Isi waktu mulai dan selesai yang valid";
            DeltaText = "";
            FormulaText = "Belum bisa dihitung";
            NewDayTotal = "—";
            CanSave = false;
        }
    }

    // ------------------------------------------------------------ Aksi

    private bool CanSubmit() => CanSave && !IsBusy;

    [RelayCommand(CanExecute = nameof(CanSubmit))]
    private async Task SaveAsync()
    {
        var user = _session.CurrentUser;
        var start = Resolve(StartText, _origStart);
        var end = Resolve(EndText, _origEnd);
        if (user is null || start is null || end is null) return;

        IsBusy = true;
        FormError = null;
        try
        {
            await _trips.ReviseTripAsync(user.Id, _logId, SelectedModeId, start.Value, end.Value);
            _onClose();
        }
        catch (ServiceException ex)
        {
            FormError = ex.Message;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Revisi] {ex}");
            FormError = "Revisi tidak dapat disimpan. Coba lagi.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void Cancel() => _onClose();
}
