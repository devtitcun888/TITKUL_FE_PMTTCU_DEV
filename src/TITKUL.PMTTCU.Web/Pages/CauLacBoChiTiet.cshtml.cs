using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TITKUL.PMTTCU.Web.ApiClients;

namespace TITKUL.PMTTCU.Web.Pages;

public sealed class CauLacBoChiTietModel(BackendApiClient api) : PageModel
{
    public CauLacBoModel.PublicClubItem? Item { get; private set; }
    public string? Error { get; private set; }
    public string? Code { get; private set; }
    public bool ClubNotFound { get; private set; }

    public async Task<IActionResult> OnGetAsync(string code)
    {
        Code = code;
        var result = await api.GetPublicJsonResultAsync<ListEnvelope>("/api/v1/public/clubs");
        var envelope = result.Value;
        if (!result.IsAvailable || envelope is null)
        {
            Error = "Chưa tải được thông tin câu lạc bộ. Vui lòng thử lại sau.";
            Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            return Page();
        }

        Item = envelope.Items?.FirstOrDefault(item =>
            string.Equals(item.Club.Code, code, StringComparison.OrdinalIgnoreCase));
        if (Item is null)
        {
            ClubNotFound = true;
            Response.StatusCode = StatusCodes.Status404NotFound;
        }

        return Page();
    }

    private sealed record ListEnvelope(IReadOnlyList<CauLacBoModel.PublicClubItem>? Items);
}
