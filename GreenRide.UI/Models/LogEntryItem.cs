using GreenRide.Models;

namespace GreenRide.UI.Models;

public sealed class LogEntryItem
{
    public DailyLog Log { get; }
    public TransportMode Mode { get; }

    public LogEntryItem(DailyLog log, TransportMode mode)
    {
        Log = log;
        Mode = mode;
    }

    public string Emoji => ModeCatalog.EmojiFor(Mode.TransportModeId);
    public string ModaNama => Mode.NamaModa;
    public double Emisi => Log.HitungEmisiHarian(Mode);
    public string EmisiText => Fmt.Kg(Emisi);
    public string DetailText => $"{Log.Durasi} menit · {Log.Tanggal.ToString("ddd, d MMM", Fmt.Id)}";

    /// <summary>Revisi hanya boleh untuk log dalam rentang 1 hari (hari ini / kemarin).</summary>
    public bool CanRevise => (DateTime.Today - Log.Tanggal.Date).TotalDays <= 1;
}
