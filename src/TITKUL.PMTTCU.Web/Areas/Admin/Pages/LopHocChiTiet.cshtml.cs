using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TITKUL.PMTTCU.Web.ApiClients;
using TITKUL.PMTTCU.Web.Observability;

namespace TITKUL.PMTTCU.Web.Areas.Admin.Pages;

public class LopHocChiTietModel : PageModel
{
    private readonly BackendApiClient _api;
    public LopHocChiTietModel(BackendApiClient api) => _api = api;
    public ClassItem? Item { get; private set; }
    public IReadOnlyList<string> AllowedTransitions { get; private set; } = [];
    public IReadOnlyList<OptionItem> Programs { get; private set; } = [];
    public IReadOnlyList<OptionItem> Rooms { get; private set; } = [];
    public IReadOnlyList<SessionItem> Sessions { get; private set; } = [];
    public IReadOnlyList<TeacherItem> Teachers { get; private set; } = [];
    public IReadOnlyList<StaffItem> Staff { get; private set; } = [];
    public IReadOnlyList<EnrollmentItem> Enrollments { get; private set; } = [];
    public IReadOnlyList<MaterialItem> Materials { get; private set; } = [];
    public string? ErrorMessage { get; private set; }
    public bool CanManage { get; private set; }
    public bool CanMaterial { get; private set; }
    public bool CanEdit => CanManage && Item is { Status: not "DONG" and not "HUY" };

    [BindProperty] public Guid? ProgramId { get; set; }
    [BindProperty] public Guid? RoomId { get; set; }
    [BindProperty] public string Code { get; set; } = "";
    [BindProperty] public string Name { get; set; } = "";
    [BindProperty] public DateOnly? StartDate { get; set; }
    [BindProperty] public DateOnly? EndDate { get; set; }
    [BindProperty] public int Capacity { get; set; }
    [BindProperty] public bool Public { get; set; } = true;
    [BindProperty] public string ToStatus { get; set; } = "";
    [BindProperty] public string SessionTitle { get; set; } = "";
    [BindProperty] public Guid? SessionRoomId { get; set; }
    [BindProperty] public string SessionStart { get; set; } = "";
    [BindProperty] public string SessionEnd { get; set; } = "";
    [BindProperty] public string SessionMode { get; set; } = "OFFLINE";
    [BindProperty] public string? MeetingUrl { get; set; }
    [BindProperty] public Guid? TeacherUserId { get; set; }
    [BindProperty] public string MaterialTitle { get; set; } = "";
    [BindProperty] public string? MaterialDescription { get; set; }
    [BindProperty] public bool MaterialPublic { get; set; }
    [BindProperty] public IFormFile? MaterialFile { get; set; }

    public string? QrUrl { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid id) => await LoadAsync(id);

    public async Task<IActionResult> OnGetQrAsync(Guid id)
    {
        if (!HasManage()) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        var (bytes, _) = await _api.GetQrAsync($"/api/v1/admin/classes/{id}/qr", token);
        if (bytes is null) return NotFound();
        return File(bytes, "image/png");
    }

    public async Task<IActionResult> OnPostAsync(Guid id)
    {
        if (!HasManage()) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        var response = await _api.SendJsonAsync(HttpMethod.Put, $"/api/v1/admin/classes/{id}", token, new
        {
            programId = ProgramId,
            roomId = RoomId,
            code = Code,
            name = Name,
            startDate = StartDate,
            endDate = EndDate,
            capacity = Capacity,
            @public = Public
        });
        if (response is null || !response.IsSuccessStatusCode)
        {
            ErrorMessage = "Không lưu được lớp. Lớp đóng hoặc hủy thì không sửa được.";
            return await LoadAsync(id);
        }

        return Redirect($"/admin/lop-hoc/{id}");
    }

    public async Task<IActionResult> OnPostTransitionAsync(Guid id)
    {
        if (!HasManage()) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        var response = await _api.SendJsonAsync(HttpMethod.Post, $"/api/v1/admin/classes/{id}/transition", token, new { toStatus = ToStatus });
        if (response is null || !response.IsSuccessStatusCode)
        {
            ErrorMessage = "Không chuyển được trạng thái. Chỉ các bước được phép mới thực hiện.";
            return await LoadAsync(id);
        }

        return Redirect($"/admin/lop-hoc/{id}");
    }

