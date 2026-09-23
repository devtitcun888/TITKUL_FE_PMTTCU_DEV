using Microsoft.AspNetCore.Mvc.RazorPages;
using TITKUL.PMTTCU.Web.ApiClients;

namespace TITKUL.PMTTCU.Web.Pages;

public class DangKyCamOnModel : PageModel
{
    private readonly BackendApiClient _api;
    public DangKyCamOnModel(BackendApiClient api) => _api = api;
    public string? ClassName { get; private set; }
    public string? RegistrationCode { get; private set; }
    public string? ErrorMessage { get; private set; }

    public async Task OnGetAsync(string ma, string? code)
    {
        RegistrationCode = code;
        var cls = await _api.GetPublicJsonAsync<ItemEnvelope<PublicClass>>($"/api/v1/public/classes/{ma}");
        ClassName = cls?.Item?.Name;
        if (string.IsNullOrWhiteSpace(code))
        {
            ErrorMessage = "Thiếu mã đăng ký.";
            return;
        }

        var found = await _api.GetPublicJsonAsync<ItemEnvelope<Result>>($"/api/v1/public/classes/{ma}/registrations/{code}");
        if (found?.Item is null)
        {
            ErrorMessage = "Không tìm thấy đăng ký này.";
        }
    }

    public sealed record PublicClass(string Code, string Name);
    private sealed record ItemEnvelope<T>(T? Item);
    private sealed record Result(string Code, string ClassCode);
}
