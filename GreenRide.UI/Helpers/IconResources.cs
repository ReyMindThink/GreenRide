using Avalonia;
using Avalonia.Media;

namespace GreenRide.UI.Helpers;

/// <summary>Mengambil ikon garis dari Styles/Icons.axaml berdasarkan kunci, mis. "Icon.Catat".</summary>
public static class IconResources
{
    public static Geometry? Get(string key)
    {
        if (Application.Current is { } app &&
            app.Resources.TryGetResource(key, app.ActualThemeVariant, out var value))
        {
            return value as Geometry;
        }

        return null;
    }
}