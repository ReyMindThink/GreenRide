using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GreenRide.UI.Models;
using GreenRide.UI.Services;

namespace GreenRide.UI.ViewModels;

public partial class RegisterViewModel : ViewModelBase
{
    private readonly IGreenRideRepository _repo;
    private readonly Action<AccountInfo> _onSuccess;
    private readonly Action _onBack;

    [ObservableProperty] private string _nama = "";
    [ObservableProperty] private string _email = "";
    [ObservableProperty] private string _password = "";
    [ObservableProperty] private string _konfirmasi = "";
    [ObservableProperty] private string? _error;

    public RegisterViewModel(IGreenRideRepository repo, Action<AccountInfo> onSuccess, Action onBack)
    {
        _repo = repo;
        _onSuccess = onSuccess;
        _onBack = onBack;
    }

    [RelayCommand]
    private void Daftar()
    {
        if (string.IsNullOrWhiteSpace(Nama) || string.IsNullOrWhiteSpace(Email) || string.IsNullOrEmpty(Password))
        { Error = "Semua kolom wajib diisi."; return; }
        if (!Email.Contains('@') || !Email.Contains('.'))
        { Error = "Format email tidak valid."; return; }
        if (Password.Length < 6)
        { Error = "Password minimal 6 karakter."; return; }
        if (Password != Konfirmasi)
        { Error = "Konfirmasi password tidak sama."; return; }

        var acc = _repo.Register(Nama, Email, Password);
        if (acc is null) { Error = "Email sudah terdaftar."; return; }
        Error = null;
        _onSuccess(acc);
    }

    [RelayCommand] private void KeLogin() => _onBack();
}
