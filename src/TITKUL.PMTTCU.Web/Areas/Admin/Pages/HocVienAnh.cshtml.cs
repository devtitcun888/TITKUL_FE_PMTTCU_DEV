using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TITKUL.PMTTCU.Web.ApiClients;
using TITKUL.PMTTCU.Web.Observability;

namespace TITKUL.PMTTCU.Web.Areas.Admin.Pages;

public sealed class HocVienAnhModel(BackendApiClient api) : PageModel
{
    public Guid LearnerId { get; private set; }
    public string? LearnerName { get; private set; }
    public bool HasPhoto { get; private set; }
    public string? ErrorMessage { get; private set; }
    [BindProperty] public IFormFile? Upload { get; set; }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        if (!HasView()) return Redirect("/admin/khong-quyen");
        return await LoadAsync(id);
    }

    public async Task<IActionResult> OnGetFileAsync(Guid id)
    {
        if (!HasView()) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        var file = await api.GetFileResultAsync($"/api/v1/admin/learners/{id}/photo", token);
        if (HostFile.RedirectIfPublic(file.RedirectUrl) is { } redirect) return redirect;
        if (!file.IsSuccess) return file.IsNotFound ? NotFound() : StatusCode(StatusCodes.Status503ServiceUnavailable);
        if (file.ContentType is not ("image/jpeg" or "image/png")) return StatusCode(StatusCodes.Status415UnsupportedMediaType);
        Response.Headers.CacheControl = "private, no-store";
        return File(file.Bytes!, file.ContentType);
    }

    public async Task<IActionResult> OnPostAsync(Guid id)
    {
        if (!Has("learner.manage")) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        if (Upload is null || Upload.Length is <= 0 or > 5 * 1024 * 1024)
        {
            ErrorMessage = "Chọn ảnh PNG hoặc JPEG, tối đa 5 MiB.";
            return await LoadAsync(id);
        }

        using var content = new MultipartFormDataContent();
        await using var stream = Upload.OpenReadStream();
        using var file = new StreamContent(stream);
        file.Headers.ContentType = new MediaTypeHeaderValue(string.IsNullOrWhiteSpace(Upload.ContentType) ? "application/octet-stream" : Upload.ContentType);
        content.Add(file, "file", Path.GetFileName(Upload.FileName));
        using var response = await api.PostMultipartAsync($"/api/v1/admin/learners/{id}/photo", token, content);
        if (response is null || !response.IsSuccessStatusCode)
        {
            ErrorMessage = "Không lưu được ảnh hồ sơ.";
            return await LoadAsync(id);
        }

        return Redirect($"/admin/hoc-vien/{id}");
    }

    private async Task<IActionResult> LoadAsync(Guid id)
    {
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        LearnerId = id;
        var body = await api.GetJsonAsync<DetailEnvelope>($"/api/v1/admin/learners/{id}", token);
        if (body?.Item is null)
        {
            ErrorMessage ??= "Không tìm thấy học viên trong phạm vi của bạn.";
            return Page();
        }

        LearnerName = body.Item.FullName;
        HasPhoto = body.Item.HasPhoto;
        ViewData["Title"] = "Ảnh hồ sơ học viên";
        return Page();
    }

    private bool HasView() => Has("learner.view") || Has("learner.manage");
    private bool Has(string permission) => (HttpContext.Items["StaffProfile"] as StaffProfile)?.Permissions?.Contains(permission) == true;
    private string? Token() => Request.Cookies[AdminGateMiddleware.CookieName];

    private sealed record LearnerDetail(Guid Id, string FullName, bool HasPhoto = false);
    private sealed record DetailEnvelope(LearnerDetail? Item);
}
