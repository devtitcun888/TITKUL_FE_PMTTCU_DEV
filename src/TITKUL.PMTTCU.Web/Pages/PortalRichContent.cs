using System.Text;

namespace TITKUL.PMTTCU.Web.Pages;

/// <summary>
/// Adds an isolated, keyboard-accessible scroll region around tables in CMS HTML.
/// The CMS HTML must already have passed the backend sanitizer before it is rendered.
/// </summary>
public static class PortalRichContent
{
    private const string ScrollRegionStart = "<div class=\"portal-rich-table\" role=\"region\" tabindex=\"0\" aria-label=\"Bảng nội dung, có thể cuộn ngang\" aria-describedby=\"cms-table-scroll-hint\">";

    public static bool ContainsTable(string? html) => html is not null && FindTableTag(html, 0, closing: false) >= 0;

    public static string WithScrollableTables(string? html)
    {
        if (string.IsNullOrEmpty(html) || !ContainsTable(html)) return html ?? string.Empty;

        var output = new StringBuilder(html.Length + 160);
        var cursor = 0;
        while (cursor < html.Length)
        {
            var tableStart = FindTableTag(html, cursor, closing: false);
            if (tableStart < 0)
            {
                output.Append(html, cursor, html.Length - cursor);
                break;
            }

            var openingEnd = FindTagEnd(html, tableStart);
            if (openingEnd < 0)
            {
                output.Append(html, cursor, html.Length - cursor);
                break;
            }

            var tableEnd = FindMatchingTableEnd(html, openingEnd);
            if (tableEnd < 0) tableEnd = html.Length;

            output.Append(html, cursor, tableStart - cursor);
            output.Append(ScrollRegionStart);
            output.Append(html, tableStart, tableEnd - tableStart);
            output.Append("</div>");
            cursor = tableEnd;
        }

        return output.ToString();
    }

    private static int FindMatchingTableEnd(string html, int cursor)
    {
        var depth = 1;
        while (cursor < html.Length)
        {
            var opening = FindTableTag(html, cursor, closing: false);
            var closing = FindTableTag(html, cursor, closing: true);
            if (opening < 0 && closing < 0) return -1;

            var next = opening < 0 ? closing : closing < 0 ? opening : Math.Min(opening, closing);
            var tagEnd = FindTagEnd(html, next);
            if (tagEnd < 0) return -1;

            if (next == opening)
            {
                depth++;
            }
            else if (--depth == 0)
            {
                return tagEnd;
            }

            cursor = tagEnd;
        }

        return -1;
    }

    private static int FindTableTag(string html, int startIndex, bool closing)
    {
        var token = closing ? "</table" : "<table";
        var searchAt = startIndex;
        while (searchAt < html.Length)
        {
            var found = html.IndexOf(token, searchAt, StringComparison.OrdinalIgnoreCase);
            if (found < 0) return -1;

            var boundaryIndex = found + token.Length;
            if (boundaryIndex == html.Length || char.IsWhiteSpace(html[boundaryIndex]) || html[boundaryIndex] is '>' or '/')
            {
                return found;
            }

            searchAt = boundaryIndex;
        }

        return -1;
    }

    private static int FindTagEnd(string html, int tagStart)
    {
        char quote = '\0';
        for (var index = tagStart; index < html.Length; index++)
        {
            var character = html[index];
            if (quote != '\0')
            {
                if (character == quote) quote = '\0';
                continue;
            }

            if (character is '\'' or '"') quote = character;
            else if (character == '>') return index + 1;
        }

        return -1;
    }
}
