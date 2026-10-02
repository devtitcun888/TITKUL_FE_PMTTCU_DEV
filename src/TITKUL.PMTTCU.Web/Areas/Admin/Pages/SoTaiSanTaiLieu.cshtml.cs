using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using TITKUL.PMTTCU.Web.ApiClients;
using TITKUL.PMTTCU.Web.Areas.Admin;
using TITKUL.PMTTCU.Web.Observability;

namespace TITKUL.PMTTCU.Web.Areas.Admin.Pages;

public sealed class SoTaiSanTaiLieuModel : PageModel
{
    private readonly BackendApiClient _api;
    public SoTaiSanTaiLieuModel(BackendApiClient api) => _api = api;
    public IReadOnlyList<AssetItem> Assets { get; private set; } = [];
    public IReadOnlyList<CatalogItem> Catalog { get; private set; } = [];
    public IReadOnlyList<LoanItem> Loans { get; private set; } = [];
    public string? ErrorMessage { get; private set; }
    public string? SuccessMessage { get; private set; }
    public bool ShowAsset { get; private set; }
    public bool ShowCatalog { get; private set; }
    public bool ShowLoan { get; private set; }
    public string? ImportKind { get; private set; }
    public string? Query { get; private set; }
    public string? Sort { get; private set; }
    public string Dir { get; private set; } = "asc";
    public string? CatalogSort { get; private set; }
    public string CatalogDir { get; private set; } = "asc";
    public string? LoanSort { get; private set; }
    public string LoanDir { get; private set; } = "asc";
    public bool FiltersActive => Query is not null;

    [BindProperty(SupportsGet = true)] public Guid? AssetId { get; set; }
    [BindProperty(SupportsGet = true)] public Guid? CatalogId { get; set; }
    [BindProperty(SupportsGet = true)] public Guid? LoanId { get; set; }
    [BindProperty] public int? Sequence { get; set; }
    [BindProperty] public string? Name { get; set; }
    [BindProperty] public string? Brand { get; set; }
    [BindProperty] public string? Country { get; set; }
    [BindProperty] public decimal? UnitPrice { get; set; }
    [BindProperty] public int? Quantity { get; set; }
    [BindProperty] public string? Supplier { get; set; }
    [BindProperty] public string? Note { get; set; }
    [BindProperty] public DateOnly? ReceivedOn { get; set; }
    [BindProperty] public string? BorrowerName { get; set; }
    [BindProperty] public string? MaterialName { get; set; }
    [BindProperty] public DateOnly? BorrowedOn { get; set; }
    [BindProperty] public string? BorrowCondition { get; set; }
    [BindProperty] public int? BorrowedQuantity { get; set; }
    [BindProperty] public string? BorrowSignature { get; set; }
    [BindProperty] public DateOnly? ReturnedOn { get; set; }
    [BindProperty] public string? ReturnCondition { get; set; }
    [BindProperty] public int? ReturnedQuantity { get; set; }
    [BindProperty] public string? ReturnSignature { get; set; }
    [BindProperty] public IFormFile? ImportFile { get; set; }

    public async Task<IActionResult> OnGetAsync(string? q, string? sort, string? dir, string? csort, string? cdir, string? lsort, string? ldir, Guid? assetId, Guid? catalogId, Guid? loanId, string? create, string? import)
    {
        var page = await LoadAsync(q, sort, dir, csort, cdir, lsort, ldir, assetId, catalogId, loanId);
        ShowAsset = AssetId.HasValue || create == "asset";
        ShowCatalog = CatalogId.HasValue || create == "catalog";
        ShowLoan = LoanId.HasValue || create == "loan";
        ImportKind = import is "asset" or "catalog" or "loan" ? import : null;
        return page;
    }

    public string ListUrl(string? q = null, string? sort = null, string? dir = null)
    {
        var query = BuildQuery(q);
        CmsListSort.Append(query, sort ?? Sort, dir ?? Dir);
        AppendExtraSort(query);
        return QueryHelpers.AddQueryString("/admin/so-tai-san-tai-lieu", query);
    }

    public string SortUrl(string column) => ListUrl(sort: column, dir: CmsListSort.NextDir(Sort, column, Dir));

    public string CatalogSortUrl(string column)
    {
        var query = BuildQuery(Query);
        CmsListSort.Append(query, Sort, Dir);
        var next = CmsListSort.Normalize(column, "seq", "received", "name", "price", "qty", "total", "supplier");
        var nextDir = CmsListSort.NextDir(CatalogSort, column, CatalogDir);
        if (next is not null)
        {
            query["csort"] = next;
            if (nextDir == "desc") query["cdir"] = "desc";
        }
        AppendLoanSort(query);
        return QueryHelpers.AddQueryString("/admin/so-tai-san-tai-lieu", query);
    }

