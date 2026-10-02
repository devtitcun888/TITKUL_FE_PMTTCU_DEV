using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TITKUL.PMTTCU.Web.ApiClients;

namespace TITKUL.PMTTCU.Web.Pages;

public class LopHocMaModel : PageModel
{
    private readonly BackendApiClient _api;
    public LopHocMaModel(BackendApiClient api) => _api = api;
    public PublicClass? Item { get; private set; }
    public JoinStatus? Join { get; private set; }
    public IReadOnlyList<PublicMaterial> Materials { get; private set; } = [];
    public string? ErrorMessage { get; private set; }
    public string? SupplementalErrorMessage { get; private set; }
    public bool MaterialsUnavailable { get; private set; }
    public bool ResourceNotFound { get; private set; }

    public async Task<IActionResult> OnGetAsync(string ma)
    {
        var classPath = $"/api/v1/public/classes/{Uri.EscapeDataString(ma)}";
        var classResult = await _api.GetPublicJsonResultAsync<ItemEnvelope<PublicClass>>(classPath);
        var body = classResult.Value;
        Item = body?.Item;
        if (Item is null)
        {
            ResourceNotFound = classResult.IsNotFound || classResult.IsAvailable;
            ErrorMessage = ResourceNotFound
                ? "Không tìm thấy lớp công khai."
                : "Chưa thể tải thông tin lớp. Vui lòng thử lại sau.";
            Response.StatusCode = ResourceNotFound
                ? StatusCodes.Status404NotFound
                : StatusCodes.Status503ServiceUnavailable;
            return Page();
        }

        var joinTask = _api.GetPublicJsonResultAsync<ItemEnvelope<JoinStatus>>(classPath + "/join-status");
        var materialsTask = _api.GetPublicJsonResultAsync<ListEnvelope<PublicMaterial>>(classPath + "/materials");
        await Task.WhenAll(joinTask, materialsTask);
        var join = await joinTask;
        Join = join.Value?.Item;
        var materials = await materialsTask;
        MaterialsUnavailable = !materials.IsAvailable || materials.Value is null;
        Materials = materials.Value?.Items ?? [];
        if (!join.IsAvailable || join.Value is null || MaterialsUnavailable)
        {
            SupplementalErrorMessage = "Một số thông tin tham gia hoặc học liệu chưa tải được. Vui lòng thử lại sau.";
        }
        return Page();
    }

    public async Task<IActionResult> OnGetHocLieuAsync(string ma, Guid id)
    {
        var file = await _api.GetFileResultAsync($"/api/v1/public/materials/{id}/file");
        return HostFile.Download(file, "hoc-lieu");
    }

    public async Task<IActionResult> OnGetCalendarAsync(string ma)
    {
        var classResult = await _api.GetPublicJsonResultAsync<ItemEnvelope<PublicClass>>($"/api/v1/public/classes/{Uri.EscapeDataString(ma)}");
        var item = classResult.Value?.Item;
        if (item is null) return classResult.IsNotFound || classResult.IsAvailable ? NotFound() : StatusCode(503);

        var now = DateTimeOffset.UtcNow;
        var sessions = (item.Sessions ?? [])
            .Where(session => session.Status == "SCHEDULED" && session.StartAt > now)
            .OrderBy(session => session.StartAt)
            .ToArray();
        if (sessions.Length == 0) return NotFound();

        var calendar = new StringBuilder();
        AppendFolded(calendar, "BEGIN:VCALENDAR");
        AppendFolded(calendar, "VERSION:2.0");
        AppendFolded(calendar, "PRODID:-//PMTTCU//Lich hoc cong dong//VI");
        AppendFolded(calendar, "CALSCALE:GREGORIAN");
        AppendFolded(calendar, "METHOD:PUBLISH");
        foreach (var session in sessions)
        {
            var uidSource = $"{item.Code}|{session.StartAt.UtcTicks}|{session.Title}";
            var uid = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(uidSource))).ToLowerInvariant();
            var summary = CalendarEscape($"{item.Name} - {session.Title}");
            AppendFolded(calendar, "BEGIN:VEVENT");
            AppendFolded(calendar, "UID:" + uid + "@pmt-tantru");
            AppendFolded(calendar, "DTSTAMP:" + IcsUtc(now));
            AppendFolded(calendar, "DTSTART:" + IcsUtc(session.StartAt));
            AppendFolded(calendar, "DTEND:" + IcsUtc(session.EndAt));
            AppendFolded(calendar, "SUMMARY:" + summary);
            AppendFolded(calendar, "DESCRIPTION:" + CalendarEscape($"Lớp {item.Code} · {item.ProgramName} · Hình thức: {session.Mode}"));
            AppendFolded(calendar, "BEGIN:VALARM");
            AppendFolded(calendar, "TRIGGER:-PT30M");
            AppendFolded(calendar, "ACTION:DISPLAY");
            AppendFolded(calendar, "DESCRIPTION:" + summary);
            AppendFolded(calendar, "END:VALARM");
            AppendFolded(calendar, "END:VEVENT");
        }
        AppendFolded(calendar, "END:VCALENDAR");

        var safeCode = new string(item.Code.Where(char.IsAsciiLetterOrDigit).ToArray());
        return File(Encoding.UTF8.GetBytes(calendar.ToString()), "text/calendar; charset=utf-8", $"lich-hoc-{safeCode}.ics");
    }

    private static string IcsUtc(DateTimeOffset value) => value.UtcDateTime.ToString("yyyyMMdd'T'HHmmss'Z'", System.Globalization.CultureInfo.InvariantCulture);

    private static string CalendarEscape(string value) => value
        .Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace("\r\n", "\n", StringComparison.Ordinal)
        .Replace("\n", "\\n", StringComparison.Ordinal)
        .Replace("\r", "", StringComparison.Ordinal)
        .Replace(",", "\\,", StringComparison.Ordinal)
        .Replace(";", "\\;", StringComparison.Ordinal);

    private static void AppendFolded(StringBuilder output, string line)
    {
        var bytesOnLine = 0;
        foreach (var rune in line.EnumerateRunes())
        {
            if (bytesOnLine + rune.Utf8SequenceLength > 75)
            {
                output.Append("\r\n ");
                bytesOnLine = 1;
            }
            output.Append(rune.ToString());
            bytesOnLine += rune.Utf8SequenceLength;
        }
        output.Append("\r\n");
    }

    public bool HasUpcomingSessions => Item?.Sessions?.Any(session => session.Status == "SCHEDULED" && session.StartAt > DateTimeOffset.UtcNow) == true;

    public sealed record PublicSession(string Title, DateTimeOffset StartAt, DateTimeOffset EndAt, string Mode, string Status, string? Location = null);
    public sealed record PublicClass(string Code, string Name, string ProgramName, string? Description, DateOnly StartDate, DateOnly EndDate, int Capacity, int Remaining, string Status, bool CanRegister, string? ClosedReason, IReadOnlyList<PublicSession>? Sessions, DateTimeOffset? RegistrationOpensAt = null, DateTimeOffset? RegistrationClosesAt = null);
    public sealed record JoinStatus(bool CanJoin, string? Url, string? SessionTitle, DateTimeOffset? StartAt, DateTimeOffset? EndAt, string? Reason);
    public sealed record PublicMaterial(Guid Id, string Title, string? Description, string FileName, string MimeType, long Size, string? FileUrl = null);
    private sealed record ItemEnvelope<T>(T? Item);
    private sealed record ListEnvelope<T>(IReadOnlyList<T>? Items);
}
