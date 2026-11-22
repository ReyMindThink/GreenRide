using Avalonia;
using Avalonia.Controls;
using GreenRide.UI.Helpers;

namespace GreenRide.UI.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        UpdateSizeClass(Width);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ClientSizeProperty)
            UpdateSizeClass(ClientSize.Width);
    }

    /// <summary>
    /// Menandai jendela dengan pseudo-class :compact atau :medium agar style/layout bisa menyesuaikan
    /// (mis. sidebar menyempit) tanpa mengubah desain besarnya.
    /// </summary>
    private void UpdateSizeClass(double width)
    {
        PseudoClasses.Set(":compact", width < AppConfig.CompactWidth);
        PseudoClasses.Set(":medium", width >= AppConfig.CompactWidth && width < AppConfig.MediumWidth);
    }
}
