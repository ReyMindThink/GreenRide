using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GreenRide.Models;
using GreenRide.UI.Models;
using GreenRide.UI.Services;

namespace GreenRide.UI.ViewModels;

/// <summary>
/// Form revisi waktu mulai/selesai sebuah trip. Input berupa jam lokal "HH:mm";
/// disimpan ke database sebagai UTC (konsisten dengan StartTrip dan NOW()).
/// </summary>
public partial class RevisiViewModel : PageViewModelBase
{
    private readonly IGreenRideRepository _repo;
    private readonly LogEntryItem _item;
    private readonly Action _onDone;
    private readonly DateTime _tanggalLokal;
    private readonly List<TripLog> _tripLain;
    private readonly Dictionary<int, TransportMode> _modes;

    public string ModaNama => _item.ModaNama;
    public string TanggalText => _tanggalLokal.ToString("dddd, d MMMM yyyy", Fmt.Id);

    [ObservableProperty] private string _waktuMulaiText;
    [ObservableProperty] private string _waktuSelesaiText;
    [ObservableProperty] private string? _errorMessage;

    /// <param name="tripHariIni">Semua trip milik user pada hari trip ini; trip yang sedang direvisi disaring otomatis.</param>
    public RevisiViewModel(IGreenRideRepository repo, LogEntryItem item,
                           IEnumerable<TripLog> tripHariIni, Action onDone)
    {
        _repo   = repo;
        _item   = item;
        _onDone = onDone;
        _tripLain = tripHariIni
            .Where(t => t.Id != item.Log.Id && t.UserId == item.Log.UserId && t.Tanggal == item.Log.Tanggal)
            .ToList();
        _modes = repo.GetTransportModes().ToDictionary(m => m.TransportModeId);

        var mulaiLokal   = item.Log.WaktuMulai.ToLocalTime();
        _tanggalLokal    = mulaiLokal.Date;
        _waktuMulaiText  = mulaiLokal.ToString("HH:mm");
        _waktuSelesaiText = (item.Log.WaktuSelesai ?? item.Log.WaktuMulai)
                                .ToLocalTime().ToString("HH:mm");
    }

    private bool TryParseJam(string text, out DateTime utc)
    {
        utc = default;
        if (!TimeSpan.TryParseExact(text.Trim(), @"hh\:mm", CultureInfo.InvariantCulture, out var ts)
            && !TimeSpan.TryParseExact(text.Trim(), @"h\:mm", CultureInfo.InvariantCulture, out ts))
            return false;
        if (ts >= TimeSpan.FromDays(1)) return false;
        var lokal = DateTime.SpecifyKind(_tanggalLokal + ts, DateTimeKind.Local);
        utc = lokal.ToUniversalTime();
        return true;
    }

    [RelayCommand]
    private async Task SimpanRevisiAsync()
    {
        ErrorMessage = null;
        if (!TryParseJam(WaktuMulaiText, out var mulai) || !TryParseJam(WaktuSelesaiText, out var selesai))
        {
            ErrorMessage = "Format jam harus HH:mm, mis. 07:30.";
            return;
        }
        if (selesai <= mulai)
        {
            ErrorMessage = "Waktu selesai harus setelah waktu mulai.";
            return;
        }

        // Validasi 1: waktu selesai tidak boleh di masa depan (semua waktu UTC)
        if (selesai > DateTime.UtcNow)
        {
            ErrorMessage = "Waktu selesai tidak boleh melebihi waktu saat ini";
            return;
        }

        // Validasi 2: tidak boleh bertabrakan dengan trip lain di hari yang sama.
        // Trip yang masih berjalan (WaktuSelesai null) dianggap berakhir sekarang.
        foreach (var lain in _tripLain)
        {
            var mulaiB   = lain.WaktuMulai;
            var selesaiB = lain.WaktuSelesai ?? DateTime.UtcNow;
            if (mulai < selesaiB && selesai > mulaiB)
            {
                _modes.TryGetValue(lain.ModaId, out var m);
                ErrorMessage = $"Waktu bertabrakan dengan perjalanan {m?.NamaModa ?? "lain"} yang lain";
                return;
            }
        }

        try
        {
            var hasil = await _repo.UpdateTripLog(_item.Log.Id, mulai, selesai);
            if (hasil is null)
            {
                ErrorMessage = "Perjalanan tidak ditemukan.";
                return;
            }
            _onDone();
        }
        catch (Exception ex)
        {
            ErrorMessage = "Gagal menyimpan revisi: " + ex.Message;
        }
    }

    [RelayCommand]
    private void Batal() => _onDone();
}
