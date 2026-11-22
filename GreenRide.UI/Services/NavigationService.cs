using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using GreenRide.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace GreenRide.UI.Services;

public sealed partial class NavigationService : ObservableObject, INavigationService
{
    private readonly IServiceProvider _services;

    [ObservableProperty]
    private ViewModelBase? _current;

    public NavigationService(IServiceProvider services) => _services = services;

    public void NavigateTo<TViewModel>(Action<TViewModel>? init = null) where TViewModel : ViewModelBase
    {
        var vm = _services.GetRequiredService<TViewModel>();
        init?.Invoke(vm);
        Current = vm;

        if (vm is INavigationAware aware)
            _ = RunAsync(aware);
    }

    private static async Task RunAsync(INavigationAware aware)
    {
        try
        {
            await aware.OnNavigatedToAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Navigation] {ex}");
        }
    }
}
