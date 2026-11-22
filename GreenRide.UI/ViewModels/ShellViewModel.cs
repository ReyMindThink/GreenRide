using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GreenRide.UI.Helpers;
using GreenRide.UI.Models;
using GreenRide.UI.Services;
using Microsoft.Extensions.DependencyInjection;

namespace GreenRide.UI.ViewModels;

/// <summary>Kerangka setelah login: sidebar + halaman aktif.</summary>
public sealed partial class ShellViewModel : ViewModelBase, INavigationAware
{
    private readonly IServiceProvider _services;
    private readonly ISessionService _session;
    private readonly INavigationService _navigation;

    private readonly NavItem _catat;
    private readonly NavItem _leaderboard;
    private readonly NavItem _ringkasan;

    public ShellViewModel(IServiceProvider services, ISessionService session, INavigationService navigation)
    {
        _services = services;
        _session = session;
        _navigation = navigation;

        _catat = new NavItem("CATAT", IconResources.Get("Icon.Catat"), ShowCatat);
        _leaderboard = new NavItem("LEADERBOARD", IconResources.Get("Icon.Trophy"), ShowLeaderboard);
        _ringkasan = new NavItem("RINGKASAN", IconResources.Get("Icon.Ringkasan"), ShowRingkasan);
        NavItems = new[] { _catat, _leaderboard, _ringkasan };
        KeluarIcon = IconResources.Get("Icon.Keluar");
    }

    public IReadOnlyList<NavItem> NavItems { get; }
    public Avalonia.Media.Geometry? KeluarIcon { get; }

    [ObservableProperty] private ViewModelBase? _currentPage;

    public Task OnNavigatedToAsync()
    {
        ShowCatat();
        return Task.CompletedTask;
    }

    private void ShowCatat() => Show(_services.GetRequiredService<CatatViewModel>(), _catat);

    private void ShowLeaderboard() => Show(_services.GetRequiredService<LeaderboardViewModel>(), _leaderboard);

    private void ShowRingkasan()
    {
        var vm = _services.GetRequiredService<RingkasanViewModel>();
        vm.OnRevise = ShowRevisi;
        Show(vm, _ringkasan);
    }

    // Revisi dibuka dari baris tabel di Ringkasan, jadi menu "Ringkasan" tetap menyala.
    private void ShowRevisi(TripLog log)
    {
        var vm = _services.GetRequiredService<RevisiViewModel>();
        vm.Initialize(log, ShowRingkasan);
        Show(vm, _ringkasan);
    }

    private void Show(ViewModelBase page, NavItem active)
    {
        (CurrentPage as IDisposable)?.Dispose();
        CurrentPage = page;
        foreach (var item in NavItems) item.IsActive = ReferenceEquals(item, active);

        if (page is INavigationAware aware)
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
            Debug.WriteLine($"[Shell] {ex}");
        }
    }

    [RelayCommand]
    private void SignOut()
    {
        (CurrentPage as IDisposable)?.Dispose();
        CurrentPage = null;
        _session.SignOut();
        _navigation.NavigateTo<IntroViewModel>();
    }
}
