namespace GreenRide.UI.Models;

/// <summary>Cermin tabel trip_logs. Satu baris = satu segmen perjalanan (satu moda).</summary>
public sealed class TripLog
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid UserId { get; init; }
    public int ModaId { get; set; }
    public DateOnly Tanggal { get; set; }
    public DateTime WaktuMulai { get; set; }
    public DateTime? WaktuSelesai { get; set; }
    public decimal? DurasiMenit { get; set; }
    public decimal? EmisiKg { get; set; }
    public bool IsRevised { get; set; }

    /// <summary>waktu_selesai NULL = timer masih berjalan.</summary>
    public bool IsRunning => WaktuSelesai is null;
}
