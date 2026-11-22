using Avalonia.Controls;
using Avalonia.Interactivity;

namespace GreenRide.UI.Views;

public partial class RegisterView : UserControl
{
    public RegisterView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e) =>
        this.FindControl<TextBox>("NamaBox")?.Focus();
}
