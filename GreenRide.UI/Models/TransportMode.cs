using System.Globalization;

namespace GreenRide.UI.Models;

/// <summary>Cermin tabel transport_modes (data statis, tidak diubah saat runtime).</summary>
public sealed record TransportMode(int Id, string Nama, string Label, decimal FaktorEmisi, string IconUri)
{
    /// <summary>Teks di kartu moda, mis. "0.027 kg/mnt" (format sesuai desain).</summary>
    public string FaktorLabel =>
        FaktorEmisi.ToString("0.###", CultureInfo.InvariantCulture) + " kg/mnt";
}

public static class ModeCatalog
{
    public const int Motor = 1;
    public const int Mobil = 2;
    public const int Bus = 3;
    public const int Kereta = 4;

    public static IReadOnlyList<TransportMode> All { get; } = new[]
    {
        new TransportMode(Motor,  "Motor",           "Motor",  0.0270m, "/Assets/Moda/motor.svg"),
        new TransportMode(Mobil,  "Mobil",           "Mobil",  0.0720m, "/Assets/Moda/mobil.svg"),
        new TransportMode(Bus,    "Bus Trans Jogja", "Bus",    0.0430m, "/Assets/Moda/bus.svg"),
        new TransportMode(Kereta, "Kereta Api",      "Kereta", 0.0160m, "/Assets/Moda/kereta.svg"),
    };

    public static TransportMode Get(int id) => All.First(m => m.Id == id);
}
