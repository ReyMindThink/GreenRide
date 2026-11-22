using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GreenRide.UI.Helpers;
using GreenRide.UI.Services;

namespace GreenRide.UI.ViewModels;

/// <summary>Layar Log In (PRD 5.1): email + password, lalu membuka sesi.</summary>
public sealed partial class LoginViewModel : ViewModelBase
{
    private readonly INavigationService _navigation;
    private readonly ISessionService _session;
    private readonly IAuthService _auth;
    private readonly IBadgeProgressService _badges;

    public LoginViewModel(
        INavigationService navigation,
        ISessionService session,
        IAuthService auth,
        IBadgeProgressService badges)
    {
        _navigation = navigation;
        _session = session;
        _auth = auth;
        _badges = badges;
    }

    [ObservableProperty] private string _email = "";
    [ObservableProperty] private string _password = "";

    [ObservableProperty] private string? _emailError;
    [ObservableProperty] private string? _passwordError;

    /// <summary>Kesalahan dari service (mis. "Email atau password salah.").</summary>
    [ObservableProperty] private string? _formError;

    /// <summary>Pesan sukses, mis. setelah Sign Up berhasil.</summary>
    [ObservableProperty] private string? _infoMessage;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SubmitCommand))]
    [NotifyPropertyChangedFor(nameof(SubmitText))]
    private bool _isBusy;

    public string SubmitText => IsBusy ? "Memproses..." : "Masuk";

    partial void OnEmailChanged(string value)
    {
        EmailError = null;
        FormError = null;
    }

    partial void OnPasswordChanged(string value)
    {
        PasswordError = null;
        FormError = null;
    }

    private bool CanSubmit() => !IsBusy;

    [RelayCommand(CanExecute = nameof(CanSubmit))]
    private async Task SubmitAsync()
    {
        FormError = null;
        InfoMessage = null;

        EmailError = AuthValidator.ValidateEmail(Email);
        PasswordError = AuthValidator.ValidateLoginPassword(Password);
        if (EmailError is not null || PasswordError is not null) return;

        IsBusy = true;
        try
        {
            var result = await _auth.LoginAsync(Email.Trim(), Password);
            if (!result.Success || result.User is null)
            {
                FormError = result.Error ?? "Email atau password salah.";
                return;
            }

            var user = result.User;
            _session.SignIn(user);

            // Keputusan #9: saat login, hitung badge untuk minggu lampau yang belum tercatat.
            // Kegagalan di sini tidak boleh menghalangi pengguna masuk.
            try
            {
                await _badges.SettleCompletedWeeksAsync(user.Id);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Login] Settle badge gagal: {ex.Message}");
            }

            _navigation.NavigateTo<ShellViewModel>();
        }
        catch (ServiceException ex)
        {
            FormError = ex.Message;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Login] {ex}");
            FormError = "Tidak dapat masuk saat ini. Coba lagi.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void GoToRegister() => _navigation.NavigateTo<RegisterViewModel>();
}
