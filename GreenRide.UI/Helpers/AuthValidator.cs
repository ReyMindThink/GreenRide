using System.Text;
using System.Text.RegularExpressions;

namespace GreenRide.UI.Helpers;

/// <summary>
/// Validasi form Login / Sign Up. Mengembalikan pesan error (Bahasa Indonesia) atau null bila valid.
/// Aturan sesuai PRD 5.1 + kolom di tabel users (nama dan email maks. 100 karakter).
/// </summary>
public static partial class AuthValidator
{
    public const int MinPasswordLength = 6;

    /// <summary>BCrypt hanya memakai 72 byte pertama, jadi lebih panjang dari itu ditolak.</summary>
    public const int MaxPasswordBytes = 72;

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailPattern();

    public static string? ValidateNama(string? value)
    {
        var v = value?.Trim() ?? "";
        if (v.Length == 0) return "Nama wajib diisi.";
        if (v.Length > 100) return "Nama maksimal 100 karakter.";
        return null;
    }

    public static string? ValidateEmail(string? value)
    {
        var v = value?.Trim() ?? "";
        if (v.Length == 0) return "Email wajib diisi.";
        if (v.Length > 100) return "Email maksimal 100 karakter.";
        if (!EmailPattern().IsMatch(v)) return "Format email tidak valid.";
        return null;
    }

    /// <summary>Untuk Sign Up: minimal 6 karakter.</summary>
    public static string? ValidateNewPassword(string? value)
    {
        var v = value ?? "";
        if (v.Length == 0) return "Password wajib diisi.";
        if (v.Length < MinPasswordLength) return $"Password minimal {MinPasswordLength} karakter.";
        if (Encoding.UTF8.GetByteCount(v) > MaxPasswordBytes) return "Password terlalu panjang (maksimal 72 karakter).";
        return null;
    }

    /// <summary>Untuk Log In: cukup tidak kosong. Kecocokan dicek oleh service.</summary>
    public static string? ValidateLoginPassword(string? value) =>
        string.IsNullOrEmpty(value) ? "Password wajib diisi." : null;

    public static string? ValidateConfirm(string? password, string? confirm)
    {
        if (string.IsNullOrEmpty(confirm)) return "Konfirmasi password wajib diisi.";
        if (!string.Equals(password, confirm, StringComparison.Ordinal)) return "Konfirmasi password tidak sama.";
        return null;
    }
}