    public string LoanSortUrl(string column)
    {
        var query = BuildQuery(Query);
        CmsListSort.Append(query, Sort, Dir);
        AppendCatalogSort(query);
        var next = CmsListSort.Normalize(column, "seq", "borrower", "material", "borrowed", "returned");
        var nextDir = CmsListSort.NextDir(LoanSort, column, LoanDir);
        if (next is not null)
        {
            query["lsort"] = next;
            if (nextDir == "desc") query["ldir"] = "desc";
        }
        return QueryHelpers.AddQueryString("/admin/so-tai-san-tai-lieu", query);
    }

    public async Task<IActionResult> OnPostSaveAssetAsync()
    {
        if (!CanManage()) return Redirect("/admin/khong-quyen"); var token = Token(); if (token is null) return Redirect("/admin/dang-nhap");
        var body = new { sequence = Sequence, name = Name, brand = Brand, country = Country, unitPrice = UnitPrice, quantity = Quantity, supplier = Supplier, note = Note };
        var response = AssetId is Guid id ? await _api.SendJsonAsync(HttpMethod.Put, $"/api/v1/admin/material-register/assets/{id}", token, body)
            : await _api.SendJsonAsync(HttpMethod.Post, "/api/v1/admin/material-register/assets", token, body);
        if (response is null || !response.IsSuccessStatusCode) { ErrorMessage = await ReadErrorAsync(response) ?? "Không lưu được sổ tài sản."; ShowAsset = true; return await LoadAsync(Query, Sort, Dir, CatalogSort, CatalogDir, LoanSort, LoanDir, AssetId, CatalogId, LoanId); }
        return Redirect("/admin/so-tai-san-tai-lieu");
    }

    public async Task<IActionResult> OnPostDeleteAssetAsync(Guid assetId)
    {
        if (!CanManage()) return Redirect("/admin/khong-quyen"); var token = Token(); if (token is null) return Redirect("/admin/dang-nhap");
        var response = await _api.SendJsonAsync(HttpMethod.Delete, $"/api/v1/admin/material-register/assets/{assetId}", token, new { });
        if (response is null || !response.IsSuccessStatusCode) { ErrorMessage = await ReadErrorAsync(response) ?? "Không xóa được dòng tài sản."; return await LoadAsync(Query, Sort, Dir, CatalogSort, CatalogDir, LoanSort, LoanDir, null, null, null); }
        return Redirect("/admin/so-tai-san-tai-lieu");
    }

    public async Task<IActionResult> OnPostImportAssetsAsync() => await ImportAsync("/api/v1/admin/material-register/assets/import", "Đã nhập sổ tài sản.");

    public async Task<IActionResult> OnPostSaveCatalogAsync()
    {
        if (!CanManage()) return Redirect("/admin/khong-quyen"); var token = Token(); if (token is null) return Redirect("/admin/dang-nhap");
        var body = new { sequence = Sequence, receivedOn = ReceivedOn, name = Name, unitPrice = UnitPrice, quantity = Quantity, supplier = Supplier, note = Note };
        var response = CatalogId is Guid id ? await _api.SendJsonAsync(HttpMethod.Put, $"/api/v1/admin/material-register/catalog/{id}", token, body)
            : await _api.SendJsonAsync(HttpMethod.Post, "/api/v1/admin/material-register/catalog", token, body);
        if (response is null || !response.IsSuccessStatusCode) { ErrorMessage = await ReadErrorAsync(response) ?? "Không lưu được danh mục tài liệu."; ShowCatalog = true; return await LoadAsync(Query, Sort, Dir, CatalogSort, CatalogDir, LoanSort, LoanDir, AssetId, CatalogId, LoanId); }
        return Redirect("/admin/so-tai-san-tai-lieu");
    }

    public async Task<IActionResult> OnPostDeleteCatalogAsync(Guid catalogId)
    {
        if (!CanManage()) return Redirect("/admin/khong-quyen"); var token = Token(); if (token is null) return Redirect("/admin/dang-nhap");
        var response = await _api.SendJsonAsync(HttpMethod.Delete, $"/api/v1/admin/material-register/catalog/{catalogId}", token, new { });
        if (response is null || !response.IsSuccessStatusCode) { ErrorMessage = await ReadErrorAsync(response) ?? "Không xóa được dòng danh mục."; return await LoadAsync(Query, Sort, Dir, CatalogSort, CatalogDir, LoanSort, LoanDir, null, null, null); }
        return Redirect("/admin/so-tai-san-tai-lieu");
    }

