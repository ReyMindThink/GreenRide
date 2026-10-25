using Avalonia;
using Avalonia.Controls;

namespace GreenRide.UI.Controls;

public partial class BadgeIcon : UserControl
{
    public static readonly StyledProperty<string> KindProperty =
        AvaloniaProperty.Register<BadgeIcon, string>(nameof(Kind), "Locked");

    /// <summary>Pemula | Hemat | Pahlawan | Locked</summary>
    public string Kind
    {
        get => GetValue(KindProperty);
        set => SetValue(KindProperty, value);
    }

    public BadgeIcon() => InitializeComponent();
}
