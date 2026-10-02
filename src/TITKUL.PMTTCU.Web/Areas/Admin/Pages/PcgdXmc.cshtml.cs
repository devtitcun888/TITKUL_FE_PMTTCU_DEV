using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using TITKUL.PMTTCU.Web.ApiClients;
using TITKUL.PMTTCU.Web.Areas.Admin;
using TITKUL.PMTTCU.Web.Observability;

namespace TITKUL.PMTTCU.Web.Areas.Admin.Pages;

public sealed class PcgdXmcModel : PageModel
{
    private readonly BackendApiClient _api;
    public PcgdXmcModel(BackendApiClient api) => _api = api;

    public IReadOnlyList<RoundItem> Rounds { get; private set; } = [];
    public IReadOnlyList<EntryItem> Entries { get; private set; } = [];
    public RoundItem? SelectedRound { get; private set; }
    public string? ErrorMessage { get; private set; }
    public string? SuccessMessage { get; private set; }
    public bool ShowCreate { get; private set; }
    public bool ShowImport { get; private set; }
    public bool ShowEntry { get; private set; }
    public string? Query { get; private set; }
    public int? FilterYear { get; private set; }
    public string? Sort { get; private set; }
    public string Dir { get; private set; } = "asc";
    public string? EntrySort { get; private set; }
    public string EntryDir { get; private set; } = "asc";
    public bool FiltersActive => Query is not null || FilterYear is not null;

    [BindProperty(SupportsGet = true)] public Guid? Id { get; set; }
    [BindProperty(SupportsGet = true)] public Guid? EntryId { get; set; }
    [BindProperty] public int? Year { get; set; }
    [BindProperty] public string RoundTitle { get; set; } = "";
    [BindProperty] public string? SourceSheet { get; set; }
    [BindProperty] public int? Sequence { get; set; }
    [BindProperty] public string? Province { get; set; }
    [BindProperty] public string? Commune { get; set; }
    [BindProperty] public string? Hamlet { get; set; }
    [BindProperty] public string? SurveyTime { get; set; }
    [BindProperty] public string? FamilyName { get; set; }
    [BindProperty] public string? GivenName { get; set; }
    [BindProperty] public int? BirthYear { get; set; }
    [BindProperty] public bool Female { get; set; }
    [BindProperty] public string? Address { get; set; }
    [BindProperty] public string? EducationLevel { get; set; }
    [BindProperty] public string? StudyStatus { get; set; }
    [BindProperty] public string? Note { get; set; }
    [BindProperty] public IFormFile? ImportFile { get; set; }

    public async Task<IActionResult> OnGetAsync(string? q, int? year, string? sort, string? dir, string? esort, string? edir, Guid? id, Guid? entryId, bool create = false, bool import = false, bool entry = false)
    {
        var page = await LoadAsync(q, year, sort, dir, esort, edir, id, entryId);
        ShowCreate = create;
        ShowImport = import && SelectedRound is not null;
        ShowEntry = SelectedRound is not null && (EntryId.HasValue || entry);
        return page;
    }

    public string ListUrl(string? q = null, int? year = null, string? sort = null, string? dir = null, Guid? id = null, bool omitId = false, bool clearYear = false)
    {
        var query = new Dictionary<string, string?>();
        var nextQ = q ?? Query;
        var nextYear = clearYear ? null : year ?? FilterYear;
        var nextId = omitId ? null : id ?? Id;
        if (!string.IsNullOrWhiteSpace(nextQ)) query["q"] = nextQ;
        if (nextYear is int y) query["year"] = y.ToString(CultureInfo.InvariantCulture);
        CmsListSort.Append(query, sort ?? Sort, dir ?? Dir);
        if (!string.IsNullOrWhiteSpace(EntrySort))
        {
            query["esort"] = EntrySort;
            if (EntryDir == "desc") query["edir"] = "desc";
        }
        if (nextId is Guid roundId) query["id"] = roundId.ToString();
        return QueryHelpers.AddQueryString("/admin/pcgd-xmc", query);
    }

    public string SortUrl(string column) => ListUrl(sort: column, dir: CmsListSort.NextDir(Sort, column, Dir));

    public string EntrySortUrl(string column)
    {
        var query = new Dictionary<string, string?>();
        if (Query is not null) query["q"] = Query;
        if (FilterYear is int y) query["year"] = y.ToString(CultureInfo.InvariantCulture);
        CmsListSort.Append(query, Sort, Dir);
        var next = CmsListSort.Normalize(column, "sheet", "hamlet", "seq", "name", "birth", "female");
        var nextDir = CmsListSort.NextDir(EntrySort, column, EntryDir);
        if (next is not null)
        {
            query["esort"] = next;
            if (nextDir == "desc") query["edir"] = "desc";
        }
        if (Id is Guid roundId) query["id"] = roundId.ToString();
        return QueryHelpers.AddQueryString("/admin/pcgd-xmc", query);
    }

