using GreenRide.UI.Helpers;
using GreenRide.UI.Services;

namespace GreenRide.UI.ViewModels;

public sealed class SplashViewModel : ViewModelBase, INavigationAware
{
    private readonly INavigationService _navigation;

    public SplashViewModel(INavigationService navigation) => _navigation = navigation;

    public async Task OnNavigatedToAsync()
    {
        await Task.Delay(AppConfig.SplashDuration);
        _navigation.NavigateTo<IntroViewModel>();
    }
}
