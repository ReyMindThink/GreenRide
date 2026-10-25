using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using GreenRide.UI.Services;
using GreenRide.UI.ViewModels;
using GreenRide.UI.Views;

namespace GreenRide.UI;

public partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Ganti InMemoryGreenRideRepository dengan implementasi PostgreSQL/MongoDB nanti.
            IGreenRideRepository repo = new InMemoryGreenRideRepository();
            desktop.MainWindow = new MainWindow { DataContext = new MainViewModel(repo) };
        }
        base.OnFrameworkInitializationCompleted();
    }
}
