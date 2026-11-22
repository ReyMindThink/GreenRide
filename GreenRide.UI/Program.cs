using Avalonia;

namespace GreenRide.UI;

internal sealed class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // Aktifkan legacy timestamp agar DateTime bekerja dengan kolom TIMESTAMP (tanpa TZ)
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

        // Muat variabel dari file .env (gitignored) supaya GREENRIDE_DB tersedia
        LoadDotEnv();

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    /// <summary>
    /// Mencari file .env di beberapa lokasi (mendukung dev dan production).
    /// </summary>
    private static void LoadDotEnv()
    {
        // Cari .env dari direktori kerja, lalu naik ke induk (maks 4 level)
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        for (int i = 0; i < 5 && dir is not null; i++, dir = dir.Parent)
        {
            var path = Path.Combine(dir.FullName, ".env");
            if (!File.Exists(path)) continue;

            foreach (var raw in File.ReadAllLines(path))
            {
                var line = raw.Trim();
                if (line.Length == 0 || line.StartsWith('#')) continue;
                var idx = line.IndexOf('=');
                if (idx < 1) continue;
                var key = line[..idx].Trim();
                var val = line[(idx + 1)..].Trim();
                if (!string.IsNullOrEmpty(key))
                    Environment.SetEnvironmentVariable(key, val);
            }
            return; // berhenti di file .env pertama yang ditemukan
        }
    }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