    public async Task<IActionResult> OnPostSessionAsync(Guid id)
    {
        if (!HasManage()) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        var response = await _api.SendJsonAsync(HttpMethod.Post, $"/api/v1/admin/classes/{id}/sessions", token, new
        {
            title = SessionTitle,
            roomId = SessionRoomId,
            startAt = ToOffset(SessionStart),
            endAt = ToOffset(SessionEnd),
            mode = SessionMode,
            meetingUrl = MeetingUrl
        });
        if (response is null || !response.IsSuccessStatusCode)
        {
            ErrorMessage = await ReadErrorAsync(response) ?? "Không lưu được buổi học. Kiểm tra giờ, phòng và giảng viên trùng lịch.";
            return await LoadAsync(id);
        }

        return Redirect($"/admin/lop-hoc/{id}");
    }

    public async Task<IActionResult> OnPostCancelSessionAsync(Guid id, Guid sessionId)
    {
        if (!HasManage()) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        var response = await _api.SendJsonAsync(HttpMethod.Post, $"/api/v1/admin/sessions/{sessionId}/cancel", token, new { });
        if (response is null || !response.IsSuccessStatusCode)
        {
            ErrorMessage = "Không hủy được buổi học.";
            return await LoadAsync(id);
        }

        return Redirect($"/admin/lop-hoc/{id}");
    }

    public async Task<IActionResult> OnPostAssignAsync(Guid id)
    {
        if (!HasManage()) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        var response = await _api.SendJsonAsync(HttpMethod.Post, $"/api/v1/admin/classes/{id}/teachers", token, new { userId = TeacherUserId });
        if (response is null || !response.IsSuccessStatusCode)
        {
            ErrorMessage = await ReadErrorAsync(response) ?? "Không phân công được giảng viên.";
            return await LoadAsync(id);
        }

        return Redirect($"/admin/lop-hoc/{id}");
    }

    public async Task<IActionResult> OnPostMaterialAsync(Guid id)
    {
        if (!Has("education.material.manage")) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        if (MaterialFile is null)
        {
            ErrorMessage = "Chọn tệp PDF hoặc ảnh JPEG/PNG.";
            return await LoadAsync(id);
        }

        using var content = new MultipartFormDataContent();
        await using var stream = MaterialFile.OpenReadStream();
        using var fileContent = new StreamContent(stream);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(MaterialFile.ContentType ?? "application/octet-stream");
        content.Add(fileContent, "file", MaterialFile.FileName);
        var uploaded = await _api.PostMultipartAsync("/api/v1/admin/files", token, content);
        if (uploaded is null || !uploaded.IsSuccessStatusCode)
        {
            ErrorMessage = "Không tải được tệp. Chỉ nhận PDF/JPEG/PNG đúng chữ ký.";
            return await LoadAsync(id);
        }

        var saved = await uploaded.Content.ReadFromJsonAsync<UploadBody>();
        var response = await _api.SendJsonAsync(HttpMethod.Post, $"/api/v1/admin/classes/{id}/materials", token, new
        {
            title = MaterialTitle,
            description = MaterialDescription,
            storageKey = saved?.StorageKey,
            fileName = MaterialFile.FileName,
            mimeType = MaterialFile.ContentType,
            size = MaterialFile.Length,
            @public = MaterialPublic,
            status = "PUBLISHED"
        });
        if (response is null || !response.IsSuccessStatusCode)
        {
            ErrorMessage = await ReadErrorAsync(response) ?? "Không lưu được học liệu.";
            return await LoadAsync(id);
        }

        return Redirect($"/admin/lop-hoc/{id}");
    }

    public async Task<IActionResult> OnGetMaterialFileAsync(Guid id, Guid materialId)
    {
        if (!HasView()) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        var file = await _api.GetFileAsync($"/api/v1/admin/materials/{materialId}/file", token);
        if (file.Bytes is null) return NotFound();
        return File(file.Bytes, file.ContentType ?? "application/octet-stream", file.FileName ?? "hoc-lieu");
    }

    public async Task<IActionResult> OnPostRemoveTeacherAsync(Guid id, Guid userId)
    {
        if (!HasManage()) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        var response = await _api.SendJsonAsync(HttpMethod.Delete, $"/api/v1/admin/classes/{id}/teachers/{userId}", token, new { });
        if (response is null || !response.IsSuccessStatusCode)
        {
            ErrorMessage = "Không gỡ được giảng viên.";
            return await LoadAsync(id);
        }

        return Redirect($"/admin/lop-hoc/{id}");
    }

