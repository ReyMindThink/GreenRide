namespace GreenRide.UI.Models;

public enum BadgeLevel
{
    None = 0,
    EcoInitiate = 1,
    CarbonTamer = 2,
    TerraSentinel = 3,
    ZeroTrace = 4,
}

public sealed record BadgeDefinition(
    BadgeLevel Level,
    string Nama,          // teks di UI, mis. "Zero Trace"
    string DbName,        // nilai di weekly_badges.badge_name, mis. "ZeroTrace"
    decimal BatasMingguKg,
    string BatasLabel,    // mis. "≤ 10 kg CO₂/minggu"
    string IconUri,
    string LockedIconUri);

public static class BadgeCatalog
{
    /// <summary>Urut dari tingkat terendah ke tertinggi (sama dengan urutan di desain).</summary>
    public static IReadOnlyList<BadgeDefinition> All { get; } = new[]
    {
        new BadgeDefinition(BadgeLevel.EcoInitiate,   "Eco Initiate",   "Eco Initiate",   10m, "≤ 10 kg CO₂/minggu",
            "/Assets/Badges/eco-initiate.svg",   "/Assets/Badges/eco-initiate-locked.svg"),
        new BadgeDefinition(BadgeLevel.CarbonTamer,   "Carbon Tamer",   "Carbon Tamer",   6m,  "≤ 6 kg CO₂/minggu",
            "/Assets/Badges/carbon-tamer.svg",   "/Assets/Badges/carbon-tamer-locked.svg"),
        new BadgeDefinition(BadgeLevel.TerraSentinel, "Terra Sentinel", "Terra Sentinel", 3m,  "≤ 3 kg CO₂/minggu",
            "/Assets/Badges/terra-sentinel.svg", "/Assets/Badges/terra-sentinel-locked.svg"),
        new BadgeDefinition(BadgeLevel.ZeroTrace,     "Zero Trace",     "ZeroTrace",      0m,  "0 kg CO₂/minggu",
            "/Assets/Badges/zero-trace.svg",     "/Assets/Badges/zero-trace-locked.svg"),
    };

    public static BadgeDefinition? Get(BadgeLevel level) =>
        All.FirstOrDefault(b => b.Level == level);

    public static BadgeLevel FromDbName(string? dbName) =>
        All.FirstOrDefault(b => b.DbName == dbName)?.Level ?? BadgeLevel.None;
}

/// <summary>Cermin tabel weekly_badges.</summary>
public sealed record WeeklyBadgeRecord(
    DateOnly MingguMulai,
    DateOnly MingguSelesai,
    decimal TotalEmisiKg,
    BadgeLevel Badge,
    int HariTercatat);
