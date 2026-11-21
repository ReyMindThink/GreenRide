using System.Globalization;

namespace GreenRide.UI.Models;

public static class Fmt
{
    public static readonly CultureInfo Id = new("id-ID");

    public static string Kg(double v)  => v.ToString("0.00", Id) + " kg CO₂";
    public static string Kg(decimal v) => Kg((double)v);
}
