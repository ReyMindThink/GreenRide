using GreenRide.Models;

namespace GreenRide.UI.Models;

/// <summary>Item tampilan untuk satu perjalanan (TripLog).</summary>
public sealed class LogEntryItem
{
    public TripLog Log { get; }
    /// <summary>Mode opsional untuk menampilkan nama moda.</summary>
    public TransportMode? Mode { get; }

    public LogEntryItem(TripLog log, TransportMode? mode = null)
    {
        Log  = log;
        Mode = mode;
    }

    /// <summary>ID moda transportasi — dipakai front end untuk menampilkan ikon vektor.</summary>
    public int ModaId => Log.ModaId;

    public string ModaNama => Mode?.NamaModa ?? "";

    /// <summary>Emisi sudah dihitung backend saat CompleteTrip.</summary>
    public double Emisi => (double)(Log.EmisiKg ?? 0m);
    public string EmisiText => Fmt.Kg(Emisi);

    /// <summary>Durasi dari DurasiMenit, mis. "2,13"; kosong-nol jika trip belum selesai.</summary>
    public string DurasiText => (Log.DurasiMenit ?? 0m).ToString("0.00", Fmt.Id);

    public string DetailText
    {
        get
        {
            var tgl = Log.Tanggal.ToString("ddd, d MMM", Fmt.Id);
            if (Log.WaktuSelesai.HasValue)
            {
                var dur = (int)(Log.WaktuSelesai.Value - Log.WaktuMulai).TotalMinutes;
                return $"{tgl} · {dur} menit";
            }
            return tgl;
        }
    }

    /// <summary>Revisi hanya boleh untuk log hari ini atau kemarin.</summary>
    public bool CanRevise =>
        (DateOnly.FromDateTime(DateTime.Today).DayNumber - Log.Tanggal.DayNumber) <= 1;
}
