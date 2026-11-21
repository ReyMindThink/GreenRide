namespace GreenRide.Models;

public sealed class WeeklyBadge
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public DateOnly MingguMulai { get; private set; }
    public DateOnly MingguSelesai { get; private set; }
    public decimal TotalEmisiKg { get; private set; }
    public string? BadgeName { get; private set; }
    public int HariTercatat { get; private set; }

    public WeeklyBadge(Guid userId, DateOnly mingguMulai, DateOnly mingguSelesai,
                       decimal totalEmisiKg, string? badgeName, int hariTercatat)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        MingguMulai = mingguMulai;
        MingguSelesai = mingguSelesai;
        TotalEmisiKg = totalEmisiKg;
        BadgeName = badgeName;
        HariTercatat = hariTercatat;
    }

    public WeeklyBadge(Guid id, Guid userId, DateOnly mingguMulai, DateOnly mingguSelesai,
                       decimal totalEmisiKg, string? badgeName, int hariTercatat)
    {
        Id = id;
        UserId = userId;
        MingguMulai = mingguMulai;
        MingguSelesai = mingguSelesai;
        TotalEmisiKg = totalEmisiKg;
        BadgeName = badgeName;
        HariTercatat = hariTercatat;
    }

    public bool HasBadge => BadgeName is not null;
}
