using System.Globalization;

namespace GreenRide.UI.Models;

public static class Fmt
{
    public static readonly CultureInfo Id = new("id-ID");
    public static string Kg(double v) => v.ToString("0.00", Id) + " kg CO₂";
}
