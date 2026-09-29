using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TITKUL.PMTTCU.Web.ApiClients;
using TITKUL.PMTTCU.Web.Observability;

namespace TITKUL.PMTTCU.Web.Areas.Admin.Pages;

public sealed class CmsImageUploadModel(BackendApiClient api) : PageModel
{
    public async Task<IActionResult> OnPostAsync(IFormFile? upload, IFormFile? thumbnail)
    {
        var canEdit = (HttpContext.Items["StaffProfile"] as StaffProfile)?.Permissions?.Any(permission => permission is "cms.create" or "cms.update") == true;
        if (!canEdit) return StatusCode(StatusCodes.Status403Forbidden, new { error = new { message = "Không có quyền tải ảnh lên." } });
        var token = Request.Cookies[AdminGateMiddleware.CookieName];
        if (token is null) return StatusCode(StatusCodes.Status401Unauthorized, new { error = new { message = "Phiên đăng nhập đã hết hạn." } });
        if (upload is null || upload.Length is <= 0 or > 5 * 1024 * 1024)
            return BadRequest(new { error = new { message = "Chọn ảnh PNG hoặc JPEG, tối đa 5 MiB." } });

        using var content = new MultipartFormDataContent();
        await using var stream = upload.OpenReadStream();
        using var file = new StreamContent(stream);
        file.Headers.ContentType = new MediaTypeHeaderValue(string.IsNullOrWhiteSpace(upload.ContentType) ? "application/octet-stream" : upload.ContentType);
        content.Add(file, "file", Path.GetFileName(upload.FileName));
        if (thumbnail is not null)
        {
            if (thumbnail.Length is <= 0 or > 5 * 1024 * 1024)
                return BadRequest(new { error = new { message = "Ảnh thumbnail phải nhỏ hơn 5 MiB." } });
            var thumbnailStream = thumbnail.OpenReadStream();
            var thumbnailContent = new StreamContent(thumbnailStream);
            thumbnailContent.Headers.ContentType = new MediaTypeHeaderValue(string.IsNullOrWhiteSpace(thumbnail.ContentType) ? "application/octet-stream" : thumbnail.ContentType);
            content.Add(thumbnailContent, "thumbnail", Path.GetFileName(thumbnail.FileName));
        }
        using var response = await api.PostMultipartAsync("/api/v1/admin/cms-images", token, content);
        if (response is null || !response.IsSuccessStatusCode)
            return BadRequest(new { error = new { message = "Ảnh không hợp lệ hoặc máy chủ không thể lưu ảnh." } });

        using var body = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        var imageUrl = body.RootElement.TryGetProperty("imageUrl", out var url) ? url.GetString() : null;
        var storageKey = body.RootElement.TryGetProperty("storageKey", out var key) ? key.GetString() : null;
        var thumbnailUrl = body.RootElement.TryGetProperty("thumbnailUrl", out var thumbUrl) ? thumbUrl.GetString() : imageUrl;
        var thumbnailKey = body.RootElement.TryGetProperty("thumbnailKey", out var thumbKey) ? thumbKey.GetString() : storageKey;
        return string.IsNullOrWhiteSpace(imageUrl)
            ? BadRequest(new { error = new { message = "Máy chủ chưa trả đường dẫn ảnh." } })
            : new JsonResult(new { urls = new Dictionary<string, string> { ["default"] = imageUrl, ["thumbnail"] = thumbnailUrl! }, storageKey, thumbnailKey });
    }
}
