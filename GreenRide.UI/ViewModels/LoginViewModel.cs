using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GreenRide.UI.Models;
using GreenRide.UI.Services;

namespace GreenRide.UI.ViewModels;

public partial class LoginViewModel : ViewModelBase
{
    private readonly IGreenRideRepository _repo;
    private readonly Action<AccountInfo> _onSuccess;
    private readonly Action _onRegister;

    [ObservableProperty] private string _email = "";
    [ObservableProperty] private string _password = "";
    [ObservableProperty] private string? _error;

    public LoginViewModel(IGreenRideRepository repo, Action<AccountInfo> onSuccess, Action onRegister)
    {
        _repo = repo;
        _onSuccess = onSuccess;
        _onRegister = onRegister;
    }

    [RelayCommand]
    private void Masuk()
    {
        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrEmpty(Password))
        {
            Error = "Email dan password wajib diisi.";
            return;
        }
        var acc = _repo.Authenticate(Email, Password);
        if (acc is null) { Error = "Email atau password salah."; return; }
        Error = null;
        _onSuccess(acc);
    }

    [RelayCommand] private void KeDaftar() => _onRegister();
}
