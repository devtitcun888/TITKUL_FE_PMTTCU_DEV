using System.Globalization;
using System.Text.RegularExpressions;

namespace TITKUL.PMTTCU.Web.Pages;

public static partial class GoogleMapsUrl
{
    public static string? Link(string? value, string? fallbackQuery = null)
    {
        if (!TryMapsUri(value, out var uri))
            return FallbackLink(fallbackQuery);

        if (TryQuery(uri, "q", out var query) && query.Length > 0)
            return PlaceLink(query);
        if (TryCoordinatesFromPb(uri.Query, out var lat, out var lng))
            return PlaceLink(lat.ToString("0.######", CultureInfo.InvariantCulture) + "," + lng.ToString("0.######", CultureInfo.InvariantCulture));
        if (TryQuery(uri, "query", out query) && query.Length > 0)
            return PlaceLink(query);

        return FallbackLink(fallbackQuery);
    }

    public static string? Embed(string? value)
    {
        if (!TryMapsUri(value, out var uri))
            return null;

        if (uri.AbsolutePath.Contains("/maps/embed/v1", StringComparison.OrdinalIgnoreCase))
            return null;

        if (uri.AbsolutePath.StartsWith("/maps/embed", StringComparison.OrdinalIgnoreCase))
            return uri.AbsoluteUri;

        if (TryQuery(uri, "q", out var query) && TryCoordinates(query, out var lat, out var lng))
            return PbEmbed(lat, lng);

        if (TryQuery(uri, "q", out query) && query.Length > 0)
            return "https://maps.google.com/maps?q=" + Uri.EscapeDataString(query) + "&hl=vi&z=16&output=embed";

        if (TryCoordinatesFromPb(uri.Query, out lat, out lng))
            return PbEmbed(lat, lng);

        return HasOutputEmbed(uri) ? uri.AbsoluteUri : null;
    }

    private static bool TryMapsUri(string? value, out Uri uri)
    {
        uri = null!;
        if (!Uri.TryCreate(value, UriKind.Absolute, out var parsed) || parsed.Scheme != Uri.UriSchemeHttps)
            return false;
        var host = parsed.IdnHost.ToLowerInvariant();
        if (host is not ("maps.google.com" or "www.google.com" or "www.google.com.vn"))
            return false;
        uri = parsed;
        return true;
    }

    private static string PlaceLink(string query) =>
        "https://www.google.com/maps/search/?api=1&query=" + Uri.EscapeDataString(query);

    private static string? FallbackLink(string? fallbackQuery) =>
        string.IsNullOrWhiteSpace(fallbackQuery) ? null : PlaceLink(fallbackQuery.Trim());

    private static string PbEmbed(double lat, double lng)
    {
        var latText = lat.ToString("0.######", CultureInfo.InvariantCulture);
        var lngText = lng.ToString("0.######", CultureInfo.InvariantCulture);
        return "https://www.google.com/maps/embed?pb=!1m18!1m12!1m3!1d2000!2d" + lngText
            + "!3d" + latText
            + "!2m3!1f0!2f0!3f0!3m2!1i1024!2i768!4f13.1!3m3!1m2!1s0x0:0x0!2s"
            + latText + "%2C%20" + lngText
            + "!5e0!3m2!1svi!2svn";
    }

    private static bool TryQuery(Uri uri, string name, out string value)
    {
        value = uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => part.Split('=', 2))
            .Where(parts => parts.Length == 2 && Uri.UnescapeDataString(parts[0]).Equals(name, StringComparison.OrdinalIgnoreCase))
            .Select(parts => Uri.UnescapeDataString(parts[1]).Replace('+', ' ').Trim())
            .FirstOrDefault() ?? "";
        return value.Length > 0;
    }

    private static bool HasOutputEmbed(Uri uri) =>
        uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => part.Split('=', 2))
            .Any(parts => parts.Length == 2
                && Uri.UnescapeDataString(parts[0]).Equals("output", StringComparison.OrdinalIgnoreCase)
                && Uri.UnescapeDataString(parts[1]).Equals("embed", StringComparison.OrdinalIgnoreCase));

    private static bool TryCoordinates(string value, out double lat, out double lng)
    {
        lat = 0;
        lng = 0;
        var match = CoordinatePattern().Match(value);
        return match.Success
            && double.TryParse(match.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out lat)
            && double.TryParse(match.Groups[2].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out lng);
    }

    private static bool TryCoordinatesFromPb(string query, out double lat, out double lng)
    {
        lat = 0;
        lng = 0;
        var match = PbCoordinatePattern().Match(Uri.UnescapeDataString(query));
        return match.Success
            && double.TryParse(match.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out lng)
            && double.TryParse(match.Groups[2].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out lat);
    }

    [GeneratedRegex(@"^\s*(-?\d+(?:\.\d+)?)\s*,\s*(-?\d+(?:\.\d+)?)\s*$", RegexOptions.CultureInvariant)]
    private static partial Regex CoordinatePattern();

    [GeneratedRegex(@"!2d(-?\d+(?:\.\d+)?)!3d(-?\d+(?:\.\d+)?)", RegexOptions.CultureInvariant)]
    private static partial Regex PbCoordinatePattern();
}
