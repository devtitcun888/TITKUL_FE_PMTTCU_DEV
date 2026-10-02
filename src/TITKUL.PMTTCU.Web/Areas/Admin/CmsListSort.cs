using Microsoft.AspNetCore.WebUtilities;

namespace TITKUL.PMTTCU.Web.Areas.Admin;

public static class CmsListSort
{
    public static string? Normalize(string? sort, params string[] allowed)
    {
        var value = sort?.Trim().ToLowerInvariant();
        return value is not null && allowed.Contains(value) ? value : null;
    }

    public static string Dir(string? dir) =>
        string.Equals(dir, "desc", StringComparison.OrdinalIgnoreCase) ? "desc" : "asc";

    public static string NextDir(string? current, string column, string dir) =>
        string.Equals(current, column, StringComparison.OrdinalIgnoreCase) && dir == "asc" ? "desc" : "asc";

    public static void Append(IDictionary<string, string?> query, string? sort, string dir)
    {
        if (string.IsNullOrWhiteSpace(sort)) return;
        query["sort"] = sort;
        if (dir == "desc") query["dir"] = "desc";
    }

    public static void AppendParts(ICollection<string> parts, string? sort, string dir)
    {
        if (string.IsNullOrWhiteSpace(sort)) return;
        parts.Add("sort=" + Uri.EscapeDataString(sort));
        if (dir == "desc") parts.Add("dir=desc");
    }

    public static IReadOnlyList<T> Order<T>(IEnumerable<T> items, string dir, IOrderedEnumerable<T> ordered) =>
        (dir == "desc" ? ordered.Reverse() : (IEnumerable<T>)ordered).ToArray();

    public static string Path(string path, IDictionary<string, string?> query) =>
        QueryHelpers.AddQueryString(path, query);
}
