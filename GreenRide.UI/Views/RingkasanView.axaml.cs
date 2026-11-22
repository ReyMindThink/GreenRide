using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using GreenRide.UI.Helpers;

namespace GreenRide.UI.Views;

public partial class RingkasanView : UserControl
{
    public RingkasanView()
    {
        InitializeComponent();

        // Template baris tabel mengikuti lebar tampilan, jadi dipasang dari kode.
        SetRowTemplate(narrow: false);
        Responsive.Watch(this, 760, Apply);
    }

    private void SetRowTemplate(bool narrow)
    {
        var list = this.FindControl<ItemsControl>("TripList");
        if (list is null) return;
        list.ItemTemplate = (IDataTemplate)Resources[narrow ? "NarrowRow" : "WideRow"]!;
    }

    /// <summary>
    /// Lebar (>= 760 px): grafik dan kalender berdampingan, tabel di bawahnya.
    /// Sempit: semua kartu bertumpuk, pemilih bulan di bawah judul, baris tabel dua baris.
    /// </summary>
    private void Apply(bool narrow)
    {
        Responsive.Set(this, "TitleSize", narrow ? 22d : 32d);
        Responsive.Set(this, "BarWidth", narrow ? 30d : 44d);
        Responsive.Set(this, "BigValueSize", narrow ? 24d : 28d);
        Responsive.Set(this, "CardPadding", new Thickness(narrow ? 14 : 20));
        SetRowTemplate(narrow);

        var header = this.FindControl<Grid>("HeaderGrid");
        var nav = this.FindControl<Border>("MonthNav");
        var root = this.FindControl<Grid>("Root");
        var chart = this.FindControl<Border>("ChartCard");
        var calendar = this.FindControl<Border>("CalendarCard");
        var trips = this.FindControl<Border>("TripsCard");
        var summary = this.FindControl<Grid>("SummaryGrid");
        var total = this.FindControl<StackPanel>("TotalBlock");
        var compare = this.FindControl<Border>("ComparePanel");
        var tripHeader = this.FindControl<Grid>("TripHeader");
        if (header is null || nav is null || root is null || chart is null || calendar is null ||
            trips is null || summary is null || total is null || compare is null) return;

        if (narrow)
        {
            header.ColumnDefinitions = new ColumnDefinitions("*");
            Place(nav, 1, 0, 1);
            nav.HorizontalAlignment = HorizontalAlignment.Stretch;

            root.ColumnDefinitions = new ColumnDefinitions("*");
            Place(chart, 0, 0, 1);
            Place(calendar, 1, 0, 1);
            Place(trips, 2, 0, 1);

            chart.Margin = new Thickness(0, 0, 0, 16);
            calendar.Margin = new Thickness(0, 0, 0, 16);
            trips.Margin = new Thickness(0);

            summary.ColumnDefinitions = new ColumnDefinitions("*");
            Place(total, 0, 0, 1);
            Place(compare, 1, 0, 1);
            compare.HorizontalAlignment = HorizontalAlignment.Stretch;
        }
        else
        {
            header.ColumnDefinitions = new ColumnDefinitions("*,Auto");
            Place(nav, 0, 1, 1);
            nav.HorizontalAlignment = HorizontalAlignment.Right;

            root.ColumnDefinitions = new ColumnDefinitions("520*,424*");
            Place(chart, 0, 0, 1);
            Place(calendar, 0, 1, 1);
            Place(trips, 1, 0, 2);

            chart.Margin = new Thickness(0, 0, 8, 8);
            calendar.Margin = new Thickness(8, 0, 0, 8);
            trips.Margin = new Thickness(0, 8, 0, 0);

            summary.ColumnDefinitions = new ColumnDefinitions("*,Auto");
            Place(total, 0, 0, 1);
            Place(compare, 0, 1, 1);
            compare.HorizontalAlignment = HorizontalAlignment.Right;
        }

        if (tripHeader is not null) tripHeader.IsVisible = !narrow;
    }

    private static void Place(Control c, int row, int column, int columnSpan)
    {
        Grid.SetRow(c, row);
        Grid.SetColumn(c, column);
        Grid.SetColumnSpan(c, columnSpan);
    }
}