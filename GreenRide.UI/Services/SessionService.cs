using GreenRide.UI.Models;

namespace GreenRide.UI.Services;

/// <summary>Sesi hanya di memori (keputusan: login ulang setiap app dibuka).</summary>
public sealed class SessionService : ISessionService
{
    public UserAccount? CurrentUser { get; private set; }
    public bool IsLoggedIn => CurrentUser is not null;
    public event Action? Changed;

    public void SignIn(UserAccount user)
    {
        CurrentUser = user;
        Changed?.Invoke();
    }

    public void SignOut()
    {
        CurrentUser = null;
        Changed?.Invoke();
    }
}
