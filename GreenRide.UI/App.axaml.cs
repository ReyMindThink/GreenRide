using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using GreenRide.UI.Services;
using GreenRide.UI.Services.Mock;
using GreenRide.UI.ViewModels;
using GreenRide.UI.Views;
using Microsoft.Extensions.DependencyInjection;

namespace GreenRide.UI;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        var collection = new ServiceCollection();
        ConfigureServices(collection);
        Services = collection.BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var main = Services.GetRequiredService<MainViewModel>();
            desktop.MainWindow = new MainWindow { DataContext = main };
            main.Start();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static void ConfigureServices(IServiceCollection sc)
    {
        // Infrastruktur UI
        sc.AddSingleton<IClock, SystemClock>();
        sc.AddSingleton<ISessionService, SessionService>();
        sc.AddSingleton<INavigationService, NavigationService>();

        // Data. TAHAP 1: Mock (data palsu). Nanti diganti adapter ke backend/Supabase,
        // cukup ganti lima baris di bawah ini.
        sc.AddSingleton<MockGreenRideBackend>();
        sc.AddSingleton<IAuthService>(p => p.GetRequiredService<MockGreenRideBackend>());
        sc.AddSingleton<ITripService>(p => p.GetRequiredService<MockGreenRideBackend>());
        sc.AddSingleton<IEmissionService>(p => p.GetRequiredService<MockGreenRideBackend>());
        sc.AddSingleton<IBadgeProgressService>(p => p.GetRequiredService<MockGreenRideBackend>());
        sc.AddSingleton<ILeaderboardService>(p => p.GetRequiredService<MockGreenRideBackend>());

        // ViewModel
        sc.AddSingleton<MainViewModel>();
        sc.AddTransient<SplashViewModel>();
        sc.AddTransient<IntroViewModel>();
        sc.AddTransient<LoginViewModel>();
        sc.AddTransient<RegisterViewModel>();
        sc.AddTransient<ShellViewModel>();
        sc.AddTransient<CatatViewModel>();
        sc.AddTransient<RingkasanViewModel>();
        sc.AddTransient<RevisiViewModel>();
        sc.AddTransient<LeaderboardViewModel>();
        sc.AddTransient<PlaceholderViewModel>();
    }
}