    public async Task<IActionResult> OnPostImportCatalogAsync() => await ImportAsync("/api/v1/admin/material-register/catalog/import", "Đã nhập danh mục tài liệu.");

    public async Task<IActionResult> OnPostSaveLoanAsync()
    {
        if (!CanManage()) return Redirect("/admin/khong-quyen"); var token = Token(); if (token is null) return Redirect("/admin/dang-nhap");
        var body = new { sequence = Sequence, borrowerName = BorrowerName, materialName = MaterialName, borrowedOn = BorrowedOn,
            borrowCondition = BorrowCondition, borrowedQuantity = BorrowedQuantity, borrowSignature = BorrowSignature,
            returnedOn = ReturnedOn, returnCondition = ReturnCondition, returnedQuantity = ReturnedQuantity, returnSignature = ReturnSignature };
        var response = LoanId is Guid id ? await _api.SendJsonAsync(HttpMethod.Put, $"/api/v1/admin/material-register/loans/{id}", token, body)
            : await _api.SendJsonAsync(HttpMethod.Post, "/api/v1/admin/material-register/loans", token, body);
        if (response is null || !response.IsSuccessStatusCode) { ErrorMessage = await ReadErrorAsync(response) ?? "Không lưu được sổ mượn–trả."; ShowLoan = true; return await LoadAsync(Query, Sort, Dir, CatalogSort, CatalogDir, LoanSort, LoanDir, AssetId, CatalogId, LoanId); }
        return Redirect("/admin/so-tai-san-tai-lieu");
    }

    public async Task<IActionResult> OnPostDeleteLoanAsync(Guid loanId)
    {
        if (!CanManage()) return Redirect("/admin/khong-quyen"); var token = Token(); if (token is null) return Redirect("/admin/dang-nhap");
        var response = await _api.SendJsonAsync(HttpMethod.Delete, $"/api/v1/admin/material-register/loans/{loanId}", token, new { });
        if (response is null || !response.IsSuccessStatusCode) { ErrorMessage = await ReadErrorAsync(response) ?? "Không xóa được dòng mượn–trả."; return await LoadAsync(Query, Sort, Dir, CatalogSort, CatalogDir, LoanSort, LoanDir, null, null, null); }
        return Redirect("/admin/so-tai-san-tai-lieu");
    }

    public async Task<IActionResult> OnPostImportLoansAsync() => await ImportAsync("/api/v1/admin/material-register/loans/import", "Đã nhập sổ mượn–trả.");

    public async Task<IActionResult> OnGetExportAsync()
    {
        if (!CanManage()) return Redirect("/admin/khong-quyen"); var token = Token(); if (token is null) return Redirect("/admin/dang-nhap");
        var file = await _api.GetFileAsync("/api/v1/admin/material-register/export", token);
        return file.Bytes is null ? NotFound() : File(file.Bytes, file.ContentType ?? "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", file.FileName ?? "so-tai-san-tai-lieu-muon-tra.xlsx");
    }

