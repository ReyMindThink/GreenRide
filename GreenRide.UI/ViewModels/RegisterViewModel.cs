using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GreenRide.UI.Helpers;
using GreenRide.UI.Services;

namespace GreenRide.UI.ViewModels;

/// <summary>Layar Sign Up (PRD 5.1): nama, email, password, konfirmasi password.</summary>
public sealed partial class RegisterViewModel : ViewModelBase
{
    private readonly INavigationService _navigation;
    private readonly IAuthService _auth;

    public RegisterViewModel(INavigationService navigation, IAuthService auth)
    {
        _navigation = navigation;
        _auth = auth;
    }

    [ObservableProperty] private string _nama = "";
    [ObservableProperty] private string _email = "";
    [ObservableProperty] private string _password = "";
    [ObservableProperty] private string _confirmPassword = "";

    [ObservableProperty] private string? _namaError;
    [ObservableProperty] private string? _emailError;
    [ObservableProperty] private string? _passwordError;
    [ObservableProperty] private string? _confirmError;

    /// <summary>Kesalahan dari service (mis. "Email sudah terdaftar.").</summary>
    [ObservableProperty] private string? _formError;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SubmitCommand))]
    [NotifyPropertyChangedFor(nameof(SubmitText))]
    private bool _isBusy;

    public string SubmitText => IsBusy ? "Memproses..." : "Daftar";

    partial void OnNamaChanged(string value)
    {
        NamaError = null;
        FormError = null;
    }

    partial void OnEmailChanged(string value)
    {
        EmailError = null;
        FormError = null;
    }

    partial void OnPasswordChanged(string value)
    {
        PasswordError = null;
        ConfirmError = null;
        FormError = null;
    }

    partial void OnConfirmPasswordChanged(string value)
    {
        ConfirmError = null;
        FormError = null;
    }

    private bool CanSubmit() => !IsBusy;

    [RelayCommand(CanExecute = nameof(CanSubmit))]
    private async Task SubmitAsync()
    {
        FormError = null;

        NamaError = AuthValidator.ValidateNama(Nama);
        EmailError = AuthValidator.ValidateEmail(Email);
        PasswordError = AuthValidator.ValidateNewPassword(Password);
        ConfirmError = AuthValidator.ValidateConfirm(Password, ConfirmPassword);
        if (NamaError is not null || EmailError is not null ||
            PasswordError is not null || ConfirmError is not null)
            return;

        IsBusy = true;
        try
        {
            var email = Email.Trim();
            var result = await _auth.RegisterAsync(Nama.Trim(), email, Password);
            if (!result.Success)
            {
                FormError = result.Error ?? "Pendaftaran gagal.";
                return;
            }

            // PRD 7: Register -> Login. Email diisi otomatis supaya tinggal mengetik password.
            _navigation.NavigateTo<LoginViewModel>(vm =>
            {
                vm.Email = email;
                vm.InfoMessage = "Akun berhasil dibuat. Silakan masuk.";
            });
        }
        catch (ServiceException ex)
        {
            FormError = ex.Message;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Register] {ex}");
            FormError = "Tidak dapat mendaftar saat ini. Coba lagi.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void GoToLogin() => _navigation.NavigateTo<LoginViewModel>();
}
