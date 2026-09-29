using System.Net;
using System.Text.RegularExpressions;

namespace TITKUL.PMTTCU.Web.Pages;

public static partial class CmsMetaText
{
    public static string FromHtml(string? html, int maxLength = 180)
    {
        if (string.IsNullOrWhiteSpace(html)) return string.Empty;
        var text = WebUtility.HtmlDecode(TagPattern().Replace(html, " "));
        text = WhitespacePattern().Replace(text, " ").Trim();
        if (text.Length <= maxLength) return text;
        var cut = text.LastIndexOf(' ', maxLength - 1);
        return text[..(cut > maxLength / 2 ? cut : maxLength)].TrimEnd() + "…";
    }

    [GeneratedRegex("<[^>]*>", RegexOptions.CultureInvariant)]
    private static partial Regex TagPattern();

    [GeneratedRegex("\\s+", RegexOptions.CultureInvariant)]
    private static partial Regex WhitespacePattern();
}
