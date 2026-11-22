using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using GreenRide.UI.Helpers;

namespace GreenRide.UI.Views;

public partial class CatatView : UserControl
{
    public CatatView()
    {
        InitializeComponent();
        Responsive.Watch(this, 760, Apply);
    }

    /// <summary>
    /// Lebar (>= 760 px): dua kolom seperti desain. Sempit: satu kolom, urutan Catat, Badge, Koleksi,
    /// dan semua ukuran diperkecil. Penempatan grid diatur lewat kode karena nilai Grid.Row/Column
    /// yang tertulis di XAML tidak bisa ditimpa oleh Style.
    /// </summary>
    private void Apply(bool narrow)
    {
        // Ukuran
        Responsive.Set(this, "GreetingSize", narrow ? 22d : 32d);
        Responsive.Set(this, "CardTitleSize", narrow ? 18d : 22d);
        Responsive.Set(this, "BadgeBoxHeight", narrow ? 170d : 250d);
        Responsive.Set(this, "ShelfIconHeight", narrow ? 84d : 126d);
        Responsive.Set(this, "TimerSize", narrow ? 34d : 40d);
        Responsive.Set(this, "ModeCardHeight", narrow ? 176d : 242d);
        Responsive.Set(this, "ModeIconWidth", narrow ? 80d : 120d);
        Responsive.Set(this, "ModeIconHeight", narrow ? 64d : 100d);
        Responsive.Set(this, "ModeLabelSize", narrow ? 17d : 22d);
        Responsive.Set(this, "CardPadding", new Avalonia.Thickness(narrow ? 16 : 24));
        Responsive.Set(this, "TripCardPadding", narrow ? new Avalonia.Thickness(4, 20, 4, 4) : new Avalonia.Thickness(8, 24, 8, 8));
        Responsive.Set(this, "ModeInnerMargin", narrow ? new Avalonia.Thickness(4, 0, 4, 14) : new Avalonia.Thickness(8, 0, 8, 28));

        var root = this.FindControl<Grid>("Root");
        var badge = this.FindControl<Border>("BadgeCard");
        var shelf = this.FindControl<Border>("ShelfCard");
        var trip = this.FindControl<Border>("TripCard");
        var list = this.FindControl<ItemsControl>("ShelfList");
        if (root is null || badge is null || shelf is null || trip is null) return;

        if (narrow)
        {
            root.ColumnDefinitions = new ColumnDefinitions("*");
            Place(trip, 0, 0, 1, 1);
            Place(badge, 1, 0, 1, 1);
            Place(shelf, 2, 0, 1, 1);
            badge.MinHeight = 0;
            shelf.MinHeight = 0;
            trip.MinHeight = 0;
        }
        else
        {
            root.ColumnDefinitions = new ColumnDefinitions("528*,448*");
            Place(badge, 0, 0, 1, 1);
            Place(shelf, 1, 0, 1, 1);
            Place(trip, 0, 1, 2, 1);
            badge.MinHeight = 409;
            shelf.MinHeight = 266;
            trip.MinHeight = 683;
        }

        if (list is not null)
            list.ItemsPanel = new FuncTemplate<Panel?>(() => new UniformGrid { Columns = narrow ? 2 : 4 });
    }

    private static void Place(Control c, int row, int column, int rowSpan, int columnSpan)
    {
        Grid.SetRow(c, row);
        Grid.SetColumn(c, column);
        Grid.SetRowSpan(c, rowSpan);
        Grid.SetColumnSpan(c, columnSpan);
    }
}
