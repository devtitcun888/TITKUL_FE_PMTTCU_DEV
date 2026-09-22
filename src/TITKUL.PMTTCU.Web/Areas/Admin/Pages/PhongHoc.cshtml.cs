using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TITKUL.PMTTCU.Web.ApiClients;
using TITKUL.PMTTCU.Web.Observability;

namespace TITKUL.PMTTCU.Web.Areas.Admin.Pages;

public class PhongHocModel : PageModel
{
    private readonly BackendApiClient _api;
    public PhongHocModel(BackendApiClient api) => _api = api;
    public IReadOnlyList<Item> Items { get; private set; } = [];
    public string? ErrorMessage { get; private set; }
    public bool CanManage { get; private set; }
    public bool Editing => Id.HasValue;
    [BindProperty] public Guid? Id { get; set; }
    [BindProperty] public string Code { get; set; } = "";
    [BindProperty] public string Name { get; set; } = "";
    [BindProperty] public string Location { get; set; } = "";
    [BindProperty] public int? Capacity { get; set; }
    [BindProperty] public bool Active { get; set; } = true;

    public async Task<IActionResult> OnGetAsync(Guid? id) => await LoadAsync(id);

    public async Task<IActionResult> OnPostAsync()
    {
        if (!HasManage()) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        var body = new { code = Code, name = Name, location = Location, capacity = Capacity, active = Active };
        var response = Id is Guid id
            ? await _api.SendJsonAsync(HttpMethod.Put, $"/api/v1/admin/rooms/{id}", token, body)
            : await _api.SendJsonAsync(HttpMethod.Post, "/api/v1/admin/rooms", token, body);
        if (response is null || !response.IsSuccessStatusCode)
        {
            ErrorMessage = "Không lưu được phòng học. Kiểm tra mã và sức chứa.";
            return await LoadAsync(Id);
        }

        return Redirect("/admin/phong-hoc");
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
        var body = await _api.GetJsonAsync<ListEnvelope<Item>>("/api/v1/admin/rooms?pageSize=100", token);
        if (body is null) ErrorMessage ??= "Không tải được danh sách.";
        Items = body?.Items ?? [];
        if (id is Guid editId)
        {
            var detail = await _api.GetJsonAsync<ItemEnvelope<Item>>($"/api/v1/admin/rooms/{editId}", token);
            if (detail?.Item is Item item)
            {
                Id = item.Id;
                Code = item.Code;
                Name = item.Name;
                Location = item.Location;
                Capacity = item.Capacity;
                Active = item.Active;
            }
        }

        return Page();
    }

    public sealed record Item(Guid Id, string Code, string Name, string Location, int? Capacity, bool Active);
    private sealed record ListEnvelope<T>(IReadOnlyList<T>? Items);
    private sealed record ItemEnvelope<T>(T? Item);
}