    private async Task<IActionResult> ImportAsync(string path, string successPrefix)
    {
        if (!CanManage()) return Redirect("/admin/khong-quyen"); var token = Token(); if (token is null) return Redirect("/admin/dang-nhap");
        if (ImportFile is null || ImportFile.Length == 0) { ErrorMessage = "Chọn workbook Excel .xlsx."; ImportKind = path.Contains("catalog", StringComparison.Ordinal) ? "catalog" : path.Contains("loans", StringComparison.Ordinal) ? "loan" : "asset"; return await LoadAsync(Query, Sort, Dir, CatalogSort, CatalogDir, LoanSort, LoanDir, null, null, null); }
        using var content = new MultipartFormDataContent(); await using var stream = ImportFile.OpenReadStream(); using var fileContent = new StreamContent(stream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(ImportFile.ContentType ?? "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"); content.Add(fileContent, "file", ImportFile.FileName);
        var response = await _api.PostMultipartAsync(path, token, content);
        if (response is null || !response.IsSuccessStatusCode) { ErrorMessage = await ReadErrorAsync(response) ?? "Không nhập được workbook."; ImportKind = path.Contains("catalog", StringComparison.Ordinal) ? "catalog" : path.Contains("loans", StringComparison.Ordinal) ? "loan" : "asset"; return await LoadAsync(Query, Sort, Dir, CatalogSort, CatalogDir, LoanSort, LoanDir, null, null, null); }
        var result = await response.Content.ReadFromJsonAsync<ImportResult>(); SuccessMessage = $"{successPrefix} Đã thêm {result?.Created ?? 0} dòng; bỏ qua {result?.SkippedBlankRows ?? 0} dòng trống.";
        return await LoadAsync(null, null, null, null, null, null, null, null, null, null);
    }

    private bool CanManage() => (HttpContext.Items["StaffProfile"] as StaffProfile)?.Permissions?.Contains("education.manage") == true;
    private string? Token() => Request.Cookies[AdminGateMiddleware.CookieName];

    private async Task<IActionResult> LoadAsync(string? q, string? sort, string? dir, string? csort, string? cdir, string? lsort, string? ldir, Guid? assetId, Guid? catalogId, Guid? loanId)
    {
        if (!CanManage()) return Redirect("/admin/khong-quyen"); var token = Token(); if (token is null) return Redirect("/admin/dang-nhap");
        Query = string.IsNullOrWhiteSpace(q) ? null : q.Trim();
        Sort = CmsListSort.Normalize(sort, "seq", "name", "brand", "country", "price", "qty", "total", "supplier");
        Dir = CmsListSort.Dir(dir);
        CatalogSort = CmsListSort.Normalize(csort, "seq", "received", "name", "price", "qty", "total", "supplier");
        CatalogDir = CmsListSort.Dir(cdir);
        LoanSort = CmsListSort.Normalize(lsort, "seq", "borrower", "material", "borrowed", "returned");
        LoanDir = CmsListSort.Dir(ldir);
        var assets = await _api.GetJsonAsync<ListEnvelope<AssetItem>>("/api/v1/admin/material-register/assets", token);
        var catalog = await _api.GetJsonAsync<ListEnvelope<CatalogItem>>("/api/v1/admin/material-register/catalog", token);
        var loans = await _api.GetJsonAsync<ListEnvelope<LoanItem>>("/api/v1/admin/material-register/loans", token);
        var assetItems = assets?.Items ?? [];
        var catalogItems = catalog?.Items ?? [];
        var loanItems = loans?.Items ?? [];
        if (Query is not null)
        {
            assetItems = assetItems.Where(item =>
                (item.Name?.Contains(Query, StringComparison.CurrentCultureIgnoreCase) ?? false)
                || (item.Brand?.Contains(Query, StringComparison.CurrentCultureIgnoreCase) ?? false)
                || (item.Supplier?.Contains(Query, StringComparison.CurrentCultureIgnoreCase) ?? false)).ToArray();
            catalogItems = catalogItems.Where(item =>
                (item.Name?.Contains(Query, StringComparison.CurrentCultureIgnoreCase) ?? false)
                || (item.Supplier?.Contains(Query, StringComparison.CurrentCultureIgnoreCase) ?? false)).ToArray();
            loanItems = loanItems.Where(item =>
                (item.BorrowerName?.Contains(Query, StringComparison.CurrentCultureIgnoreCase) ?? false)
                || (item.MaterialName?.Contains(Query, StringComparison.CurrentCultureIgnoreCase) ?? false)).ToArray();
        }
        Assets = assetItems;
        Catalog = catalogItems;
        Loans = loanItems;
        if (Sort is not null)
        {
            Assets = CmsListSort.Order(Assets, Dir, Sort switch
            {
                "name" => Assets.OrderBy(item => item.Name ?? "", StringComparer.CurrentCultureIgnoreCase),
                "brand" => Assets.OrderBy(item => item.Brand ?? "", StringComparer.CurrentCultureIgnoreCase),
                "country" => Assets.OrderBy(item => item.Country ?? "", StringComparer.CurrentCultureIgnoreCase),
                "price" => Assets.OrderBy(item => item.UnitPrice ?? 0),
                "qty" => Assets.OrderBy(item => item.Quantity ?? 0),
                "total" => Assets.OrderBy(item => item.TotalAmount ?? 0),
                "supplier" => Assets.OrderBy(item => item.Supplier ?? "", StringComparer.CurrentCultureIgnoreCase),
                _ => Assets.OrderBy(item => item.Sequence)
            });
        }
        if (CatalogSort is not null)
        {
            Catalog = CmsListSort.Order(Catalog, CatalogDir, CatalogSort switch
            {
                "received" => Catalog.OrderBy(item => item.ReceivedOn ?? DateOnly.MinValue),
                "name" => Catalog.OrderBy(item => item.Name ?? "", StringComparer.CurrentCultureIgnoreCase),
                "price" => Catalog.OrderBy(item => item.UnitPrice ?? 0),
                "qty" => Catalog.OrderBy(item => item.Quantity ?? 0),
                "total" => Catalog.OrderBy(item => item.TotalAmount ?? 0),
                "supplier" => Catalog.OrderBy(item => item.Supplier ?? "", StringComparer.CurrentCultureIgnoreCase),
                _ => Catalog.OrderBy(item => item.Sequence)
            });
        }
        if (LoanSort is not null)
        {
            Loans = CmsListSort.Order(Loans, LoanDir, LoanSort switch
            {
                "borrower" => Loans.OrderBy(item => item.BorrowerName ?? "", StringComparer.CurrentCultureIgnoreCase),
                "material" => Loans.OrderBy(item => item.MaterialName ?? "", StringComparer.CurrentCultureIgnoreCase),
                "borrowed" => Loans.OrderBy(item => item.BorrowedOn ?? DateOnly.MinValue),
                "returned" => Loans.OrderBy(item => item.ReturnedOn ?? DateOnly.MinValue),
                _ => Loans.OrderBy(item => item.Sequence)
            });
        }
        if (assetId is Guid aid && (assets?.Items ?? []).FirstOrDefault(x => x.Id == aid) is { } asset) LoadAsset(asset);
        if (catalogId is Guid cid && (catalog?.Items ?? []).FirstOrDefault(x => x.Id == cid) is { } catalogItem) LoadCatalog(catalogItem);
        if (loanId is Guid lid && (loans?.Items ?? []).FirstOrDefault(x => x.Id == lid) is { } loan) LoadLoan(loan);
        return Page();
    }

    private Dictionary<string, string?> BuildQuery(string? q)
    {
        var query = new Dictionary<string, string?>();
        var nextQ = q ?? Query;
        if (!string.IsNullOrWhiteSpace(nextQ)) query["q"] = nextQ;
        return query;
    }

    private void AppendExtraSort(IDictionary<string, string?> query)
    {
        AppendCatalogSort(query);
        AppendLoanSort(query);
    }

    private void AppendCatalogSort(IDictionary<string, string?> query)
    {
        if (string.IsNullOrWhiteSpace(CatalogSort)) return;
        query["csort"] = CatalogSort;
        if (CatalogDir == "desc") query["cdir"] = "desc";
    }

    private void AppendLoanSort(IDictionary<string, string?> query)
    {
        if (string.IsNullOrWhiteSpace(LoanSort)) return;
        query["lsort"] = LoanSort;
        if (LoanDir == "desc") query["ldir"] = "desc";
    }

    private void LoadAsset(AssetItem x) { AssetId = x.Id; Sequence = x.Sequence; Name = x.Name; Brand = x.Brand; Country = x.Country; UnitPrice = x.UnitPrice; Quantity = x.Quantity; Supplier = x.Supplier; Note = x.Note; }
    private void LoadCatalog(CatalogItem x) { CatalogId = x.Id; Sequence = x.Sequence; ReceivedOn = x.ReceivedOn; Name = x.Name; UnitPrice = x.UnitPrice; Quantity = x.Quantity; Supplier = x.Supplier; Note = x.Note; }
    private void LoadLoan(LoanItem x) { LoanId = x.Id; Sequence = x.Sequence; BorrowerName = x.BorrowerName; MaterialName = x.MaterialName; BorrowedOn = x.BorrowedOn; BorrowCondition = x.BorrowCondition; BorrowedQuantity = x.BorrowedQuantity; BorrowSignature = x.BorrowSignature; ReturnedOn = x.ReturnedOn; ReturnCondition = x.ReturnCondition; ReturnedQuantity = x.ReturnedQuantity; ReturnSignature = x.ReturnSignature; }
    private static async Task<string?> ReadErrorAsync(HttpResponseMessage? response) { if (response is null) return null; try { return (await response.Content.ReadFromJsonAsync<ApiErrorBody>())?.Message; } catch (Exception) { return null; } }

    public sealed record AssetItem(Guid Id, int Sequence, string? Name, string? Brand, string? Country, decimal? UnitPrice, int? Quantity, decimal? TotalAmount, string? Supplier, string? Note);
    public sealed record CatalogItem(Guid Id, int Sequence, DateOnly? ReceivedOn, string? Name, decimal? UnitPrice, int? Quantity, decimal? TotalAmount, string? Supplier, string? Note);
    public sealed record LoanItem(Guid Id, int Sequence, string? BorrowerName, string? MaterialName, DateOnly? BorrowedOn, string? BorrowCondition, int? BorrowedQuantity, string? BorrowSignature, DateOnly? ReturnedOn, string? ReturnCondition, int? ReturnedQuantity, string? ReturnSignature);
    private sealed record ListEnvelope<T>(IReadOnlyList<T>? Items);
    private sealed record ImportResult(int Created, int SkippedBlankRows);
    private sealed record ApiErrorBody(string? Code, string? Message);
}
