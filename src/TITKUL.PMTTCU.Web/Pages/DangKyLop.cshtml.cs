using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TITKUL.PMTTCU.Web.ApiClients;

namespace TITKUL.PMTTCU.Web.Pages;

public class DangKyLopModel : PageModel
{
    private readonly BackendApiClient _api;
    public DangKyLopModel(BackendApiClient api) => _api = api;
    public PublicClass? Class { get; private set; }
    public IReadOnlyList<Hamlet> Hamlets { get; private set; } = [];
    public string? ErrorMessage { get; private set; }
    public string? SuccessCode { get; private set; }

    [BindProperty] public string FullName { get; set; } = "";
    [BindProperty] public string Phone { get; set; } = "";
    [BindProperty] public string CitizenId { get; set; } = "";
    [BindProperty] public DateOnly? BirthDate { get; set; }
    [BindProperty] public string Gender { get; set; } = "NAM";
    [BindProperty] public Guid? HamletId { get; set; }
    [BindProperty] public string? Note { get; set; }
    [BindProperty] public bool Consent { get; set; }
    [BindProperty] public string? Website { get; set; }

    public async Task<IActionResult> OnGetAsync(string ma) => await LoadAsync(ma);

    public async Task<IActionResult> OnPostAsync(string ma)
    {
        var loaded = await LoadAsync(ma);
        if (Class is null || !Class.CanRegister) return loaded;
        var response = await _api.PostPublicJsonAsync($"/api/v1/public/classes/{ma}/registrations", new
        {
            fullName = FullName,
            phone = Phone,
            citizenId = CitizenId,
            birthDate = BirthDate,
            gender = Gender,
            hamletId = HamletId,
            note = Note,
            consent = Consent,
            website = Website
        });
        if (response is null)
        {
            ErrorMessage = "Không gửi được đăng ký. Thử lại sau.";
            return Page();
        }

        if (response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadFromJsonAsync<ItemEnvelope<Result>>();
            SuccessCode = body?.Item?.Code;
            return Page();
        }

        try
        {
            var err = await response.Content.ReadFromJsonAsync<ApiErr>();
            ErrorMessage = err?.Message ?? "Không đăng ký được.";
        }
        catch (Exception)
        {
            ErrorMessage = "Không đăng ký được.";
        }

        return Page();
    }

    private async Task<IActionResult> LoadAsync(string ma)
    {
        var cls = await _api.GetPublicJsonAsync<ItemEnvelope<PublicClass>>($"/api/v1/public/classes/{ma}");
        var hamlets = await _api.GetPublicJsonAsync<ListEnvelope<Hamlet>>("/api/v1/public/hamlets");
        Class = cls?.Item;
        Hamlets = hamlets?.Items ?? [];
        if (Class is null) ErrorMessage ??= "Không tìm thấy lớp.";
        return Page();
    }

    public sealed record PublicClass(string Code, string Name, string ProgramName, DateOnly StartDate, DateOnly EndDate, int Remaining, bool CanRegister, string? ClosedReason);
    public sealed record Hamlet(Guid Id, string Code, string Name);
    private sealed record ItemEnvelope<T>(T? Item);
    private sealed record ListEnvelope<T>(IReadOnlyList<T>? Items);
    private sealed record Result(string Code, string ClassCode);
    private sealed record ApiErr(string? Code, string? Message);
}
