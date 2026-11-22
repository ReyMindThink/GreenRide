using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using GreenRide.UI.Helpers;

namespace GreenRide.UI.Views;

public partial class LeaderboardView : UserControl
{
    public LeaderboardView()
    {
        InitializeComponent();
        Responsive.Watch(this, 760, Apply);
    }

    /// <summary>Sempit: podium bertumpuk satu kolom dan baris tabel jadi dua baris.</summary>
    private void Apply(bool narrow)
    {
        Responsive.Set(this, "TitleSize", narrow ? 22d : 32d);
        Responsive.Set(this, "CardPadding", new Avalonia.Thickness(narrow ? 14 : 20));

        var podium = this.FindControl<ItemsControl>("PodiumList");
        if (podium is not null)
        {
            podium.ItemsPanel = new Avalonia.Controls.Templates.FuncTemplate<Avalonia.Controls.Panel?>(() =>
                narrow ? new StackPanel { Spacing = 8 } : new UniformGrid { Rows = 1 });
        }

        var header = this.FindControl<Grid>("RankHeader");
        var wide = this.FindControl<ItemsControl>("RankListWide");
        var slim = this.FindControl<ItemsControl>("RankListNarrow");
        if (header is not null) header.IsVisible = !narrow;
        if (wide is not null) wide.IsVisible = !narrow;
        if (slim is not null) slim.IsVisible = narrow;
    }
}
