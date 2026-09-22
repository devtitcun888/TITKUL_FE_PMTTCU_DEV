using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TITKUL.PMTTCU.Web.ApiClients;
using TITKUL.PMTTCU.Web.Observability;

namespace TITKUL.PMTTCU.Web.Areas.Admin.Pages;

public class ChuongTrinhModel : PageModel
{
    private readonly BackendApiClient _api;

    public ChuongTrinhModel(BackendApiClient api) => _api = api;

    public IReadOnlyList<Item> Items { get; private set; } = [];
    public string? ErrorMessage { get; private set; }
    public bool CanManage { get; private set; }
    public bool Editing => Id.HasValue;

    [BindProperty] public Guid? Id { get; set; }
    [BindProperty] public string Code { get; set; } = "";
    [BindProperty] public string Name { get; set; } = "";
    [BindProperty] public string Goal { get; set; } = "";
    [BindProperty] public string Status { get; set; } = "ACTIVE";

    public async Task<IActionResult> OnGetAsync(Guid? id) => await LoadAsync(id);

    public async Task<IActionResult> OnPostAsync()
    {
        if (!HasManage()) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        var body = new { code = Code, name = Name, goal = Goal, status = Status };
        var response = Id is Guid id
            ? await _api.SendJsonAsync(HttpMethod.Put, $"/api/v1/admin/programs/{id}", token, body)
            : await _api.SendJsonAsync(HttpMethod.Post, "/api/v1/admin/programs", token, body);
        if (response is null || !response.IsSuccessStatusCode)
        {
            ErrorMessage = "Không lưu được chương trình. Kiểm tra mã trùng hoặc dữ liệu bắt buộc.";
            return await LoadAsync(Id);
        }

        return Redirect("/admin/chuong-trinh");
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        if (!HasManage()) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        var response = await _api.SendJsonAsync(HttpMethod.Delete, $"/api/v1/admin/programs/{id}", token, new { });
        if (response is null || !response.IsSuccessStatusCode)
        {
            ErrorMessage = "Không xóa được. Chương trình còn lớp thì phải giữ lại.";
            return await LoadAsync(null);
        }

        return Redirect("/admin/chuong-trinh");
    }

    private bool HasManage() => Has("education.manage");
    private bool HasView() => Has("education.manage") || Has("education.view");
    private bool Has(string permission) => (HttpContext.Items["StaffProfile"] as StaffProfile)?.Permissions?.Contains(permission) == true;
    private string? Token() => Request.Cookies[AdminGateMiddleware.CookieName];

    private async Task<IActionResult> LoadAsync(Guid? id)
    {
        CanManage = HasManage();
        if (!HasView()) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        var body = await _api.GetJsonAsync<ListEnvelope<Item>>("/api/v1/admin/programs?pageSize=100", token);
        if (body is null) ErrorMessage ??= "Không tải được danh sách.";
        Items = body?.Items ?? [];
        if (id is Guid editId)
        {
            var detail = await _api.GetJsonAsync<ItemEnvelope<Detail>>($"/api/v1/admin/programs/{editId}", token);
            if (detail?.Item is Detail item)
            {
                Id = item.Id;
                Code = item.Code;
                Name = item.Name;
                Goal = item.Goal;
                Status = item.Status;
            }
        }

        return Page();
    }

    public sealed record Item(Guid Id, string Code, string Name, string Status);
    private sealed record Detail(Guid Id, string Code, string Name, string Goal, string Status);
    private sealed record ListEnvelope<T>(IReadOnlyList<T>? Items);
    private sealed record ItemEnvelope<T>(T? Item);
}
