using Avalonia.Controls;

namespace GreenRide.UI.Helpers;

/// <summary>
/// Membantu sebuah layar menyesuaikan tata letak dengan lebarnya sendiri (bukan lebar jendela).
/// Dipanggil sekali dari konstruktor view. <paramref name="apply"/> dijalankan saat pertama kali
/// lebar diketahui dan setiap kali status sempit/lebar berubah.
/// </summary>
public static class Responsive
{
    public static void Watch(Control view, double breakpoint, Action<bool> apply)
    {
        bool? last = null;

        view.SizeChanged += (_, e) =>
        {
            var width = e.NewSize.Width;
            if (width <= 0) return;

            var narrow = width < breakpoint;
            if (last == narrow) return;
            last = narrow;

            if (narrow)
            {
                if (!view.Classes.Contains("narrow")) view.Classes.Add("narrow");
            }
            else
            {
                view.Classes.Remove("narrow");
            }

            apply(narrow);
        };
    }

    /// <summary>Mengubah nilai resource lokal (mis. ukuran) yang dipakai lewat {DynamicResource}.</summary>
    public static void Set(Control view, string key, object value) => view.Resources[key] = value;
}
