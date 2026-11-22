using Avalonia.Controls;
using Avalonia.Interactivity;

namespace GreenRide.UI.Views;

public partial class LoginView : UserControl
{
    public LoginView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e) =>
        this.FindControl<TextBox>("EmailBox")?.Focus();
}
