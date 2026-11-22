namespace GreenRide.UI.Models;

/// <param name="BadgeId">1=Eco Initiate, 2=Carbon Tamer, 3=Terra Sentinel, 4=ZeroTrace</param>
/// <param name="IconKind">Pemula | Hemat | Pahlawan — kunci ikon vektor di BadgeIcon</param>
public sealed record BadgeDefinition(int BadgeId, string Nama, string Syarat, double BatasKg, string IconKind);

public static class BadgeRules
{
    public static readonly IReadOnlyList<BadgeDefinition> All = new[]
    {
        new BadgeDefinition(1, "Eco Initiate",   "≤ 10 kg CO₂ per minggu", 10, "Pemula"),
        new BadgeDefinition(2, "Carbon Tamer",   "≤ 6 kg CO₂ per minggu",   6, "Hemat"),
        new BadgeDefinition(3, "Terra Sentinel", "≤ 3 kg CO₂ per minggu",   3, "Pahlawan"),
        // Ikon khusus ZeroTrace belum ada di BadgeIcon.axaml; sementara memakai ikon Pahlawan.
        new BadgeDefinition(4, "ZeroTrace",      "0 kg CO₂ selama seminggu penuh", 0, "Pahlawan"),
    };
}