using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TITKUL.PMTTCU.Web.ApiClients;
using TITKUL.PMTTCU.Web.Observability;

namespace TITKUL.PMTTCU.Web.Areas.Admin.Pages;

public sealed class DieuTraNhuCauModel : PageModel
{
    private readonly BackendApiClient _api;
    public DieuTraNhuCauModel(BackendApiClient api) => _api = api;

    public IReadOnlyList<RoundItem> Rounds { get; private set; } = [];
    public IReadOnlyList<EntryItem> Entries { get; private set; } = [];
    public RoundItem? SelectedRound { get; private set; }
    public SummaryItem? Summary { get; private set; }
    public string? ErrorMessage { get; private set; }
    public string? SuccessMessage { get; private set; }

    [BindProperty(SupportsGet = true)] public Guid? Id { get; set; }
    [BindProperty(SupportsGet = true)] public Guid? EntryId { get; set; }
    [BindProperty] public int? Year { get; set; }
    [BindProperty] public string RoundTitle { get; set; } = "";
    [BindProperty] public string? SourceSheet { get; set; }
    [BindProperty] public int? Sequence { get; set; }
    [BindProperty] public string? LocationLabel { get; set; }
    [BindProperty] public string? FullName { get; set; }
    [BindProperty] public bool Female { get; set; }
    [BindProperty] public int? BirthYear { get; set; }
    [BindProperty] public string? HouseNumber { get; set; }
    [BindProperty] public string? EducationLevel { get; set; }
    [BindProperty] public string? Occupation { get; set; }
    [BindProperty] public bool WantsRice { get; set; }
    [BindProperty] public bool WantsLivestock { get; set; }
    [BindProperty] public string? OtherNeed { get; set; }
    [BindProperty] public IFormFile? ImportFile { get; set; }

    public async Task<IActionResult> OnGetAsync(Guid? id, Guid? entryId) => await LoadAsync(id, entryId);

    public async Task<IActionResult> OnPostCreateRoundAsync()
    {
        if (!CanManage()) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        var response = await _api.SendJsonAsync(HttpMethod.Post, "/api/v1/admin/need-survey/rounds", token, new { year = Year, title = RoundTitle });
        if (response is null || !response.IsSuccessStatusCode)
        {
            ErrorMessage = await ReadErrorAsync(response) ?? "Không tạo được đợt điều tra.";
            return await LoadAsync(null, null);
        }
        var result = await response.Content.ReadFromJsonAsync<ItemEnvelope<RoundItem>>();
        return result?.Item is RoundItem round ? Redirect($"/admin/dieu-tra-nhu-cau?id={round.Id}") : Redirect("/admin/dieu-tra-nhu-cau");
    }

