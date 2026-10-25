namespace GreenRide.UI.Models;

/// <summary>Emoji per moda (berdasarkan TransportModeId) — backend tidak menyimpan emoji.</summary>
public static class ModeCatalog
{
    public const double FaktorAcuanMobil = 0.10; // kg CO2 / menit, pembanding "hemat"

    private static readonly Dictionary<int, string> Emojis = new()
    {
        [1] = "🚶", [2] = "🚲", [3] = "🚌", [4] = "🏍️", [5] = "🚗",
    };

    public static string EmojiFor(int transportModeId) =>
        Emojis.TryGetValue(transportModeId, out var e) ? e : "🧭";
}
