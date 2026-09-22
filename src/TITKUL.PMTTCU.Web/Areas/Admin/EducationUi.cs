namespace TITKUL.PMTTCU.Web.Areas.Admin;

public static class EducationUi
{
    public static string Status(string status) => status switch
    {
        "DU_KIEN" => "Dự kiến",
        "MO_DANG_KY" => "Mở đăng ký",
        "DANG_HOC" => "Đang học",
        "DONG" => "Đóng",
        "HUY" => "Hủy",
        "DRAFT" => "Nháp",
        "ACTIVE" => "Đang dùng",
        "INACTIVE" => "Ngưng",
        _ => status
    };
}
