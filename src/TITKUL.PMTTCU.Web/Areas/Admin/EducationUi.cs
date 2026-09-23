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
        "DANG_KY" => "Đã đăng ký",
        "DRAFT" => "Nháp",
        "PUBLISHED" => "Đã đăng",
        "ARCHIVED" => "Lưu trữ",
        "ACTIVE" => "Đang dùng",
        "INACTIVE" => "Ngưng",
        "SCHEDULED" => "Đã xếp",
        "COMPLETED" => "Đã học",
        "CANCELLED" => "Đã hủy",
        "OFFLINE" => "Trực tiếp",
        "ONLINE" => "Trực tuyến",
        "HYBRID" => "Kết hợp",
        "NAM" => "Nam",
        "NU" => "Nữ",
        "KHAC" => "Khác",
        _ => status
    };

    public static DateOnly MondayOf(DateOnly day)
    {
        var offset = ((int)day.DayOfWeek + 6) % 7;
        return day.AddDays(-offset);
    }

    public static string Weekday(DateOnly day) => day.DayOfWeek switch
    {
        DayOfWeek.Monday => "Thứ 2",
        DayOfWeek.Tuesday => "Thứ 3",
        DayOfWeek.Wednesday => "Thứ 4",
        DayOfWeek.Thursday => "Thứ 5",
        DayOfWeek.Friday => "Thứ 6",
        DayOfWeek.Saturday => "Thứ 7",
        _ => "Chủ nhật"
    };
}
