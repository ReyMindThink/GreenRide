using Avalonia;
using Avalonia.Controls;
using GreenRide.UI.ViewModels;

namespace GreenRide.UI.Views;

public partial class ShellView : UserControl
{
    /// <summary>Lebar (px) mulai dari mana layout "lebar": navigasi samping + dashboard 2 kolom.</summary>
    private const double WideBreakpoint = 860;

    public ShellView() => InitializeComponent();

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == BoundsProperty || change.Property == DataContextProperty)
            ApplyAdaptiveLayout();
    }

    private void ApplyAdaptiveLayout()
    {
        if (DataContext is ShellViewModel vm && Bounds.Width > 0)
            vm.IsWide = Bounds.Width >= WideBreakpoint;
    }
}
