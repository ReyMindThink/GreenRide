namespace GreenRide.UI.Models;

public sealed record UserAccount(Guid Id, string Nama, string Email);

public sealed record AuthResult(bool Success, string? Error, UserAccount? User)
{
    public static AuthResult Ok(UserAccount user) => new(true, null, user);
    public static AuthResult Fail(string error) => new(false, error, null);
}
