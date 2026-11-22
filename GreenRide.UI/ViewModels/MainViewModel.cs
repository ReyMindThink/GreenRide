using GreenRide.UI.Services;

namespace GreenRide.UI.ViewModels;

/// <summary>ViewModel jendela utama: hanya menampung layar aktif dari NavigationService.</summary>
public sealed class MainViewModel : ViewModelBase
{
    public INavigationService Navigation { get; }

    public MainViewModel(INavigationService navigation) => Navigation = navigation;

    public void Start() => Navigation.NavigateTo<SplashViewModel>();
}
