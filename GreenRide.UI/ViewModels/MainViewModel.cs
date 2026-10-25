using CommunityToolkit.Mvvm.ComponentModel;
using GreenRide.UI.Models;
using GreenRide.UI.Services;

namespace GreenRide.UI.ViewModels;

/// <summary>Alur: Splash → Intro → Login ↔ Daftar → Shell (Dashboard/Catat/Ringkasan).</summary>
public partial class MainViewModel : ViewModelBase
{
    private readonly IGreenRideRepository _repo;

    [ObservableProperty] private ViewModelBase? _currentViewModel;

    public MainViewModel(IGreenRideRepository repo)
    {
        _repo = repo;
        _ = ShowSplashAsync();
    }

    private async Task ShowSplashAsync()
    {
        CurrentViewModel = new SplashViewModel();
        await Task.Delay(1800);
        ShowIntro();
    }

    private void ShowIntro() => CurrentViewModel = new IntroViewModel(ShowLogin);
    private void ShowLogin() => CurrentViewModel = new LoginViewModel(_repo, ShowShell, ShowRegister);
    private void ShowRegister() => CurrentViewModel = new RegisterViewModel(_repo, ShowShell, ShowLogin);
    private void ShowShell(AccountInfo account) => CurrentViewModel = new ShellViewModel(_repo, account, ShowLogin);
}
