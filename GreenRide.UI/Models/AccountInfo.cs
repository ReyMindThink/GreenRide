namespace GreenRide.UI.Models;

/// <summary>Akun login sisi UI (sementara, sampai User backend dihubungkan ke database).</summary>
public sealed record AccountInfo(int UserId, string Nama, string Email, string Password);