    private bool HasManage() => Has("education.manage");
    private bool HasView() => Has("education.manage") || Has("education.view") || Has("report.own_classes.view");
    private bool Has(string permission) => (HttpContext.Items["StaffProfile"] as StaffProfile)?.Permissions?.Contains(permission) == true;
    private string? Token() => Request.Cookies[AdminGateMiddleware.CookieName];

    private async Task<IActionResult> LoadAsync(Guid id)
    {
        CanManage = HasManage();
        CanMaterial = Has("education.material.manage");
        if (!HasView()) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        var body = await _api.GetJsonAsync<ClassEnvelope>($"/api/v1/admin/classes/{id}", token);
        Item = body?.Item;
        AllowedTransitions = body?.AllowedTransitions ?? [];
        if (CanManage)
        {
            var programs = await _api.GetJsonAsync<ListEnvelope<OptionItem>>("/api/v1/admin/programs?pageSize=100", token);
            var rooms = await _api.GetJsonAsync<ListEnvelope<OptionItem>>("/api/v1/admin/rooms?pageSize=100", token);
            var staff = await _api.GetJsonAsync<ListEnvelope<StaffItem>>("/api/v1/admin/education/staff", token);
            Programs = programs?.Items ?? [];
            Rooms = rooms?.Items ?? [];
            Staff = staff?.Items ?? [];
        }

        var sessions = await _api.GetJsonAsync<ListEnvelope<SessionItem>>($"/api/v1/admin/classes/{id}/sessions", token);
        var teachers = await _api.GetJsonAsync<ListEnvelope<TeacherItem>>($"/api/v1/admin/classes/{id}/teachers", token);
        var enrollments = await _api.GetJsonAsync<ListEnvelope<EnrollmentItem>>($"/api/v1/admin/classes/{id}/enrollments", token);
        var materials = await _api.GetJsonAsync<ListEnvelope<MaterialItem>>($"/api/v1/admin/classes/{id}/materials", token);
        Sessions = sessions?.Items ?? [];
        Teachers = teachers?.Items ?? [];
        Enrollments = enrollments?.Items ?? [];
        Materials = materials?.Items ?? [];
        if (Item is null)
        {
            ErrorMessage ??= "Không tìm thấy lớp hoặc không có quyền xem.";
        }
        else
        {
            ProgramId = Item.ProgramId;
            RoomId = Item.RoomId;
            Code = Item.Code;
            Name = Item.Name;
            StartDate = Item.StartDate;
            EndDate = Item.EndDate;
            Capacity = Item.Capacity;
            Public = Item.Public;
            var qr = await _api.GetQrAsync($"/api/v1/admin/classes/{id}/qr", token);
            QrUrl = qr.QrUrl;
        }

        return Page();
    }

    private static DateTimeOffset? ToOffset(string value) =>
        DateTimeOffset.TryParse(value + "+07:00", out var parsed) ? parsed : null;

    private static async Task<string?> ReadErrorAsync(HttpResponseMessage? response)
    {
        if (response is null) return null;
        try
        {
            var body = await response.Content.ReadFromJsonAsync<ApiErr>();
            return string.IsNullOrWhiteSpace(body?.Message) ? null : body.Message;
        }
        catch (Exception)
        {
            return null;
        }
    }

    public sealed record ClassItem(Guid Id, Guid ProgramId, Guid? RoomId, string Code, string Name, DateOnly StartDate, DateOnly EndDate, int Capacity, string Status, bool Public);
    public sealed record OptionItem(Guid Id, string Code, string Name);
    public sealed record SessionItem(Guid Id, Guid ClassId, Guid? RoomId, string Title, DateTimeOffset StartAt, DateTimeOffset EndAt, string Mode, string? MeetingUrl, string Status);
    public sealed record TeacherItem(Guid Id, Guid ClassId, Guid UserId, string Username, string Role);
    public sealed record StaffItem(Guid Id, string Username, IReadOnlyList<string>? Roles);
    public sealed record EnrollmentItem(Guid Id, string Code, string FullName, string Phone, string Status, DateTimeOffset RegisteredAt);
    public sealed record MaterialItem(Guid Id, string Title, string? Description, string FileName, string MimeType, long Size, bool Public, string Status);
    private sealed record UploadBody(string? StorageKey);
    private sealed record ClassEnvelope(ClassItem? Item, IReadOnlyList<string>? AllowedTransitions);
    private sealed record ListEnvelope<T>(IReadOnlyList<T>? Items);
    private sealed record ApiErr(string? Code, string? Message);
}
