namespace GreenRide.UI.Helpers;

public static class AssetUri
{
    /// <summary>"/Assets/x.svg" menjadi "avares://GreenRide.UI/Assets/x.svg" (aman dipakai di binding Svg.Path).</summary>
    public static string Of(string assetPath) =>
        assetPath.StartsWith("avares://", StringComparison.Ordinal)
            ? assetPath
            : "avares://GreenRide.UI/" + assetPath.TrimStart('/');
}