    public async Task<IActionResult> OnPostImportAsync(Guid id)
    {
        if (!CanManage()) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        if (ImportFile is null || ImportFile.Length == 0)
        {
            ErrorMessage = "Chọn file Excel phiếu điều tra nhu cầu học tập.";
            return await LoadAsync(id, null);
        }
        using var content = new MultipartFormDataContent();
        await using var stream = ImportFile.OpenReadStream();
        using var fileContent = new StreamContent(stream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(ImportFile.ContentType ?? "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        content.Add(fileContent, "file", ImportFile.FileName);
        var response = await _api.PostMultipartAsync($"/api/v1/admin/need-survey/rounds/{id}/import", token, content);
        if (response is null || !response.IsSuccessStatusCode)
        {
            ErrorMessage = await ReadErrorAsync(response) ?? "Không nhập được file Excel.";
            return await LoadAsync(id, null);
        }
        var result = await response.Content.ReadFromJsonAsync<ImportResult>();
        SuccessMessage = $"Đã nhập {result?.Created ?? 0} phiếu; bỏ qua {result?.SkippedBlankRows ?? 0} dòng trống.";
        return await LoadAsync(id, null);
    }

    public async Task<IActionResult> OnGetExportAsync(Guid id)
    {
        if (!CanManage()) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        var file = await _api.GetFileAsync($"/api/v1/admin/need-survey/rounds/{id}/export", token);
        if (file.Bytes is null) return NotFound();
        return File(file.Bytes, file.ContentType ?? "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", file.FileName ?? "phieu-dieu-tra-nhu-cau.xlsx");
    }

    public async Task<IActionResult> OnPostSaveEntryAsync(Guid id)
    {
        if (!CanManage()) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        var body = new { sourceSheet = SourceSheet, sequence = Sequence, locationLabel = LocationLabel, fullName = FullName,
            female = Female, birthYear = BirthYear, houseNumber = HouseNumber, educationLevel = EducationLevel,
            occupation = Occupation, wantsRice = WantsRice, wantsLivestock = WantsLivestock, otherNeed = OtherNeed };
        var response = EntryId is Guid entryId
            ? await _api.SendJsonAsync(HttpMethod.Put, $"/api/v1/admin/need-survey/entries/{entryId}", token, body)
            : await _api.SendJsonAsync(HttpMethod.Post, $"/api/v1/admin/need-survey/rounds/{id}/entries", token, body);
        if (response is null || !response.IsSuccessStatusCode)
        {
            ErrorMessage = await ReadErrorAsync(response) ?? "Không lưu được phiếu điều tra.";
            return await LoadAsync(id, EntryId);
        }
        return Redirect($"/admin/dieu-tra-nhu-cau?id={id}");
    }

    public async Task<IActionResult> OnPostDeleteEntryAsync(Guid id, Guid entryId)
    {
        if (!CanManage()) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        var response = await _api.SendJsonAsync(HttpMethod.Delete, $"/api/v1/admin/need-survey/entries/{entryId}", token, new { });
        if (response is null || !response.IsSuccessStatusCode)
        {
            ErrorMessage = await ReadErrorAsync(response) ?? "Không xóa được phiếu điều tra.";
            return await LoadAsync(id, null);
        }
        return Redirect($"/admin/dieu-tra-nhu-cau?id={id}");
    }

    private bool CanManage() => (HttpContext.Items["StaffProfile"] as StaffProfile)?.Permissions?.Contains("survey.manage") == true;
    private string? Token() => Request.Cookies[AdminGateMiddleware.CookieName];

    private async Task<IActionResult> LoadAsync(Guid? id, Guid? entryId)
    {
        if (!CanManage()) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        var rounds = await _api.GetJsonAsync<ListEnvelope<RoundItem>>("/api/v1/admin/need-survey/rounds", token);
        Rounds = rounds?.Items ?? [];
        if (id is Guid roundId)
        {
            var detail = await _api.GetJsonAsync<RoundDetailEnvelope>($"/api/v1/admin/need-survey/rounds/{roundId}", token);
            SelectedRound = detail?.Item;
            Entries = detail?.Entries ?? [];
            Summary = detail?.Summary;
            if (entryId is Guid selectedId)
            {
                var item = Entries.FirstOrDefault(row => row.Id == selectedId);
                if (item is not null) LoadEntry(item);
            }
        }
        return Page();
    }

    private void LoadEntry(EntryItem item)
    {
        EntryId = item.Id;
        SourceSheet = item.SourceSheet;
        Sequence = item.Sequence;
        LocationLabel = item.LocationLabel;
        FullName = item.FullName;
        Female = item.Female;
        BirthYear = item.BirthYear;
        HouseNumber = item.HouseNumber;
        EducationLevel = item.EducationLevel;
        Occupation = item.Occupation;
        WantsRice = item.WantsRice;
        WantsLivestock = item.WantsLivestock;
        OtherNeed = item.OtherNeed;
    }

    private static async Task<string?> ReadErrorAsync(HttpResponseMessage? response)
    {
        if (response is null) return null;
        try
        {
            var error = await response.Content.ReadFromJsonAsync<ApiErrorBody>();
            return string.IsNullOrWhiteSpace(error?.Message) ? null : error.Message;
        }
        catch (Exception)
        {
            return null;
        }
    }

    public sealed record RoundItem(Guid Id, int? Year, string Title, DateTimeOffset CreatedAt);
    public sealed record EntryItem(Guid Id, Guid RoundId, string SourceSheet, int Sequence, string LocationLabel, string? FullName,
        bool Female, int? BirthYear, string? HouseNumber, string? EducationLevel, string? Occupation, bool WantsRice, bool WantsLivestock, string? OtherNeed);
    public sealed record LocationSummary(string LocationLabel, int Total, int RiceNeedCount, int LivestockNeedCount, int OtherNeedCount);
    public sealed record SummaryItem(int Total, int FemaleCount, int RiceNeedCount, int LivestockNeedCount, int OtherNeedCount, IReadOnlyList<LocationSummary> Locations);
    private sealed record ListEnvelope<T>(IReadOnlyList<T>? Items);
    private sealed record ItemEnvelope<T>(T? Item);
    private sealed record RoundDetailEnvelope(RoundItem? Item, IReadOnlyList<EntryItem>? Entries, SummaryItem? Summary);
    private sealed record ImportResult(int Created, int SkippedBlankRows);
    private sealed record ApiErrorBody(string? Code, string? Message);
}