    public async Task<IActionResult> OnPostCreateRoundAsync()
    {
        if (!CanManage()) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        var response = await _api.SendJsonAsync(HttpMethod.Post, "/api/v1/admin/pcgd-xmc/rounds", token, new { year = Year, title = RoundTitle });
        if (response is null || !response.IsSuccessStatusCode)
        {
            ErrorMessage = await ReadErrorAsync(response) ?? "Không tạo được đợt điều tra.";
            ShowCreate = true;
            return await LoadAsync(Query, FilterYear, Sort, Dir, EntrySort, EntryDir, null, null);
        }
        var result = await response.Content.ReadFromJsonAsync<ItemEnvelope<RoundItem>>();
        return result?.Item is RoundItem round ? Redirect($"/admin/pcgd-xmc?id={round.Id}") : Redirect("/admin/pcgd-xmc");
    }

    public async Task<IActionResult> OnPostImportAsync(Guid id)
    {
        if (!CanManage()) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        if (ImportFile is null || ImportFile.Length == 0)
        {
            ErrorMessage = "Chọn workbook Excel PCGD/XMC (.xlsx).";
            ShowImport = true;
            return await LoadAsync(Query, FilterYear, Sort, Dir, EntrySort, EntryDir, id, null);
        }
        using var content = new MultipartFormDataContent();
        await using var stream = ImportFile.OpenReadStream();
        using var fileContent = new StreamContent(stream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(ImportFile.ContentType ?? "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        content.Add(fileContent, "file", ImportFile.FileName);
        var response = await _api.PostMultipartAsync($"/api/v1/admin/pcgd-xmc/rounds/{id}/import", token, content);
        if (response is null || !response.IsSuccessStatusCode)
        {
            ErrorMessage = await ReadErrorAsync(response) ?? "Không nhập được workbook.";
            ShowImport = true;
            return await LoadAsync(Query, FilterYear, Sort, Dir, EntrySort, EntryDir, id, null);
        }
        var result = await response.Content.ReadFromJsonAsync<ImportResult>();
        SuccessMessage = $"Đã nhập {result?.Created ?? 0} dòng; bỏ qua {result?.SkippedBlankRows ?? 0} dòng trống.";
        return await LoadAsync(Query, FilterYear, Sort, Dir, EntrySort, EntryDir, id, null);
    }

    public async Task<IActionResult> OnPostSaveEntryAsync(Guid id)
    {
        if (!CanManage()) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        var body = new { sourceSheet = SourceSheet, sequence = Sequence, province = Province, commune = Commune, hamlet = Hamlet,
            surveyTime = SurveyTime, familyName = FamilyName, givenName = GivenName, birthYear = BirthYear, female = Female,
            address = Address, educationLevel = EducationLevel, studyStatus = StudyStatus, note = Note };
        var response = EntryId is Guid entryId
            ? await _api.SendJsonAsync(HttpMethod.Put, $"/api/v1/admin/pcgd-xmc/entries/{entryId}", token, body)
            : await _api.SendJsonAsync(HttpMethod.Post, $"/api/v1/admin/pcgd-xmc/rounds/{id}/entries", token, body);
        if (response is null || !response.IsSuccessStatusCode)
        {
            ErrorMessage = await ReadErrorAsync(response) ?? "Không lưu được dòng PCGD/XMC.";
            ShowEntry = true;
            return await LoadAsync(Query, FilterYear, Sort, Dir, EntrySort, EntryDir, id, EntryId);
        }
        return Redirect($"/admin/pcgd-xmc?id={id}");
    }

    public async Task<IActionResult> OnPostDeleteEntryAsync(Guid id, Guid entryId)
    {
        if (!CanManage()) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        var response = await _api.SendJsonAsync(HttpMethod.Delete, $"/api/v1/admin/pcgd-xmc/entries/{entryId}", token, new { });
        if (response is null || !response.IsSuccessStatusCode)
        {
            ErrorMessage = await ReadErrorAsync(response) ?? "Không xóa được dòng dữ liệu.";
            return await LoadAsync(Query, FilterYear, Sort, Dir, EntrySort, EntryDir, id, null);
        }
        return Redirect($"/admin/pcgd-xmc?id={id}");
    }

    public async Task<IActionResult> OnGetExportAsync(Guid id)
    {
        if (!CanManage()) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        var file = await _api.GetFileAsync($"/api/v1/admin/pcgd-xmc/rounds/{id}/export", token);
        return file.Bytes is null ? NotFound() : File(file.Bytes, file.ContentType ?? "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", file.FileName ?? "pcgd-xmc.xlsx");
    }

    private bool CanManage() => (HttpContext.Items["StaffProfile"] as StaffProfile)?.Permissions?.Contains("education.manage") == true;
    private string? Token() => Request.Cookies[AdminGateMiddleware.CookieName];

    private async Task<IActionResult> LoadAsync(string? q, int? year, string? sort, string? dir, string? esort, string? edir, Guid? id, Guid? entryId)
    {
        if (!CanManage()) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        Query = string.IsNullOrWhiteSpace(q) ? null : q.Trim();
        FilterYear = year is int y && y >= 1 ? y : null;
        Sort = CmsListSort.Normalize(sort, "year", "title", "created");
        Dir = CmsListSort.Dir(dir);
        EntrySort = CmsListSort.Normalize(esort, "sheet", "hamlet", "seq", "name", "birth", "female");
        EntryDir = CmsListSort.Dir(edir);
        var rounds = await _api.GetJsonAsync<ListEnvelope<RoundItem>>("/api/v1/admin/pcgd-xmc/rounds", token);
        var all = rounds?.Items ?? [];
        if (rounds is null && string.IsNullOrEmpty(ErrorMessage)) ErrorMessage = "Không tải được các đợt điều tra.";
        IEnumerable<RoundItem> filtered = all;
        if (FilterYear is int filterYear) filtered = filtered.Where(item => item.Year == filterYear);
        if (Query is not null)
        {
            filtered = filtered.Where(item =>
                item.Title.Contains(Query, StringComparison.CurrentCultureIgnoreCase)
                || (item.Year?.ToString().Contains(Query, StringComparison.Ordinal) ?? false));
        }
        Rounds = filtered.ToArray();
        if (Sort is not null)
        {
            Rounds = CmsListSort.Order(Rounds, Dir, Sort switch
            {
                "title" => Rounds.OrderBy(item => item.Title, StringComparer.CurrentCultureIgnoreCase),
                "created" => Rounds.OrderBy(item => item.CreatedAt),
                _ => Rounds.OrderBy(item => item.Year ?? 0)
            });
        }
        if (id is Guid roundId)
        {
            var detail = await _api.GetJsonAsync<RoundDetailEnvelope>($"/api/v1/admin/pcgd-xmc/rounds/{roundId}", token);
            SelectedRound = detail?.Item;
            var entries = detail?.Entries ?? [];
            if (Query is not null && SelectedRound is not null)
            {
                entries = entries.Where(item =>
                    ($"{item.FamilyName} {item.GivenName}").Contains(Query, StringComparison.CurrentCultureIgnoreCase)
                    || (item.Hamlet?.Contains(Query, StringComparison.CurrentCultureIgnoreCase) ?? false)
                    || (item.Address?.Contains(Query, StringComparison.CurrentCultureIgnoreCase) ?? false)).ToArray();
            }
            Entries = entries;
            if (EntrySort is not null)
            {
                Entries = CmsListSort.Order(Entries, EntryDir, EntrySort switch
                {
                    "sheet" => Entries.OrderBy(item => item.SourceSheet, StringComparer.OrdinalIgnoreCase),
                    "hamlet" => Entries.OrderBy(item => item.Hamlet ?? "", StringComparer.CurrentCultureIgnoreCase),
                    "name" => Entries.OrderBy(item => item.FamilyName ?? "", StringComparer.CurrentCultureIgnoreCase).ThenBy(item => item.GivenName ?? "", StringComparer.CurrentCultureIgnoreCase),
                    "birth" => Entries.OrderBy(item => item.BirthYear ?? 0),
                    "female" => Entries.OrderBy(item => item.Female),
                    _ => Entries.OrderBy(item => item.Sequence)
                });
            }
            if (entryId is Guid selectedId && (detail?.Entries ?? []).FirstOrDefault(row => row.Id == selectedId) is { } item) LoadEntry(item);
        }
        return Page();
    }

    private void LoadEntry(EntryItem item)
    {
        EntryId = item.Id; SourceSheet = item.SourceSheet; Sequence = item.Sequence; Province = item.Province; Commune = item.Commune;
        Hamlet = item.Hamlet; SurveyTime = item.SurveyTime; FamilyName = item.FamilyName; GivenName = item.GivenName;
        BirthYear = item.BirthYear; Female = item.Female; Address = item.Address; EducationLevel = item.EducationLevel;
        StudyStatus = item.StudyStatus; Note = item.Note;
    }

    private static async Task<string?> ReadErrorAsync(HttpResponseMessage? response)
    {
        if (response is null) return null;
        try { return (await response.Content.ReadFromJsonAsync<ApiErrorBody>())?.Message; }
        catch (Exception) { return null; }
    }

    public sealed record RoundItem(Guid Id, int? Year, string Title, DateTimeOffset CreatedAt);
    public sealed record EntryItem(Guid Id, Guid RoundId, string SourceSheet, int Sequence, string? Province, string? Commune, string? Hamlet,
        string? SurveyTime, string? FamilyName, string? GivenName, int? BirthYear, bool Female, string? Address,
        string? EducationLevel, string? StudyStatus, string? Note);
    private sealed record ListEnvelope<T>(IReadOnlyList<T>? Items);
    private sealed record ItemEnvelope<T>(T? Item);
    private sealed record RoundDetailEnvelope(RoundItem? Item, IReadOnlyList<EntryItem>? Entries);
    private sealed record ImportResult(int Created, int SkippedBlankRows);
    private sealed record ApiErrorBody(string? Code, string? Message);
}
