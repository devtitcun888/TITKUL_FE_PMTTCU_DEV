using System.Text.RegularExpressions;

namespace TITKUL.PMTTCU.Web.Pages;

public static partial class VideoEmbedUrl
{
    public static string? From(string? value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
            return null;

        var host = uri.Host.ToLowerInvariant();
        if (host is "youtu.be" or "www.youtu.be" or "youtube.com" or "www.youtube.com" or "m.youtube.com")
        {
            var id = host.EndsWith("youtu.be", StringComparison.Ordinal)
                ? uri.AbsolutePath.Trim('/')
                : uri.AbsolutePath == "/watch"
                    ? GetQuery(uri.Query, "v")
                    : uri.AbsolutePath.StartsWith("/embed/", StringComparison.Ordinal)
                        ? uri.AbsolutePath[7..]
                        : "";
            return id is not null && VideoIdPattern().IsMatch(id) ? "https://www.youtube-nocookie.com/embed/" + id : null;
        }

        if (host is "facebook.com" or "www.facebook.com" or "m.facebook.com" or "fb.watch")
        {
            if (!uri.AbsolutePath.Contains("/videos/", StringComparison.OrdinalIgnoreCase) && host != "fb.watch")
                return null;
            return "https://www.facebook.com/plugins/video.php?href=" + Uri.EscapeDataString(uri.ToString()) + "&show_text=false";
        }

        return null;
    }

    private static string? GetQuery(string query, string name) =>
        query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(pair => pair.Split('=', 2))
            .Where(parts => parts.Length == 2 && Uri.UnescapeDataString(parts[0]) == name)
            .Select(parts => Uri.UnescapeDataString(parts[1]))
            .FirstOrDefault();

    [GeneratedRegex("^[A-Za-z0-9_-]{11}$", RegexOptions.CultureInvariant)]
    private static partial Regex VideoIdPattern();
}
