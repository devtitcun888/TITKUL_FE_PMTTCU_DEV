using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TITKUL.PMTTCU.Web.ApiClients;
using TITKUL.PMTTCU.Web.Observability;

namespace TITKUL.PMTTCU.Web.Areas.Admin.Pages;

public sealed class XoaMuChuModel : PageModel
{
    private readonly BackendApiClient _api;
    public XoaMuChuModel(BackendApiClient api) => _api = api;

    public IReadOnlyList<EntryItem> Entries { get; private set; } = [];
    public string? ErrorMessage { get; private set; }
    public string? SuccessMessage { get; private set; }
    [BindProperty(SupportsGet = true)] public Guid? EntryId { get; set; }
    [BindProperty] public string? SourceSheet { get; set; }
    [BindProperty] public int? Sequence { get; set; }
    [BindProperty] public string? FamilyName { get; set; }
    [BindProperty] public string? GivenName { get; set; }
    [BindProperty] public bool Female { get; set; }
    [BindProperty] public string? Occupation { get; set; }
    [BindProperty] public string? Position { get; set; }
    [BindProperty] public string? Address { get; set; }
    [BindProperty] public int? IlliterateYear { get; set; }
    [BindProperty] public int? Grade1Year { get; set; }
    [BindProperty] public int? Grade2Year { get; set; }
    [BindProperty] public int? Grade3Year { get; set; }
    [BindProperty] public int? LiterateYear { get; set; }
    [BindProperty] public int? Grade4Year { get; set; }
    [BindProperty] public int? Grade5Year { get; set; }
    [BindProperty] public int? PrimaryCompleteYear { get; set; }
    [BindProperty] public string? Note { get; set; }
    [BindProperty] public IFormFile? ImportFile { get; set; }

    public Task<IActionResult> OnGetAsync(Guid? entryId) => LoadAsync(entryId);

    public async Task<IActionResult> OnPostImportAsync()
    {
        if (!CanManage()) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        if (ImportFile is null || ImportFile.Length == 0)
        {
            ErrorMessage = "Chọn workbook Excel sổ theo dõi xóa mù chữ (.xlsx).";
            return await LoadAsync(null);
        }
        using var content = new MultipartFormDataContent();
        await using var stream = ImportFile.OpenReadStream();
        using var fileContent = new StreamContent(stream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(ImportFile.ContentType ?? "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        content.Add(fileContent, "file", ImportFile.FileName);
        var response = await _api.PostMultipartAsync("/api/v1/admin/literacy-progress/import", token, content);
        if (response is null || !response.IsSuccessStatusCode)
        {
            ErrorMessage = await ReadErrorAsync(response) ?? "Không nhập được workbook.";
            return await LoadAsync(null);
        }
        var result = await response.Content.ReadFromJsonAsync<ImportResult>();
        SuccessMessage = $"Đã nhập {result?.Created ?? 0} dòng; bỏ qua {result?.SkippedBlankRows ?? 0} dòng trống.";
        return await LoadAsync(null);
    }

    public async Task<IActionResult> OnPostSaveEntryAsync()
    {
        if (!CanManage()) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        var body = new { sourceSheet = SourceSheet, sequence = Sequence, familyName = FamilyName, givenName = GivenName, female = Female,
            occupation = Occupation, position = Position, address = Address, illiterateYear = IlliterateYear, grade1Year = Grade1Year,
            grade2Year = Grade2Year, grade3Year = Grade3Year, literateYear = LiterateYear, grade4Year = Grade4Year,
            grade5Year = Grade5Year, primaryCompleteYear = PrimaryCompleteYear, note = Note };
        var response = EntryId is Guid entryId
            ? await _api.SendJsonAsync(HttpMethod.Put, $"/api/v1/admin/literacy-progress/{entryId}", token, body)
            : await _api.SendJsonAsync(HttpMethod.Post, "/api/v1/admin/literacy-progress", token, body);
        if (response is null || !response.IsSuccessStatusCode)
        {
            ErrorMessage = await ReadErrorAsync(response) ?? "Không lưu được dòng theo dõi.";
            return await LoadAsync(EntryId);
        }
        return Redirect("/admin/xoa-mu-chu");
    }

    public async Task<IActionResult> OnPostDeleteEntryAsync(Guid entryId)
    {
        if (!CanManage()) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        var response = await _api.SendJsonAsync(HttpMethod.Delete, $"/api/v1/admin/literacy-progress/{entryId}", token, new { });
        if (response is null || !response.IsSuccessStatusCode)
        {
            ErrorMessage = await ReadErrorAsync(response) ?? "Không xóa được dòng dữ liệu.";
            return await LoadAsync(null);
        }
        return Redirect("/admin/xoa-mu-chu");
    }

    public async Task<IActionResult> OnGetExportAsync()
    {
        if (!CanManage()) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        var file = await _api.GetFileAsync("/api/v1/admin/literacy-progress/export", token);
        return file.Bytes is null ? NotFound() : File(file.Bytes, file.ContentType ?? "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", file.FileName ?? "so-theo-doi-xoa-mu-chu.xlsx");
    }

    private bool CanManage() => (HttpContext.Items["StaffProfile"] as StaffProfile)?.Permissions?.Contains("education.manage") == true;
    private string? Token() => Request.Cookies[AdminGateMiddleware.CookieName];

    private async Task<IActionResult> LoadAsync(Guid? entryId)
    {
        if (!CanManage()) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        var response = await _api.GetJsonAsync<ListEnvelope<EntryItem>>("/api/v1/admin/literacy-progress", token);
        Entries = response?.Items ?? [];
        if (entryId is Guid id && Entries.FirstOrDefault(row => row.Id == id) is { } item) LoadEntry(item);
        return Page();
    }

    private void LoadEntry(EntryItem item)
    {
        EntryId = item.Id; SourceSheet = item.SourceSheet; Sequence = item.Sequence; FamilyName = item.FamilyName; GivenName = item.GivenName;
        Female = item.Female; Occupation = item.Occupation; Position = item.Position; Address = item.Address; IlliterateYear = item.IlliterateYear;
        Grade1Year = item.Grade1Year; Grade2Year = item.Grade2Year; Grade3Year = item.Grade3Year; LiterateYear = item.LiterateYear;
        Grade4Year = item.Grade4Year; Grade5Year = item.Grade5Year; PrimaryCompleteYear = item.PrimaryCompleteYear; Note = item.Note;
    }

    private static async Task<string?> ReadErrorAsync(HttpResponseMessage? response)
    {
        if (response is null) return null;
        try { return (await response.Content.ReadFromJsonAsync<ApiErrorBody>())?.Message; }
        catch (Exception) { return null; }
    }

    public sealed record EntryItem(Guid Id, string SourceSheet, int Sequence, string? FamilyName, string? GivenName, bool Female,
        string? Occupation, string? Position, string? Address, int? IlliterateYear, int? Grade1Year, int? Grade2Year, int? Grade3Year,
        int? LiterateYear, int? Grade4Year, int? Grade5Year, int? PrimaryCompleteYear, string? Note);
    private sealed record ListEnvelope<T>(IReadOnlyList<T>? Items);
    private sealed record ImportResult(int Created, int SkippedBlankRows);
    private sealed record ApiErrorBody(string? Code, string? Message);
}
