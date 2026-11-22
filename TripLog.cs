namespace GreenRide.Models;

public sealed class TripLog
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public int ModaId { get; private set; }
    public DateOnly Tanggal { get; private set; }
    public DateTime WaktuMulai { get; private set; }
    public DateTime? WaktuSelesai { get; private set; }
    public decimal? DurasiMenit { get; private set; }
    public decimal? EmisiKg { get; private set; }
    public bool IsRevised { get; private set; }

    public TripLog(Guid userId, int modaId, DateOnly tanggal, DateTime waktuMulai)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        ModaId = modaId;
        Tanggal = tanggal;
        WaktuMulai = waktuMulai;
    }

    public TripLog(Guid id, Guid userId, int modaId, DateOnly tanggal,
                   DateTime waktuMulai, DateTime? waktuSelesai,
                   decimal? durasiMenit, decimal? emisiKg, bool isRevised)
    {
        Id = id;
        UserId = userId;
        ModaId = modaId;
        Tanggal = tanggal;
        WaktuMulai = waktuMulai;
        WaktuSelesai = waktuSelesai;
        DurasiMenit = durasiMenit;
        EmisiKg = emisiKg;
        IsRevised = isRevised;
    }

    public bool IsActive => WaktuSelesai is null;

    public void Selesaikan(DateTime waktuSelesai, decimal faktorEmisi)
    {
        if (!IsActive) throw new InvalidOperationException("Perjalanan sudah selesai.");
        WaktuSelesai = waktuSelesai;
        DurasiMenit = (decimal)(waktuSelesai - WaktuMulai).TotalMinutes;
        EmisiKg = DurasiMenit * faktorEmisi;
    }

    public void Revisi(int modaIdBaru, decimal faktorEmisiBaru)
    {
        ModaId = modaIdBaru;
        EmisiKg = DurasiMenit * faktorEmisiBaru;
        IsRevised = true;
    }
}
