namespace GreenRide.UI.Helpers;

public static class AppConfig
{
    /// <summary>Lama splash sebelum masuk ke Layar Awal.</summary>
    public static readonly TimeSpan SplashDuration = TimeSpan.FromSeconds(1.5);

    /// <summary>Lebar jendela di bawah ini dianggap "compact" (layout mobile).</summary>
    public const double CompactWidth = 720;

    /// <summary>Lebar jendela di bawah ini dianggap "medium" (tablet / jendela sempit).</summary>
    public const double MediumWidth = 1100;
}
