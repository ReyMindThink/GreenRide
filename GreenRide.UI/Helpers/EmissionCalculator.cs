namespace GreenRide.UI.Helpers;

/// <summary>Rumus PRD bagian 4.1:  emisi_kg = durasi_menit x faktor_emisi.</summary>
public static class EmissionCalculator
{
    /// <summary>durasi_menit (DECIMAL(8,2)).</summary>
    public static decimal DurationMinutes(DateTime mulai, DateTime selesai) =>
        Math.Round((decimal)(selesai - mulai).TotalMinutes, 2, MidpointRounding.AwayFromZero);

    /// <summary>emisi_kg = ROUND(durasi_menit x faktor, 4).</summary>
    public static decimal EmissionKg(decimal durasiMenit, decimal faktorEmisi) =>
        Math.Round(durasiMenit * faktorEmisi, 4, MidpointRounding.AwayFromZero);

    public static (decimal Menit, decimal Kg) Calculate(DateTime mulai, DateTime selesai, decimal faktorEmisi)
    {
        var menit = DurationMinutes(mulai, selesai);
        return (menit, EmissionKg(menit, faktorEmisi));
    }

    /// <summary>Emisi sementara untuk timer yang masih berjalan.</summary>
    public static decimal LiveKg(DateTime mulai, DateTime now, decimal faktorEmisi)
    {
        var menit = (decimal)Math.Max(0, (now - mulai).TotalMinutes);
        return EmissionKg(menit, faktorEmisi);
    }
}
