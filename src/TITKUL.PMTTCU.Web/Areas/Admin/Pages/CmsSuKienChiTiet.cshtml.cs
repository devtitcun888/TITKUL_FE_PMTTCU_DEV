using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TITKUL.PMTTCU.Web.ApiClients;
using TITKUL.PMTTCU.Web.Observability;

namespace TITKUL.PMTTCU.Web.Areas.Admin.Pages;

public class CmsSuKienChiTietModel : PageModel
{
    private readonly BackendApiClient _api;
    public CmsSuKienChiTietModel(BackendApiClient api) => _api = api;
    public Guid? EventId { get; private set; }
    public string? ErrorMessage { get; private set; }
    public string? Status { get; private set; }
    public bool CanEdit { get; private set; }

    [BindProperty] public string Title { get; set; } = "";
    [BindProperty] public string? Slug { get; set; }
    [BindProperty] public string? Summary { get; set; }
    [BindProperty] public string? Html { get; set; }
    [BindProperty] public string? Location { get; set; }
    [BindProperty] public string StartAt { get; set; } = "";
    [BindProperty] public string EndAt { get; set; } = "";

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
        var payload = new { title = Title, slug = Slug, summary = Summary, html = Html, location = Location, startAt = ToOffset(StartAt), endAt = ToOffset(EndAt) };
        var response = id is Guid existing
            ? await _api.SendJsonAsync(HttpMethod.Put, "/api/v1/admin/events/" + existing, token, payload)
            : await _api.SendJsonAsync(HttpMethod.Post, "/api/v1/admin/events", token, payload);
        if (response is null || !response.IsSuccessStatusCode)
        {
            ErrorMessage = "Không lưu được sự kiện. Kiểm tra tiêu đề và giờ kết thúc phải sau giờ bắt đầu.";
            return await LoadAsync(id);
        }

        var saved = await response.Content.ReadFromJsonAsync<ItemEnvelope<Saved>>();
        return Redirect("/admin/cms/su-kien/sua/" + saved!.Item!.Id);
    }

    public async Task<IActionResult> OnPostPublishAsync(Guid id)
    {
        if (!Has("cms.publish")) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        var response = await _api.SendJsonAsync(HttpMethod.Post, "/api/v1/admin/events/" + id + "/publish", token, new { });
        if (response is null || !response.IsSuccessStatusCode) ErrorMessage = "Không đăng được sự kiện.";
        return await LoadAsync(id);
    }

    public async Task<IActionResult> OnPostCancelAsync(Guid id)
    {
        if (!Has("cms.publish")) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        await _api.SendJsonAsync(HttpMethod.Post, "/api/v1/admin/events/" + id + "/cancel", token, new { });
        return await LoadAsync(id);
    }

    private async Task<IActionResult> LoadAsync(Guid? id)
    {
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        CanEdit = Has("cms.create") || Has("cms.update");
        if (id is Guid existing)
        {
            EventId = existing;
            var body = await _api.GetJsonAsync<ItemEnvelope<EventDetail>>("/api/v1/admin/events/" + existing, token);
            if (body?.Item is { } item)
            {
                Title = item.Title;
                Slug = item.Slug;
                Summary = item.Summary;
                Html = item.Html;
                Location = item.Location;
                StartAt = LocalInput(item.StartAt);
                EndAt = LocalInput(item.EndAt);
                Status = item.Status;
            }
            else ErrorMessage ??= "Không tìm thấy sự kiện.";
        }

        return Page();
    }

    private bool HasView() => Has("cms.view") || Has("cms.create");
    private bool Has(string permission) => (HttpContext.Items["StaffProfile"] as StaffProfile)?.Permissions?.Contains(permission) == true;
    private string? Token() => Request.Cookies[AdminGateMiddleware.CookieName];
    private static DateTimeOffset? ToOffset(string value) =>
        DateTimeOffset.TryParse(value + "+07:00", out var parsed) ? parsed : null;
    private static string LocalInput(DateTimeOffset value) =>
        value.ToOffset(TimeSpan.FromHours(7)).ToString("yyyy-MM-ddTHH:mm");

    public sealed record EventDetail(Guid Id, string Title, string Slug, string? Summary, string? Html, DateTimeOffset StartAt, DateTimeOffset EndAt, string? Location, string Status);
    public sealed record Saved(Guid Id);
    private sealed record ItemEnvelope<T>(T? Item);
}
