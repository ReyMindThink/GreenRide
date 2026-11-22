using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using GreenRide.UI.Helpers;

namespace GreenRide.UI.Views;

public partial class RevisiView : UserControl
{
    public RevisiView()
    {
        InitializeComponent();
        Responsive.Watch(this, 760, Apply);
    }

    /// <summary>Lebar: form dan pratinjau berdampingan. Sempit: bertumpuk, pilihan moda 2 x 2.</summary>
    private void Apply(bool narrow)
    {
        Responsive.Set(this, "TitleSize", narrow ? 22d : 32d);
        Responsive.Set(this, "CardPadding", new Avalonia.Thickness(narrow ? 16 : 24));

        var root = this.FindControl<Grid>("Root");
        var form = this.FindControl<Border>("FormCard");
        var preview = this.FindControl<Border>("PreviewCard");
        var modes = this.FindControl<ItemsControl>("ModeList");
        if (root is null || form is null || preview is null) return;

        if (narrow)
        {
            root.ColumnDefinitions = new ColumnDefinitions("*");
            Grid.SetRow(form, 0);
            Grid.SetColumn(form, 0);
            Grid.SetRow(preview, 1);
            Grid.SetColumn(preview, 0);
        }
        else
        {
            root.ColumnDefinitions = new ColumnDefinitions("620*,364*");
            Grid.SetRow(form, 0);
            Grid.SetColumn(form, 0);
            Grid.SetRow(preview, 0);
            Grid.SetColumn(preview, 1);
        }

        if (modes is not null)
            modes.ItemsPanel = new FuncTemplate<Panel?>(() => new UniformGrid { Columns = narrow ? 2 : 4 });
    }
}
