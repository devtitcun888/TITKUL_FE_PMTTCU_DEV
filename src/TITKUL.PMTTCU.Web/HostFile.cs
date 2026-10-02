using Microsoft.AspNetCore.Mvc;
using TITKUL.PMTTCU.Web.ApiClients;

namespace TITKUL.PMTTCU.Web;

public static class HostFile
{
    public const string Host = "repos-document.titkul.edu.vn";
    public const string Origin = "https://repos-document.titkul.edu.vn";

    public static bool IsPublicUrl(string? value) =>
        !string.IsNullOrWhiteSpace(value)
        && Uri.TryCreate(value, UriKind.Absolute, out var uri)
        && uri.Scheme == Uri.UriSchemeHttps
        && uri.Host.Equals(Host, StringComparison.OrdinalIgnoreCase);

    public static string Href(string? fileUrl, string fallback) =>
        IsPublicUrl(fileUrl) ? fileUrl! : fallback;

    public static IActionResult? RedirectIfPublic(string? fileUrl) =>
        IsPublicUrl(fileUrl) ? new RedirectResult(fileUrl!, permanent: false) : null;

    public static IActionResult Download(PublicFileResult file, string fallbackName, string fallbackMime = "application/octet-stream")
    {
        if (RedirectIfPublic(file.RedirectUrl) is { } redirect) return redirect;
        if (!file.IsSuccess) return file.IsNotFound ? new NotFoundResult() : new StatusCodeResult(StatusCodes.Status503ServiceUnavailable);
        return new FileContentResult(file.Bytes!, file.ContentType ?? fallbackMime)
        {
            FileDownloadName = file.FileName ?? fallbackName
        };
    }
}
