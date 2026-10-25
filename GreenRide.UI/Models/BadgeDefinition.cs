namespace GreenRide.UI.Models;

/// <param name="BadgeId">1=Pemula, 2=Hemat, 3=Pahlawan (samakan dengan BadgeId di backend/DB)</param>
/// <param name="IconKind">Pemula | Hemat | Pahlawan — kunci ikon vektor di BadgeIcon</param>
public sealed record BadgeDefinition(int BadgeId, string Nama, string Syarat, double BatasKg, string IconKind);

public static class BadgeRules
{
    public static readonly IReadOnlyList<BadgeDefinition> All = new[]
    {
        new BadgeDefinition(1, "Hijau Pemula",   "≤ 10 kg CO₂ per minggu", 10, "Pemula"),
        new BadgeDefinition(2, "Hemat Emisi",    "≤ 6 kg CO₂ per minggu",   6, "Hemat"),
        new BadgeDefinition(3, "Pahlawan Bumi",  "≤ 3 kg CO₂ per minggu",   3, "Pahlawan"),
    };
}
