namespace GreenRide.UI.Models;

public sealed record LeaderboardEntry(
    int Peringkat,
    string Nama,
    decimal TotalEmisiKg
);
