using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TITKUL.PMTTCU.Web.ApiClients;

namespace TITKUL.PMTTCU.Web.Pages;

public class HocLieuChiTietModel : PageModel
{
    private readonly BackendApiClient _api;
    public HocLieuChiTietModel(BackendApiClient api) => _api = api;
    public AlbumHead? Item { get; private set; }
    public IReadOnlyList<MediaItem> Media { get; private set; } = [];
    public string? ErrorMessage { get; private set; }
    public bool ResourceNotFound { get; private set; }

    public async Task<IActionResult> OnGetAsync(string slug)
    {
        var result = await _api.GetPublicJsonResultAsync<DetailEnvelope>("/api/v1/public/albums/" + Uri.EscapeDataString(slug));
        var body = result.Value;
        if (body?.Item is null || body.Item.Kind != "HOC_LIEU")
        {
            var notFound = result.IsNotFound || result.IsAvailable;
            ResourceNotFound = notFound;
            ErrorMessage = notFound ? "Không tìm thấy học liệu." : "Chưa thể tải học liệu. Vui lòng thử lại sau.";
            Response.StatusCode = notFound ? StatusCodes.Status404NotFound : StatusCodes.Status503ServiceUnavailable;
            return Page();
        }

        Item = body.Item;
        Media = body.Media ?? [];
        ViewData["Title"] = Item.Title;
        ViewData["Description"] = Item.Summary;
        ViewData["CanonicalPath"] = "/hoc-lieu/" + Uri.EscapeDataString(Item.Slug);
        return Page();
    }

    public async Task<IActionResult> OnGetTaiAsync(string slug, Guid id)
    {
        var file = await _api.GetFileResultAsync("/api/v1/public/files/media/" + id);
        return HostFile.Download(file, "hoc-lieu");
    }

    public async Task<IActionResult> OnGetXemAsync(string slug, Guid id)
    {
        var detail = await _api.GetPublicJsonResultAsync<DetailEnvelope>("/api/v1/public/albums/" + Uri.EscapeDataString(slug));
        var item = detail.Value?.Media?.FirstOrDefault(file => file.FileId == id);
        if (item is null || !string.Equals(item.MimeType, "application/pdf", StringComparison.OrdinalIgnoreCase) || item.ViewMode == "DOWNLOAD_ONLY") return NotFound();
        if (HostFile.RedirectIfPublic(item.FileUrl) is { } jump) return jump;
        var file = await _api.GetFileResultAsync("/api/v1/public/files/media/" + id + "?inline=true");
        if (HostFile.RedirectIfPublic(file.RedirectUrl) is { } redirect) return redirect;
        if (!file.IsSuccess) return file.IsNotFound ? NotFound() : StatusCode(503);
        var bytes = file.Bytes!;
        if (bytes.Length < 5 || bytes[0] != 0x25 || bytes[1] != 0x50 || bytes[2] != 0x44 || bytes[3] != 0x46 || bytes[4] != 0x2D) return StatusCode(415);
        Response.Headers["Content-Disposition"] = "inline";
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        Response.Headers["Cache-Control"] = "private, no-store";
        return new FileContentResult(bytes, "application/pdf") { EnableRangeProcessing = true };
    }

    public sealed record MediaItem(string Kind, string? Title, Guid? FileId, string? MimeType = null, long? Size = null, string ViewMode = "AUTO", string? FileUrl = null);
    public sealed record AlbumHead(string Title, string Slug, string Kind, string? Summary);
    private sealed record DetailEnvelope(AlbumHead? Item, IReadOnlyList<MediaItem>? Media);

    public static string FileType(string? mimeType) => mimeType switch
    {
        "application/pdf" => "PDF",
        "application/msword" or "application/vnd.openxmlformats-officedocument.wordprocessingml.document" => "Word",
        "application/vnd.ms-excel" or "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" => "Excel",
        "application/vnd.ms-powerpoint" or "application/vnd.openxmlformats-officedocument.presentationml.presentation" => "PowerPoint",
        "text/plain" => "Văn bản",
        _ => "Tệp tải xuống"
    };

    public static string FileSize(long size)
    {
        if (size < 1024) return $"{size} B";
        var kibibytes = size / 1024d;
        if (kibibytes < 1024) return $"{kibibytes:0.#} KiB";
        return $"{kibibytes / 1024d:0.#} MiB";
    }
}
