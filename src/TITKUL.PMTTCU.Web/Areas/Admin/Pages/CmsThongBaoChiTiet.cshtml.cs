using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TITKUL.PMTTCU.Web.ApiClients;
using TITKUL.PMTTCU.Web.Observability;

namespace TITKUL.PMTTCU.Web.Areas.Admin.Pages;

public class CmsThongBaoChiTietModel : PageModel
{
    private readonly BackendApiClient _api;
    public CmsThongBaoChiTietModel(BackendApiClient api) => _api = api;
    public Guid? NoticeId { get; private set; }
    public string? ErrorMessage { get; private set; }
    public string? Status { get; private set; }
    public bool CanEdit { get; private set; }

    [BindProperty] public string Title { get; set; } = "";
    [BindProperty] public string? Slug { get; set; }
    [BindProperty] public string Html { get; set; } = "";
    [BindProperty] public string Level { get; set; } = "NORMAL";
    [BindProperty] public string? VisibleFrom { get; set; }
    [BindProperty] public string? VisibleTo { get; set; }

    public async Task<IActionResult> OnGetAsync(Guid? id)
    {
        if (!HasView()) return Redirect("/admin/khong-quyen");
        return await LoadAsync(id);
    }

    public async Task<IActionResult> OnPostSaveAsync(Guid? id)
    {
        if (!Has("cms.create") && !Has("cms.update")) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        var payload = new { title = Title, slug = Slug, html = Html, level = Level, visibleFrom = ToOffset(VisibleFrom), visibleTo = ToOffset(VisibleTo) };
        var response = id is Guid existing
            ? await _api.SendJsonAsync(HttpMethod.Put, "/api/v1/admin/notices/" + existing, token, payload)
            : await _api.SendJsonAsync(HttpMethod.Post, "/api/v1/admin/notices", token, payload);
        if (response is null || !response.IsSuccessStatusCode)
        {
            ErrorMessage = "Không lưu được thông báo. Kiểm tra tiêu đề, nội dung và cửa sổ hiển thị.";
            return await LoadAsync(id);
        }

        var saved = await response.Content.ReadFromJsonAsync<ItemEnvelope<Saved>>();
        return Redirect("/admin/cms/thong-bao/sua/" + saved!.Item!.Id);
    }

    public async Task<IActionResult> OnPostPublishAsync(Guid id)
    {
        if (!Has("cms.publish")) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        var response = await _api.SendJsonAsync(HttpMethod.Post, "/api/v1/admin/notices/" + id + "/publish", token, new { });
        if (response is null || !response.IsSuccessStatusCode) ErrorMessage = "Không đăng được thông báo.";
        return await LoadAsync(id);
    }

    public async Task<IActionResult> OnPostArchiveAsync(Guid id)
    {
        if (!Has("cms.publish")) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        await _api.SendJsonAsync(HttpMethod.Post, "/api/v1/admin/notices/" + id + "/archive", token, new { });
        return await LoadAsync(id);
    }

    private async Task<IActionResult> LoadAsync(Guid? id)
    {
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        CanEdit = Has("cms.create") || Has("cms.update");
        if (id is Guid existing)
        {
            NoticeId = existing;
            var body = await _api.GetJsonAsync<ItemEnvelope<NoticeDetail>>("/api/v1/admin/notices/" + existing, token);
            if (body?.Item is { } item)
            {
                Title = item.Title;
                Slug = item.Slug;
                Html = item.Html;
                Level = item.Level;
                VisibleFrom = LocalInput(item.VisibleFrom);
                VisibleTo = LocalInput(item.VisibleTo);
                Status = item.Status;
            }
            else ErrorMessage ??= "Không tìm thấy thông báo.";
        }

        return Page();
    }

    private bool HasView() => Has("cms.view") || Has("cms.create");
    private bool Has(string permission) => (HttpContext.Items["StaffProfile"] as StaffProfile)?.Permissions?.Contains(permission) == true;
    private string? Token() => Request.Cookies[AdminGateMiddleware.CookieName];
    private static DateTimeOffset? ToOffset(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : DateTimeOffset.TryParse(value + "+07:00", out var parsed) ? parsed : null;
    private static string LocalInput(DateTimeOffset? value) =>
        value is null ? "" : value.Value.ToOffset(TimeSpan.FromHours(7)).ToString("yyyy-MM-ddTHH:mm");

    public sealed record NoticeDetail(Guid Id, string Title, string Slug, string Html, string Level, string Status, DateTimeOffset? VisibleFrom, DateTimeOffset? VisibleTo);
    public sealed record Saved(Guid Id);
    private sealed record ItemEnvelope<T>(T? Item);
}
