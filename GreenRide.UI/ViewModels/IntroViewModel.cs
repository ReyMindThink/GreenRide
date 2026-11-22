using CommunityToolkit.Mvvm.Input;
using GreenRide.UI.Services;

namespace GreenRide.UI.ViewModels;

/// <summary>Layar Awal: sambutan + tombol Masuk / Daftar.</summary>
public sealed partial class IntroViewModel : ViewModelBase
{
    private readonly INavigationService _navigation;

    public IntroViewModel(INavigationService navigation) => _navigation = navigation;

    [RelayCommand]
    private void GoToLogin() => _navigation.NavigateTo<LoginViewModel>();

    [RelayCommand]
    private void GoToRegister() => _navigation.NavigateTo<RegisterViewModel>();
}
