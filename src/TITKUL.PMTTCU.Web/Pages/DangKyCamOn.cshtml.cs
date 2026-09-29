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
        PublicPrivatePageHeaders.Apply(Response);
        RegistrationCode = code;
        var classPath = $"/api/v1/public/classes/{Uri.EscapeDataString(ma)}";
        if (string.IsNullOrWhiteSpace(code))
        {
            ErrorMessage = "Thiếu mã đăng ký.";
            Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        var classTask = _api.GetPublicJsonResultAsync<ItemEnvelope<PublicClass>>(classPath);
        var registrationTask = _api.GetPublicJsonResultAsync<ItemEnvelope<Result>>($"{classPath}/registrations/{Uri.EscapeDataString(code)}");
        await Task.WhenAll(classTask, registrationTask);
        var cls = await classTask;
        var found = await registrationTask;
        ClassName = cls.Value?.Item?.Name;
        if (found.Value?.Item is null)
        {
            var notFound = found.IsNotFound || found.IsAvailable;
            ErrorMessage = notFound
                ? "Không tìm thấy đăng ký này."
                : "Chưa thể xác nhận đăng ký lúc này. Vui lòng thử lại sau.";
            Response.StatusCode = notFound
                ? StatusCodes.Status404NotFound
                : StatusCodes.Status503ServiceUnavailable;
        }
    }

    public sealed record PublicClass(string Code, string Name);
    private sealed record ItemEnvelope<T>(T? Item);
    private sealed record Result(string Code, string ClassCode);
}
