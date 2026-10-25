using GreenRide.Models;

namespace GreenRide.UI.Models;

public sealed class TransportOption
{
    public TransportMode Mode { get; }
    public string Emoji { get; }
    public string Nama => Mode.NamaModa;
    public string FaktorText => Mode.FaktorEmisi <= 0
        ? "0 emisi"
        : Mode.FaktorEmisi.ToString("0.00", Fmt.Id) + " kg/mnt";

    public TransportOption(TransportMode mode)
    {
        Mode = mode;
        Emoji = ModeCatalog.EmojiFor(mode.TransportModeId);
    }
}
